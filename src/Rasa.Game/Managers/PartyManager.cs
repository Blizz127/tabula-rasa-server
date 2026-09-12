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
         * - SendJoinRequestToPartyByName', (targetName,))       => implemented
         * - InviteSquad', (targetName,))
         * - SendJoinRequestToSquadLeader', (targetName,))       => implemented
         * - CancelSquadInviteRequest', (targetName,))
         * - CancelSquadJoinRequest', (targetName,))
         * - PartyInvitationResponse', (accepted,))
         * - PartyJoinRequestResponse', (accepted, senderUserId)) => implemented
         * - LeaveParty', ())
         * - DisbandParty', ())
         * - KickUserFromParty', (name,))
         * - KickUserFromPartyById', (id,))
         * - MakeUserPartyLeader', (name,))                     => implemented
         * - MakeUserPartyLeaderById', (id,))                   => implemented
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

        /// <summary>
        /// Open join requests, keyed by the requester's account id. A leader can hold several, so
        /// PartyJoinRequestResponse carries the requester's account id; the requester holds one,
        /// because their revoke dialog is a single window.
        /// </summary>
        private readonly Dictionary<uint, PendingJoinRequest> _joinRequests = new Dictionary<uint, PendingJoinRequest>();

        private sealed class PendingJoinRequest
        {
            public uint RequesterId;
            public string RequesterName;
            public uint LeaderId;
            public string LeaderName;

            /// <summary>
            /// The name the requester's client is showing this request under - the player they
            /// asked, who may not be the leader it was routed to. Every message that clears their
            /// pending indicator has to use it.
            /// </summary>
            public string DisplayName;
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

            invitee.CallMethod(SysEntity.ClientPartyManagerId, new InviteToPartyPacket(client.Player.FamilyName, SquadInfo(client)));
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

        /// <summary>
        /// client/party.py SendJoinRequest: asking a player to be let into their squad. The whole
        /// of the requester's squad joins, so they have to be leading it, and the request goes to
        /// the leader of the squad they are asking to join.
        /// </summary>
        internal void SendJoinRequestToPartyByName(Client client, SendJoinRequestToPartyByNamePacket packet)
        {
            var target = FindJoinTarget(client, packet.FamilyName);

            if (target == null)
                return;

            var targetParty = PartyOf(target);

            // Asking a squad member rather than its leader: the client offers to send it on.
            if (targetParty != null && targetParty.PartyLeaderId != target.AccountEntry.Id)
            {
                client.CallMethod(SysEntity.ClientPartyManagerId, new RequestToJoinLeaderConfirmationRequestPacket(target.Player.FamilyName));
                return;
            }

            CreateJoinRequest(client, target, target.Player.FamilyName);
        }

        /// <summary>
        /// The answer to that offer: the same name again, now to be routed to the leader of that
        /// player's squad.
        /// </summary>
        internal void SendJoinRequestToSquadLeader(Client client, SendJoinRequestToSquadLeaderPacket packet)
        {
            var target = FindJoinTarget(client, packet.FamilyName);

            if (target == null)
                return;

            var targetParty = PartyOf(target);
            var leader = targetParty == null ? target : FindIngame(targetParty.PartyLeaderId);

            if (leader == null || leader == client)
            {
                Message(client, PlayerMessage.PmPartyRequestIsNoLongerValid);
                return;
            }

            // The requester's window is showing the player they asked, so that is the name every
            // message about this request has to carry back.
            CreateJoinRequest(client, leader, target.Player.FamilyName);
        }

        internal void CancelSquadJoinRequest(Client client, CancelSquadJoinRequestPacket packet)
        {
            if (client.AccountEntry == null || !_joinRequests.TryGetValue(client.AccountEntry.Id, out var request))
                return;

            if (!string.Equals(request.DisplayName, packet.FamilyName?.Trim(), StringComparison.OrdinalIgnoreCase))
                return;

            _joinRequests.Remove(request.RequesterId);

            // Clears the leader's merge window and its pending indicator.
            FindIngame(request.LeaderId)?.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestCanceledPacket(request.RequesterName));
        }

        /// <summary>The leader answering a join request; the client sends the requester's account id with it.</summary>
        internal void PartyJoinRequestResponse(Client client, PartyJoinRequestResponsePacket packet)
        {
            if (!InWorld(client))
                return;

            if (!_joinRequests.TryGetValue(packet.SenderUserId, out var request) || request.LeaderId != client.AccountEntry.Id)
            {
                Message(client, PlayerMessage.PmPartyRequestIsNoLongerValid);
                return;
            }

            _joinRequests.Remove(request.RequesterId);

            var requester = FindIngame(request.RequesterId);

            if (requester == null)
                return;

            // Closes the requester's revoke dialog and pending indicator, and says nothing itself.
            requester.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestSuccessPacket(request.DisplayName));

            if (!packet.Accepted)
            {
                requester.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestDeclinedPacket(client.Player.FamilyName));
                return;
            }

            var requesterParty = PartyOf(requester);
            var leaderParty = PartyOf(client);

            // Everything is checked again: squads change while a request sits on screen.
            if (requesterParty != null && requesterParty.PartyLeaderId != requester.AccountEntry.Id
                || leaderParty != null && leaderParty.PartyLeaderId != client.AccountEntry.Id
                || requesterParty != null && requesterParty == leaderParty)
            {
                Message(client, PlayerMessage.PmPartyRequestIsNoLongerValid);
                Message(requester, PlayerMessage.PmPartyRequestIsNoLongerValid);
                return;
            }

            if (JoiningSize(requesterParty) + (leaderParty?.Members.Count ?? 1) > MaxPartySize)
            {
                Message(client, PlayerMessage.PmPartyIsFull);
                Message(requester, PlayerMessage.PmPartyIsFull);
                return;
            }

            Message(client, PlayerMessage.PmPartyInvitationAccepted, "invitee", requester.Player.FamilyName);

            if (leaderParty == null)
            {
                if (requesterParty == null)
                {
                    CreateParty(client, requester);
                    return;
                }

                leaderParty = CreateParty(client);
            }

            if (requesterParty == null)
                AddMember(leaderParty, requester);
            else
                MergeInto(leaderParty, requesterParty);
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

        /// <summary>
        /// client/party.py SendChangeLeader: the leader naming the member to hand the squad to.
        /// Typed names reach this one, so a name that is not in the squad is answered.
        /// </summary>
        internal void MakeUserPartyLeader(Client client, MakeUserPartyLeaderPacket packet)
        {
            var party = LedParty(client);

            if (party == null)
                return;

            var name = packet.FamilyName?.Trim() ?? string.Empty;
            var target = party.Members.Find(m => string.Equals(m.MemberName, name, StringComparison.OrdinalIgnoreCase));

            if (target == null)
            {
                Message(client, PlayerMessage.PmWhisperTargetNotInGame, "player", name);
                return;
            }

            HandLeadership(party, client, target);
        }

        /// <summary>
        /// client/party.py SendChangeLeaderById, from the party window. The id comes out of that
        /// window, so one that is not in the squad means the window is behind: it is resent
        /// rather than answered with a message about a player the leader never named.
        /// </summary>
        internal void MakeUserPartyLeaderById(Client client, MakeUserPartyLeaderByIdPacket packet)
        {
            var party = LedParty(client);

            if (party == null)
                return;

            var target = party.Find(packet.UserId);

            if (target == null)
            {
                SendPartyState(party, client);
                return;
            }

            HandLeadership(party, client, target);
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

        /// <summary>
        /// Shared checks for both join-request entry points. Returns the player being asked, or
        /// null after telling the requester why not.
        /// </summary>
        private Client FindJoinTarget(Client client, string familyName)
        {
            if (!InWorld(client))
                return null;

            var name = familyName?.Trim() ?? string.Empty;
            var party = PartyOf(client);

            // The whole of the requester's squad joins, so a member cannot ask on its behalf.
            if (party != null && party.PartyLeaderId != client.AccountEntry.Id)
            {
                Message(client, PlayerMessage.PmYouAreNotPartyLeader);
                return null;
            }

            var target = FindIngame(name);

            if (target == null)
            {
                Message(client, PlayerMessage.PmWhisperTargetNotInGame, "player", name);
                return null;
            }

            if (target == client)
                return null;

            if (party?.Find(target.AccountEntry.Id) != null)
            {
                Message(client, PlayerMessage.PmTheyAreAlreadyInYourParty, "invitee", target.Player.FamilyName);
                return null;
            }

            return target;
        }

        private void CreateJoinRequest(Client requester, Client leader, string displayName)
        {
            var requesterParty = PartyOf(requester);
            var leaderParty = PartyOf(leader);

            if (JoiningSize(requesterParty) + (leaderParty?.Members.Count ?? 1) > MaxPartySize)
            {
                Message(requester, PlayerMessage.PmPartyIsFull);
                return;
            }

            // One request at a time: the requester has a single revoke dialog. A new one replaces
            // the old, which means telling whoever was asked that it is gone.
            if (_joinRequests.TryGetValue(requester.AccountEntry.Id, out var previous))
            {
                _joinRequests.Remove(previous.RequesterId);
                FindIngame(previous.LeaderId)?.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestCanceledPacket(previous.RequesterName));
            }

            _joinRequests[requester.AccountEntry.Id] = new PendingJoinRequest
            {
                RequesterId = requester.AccountEntry.Id,
                RequesterName = requester.Player.FamilyName,
                LeaderId = leader.AccountEntry.Id,
                LeaderName = leader.Player.FamilyName,
                DisplayName = displayName
            };

            leader.CallMethod(SysEntity.ClientPartyManagerId,
                new JoinSquadRequestReceivedPacket(requester.Player.FamilyName, SquadInfo(requester), requester.AccountEntry.Id));
            requester.CallMethod(SysEntity.ClientPartyManagerId, new JoinSquadRequestSentPacket(displayName, leader.Player.IsAFK));
        }

        /// <summary>How many seats a join request needs: the requester alone, or their whole squad.</summary>
        private static int JoiningSize(Party requesterParty) => requesterParty?.Members.Count ?? 1;

        /// <summary>
        /// Moves every member of one squad into another and drops the empty one. Members who are
        /// out of the world keep their held spot in the squad they end up in.
        /// </summary>
        private void MergeInto(Party party, Party source)
        {
            var moving = source.Members.ToList();
            // The members already there: the arrivals hear about each other from the member list
            // that follows, not from one AddPartyMember per arrival.
            var existing = OnlineClients(party);

            source.Members.Clear();
            Parties.Remove(source.Id);
            FreePartyId(source.Id);

            foreach (var member in moving)
                AddMemberEntry(party, member, existing);

            // State goes out after every member is in the list, so the arrivals see each other.
            foreach (var member in moving)
            {
                var client = member.IsOnline ? FindIngame(member.UserId) : null;

                if (client == null)
                    continue;

                client.Player.PartyId = party.Id;
                SendPartyState(party, client);
                Message(client, PlayerMessage.PmYouJoinedTheParty);
            }

            AdsChanged(party);
        }

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

            AdsChanged(party);
        }

        /// <summary>A squad of one, for a leader who is about to be joined by another squad.</summary>
        private Party CreateParty(Client leader)
        {
            var party = new Party(GetPartyId, leader.AccountEntry.Id, new List<PartyMember> { new PartyMember(leader) });

            Parties[party.Id] = party;
            leader.Player.PartyId = party.Id;
            SendPartyState(party, leader);

            return party;
        }

        private void AddMember(Party party, Client client)
        {
            AddMemberEntry(party, new PartyMember(client), OnlineClients(party));

            client.Player.PartyId = party.Id;

            SendPartyState(party, client);
            Message(client, PlayerMessage.PmYouJoinedTheParty);

            AdsChanged(party);
        }

        /// <summary>Adds one member to a squad and tells the members it already had.</summary>
        private static void AddMemberEntry(Party party, PartyMember member, List<Client> tell)
        {
            foreach (var other in tell)
            {
                other.CallMethod(SysEntity.ClientPartyManagerId, new AddPartyMemberPacket(member));

                if (member.IsOnline)
                    other.CallMethod(SysEntity.ClientPartyManagerId, new AddSquadMemberPacket(member.UserId, member.EntityId));
            }

            party.Members.Add(member);
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
                AdsChanged(null, member.UserId);
                return;
            }

            if (party.PartyLeaderId == member.UserId)
                PassLeadership(party);

            // The leaver too: their own ad now recruits for a squad they are not in.
            AdsChanged(party, member.UserId);
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

            var former = party.Members.Select(m => m.UserId).ToArray();

            party.Members.Clear();
            Parties.Remove(party.Id);
            FreePartyId(party.Id);

            AdsChanged(null, former);
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

        /// <summary>
        /// The leader handing the squad to another member. A member whose spot is only being
        /// held is refused: leading is done from the world, so a squad led by someone who is not
        /// in it has nobody who can invite, kick, set loot or hand it on again, and the only way
        /// out is for everyone to leave.
        /// </summary>
        private void HandLeadership(Party party, Client leader, PartyMember target)
        {
            if (target.UserId == leader.AccountEntry.Id)
                return;

            if (!target.IsOnline)
            {
                Message(leader, PlayerMessage.PmPartyMemberLoggedOut, "player", target.MemberName);
                return;
            }

            SetLeader(party, target);
        }

        private void SetLeader(Party party, PartyMember leader)
        {
            if (party.PartyLeaderId == leader.UserId)
                return;

            var previousLeaderId = party.PartyLeaderId;

            party.PartyLeaderId = leader.UserId;

            // The client reads leadership off this one call: SetPartyLeader with an id that is
            // not in that client's member list - which never contains itself - is how it learns
            // that it is the leader now. Everyone gets it, so both sides of the handover update.
            foreach (var member in OnlineClients(party))
                member.CallMethod(SysEntity.ClientPartyManagerId, new SetPartyLeaderPacket(leader.UserId));

            MoveJoinRequests(previousLeaderId, leader);
            AdsChanged(party, previousLeaderId);
        }

        /// <summary>
        /// Join requests are answered by whoever leads the squad, so a handover carries the open
        /// ones across: the old leader's merge window is closed and the new leader's opens, with
        /// the requester left looking at the name they asked for. If the new leader is not in the
        /// world the request is dropped instead and the requester's dialog closed.
        /// </summary>
        private void MoveJoinRequests(uint previousLeaderId, PartyMember leader)
        {
            var pending = _joinRequests.Values.Where(r => r.LeaderId == previousLeaderId).ToList();

            if (pending.Count == 0)
                return;

            var previousLeader = FindIngame(previousLeaderId);
            var newLeader = leader.IsOnline ? FindIngame(leader.UserId) : null;

            foreach (var request in pending)
            {
                previousLeader?.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestCanceledPacket(request.RequesterName));

                var requester = FindIngame(request.RequesterId);

                if (newLeader == null || requester == null)
                {
                    _joinRequests.Remove(request.RequesterId);

                    if (requester != null)
                    {
                        requester.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestSuccessPacket(request.DisplayName));
                        Message(requester, PlayerMessage.PmPartyRequestIsNoLongerValid);
                    }

                    continue;
                }

                request.LeaderId = newLeader.AccountEntry.Id;
                request.LeaderName = newLeader.Player.FamilyName;

                newLeader.CallMethod(SysEntity.ClientPartyManagerId,
                    new JoinSquadRequestReceivedPacket(request.RequesterName, SquadInfo(requester), request.RequesterId));
            }
        }

        /// <summary>SetCurrentPartyId(None) and SetPartyLeader(None): the client's "not in a squad" state.</summary>
        private static void ResetClient(Client client, bool kicked)
        {
            client.CallMethod(SysEntity.ClientPartyManagerId, new SetCurrentPartyIdPacket(0, kicked));
            client.CallMethod(SysEntity.ClientPartyManagerId, new SetPartyLeaderPacket(0));
        }

        private void DropInvites(uint accountId)
        {
            if (_joinRequests.TryGetValue(accountId, out var sentRequest))
            {
                _joinRequests.Remove(accountId);
                FindIngame(sentRequest.LeaderId)?.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestCanceledPacket(sentRequest.RequesterName));
            }

            foreach (var asked in _joinRequests.Values.Where(r => r.LeaderId == accountId).ToList())
            {
                _joinRequests.Remove(asked.RequesterId);

                var requester = FindIngame(asked.RequesterId);

                if (requester != null)
                {
                    requester.CallMethod(SysEntity.ClientPartyManagerId, new SquadRequestSuccessPacket(asked.DisplayName));
                    Message(requester, PlayerMessage.PmInviteeLoggedOut, "name", asked.LeaderName);
                }
            }

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

        /// <summary>
        /// The squad tuples the client's invite and join-request windows list for a player: their
        /// whole squad, or the player on their own.
        /// </summary>
        private List<PartyMember> SquadInfo(Client client)
        {
            var party = PartyOf(client);

            return party != null ? LiveMembers(party) : new List<PartyMember> { new PartyMember(client) };
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

        /// <summary>
        /// A looking-for-group ad recruits for the squad its placer leads, so every change to a
        /// squad's membership or leadership is handed to LookingForGroupManager: it drops the ad
        /// of anyone who has stopped leading, and one whose squad has reached the size it asked
        /// for. Accounts that have no ad cost a dictionary miss, so it is cheaper to notify
        /// everyone involved than to work out who might be affected here.
        /// </summary>
        private static void AdsChanged(Party party, params uint[] alsoAccounts)
        {
            var lfg = LookingForGroupManager.Instance;

            if (party != null)
                foreach (var member in party.Members.ToList())
                    lfg.PartyChanged(member.UserId);

            foreach (var account in alsoAccounts)
                lfg.PartyChanged(account);
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
