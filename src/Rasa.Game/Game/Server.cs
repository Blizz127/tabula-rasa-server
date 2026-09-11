using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;

namespace Rasa.Game
{
    using Commands;
    using Config;
    using Data;
    using Hosting;
    using Login;
    using Managers;
    using Memory;
    using Networking;
    using Packets;
    using Packets.Communicator;
    using Queue;
    using Repositories.UnitOfWork;
    using Structures;
    using Threading;
    using Timer;

    public class Server : ILoopable, IRasaServer
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly IClientFactory _clientFactory;
        public static IGameUnitOfWorkFactory GameUnitOfWorkFactory;

        public string ServerType { get; } = "Game";

        public const int MainLoopTime = 100; // Milliseconds

        public Config Config { get; private set; }
        public IPAddress PublicAddress { get; }
        public LengthedSocket AuthCommunicator { get; private set; }
        public LengthedSocket ListenerSocket { get; private set; }
        public QueueManager QueueManager { get; private set; }
        public LoginManager LoginManager { get; set; } = new LoginManager();
        public static List<Client> Clients { get; } = new List<Client>();
        public Dictionary<uint, LoginAccountEntry> IncomingClients { get; } = new Dictionary<uint, LoginAccountEntry>();
        public MainLoop Loop { get; }
        public Timer Timer { get; } = new Timer();
        public bool Running => Loop != null && Loop.Running;
        public bool IsFull => CurrentPlayers >= Config.ServerInfoConfig.MaxPlayers;

        /// <summary>
        /// Players holding a slot: every world connection past login (character selection,
        /// loading, in world, teleporting), plus queue clients already handed off to the world
        /// port but not yet logged in there - without those, a burst of arrivals all see a free
        /// slot before any of them reaches the world. Computed on demand; it used to be a settable
        /// property nothing ever set, so it read 0, IsFull was never true, the queue let everyone
        /// straight through and the server list always showed an empty server.
        /// </summary>
        public ushort CurrentPlayers
        {
            get
            {
                int count;

                lock (Clients)
                    count = Clients.Count(c => c.IsAuthenticated());

                if (QueueManager != null)
                    count += QueueManager.RedirectingClients;

                return (ushort)Math.Min(count, ushort.MaxValue);
            }
        }

        private readonly List<Client> _clientsToRemove = new List<Client>();

        /// <summary>
        /// Accounts Auth has reported as banned while this process was connected to it. Auth refuses
        /// a locked account at login, so this only has to cover players who were already past that
        /// check when the ban landed - a redirect in flight is refused at the world login.
        /// </summary>
        private readonly HashSet<uint> _lockedAccounts = new HashSet<uint>();
        private readonly PacketRouter<Server, CommOpcode> _router = new PacketRouter<Server, CommOpcode>();
        public Server(
            IHostApplicationLifetime hostApplicationLifetime,
            IClientFactory clientFactory,
            IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _clientFactory = clientFactory;
            GameUnitOfWorkFactory= gameUnitOfWorkFactory;

            Configuration.OnLoad += ConfigLoaded;
            Configuration.OnReLoad += ConfigReLoaded;
            Configuration.Load();

            Loop = new MainLoop(this, MainLoopTime);

            PublicAddress = IPAddress.Parse(Config.GameConfig.PublicAddress);

            LengthedSocket.InitializeEventArgsPool(Config.SocketAsyncConfig.MaxClients * Config.SocketAsyncConfig.ConcurrentOperationsByClient);

            BufferManager.Initialize(Config.SocketAsyncConfig.BufferSize, Config.SocketAsyncConfig.MaxClients, Config.SocketAsyncConfig.ConcurrentOperationsByClient);

            CommandProcessor.RegisterCommand("exit", ProcessExitCommand);
            CommandProcessor.RegisterCommand("reload", ProcessReloadCommand);
        }

        ~Server()
        {
            Shutdown();
        }

        #region Configuration
        private static void ConfigReLoaded()
        {
            Logger.WriteLog(LogType.Initialize, "Config file reloaded by external change!");

            // Totally reload the configuration, because it's automatic reload case can only handle one reload. Our code's bug?
            Configuration.Load();
        }

