using System;
using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.ClientMethod.Server;
    using Packets.Game.Server;
    using Packets.MapChannel.Server;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.World;
    using Timer;

    public class MapChannelManager
    {
        private static MapChannelManager _instance;
        private static readonly object InstanceLock = new object();
        private readonly int MapChannel_PlayerQueue = 32;
        public readonly Dictionary<uint, MapChannel> MapChannelArray = new Dictionary<uint, MapChannel>();           // list of loaded maps
        public readonly Timer Timer = new();

        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;
        private readonly Func<long> _getMonotonicMilliseconds;
        public static MapChannelManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new MapChannelManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }
        public MapChannelManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory, Func<long> getMonotonicMilliseconds = null)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
            _getMonotonicMilliseconds = getMonotonicMilliseconds ?? (() => Environment.TickCount64);
        }

        public void CharacterLogout(Client client)
        {
            if (client.State == ClientState.Disconnected || client.Player.RemoveFromMap ||
                !client.Player.LogoutCountdown.TryComplete(_getMonotonicMilliseconds()))
                return;

            client.Player.RemoveFromMap = true;
            client.State = ClientState.LoggedIn;
        }

        public MapChannel FindByContextId(uint contextId)
        {
            return MapChannelArray[contextId];
        }

        public bool TryFindByContextId(uint contextId, out MapChannel mapChannel)
        {
            return MapChannelArray.TryGetValue(contextId, out mapChannel);
        }

        public Dictionary<int, AbilityDrawerData> GetPlayerAbilities(uint characterId)
        {
            var abilities = new Dictionary<int, AbilityDrawerData>();
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var abilitiesData = unitOfWork.CharacterAbilityDrawers.GetCharacterAbilities(characterId);

            foreach (var ability in abilitiesData)
            {
                if (ability.AbilityId == 0) continue;

                abilities.Add(ability.AbilitySlot, new AbilityDrawerData(ability.AbilitySlot, ability.AbilityId, ability.AbilityLevel));
            }

            return abilities;
        }

        public Dictionary<SkillId, SkillsData> GetPlayerSkills(uint characterId)
        {
            var skills = new Dictionary<SkillId, SkillsData>();
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var skillsData = unitOfWork.CharacterSkills.GetCharacterSkills(characterId);

            foreach (var skill in skillsData)
                skills.Add((SkillId)skill.SkillId, new SkillsData((SkillId)skill.SkillId, skill.AbilityId, skill.SkillLevel));

            return skills;
        }

        public void MapChannelInit()
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var loadedMaps = unitOfWork.MapInfos.Get();

            foreach (var mapInfo in loadedMaps)
            {
                // load all maps
                var newMapChannel = new MapChannel
                {
                    MapInfo = new MapInfo(mapInfo),
                    //TimerClientEffectUpdate = Environment.TickCount,
                    //TimerMissileUpdate = Environment.TickCount,
                    //TimerDynObjUpdate = Environment.TickCount,
                    //TimerGeneralTimer = Environment.TickCount,
                    //TimerController = Environment.TickCount,
                    //TimerPlayerUpdate = Environment.TickCount,
                    //PlayerCount = 0,
                    PlayerLimit = 128,
                    ClientList = new List<Client>()
                };
                // register mapChannel
                MapChannelArray.Add(mapInfo.Id, newMapChannel);
            }
            Logger.WriteLog(LogType.Initialize, "");
            Logger.WriteLog(LogType.Initialize, "Server ready!");

            Timer.Add("CheckForLogingClients", 1000, true, null);
            Timer.Add("CheckForObjects", 1000, true, null);
            Timer.Add("CellUpdateVisibility", 1000, true, null);
            Timer.Add("CheckForCreatures", 1000, true, null);
            Timer.Add("CheckForMapTriggers", 1000, true, null);
        }

        public void MapChannelWorker(long delta)
        {
            Timer.Update(delta);

            foreach (var t in MapChannelArray)
            {
                var mapChannel = t.Value;

                mapChannel.MapChannelElapsed += delta;

                if (Timer.IsTriggered("CheckForLogingClients"))
                    if (mapChannel.QueuedClients.Count > 0)
                    {
                        // create new mapClient
                        var dequedClient = mapChannel.QueuedClients.Dequeue();

                        // add it to list
                        if (dequedClient.State != ClientState.Disconnected && !dequedClient.Player.Disconected)
                            mapChannel.ClientList.Add(dequedClient);
                    }

                if (mapChannel.ClientList.Count > 0)
                {
                    ActorActionManager.Instance.DoWork(mapChannel, delta);
                    MissileManager.Instance.DoWork(mapChannel, delta);
                    BehaviorManager.Instance.MapChannelThink(mapChannel, delta);
                    DynamicObjectManager.Instance.DropshipsWorker(mapChannel, delta);

                    // CellManager worker
                    if (Timer.IsTriggered("CellUpdateVisibility"))
                        CellManager.Instance.DoWork(mapChannel);

                    // check for objects
                    if (Timer.IsTriggered("CheckForObjects"))
                        DynamicObjectManager.Instance.DynamicObjectWorker(mapChannel, delta);

                    // check for creatures
                    if (Timer.IsTriggered("CheckForCreatures"))
                        SpawnPoolManager.Instance.SpawnPoolWorker(mapChannel, delta);

                    // check for mapTriggers
                    if (Timer.IsTriggered("CheckForMapTriggers"))
                        MapTriggerManager.Instance.TriggersProximityWorker(mapChannel);

                    // Account for every elapsed interval; sampling only one delta
                    // every 500 ms made effect durations and drain run too slowly.
                    GameEffectManager.Instance.DoWork(mapChannel, delta);

                    // Area-bound mission objectives (no work in contexts without live content areas).
                    MissionContentManager.Instance.DoWork(mapChannel);

                    // chack for player LogOut
                    foreach (var client in mapChannel.ClientList)
                        if (client != null)
                            if (client.Player.RemoveFromMap == true)
                            {
                                RemovePlayer(client, true);
                                break;
                            }
                }
            }
            // The autofire list is global: advance it once per elapsed interval,
            // independent of how many maps currently contain players.
            ManifestationManager.Instance.AutoFireTimerDoWork(delta);
        }

        public void MapLoaded(Client client)
        {
            if (client.State == ClientState.Teleporting)
            {
                var dropship = new Dropship(Factions.AFS, DropshipType.Teleporter, client);
                var mapChannel = MapChannelArray[client.LoadingMap];

                client.Player.MapChannel = mapChannel;
                client.Player.MapContextId = dropship.Client.LoadingMap;

                mapChannel.ClientList.Add(client);

                CellManager.Instance.AddToWorld(client.Player.MapChannel, dropship);
                DynamicObjectManager.Instance.Dropships.Add(dropship.EntityId, dropship);
                CommunicatorManager.Instance.LoginOk(dropship.Client);

                InventoryManager.Instance.InitForClient(client);
                ManifestationManager.Instance.UpdateStatsValues(client, true);

                CellManager.Instance.AddToWorld(dropship.Client); // will introduce the player to all clients, including the current owner
                CellManager.Instance.CellCallMethod(dropship.Client.Player.MapChannel, dropship.Client.Player, new TeleportArrivalPacket());
                client.CallMethod(SysEntity.ClientMethodId, new RequestMovementBlockPacket());
                ManifestationManager.Instance.AssignPlayer(client);
                MissionManager.Instance.SendMissionStatusInfo(client);
                MissionContentManager.Instance.OnPlayerEnteredMap(client);
                CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Position);
                CommunicatorManager.Instance.PlayerEnterMap(dropship.Client);

                return;
            }

            client.State = ClientState.Ingame;
            InventoryManager.Instance.InitForClient(client);
            ManifestationManager.Instance.UpdateStatsValues(client, true);

            // register new Player
            EntityManager.Instance.RegisterEntity(client.Player.EntityId, EntityType.Character);
            EntityManager.Instance.RegisterPlayer(client.Player.EntityId, client.Player);
            EntityManager.Instance.RegisterActor(client.Player.EntityId, client.Player);
            CommunicatorManager.Instance.LoginOk(client);

            CellManager.Instance.AddToWorld(client); // will introduce the player to all clients, including the current owner
            ManifestationManager.Instance.AssignPlayer(client);
            MissionManager.Instance.SendMissionStatusInfo(client);
            MissionContentManager.Instance.OnPlayerEnteredMap(client);

            ClanManager.Instance.InitializePlayerClanData(client);
            InventoryManager.Instance.InitClanInventory(client);
            CommunicatorManager.Instance.PlayerEnterMap(client);
        }

        public void PassClientToCharacterSelection(Client client)
        {
            // ToDo
            /*if (ClientsGameMainCount >= MAX_GAMEMAIN_CLIENTS)
            {
                // force disconnect
                closesocket(cgm->socket);
                //free(cgm);
                return;
            }*/
            CharacterManager.Instance.StartCharacterSelection(client);
            //Increase count and return struct
            //ClientsGameMainCount++;
        }
        public void PassClientToMapInstance(Client client)
        {
            var mapInstance = client.Player.MapChannel;
            client.CallMethod(SysEntity.ClientMethodId, new PreWonkavatePacket());
            client.CallMethod(SysEntity.CurrentInputStateId, new WonkavatePacket
               (
                   mapInstance.MapInfo.MapContextId,
                   1,           // InstanceId
                   mapInstance.MapInfo.MapVersion,
                    client.Player.Position,
                   (float)client.Player.Rotation
               ));

            client.State = ClientState.Loading;
            client.State = ClientState.Loading;
            client.Player.MapChannel.QueuedClients.Enqueue(client);
        }

        public void Ping(Client client, double ping)
        {
            client.CallMethod(SysEntity.ClientMethodId, new AckPingPacket(ping));
        }

        public void RemovePlayer(Client client, bool logout)
        {
            var player = client.Player;
            if (player == null || player.Disconected)
                return;

            client.SaveCharacter();

            // unregister Communicator
            if (EntityManager.Instance.Players.TryGetValue(player.EntityId, out var registered) && registered == player)
                CommunicatorManager.Instance.PlayerExitMap(client);
            // unregister mapChannelClient
            EntityManager.Instance.UnregisterEntity(client.Player.EntityId);
            EntityManager.Instance.UnregisterPlayer(client.Player.EntityId);
            EntityManager.Instance.UnregisterActor(client.Player.EntityId);

            // unregister character Inventory
            var inventoryIds = new HashSet<ulong>(player.Inventory.EquippedInventory);
            inventoryIds.UnionWith(player.Inventory.HomeInventory);
            inventoryIds.UnionWith(player.Inventory.PersonalInventory);
            inventoryIds.UnionWith(player.Inventory.WeaponDrawer);
            foreach (var entityId in inventoryIds)
                if (entityId != 0)
                    EntityManager.Instance.DestroyPhysicalEntity(client, entityId, EntityType.Item);

            if (registered == player)
                ManifestationManager.Instance.StopAutoFire(client);
            CellManager.Instance.RemoveFromWorld(client);
            ManifestationManager.Instance.RemovePlayerCharacter(client);
            ClanManager.Instance.RemovePlayer(client);

            player.Disconected = true;
            player.RemoveFromMap = true;
            player.LogoutCountdown.Cancel();
            player.CurrentAbility = null;
            player.CurrentWeaponAction = null;
            player.CurrentWeaponAttack = null;
            var map = player.MapChannel;
            if (map != null)
            {
                map.ClientList.RemoveAll(candidate => candidate == client);
                map.PerformRecovery.RemoveAll(action => action.Actor == player);
                // A socket may close before the loading queue has admitted its character.
                for (var remaining = map.QueuedClients.Count; remaining > 0; remaining--)
                {
                    var queued = map.QueuedClients.Dequeue();
                    if (queued != client)
                        map.QueuedClients.Enqueue(queued);
                }
            }

            if (logout && client.State != ClientState.Disconnected)
                PassClientToCharacterSelection(client);
        }

        public static bool HasWorldPresence(Client client)
            => client.Player?.MapChannel != null && !client.Player.Disconected;

        public bool TryRemoveDisconnectedClient(Client client)
        {
            if (client.State != ClientState.Disconnected)
                return false;

            if (HasWorldPresence(client) && client.Player.LogoutCountdown.IsWaiting(_getMonotonicMilliseconds()))
                return false;

            // No verified retail grace period exists for loss without RequestLogout.
            RemovePlayer(client, false);
            return true;
        }

        public void RequestLogout(Client client)
        {
            if (client.State == ClientState.Disconnected || client.Player.RemoveFromMap)
                return;

            var remaining = client.Player.LogoutCountdown.Begin(_getMonotonicMilliseconds());
            client.CallMethod(SysEntity.ClientMethodId, new LogoutTimeRemainingPacket(remaining));
        }

        public void CancelLogoutRequest(Client client)
        {
            if (client.State != ClientState.Disconnected)
                client.Player.LogoutCountdown.Cancel();
        }

        public MapInstance GetMapInstance(uint mapContextId)
        {
            // TODO support additional maps
            var map = new MapInstance(new MapInfo(1220, "adv_foreas_concordia_wilderness", 1556, 0));

            return map;
        }
    }
}
