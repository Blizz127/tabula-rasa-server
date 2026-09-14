using System;
using System.Net.Sockets;

namespace Rasa.Auth
{
    using Cryptography;
    using Data;
    using Extensions;
    using Memory;
    using Networking;
    using Packets;
    using Packets.Auth.Client;
    using Packets.Auth.Server;
    using Repositories;
    using Repositories.Auth.Account;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Auth;
    using Timer;

    public class Client
    {
        private readonly IAuthUnitOfWorkFactory _authUnitOfWorkFactory;

        public const int LengthSize = 2;

        public LengthedSocket Socket { get; }
        public Server Server { get; }

        public uint OneTimeKey { get; }
        public uint SessionId1 { get; }
        public uint SessionId2 { get; }
        public AuthAccountEntry AccountEntry { get; private set; }
        public ClientState State { get; private set; }
        public Timer Timer { get; }

        private PacketQueue _packetQueue = new();

        public Client(LengthedSocket socket, Server server, IAuthUnitOfWorkFactory authUnitOfWorkFactory)
        {
            _authUnitOfWorkFactory = authUnitOfWorkFactory;

            Socket = socket;
            Server = server;
            State = ClientState.Connected;

            Timer = new Timer();

            Socket.OnError += OnError;
            Socket.OnDrop += OnDrop;
            Socket.OnReceive += OnReceive;
            Socket.OnDecrypt += OnDecrypt;

            Socket.ReceiveAsync();

            var rnd = new Random();

            OneTimeKey = rnd.NextUInt();
            SessionId1 = rnd.NextUInt();
            SessionId2 = rnd.NextUInt();

            SendPacket(new ProtocolVersionPacket(OneTimeKey));

            // This is here (after ProtocolVersionPacket), so it won't get encrypted
            Socket.OnEncrypt += OnEncrypt;

            Timer.Add("timeout", Server.Config.AuthConfig.ClientTimeout * 1000, false, () =>
            {
                Logger.WriteLog(LogType.Network, "*** Client timed out! Ip: {0}", Socket.RemoteAddress);

                Close();
            });

            Logger.WriteLog(LogType.Network, "*** Client connected from {0}", Socket.RemoteAddress);
        }

        public void Update(long delta)
        {
            // The timeout timer's callback closes the connection, and Close() does real work.
            try
            {
                Timer.Update(delta);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Error updating timers for {Socket.RemoteAddress}, disconnecting client: {e}");
                Close();
                return;
            }

            if (State == ClientState.Disconnected)
                return;

            IBasePacket packet;

            // This is the auth server's main loop thread, and nothing above it catches: an
            // exception out of a handler used to end the process. Now it ends the connection.
            while ((packet = _packetQueue.PopIncoming()) != null)
            {
                try
                {
                    HandlePacket(packet);
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"Error handling {packet.GetType().Name} from {Socket.RemoteAddress}, disconnecting client: {e}");
                    Close();
                    return;
                }
            }

            // Writing a packet can throw - serialization, or a socket that has gone since the
            // packet was queued - and the game server has guarded this half for a while. Auth
            // had not: an exception here reached the main loop with nothing above it.
            try
            {
                while ((packet = _packetQueue.PopOutgoing()) != null)
                    SendPacket(packet);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Error sending queued packets to {Socket.RemoteAddress}, disconnecting client: {e}");
                Close();
            }
        }
        
        public void Close()
        {
            if (State == ClientState.Disconnected)
                return;

            Logger.WriteLog(LogType.Network, "*** Client disconnected! Ip: {0}", Socket.RemoteAddress);

            Timer.Remove("timeout");

            State = ClientState.Disconnected;

            Socket.Close();

            Server.Disconnect(this);
        }

        public void SendPacket(IBasePacket packet)
        {
            Socket.Send(packet);
        }

        public void HandlePacket(IBasePacket packet)
        {
            if (packet is not IOpcodedPacket<ClientOpcode> authPacket)
                return;

            // Each message belongs to a point in the conversation, and the handlers assume it:
            // everything after Login reads AccountEntry, which Login sets. A ServerListExt or
            // AboutToPlay sent first dereferenced null on the main loop thread. Login is the
            // only thing a fresh connection may say; once it has said it, it may not again.
            if (!IsExpected(authPacket.Opcode))
            {
                Logger.WriteLog(LogType.Security, $"Client {Socket.RemoteAddress} sent {authPacket.Opcode} in state {State}; disconnecting.");
                Close();
                return;
            }

            switch (authPacket.Opcode)
            {
                case ClientOpcode.Login:
                    MsgLogin(authPacket as LoginPacket);
                    break;

                case ClientOpcode.Logout:
                    MsgLogout(authPacket as LogoutPacket);
                    break;

                case ClientOpcode.AboutToPlay:
                    MsgAboutToPlay(authPacket as AboutToPlayPacket);
                    break;

                case ClientOpcode.ServerListExt:
                    MsgServerListExt(authPacket as ServerListExtPacket);
                    break;
            }
        }

        private bool IsExpected(ClientOpcode opcode)
        {
            switch (opcode)
            {
                case ClientOpcode.Login:
                    return State == ClientState.Connected;

                case ClientOpcode.ServerListExt:
                case ClientOpcode.AboutToPlay:
                    return AccountEntry != null && (State == ClientState.LoggedIn || State == ClientState.ServerList);

                default:
                    // Logout and SCCheck carry nothing the state has to be ready for.
                    return true;
            }
        }

