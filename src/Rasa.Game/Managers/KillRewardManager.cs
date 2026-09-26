using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.ClientMethod.Server;
    using Packets.Communicator.Server;
    using Structures;

    /// <summary>Per-character kill streak; lives with the session only.</summary>
    public sealed class KillStreakState
    {
        public int Kills { get; set; }
        public int Value { get; set; }
        public long LastKillMs { get; set; }
        public bool MaxRewarded { get; set; }
    }

    /// <summary>
    /// Experience, credits and kill streak for a player's creature kill (docs/evidence/kill-rewards.json):
    /// the killer's streak, the squad share for squadmates in range, the danger penalty for each
    /// recipient's own level, and the crit-kill chunk. Partial credit for shared damage, a squad
    /// streak, team crit kills and squad credits are recorded gaps.
    /// </summary>
    public sealed class KillRewardManager
    {
        private static readonly Lazy<KillRewardManager> Singleton = new(() => new KillRewardManager(
            () => Environment.TickCount64,
            (client, reward) => ManifestationManager.Instance.GainKillExperience(client, reward),
            (client, update, value) => CharacterManager.Instance.UpdateCharacter(client, update, value)));
        public static KillRewardManager Instance => Singleton.Value;

        private readonly Func<long> _clock;
        private readonly Action<Client, KillExperience> _gainExperience;
        private readonly Action<Client, CharacterUpdate, object> _update;

        public KillRewardManager(Func<long> clock, Action<Client, KillExperience> gainExperience, Action<Client, CharacterUpdate, object> update)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _gainExperience = gainExperience ?? throw new ArgumentNullException(nameof(gainExperience));
            _update = update ?? throw new ArgumentNullException(nameof(update));
        }

        /// <summary>A kill by a player outside a squad.</summary>
        public void AwardKill(Client client, Creature creature, bool critKill = false)
            => AwardKill(client, creature, Array.Empty<Client>(), 1, critKill);

        /// <summary>
        /// A kill by <paramref name="client"/>, whose squad has <paramref name="squadSize"/> members in the
        /// world, of whom <paramref name="squadmates"/> (not the killer) were in range (<see cref="SquadShare"/>).
        /// <paramref name="critKill"/> is a finishing blow on an overkilled creature; nothing raises it yet,
        /// because the crit-death finisher (RequestCritDeathFinish) is not implemented (GAP-CRIT-DEATH-FINISH).
        /// </summary>
        public void AwardKill(Client client, Creature creature, IReadOnlyCollection<Client> squadmates, int squadSize, bool critKill = false)
        {
            var player = client?.Player;
            if (player == null || creature == null)
                return;

            var level = (int)creature.Level;
            var size = Math.Clamp(squadSize, 1, KillRewardRules.MaxSquadSize);

            // TaRapedia (Experience, 2008-10-06): a finishing blow on an overkill "counts as two kills in a
            // chain", paid as two chunks; B1-028 shows the "by Crit Killing" chunk first, each equal to the
            // plain kill, and one credit line after both.
            if (critKill)
                Pay(client, KillRewardRules.Experience(level, AdvanceStreak(client), size, player.Level).AsCritKill());
            Pay(client, KillRewardRules.Experience(level, AdvanceStreak(client), size, player.Level));

            // Squadmates in range share the kill without dealing damage (TaRapedia, "Experience In Squads").
            // Only the killer's streak is known to apply; theirs is left out (GAP-XP-SQUAD-STREAK).
            if (size > 1 && squadmates != null)
                foreach (var mate in squadmates)
                    if (mate?.Player != null && mate != client)
                        Pay(mate, KillRewardRules.Experience(level, 1, size, mate.Player.Level));

            // Paid at the kill, not from the corpse: the credit line arrives with the experience line
            // (A3-081, B1-018). Recv_GotLoot with no items prints "You received %(amount)s credits.".
            var credits = KillRewardRules.Credits(level);
            _update(client, CharacterUpdate.Credits, credits);
            client.CallMethod(SysEntity.ClientMethodId, new GotLootPacket(creature.EntityId, credits));
        }

        /// <summary>
        /// The squad size a kill is shared by and the killer's squadmates who share it: every member in the
        /// world counts toward the size, as in the client's experience-bar tooltip (len(GetSquadMembers()) + 1);
        /// members on this channel within MIN_DISTANCE_FOR_KILL_CREDIT of the creature receive a share.
        /// Whether absent members count toward the size is a gap (GAP-XP-SQUAD-RANGE).
        /// </summary>
        public static (int Size, List<Client> Squadmates) SquadShare(MapChannel mapChannel, Client killer, Creature creature, Party party)
        {
            var squadmates = new List<Client>();
            if (party == null || killer?.Player == null || creature == null)
                return (1, squadmates);

            var size = Math.Clamp(party.Members.Count(member => member.IsOnline), 1, KillRewardRules.MaxSquadSize);

            foreach (var client in mapChannel?.ClientList ?? new List<Client>())
            {
                var player = client?.Player;
                if (player == null || client == killer || client.State != ClientState.Ingame)
                    continue;
                if (!party.Members.Any(member => member.IsOnline && member.EntityId == player.EntityId))
                    continue;
                if (Vector3.Distance(player.Position, creature.Position) > KillRewardRules.MinDistanceForKillCredit)
                    continue;
                squadmates.Add(client);
            }

            return (size, squadmates);
        }

        private void Pay(Client client, KillExperience reward)
        {
            // Recv_ExperienceChanged prints nothing for gained 0; beyond the danger ramp there is nothing to send.
            if (reward.Gained > 0)
                _gainExperience(client, reward);
        }

        /// <summary>Counts one kill toward the killer's streak and returns the streakMod it pays at.</summary>
        private int AdvanceStreak(Client client)
        {
            var player = client.Player;
            var now = _clock();
            var streak = player.KillStreak;

            if (streak.Kills > 0 && now - streak.LastKillMs > KillRewardRules.StreakWindowMs)
                Reset(client, streak);

            streak.Kills++;
            streak.LastKillMs = now;

            var value = Math.Min(KillRewardRules.MaxStreak(player.Level), streak.Kills / KillRewardRules.StreakKillsPerStep);
            if (value > streak.Value)
            {
                streak.Value = value;
                client.CallMethod(SysEntity.ClientMethodId, new SetKillStreakPacket(value));

                // A4-10, B1-041: the prestige line comes before the doubled experience line.
                if (value == KillRewardRules.MaxStreak(player.Level) && !streak.MaxRewarded)
                {
                    streak.MaxRewarded = true;
                    client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(
                        PlayerMessage.PmPrestigePointsReceivedKillstreakmax,
                        new Dictionary<string, string> { ["amount"] = KillRewardRules.MaxStreakPrestige.ToString() },
                        MsgFilterId.PrestigeGainLose));
                    _update(client, CharacterUpdate.Prestige, KillRewardRules.MaxStreakPrestige);
                }
            }

            return 1 + streak.Value;
        }

        /// <summary>Ends streaks whose window has passed. Run once per second from the map worker.</summary>
        public void ExpireStreaks(MapChannel mapChannel)
        {
            if (mapChannel?.ClientList == null)
                return;

            var now = _clock();
            foreach (var client in mapChannel.ClientList)
            {
                var streak = client?.Player?.KillStreak;
                if (streak != null && streak.Kills > 0 && now - streak.LastKillMs > KillRewardRules.StreakWindowMs)
                    Reset(client, streak);
            }
        }

        private static void Reset(Client client, KillStreakState streak)
        {
            if (streak.Value > 0)
                client.CallMethod(SysEntity.ClientMethodId, new SetKillStreakPacket(0));

            streak.Kills = 0;
            streak.Value = 0;
            streak.MaxRewarded = false;
        }
    }
}
