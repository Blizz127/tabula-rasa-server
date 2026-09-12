using System;
using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Communicator.Both;
    using Packets.Communicator.Client;
    using Packets.ClientMethod.Server;
    using Packets.Protocol;
    using Packets.Communicator.Server;
    using Packets.MapChannel.Server;
    using Structures;

    public class CommunicatorManager
    {
        /*      Communicator Packets:
         *      -- WorldMsg
         * - Who                                => implemented
         * - ChangeClanName
         * - ChallengeClanToFeud
         * - FeudChallengeResponse
         * - RevokeClanFeud
         * - SurrenderClanFeud
         * - SurrenderWargame
         *      -- UserMethod
         * - PrivilegedCommand
         * - Whisper                            => implemented
         * - PartyChat
         * - GuildChat
         * - Shout
         * - RadialChat
         * - ChannelChat
         * - Reply                              => implemented
         * - ClanLeadersChat
         * - ChangeLastName
         * - ChangeFirstName
         * - Emote                              => implemented
         *      -- ActorMethod
         * - RequestLOSReport
         * - ToggleAfk                          => implemented (ManifestationManager)
         * - GotoMob
         * 
         *      Comunicator Handlers:
         * - AddFriendAck
         * - AddIgnoreAck
         * - AdminMessage
         * - ChatChannelJoined
         * - ChatChannelLeft
         * - DisplayClientMessage
         * - FriendList
         * - IgnoreList
         * - LoginOk
         * - PlayerCountAck                     => implemented (Who result count)
         * - PlayerLogin
         * - PlayerLogout
         * - PreviewMOTD
         * - RemoveFriendAck
         * - RemoveIgnoreAck
         * - SendMOTD
         * - SystemMessage
         * - WhisperAck                         => implemented
         * - WhisperFailAck                     => implemented
         * - WhisperSelf                        => implemented
         * - WhoAck                             => implemented
         * - WhoFailAck                         => implemented
         * 
         *      Client and server packets:
         * - RadialChat
         * - ChannelChat
         * - ClanChat
         * - ClanLeadersChat
         * - Emote                              => implemented
         * - PartyChat
         * - Radial
         * - Shout                              => implemented
         * - Whisper                            => implemented
         */

        private static CommunicatorManager _instance;
        private static readonly object InstanceLock = new object();
        public static Dictionary<int, ChatChannel> ChannelsBySeed = new Dictionary<int, ChatChannel>();

        public static CommunicatorManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new CommunicatorManager();
                    }
                }

                return _instance;
            }
        }

        private CommunicatorManager()
        {
        }

        #region Handlers

        internal void ClanChat(Client client, ClanChatPacket packet)
        {
            var clanMembers = Server.Clients.FindAll(c => c.Player.ClanId == packet.ClanId);
            
            foreach(var member in clanMembers)
                member.CallMethod(SysEntity.CommunicatorId, new ClanChatPacket(client.Player.FamilyName, packet.Message));
        }

        internal void ChannelChat(Client client, ChannelChatPacket packet)
        {
            client.CallMethod(SysEntity.CommunicatorId, new ChannelChatPacket(client.Player.FamilyName, packet.ChannelId, packet.MapEntityId, packet.MapContextId, packet.Message));
        }

        internal void Emote(Client client, EmotePacket packet)
        {
            if (client.Player == null)
                return;

            // The client files this under RADIAL_EMOTE, so it is local chat like RadialChat
            // rather than something wider. Recv_Emote renders link(sender) + " " + msg, and
            // sender is the same string the rest of the chat system uses - FamilyName, which
            // is what Whisper and Reply resolve a target by.
            var mapChannel = client.Player.MapChannel;

            for (var i = 0; i < mapChannel.ClientList.Count; i++)
            {
                var tempClient = mapChannel.ClientList[i];

                if (tempClient.Player == null)
                    continue;

                if (Vector3.Distance(client.Player.Position, tempClient.Player.Position) <= RadialRange)
                    tempClient.CallMethod(SysEntity.CommunicatorId, new EmotePacket(client.Player.FamilyName, packet.Emote));
            }
        }

        internal void Reply(Client client, ReplyPacket packet)
        {
            // /r carries the name from the last Recv_Whisper, which is the family name this
            // server sent as the sender, so it resolves exactly like /w.
            DeliverWhisper(client, packet.Reciver, packet.Message);
        }

        internal void PartyChat(Client client, PartyChatPacket packet)
        {
            var party = PartyManager.Instance.PartyOf(client);

            if (party == null)
            {
                client.CallMethod(SysEntity.CommunicatorId,
                    new DisplayClientMessagePacket(PlayerMessage.PmActionFailedNoParty, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
                return;
            }

            // Recv_PartyChat(sender, msg, senderUserId, senderEntityId): senderUserId is compared
            // with the party leader's userId and GetCurrentUserId() to pick the leader colour, and
            // senderEntityId places the chat bubble. Members whose spot is held are skipped.
            foreach (var partyMember in party.Members)
            {
                if (!partyMember.IsOnline)
                    continue;

                var tempClient = Server.Clients.Find(c =>
                    c.State == ClientState.Ingame && c.AccountEntry != null && c.AccountEntry.Id == partyMember.UserId);

                tempClient?.CallMethod(SysEntity.CommunicatorId, new PartyChatPacket
                {
                    Sender = client.Player.FamilyName,
                    Message = packet.Message,
                    SenderUserId = client.AccountEntry.Id,
                    SenderEntityId = client.Player.EntityId
                });
            }
        }

        internal void Whisper(Client client, WhisperPacket packet)
        {
            DeliverWhisper(client, packet.Reciver, packet.Message);
        }

        /// <summary>
        /// Shared by Whisper and Reply. The sender used to be sent a Whisper from themselves,
        /// which the client prints as an incoming message, bubbles over the sender's own head,
        /// and pushes onto g_replyTo - so /r afterwards targeted yourself. An unknown or offline
        /// target threw on the null receiver and disconnected the sender. WhisperAck,
        /// WhisperFailAck and WhisperSelf were never sent at all.
        /// </summary>
        private void DeliverWhisper(Client sender, string targetName, string message)
        {
            var name = targetName?.Trim() ?? string.Empty;

            if (name.Length > 0 && string.Equals(name, sender.Player.FamilyName, StringComparison.OrdinalIgnoreCase))
            {
                sender.CallMethod(SysEntity.CommunicatorId, new WhisperSelfPacket(message));
                return;
            }

            // Family names are unique; typed names should not have to match their case.
            var target = name.Length == 0
                ? null
                : Server.Clients.Find(c =>
                    c.State == ClientState.Ingame &&
                    string.Equals(c.Player.FamilyName, name, StringComparison.OrdinalIgnoreCase));

            if (target == null)
            {
                sender.CallMethod(SysEntity.CommunicatorId,
                    new WhisperFailAckPacket(name, PlayerMessage.PmWhisperTargetNotInGame));
                return;
            }

            // Ignore lists hold account ids, loaded by SocialManager.SetSocialContactList.
            if (target.Player.IgnoredPlayers.Contains(sender.AccountEntry.Id))
            {
                sender.CallMethod(SysEntity.CommunicatorId,
                    new WhisperFailAckPacket(target.Player.FamilyName, PlayerMessage.PmWhisperTargetIgnoringYou));
                return;
            }

            target.CallMethod(SysEntity.CommunicatorId, new WhisperPacket
            {
                Sender = sender.Player.FamilyName,
                Message = message,
                SenderEntityId = sender.Player.EntityId
            });

            // The target's canonical family name, not what was typed, so the sender sees the
            // name the way the target's family is actually spelled.
            sender.CallMethod(SysEntity.CommunicatorId,
                new WhisperAckPacket(target.Player.FamilyName, message, target.Player.IsAFK));
        }

        /// <summary>
        /// Most results the server will report for one /who. The client prints a line per
        /// result into the chat window, so an unbounded list on a busy server would flood it.
        /// </summary>
        private const int WhoResultLimit = 50;

        internal void Who(Client client, WhoPacket packet)
        {
            var search = (packet.SearchText ?? string.Empty).Trim();

            var matches = new List<Client>();

            foreach (var candidate in Server.Clients)
            {
                if (candidate.State != ClientState.Ingame || candidate.Player == null)
                    continue;

                // No argument lists everyone online; otherwise match either name, since
                // /who, /lookup and /whois all send free text rather than a specific field.
                if (search.Length > 0
                    && (candidate.Player.Name == null
                        || candidate.Player.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                    && (candidate.Player.FamilyName == null
                        || candidate.Player.FamilyName.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0))
                    continue;

                matches.Add(candidate);

                if (matches.Count >= WhoResultLimit)
                    break;
            }

            if (matches.Count == 0)
            {
                // Recv_WhoFailAck fills only {'player': charName}. PmNoSuchUser's text uses
                // %(name)s, so it printed a translation-substitution error instead.
                // PmWhisperTargetNotInGame is "%(player)s could not be found."
                client.CallMethod(SysEntity.CommunicatorId,
                    new WhoFailAckPacket(search, (uint)PlayerMessage.PmWhisperTargetNotInGame));

                return;
            }

            foreach (var match in matches)
                client.CallMethod(SysEntity.CommunicatorId, new WhoAckPacket
                {
                    CharacterName = match.Player.Name,
                    FamilyName = match.Player.FamilyName,
                    ClanName = match.Player.ClanName,
                    // The client omits the title when this is None, and titledata has no
                    // entry for 0, which is what an untitled character carries.
                    TitleId = match.Player.CurrentTitle == 0 ? null : match.Player.CurrentTitle,
                    CharacterClass = match.Player.Class,
                    Level = match.Player.Level,
                    ContextId = match.Player.MapContextId,
                    // One instance per map today, so there is no ordinal to disambiguate;
                    // sending a value would make the client render "MapName(1)".
                    CurrentGameContextOrdinal = null,
                    IsAfk = match.Player.IsAFK,
                    IsTrialAccount = match.Player.IsTrialAccount
                });

            client.CallMethod(SysEntity.CommunicatorId,
                new PlayerCountAckPacket((uint)PlayerMessage.PmWhoCount, (uint)matches.Count));
        }

        #endregion

        public void AddClientToChannel(Client client, int cHash)
        {
            if (ChannelsBySeed.TryGetValue(cHash, out ChatChannel chatChannel))
            {
                var newLink = new ChatChannelPlayerLink
                {
                    Next = null,
                    EntityId = client.Player.EntityId,
                    Previous = null
                };

                if (chatChannel.FirstPlayer != null)
                {
                    // append
                    var currentLink = new ChatChannelPlayerLink();

                    while (currentLink.Next != null)
                        currentLink = currentLink.Next;

                    newLink.Previous = currentLink;
                    currentLink.Next = newLink;
                }
                else
                {
                    // set as first
                    chatChannel.FirstPlayer = newLink;
                }
            }
        }

        internal void AddFriendAck(Client client, string familyName, bool succsess)
        {
            client.CallMethod(SysEntity.CommunicatorId, new AddFriendAckPacket(familyName, succsess));
        }

        internal void AddIgnoreAck(Client client, string familyName, bool succsess)
        {
            client.CallMethod(SysEntity.CommunicatorId, new AddIgnoreAckPacket(familyName, succsess));
        }

        /// <summary>PM_REMOVED_FROM_FRIEND_LIST or PM_FAILED_FRIEND_REMOVE, by success.</summary>
        internal void RemoveFriendAck(Client client, string familyName, bool success)
        {
            client.CallMethod(SysEntity.CommunicatorId, new RemoveFriendAckPacket(familyName, success));
        }

        /// <summary>PM_REMOVED_FROM_IGNORE_LIST or PM_FAILED_IGNORE_REMOVE, by success.</summary>
        internal void RemoveIgnoreAck(Client client, string familyName, bool success)
        {
            client.CallMethod(SysEntity.CommunicatorId, new RemoveIgnoreAckPacket(familyName, success));
        }

        public int GenerateDefaultChannelHash(int channelId, int mapContextId, int instanceId)
        {
            var v = 0;
            v = (channelId ^ (channelId << 7)) ^ mapContextId ^ (mapContextId * 121) ^ ((instanceId + instanceId * 13) << 3);
            return v;
        }

        public void JoinDefaultLocalChannel(Client client, uint channelId)
        {
            if (client.Player.JoinedChannels >= 14)
                return; // todo, send error to client
            // generate channel hash
            var cHash = GenerateDefaultChannelHash((int)channelId, (int)client.Player.MapChannel.MapInfo.MapContextId, 0);
            // find channel
            ChatChannel chatChannel;
            if (ChannelsBySeed.TryGetValue(cHash, out chatChannel))
            {
            }
            else
            {
                // channel does not exist, create it
                chatChannel = new ChatChannel();
                chatChannel.Name[0] = '\0';
                chatChannel.InstanceId = 0;
                chatChannel.ChannelId = channelId;
                chatChannel.MapContextId = client.Player.MapChannel.MapInfo.MapContextId;
                chatChannel.IsDefaultChannel = true;
                chatChannel.FirstPlayer = null;
                // register it
                ChannelsBySeed.Add(cHash, chatChannel);
            }
            // add channel entry to player
            client.Player.ChannelHashes[client.Player.JoinedChannels] = cHash;
            client.Player.JoinedChannels++;
            // add client to channel
            AddClientToChannel(client, cHash);
            client.CallMethod(SysEntity.CommunicatorId, new ChatChannelJoinedPacket(channelId, client.Player.MapChannel.MapInfo.MapContextId, client.Player.EntityId));
        }

        public void LoginOk(Client client)
        {
            // send LoginOk (despite the original description in the python files, this will only show 'You have arrived at ....' msg in chat)
            client.CallMethod(SysEntity.CommunicatorId, new LoginOkPacket(client.Player.Name));
            // send MOTD ( Recv_SendMOTD - receives MOTDDict {languageId: text} )
            // SendMOTD = 770		// Displayed only if different
            // PreviewMOTD = 769	// Displayed always
            client.CallMethod(SysEntity.CommunicatorId, new PreviewMOTDPacket("Welcome to the Infinite Rasa server."));
        }

        public void PlayerEnterMap(Client client)
        {
            JoinDefaultLocalChannel(client, 1); // join general

            SocialManager.Instance.FriendLoggedIn(client);
        }

        public void PlayerExitMap(Client client)
        {            
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Position, null);
            // save player time
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Login, null);
            // remove client from all channels
            for (var i = 0; i < client.Player.JoinedChannels; i++)
            {
                var chatChannel = ChannelsBySeed[client.Player.ChannelHashes[i]];
                if (chatChannel != null)
                {
                    // remove client link from channel
                    var currentLink = chatChannel.FirstPlayer;
                    while (currentLink != null)
                    {
                        if (currentLink.EntityId == client.Player.EntityId)
                        {
                            // do removing
                            if (currentLink.Previous == null)
                            {
                                chatChannel.FirstPlayer = currentLink.Next;
                                if (currentLink.Next != null)
                                    currentLink.Next.Previous = null;
                            }
                            else
                            {
                                currentLink.Previous.Next = currentLink.Next;
                                if (currentLink.Next != null)
                                    currentLink.Next.Previous = currentLink.Previous;
                            }
                            break;
                        }
                        // next
                        currentLink = currentLink.Next;
                    }
                }
            }

            client.Player.JoinedChannels = 0;

            SocialManager.Instance.FriendLoggedOut(client);
        }

        public void RadialChat(Client client, string textMsg)
        {
            // check if it's gm command
            if (textMsg[0] == '.')
            {
                // it's GM command, check if client is GM
                if (client.AccountEntry.Level > 0)
                {
                    // Client is GM
                    ChatCommandsManager.Instance.ProcessCommand(client, textMsg);
                    return;
                }
                else
                    Logger.WriteLog(LogType.Security, $"AccountId = {client.AccountEntry.Id} tryed to use GM Command = {textMsg}");
            } 
            if (client.Player == null)
                return;
            // go through all players and send chat message ( can ignore sync because playerList will not change )
            var mapChannel = client.Player.MapChannel;
            for (var i = 0; i < mapChannel.ClientList.Count; i++)
            {
                var tempClient = mapChannel.ClientList[i];
                if (tempClient.Player != null)
                {
                    var distance = Vector3.Distance(client.Player.Position, tempClient.Player.Position);
                    if (distance <= RadialRange)
                        tempClient.CallMethod(SysEntity.CommunicatorId, new RadialChatPacket
                        {
                            FamilyName = client.Player.FamilyName,
                            TextMsg = textMsg,
                            EntityId = client.Player.EntityId
                        }
                    );
                }
            }
        }

        /// <summary>
        /// How far a shout carries, in world units. RadialChat uses 70, "about the range the
        /// client is visible". Shout is deliberately wider: the client renders it in the chat
        /// window only, with no overhead bubble and no sender entity id, so the shouter does
        /// not need to be visible to the listener. The client ships no chat-range table, so
        /// this figure is ours to pick - raise it toward map size if a shout should reach the
        /// whole zone.
        /// </summary>
        private const float ShoutRange = 150.0f;

        /// <summary>
        /// Range of the local chat types (RadialChat, Emote). "70 is about the range the
        /// client is visible", per the original comment on RadialChat.
        /// </summary>
        private const float RadialRange = 70.0f;

        public void Shout(Client client, string textMsg)
        {
            if (client.Player == null)
                return;

            // Same iteration style as RadialChat: the map channel's client list does not
            // change while we are on the main loop, so no extra synchronisation is needed.
            var mapChannel = client.Player.MapChannel;

            for (var i = 0; i < mapChannel.ClientList.Count; i++)
            {
                var tempClient = mapChannel.ClientList[i];

                if (tempClient.Player == null)
                    continue;

                if (Vector3.Distance(client.Player.Position, tempClient.Player.Position) <= ShoutRange)
                    tempClient.CallMethod(SysEntity.CommunicatorId, new ShoutPacket
                    {
                        FamilyName = client.Player.FamilyName,
                        TextMsg = textMsg
                    });
            }
        }

        public void SystemMessage(Client client, string textMsg)
        {
            client.CallMethod(SysEntity.CommunicatorId, new SystemMessagePacket(textMsg));
        }

        #region Error dialogs

        /// <summary>
        /// A modal error the player can dismiss and carry on from. Use it only when the message
        /// has to be acknowledged - while the dialog is up the rest of the client's UI is
        /// suppressed - and the chat window for everything else.
        /// </summary>
        public void NonFatalError(Client client, PlayerMessage message, Dictionary<string, string> args = null)
        {
            client.CallMethod(SysEntity.ClientMethodId, new NonFatalErrorPacket(message, args));
        }

        /// <summary>
        /// The last thing a player is told. The client's OK button on this dialog calls
        /// PostQuitRequest, so it ends the session; send it only when the connection is going away
        /// regardless, to replace a silent drop with a reason.
        ///
        /// Sent straight down the socket rather than through the packet queue. The queue is drained
        /// by the MainLoop and Client.Close() closes the socket where it stands, so a queued fatal
        /// error would be thrown away by the very disconnect it is explaining. Callers should send
        /// this and then close.
        /// </summary>
        public void FatalError(Client client, PlayerMessage message, Dictionary<string, string> args = null)
        {
            if (client == null || client.State == ClientState.Disconnected)
                return;

            try
            {
                client.SendMessage(new CallMethodMessage((ulong)SysEntity.ClientMethodId,
                    new FatalErrorPacket(message, args)), false, 0, false);
            }
            catch (Exception e)
            {
                // The socket is already going; the disconnect it was explaining still happens.
                Logger.WriteLog(LogType.Network, $"Could not deliver a fatal error to a closing connection: {e.Message}");
            }
        }

        #endregion
    }
}
