using System;
using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Communicator.Server;
    using Packets.Trade.Client;
    using Packets.Trade.Server;
    using Structures;

    /// <summary>
    /// Player-to-player trade, client/trade.py.
    ///
    ///     Client -> server (ActorMethod)
    /// - RequestTrade                        => implemented
    /// - RequestAcceptTradeRequest           => implemented
    /// - RequestCancelTrade                  => implemented
    /// - RequestChangeEnergyUnitAmount       => implemented (credits)
    /// - RequestConfirmTrade                 => implemented
    /// - RequestUnconfirmTrade               => implemented
    /// - RequestAddItemToTrade               => refused: items not implemented yet
    /// - RequestRemoveItemFromTrade          => ignored: no item can be in a trade yet
    ///
    ///     Server -> client (ClientTradeManagerId)
    /// - TradeInvite, TradeCreate, TradeDestroy, TradeCompleted,
    ///   TradeEnergyUnitsChange, TradeConfirmChange => implemented
    /// - TradeAddItem, TradeRemoveItem, TradeUpdateItem => not sent until item trading exists
    ///
    /// All handlers and RemovePlayer run on the MainLoop, so the session table needs no lock.
    /// </summary>
    public class TradeManager
    {
        /// <summary>
        /// The client offers trade only inside distance-squared 36 (client/trade.py InTradingRange)
        /// and cancels itself when a partner leaves it. The server's copy of a position trails the
        /// client's by up to a movement update, so it allows a little more before refusing.
        /// </summary>
        private const float TradeRange = 6.0f;
        private const float ServerRangeSlack = 2.0f;

        /// <summary>
        /// The client has no way to decline an invite - the pending indicator only accepts - so an
        /// ignored one would leave the target "busy" until the inviter revoked it. Unaccepted
        /// invites older than this are treated as gone the next time anyone runs into them.
        /// </summary>
        private const long PendingInviteTimeoutMs = 60 * 1000;

        private static TradeManager _instance;
        private static readonly object InstanceLock = new object();

        /// <summary>Both participants of every pending or open trade map to their session.</summary>
        private readonly Dictionary<Client, TradeSession> _sessions = new Dictionary<Client, TradeSession>();

        public static TradeManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new TradeManager();
                    }
                }

                return _instance;
            }
        }

        private TradeManager()
        {
        }

        #region Handlers

        internal void RequestTrade(Client client, RequestTradePacket packet)
        {
            if (_sessions.TryGetValue(client, out var existing))
            {
                if (existing.Accepted)
                {
                    Refuse(client, PlayerMessage.PmTradeYouAreTooBusy);
                    return;
                }

                // An unaccepted invite in either direction is superseded by a new request.
                Withdraw(existing, client);
            }

            var target = FindIngamePlayer((ulong)packet.TargetEntityId);

            if (target == null || target == client || !InRange(client, target))
            {
                Refuse(client, PlayerMessage.PmTradeYouAreTooBusy);
                return;
            }

            if (_sessions.TryGetValue(target, out var targetSession) && !targetSession.Accepted && Expired(targetSession))
                End(targetSession, notifyPartnerOf: null, message: PlayerMessage.PmTradeCancelled);

            // Busy, or ignoring the requester. Both read as "too busy" so an ignore is not revealed.
            if (_sessions.ContainsKey(target) || target.Player.IgnoredPlayers.Contains(client.AccountEntry.Id))
            {
                Refuse(client, PlayerMessage.PmTradeTheyAreTooBusy);
                return;
            }

            var session = new TradeSession(client, target);
            _sessions[client] = session;
            _sessions[target] = session;

            target.CallMethod(SysEntity.ClientTradeManagerId, new TradeInvitePacket());
        }

        internal void RequestAcceptTradeRequest(Client client, RequestAcceptTradeRequestPacket packet)
        {
            if (!_sessions.TryGetValue(client, out var session) || session.Accepted || session.IsInitiator(client))
            {
                // The invite is gone - revoked, or the inviter left. TradeDestroy is what clears the
                // indicator the player just clicked.
                client.CallMethod(SysEntity.ClientTradeManagerId, new TradeDestroyPacket());
                return;
            }

            if (Expired(session) || !StillValid(session))
            {
                End(session, notifyPartnerOf: null, message: PlayerMessage.PmTradeCancelled);
                return;
            }

            session.Accepted = true;
            session.ConfigurationId = 1;

            session.Initiator.CallMethod(SysEntity.ClientTradeManagerId,
                new TradeCreatePacket(session.Target.Player.EntityId, session.ConfigurationId, true));
            session.Target.CallMethod(SysEntity.ClientTradeManagerId,
                new TradeCreatePacket(session.Initiator.Player.EntityId, session.ConfigurationId, false));
        }

        internal void RequestCancelTrade(Client client, RequestCancelTradePacket packet)
        {
            if (!_sessions.TryGetValue(client, out var session))
                return;

            End(session, notifyPartnerOf: client);
        }

        internal void RequestChangeEnergyUnitAmount(Client client, RequestChangeEnergyUnitAmountPacket packet)
        {
            if (!TryGetOpenSession(client, out var session))
                return;

            var funds = client.Player.Credits.TryGetValue(CurencyType.Credits, out var c) ? c : 0;
            var amount = (int)Math.Clamp(packet.Amount, 0, Math.Max(funds, 0));

            if (amount == session.CreditsOf(client))
                return;

            session.SetCredits(client, amount);
            TermsChanged(session, client);

            var change = new TradeEnergyUnitsChangePacket(client.Player.EntityId, amount, session.ConfigurationId);
            session.Initiator.CallMethod(SysEntity.ClientTradeManagerId, change);
            session.Target.CallMethod(SysEntity.ClientTradeManagerId, change);
        }

        internal void RequestConfirmTrade(Client client, RequestConfirmTradePacket packet)
        {
            if (!TryGetOpenSession(client, out var session))
                return;

            if (packet.ConfigurationId != session.ConfigurationId)
            {
                // Confirmed terms that have since changed. Tell the client it is not confirmed:
                // it hid its Accept button on click and only restores it on this message.
                client.CallMethod(SysEntity.ClientTradeManagerId, new TradeConfirmChangePacket(client.Player.EntityId, false));
                return;
            }

            SetConfirmed(session, client, true);

            if (session.InitiatorConfirmed && session.TargetConfirmed)
                Complete(session);
        }

        internal void RequestUnconfirmTrade(Client client, RequestUnconfirmTradePacket packet)
        {
            if (!TryGetOpenSession(client, out var session))
                return;

            SetConfirmed(session, client, false);
        }

        internal void RequestAddItemToTrade(Client client, RequestAddItemToTradePacket packet)
        {
            // Item trading is not implemented. The client only draws an item into the trade
            // window when TradeAddItem arrives, so refusing leaves nothing to undo.
            client.CallMethod(SysEntity.CommunicatorId,
                new DisplayClientMessagePacket(PlayerMessage.PmTradeItemCanNotBeTraded, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
        }

        internal void RequestRemoveItemFromTrade(Client client, RequestRemoveItemFromTradePacket packet)
        {
            // Nothing to remove: no item can be added yet. Handled so the call is not an
            // unhandled opcode, which disconnects the player.
        }

        #endregion

        /// <summary>
        /// Ends any trade the player is part of. Called from MapChannelManager.RemovePlayer, which
        /// covers /logout, the inactivity logout and dropped connections.
        /// </summary>
        public void RemovePlayer(Client client)
        {
            if (_sessions.TryGetValue(client, out var session))
                End(session, notifyPartnerOf: client);
        }

        private void Complete(TradeSession session)
        {
            var a = session.Initiator;
            var b = session.Target;

            // Everything is re-checked at the moment of exchange: either player may have moved,
            // spent credits elsewhere, or lost their connection since confirming.
            if (!StillValid(session)
                || session.InitiatorCredits > CreditsOnHand(a)
                || session.TargetCredits > CreditsOnHand(b))
            {
                End(session, notifyPartnerOf: null, message: PlayerMessage.PmTradeCancelled);
                return;
            }

            // Only the net amount moves. The debit is applied first: each update is its own
            // database write, so if the second one fails credits are lost rather than duplicated.
            var toB = session.InitiatorCredits - session.TargetCredits;
            var payer = toB >= 0 ? a : b;
            var payee = toB >= 0 ? b : a;
            var net = Math.Abs(toB);

            if (net > 0)
            {
                CharacterManager.Instance.UpdateCharacter(payer, CharacterUpdate.Credits, -net);
                CharacterManager.Instance.UpdateCharacter(payee, CharacterUpdate.Credits, net);
            }

            Logger.WriteLog(LogType.Security,
                $"Trade completed: {a.Player.FamilyName} gave {session.InitiatorCredits} credits, {b.Player.FamilyName} gave {session.TargetCredits} credits");

            Forget(session);

            foreach (var participant in new[] { a, b })
            {
                participant.CallMethod(SysEntity.ClientTradeManagerId, new TradeCompletedPacket());
                participant.CallMethod(SysEntity.CommunicatorId,
                    new DisplayClientMessagePacket(PlayerMessage.PmTradeCompleted, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
            }
        }

        /// <summary>
        /// Any change to the terms withdraws both confirmations, and tells a partner who had
        /// already confirmed why their accept just disappeared.
        /// </summary>
        private void TermsChanged(TradeSession session, Client changedBy)
        {
            session.ConfigurationId++;

            var partner = session.PartnerOf(changedBy);

            if (session.IsConfirmed(partner))
                partner.CallMethod(SysEntity.CommunicatorId,
                    new DisplayClientMessagePacket(PlayerMessage.PmTradeChangeAfterConfirm, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));

            SetConfirmed(session, session.Initiator, false);
            SetConfirmed(session, session.Target, false);
        }

        private static void SetConfirmed(TradeSession session, Client client, bool confirmed)
        {
            if (session.IsConfirmed(client) == confirmed)
                return;

            session.SetConfirmed(client, confirmed);

            var change = new TradeConfirmChangePacket(client.Player.EntityId, confirmed);
            session.Initiator.CallMethod(SysEntity.ClientTradeManagerId, change);
            session.Target.CallMethod(SysEntity.ClientTradeManagerId, change);
        }

        /// <summary>
        /// Removes the session and closes it on both clients. TradeDestroy also clears the
        /// inviter's "trade requested" indicator and the invitee's pending-request indicator.
        /// </summary>
        private void End(TradeSession session, Client notifyPartnerOf, PlayerMessage? message = null)
        {
            Forget(session);

            foreach (var participant in new[] { session.Initiator, session.Target })
            {
                if (participant.State == ClientState.Disconnected)
                    continue;

                participant.CallMethod(SysEntity.ClientTradeManagerId, new TradeDestroyPacket());

                var tell = message ?? (notifyPartnerOf != null && participant != notifyPartnerOf ? PlayerMessage.PmTradeCancelled : (PlayerMessage?)null);

                if (tell.HasValue)
                    participant.CallMethod(SysEntity.CommunicatorId,
                        new DisplayClientMessagePacket(tell.Value, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
            }
        }

        /// <summary>
        /// Drops an unaccepted invite because one of its players started another request. That
        /// player's client has already raised the "trade requested" indicator for the new
        /// request, and TradeDestroy clears it by name, so only the other side is told.
        /// </summary>
        private void Withdraw(TradeSession session, Client requester)
        {
            Forget(session);

            var other = session.PartnerOf(requester);

            if (other.State == ClientState.Disconnected)
                return;

            other.CallMethod(SysEntity.ClientTradeManagerId, new TradeDestroyPacket());

            // The inviter learns their invite lapsed; an invitee whose inviter moved on needs no message.
            if (!session.IsInitiator(requester))
                other.CallMethod(SysEntity.CommunicatorId,
                    new DisplayClientMessagePacket(PlayerMessage.PmTradeTheyAreTooBusy, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
        }

        private static bool Expired(TradeSession session)
        {
            return Environment.TickCount64 - session.CreatedTick > PendingInviteTimeoutMs;
        }

        private void Forget(TradeSession session)
        {
            if (_sessions.TryGetValue(session.Initiator, out var s1) && s1 == session)
                _sessions.Remove(session.Initiator);

            if (_sessions.TryGetValue(session.Target, out var s2) && s2 == session)
                _sessions.Remove(session.Target);
        }

        private bool TryGetOpenSession(Client client, out TradeSession session)
        {
            return _sessions.TryGetValue(client, out session) && session.Accepted;
        }

        private static void Refuse(Client client, PlayerMessage message)
        {
            // The requester's client already shows a "trade requested" indicator; TradeDestroy
            // removes it along with the message saying why.
            client.CallMethod(SysEntity.ClientTradeManagerId, new TradeDestroyPacket());
            client.CallMethod(SysEntity.CommunicatorId,
                new DisplayClientMessagePacket(message, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
        }

        private static bool StillValid(TradeSession session)
        {
            var a = session.Initiator;
            var b = session.Target;

            return a.State == ClientState.Ingame && b.State == ClientState.Ingame && InRange(a, b);
        }

        private static bool InRange(Client a, Client b)
        {
            if (a.Player.MapChannel == null || a.Player.MapChannel != b.Player.MapChannel)
                return false;

            return Vector3.Distance(a.Player.Position, b.Player.Position) <= TradeRange + ServerRangeSlack;
        }

        private static int CreditsOnHand(Client client)
        {
            return client.Player.Credits.TryGetValue(CurencyType.Credits, out var credits) ? credits : 0;
        }

        private static Client FindIngamePlayer(ulong entityId)
        {
            return Server.Clients.Find(c => c.State == ClientState.Ingame && c.Player.EntityId == entityId);
        }
    }
}
