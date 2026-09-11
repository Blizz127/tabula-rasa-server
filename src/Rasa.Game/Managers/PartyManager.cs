using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Communicator.Server;
    using Packets.Party.Both;
    using Packets.Party.Client;
    using Packets.Party.Server;
    using Structures;

    /// <summary>
    /// Squads. Members are identified by account id (the client's userId) and stay in the
    /// party while they are out of the world: a member who logs out, drops or changes map is
    /// greyed out for the others and rejoins automatically when they next enter the world,
    /// unless HeldSpotMs passes first. Everything here runs on the main loop.
    /// </summary>
    public class PartyManager
    {
        /*   Party Packets:
         * - InviteUserToPartyByName', (targetName,))
         * - SendJoinRequestToPartyByName', (targetName,))
         * - InviteSquad', (targetName,))
         * - SendJoinRequestToSquadLeader', (targetName,))
         * - CancelSquadInviteRequest', (targetName,))
         * - CancelSquadJoinRequest', (targetName,))
         * - PartyInvitationResponse', (accepted,))
         * - PartyJoinRequestResponse', (accepted, senderUserId))
         * - LeaveParty', ())
         * - DisbandParty', ())
         * - KickUserFromParty', (name,))
         * - KickUserFromPartyById', (id,))
         * - MakeUserPartyLeader', (name,))
         * - MakeUserPartyLeaderById', (id,))
         * - ChangePartyLootMethod', (option,))
         * - ChangePartyLootThreshold', (quality,))
         * - AcceptPartyInvitesChanged', (value.lower() == 'true',))
         *  
         *   Party Handlers:
         * - SetCurrentPartyId(squadId, wasKicked = False)
         * - SquadMemberList(squadMembers, partyExclusiveMap)
         * - AddSquadMember(userId, entityId)
         * - RemoveSquadMember(userId, entityId)
         * - AddPartyMember(userId, name, classId, level, isAfk)
         * - RemovePartyMember(userId, wasKicked = False)
         * - PartyMemberList(partyList)
         * - UpdatePartyMemberInfo(userId, name, classId, level, isAfk)
         * - SetPartyLeader(userId)
         * - ChangePartyLootMethod(newOption)
         * - ChangePartyLootThreshold(newOption)
         * - InviteToParty(senderName, senderSquadInfo)
         * - JoinSquadRequestReceived(senderName, senderSquadInfo, senderUserId)
         * - InvitedPlayerToParty(inviteeName, isTargetAfk)
         * - JoinSquadRequestSent(inviteeName, isTargetAfk)
         * - SquadRequestCanceled(inviterName)
         * - SquadRequestSuccess(inviteeName)
         * - InviteSquadConfirmationRequest(inviteeName)
         * - RequestToJoinLeaderConfirmationRequest(inviteeName)
         * - SquadRequestDeclined(receiverName)
         * - PartyDisbanded()
         * - DisplayPartyMessage(msgId, args = { })
         * - PartyMemberRoll(itemClassId, winnerUserId, rolls, isGreedRoll)
         * - PartyMemberLoot(userId, creatureEntityId, lootClassIds, moneyAmount)
         * - VoiceChatAvailable(isAvail)
         * - PartyMemberVoiceId(userId, voiceId)
         * - PartyMemberVoiceIds(memberList)
         * - VoiceChatConnectInfo(serverAddr, groupId, playerId, token)
         */

        #region Singleton

        private static PartyManager _instance;
        private static readonly object InstanceLock = new object();
        private uint _partyId = 1;
        private object _partyIdLock = new object();
        private List<uint> _freePartyIds = new List<uint>();
        public static PartyManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new PartyManager();
                    }
                }

                return _instance;
            }
        }

        private PartyManager()
        {
        }

        #endregion

        /// <summary>shared/gameconstants.py MAX_PARTY_SIZE.</summary>
        public const int MaxPartySize = 6;

        /// <summary>How long a member's spot is kept after they leave the world.</summary>
        public const long HeldSpotMs = 5 * 60 * 1000;

        public uint GetPartyId
        {
            get
            {
                lock (_partyIdLock)
                {
                    if (_freePartyIds.Count > 0)
                    {
                        var freePartyId = _freePartyIds[0];

                        _freePartyIds.RemoveAt(0);

                        return freePartyId;
                    }

                    return _partyId++;
                }
            }
        }

        public void FreePartyId(uint id)
        {
            lock (_partyIdLock)
                if (!_freePartyIds.Contains(id))
                    _freePartyIds.Add(id);
        }

        internal Dictionary<uint, Party> Parties = new Dictionary<uint, Party>();

        /// <summary>
        /// Open invitations, keyed by invitee account id. PartyInvitationResponse carries only
        /// (accepted,) - no sender - so an invitee can hold one invitation at a time, and
        /// PM_CAN_ONLY_INVITE_ONE_PERSON_AT_A_TIME limits an inviter to one as well.
        /// </summary>
        private readonly Dictionary<uint, PendingInvite> _invites = new Dictionary<uint, PendingInvite>();

        private sealed class PendingInvite
        {
            public uint InviterId;
            public string InviterName;
            public uint InviteeId;
            public string InviteeName;
        }

        #region Handlers

        internal void InviteUserToPartyByName(Client client, InviteUserToPartyByNamePacket packet)
        {
            if (!InWorld(client))
                return;

            var name = packet.FamilyName?.Trim() ?? string.Empty;
            var party = PartyOf(client);

            if (party != null && party.PartyLeaderId != client.AccountEntry.Id)
            {
                Message(client, PlayerMessage.PmYouAreNotPartyLeader);
                return;
            }

            var invitee = FindIngame(name);

            if (invitee == null)
            {
                Message(client, PlayerMessage.PmWhisperTargetNotInGame, "player", name);
                return;
            }

            var inviteeName = invitee.Player.FamilyName;

            if (invitee == client)
                return;

            if (party?.Find(invitee.AccountEntry.Id) != null)
            {
                Message(client, PlayerMessage.PmTheyAreAlreadyInYourParty, "invitee", inviteeName);
                return;
            }

            if (PartyOf(invitee) != null)
            {
                Message(client, PlayerMessage.PmTheyAreAlreadyInAParty, "invitee", inviteeName);
                return;
            }

            if (!invitee.Player.AcceptPartyInvites)
            {
                Message(client, PlayerMessage.PmPartyNotAcceptingInvite);
                return;
            }

            if (_invites.ContainsKey(invitee.AccountEntry.Id))
            {
                // Includes a repeat invitation from this inviter: every InviteToParty adds
                // another pending indicator on the invitee's screen.
                Message(client, PlayerMessage.PmUserAlreadyInvited, "name", inviteeName);
                return;
            }

            if (_invites.Values.Any(i => i.InviterId == client.AccountEntry.Id))
            {
                Message(client, PlayerMessage.PmCanOnlyInviteOnePersonAtATime);
                return;
            }

            if (party != null && party.Members.Count >= MaxPartySize)
            {
                Message(client, PlayerMessage.PmPartyIsFull);
                return;
            }

            _invites[invitee.AccountEntry.Id] = new PendingInvite
            {
                InviterId = client.AccountEntry.Id,
                InviterName = client.Player.FamilyName,
                InviteeId = invitee.AccountEntry.Id,
                InviteeName = inviteeName
            };

            var squadInfo = party != null ? LiveMembers(party) : new List<PartyMember> { new PartyMember(client) };

            invitee.CallMethod(SysEntity.ClientPartyManagerId, new InviteToPartyPacket(client.Player.FamilyName, squadInfo));
            client.CallMethod(SysEntity.ClientPartyManagerId, new InvitedPlayerToPartyPacket(inviteeName, invitee.Player.IsAFK));
        }

        internal void CancelSquadInviteRequest(Client client, CancelSquadInviteRequestPacket packet)
        {
            if (client.AccountEntry == null)
                return;

            var invite = _invites.Values.FirstOrDefault(i =>
                i.InviterId == client.AccountEntry.Id && string.Equals(i.InviteeName, packet.FamilyName?.Trim(), StringComparison.OrdinalIgnoreCase));

            if (invite == null)
                return;

            _invites.Remove(invite.InviteeId);

            FindIngame(invite.InviteeId)?.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestCanceledPacket(invite.InviterName));
        }

        internal void CancelSquadJoinRequest(Client client, CancelSquadJoinRequestPacket packet)
        {
            Logger.WriteLog(LogType.AI, $"CancelSquadJoinRequest ToDo");
        }

        internal void PartyInvitationResponse(Client client, PartyInvitationResponsePacket packet)
        {
            if (!InWorld(client))
                return;

            if (!_invites.Remove(client.AccountEntry.Id, out var invite))
            {
                if (packet.Response)
                    Message(client, PlayerMessage.PmPartyInvitationHasBeenRevoked);

                return;
            }

            var inviter = FindIngame(invite.InviterId);

            if (inviter == null)
            {
                if (packet.Response)
                    Message(client, PlayerMessage.PmPartyInvitationHasBeenRevoked);

                return;
            }

            if (!packet.Response)
            {
                inviter.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestDeclinedPacket(client.Player.FamilyName));
                return;
            }

            // The world may have moved on since the invitation was sent.
            var party = PartyOf(inviter);

            if (PartyOf(client) != null || (party != null && party.PartyLeaderId != inviter.AccountEntry.Id))
            {
                Message(client, PlayerMessage.PmPartyInvitationHasBeenRevoked);
                inviter.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestSuccessPacket(client.Player.FamilyName));
                return;
            }

            if (party != null && party.Members.Count >= MaxPartySize)
            {
                Message(client, PlayerMessage.PmPartyIsFull);
                Message(inviter, PlayerMessage.PmPartyIsFull);
                inviter.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestSuccessPacket(client.Player.FamilyName));
                return;
            }

            inviter.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestSuccessPacket(client.Player.FamilyName));
            Message(inviter, PlayerMessage.PmPartyInvitationAccepted, "invitee", client.Player.FamilyName);

            if (party == null)
                CreateParty(inviter, client);
            else
                AddMember(party, client);
        }

        internal void InviteSquad(Client client, InviteSquadPacket packet)
        {
        }

        internal void AcceptPartyInvitesChanged(Client client, AcceptPartyInvitesChangedPacket packet)
        {
            if (client.Player != null)
                client.Player.AcceptPartyInvites = packet.Accept;
        }

        internal void LeaveParty(Client client)
        {
            if (!InWorld(client))
                return;

            var party = PartyOf(client);

            if (party == null)
            {
                // Nothing to leave here, but the client thinks otherwise: bring it back in line.
                ResetClient(client, false);
                return;
            }

            RemoveMember(party, party.Find(client.AccountEntry.Id), false);
        }

        internal void DisbandParty(Client client)
        {
            if (!InWorld(client))
                return;

            var party = PartyOf(client);

            if (party == null)
            {
                ResetClient(client, false);
                return;
            }

            if (party.PartyLeaderId != client.AccountEntry.Id)
            {
                Message(client, PlayerMessage.PmYouAreNotPartyLeader);
                return;
            }

            Disband(party);
        }

        internal void KickUserFromParty(Client client, KickUserFromPartyPacket packet)
        {
            var party = LedParty(client);

            var target = party?.Members.Find(m => string.Equals(m.MemberName, packet.FamilyName, StringComparison.OrdinalIgnoreCase));

            if (target == null)
            {
                if (party != null)
                    Message(client, PlayerMessage.PmWhisperTargetNotInGame, "player", packet.FamilyName);

                return;
            }

            Kick(party, client, target);
        }

        internal void KickUserFromPartyById(Client client, KickUserFromPartyByIdPacket packet)
        {
            var party = LedParty(client);
            var target = party?.Find(packet.UserId);

            if (target != null)
                Kick(party, client, target);
        }

        internal void MakeUserPartyLeader(Client client, MakeUserPartyLeaderPacket packet)
        {
            var party = LedParty(client);
            var target = party?.Members.Find(m => string.Equals(m.MemberName, packet.FamilyName, StringComparison.OrdinalIgnoreCase));

            if (target != null)
                SetLeader(party, target);
        }

        internal void MakeUserPartyLeaderById(Client client, MakeUserPartyLeaderByIdPacket packet)
        {
            var party = LedParty(client);
            var target = party?.Find(packet.UserId);

            if (target != null)
                SetLeader(party, target);
        }

        internal void ChangePartyLootMethod(Client client, ChangePartyLootMethodPacket packet)
        {
            var party = LedParty(client);

            if (party == null)
                return;

            party.LootMethod = packet.PartyLootMethod;

            foreach (var member in OnlineClients(party))
                member.CallMethod(SysEntity.ClientPartyManagerId, new ChangePartyLootMethodPacket(packet.PartyLootMethod));
        }

        internal void ChangePartyLootThreshold(Client client, ChangePartyLootThresholdPacket packet)
        {
            var party = LedParty(client);

            if (party == null)
                return;

            party.LootThreshold = packet.PartyLootThreshold;

            foreach (var member in OnlineClients(party))
                member.CallMethod(SysEntity.ClientPartyManagerId, new ChangePartyLootThresholdPacket(packet.PartyLootThreshold));
        }

        #endregion

        #region World entry and exit

        /// <summary>
        /// MapChannelManager.RemovePlayer: logout, inactivity logout, dropped connection, and
        /// the GM teleport between maps. The member's spot is held rather than given up.
        /// </summary>
        public void RemovePlayer(Client client)
        {
            if (client.AccountEntry == null)
                return;

            DropInvites(client.AccountEntry.Id);

            var party = FindPartyOfAccount(client.AccountEntry.Id);

            if (client.Player != null)
                client.Player.PartyId = 0;

            var member = party?.Find(client.AccountEntry.Id);

            if (member == null || !member.IsOnline)
                return;

            var entityId = member.EntityId;

            member.EntityId = 0;
            member.OfflineSinceTick = Environment.TickCount64;

            foreach (var other in OnlineClients(party))
            {
                other.CallMethod(SysEntity.ClientPartyManagerId, new RemoveSquadMemberPacket(member.UserId, entityId));
                Message(other, PlayerMessage.PmPartyMemberLoggedOut, "player", member.MemberName);
            }

            // A squad whose leader is away cannot invite; leadership moves to someone present.
            if (party.PartyLeaderId == member.UserId)
                PassLeadership(party);
        }

        /// <summary>
        /// MapChannelManager.MapLoaded, when a character enters the world (not on a dropship
        /// teleport, which keeps the same manifestation). Rejoins a held spot, or clears any
        /// party the client still remembers from before it left the world.
        /// </summary>
        public void PlayerEnteredWorld(Client client)
        {
            var party = FindPartyOfAccount(client.AccountEntry.Id);

            if (party == null)
            {
                ResetClient(client, false);
                return;
            }

            var member = party.Find(client.AccountEntry.Id);

            member.Refresh(client);
            member.OfflineSinceTick = 0;
            client.Player.PartyId = party.Id;

            foreach (var other in OnlineClients(party))
            {
                if (other == client)
                    continue;

                other.CallMethod(SysEntity.ClientPartyManagerId, new UpdatePartyMemberInfoPacket(member));
                other.CallMethod(SysEntity.ClientPartyManagerId, new AddSquadMemberPacket(member.UserId, member.EntityId));
                Message(other, PlayerMessage.PmPartyMemberLoggedIn, "player", member.MemberName);
            }

            SendPartyState(party, client);

            if (party.Find(party.PartyLeaderId)?.IsOnline != true)
                PassLeadership(party);
        }

        /// <summary>Called every MapChannelWorker tick. Gives up spots held longer than HeldSpotMs.</summary>
        public void ExpireHeldMembers()
        {
            if (Parties.Count == 0)
                return;

            var now = Environment.TickCount64;

            foreach (var party in Parties.Values.ToList())
                foreach (var member in party.Members.Where(m => !m.IsOnline && now - m.OfflineSinceTick >= HeldSpotMs).ToList())
                    if (Parties.ContainsKey(party.Id))
                        RemoveMember(party, member, false);
        }

        #endregion

        #region Helper Functions

        private void CreateParty(Client leader, Client member)
        {
            var party = new Party(GetPartyId, leader.AccountEntry.Id, new List<PartyMember>
            {
                new PartyMember(leader),
                new PartyMember(member)
            });

            Parties[party.Id] = party;
            leader.Player.PartyId = party.Id;
            member.Player.PartyId = party.Id;

            SendPartyState(party, leader);
            SendPartyState(party, member);
            Message(member, PlayerMessage.PmYouJoinedTheParty);
        }

        private void AddMember(Party party, Client client)
        {
            var member = new PartyMember(client);

            foreach (var other in OnlineClients(party))
            {
                other.CallMethod(SysEntity.ClientPartyManagerId, new AddPartyMemberPacket(member));
                other.CallMethod(SysEntity.ClientPartyManagerId, new AddSquadMemberPacket(member.UserId, member.EntityId));
            }

            party.Members.Add(member);
            client.Player.PartyId = party.Id;

            SendPartyState(party, client);
            Message(client, PlayerMessage.PmYouJoinedTheParty);
        }

        /// <summary>
        /// Everything a client needs to show the party it is in. Recv_SetCurrentPartyId raises
        /// if the client already has a party id, and a client that went back to character
        /// select still has the one it left with, so it is cleared first; with no party id on
        /// the client that clear does nothing visible.
        /// </summary>
        private void SendPartyState(Party party, Client client)
        {
            var others = LiveMembers(party).Where(m => m.UserId != client.AccountEntry.Id).ToList();

            client.CallMethod(SysEntity.ClientPartyManagerId, new SetCurrentPartyIdPacket(0));
            client.CallMethod(SysEntity.ClientPartyManagerId, new SetCurrentPartyIdPacket(party.Id));
            // The recipient is not in their own list: g_partyMembers holds everyone else
            // (party.py GetFullPartyMembersCopy adds the current player itself). Sending the
            // recipient too put them in their own party window.
            client.CallMethod(SysEntity.ClientPartyManagerId, new PartyMemberListPacket(others));
            // After the list: SetPartyLeader resolves the id against it.
            client.CallMethod(SysEntity.ClientPartyManagerId, new SetPartyLeaderPacket(party.PartyLeaderId));
            client.CallMethod(SysEntity.ClientPartyManagerId, new SquadMemberListPacket(
                others.Where(m => m.IsOnline).Select(m => (m.UserId, m.EntityId)).ToList()));
        }

        private void RemoveMember(Party party, PartyMember member, bool kicked)
        {
            if (member == null)
                return;

            party.Members.Remove(member);

            if (member.IsOnline)
            {
                var leaver = FindIngame(member.UserId);

                if (leaver != null)
                {
                    leaver.Player.PartyId = 0;
                    ResetClient(leaver, kicked);
                }
            }

            foreach (var other in OnlineClients(party))
                other.CallMethod(SysEntity.ClientPartyManagerId, new RemovePartyMemberPacket(member.UserId, kicked));

            if (party.Members.Count < 2)
            {
                Disband(party);
                return;
            }

            if (party.PartyLeaderId == member.UserId)
                PassLeadership(party);
        }

        private void Kick(Party party, Client leader, PartyMember target)
        {
            if (target.UserId == leader.AccountEntry.Id)
                return;

            RemoveMember(party, target, true);
        }

        private void Disband(Party party)
        {
            foreach (var member in OnlineClients(party))
            {
                member.Player.PartyId = 0;
                member.CallMethod(SysEntity.ClientPartyManagerId, new PartyDisbandedPacket());
            }

            party.Members.Clear();
            Parties.Remove(party.Id);
            FreePartyId(party.Id);
        }

        /// <summary>
        /// Hands leadership to the first member in the world, in join order. With nobody in
        /// the world the current leader keeps it.
        /// </summary>
        private void PassLeadership(Party party)
        {
            var next = party.Members.FirstOrDefault(m => m.IsOnline && m.UserId != party.PartyLeaderId)
                       ?? party.Members.FirstOrDefault(m => m.IsOnline);

            if (next == null && party.Find(party.PartyLeaderId) == null)
                next = party.Members.FirstOrDefault();

            if (next != null && next.UserId != party.PartyLeaderId)
                SetLeader(party, next);
        }

        private void SetLeader(Party party, PartyMember leader)
        {
            if (party.PartyLeaderId == leader.UserId)
                return;

            party.PartyLeaderId = leader.UserId;

            foreach (var member in OnlineClients(party))
                member.CallMethod(SysEntity.ClientPartyManagerId, new SetPartyLeaderPacket(leader.UserId));
        }

        /// <summary>SetCurrentPartyId(None) and SetPartyLeader(None): the client's "not in a squad" state.</summary>
        private static void ResetClient(Client client, bool kicked)
        {
            client.CallMethod(SysEntity.ClientPartyManagerId, new SetCurrentPartyIdPacket(0, kicked));
            client.CallMethod(SysEntity.ClientPartyManagerId, new SetPartyLeaderPacket(0));
        }

        private void DropInvites(uint accountId)
        {
            if (_invites.Remove(accountId, out var received))
            {
                var inviter = FindIngame(received.InviterId);

                if (inviter != null)
                {
                    // Success is the only message that closes the inviter's revoke dialog as
                    // well as the pending indicator, and it prints nothing of its own.
                    inviter.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestSuccessPacket(received.InviteeName));
                    Message(inviter, PlayerMessage.PmInviteeLoggedOut, "name", received.InviteeName);
                }
            }

            foreach (var sent in _invites.Values.Where(i => i.InviterId == accountId).ToList())
            {
                _invites.Remove(sent.InviteeId);
                FindIngame(sent.InviteeId)?.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestCanceledPacket(sent.InviterName));
            }
        }

        /// <summary>The members with name, level and AFK read from the live character where there is one.</summary>
        private static List<PartyMember> LiveMembers(Party party)
        {
            foreach (var member in party.Members.Where(m => m.IsOnline))
            {
                var client = FindIngame(member.UserId);

                if (client != null)
                    member.Refresh(client);
            }

            return party.Members.ToList();
        }

        private static List<Client> OnlineClients(Party party)
        {
            var clients = new List<Client>();

            foreach (var member in party.Members)
            {
                if (!member.IsOnline)
                    continue;

                var client = FindIngame(member.UserId);

                if (client != null)
                    clients.Add(client);
            }

            return clients;
        }

        internal Party PartyOf(Client client)
        {
            if (client?.Player == null || client.Player.PartyId == 0)
                return null;

            return Parties.TryGetValue(client.Player.PartyId, out var party) && party.Find(client.AccountEntry.Id) != null
                ? party
                : null;
        }

        private Party FindPartyOfAccount(uint accountId) => Parties.Values.FirstOrDefault(p => p.Find(accountId) != null);

        /// <summary>The caller's party if they lead it; otherwise tells them why not and returns null.</summary>
        private Party LedParty(Client client)
        {
            if (!InWorld(client))
                return null;

            var party = PartyOf(client);

            if (party == null)
            {
                Message(client, PlayerMessage.PmActionFailedNoParty);
                return null;
            }

            if (party.PartyLeaderId != client.AccountEntry.Id)
            {
                Message(client, PlayerMessage.PmYouAreNotPartyLeader);
                return null;
            }

            return party;
        }

        private static bool InWorld(Client client) =>
            client?.Player != null && client.AccountEntry != null && client.State == ClientState.Ingame;

        private static Client FindIngame(uint accountId) =>
            Server.Clients.Find(c => c.State == ClientState.Ingame && c.Player != null && c.AccountEntry != null && c.AccountEntry.Id == accountId);

        private static Client FindIngame(string familyName) =>
            string.IsNullOrEmpty(familyName)
                ? null
                : Server.Clients.Find(c => c.State == ClientState.Ingame && c.Player != null && c.AccountEntry != null
                                           && string.Equals(c.Player.FamilyName, familyName, StringComparison.OrdinalIgnoreCase));

        private static void Message(Client client, PlayerMessage message, string key = null, string value = null)
        {
            var args = new Dictionary<string, string>();

            if (key != null)
                args[key] = value ?? string.Empty;

            client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(message, args, MsgFilterId.GeneralSystemMessages));
        }

        #endregion
    }
}
