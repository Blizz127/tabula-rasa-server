using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Packets.Wargame.Client;
    using Packets.Wargame.Server;
    using Structures;

    /// <summary>
    /// Player-versus-player wargames. Only the DUEL branch is implemented: one challenger against
    /// one challenged player, no party, no clan, no battleground.
    ///
    /// The whole protocol is readable out of the 1.16.5.0 client. The manager side lives on
    /// sysentity.ClientWargameManagerId (23) and is client/wargame.py; the per-body side is
    /// opcode 696 on the actor's own entity, client/augmentations/actor.py Recv_WargameData.
    ///
    ///   client -> server   ChallengeUserToWargameByName 626 (targetName, timeMins, maxKills)
    ///                      WargameChallengeResponse     606 (accepted, pmMsg = None)
    ///                      WargameChallengeRevoked      638 ()
    ///                      SurrenderWargame             674 ()
    ///   server -> client   ChallengingToWargameDuel     635 (wargameId, targetName)
    ///                      ChallengedToWargameDuel      603 (wargameId, agressorName)
    ///                      WargameChallengeRefused      637 (wargameId, yourPartyRefused)
    ///                      RevokeWargameChallenge       630 (wargameId)
    ///                      WargameStarted               633 (wargameId, enemyUserIds)
    ///                      SetWargameMaxKills           631 (wargameId, maxKills)
    ///                      DisplayWargameTimer          628 (wargameId, timeMs)
    ///                      WargameScoreboard            632 (wargameId, yourKills, theirKills, victimId, killerId)
    ///                      WargameVictory 608 / WargameDefeat 607 / WargameTied 634 / WargameCancelled 605 (wargameId)
    ///                      RemoveFromWargame            715 (wargameId)
    ///                      DisplayWargameMessage        604 (msgId, args = {})
    ///                      WargameData                  696 ({wargameId: sideToken}) on the actor
    ///
    /// Two ordering rules come straight out of the bytecode. First, the challenge packet is what
    /// creates the client's WargameStatus (_AddWargameStatus), and WargameStarted,
    /// SetWargameMaxKills and DisplayWargameTimer all go through _GetWargameStatusEnsured, which
    /// returns None for an unknown id - so a duel may never be started for an id the client was
    /// not challenged under. Second, Recv_WargameScoreboard only records a score while bActive, so
    /// the last scoreboard has to precede the victory or defeat that clears it.
    ///
    /// The client will not aim at a body it has been told is Friendly
    /// (client/actions/targetedaction.py: targetType TARGET_HOSTILE with category != HOSTILE is an
    /// invalid target), so a duel also flips TargetCategory to Hostile - but only in the two
    /// duellists' own clients, never for onlookers.
    /// </summary>
    public class WargameManager
    {
        #region Singleton

        private static WargameManager _instance;
        private static readonly object InstanceLock = new object();

        public static WargameManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new WargameManager();
                    }
                }

                return _instance;
            }
        }

        internal WargameManager()
        {
        }

        #endregion

        /// <summary>
        /// shared/teamdefs.py WargameTeamIds, numbered by shared/stuple.py Struct: teamShirt is
        /// field 0 and teamSkin is field 1. The client only compares the two tokens for equality
        /// (actor.py GetWargameParticipantStatus), so which side holds which is arbitrary; the
        /// challenger is the shirt.
        /// </summary>
        public const int TeamShirt = 0;
        public const int TeamSkin = 1;

        /// <summary>
        /// How long an unanswered challenge stands. Server choice: the client has no constant for
        /// it, and none survives - it only has the text for the outcome,
        /// PM_WARGAME_CHALLENGE_TIMED_OUT ("The Wargame challenge has timed out."). One minute
        /// matches the one other pending-invitation timeout this server already keeps
        /// (TradeManager.PendingInviteTimeoutMs).
        /// </summary>
        public const long ChallengeTimeoutMs = 60 * 1000;

        /// <summary>
        /// What a challenge that named no length or score gets, which is every challenge made from
        /// the radial menu (client/communicator.py ChallengeWargameDuel passes the name alone and
        /// leaves both at 0). Server choice: no original duel default survives. Kept at the
        /// smallest values that still describe a duel - first blood, and a five minute bout - so
        /// that a match ends rather than running on under an invented rule.
        /// </summary>
        public const int DefaultMaxKills = 1;
        public const int DefaultTimeMins = 5;

        /// <summary>
        /// Bounds on what a typed "/duel Name 90 500" may ask for. Server choice, no original
        /// limits survive; these only stop one command from pinning two players in a wargame for
        /// an afternoon.
        /// </summary>
        public const int MaxTimeMins = 60;
        public const int MaxKillsCap = 10;

        /// <summary>
        /// How far apart the two duellists may drift before the duel stops. Server choice: the
        /// client has no leave radius for a duel, and no original rule survives. This is the
        /// server's own maximum engagement range (MissileManager.MaxTargetDistance, 128 units) -
        /// past it neither player could shoot the other anyway, so it is the widest value that
        /// still describes a fight. Note what it does *not* do: separating cancels the duel
        /// (WargameCancelled, nobody wins), because no surviving source says a duellist who walks
        /// away forfeits.
        /// </summary>
        public const float SeparationRange = 128f;

        /// <summary>
        /// The refusal ids a client may ask the server to relay to the challenger. The refusing
        /// client names the message (client/wargame.py OnRefuseWargameDuelChallenge sends
        /// PM_WARGAME_REFUSED), so it is input, not instruction: anything outside this list
        /// becomes the plain refusal.
        /// </summary>
        private static readonly HashSet<PlayerMessage> RelayableRefusals = new HashSet<PlayerMessage>
        {
            PlayerMessage.PmWargameRefused,
            PlayerMessage.PmWargameInviteIgnored
        };

        internal sealed class Duel
        {
            public uint WargameId;
            public Client Challenger;
            public Client Target;
            public string ChallengerName;
            public string TargetName;
            public uint ChallengerAccountId;
            public uint TargetAccountId;
            public long CreatedTick;
            public bool Active;
            public long DurationMs;
            public long EndsAtTick;
            public int MaxKills;
            public int ChallengerKills;
            public int TargetKills;

            public Client Other(Client client) => ReferenceEquals(client, Challenger) ? Target : Challenger;
            public bool Holds(Client client) => ReferenceEquals(client, Challenger) || ReferenceEquals(client, Target);
            public int SideOf(Client client) => ReferenceEquals(client, Challenger) ? TeamShirt : TeamSkin;
            public int KillsOf(Client client) => ReferenceEquals(client, Challenger) ? ChallengerKills : TargetKills;
            public uint AccountOf(Client client) => ReferenceEquals(client, Challenger) ? ChallengerAccountId : TargetAccountId;
            public string NameOf(Client client) => ReferenceEquals(client, Challenger) ? ChallengerName : TargetName;
        }

        /// <summary>
        /// One wargame per player, keyed by account id for both participants. The client's own
        /// vocabulary is what limits it to one: a challenger who tries again is told
        /// PM_WARGAME_FAIL_WAIT_FOR_RESPONSE and a challenged player who tries to start their own
        /// is told PM_WARGAME_ACCEPT_OR_DECLINE_FIRST.
        /// </summary>
        private readonly Dictionary<uint, Duel> _byAccount = new Dictionary<uint, Duel>();

        /// <summary>
        /// Never reused inside a session: client/wargame.py _AddWargameStatus logs
        /// "Duplicate wargameId used" and keeps the stale entry, and only
        /// Recv_RevokeWargameChallenge ever deletes one - a finished duel's status lingers.
        /// </summary>
        private uint _nextWargameId = 1;
        private readonly object _idLock = new object();

        private uint NextWargameId
        {
            get
            {
                lock (_idLock)
                    return _nextWargameId++;
            }
        }

        internal IReadOnlyDictionary<uint, Duel> Duels => _byAccount;

        #region Handlers

        /// <summary>
        /// Right-click a player, "Challenge to Duel" - or type the slash command with a length and
        /// a score. Every refusal below is a line the client already has the text for; before this
        /// existed, opcode 626 fell through to "Unhandled game opcode" and the player saw nothing
        /// at all.
        /// </summary>
        internal void ChallengeUserToWargameByName(Client client, ChallengeUserToWargameByNamePacket packet)
        {
            if (!InWorld(client))
                return;

            var name = packet.TargetName?.Trim() ?? string.Empty;

            if (_byAccount.TryGetValue(client.AccountEntry.Id, out var existing))
            {
                if (existing.Active)
                    Message(client, PlayerMessage.PmWargameYouAlreadyWargaming);
                else if (ReferenceEquals(existing.Challenger, client))
                    Message(client, PlayerMessage.PmWargameFailWaitForResponse);
                else
                    Message(client, PlayerMessage.PmWargameAcceptOrDeclineFirst);
                return;
            }

            var target = FindIngame(name);

            if (target == null)
            {
                Message(client, PlayerMessage.PmWargameNoTargetByName, "target", name);
                return;
            }

            var targetName = target.Player.FamilyName;

            if (ReferenceEquals(target, client))
            {
                Message(client, PlayerMessage.PmWargameCannotChallengeYourself);
                return;
            }

            if (_byAccount.TryGetValue(target.AccountEntry.Id, out var theirs))
            {
                if (theirs.Active)
                    Message(client, PlayerMessage.PmWargameTargetAlreadyWargaming, "target", targetName);
                else if (ReferenceEquals(theirs.Challenger, target))
                    Message(client, PlayerMessage.PmWargameAlreadyChallenging, "target", targetName);
                else
                    Message(client, PlayerMessage.PmWargameAlreadyChallenged, "target", targetName);
                return;
            }

            if (target.Player.IgnoredPlayers.Contains(client.AccountEntry.Id))
            {
                Message(client, PlayerMessage.PmWargameInviteIgnored, "target", targetName);
                return;
            }

            var party = PartyManager.Instance.PartyOf(client);

            if (party != null && ReferenceEquals(party, PartyManager.Instance.PartyOf(target)))
            {
                Message(client, PlayerMessage.PmWargameFailInSameParty, "target", targetName);
                return;
            }

            if (!SameMap(client, target))
            {
                Message(client, PlayerMessage.PmWargameChallengeNotOnSameMap, "target", targetName);
                return;
            }

            if (target.Player.State == CharacterState.Dead)
            {
                Message(client, PlayerMessage.PmWargameRefusedPlayerDead, "target", targetName);
                return;
            }

            var duel = new Duel
            {
                WargameId = NextWargameId,
                Challenger = client,
                Target = target,
                ChallengerName = client.Player.FamilyName,
                TargetName = targetName,
                ChallengerAccountId = client.AccountEntry.Id,
                TargetAccountId = target.AccountEntry.Id,
                CreatedTick = Environment.TickCount64,
                MaxKills = Clamp(packet.MaxKills, DefaultMaxKills, MaxKillsCap),
                DurationMs = Clamp(packet.TimeMins, DefaultTimeMins, MaxTimeMins) * 60L * 1000L
            };

            _byAccount[duel.ChallengerAccountId] = duel;
            _byAccount[duel.TargetAccountId] = duel;

            // The two dialogs, then the two chat lines that go with them.
            client.CallMethod(SysEntity.ClientWargameManagerId, new ChallengingToWargameDuelPacket(duel.WargameId, duel.TargetName));
            target.CallMethod(SysEntity.ClientWargameManagerId, new ChallengedToWargameDuelPacket(duel.WargameId, duel.ChallengerName));

            Message(client, PlayerMessage.PmWargameDuelPlayerChallenged, "target", duel.TargetName);
            Message(target, PlayerMessage.PmWargameDuelPlayerChallengeReceived, "challenger", duel.ChallengerName);
        }

        /// <summary>Accept or Decline on the challenged player's dialog.</summary>
        internal void WargameChallengeResponse(Client client, WargameChallengeResponsePacket packet)
        {
            if (!InWorld(client))
                return;

            // Only the challenged player answers, and only while the challenge stands.
            if (!_byAccount.TryGetValue(client.AccountEntry.Id, out var duel) || duel.Active || !ReferenceEquals(duel.Target, client))
                return;

            var challenger = duel.Challenger;

            if (!packet.Accepted)
            {
                Forget(duel);

                // The challenger's dialog has to be closed from here. The decliner's own dialog is
                // closed by the button they pressed; sending it to them as well is a server
                // choice, and _CloseWargameChallengeUI is idempotent.
                challenger.CallMethod(SysEntity.ClientWargameManagerId, new WargameChallengeRefusedPacket(duel.WargameId));
                client.CallMethod(SysEntity.ClientWargameManagerId, new WargameChallengeRefusedPacket(duel.WargameId));

                var refusal = packet.PmMsg.HasValue && RelayableRefusals.Contains(packet.PmMsg.Value)
                    ? packet.PmMsg.Value : PlayerMessage.PmWargameRefused;

                Message(challenger, refusal, "target", duel.TargetName);
                Message(client, PlayerMessage.PmWargameYouRefused, "challenger", duel.ChallengerName);
                return;
            }

            // The dialog can sit on screen while the world moves on.
            if (!InWorld(challenger) || !SameMap(client, challenger))
            {
                Forget(duel);

                client.CallMethod(SysEntity.ClientWargameManagerId, new RevokeWargameChallengePacket(duel.WargameId));
                Message(client, PlayerMessage.PmWargameResponseNotOnSameMap, "challenger", duel.ChallengerName);

                if (InWorld(challenger))
                {
                    challenger.CallMethod(SysEntity.ClientWargameManagerId, new RevokeWargameChallengePacket(duel.WargameId));
                    Message(challenger, PlayerMessage.PmWargameRespondedButNotOnSameMap, "target", duel.TargetName);
                }

                return;
            }

            Start(duel);
        }

        /// <summary>Revoke on the challenger's own dialog.</summary>
        internal void WargameChallengeRevoked(Client client)
        {
            if (!InWorld(client))
                return;

            if (!_byAccount.TryGetValue(client.AccountEntry.Id, out var duel) || duel.Active || !ReferenceEquals(duel.Challenger, client))
                return;

            Forget(duel);

            client.CallMethod(SysEntity.ClientWargameManagerId, new RevokeWargameChallengePacket(duel.WargameId));
            Message(client, PlayerMessage.PmWargameChallengeRevoked);

            if (!InWorld(duel.Target))
                return;

            duel.Target.CallMethod(SysEntity.ClientWargameManagerId, new RevokeWargameChallengePacket(duel.WargameId));
            Message(duel.Target, PlayerMessage.PmWargameRevoked, "player", duel.ChallengerName);
        }

        /// <summary>
        /// The /surrender command (client/communicator.py SurrenderWargame -> opcode 674, no
        /// arguments). PM_WARGAME_NOT_IN_SQUAD_OR_DUEL is the client's own answer for a player who
        /// is not in one.
        /// </summary>
        internal void SurrenderWargame(Client client)
        {
            if (!InWorld(client))
                return;

            if (!_byAccount.TryGetValue(client.AccountEntry.Id, out var duel) || !duel.Active)
            {
                Message(client, PlayerMessage.PmWargameNotInSquadOrDuel);
                return;
            }

            Finish(duel, winner: duel.Other(client), loser: client);
        }

        #endregion

        #region World events

        /// <summary>
        /// Called from MapChannelManager.DetachFromMap: logging out, dropping or changing map.
        /// A standing challenge is dropped; a duel under way is cancelled for the player left
        /// behind, and the leaver is told they left it, which is exactly what
        /// PM_WARGAME_YOU_LEFT is for.
        ///
        /// It is not a forfeit. No surviving source says who wins when a duellist walks out, and
        /// inventing one would be inventing gameplay.
        /// </summary>
        internal void RemovePlayer(Client client)
        {
            if (client?.AccountEntry == null || !_byAccount.TryGetValue(client.AccountEntry.Id, out var duel))
                return;

            var other = duel.Other(client);
            var active = duel.Active;

            Forget(duel);
            Strip(duel, client);
            Strip(duel, other);

            if (client.State != ClientState.Disconnected)
            {
                if (active)
                    client.CallMethod(SysEntity.ClientWargameManagerId, new RemoveFromWargamePacket(duel.WargameId));
                else
                    client.CallMethod(SysEntity.ClientWargameManagerId, new RevokeWargameChallengePacket(duel.WargameId));
            }

            if (!InWorld(other))
                return;

            if (active)
            {
                other.CallMethod(SysEntity.ClientWargameManagerId, new WargameCancelledPacket(duel.WargameId));
                Message(other, PlayerMessage.PmWargamePlayerLeft, "player", duel.NameOf(client));
                return;
            }

            other.CallMethod(SysEntity.ClientWargameManagerId, new RevokeWargameChallengePacket(duel.WargameId));

            // A challenger who leaves has revoked; a challenged player who leaves has, from the
            // challenger's side, simply never answered.
            if (ReferenceEquals(duel.Challenger, client))
                Message(other, PlayerMessage.PmWargameRevoked, "player", duel.ChallengerName);
            else
                Message(other, PlayerMessage.PmWargameChallengeTimedOut);
        }

        /// <summary>
        /// Re-sends what a cell introduction would otherwise overwrite. CreatePlayerEntityData
        /// carries TargetCategory Friendly for every player and no wargame data at all, so a
        /// duellist who walks out of a cell and back would be re-introduced as an untouchable
        /// ally. The observer learns the subject's wargame data either way (it drives the overhead
        /// icon for onlookers too); the Hostile category goes only to the other duellist.
        /// </summary>
        internal void OnActorIntroduced(Client observer, Client subject)
        {
            if (observer?.AccountEntry == null || subject?.AccountEntry == null || ReferenceEquals(observer, subject))
                return;

            if (!_byAccount.TryGetValue(subject.AccountEntry.Id, out var duel) || !duel.Active)
                return;

            observer.CallMethod(subject.Player.EntityId, new WargameDataPacket(SideDictionary(duel, subject)));

            if (duel.Holds(observer))
                observer.CallMethod(subject.Player.EntityId, new TargetCategoryPacket(TargetCategory.Hostile));
        }

        /// <summary>
        /// Once per main-loop pass, beside PartyManager.ExpireHeldMembers: times out unanswered
        /// challenges, ends duels whose clock has run out, and stops duels whose two players have
        /// drifted apart.
        /// </summary>
        internal void Worker() => Worker(Environment.TickCount64);

        internal void Worker(long now)
        {
            if (_byAccount.Count == 0)
                return;

            foreach (var duel in _byAccount.Values.Distinct().ToList())
            {
                if (!_byAccount.TryGetValue(duel.ChallengerAccountId, out var live) || !ReferenceEquals(live, duel))
                    continue;

                if (!duel.Active)
                {
                    if (now - duel.CreatedTick < ChallengeTimeoutMs)
                        continue;

                    Forget(duel);

                    foreach (var side in new[] { duel.Challenger, duel.Target })
                        if (InWorld(side))
                        {
                            side.CallMethod(SysEntity.ClientWargameManagerId, new RevokeWargameChallengePacket(duel.WargameId));
                            Message(side, PlayerMessage.PmWargameChallengeTimedOut);
                        }

                    continue;
                }

                if (!InWorld(duel.Challenger) || !InWorld(duel.Target) || !SameMap(duel.Challenger, duel.Target))
                {
                    Cancel(duel);
                    continue;
                }

                if (Vector3.Distance(duel.Challenger.Player.Position, duel.Target.Player.Position) > SeparationRange)
                {
                    Cancel(duel);
                    continue;
                }

                if (now >= duel.EndsAtTick)
                {
                    if (duel.ChallengerKills == duel.TargetKills)
                        Tie(duel);
                    else if (duel.ChallengerKills > duel.TargetKills)
                        Finish(duel, duel.Challenger, duel.Target);
                    else
                        Finish(duel, duel.Target, duel.Challenger);
                }
            }
        }

        #endregion

        #region Combat

        /// <summary>
        /// Whether these two bodies are the two sides of a duel that is under way. This is the
        /// whole of the server's friendly-fire rule: no other pair of players may damage each
        /// other, and the pair may only do it while their own duel is running.
        /// </summary>
        internal bool AreDuelOpponents(Actor first, Actor second)
        {
            if (first == null || second == null || ReferenceEquals(first, second))
                return false;

            if (!(first is Manifestation) || !(second is Manifestation))
                return false;

            foreach (var duel in _byAccount.Values)
                if (duel.Active &&
                    (ReferenceEquals(duel.Challenger.Player, first) && ReferenceEquals(duel.Target.Player, second) ||
                     ReferenceEquals(duel.Target.Player, first) && ReferenceEquals(duel.Challenger.Player, second)))
                    return true;

            return false;
        }

        /// <summary>
        /// The blow that would have killed a duellist. The client is explicit that it costs
        /// nothing: shared/gameconstants.py gives a duel WARGAME_FLAGS_DUEL of
        /// WARGAME_BLOCK_INTERACTTIONS | WARGAME_IS_AGGRESSIVE, without the WARGAME_REZ_SICKNESS
        /// and WARGAME_WEAPON_DECAY that WARGAME_FLAGS_CLAN carries, and Recv_WargameDefeat does
        /// nothing but show text. So the server scores the takedown and lets the loser stand.
        ///
        /// MissileManager leaves the victim on one point of health. Server choice: the damage that
        /// was dealt stays dealt, only the death is refused, which is the smallest intervention
        /// that keeps the loser alive. Nothing surviving says a duel loser is healed.
        /// </summary>
        internal void ScoreDuelTakedown(Actor victim, Actor killer)
        {
            var duel = _byAccount.Values.FirstOrDefault(d => d.Active &&
                (ReferenceEquals(d.Challenger.Player, victim) && ReferenceEquals(d.Target.Player, killer) ||
                 ReferenceEquals(d.Target.Player, victim) && ReferenceEquals(d.Challenger.Player, killer)));

            if (duel == null)
                return;

            var killerClient = ReferenceEquals(duel.Challenger.Player, killer) ? duel.Challenger : duel.Target;
            var victimClient = duel.Other(killerClient);

            if (ReferenceEquals(killerClient, duel.Challenger))
                duel.ChallengerKills++;
            else
                duel.TargetKills++;

            // The scoreboard has to land while bActive is still set, so it precedes the ending.
            Scoreboard(duel, duel.AccountOf(victimClient), duel.AccountOf(killerClient));

            if (duel.KillsOf(killerClient) >= duel.MaxKills)
                Finish(duel, killerClient, victimClient);
        }

        #endregion

        #region Flow

        private void Start(Duel duel)
        {
            duel.Active = true;
            duel.EndsAtTick = Environment.TickCount64 + duel.DurationMs;

            var timeMs = (int)duel.DurationMs;

            foreach (var side in new[] { duel.Challenger, duel.Target })
            {
                var other = duel.Other(side);

                side.CallMethod(SysEntity.ClientWargameManagerId,
                    new WargameStartedPacket(duel.WargameId, new List<uint> { duel.AccountOf(other) }));
                side.CallMethod(SysEntity.ClientWargameManagerId, new SetWargameMaxKillsPacket(duel.WargameId, duel.MaxKills));
                side.CallMethod(SysEntity.ClientWargameManagerId, new DisplayWargameTimerPacket(duel.WargameId, timeMs));
            }

            // The bodies, last: the tracker is a manager-side window, but IsWargaming() and every
            // targeting and overhead rule that depends on it live on the actor.
            foreach (var side in new[] { duel.Challenger, duel.Target })
            {
                Broadcast(side, new WargameDataPacket(SideDictionary(duel, side)));
                duel.Other(side).CallMethod(side.Player.EntityId, new TargetCategoryPacket(TargetCategory.Hostile));
            }

            Message(duel.Challenger, PlayerMessage.PmWargameYourChallengeAccepted, "target", duel.TargetName);
            Message(duel.Target, PlayerMessage.PmWargameYouAccepted);
        }

        private void Finish(Duel duel, Client winner, Client loser)
        {
            Forget(duel);

            if (InWorld(winner))
                winner.CallMethod(SysEntity.ClientWargameManagerId, new WargameVictoryPacket(duel.WargameId));

            if (InWorld(loser))
                loser.CallMethod(SysEntity.ClientWargameManagerId, new WargameDefeatPacket(duel.WargameId));

            Strip(duel, winner);
            Strip(duel, loser);
        }

        private void Tie(Duel duel)
        {
            Forget(duel);

            foreach (var side in new[] { duel.Challenger, duel.Target })
                if (InWorld(side))
                    side.CallMethod(SysEntity.ClientWargameManagerId, new WargameTiedPacket(duel.WargameId));

            Strip(duel, duel.Challenger);
            Strip(duel, duel.Target);
        }

        private void Cancel(Duel duel)
        {
            Forget(duel);

            foreach (var side in new[] { duel.Challenger, duel.Target })
                if (InWorld(side))
                {
                    side.CallMethod(SysEntity.ClientWargameManagerId, new WargameCancelledPacket(duel.WargameId));
                    Message(side, PlayerMessage.PmWargameCancelled);
                }

            Strip(duel, duel.Challenger);
            Strip(duel, duel.Target);
        }

        private static void Scoreboard(Duel duel, uint victimAccountId, uint killerAccountId)
        {
            foreach (var side in new[] { duel.Challenger, duel.Target })
                if (InWorld(side))
                    side.CallMethod(SysEntity.ClientWargameManagerId, new WargameScoreboardPacket(
                        duel.WargameId, duel.KillsOf(side), duel.KillsOf(duel.Other(side)), victimAccountId, killerAccountId));
        }

        private void Forget(Duel duel)
        {
            if (_byAccount.TryGetValue(duel.ChallengerAccountId, out var mine) && ReferenceEquals(mine, duel))
                _byAccount.Remove(duel.ChallengerAccountId);

            if (_byAccount.TryGetValue(duel.TargetAccountId, out var theirs) && ReferenceEquals(theirs, duel))
                _byAccount.Remove(duel.TargetAccountId);
        }

        /// <summary>
        /// Takes the duel off a body: an empty wargameData dict, so IsWargaming() reads False
        /// again, and the opponent's own client is told this player is Friendly once more.
        /// </summary>
        private static void Strip(Duel duel, Client client)
        {
            if (!duel.Active || !InWorld(client))
                return;

            Broadcast(client, new WargameDataPacket(new Dictionary<uint, int>()));

            var other = duel.Other(client);

            if (InWorld(other))
                other.CallMethod(client.Player.EntityId, new TargetCategoryPacket(TargetCategory.Friendly));
        }

        private static Dictionary<uint, int> SideDictionary(Duel duel, Client client)
            => new Dictionary<uint, int> { { duel.WargameId, duel.SideOf(client) } };

        private static void Broadcast(Client client, WargameDataPacket packet)
        {
            var map = client.Player?.MapChannel;

            if (map == null)
                return;

            CellManager.Instance.CellCallMethod(map, client.Player, packet);
        }

        #endregion

        #region Helpers

        private static int Clamp(int requested, int fallback, int max)
            => requested <= 0 ? fallback : Math.Min(requested, max);

        private static bool InWorld(Client client) =>
            client?.Player != null && client.AccountEntry != null && client.State == ClientState.Ingame;

        private static bool SameMap(Client first, Client second) =>
            first.Player.MapChannel != null && ReferenceEquals(first.Player.MapChannel, second.Player.MapChannel);

        private static Client FindIngame(string familyName) =>
            string.IsNullOrEmpty(familyName)
                ? null
                : Server.Clients.Find(c => c.State == ClientState.Ingame && c.Player != null && c.AccountEntry != null
                                           && string.Equals(c.Player.FamilyName, familyName, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// One line of wargame text, on the wargame manager's own channel
        /// (client/wargame.py Recv_DisplayWargameMessage -> UI_DISPLAY_PLAYER_MESSAGE with the
        /// SYSTEM_GENERAL filter).
        /// </summary>
        private static void Message(Client client, PlayerMessage message, string key = null, string value = null)
        {
            if (!InWorld(client))
                return;

            var args = new Dictionary<string, string>();

            if (key != null)
                args[key] = value ?? string.Empty;

            client.CallMethod(SysEntity.ClientWargameManagerId, new DisplayWargameMessagePacket(message, args));
        }

        #endregion
    }
}