        private void ConfigLoaded()
        {
            Config = new Config();
            Configuration.Bind(Config);

            Logger.UpdateConfig(Config.LoggerConfig);
        }
        #endregion

        public void Disconnect(Client client)
        {
            lock (_clientsToRemove)
                _clientsToRemove.Add(client);
        }

        public void MainLoop(long delta)
        {
            Timer.Update(delta);

            if (Clients.Count == 0)
                return;

            MapChannelManager.Instance.MapChannelWorker(delta);

            lock (Clients)
            {
                foreach (var client in Clients)
                    client.Update(delta);

                if (_clientsToRemove.Count > 0)
                {
                    lock (_clientsToRemove)
                    {
                        foreach (var client in _clientsToRemove)
                            Clients.Remove(client);

                        _clientsToRemove.Clear();
                    }
                }
            }
        }

        #region Socketing

        public bool Start()
        {
            // If no config file has been found, these values are 0 by default
            if (Config.GameConfig.Port == 0 || Config.GameConfig.Backlog == 0)
            {
                Logger.WriteLog(LogType.Error, "Invalid config values!");
                return false;
            }

            Loop.Start();

            SetupCommunicator();

            try
            {
                ListenerSocket = new LengthedSocket(SizeType.Dword, false);
                ListenerSocket.OnError += OnError;
                ListenerSocket.OnAccept += OnAccept;
                ListenerSocket.Bind(new IPEndPoint(IPAddress.Any, Config.GameConfig.Port));
                ListenerSocket.Listen(Config.GameConfig.Backlog);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, "Unable to create or start listening on the client socket! Exception:");
                Logger.WriteLog(LogType.Error, e);

                return false;
            }

            LoginManager.OnLogin += OnLogin;

            QueueManager = new QueueManager(this);

            ListenerSocket.AcceptAsync();

            Logger.WriteLog(LogType.Network, "*** Listening for clients on port {0}", Config.GameConfig.Port);

            Timer.Add("SessionExpire", 10000, true, () =>
            {
                var toRemove = new List<uint>();

                // A redirect session lasts one minute, which used to be harmless because the queue
                // never held anyone. With a real player count it does: keep the session alive while
                // its account is still connected to the queue, so a player who waited more than a
                // minute is not refused at the world login. Once the queue lets go of them they have
                // the usual minute to arrive.
                var queued = QueueManager?.ConnectedUserIds() ?? new HashSet<uint>();

                lock (IncomingClients)
                {
                    foreach (var entry in IncomingClients)
                        if (queued.Contains(entry.Key))
                            entry.Value.ExpireTime = DateTime.Now.AddMinutes(1);

                    toRemove.AddRange(IncomingClients.Where(ic => ic.Value.ExpireTime < DateTime.Now).Select(ic => ic.Key));

                    foreach (var rem in toRemove)
                        IncomingClients.Remove(rem);
                }
            });

            Timer.Add("QueueManagerUpdate", Config.QueueConfig.UpdateInterval, true, () =>
            {
                QueueManager.Update(Config.ServerInfoConfig.MaxPlayers - CurrentPlayers);
            });

            // Load items from db
            EntityClassManager.Instance.LoadEntityClasses();
            MissionManager.Instance.LoadMissions();
            CreatureManager.Instance.CreatureInit();
            SpawnPoolManager.Instance.SpawnPoolInit();
            ChatCommandsManager.Instance.RegisterChatCommands();
            MapChannelManager.Instance.MapChannelInit();
            ClanManager.Instance.ClansInit();
            DynamicObjectManager.Instance.InitDynamicObjects();
            MapTriggerManager.Instance.MapTriggerInit();

