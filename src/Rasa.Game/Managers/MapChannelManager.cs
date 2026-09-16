using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

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
        private readonly Func<long> _getMonotonicMilliseconds;

        public MapChannelManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory, Func<long> getMonotonicMilliseconds = null)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
            _getMonotonicMilliseconds = getMonotonicMilliseconds ?? (() => Environment.TickCount64);
        }

        /// <summary>
        /// Wait between RequestLogout and an honoured CharacterLogout. Sent to the client as
        /// LogoutTimeRemaining, which keeps the logout window's Logout button disabled until it
        /// has elapsed.
        /// </summary>
        public const int LogoutDelayMs = 5000;

        /// <summary>
        /// Allowance for clock-rate drift on the early-logout check. A normal client cannot be
        /// early: it starts its countdown when LogoutTimeRemaining arrives, which is after the
        /// server recorded the request.
        /// </summary>
        private const int LogoutDelayToleranceMs = 250;

        public void CharacterLogout(Client client)
        {
            if (client.State == ClientState.Disconnected || client.Player.RemoveFromMap ||
                !client.Player.LogoutCountdown.TryComplete(_getMonotonicMilliseconds()))
                return;

            client.Player.RemoveFromMap = true;
            client.State = ClientState.LoggedIn;
        }

        /// <summary>
        /// The logout window's Cancel button (client/ui/logoutwindow.py:108). Withdraws a pending
        /// logout, so a CharacterLogout that follows is ignored until the player requests again.
        /// </summary>
        public void CancelLogoutRequest(Client client)
        {
            if (client.State != ClientState.Disconnected)
                client.Player.LogoutCountdown.Cancel();
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

        public MapChannel FindByContextId(uint contextId)
        {
            return MapChannelArray[contextId];
        }

        #region Private instances

        /// <summary>Private per-character instances by instance id (never 0 or 1).</summary>
        public readonly Dictionary<uint, MapChannel> InstanceChannels = new();

        private readonly Dictionary<uint, MapChannel> _characterInstances = new();
        private readonly List<MapChannel> _instancesToDestroy = new();
        private uint _lastInstanceId = 1;

        /// <summary>Whether a context gives each character its own instance (content_map_setting).</summary>
        public Func<uint, bool> IsPerCharacterContext { get; set; } = contextId =>
            MissionContentManager.Instance.Content.Catalog.InstancingFor(contextId) == MapInstancing.PerCharacter;

        /// <summary>Fills a new instance with its context's content placements.</summary>
        public Action<MapChannel> PopulateInstance { get; set; } = channel =>
            ContentMaterializer.Materialize(channel, MissionContentManager.Instance.Content);

        /// <summary>Every live channel, shared contexts first; a snapshot, safe to change the registry while iterating.</summary>
        public List<MapChannel> Channels() => MapChannelArray.Values.Concat(InstanceChannels.Values).ToList();

        /// <summary>The channel a creature is in, falling back to its context for creatures never added to a channel.</summary>
        public static MapChannel ChannelOf(Creature creature)
            => creature.MapChannel ?? Instance.FindByContextId(creature.MapContextId);

        public static MapChannel ChannelOf(DynamicObject dynamicObject)
            => dynamicObject.MapChannel ?? Instance.FindByContextId(dynamicObject.MapContextId);

        /// <summary>
        /// Whether a creature is in this channel. Two instances of one context share its id, so the
        /// channel itself is compared; a creature never added to a channel falls back to its context.
        /// </summary>
        public static bool IsOnChannel(Creature creature, MapChannel channel)
            => creature != null && channel != null && (creature.MapChannel != null
                ? ReferenceEquals(creature.MapChannel, channel)
                : creature.MapContextId == channel.MapInfo?.MapContextId);

        public static bool IsOnChannel(DynamicObject dynamicObject, MapChannel channel)
            => dynamicObject != null && channel != null && (dynamicObject.MapChannel != null
                ? ReferenceEquals(dynamicObject.MapChannel, channel)
                : dynamicObject.MapContextId == channel.MapInfo?.MapContextId);

        /// <summary>
        /// The channel a character enters for a context: the shared channel, or for a per-character
        /// context a new private instance (build plan S3, OD-2). A previous instance of the same
        /// character is released, so one character never holds two. Null for an unknown context.
        /// </summary>
        public MapChannel ChannelForEntry(uint characterId, uint contextId)
        {
            if (!MapChannelArray.TryGetValue(contextId, out var shared))
                return null;

            if (!IsPerCharacterContext(contextId))
                return shared;

            if (_characterInstances.TryGetValue(characterId, out var previous))
            {
                _characterInstances.Remove(characterId);
                ReleaseInstanceIfEmpty(previous);
            }

            var channel = new MapChannel
            {
                MapInfo = shared.MapInfo,
                InstanceId = ++_lastInstanceId,
                OwnerCharacterId = characterId,
                PlayerLimit = 1,
                ClientList = new List<Client>()
            };

            InstanceChannels.Add(channel.InstanceId, channel);
            _characterInstances[characterId] = channel;
            PopulateInstance(channel);

            Logger.WriteLog(LogType.Debug, $"Created instance {channel.InstanceId} of context {contextId} for character {characterId}");
            return channel;
        }

        /// <summary>Queues a private instance with nobody in or entering it for destruction after the tick.</summary>
        public void ReleaseInstanceIfEmpty(MapChannel channel)
        {
            if (channel?.IsPrivateInstance == true && channel.ClientList.Count == 0 && channel.QueuedClients.Count == 0 &&
                !_instancesToDestroy.Contains(channel))
                _instancesToDestroy.Add(channel);
        }

        /// <summary>
        /// Destroys the queued instances that are still empty: their creatures (with their corpses'
        /// loot) and objects leave the entity tables and free their ids, and the channel is dropped.
        /// Runs after the map worker, never while it iterates.
        /// </summary>
        public void DestroyQueuedInstances()
        {
            if (_instancesToDestroy.Count == 0)
                return;

            foreach (var channel in _instancesToDestroy.ToList())
            {
                if (channel.ClientList.Count > 0 || channel.QueuedClients.Count > 0)
                    continue;

                foreach (var cell in channel.MapCellInfo.Cells.Values)
                    foreach (var creature in cell.CreatureList.ToList())
                        CellManager.Instance.RemoveCreatureFromWorld(channel, creature);

                foreach (var dynamicObject in channel.DynamicObjects.ToList())
                {
                    EntityManager.Instance.UnregisterEntity(dynamicObject.EntityId);
                    EntityManager.Instance.UnregisterDynamicObject(dynamicObject.EntityId);
                    EntityManager.Instance.FreeEntity(dynamicObject.EntityId);
                }

                foreach (var loot in channel.LootDispensers.Keys.ToList())
                    EntityManager.Instance.FreeEntity(loot);

                channel.DynamicObjects.Clear();
                channel.ContentUsables.Clear();
                channel.LootDispensers.Clear();
                channel.MapCellInfo.Cells.Clear();

                InstanceChannels.Remove(channel.InstanceId);
                if (channel.OwnerCharacterId is uint owner &&
                    _characterInstances.TryGetValue(owner, out var current) && ReferenceEquals(current, channel))
                    _characterInstances.Remove(owner);

                Logger.WriteLog(LogType.Debug, $"Destroyed instance {channel.InstanceId} of context {channel.MapInfo?.MapContextId}");
            }

            _instancesToDestroy.Clear();
        }

        #endregion

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
            Timer.Add("ClientEffectUpdate", 500, true, null);
            Timer.Add("CellUpdateVisibility", 1000, true, null);
            Timer.Add("CheckForCreatures", 1000, true, null);
            Timer.Add("CheckForMapTriggers", 1000, true, null);
        }

        public void MapChannelWorker(long delta)
        {
            Timer.Update(delta);

            PartyManager.Instance.ExpireHeldMembers();

            // Server-wide lists, ticked once. Dropships used to run inside the per-map loop
            // below, guarded by that map having players, so with N populated maps every
            // dropship advanced N times per tick.
            DynamicObjectManager.Instance.DropshipsWorker(delta);

            foreach (var mapChannel in Channels())
            {
                mapChannel.MapChannelElapsed += delta;

                if (Timer.IsTriggered("CheckForLogingClients"))
                    if (mapChannel.QueuedClients.Count > 0)
                    {
                        // create new mapClient
                        var dequedClient = mapChannel.QueuedClients.Dequeue();

                        // add it to list, unless its socket closed while it was queued
                        if (dequedClient.State != ClientState.Disconnected && !dequedClient.Player.Disconected)
                            mapChannel.ClientList.Add(dequedClient);
                    }

                if (mapChannel.ClientList.Count > 0)
                {
                    ActorActionManager.Instance.DoWork(mapChannel, delta);
                    MissileManager.Instance.DoWork(mapChannel, delta);
                    BehaviorManager.Instance.MapChannelThink(mapChannel, delta);

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
                    {
                        MapTriggerManager.Instance.TriggersProximityWorker(mapChannel);

                        // zone borders and instance doors: anyone standing in one leaves the map
                        MapLinkManager.Instance.Worker(mapChannel);

                        // hospitals gained by coming near them
                        PlayerDeathManager.Instance.DiscoverHospitals(mapChannel);

                        // kill streaks whose window has passed
                        KillRewardManager.Instance.ExpireStreaks(mapChannel);

                        // content creatures whose placement asked for a respawn
                        WorkContentRespawns(mapChannel);

                        // ambient/music/sky/minimap regions: tell whoever changed region
                        RegionManager.Instance.Worker(mapChannel);
                    }

                    // check for effects (buffs)
                    if (Timer.IsTriggered("ClientEffectUpdate"))
                        GameEffectManager.Instance.DoWork(mapChannel, delta);

                    // Area-bound mission objectives (no work in contexts without live content areas).
                    MissionContentManager.Instance.DoWork(mapChannel);

                    // Then destroyed placements restore and armed bombs detonate (build plan 1.6 tick order).
                    MissionContentManager.Instance.RestoreDestroyedUsables(mapChannel);
                    MissionContentManager.Instance.DetonateFuses(mapChannel);

                    // Objective timers run out after this tick's use recoveries (ActorActionManager above) and detonations.
                    MissionManager.Instance.ExpireObjectiveTimers(mapChannel);

                    // warn idle players and flag long-idle ones for removal below
                    ManifestationManager.Instance.CheckInactivity(mapChannel);

                    // check for players leaving the map: /logout, inactivity, and dropped
                    // connections flagged by Client.Close()
                    foreach (var client in mapChannel.ClientList)
                        if (client != null && client.Player.RemoveFromMap)
                        {
                            // The MainLoop thread has no handler of its own, so an exception
                            // escaping here stops the whole server ticking. Clear the flag
                            // first and drop the entry on failure so a bad removal is logged
                            // once instead of retried - and thrown - on every tick.
                            client.Player.RemoveFromMap = false;

                            try
                            {
                                RemovePlayer(client, true);
                            }
                            catch (Exception e)
                            {
                                Logger.WriteLog(LogType.Error, $"Failed to remove {client?.Player?.FamilyName} from map {mapChannel?.MapInfo?.MapContextId}: {e}");
                                mapChannel.ClientList.Remove(client);
                            }

                            break;
                        }
                }
            }
            // The autofire list is global: advance it once per elapsed interval,
            // independent of how many maps currently contain players.
            ManifestationManager.Instance.AutoFireTimerDoWork(delta);

            // Instances their owners left during the tick go now, outside the channel loop.
            DestroyQueuedInstances();
        }

        public void MapLoaded(Client client)
        {
            if (client.State == ClientState.Teleporting)
            {
                var dropship = new Dropship(Factions.AFS, DropshipType.Teleporter, client);
                var mapChannel = ChannelForEntry(client.Player.Id, client.LoadingMap);

                client.Player.MapChannel = mapChannel;
                client.Player.MapContextId = dropship.Client.LoadingMap;

                mapChannel.ClientList.Add(client);

                CellManager.Instance.AddToWorld(client.Player.MapChannel, dropship);
                DynamicObjectManager.Instance.Dropships.Add(dropship.EntityId, dropship);
                CommunicatorManager.Instance.LoginOk(dropship.Client);
                ServerFlagManager.Instance.SendFlags(client);

                // The manifestation, its items and its entity registrations survive the map
                // change; the client's picture of them does not. Show it what the server
                // already has rather than loading and registering it all a second time, and
                // recompute the stats without the full reset that healed the player.
                InventoryManager.Instance.ResendToClient(client);
                ManifestationManager.Instance.UpdateStatsValues(client, false);

                CellManager.Instance.AddToWorld(dropship.Client); // will introduce the player to all clients, including the current owner
                MapLinkManager.Instance.PlayerEnteredMap(client);
                PlayerDeathManager.Instance.OnPlayerEnteredMap(client);
                CellManager.Instance.CellCallMethod(dropship.Client.Player.MapChannel, dropship.Client.Player, new TeleportArrivalPacket());
                client.CallMethod(SysEntity.ClientMethodId, new RequestMovementBlockPacket());
                ManifestationManager.Instance.AssignPlayer(client);
                CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Position);
                CommunicatorManager.Instance.PlayerEnterMap(dropship.Client);

                return;
            }

            client.State = ClientState.Ingame;
            ManifestationManager.Instance.ResetInactivity(client);
            InventoryManager.Instance.InitForClient(client);
            ManifestationManager.Instance.UpdateStatsValues(client, true);

            // register new Player
            EntityManager.Instance.RegisterEntity(client.Player.EntityId, EntityType.Character);
            EntityManager.Instance.RegisterPlayer(client.Player.EntityId, client.Player);
            EntityManager.Instance.RegisterActor(client.Player.EntityId, client.Player);
            CommunicatorManager.Instance.LoginOk(client);

            // Before anything the player can act on: the client asks its own flag set whether to
            // offer a feature, and an empty set means the feature is simply missing.
            ServerFlagManager.Instance.SendFlags(client);

            // Whatever is broken about the map they have just walked into, if they are someone
            // who can do anything about it. The dialog is modal and always visible, so a player
            // would be stuck reading about server data they cannot fix.
            if (client.AccountEntry != null && client.AccountEntry.Level >= (byte)GmLevel.Observer)
                MapErrorManager.Instance.SendTo(client);

            CellManager.Instance.AddToWorld(client); // will introduce the player to all clients, including the current owner

            // Before the first link check: a player who arrives through a pass is standing in
            // the gate on this side, and must walk out of it before it can send them back.
            MapLinkManager.Instance.PlayerEnteredMap(client);
            ManifestationManager.Instance.AssignPlayer(client);
            MissionManager.Instance.SendMissionStatusInfo(client);
            MissionContentManager.Instance.OnPlayerEnteredMap(client);
            PlayerDeathManager.Instance.OnPlayerEnteredMap(client);

            ClanManager.Instance.InitializePlayerClanData(client);
            InventoryManager.Instance.InitClanInventory(client);
            CommunicatorManager.Instance.PlayerEnterMap(client);
            PartyManager.Instance.PlayerEnteredWorld(client);
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
                   mapInstance.InstanceId,
                   mapInstance.MapInfo.MapVersion,
                    client.Player.Position,
                   (float)client.Player.Rotation
               ));

            client.State = ClientState.Loading;
            client.State = ClientState.Loading;
            client.Player.MapChannel.QueuedClients.Enqueue(client);
        }

        /// <summary>
        /// Moves an ingame player to a position on any loaded map by way of the loading screen:
        /// out of the current map channel, then Wonkavate into the new one. Summon and .teleport
        /// each had a copy of this that forgot to point the player at the new map, so when the
        /// client answered with MapLoaded it was added to the OLD map's cells at its OLD position.
        /// Everyone there saw a frozen ghost, its broadcasts went to the wrong map, and the first
        /// cell crossing on the new map indexed the old map's cell table with new-map seeds and
        /// threw KeyNotFoundException on the main loop.
        /// </summary>
        /// <returns>false when the map is not loaded or the player is not in a state to move.</returns>
        public bool ChangeMap(Client client, uint mapContextId, Vector3 position, float orientation)
        {
            if (client.Player == null || client.State != ClientState.Ingame)
                return false;

            if (!MapChannelArray.ContainsKey(mapContextId))
                return false;

            client.CallMethod(SysEntity.ClientMethodId, new PreWonkavatePacket());
            client.State = ClientState.Loading;

            // Out of the old map while the player still points at it: entities, cells, the
            // managers that track it, and the old ClientList.
            DetachFromMap(client);

            // The shared channel, or a fresh private instance for a per-character context.
            var mapChannel = ChannelForEntry(client.Player.Id, mapContextId);

            // What MapLoaded reads back when the client is ready: the map channel it adds the
            // player to, and the position the cell matrix is built from.
            client.Player.MapChannel = mapChannel;
            client.Player.MapContextId = mapContextId;
            client.Player.Position = position;
            client.Player.Rotation = orientation;
            client.LoadingMap = mapContextId;

            var packet = new WonkavatePacket(
                mapChannel.MapInfo.MapContextId,
                mapChannel.InstanceId,
                mapChannel.MapInfo.MapVersion,
                position,
                orientation);

            client.CallMethod(SysEntity.CurrentInputStateId, packet);
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Position, packet);
            mapChannel.ClientList.Add(client);

            return true;
        }

        public void Ping(Client client, double ping)
        {
            client.CallMethod(SysEntity.ClientMethodId, new AckPingPacket(ping));
        }

        /// <summary>
        /// Takes the character out of the world for good: logout, socket loss, inactivity. Saves
        /// once, and is a no-op for a character already removed, so a normal logout that meets a
        /// socket loss (or a second enqueue) cannot save or unregister twice.
        /// </summary>
        public void RemovePlayer(Client client, bool logout)
        {
            var player = client.Player;
            if (player == null || player.Disconected)
                return;

            client.SaveCharacter();

            DetachFromMap(client);

            player.Disconected = true;
            player.RemoveFromMap = true;
            player.LogoutCountdown.Cancel();
            player.CurrentAbility = null;
            player.CurrentWeaponAction = null;
            player.CurrentWeaponAttack = null;

            if (logout && client.State != ClientState.Disconnected)
                PassClientToCharacterSelection(client);
        }

        /// <summary>
        /// Everything the current map and the managers hold for this character, without ending
        /// its session: shared by RemovePlayer and ChangeMap, which carries the same character on
        /// to another map and must not leave it flagged as disconnected.
        /// </summary>
        private void DetachFromMap(Client client)
        {
            var player = client.Player;

            // A target is an entity on this map; the client does not always re-target after a
            // map change, and MissileLaunch refuses cross-map targets, so drop it here.
            player.Target = 0;

            // A socket can close before MapLoaded registered the character; nothing below that
            // speaks for a registration may then run against whoever holds the entity id.
            var registered = EntityManager.Instance.Players.TryGetValue(player.EntityId, out var entry) && entry == player;

            // unregister Communicator
            if (registered)
                CommunicatorManager.Instance.PlayerExitMap(client);
            // unregister mapChannelClient
            EntityManager.Instance.UnregisterEntity(player.EntityId);
            EntityManager.Instance.UnregisterPlayer(player.EntityId);
            EntityManager.Instance.UnregisterActor(player.EntityId);

            // unregister character Inventory; an item in two lists is destroyed once
            var inventoryIds = new HashSet<ulong>(player.Inventory.EquippedInventory);
            inventoryIds.UnionWith(player.Inventory.HomeInventory);
            inventoryIds.UnionWith(player.Inventory.PersonalInventory);
            inventoryIds.UnionWith(player.Inventory.WeaponDrawer);
            foreach (var entityId in inventoryIds)
                if (entityId != 0)
                    EntityManager.Instance.DestroyPhysicalEntity(client, entityId, EntityType.Item);

            NpcManager.Instance.DiscardBuybackItems(client);
            ActorActionManager.Instance.RemoveActor(player);

            if (registered)
                ManifestationManager.Instance.StopAutoFire(client);
            CellManager.Instance.RemoveFromWorld(client);
            MapLinkManager.Instance.RemovePlayer(client);
            RegionManager.Instance.RemovePlayer(client);
            ManifestationManager.Instance.RemovePlayerCharacter(client);
            ClanManager.Instance.RemovePlayer(client);
            LookingForGroupManager.Instance.RemovePlayer(client);
            SummonManager.Instance.RemovePlayer(client);
            TradeManager.Instance.RemovePlayer(client);
            PartyManager.Instance.RemovePlayer(client);
            PetitionManager.Instance.RemovePlayer(client);

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

                // A private instance lives only while its owner is in it.
                ReleaseInstanceIfEmpty(map);
            }
        }

        public void RequestLogout(Client client)
        {
            if (client.State == ClientState.Disconnected || client.Player.RemoveFromMap)
                return;

            var remaining = client.Player.LogoutCountdown.Begin(_getMonotonicMilliseconds());
            client.CallMethod(SysEntity.ClientMethodId, new LogoutTimeRemainingPacket(remaining));
        }

        public MapInstance GetMapInstance(uint mapContextId)
        {
            // TODO support additional maps
            var map = new MapInstance(new MapInfo(1220, "adv_foreas_concordia_wilderness", 1556, 0));

            return map;
        }
        /// <summary>
        /// Brings back content creatures whose placement asked for a respawn. The placement's respawn_ms is the
        /// game's own field; the creature is materialized again at its placement position, and only when nothing of
        /// that placement is alive.
        /// </summary>
        private static void WorkContentRespawns(MapChannel mapChannel)
        {
            if (mapChannel.ContentRespawns.Count == 0)
                return;

            var now = Environment.TickCount64;
            List<uint> due = null;
            foreach (var pair in mapChannel.ContentRespawns)
            {
                if (pair.Value > now)
                    continue;
                (due ??= new List<uint>()).Add(pair.Key);
            }

            if (due == null)
                return;

            var content = MissionManager.Instance.Content?.Content;
            foreach (var placementId in due)
            {
                mapChannel.ContentRespawns.Remove(placementId);

                if (content == null || !content.Catalog.Placements.TryGetValue(placementId, out var placement))
                    continue;

                var alive = mapChannel.MapCellInfo.Cells.Values
                    .SelectMany(cell => cell.CreatureList)
                    .Any(creature => creature.ContentPlacementId == placementId && creature.State != CharacterState.Dead);
                if (alive)
                    continue;

                ContentMaterializer.Respawn(mapChannel, placement);
            }
        }

    }
}