        public void RedirectionResult(RedirectResult result, ServerInfo info)
        {
            switch (result)
            {
                case RedirectResult.Fail:
                    SendPacket(new PlayFailPacket(FailReason.UnexpectedError));

                    Close();

                    Logger.WriteLog(LogType.Error, $"Account ({AccountEntry.Username}, {AccountEntry.Id}) couldn't be redirected to server: {info.ServerId}!");
                    break;

                case RedirectResult.Success:
                    HandleSuccessfulRedirect(info);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(result));
            }
        }

        private void HandleSuccessfulRedirect(ServerInfo info)
        {
            SendPacket(new HandoffToQueuePacket
            {
                OneTimeKey = OneTimeKey,
                ServerId = info.ServerId,
                AccountId = AccountEntry.Id
            });

            using var unitOfWork = _authUnitOfWorkFactory.Create();
            unitOfWork.AuthAccountRepository.UpdateLastServer(AccountEntry.Id, info.ServerId);
            unitOfWork.Complete();

            Logger.WriteLog(LogType.Network, $"Account ({AccountEntry.Username}, {AccountEntry.Id}) was redirected to the queue of the server: {info.ServerId}!");
        }

        private void OnError(SocketAsyncEventArgs args)
        {
            Close();
        }

        /// <summary>
        /// The socket has given up on this connection - a full send queue, a stream that stopped
        /// framing, or no buffers left to serve it. Nothing more can pass either way, so let it go.
        /// </summary>
        private void OnDrop(string reason)
        {
            Close();
        }

        private static void OnEncrypt(BufferData data, ref int length)
        {
            AuthCryptManager.Encrypt(data.Buffer, data.BaseOffset + data.Offset, ref length, data.RemainingLength);
        }

        private static bool OnDecrypt(BufferData data)
        {
            return AuthCryptManager.Decrypt(data.Buffer, data.BaseOffset + data.Offset, data.RemainingLength);
        }

        /// <summary>
        /// Socket completion thread. Nothing may escape: an opcode this server has no packet for
        /// throws out of CreatePacket, and a malformed body throws out of Read - and the caller
        /// is a socket callback, where an exception used to end the process. It costs this one
        /// connection instead, and says which opcode did it.
        /// </summary>
        private void OnReceive(BufferData data)
        {
            // Nullable, not a default: Login is 0x00, so a default would name it as the culprit
            // when the failure was reading the opcode byte itself.
            ClientOpcode? opcode = null;

            try
            {
                // Reset the timeout after every action
                Timer.ResetTimer("timeout");

                using var br = data.GetReader();

                opcode = (ClientOpcode)br.ReadByte();

                var packet = CreatePacket(opcode.Value);

                packet.Read(br);

                _packetQueue.EnqueueIncoming(packet);
            }
            catch (Exception e)
            {
                var what = opcode.HasValue ? $"a {opcode.Value} packet" : "a packet whose opcode could not be read";
                Logger.WriteLog(LogType.Error, $"Error reading {what} from {Socket.RemoteAddress}, disconnecting client: {e}");
                Close();
            }
        }

        private IBasePacket CreatePacket(ClientOpcode opcode)
        {
            return opcode switch
            {
                ClientOpcode.AboutToPlay   => new AboutToPlayPacket(),
                ClientOpcode.Login         => new LoginPacket(),
                ClientOpcode.Logout        => new LogoutPacket(),
                ClientOpcode.ServerListExt => new ServerListExtPacket(),
                ClientOpcode.SCCheck       => new SCCheckPacket(),

                _ => throw new ArgumentOutOfRangeException(nameof(opcode)),
            };
        }

        #region Handlers
        private void MsgLogin(LoginPacket packet)
        {
            using var unitOfWork = _authUnitOfWorkFactory.Create();

            try
            {
                AccountEntry = unitOfWork.AuthAccountRepository.GetByUserName(packet.UserName, packet.Password);
            }
            catch (EntityNotFoundException)
            {
                SendPacket(new LoginFailPacket(FailReason.UserNameOrPassword));
                Close();
                Logger.WriteLog(LogType.Security, $"User ({packet.UserName}) tried to log in with an invalid username!");
                return;
            }
            catch (PasswordCheckFailedException e)
            {
                SendPacket(new LoginFailPacket(FailReason.UserNameOrPassword));
                Close();
                Logger.WriteLog(LogType.Security, e.Message);
                return;
            }
            catch (AccountLockedException e)
            {
                SendPacket(new BlockedAccountPacket());
                Close();
                Logger.WriteLog(LogType.Security, e.Message);
                return;
            }

            unitOfWork.AuthAccountRepository.UpdateLoginData(AccountEntry.Id, Socket.RemoteAddress);
            unitOfWork.Complete();

            State = ClientState.LoggedIn;

            SendPacket(new LoginOkPacket
            {
                SessionId1 = SessionId1,
                SessionId2 = SessionId2
            });

            Logger.WriteLog(LogType.Network, "*** Client logged in from {0}", Socket.RemoteAddress);
        }

#pragma warning disable IDE0060 // Remove unused parameter
        private void MsgLogout(LogoutPacket packet)
        {
            Close();
        }

        private void MsgServerListExt(ServerListExtPacket packet)
        {
            State = ClientState.ServerList;

            SendPacket(new SendServerListExtPacket(Server.GetServerListSnapshot(), AccountEntry.LastServerId));
        }
#pragma warning restore IDE0060 // Remove unused parameter

        private void MsgAboutToPlay(AboutToPlayPacket packet)
        {
            if (SessionId1 != packet.SessionId1 || SessionId2 != packet.SessionId2)
            {
                Logger.WriteLog(LogType.Security, $"Account ({AccountEntry.Username}, {AccountEntry.Id}) has sent an AboutToPlay packet with invalid session data!");
                return;
            }

            Server.RequestRedirection(this, packet.ServerId);
        }
        #endregion
    }
}