            return true;
        }

        private void OnLogin(LoginClient client)
        {
            lock (Clients)
            {
                var newClient = _clientFactory.Create(client.Socket, client.Data, this);
                Clients.Add(newClient);
            }
        }

        private static void OnError(SocketAsyncEventArgs args)
        {
            if (args.LastOperation == SocketAsyncOperation.Accept && args.AcceptSocket != null && args.AcceptSocket.Connected)
                args.AcceptSocket.Shutdown(SocketShutdown.Both);
        }

        private void OnAccept(LengthedSocket newSocket)
        {
            ListenerSocket.AcceptAsync();

            if (newSocket == null)
                return;

            LoginManager.LoginSocket(newSocket);
        }

        public LoginAccountEntry AuthenticateClient(Client client, uint accountId, uint oneTimeKey)
        {
            lock (IncomingClients)
            {
                if (!IncomingClients.ContainsKey(accountId))
                    return null;

                var entry = IncomingClients[accountId];
                if (entry == null || entry.OneTimeKey != oneTimeKey)
                    return null;

                IncomingClients.Remove(accountId);

                return entry;
            }
        }

        public bool IsBanned(uint accountId)
        {
            lock (_lockedAccounts)
                return _lockedAccounts.Contains(accountId);
        }

        public bool IsAlreadyLoggedIn(uint accountId)
        {
            lock (Clients)
                foreach (var client in Clients)
                    if (client.IsAuthenticated() && client.AccountEntry.Id == accountId)
                        return true;

            return false;
        }

        public void Shutdown()
        {
            AuthCommunicator?.Close();
            AuthCommunicator = null;

            ListenerSocket?.Close();
            ListenerSocket = null;

            Loop.Stop();
        }
        #endregion

        #region Communicator
        private void SetupCommunicator()
        {
            if (Config.CommunicatorConfig.Port == 0 || Config.CommunicatorConfig.Address == null)
            {
                Logger.WriteLog(LogType.Error, "Invalid Communicator config data! Can't connect!");
                return;
            }

            ConnectCommunicator();
        }

        public void ConnectCommunicator()
        {
            if (AuthCommunicator?.Connected ?? false)
                AuthCommunicator?.Close();

            try
            {
                AuthCommunicator = new LengthedSocket(SizeType.Word);
                AuthCommunicator.OnConnect += OnCommunicatorConnect;
                AuthCommunicator.OnError += OnCommunicatorError;
                AuthCommunicator.ConnectAsync(new IPEndPoint(IPAddress.Parse(Config.CommunicatorConfig.Address), Config.CommunicatorConfig.Port));
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, "Unable to create or start listening on the Auth server socket! Retrying soon... Exception:");
                Logger.WriteLog(LogType.Error, e);
            }

            Logger.WriteLog(LogType.Network, $"*** Connecting to auth server! Address: {Config.CommunicatorConfig.Address}:{Config.CommunicatorConfig.Port}");
        }

        private void OnCommunicatorError(SocketAsyncEventArgs args)
        {
            Timer.Add("CommReconnect", 10000, false, () =>
            {
                if (!AuthCommunicator?.Connected ?? true)
                    ConnectCommunicator();
            });

            Logger.WriteLog(LogType.Error, "Could not connect to the Auth server! Trying again in a few seconds...");
        }

        private void OnCommunicatorConnect(SocketAsyncEventArgs args)
        {
            if (args.SocketError != SocketError.Success)
            {
                OnCommunicatorError(args);
                return;
            }

            Logger.WriteLog(LogType.Network, "*** Connected to the Auth Server!");

            AuthCommunicator.OnReceive += OnCommunicatorReceive;
            AuthCommunicator.Send(new LoginRequestPacket
            {
                ServerId = Config.ServerInfoConfig.Id,
                Password = Config.ServerInfoConfig.Password,
                PublicAddress = PublicAddress
            });

            AuthCommunicator.ReceiveAsync();
        }

        private void OnCommunicatorReceive(BufferData data)
        {
            var opcode = (CommOpcode) data.Buffer[data.BaseOffset + data.Offset++];

            var packetType = _router.GetPacketType(opcode);
            if (packetType == null)
                return;

            var packet = Activator.CreateInstance(packetType) as IOpcodedPacket<CommOpcode>;
            if (packet == null)
                return;

            packet.Read(data.GetReader());

            _router.RoutePacket(this, packet);
        }

        // ReSharper disable once UnusedMember.Local
        [PacketHandler(CommOpcode.LoginResponse)]
        private void MsgLoginResponse(LoginResponsePacket packet)
        {
            if (packet.Response == CommLoginReason.Success)
            {
                Logger.WriteLog(LogType.Network, "Successfully authenticated with the Auth server!");
                return;
            }

            AuthCommunicator?.Close();
            AuthCommunicator = null;

            Logger.WriteLog(LogType.Error, "Could not authenticate with the Auth server! Shutting down internal communication!");
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        [PacketHandler(CommOpcode.ServerInfoRequest)]
        private void MsgGameInfoRequest(ServerInfoRequestPacket packet)
        {
            AuthCommunicator.Send(new ServerInfoResponsePacket
            {
                AgeLimit = Config.ServerInfoConfig.AgeLimit,
                PKFlag = Config.ServerInfoConfig.PKFlag,
                CurrentPlayers = CurrentPlayers,
                GamePort = Config.GameConfig.Port,
                QueuePort = Config.QueueConfig.Port,
                // The configured cap the queue enforces. This used to report the socket pool size
                // (SocketAsyncConfig.MaxClients), which the auth server list showed as capacity.
                MaxPlayers = Config.ServerInfoConfig.MaxPlayers
            });
        }

        // ReSharper disable once UnusedMember.Local
        [PacketHandler(CommOpcode.AccountLockChanged)]
        private void MsgAccountLockChanged(AccountLockChangedPacket packet)
        {
            lock (_lockedAccounts)
            {
                if (packet.Locked)
                    _lockedAccounts.Add(packet.AccountId);
                else
                    _lockedAccounts.Remove(packet.AccountId);
            }

            if (!packet.Locked)
            {
                Logger.WriteLog(LogType.Security, $"Account {packet.AccountId} unbanned by Auth.");
                return;
            }

            // Leave any pending IncomingClients entry in place: the world login consumes it and then
            // reaches IsBanned, so the player is told "account locked" instead of a generic
            // authentication failure. It expires on its own if they never arrive.
            QueueManager?.Disconnect(packet.AccountId);

            List<Client> online;

            // AccountEntry is null until the world login message is processed.
            lock (Clients)
                online = Clients.Where(c => c.AccountEntry != null && c.AccountEntry.Id == packet.AccountId).ToList();

            // Close() runs from socket threads already (OnError), saves the character and flags an
            // in-world player for removal on the MainLoop, so it is safe from this communicator thread.
            foreach (var client in online)
                client.Close();

            Logger.WriteLog(LogType.Security, $"Account {packet.AccountId} banned by Auth; disconnected {online.Count} world connection(s).");
        }

        // ReSharper disable once UnusedMember.Local
        [PacketHandler(CommOpcode.RedirectRequest)]
        private void MsgRedirectRequest(RedirectRequestPacket packet)
        {
            lock (IncomingClients)
            {
                if (IncomingClients.ContainsKey(packet.AccountId))
                    IncomingClients.Remove(packet.AccountId);

                IncomingClients.Add(packet.AccountId, new LoginAccountEntry(packet));
            }

            AuthCommunicator.Send(new RedirectResponsePacket
            {
                AccountId = packet.AccountId,
                Response = RedirectResult.Success
            });
        }
        #endregion

        #region Commands
        private void ProcessExitCommand(string[] parts)
        {
            var minutes = 0;

            if (parts.Length > 1)
                minutes = int.Parse(parts[1]);

            Timer.Add("exit", minutes * 60000, false, () =>
            {
                Shutdown();
                _hostApplicationLifetime.StopApplication();
            });

            Logger.WriteLog(LogType.Command, $"Exiting the server in {minutes} minute(s).");
        }

        private static void ProcessReloadCommand(string[] parts)
        {
            if (parts.Length > 1 && parts[1] == "config")
            {
                Configuration.Load();
                return;
            }

            Logger.WriteLog(LogType.Command, "Invalid reload command!");
        }

        /*private void ProcessRestartCommand(string[] parts)
        {
            // TODO: delayed restart, with contacting globals, so they can warn players not to leave the server, or they won't be able to reconnect
        }

        private void ProcessShutdownCommand(string[] parts)
        {
            // TODO: delayed shutdown, with contacting globals, so they can warn players not to leave the server, or they won't be able to reconnect
            // TODO: add timer to report the remaining time until shutdown?
            // TODO: add timer to contact global servers to tell them periodically that we're getting shut down?
        }*/
        #endregion
    }
}
