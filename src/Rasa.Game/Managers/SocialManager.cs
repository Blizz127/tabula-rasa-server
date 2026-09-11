using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Social.Client;
    using Packets.Social.Server;
    using Rasa.Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;
    using System.Net.Sockets;

    public class SocialManager
    {
        /*      Social Packets:
         * - AddFriend
         * - AddFriendByName                  => implemented
         * - FriendList
         * - FriendLoggedOff
         * - InviteFriendToJoin
         * - InvitedToAddAndJoinFriend
         * - InvitedToJoinFriend
         * - JoinFriendCancelled
         * - JoinFriendDeclined 
         * - RemoveFriend
         * - RemoveFriendByName
         * - RespondToAddAndJoinFriend
         * - RespondToJoinFriend
         * 
         *      Social Handlers:
         * - SetSocialContactList(friendList, ignoreList)
         * - IgnoreAdded(args)
         * - IgnoreRemoved(userId)
         * - FriendAdded(args)
         * - FriendRemoved(userId)
         * - FriendStatusUpdate(args)
         * - FriendLoggedOut(userId)
         * - FriendLoggedIn(args)
         */

        #region Singleton

        private static SocialManager _instance;
        private static readonly object InstanceLock = new object();
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;
        public static SocialManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new SocialManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        private SocialManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
        }

        #endregion

        internal void AddFriendByName(Client client, AddFriendByNamePacket packet)
        {
            var requestedName = packet.FamilyName?.Trim() ?? string.Empty;

            GameAccountEntry account = null;

            if (requestedName.Length > 0)
            {
                using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
                account = unitOfWork.GameAccounts.FindByFamilyName(requestedName);
            }

            // Unknown name, or the player's own family. Compared by id: family names are
            // matched case-insensitively now, so a name comparison could miss "self".
            if (account == null || account.Id == client.AccountEntry.Id)
            {
                CommunicatorManager.Instance.AddFriendAck(client, requestedName, false);
                return;
            }

            if (client.Player.Friends.Contains(account.Id))
            {
                CommunicatorManager.Instance.AddFriendAck(client, account.FamilyName, false);
                return;
            }

            if (!AddFriend(client, account.Id))
                return;

            // Befriending lifts an ignore; the two lists are kept mutually exclusive.
            RemoveIgnoredPlayer(client, account.Id);
        }

        internal void AddIgnore(Client client, AddIgnorePacket packet)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var account = unitOfWork.GameAccounts.Find(packet.AccountId);

            IgnoreById(client, account, string.Empty);
        }

        internal void AddIgnoreByName(Client client, AddIgnoreByNamePacket packet)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var account = unitOfWork.GameAccounts.Get(packet.FamilyName);

            IgnoreById(client, account, packet.FamilyName);
        }
        
        internal void RemoveFriend(Client client, RemoveFriendPacket packet)
        {
            RemoveFriend(client, packet.AccountId);
        }

        internal void RemoveIgnore(Client client, RemoveIgnorePacket packet)
        {
            RemoveIgnoredPlayer(client, packet.AccountId);
        }

        internal void SetSocialContactList(Client client)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var friendIds = unitOfWork.Friends.GetFriends(client.AccountEntry.Id);
            var ignoredIds = unitOfWork.Ignoreds.GetIgnored(client.AccountEntry.Id);
            var frinedList = new List<Friend>();
            var ignoreList = new List<IgnoredPlayer>();


            foreach (var id in friendIds)
            {
                var friend = GetFriendById(id);

                if (friend != null)
                {
                    frinedList.Add(friend);
                    client.Player.Friends.Add(id);
                }
            }

            foreach (var id in ignoredIds)
            {
                var ignored = GetIgnoredById(id);

                if (ignored != null)
                {
                    ignoreList.Add(ignored);
                    client.Player.IgnoredPlayers.Add(id);
                }
            }

            client.CallMethod(SysEntity.ClientSocialManagerId, new SetSocialContactListPacket(frinedList, ignoreList));
        }

        #region Helper Functions

        /// <returns>false, with the failure already acknowledged to the client, when nothing was added.</returns>
        internal bool AddFriend(Client client, uint accountId)
        {
            var friend = GetFriendById(accountId);

            if (friend == null)
            {
                CommunicatorManager.Instance.AddFriendAck(client, string.Empty, false);
                return false;
            }

            // Persist before telling the client. FriendAdded puts the row in the client's
            // friend window immediately, so announcing a friend the database then refused
            // would show one that silently vanishes at the next login.
            using (var unitOfWork = _gameUnitOfWorkFactory.CreateChar())
            {
                if (!unitOfWork.Friends.AddFriend(client.AccountEntry.Id, accountId))
                {
                    CommunicatorManager.Instance.AddFriendAck(client, friend.FamilyName, false);
                    return false;
                }
            }

            client.Player.Friends.Add(accountId);

            // No AddFriendAck on success: Recv_FriendAdded already posts PM_ADDED_TO_FRIEND_LIST
            // (client/social.py:196), so acking as well would print the message twice.
            client.CallMethod(SysEntity.ClientSocialManagerId, new FriendAddedPacket(friend));

            return true;
        }
        
        internal void AddIgnoredPlayer(Client client, uint accountId)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var ignored = GetIgnoredById(accountId);

            client.CallMethod(SysEntity.ClientSocialManagerId, new IgnoreAddedPacket(ignored));
            client.Player.IgnoredPlayers.Add(accountId);
            unitOfWork.Ignoreds.AddIgnored(client.AccountEntry.Id, accountId);
        }

        internal void FriendLoggedIn(Client client)
        {
            var notifyFriends = Server.Clients.FindAll(c => c.Player.Friends.Contains(client.AccountEntry.Id));

            foreach (var notifyClient in notifyFriends)
            {
                var friend = new Friend(client);

                notifyClient.CallMethod(SysEntity.ClientSocialManagerId, new FriendLoggedInPacket(friend));
            }
        }

        internal void FriendLoggedOut(Client client)
        {
            var notifyFriends = Server.Clients.FindAll(c => c.Player.Friends.Contains(client.AccountEntry.Id));

            foreach (var notifyClient in notifyFriends)
            {
                notifyClient.CallMethod(SysEntity.ClientSocialManagerId, new FriendLoggedOutPacket(client.AccountEntry.Id));
            }
        }

        internal void FriendStatusUpdate(Client client, Friend friend)
        {
            // ToDo
        }
        
        /// <summary>
        /// The friend-list row for an account, live when they are in game and from the
        /// database otherwise. Null when the account no longer exists; SetSocialContactList
        /// already skips nulls, so a stale row cannot break login.
        /// </summary>
        internal Friend GetFriendById(uint accountId)
        {
            var online = FindIngameClient(accountId);

            if (online != null)
                return new Friend(online);

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var account = unitOfWork.GameAccounts.Find(accountId);

            return account == null ? null : new Friend(account);
        }

        /// <summary>Same contract as GetFriendById, for the ignore list.</summary>
        internal IgnoredPlayer GetIgnoredById(uint accountId)
        {
            var online = FindIngameClient(accountId);

            if (online != null)
                return new IgnoredPlayer(online);

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var account = unitOfWork.GameAccounts.Find(accountId);

            return account == null ? null : new IgnoredPlayer(account);
        }

        /// <summary>
        /// Server.Clients holds every accepted connection, and AccountEntry is only assigned
        /// once the login message is processed - so the null check has to come before .Id,
        /// or a single half-connected client throws for everyone searching the list.
        /// </summary>
        private static Client FindIngameClient(uint accountId)
        {
            return Server.Clients.Find(c =>
                c.State == ClientState.Ingame && c.AccountEntry != null && c.AccountEntry.Id == accountId);
        }
        
        internal void IgnoreById(Client client, GameAccountEntry account, string requestedName)
        {
            // The ack used to read account.FamilyName, which is exactly the null this branch
            // exists to catch.
            if (account == null || account.Id == client.AccountEntry.Id)
            {
                CommunicatorManager.Instance.AddIgnoreAck(client, account?.FamilyName ?? requestedName, false);
                return;
            }
            
            foreach (var ignoredId in client.Player.IgnoredPlayers)
                if (ignoredId == account.Id)
                {
                    CommunicatorManager.Instance.AddIgnoreAck(client, account.FamilyName, false);
                    return;
                }

            AddIgnoredPlayer(client, account.Id);

            RemoveFriend(client, account.Id);
        }

        internal void RemoveFriend(Client client, uint accountId)
        {
            var friend = client.Player.Friends.Contains(accountId);

            if (friend)
            {
                client.CallMethod(SysEntity.ClientSocialManagerId, new FriendRemovedPacket(accountId));

                client.Player.Friends.RemoveAll(remove => remove == accountId);

                using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
                unitOfWork.Friends.RemoveFriend(client.AccountEntry.Id, accountId);
            }
        }

        internal void RemoveIgnoredPlayer(Client client, uint accountId)
        {
            var ignored = client.Player.IgnoredPlayers.Contains(accountId);

            if (ignored)
            {
                client.CallMethod(SysEntity.ClientSocialManagerId, new IgnoreRemovedPacket(accountId));

                client.Player.IgnoredPlayers.RemoveAll(remove => remove == accountId);

                using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
                unitOfWork.Ignoreds.RemoveIgnored(client.AccountEntry.Id, accountId);
            }
        }
        
        #endregion
    }
}
