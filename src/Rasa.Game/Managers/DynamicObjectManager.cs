using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Extensions;
    using Game;
    using Packets;
    using Packets.ClientMethod.Server;
    using Packets.Game.Server;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Packets.Protocol;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;
    using System;

    public class DynamicObjectManager
    {
        private static DynamicObjectManager _instance;
        private static readonly object InstanceLock = new object();
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;

        public readonly Dictionary<ulong, Dropship> Dropships = new Dictionary<ulong, Dropship>();
        public readonly Dictionary<ulong, DynamicObject> Teleporters = new Dictionary<ulong, DynamicObject>();
        public static DynamicObjectManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new DynamicObjectManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        private DynamicObjectManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
        }

        internal void InitDynamicObjects()
        {
            InitControlPoints();
            InitFootlockers();
            InitTeleporters();
            LogosManager.Instance.LogosInit();
            KraftwerksManager.Instance.KraftwerksInit();
        }

        internal void ForceState(DynamicObject obj, UseObjectState state, int delta)
        {
            CellManager.Instance.CellCallMethod(obj, new ForceStatePacket(state, delta));
        }

        internal void RequestUseObjectPacket(Client client, RequestUseObjectPacket packet)
        {
            // The client can name an entity that was removed after it was introduced.
            if (!EntityManager.Instance.TryGetObject(packet.EntityId, out var obj))
            {
                Logger.WriteLog(LogType.Debug, $"RequestUseObjectPacket: unknown entity {packet.EntityId}");
                return;
            }

            // Entity ids are global; an object in another channel (another instance of the same
            // context, or another map) is not the player's to use.
            if (client.Player?.MapChannel != null && !MapChannelManager.IsOnChannel(obj, client.Player.MapChannel))
            {
                Logger.WriteLog(LogType.Security, $"RequestUseObjectPacket: {client.Player.Name} named object {packet.EntityId} outside their channel");
                return;
            }

            switch (obj.DynamicObjectType)
            {
                case DynamicObjectType.ControlPoint:
                    {
                        client.CallMethod(client.Player.EntityId, new PerformWindupPacket(PerformType.TwoArgs, packet.ActionId, packet.ActionArgId));
                        client.CallMethod(packet.EntityId, new UsePacket(client.Player.EntityId, obj.StateId, 10000));
                        client.Player.MapChannel.PerformRecovery.Add(new ActionData(client.Player, packet.ActionId, packet.ActionArgId, 10000) { SourceId = obj.EntityId });

                        obj.TriggeredByPlayers.Add(client);
                        break;
                    }
                // Both containers are used the same way. The client's own state machines say so:
                // usableaugmentationstatetransition[66 LOCKBOX] is [(3, 3, 163)] and [76
                // CLANLOCKBOX] is [(10000010, 10000010, 163)] - one self-transition each, playing
                // the same animation 163, with no second state to move to. What differs is the
                // state each announces (see LockboxStateFor) and the window the client opens off
                // its own augmentation, neither of which is decided here.
                case DynamicObjectType.Lockbox:
                case DynamicObjectType.ClanLockbox:
                    {
                        client.CallMethod(client.Player.EntityId, new PerformWindupPacket(PerformType.TwoArgs, packet.ActionId, packet.ActionArgId));
                        client.CallMethod(packet.EntityId, new UsePacket(client.Player.EntityId, obj.StateId, 100));
                        client.Player.MapChannel.PerformRecovery.Add(new ActionData(client.Player, packet.ActionId, packet.ActionArgId, 100) { SourceId = obj.EntityId });

                        obj.TriggeredByPlayers.Add(client);
                        break;
                    }
                case DynamicObjectType.Logos:
                    {
                        var actionData = new ActionData(client.Player, packet.ActionId, packet.ActionArgId, 10000);
                        actionData.SourceId = obj.EntityId;

                        client.CallMethod(client.Player.EntityId, new PerformWindupPacket(PerformType.TwoArgs, packet.ActionId, packet.ActionArgId));
                        client.CallMethod(packet.EntityId, new UsePacket(client.Player.EntityId, obj.StateId, 10000));
                        client.Player.MapChannel.PerformRecovery.Add(actionData);

                        obj.TriggeredByPlayers.Add(client);
                        LockForUse(obj, client.Player.EntityId);
                        break;
                    }
                case DynamicObjectType.ContentUsable:
                    // Through the installed content manager (tests substitute their own).
                    MissionManager.Instance.Content.RequestUseContentUsable(client, packet, obj);
                    break;
                case DynamicObjectType.Kraftwerks:
                    KraftwerksManager.Instance.Use(client, obj, packet.ActionArgId);
                    break;
                default:
                    Logger.WriteLog(LogType.Debug, $"ToDo: RequestUseObjectPacket: unsuported object type {obj.DynamicObjectType}");
                    break;
            }
        }

        /// <summary>
        /// SetUsable (203): toggles the client-side usable state. With missionActivated set,
        /// enabling attaches and disabling detaches the MISSION_USABLE_INDICATOR glow
        /// (client/augmentations/usable.pyo Recv_SetUsable / _SetEnabled).
        /// </summary>
        internal void SetUsable(Client client, ulong entityId, bool enabled, DynamicObject obj)
        {
            client.CallMethod(entityId, new SetUsablePacket(enabled));
        }

        /// <summary>
        /// A Logos shrine is locked to the player drawing from it and plays its channelling effect: the client's
        /// usabledata.specialFX has an interruptible package for the shrine classes (state 81), and
        /// Recv_UseInterruptible only plays it for the actor Recv_LockToActor named. The order is the client's:
        /// ClanControlPoint.OnBeforeUseInterruptible shows UseInterruptible starting the use, and usable.py's gate
        /// shows the lock has to come first. It is released on completion (UnlockAfterUse) or interruption
        /// (CancelPendingUse). No capture records the original server's order; this one is inferred
        /// (GAP-USABLE-ACTOR-LOCK). Control points are left out - their client handler takes a clan id as well.
        /// </summary>
        internal static void LockForUse(DynamicObject obj, ulong actorId)
        {
            if (obj is not Logos || MapChannelManager.ChannelOf(obj)?.MapCellInfo == null)
                return;
            CellManager.Instance.CellCallMethod(obj, new LockToActorPacket(actorId));
            CellManager.Instance.CellCallMethod(obj, new UseInterruptiblePacket(actorId));
        }

        internal static void UnlockAfterUse(DynamicObject obj, ulong actorId, bool interrupted)
        {
            if (obj is not Logos || MapChannelManager.ChannelOf(obj)?.MapCellInfo == null)
                return;
            if (interrupted)
                CellManager.Instance.CellCallMethod(obj, new UseInterruptedPacket(actorId));
            CellManager.Instance.CellCallMethod(obj, new LockToActorPacket(0));
        }

        internal static void CancelPendingUse(MapChannel mapChannel, ActionData action)
        {
            if (action.ActionId != ActionId.UseObject)
                return;
            MissionManager.Instance.Content.CancelContentUsableUse(action);
            void RemoveTrigger(DynamicObject obj)
            {
                if (action.SourceId == 0 || action.SourceId == obj.EntityId)
                    if (obj.TriggeredByPlayers.RemoveAll(client => client?.Player == action.Actor) > 0)
                        UnlockAfterUse(obj, action.Actor.EntityId, interrupted: true);
            }
            foreach (var obj in mapChannel.DynamicObjects)
                RemoveTrigger(obj);
            foreach (var obj in mapChannel.ControlPoints.Values)
                RemoveTrigger(obj);
            foreach (var obj in mapChannel.FootLockers.Values)
                RemoveTrigger(obj);
        }

        internal void DynamicObjectWorker(MapChannel mapChannel, long delta)
        {
            // dynamicObjects
            // dropShips
            // etc...

            // controlPoints
            foreach (var entry in mapChannel.ControlPoints)
            {
                var controlPoint = entry.Value;
                // spawn object
                if (!controlPoint.IsInWorld)
                {
                    controlPoint.RespawnTime -= delta;

                    if (controlPoint.RespawnTime <= 0)
                    {
                        CellManager.Instance.AddToWorld(mapChannel, controlPoint);
                        controlPoint.IsInWorld = true;
                        controlPoint.StateId = UseObjectState.CpointStateUnclaimed;
                        controlPoint.WindupTime = 10000;
                    }
                }

                // check for players neer object
                DynamicObjectProximityWorker(mapChannel, controlPoint, delta);
            }

            // footlocker
            foreach (var entry in mapChannel.FootLockers)
            {
                var footlocker = entry.Value;
                // spawn object
                if (!footlocker.IsInWorld)
                {
                    footlocker.RespawnTime -= delta;

                    if (footlocker.RespawnTime <= 0)
                    {
                        // The state goes on before the object goes out: AddToWorld introduces it
                        // to every client already in the cell, and the UsableInfoPacket that
                        // introduction carries is built from the StateId the object holds at that
                        // moment. Set afterwards, the first clients to see it were told 0.
                        footlocker.StateId = LockboxStateFor(footlocker.EntityClassId);
                        footlocker.WindupTime = 10000;
                        CellManager.Instance.AddToWorld(mapChannel, footlocker);
                        footlocker.IsInWorld = true;
                    }
                }
            }

            // crafting stations
            KraftwerksManager.Instance.Worker(mapChannel);

            // teleporters
            foreach (var entry in mapChannel.Teleporters)
            {
                var teleporter = entry.Value;
                // spawn object
                if (!teleporter.IsInWorld)
                {
                    teleporter.RespawnTime -= delta;

                    if (teleporter.RespawnTime <= 0)
                    {
                        CellManager.Instance.AddToWorld(mapChannel, teleporter);
                        teleporter.IsInWorld = true;
                        teleporter.StateId = UseObjectState.TsState1;
                    }
                }

                // check for players neer object
                DynamicObjectProximityWorker(mapChannel, teleporter, delta);
            }

            // dynamicObjects
            foreach (var dynamicObject in mapChannel.DynamicObjects)
            {
                // spawn object
                if (!dynamicObject.IsInWorld)
                {
                    dynamicObject.RespawnTime -= delta;

                    if (dynamicObject.RespawnTime <= 0)
                    {
                        CellManager.Instance.AddToWorld(mapChannel, dynamicObject);
                        dynamicObject.IsInWorld = true;
                        dynamicObject.StateId = UseObjectState.IdStateActive;
                        dynamicObject.WindupTime = 10000;
                    }
                }
            }
        }

        internal void DynamicObjectProximityWorker(MapChannel mapChannel, DynamicObject obj, long delta)
        {
            switch (obj.DynamicObjectType)
            {
                // teleporters. A local teleporter pad is gained and used by walking onto it, the way a map
                // waypoint is: the client has a separate type for it (constant/waypointtype LOCALWAYPOINT 1), its
                // own gain line (manifestation.Recv_WaypointGained posts PM_GAINED_WAYPOINT for type 1) and its
                // own window (clientmethod.Recv_EnteredWaypoint takes the type). It used to fall through to the
                // default here, so none of the 42 type-1 pads could ever be gained or used.
                case DynamicObjectType.Waypoint:
                case DynamicObjectType.LocalTeleporter:
                case DynamicObjectType.Wormhole:
                case DynamicObjectType.DropshipTeleporter:
                    {
                        // check for players that enter range
                        PlayerEnterWaypoint(obj);

                        // check for players that leave range
                        PlayerExitWaypoint(obj);

                        break;
                    }
                // Control point
                case DynamicObjectType.ControlPoint:
                default:
                    break;
            }
        }

        // 1 object to n client's
        internal void CellIntroduceDynamicObjectToClients(DynamicObject dynamicObject, List<Client> listOfClients)
        {
            foreach (var client in listOfClients)
                CreateDynamicObjectOnClient(client, dynamicObject);
        }

        // n objects to 1 client
        internal void CellIntroduceDynamicObjectsToClient(Client client, List<DynamicObject> listOfObjects)
        {
            foreach (var dynamicObject in listOfObjects)
                CreateDynamicObjectOnClient(client, dynamicObject);
        }

        internal void CreateDynamicObjectOnClient(Client client, DynamicObject dynamicObject)
        {
            if (dynamicObject == null)
                return;

            if (dynamicObject.EntityClassId == 0)
                return;
				
            var classInfo = EntityClassManager.Instance.GetClassInfo(EntityManager.Instance.GetEntityClassId(dynamicObject.EntityId));

            if (classInfo == null)
                return;

            var entityData = new List<PythonPacket>
            {
                // PhysicalEntity
                new IsTargetablePacket(classInfo.TargetFlag),
                // As for creatures: no category, no target. The boot camp's practice dummy carries the
                // inertdestroyable augmentation, which has its own Recv_TargetCategory, and "Shoot the
                // Practice Dummy" cannot be done until it has one. An object is Object, not a faction -
                // which the old Factions-typed packet could not say at all.
                new TargetCategoryPacket(TargetCategory.Object),
                new WorldLocationDescriptorPacket(dynamicObject.Position, dynamicObject.Rotation),
                // set state
                new UsableInfoPacket(dynamicObject.IsEnabled, dynamicObject.StateId, 0, dynamicObject.WindupTime, dynamicObject.ActivateMission)
        };

            // A usable the client does not believe can be damaged cannot be targeted by a weapon. Its
            // canBeDamaged starts from the client's own usabledata row for the class, and for the boot
            // camp's practice dummy (29365) that row is all None - the server is expected to say. Under
            // a weapon's Hostile target type, targeting._IsTargetType only lets a non-hostile entity
            // through when IsDirectTargetable(), which requires canBeDamaged; without it the crosshair
            // never locks, SetTargetId is never sent, and every shot lands on entity 0 (live trace
            // 2026-09-19, after the target category was already being sent). DamageInfoPacket existed and
            // was never used.
            if (dynamicObject.MaxHitPoints > 0)
                entityData.Add(new DamageInfoPacket(true, false, dynamicObject.MaxHitPoints, dynamicObject.HitPoints));

            client.CallMethod(SysEntity.ClientMethodId, new CreatePhysicalEntityPacket(dynamicObject.EntityId, dynamicObject.EntityClassId, entityData));
        }

        internal void CellDiscardDynamicObjectToClients(ulong entityId, List<Client> clients)
        {
            if (entityId == 0)
                return;

            foreach (var client in clients)
                EntityManager.Instance.DestroyPhysicalEntity(client, entityId, EntityType.Object);
        }

        internal void CellDiscardDynamicObjectsToClient(Client client, List<DynamicObject> discardObjects)
        {
            foreach (var dynamicObject in discardObjects)
                client.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(dynamicObject.EntityId));
        }

        /* Destroys an object on client and serverside
         * Frees the memory and informs clients about removal
         */
        internal void DynamicObjectDestroy(MapChannel mapChannel, DynamicObject dynObject)
        {
            // TODO, check timers
            // remove from world
            EntityManager.Instance.UnregisterEntity(dynObject.EntityId);
            CellManager.Instance.RemoveFromWorld(mapChannel, dynObject);

            // destroy callback
            Logger.WriteLog(LogType.Debug, "ToDO remove dynamic object from server");
        }

        #region ControlPoint

        internal void InitControlPoints()
        {
            // An emulator-authored PvE control point (class 3814 UsableControlPointElohV01, augmentation 57) in the
            // Wilderness. Neither its position nor its id 215 is in the client map or the client's controlpointdata
            // table (GAP-W3-PVE-CONTROL-POINT-PLACEMENT); it is kept as it was, and its status tuple is not sent to
            // anyone. The evidenced PvP points are ControlPointManager's.
            var mapChannel = MapChannelManager.Instance.FindByContextId(1220);

            var newControlPoint = new DynamicObject
            {
                Position = new Vector3(197.66f, 162.27f, -54.08f),
                Rotation = 3.05f,
                MapContextId = 1220,
                EntityClassId = (EntityClasses)3814,
                DynamicObjectType = DynamicObjectType.ControlPoint,
                ObjectData = new ControlPointStatus(215, 1, ControlPointState.PreWar, 30000)
            };

            newControlPoint.DynamicObjectType = DynamicObjectType.ControlPoint;

            mapChannel.ControlPoints.Add(1, newControlPoint);
        }

        internal void CaptureControlPointRecovery(MapChannel mapChannel, ActionData action)
        {
            foreach (var entry in mapChannel.ControlPoints)
            {
                var controlpoint = entry.Value;

                foreach (var client in controlpoint.TriggeredByPlayers)
                    if (client.Player == action.Actor)
                    {
                        if (action.IsInrerrupted)
                        {
                            Logger.WriteLog(LogType.Debug, $"Action is interupted");
                            controlpoint.TriggeredByPlayers.Remove(client);
                            break;
                        }

                        Logger.WriteLog(LogType.Debug, $"Action Exicuted");
                        controlpoint.TriggeredByPlayers.Remove(client);
                        controlpoint.Faction = controlpoint.Faction == Factions.AFS ? Factions.Bane : Factions.AFS;
                        controlpoint.StateId = controlpoint.StateId == UseObjectState.CpointStateFactionAOwned ? UseObjectState.CpointStateFactionBOwned : UseObjectState.CpointStateFactionAOwned;

                        CellManager.Instance.CellCallMethod(controlpoint, new ForceStatePacket(controlpoint.StateId, 100));
                        CellManager.Instance.CellCallMethod(controlpoint, new UsableInfoPacket(true, controlpoint.StateId, 0, 10000, 0));
                        break;
                    }
            }
        }

        #endregion

        #region Dropship
        public void DropshipsWorker(long timePassed)
        {
            // Dropships is server-wide; each one is removed from its own map, not from
            // whichever map the caller happened to be iterating.
            foreach (var entry in Dropships)
            {
                var dropship = entry.Value;

                if (dropship.DropshipType != DropshipType.Spawner && dropship.DropshipType != DropshipType.Teleporter)
                {
                    Logger.WriteLog(LogType.Debug, $"error dropshiptype {dropship.DropshipType}");
                    return;
                }

                dropship.PhaseTimeleft -= timePassed;

                if (dropship.PhaseTimeleft > 0)
                    continue;

                if (dropship.Phase == 0 || dropship.Phase == 1 || dropship.Phase == 4)
                    CellManager.Instance.CellCallMethod(dropship, new ForceStatePacket(dropship.StateId, 0));

                switch (dropship.Phase)
                {
                    case 0:
                        dropship.Phase = 1;
                        dropship.StateId = UseObjectState.CsStateSpawn;
                        break;
                    case 1:
                        dropship.Phase = 2;
                        dropship.PhaseTimeleft = 2000;
                        break;
                    case 2:
                        dropship.Phase = 3;

                        if (dropship.DropshipType == DropshipType.Teleporter)
                        {
                            if (dropship.Client.State == ClientState.Ingame)
                            {
                                CellManager.Instance.CellCallMethod(dropship.Client.Player.MapChannel, dropship.Client.Player, new PreTeleportPacket(TeleportType.Default));
                                dropship.Client.CallMethod(SysEntity.ClientMethodId, new BeginTeleportPacket());
                            }
                        }

                        if (dropship.DropshipType == DropshipType.Spawner)
                        {
                            // create list of creatures to spawn
                            var creatureList = SpawnPoolManager.Instance.CreateListOfCreatures(dropship.SpawnPool);

                            // spawn creatures
                            SpawnPoolManager.Instance.SpawnCreatures(dropship.SpawnPool, creatureList);
                            SpawnPoolManager.Instance.DecreaseQueuedCreatureCount(dropship.SpawnPool, dropship.SpawnPool.QueuedCreatures);
                        }

                        break;
                    case 3:
                        dropship.PhaseTimeleft = 3000;
                        dropship.Phase = 4;
                        dropship.StateId = UseObjectState.CsStateEnd;
                        break;
                    case 4:
                        dropship.Phase = 5;
                        dropship.PhaseTimeleft = 5000;

                        if (dropship.DropshipType == DropshipType.Teleporter)
                            if (dropship.Client.State == ClientState.Teleporting)
                                dropship.Client.CallMethod(SysEntity.ClientMethodId, new UnrequestMovementBlockPacket());
                        break;
                    case 5:
                        if (dropship.DropshipType == DropshipType.Teleporter)
                        {
                            switch (dropship.Client.State)
                            {
                                case ClientState.Ingame:
                                    CellManager.Instance.RemoveFromWorld(dropship.Client);
                                    dropship.Client.Player.MapChannel.ClientList.Remove(dropship.Client);
                                    CommunicatorManager.Instance.LeaveMapChannels(dropship.Client);
                                    dropship.Client.CallMethod(SysEntity.ClientMethodId, new UnrequestMovementBlockPacket());
                                    dropship.Client.CallMethod(SysEntity.ClientMethodId, new PreWonkavatePacket());
                                    dropship.Client.CallMethod(SysEntity.CurrentInputStateId, new WonkavatePacket(dropship.DestinationMapId, 1, MapChannelManager.Instance.MapChannelArray[dropship.DestinationMapId].MapInfo.MapVersion, dropship.Destination, 0));
                                    dropship.Client.AwaitingMapLoaded = true;
                                    dropship.Client.Player.Position = dropship.Destination;
                                    dropship.Client.Player.Target = 0;
                                    dropship.Client.State = ClientState.Teleporting;
                                    break;
                                case ClientState.Teleporting:
                                    dropship.Client.State = ClientState.Ingame;
                                    ManifestationManager.Instance.ResetInactivity(dropship.Client);
                                    break;
                                default:
                                    Logger.WriteLog(LogType.Error, $"Unsupported CLientState {dropship.Client.State}");
                                    break;
                            }
                        }

                        if (dropship.DropshipType == DropshipType.Spawner)
                            SpawnPoolManager.Instance.DecreaseQueueCount(dropship.SpawnPool);

                        // remove object, from the channel it was added to (an instance's own, for its spawn pools)
                        var dropshipMap = dropship.MapChannel;
                        if (dropshipMap != null || MapChannelManager.Instance.MapChannelArray.TryGetValue(dropship.MapContextId, out dropshipMap))
                            CellManager.Instance.RemoveFromWorld(dropshipMap, dropship);

                        Dropships.Remove(dropship.EntityId);
                        break;
                    default:
                        Logger.WriteLog(LogType.Error, $"Unsupported phase {dropship.Phase}");
                        break;
                }
            }
        }
        #endregion

        /// <summary>
        /// Gives an instance channel its own copy of every static object its context's primary channel was loaded
        /// with: teleporter pads, lockboxes and Logos shrines (new entity ids, same rows). Control points, crafting
        /// stations and dropship-pad triggers are not copied: no per-squad context has any in the world seed.
        /// </summary>
        internal void CopyStaticObjects(MapChannel primary, MapChannel copy)
        {
            foreach (var (id, teleporter) in primary.Teleporters)
                copy.Teleporters.Add(id, new DynamicObject
                {
                    Position = teleporter.Position,
                    Rotation = teleporter.Rotation,
                    MapContextId = teleporter.MapContextId,
                    EntityClassId = teleporter.EntityClassId,
                    Comment = teleporter.Comment,
                    ObjectData = teleporter.ObjectData,
                    DynamicObjectType = teleporter.DynamicObjectType
                });

            foreach (var (id, footlocker) in primary.FootLockers)
                copy.FootLockers.Add(id, new DynamicObject
                {
                    Position = footlocker.Position,
                    Rotation = footlocker.Rotation,
                    MapContextId = footlocker.MapContextId,
                    EntityClassId = footlocker.EntityClassId,
                    DynamicObjectType = footlocker.DynamicObjectType,
                    StateId = LockboxStateFor(footlocker.EntityClassId),
                    Comment = footlocker.Comment
                });

            foreach (var logos in primary.DynamicObjects.OfType<Logos>())
                copy.DynamicObjects.Add(new Logos(logos));
        }

        /// <summary>Drops the dropships still flying for a channel that is being destroyed.</summary>
        internal void DropChannelDropships(MapChannel channel)
        {
            foreach (var dropship in Dropships.Values.Where(dropship => ReferenceEquals(dropship.MapChannel, channel)).ToList())
            {
                Dropships.Remove(dropship.EntityId);
                EntityManager.Instance.UnregisterEntity(dropship.EntityId);
                EntityManager.Instance.UnregisterDynamicObject(dropship.EntityId);
                EntityManager.Instance.FreeEntity(dropship.EntityId);
            }
        }

        #region Footlocker

        internal void FootlockerRecovery(MapChannel mapChannel, ActionData action)
        {
            Logger.WriteLog(LogType.Debug, $"ToDo: FootlockerRecovery, ActionId = {action.ActionId} ActionArgId = {action.ActionArgId}");
        }

        /// <summary>
        /// The one state a lockbox class is allowed to be in.
        ///
        /// A usable's state machine is built on the client out of the states its augmentation
        /// owns, and the two lockbox augmentations own one each:
        /// usabledata.usableaugmentationstate[66 LOCKBOX] is [3] (USE_LOCKBOX_STATE_0) and
        /// [76 CLANLOCKBOX] is [10000010] (USE_CLANLOCKBOX_STATE_0). Announce anything else and
        /// Usable._SetState looks the id up in self._fsmStates, takes the KeyError and returns:
        /// _curStateId stays None, every later _Transition on the object fails the same way, and
        /// the container never plays a state at all. Every one of the 35 rows in the footlocker
        /// table used to be announced in 181 USE_CPOINT_STATE_UNCLAIMED, which is not a lockbox
        /// state - it belongs to augmentation 57 CONTROLPOINT.
        ///
        /// Which augmentation a row has is the row's own class: entityclass lookup[21030]
        /// (UsableLockBoxHumFootlockerV01) carries [66], lookup[10000063] (UsableClanLockboxV01)
        /// carries [76]. Read from the 1.16.5.0 client's generated/client/usabledata.pyo and
        /// generated/client/entityclass.pyo, decoded 2026-09-13; the footlocker table holds no
        /// other class, and a class we do not know is treated as the personal footlocker it is
        /// filed with.
        /// </summary>
        internal static UseObjectState LockboxStateFor(EntityClasses entityClassId)
        {
            return entityClassId == EntityClasses.UsableClanLockboxV01
                ? UseObjectState.ClanlockboxState0
                : UseObjectState.LockboxState0;
        }

        /// <summary>
        /// The world object for one row of the footlocker table. The row's class says what it is:
        /// 21030 is the AFS issued footlocker, one player's own, and 10000063 is a clan's lockbox
        /// - a different container, with a different window, a different inventory and a state of
        /// its own (see <see cref="LockboxStateFor"/>). Two of the 35 rows are the clan kind, at
        /// Paludos and Twin Pillars, so the kind has to be read off the class rather than off the
        /// table the row arrived in. The client keeps them apart too: uimapmarker has FOOTLOCKER
        /// (17) and CLAN_FOOTLOCKER (21) as separate marker kinds.
        /// </summary>
        internal static DynamicObject CreateFootlocker(Structures.World.FootlockerEntry footlocker)
        {
            var entityClassId = (EntityClasses)footlocker.ClassId;

            return new DynamicObject
            {
                Position = footlocker.Position,
                Rotation = footlocker.Rotation,
                MapContextId = footlocker.MapContextId,
                EntityClassId = entityClassId,
                DynamicObjectType = entityClassId == EntityClasses.UsableClanLockboxV01
                    ? DynamicObjectType.ClanLockbox
                    : DynamicObjectType.Lockbox,
                StateId = LockboxStateFor(entityClassId),
                Comment = footlocker.Comment
            };
        }

        internal void InitFootlockers()
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var footlockers = unitOfWork.Footlockers.GetFootlockers();

            foreach (var footlocker in footlockers)
            {
                var mapChannel = MapChannelManager.Instance.FindByContextId(footlocker.MapContextId);

                mapChannel.FootLockers.Add(footlocker.Id, CreateFootlocker(footlocker));
            }
        }

        #endregion

        #region Logos
        internal void LogosRecovery(MapChannel mapChannel, ActionData action)
        {
            // The player may have more than one pending usable trigger. Complete only the Logos
            // entity that started this action, so another shrine or usable cannot consume it.
            foreach (var obj in mapChannel.DynamicObjects.OfType<Logos>()
                         .Where(logos => logos.EntityId == action.SourceId))
            {
                foreach (var client in obj.TriggeredByPlayers.ToList())
                    if (client?.Player == action.Actor)
                    {
                        if (action.IsInrerrupted)
                        {
                            Logger.WriteLog(LogType.Debug, $"Action is interupted");
                            obj.TriggeredByPlayers.Remove(client);
                            UnlockAfterUse(obj, action.Actor.EntityId, interrupted: true);
                            //CellManager.Instance.CellCallMethod(mapChannel, action.Actor, new PerformWindupPacket(PerformType.TwoArgs, action.ActionId, action.ActionArgId));
                            break;
                        }

                        Logger.WriteLog(LogType.Debug, $"Action Exicuted");
                        obj.TriggeredByPlayers.Remove(client);
                        UnlockAfterUse(obj, action.Actor.EntityId, interrupted: false);
                        CellManager.Instance.CellCallMethod(obj, new UsableInfoPacket(true, obj.StateId, 0, 10000, 0));

                        var logosId = obj.Id;

                        var haveLogos = false;
                        foreach (var logos in client.Player.Logos)
                        {
                            if (logos == logosId)
                            {
                                haveLogos = true;
                                break;
                            }
                        }

                        if (!haveLogos)
                            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Logos, logosId);

                        // Activating the shrine is what mission objectives bound to it wait for (1069's
                        // "Locate the Logos shrine" and the Logos missions). The shrine is a Logos dynamic
                        // object, so the objective binds to the logos row id rather than a placement.
                        MissionManager.Instance.Content.CompleteLogosBoundObjectives(client, logosId);

                        break;
                    }
            }
        }
        #endregion

        #region Waypoint

        internal void InitTeleporters()
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var teleporters = unitOfWork.Teleporters.GetTeleporters();

            foreach (var teleporter in teleporters)
            {
                if (teleporter.MapContextId == 0)
                    continue;

                var mapChannel = MapChannelManager.Instance.FindByContextId(teleporter.MapContextId);

                var newTeleporter = new DynamicObject
                {
                    Position = teleporter.Position,
                    Rotation = teleporter.Rotation,
                    MapContextId = teleporter.MapContextId,
                    EntityClassId = (EntityClasses)teleporter.ClassId,
                    Comment = teleporter.Description,
                    ObjectData = new WaypointInfo(teleporter.Id, false, (WaypointType)teleporter.Type)
                };

                switch (teleporter.Type)
                {
                    case 1:
                        newTeleporter.DynamicObjectType = DynamicObjectType.LocalTeleporter;
                        break;
                    case 2:
                        newTeleporter.DynamicObjectType = DynamicObjectType.Waypoint;
                        break;
                    case 3:
                        newTeleporter.DynamicObjectType = DynamicObjectType.Wormhole;
                        break;
                    case 4:
                        CellManager.Instance.AddToWorld(mapChannel, new MapTrigger(teleporter.Id, teleporter.Description, teleporter.Position, teleporter.Rotation, teleporter.MapContextId));
                        break;
                    case 5:
                        break;
                    default:
                        Logger.WriteLog(LogType.Error, $"InitTeleporters: unsuported teleporter type {teleporter.Type}");

                        MapErrorManager.Instance.Record(teleporter.MapContextId,
                            $"Teleporter {teleporter.Id} ({teleporter.Description}) is type {teleporter.Type}, which nothing handles.");

                        break;
                }

                mapChannel.Teleporters.Add(teleporter.Id, newTeleporter);
                Teleporters.Add(teleporter.Id, newTeleporter);
            }
        }

        internal void CheckPlayerWaypoint(Client client, WaypointInfo objectData)
        {
            // check if player has requested waypoint
            foreach (var waypoint in client.Player.GainedWaypoints)
                if (waypoint.WaypointId == objectData.WaypointId)
                 return;

            var newWaypoint = new CharacterTeleporterEntry(client.Player.Id, objectData.WaypointId, (byte)objectData.WaypointType);
            // add waypoint to player as he entered for the first time
            client.CallMethod(client.Player.EntityId, new WaypointGainedPacket(objectData.WaypointId, objectData.WaypointType));
            client.Player.GainedWaypoints.Add(newWaypoint);

            // And on the map, where this is the one thing about a marker the client cannot work
            // out for itself. The marker changes colour under the player as they stand on it.
            MapMarkerManager.Instance.WaypointDiscovered(client, objectData.WaypointId);

            // update Db
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Teleporter, newWaypoint);
        }

        internal Dictionary<uint, MapWaypointInfoList> CreateListOfWaypoints(Client client, WaypointType waypointType)
        {
            var listOfWaypoints = new Dictionary<uint, MapWaypointInfoList>();
            var waypointInfo = new List<WaypointInfo>();
            var mapChannel = client.Player.MapChannel;
            var contextId = mapChannel.MapInfo.MapContextId;

            // Inside a squad instance the map waypoint window only offers the way out: clientmethod.Recv_EnteredWaypoint
            // (line 402) - "if waypoints is None, user is on an adventure, and they can only abort the mission" - lists
            // PM 315 "Leave current adventure" as waypoint 0, which comes back as SelectWaypoint(mapId, 0).
            if (mapChannel.IsSquadInstance && waypointType == WaypointType.Waypoint)
            {
                listOfWaypoints.Add(contextId, new MapWaypointInfoList(contextId,
                    new List<MapInstanceInfo> { new MapInstanceInfo(mapChannel.Ordinal, mapChannel.MapInstanceId, MapChannelManager.Instance.StatusOf(mapChannel)) },
                    null));
                return listOfWaypoints;
            }

            // create waypoint list for player
            foreach (var waypoint in client.Player.GainedWaypoints)
            {
                if ((WaypointType)waypoint.WaypointType != waypointType)
                    continue;

                var teleporter = Teleporters[waypoint.WaypointId];
                var teleporterData = teleporter.ObjectData as WaypointInfo;

                // Map waypoints and local teleporters both travel within the map the player stands on
                // (help text 5697: "a list of all available waypoints on your current map"; a local teleporter
                // plays the LOCAL_TELEPORTER effect of a within-map move). SelectWaypoint would fly a player to
                // another map for anything listed from one.
                if (IsWithinMapType(teleporterData.WaypointType) && teleporter.MapContextId != contextId)
                    continue;

                if (teleporterData.WaypointType != waypointType)
                    continue;

                if (waypoint.WaypointId == teleporterData.WaypointId)
                    waypointInfo.Add(new WaypointInfo(teleporterData.WaypointId, teleporterData.Contested, teleporterData.WaypointType)
                    {
                        Position = teleporter.Position
                    });
            }

            // One row per copy of the map, each listing the same waypoints (waypointwindow.ShowWaypoints line 265: a
            // MapInstanceRow per (ordinal, mapId, overloadedStatus), its waypoints under it). Live notes 2007-08-21: "New
            // waypoint window that allows user to change between instances of the same map". It used to send one
            // identical row per waypoint.
            var copies = mapChannel.IsInstance && !mapChannel.IsSharedCopy
                ? new List<MapChannel> { mapChannel }
                : MapChannelManager.Instance.CopiesOf(contextId);
            var listOfMapInstances = copies
                .Select(copy => new MapInstanceInfo(copy.Ordinal, copy.MapInstanceId, MapChannelManager.Instance.StatusOf(copy)))
                .ToList();

            listOfWaypoints.Add(contextId, new MapWaypointInfoList(contextId, listOfMapInstances, waypointInfo));

            return listOfWaypoints;
        }

        internal void SelectWaypoint(Client client, SelectWaypointPacket packet)
        {
            // Both ids come from the client and used to be indexed straight into the map and
            // teleporter dictionaries, so an unknown map or waypoint id threw KeyNotFoundException
            // in the handler and the player was disconnected. Now the request is checked the way
            // the waypoint window itself is built: the player has to be standing at a waypoint,
            // the destination has to exist, and it has to be one this character has gained -
            // dropships included (help text 5697, see CreateListOfDropships).
            if (client.Player == null || client.State != ClientState.Ingame)
                return;

            // The client sends None for the map when it means the one it is on; otherwise the mapId of the waypoint
            // window row it chose - a context id for a shared primary channel, an instance's own id for a copy.
            var targetMap = packet.MapInstanceId != 0
                ? MapChannelManager.Instance.ChannelByMapInstanceId(packet.MapInstanceId)
                : client.Player.MapChannel;
            var mapContextId = targetMap?.MapInfo?.MapContextId ?? packet.MapInstanceId;

            // "Leave current adventure" (waypoint 0, PM 315) from a squad instance's waypoint window.
            if (packet.WaypointId == 0 && ReferenceEquals(targetMap, client.Player.MapChannel) && targetMap.IsSquadInstance)
            {
                if (IsAtWaypoint(client))
                    MapChannelManager.Instance.ReturnFromSquadInstance(client, false);
                else
                    Logger.WriteLog(LogType.Debug, $"SelectWaypoint: {client.Player.Name} asked to leave the adventure away from a waypoint");
                return;
            }

            // A private per-character context is entered through its own content (the boot camp's
            // exit and entry), never by dropship; nor is anyone else's squad instance.
            if (targetMap == null
                || MapChannelManager.Instance.IsPerCharacterContext(mapContextId)
                || (targetMap.IsInstance && !targetMap.IsSharedCopy && !ReferenceEquals(targetMap, client.Player.MapChannel))
                || !targetMap.Teleporters.TryGetValue(packet.WaypointId, out var teleporter)
                || !(teleporter.ObjectData is WaypointInfo objData))
            {
                Logger.WriteLog(LogType.Debug, $"SelectWaypoint: {client.Player.Name} asked for unknown waypoint {packet.WaypointId} on map {packet.MapInstanceId}");
                return;
            }

            if (!IsAtWaypoint(client))
            {
                Logger.WriteLog(LogType.Debug, $"SelectWaypoint: {client.Player.Name} is not standing at a waypoint");
                return;
            }

            if (!HasGained(client.Player, objData.WaypointId, objData.WaypointType))
            {
                Logger.WriteLog(LogType.Debug, $"SelectWaypoint: {client.Player.Name} has not gained waypoint {objData.WaypointId}");
                return;
            }

            // Another numbered copy of the map the player is on: over to it at the chosen waypoint.
            if (mapContextId == client.Player.MapContextId && !ReferenceEquals(targetMap, client.Player.MapChannel))
            {
                MapChannelManager.Instance.ChangeMap(client, mapContextId,
                    new Vector3(teleporter.Position.X, teleporter.Position.Y + 1, teleporter.Position.Z), (float)teleporter.Rotation, targetMap);
                return;
            }

            if (mapContextId != client.Player.MapContextId)
            {
                var dropship = new Dropship(Factions.AFS, DropshipType.Teleporter, client, teleporter.Position, mapContextId);

                CellManager.Instance.AddToWorld(client.Player.MapChannel, dropship);
                Dropships.Add(dropship.EntityId, dropship);
                client.CallMethod(SysEntity.ClientMethodId, new RequestMovementBlockPacket());

                client.LoadingMap = mapContextId;
                return;
            }

            var movementData = new Models.Movement
                (
                new Vector3(
                    teleporter.Position.X,
                    teleporter.Position.Y + 1,
                    teleporter.Position.Z),
                0f,
                0,
                new Vector2((float)teleporter.Rotation, 0f)
            );

            var teleportType = FxTypeOf(objData.WaypointType);

            // Actor.BeginTeleport ("Notified by the server that we are about to teleport ... send an
            // acknowledgement after the teleport message is received") has to come before Teleport,
            // or Recv_Teleport finds no pending acknowledgement to send.
            client.CellCallMethod(client, client.Player.EntityId, new PreTeleportPacket(teleportType));
            client.CallMethod(SysEntity.ClientMethodId, new BeginTeleportPacket());
            client.CallMethod(client.Player.EntityId, new TeleportPacket(teleporter.Position, teleporter.Rotation, teleportType, 5));

            // The onlookers' half of the sequence; see PlayerDeathManager.TeleportWithinMap for why
            // it straddles the move and why the teleporting client is left out of both sends.
            client.CellIgnoreSelfCallMethod(client, new PostTeleportPacket());
            client.CellMoveObject(client, new MoveObjectMessage(client.Player.EntityId, movementData), false);
            client.CellIgnoreSelfCallMethod(client, new TeleportArrivalPacket());

            teleporter.TriggeredByPlayers.Remove(client);    // ToDO: maybe safely remove client
        }

        private static bool IsWithinMapType(WaypointType waypointType)
            => waypointType == WaypointType.Waypoint || waypointType == WaypointType.LocalTeleporter;

        /// <summary>
        /// The effect a pad plays, for PreTeleport and Teleport.
        ///
        /// teleporter.type is a <see cref="WaypointType"/> - what the pad is for - and not the
        /// client's teleporterFX key, which is a separate thing the original server held and this
        /// world does not: generated/client/teleportertype.pyo keys the three-part effect
        /// (pre, post, arrive) by DEFAULT 0, BRIDGE_TELEPORTER 1, ADVENTURE_LAUNCHER 2, NOFX 3,
        /// BANE_TELEPORTER 4, SELF_DESTRUCT 5, LOCAL_TELEPORTER 6, TACTICAL_EVASION 7, and nothing
        /// in the surviving client data binds a pad to one (usabledata has no row for any of the
        /// teleporter entity classes). So only the one binding the names already make is taken:
        /// a LocalTeleporter pad is LOCAL_TELEPORTER, whose effect tuple (8555, 8551, 8550) is the
        /// same tuple as DEFAULT's, which is why this is a label and not a guess at content.
        /// Everything else stays DEFAULT until evidence turns up.
        /// </summary>
        private static TeleportType FxTypeOf(WaypointType waypointType)
        {
            return waypointType == WaypointType.LocalTeleporter ? TeleportType.LocalTeleporter : TeleportType.Default;
        }

        /// <summary>
        /// Whether the player currently has a waypoint window open on the server's side: the
        /// proximity workers add a client to a teleporter's TriggeredByPlayers or a dropship
        /// pad's TriggeredBy while it is within range, and take it out again when it leaves.
        /// </summary>
        private static bool IsAtWaypoint(Client client)
        {
            var mapChannel = client.Player.MapChannel;

            if (mapChannel == null)
                return false;

            foreach (var teleporter in mapChannel.Teleporters.Values)
                if (teleporter.TriggeredByPlayers.Contains(client))
                    return true;

            if (mapChannel.MapCellInfo.Cells.TryGetValue(client.Player.Cells[2, 2], out var cell))
                foreach (var trigger in cell.MapTriggers)
                    if (trigger.TriggeredBy.Contains(client))
                        return true;

            return false;
        }

        internal void TeleportAcknowledge(Client client)
        {
            client.CallMethod(client.Player.EntityId, new TeleportArrivalPacket());
        }

        internal void PlayerEnterWaypoint(DynamicObject obj)
        {
            var cellSeed = CellManager.Instance.GetCellSeed(obj.Position);
            // The pad's own channel: two copies of a map each have their own pads.
            var mapChannel = MapChannelManager.ChannelOf(obj);

            if (!mapChannel.MapCellInfo.Cells.ContainsKey(cellSeed))
                return;

            foreach (var client in mapChannel.MapCellInfo.Cells[cellSeed].ClientList)
            {
                // check if player is near waypoint
                if (!client.Player.IsNear2m(obj))
                {
                    continue;
                }

                // check if already added
                if (obj.TriggeredByPlayers.Any(p => p == client))
                {
                    continue;
                }

                // if not add him and send enter packet
                obj.TriggeredByPlayers.Add(client);

                var objectData = (WaypointInfo)obj.ObjectData;

                CheckPlayerWaypoint(client, objectData);

                var waypointInfoList = CreateListOfWaypoints(client, objectData.WaypointType);

                // currentMapId marks which map instance row is "(Current)" (waypointwindow.ShowWaypoints line 270).
                client.CallMethod(SysEntity.ClientMethodId, new EnteredWaypointPacket(mapChannel.MapInstanceId, obj.MapContextId, waypointInfoList, objectData.WaypointType, objectData.WaypointId));

                // check if we already added him to the waypoint
            }
        }

        internal void PlayerExitWaypoint(DynamicObject obj)
        {
            for (var i = obj.TriggeredByPlayers.Count - 1; i >= 0; i--)
            {
                var client = obj.TriggeredByPlayers[i];

                if (!client.Player.IsNear2m(obj))
                {
                    obj.TriggeredByPlayers.RemoveAt(i);

                    client.CallMethod(SysEntity.ClientMethodId, new ExitedWaypointPacket());
                }
            }
        }

        /// <summary>
        /// Whether the character has gained this waypoint of this kind. Persisted rows carry the type the
        /// pad was gained as (CharacterTeleporterEntry.WaypointType).
        /// </summary>
        internal static bool HasGained(Manifestation player, uint waypointId, WaypointType waypointType)
            => player.GainedWaypoints.Any(w => w.WaypointId == waypointId && (WaypointType)w.WaypointType == waypointType);

        /// <summary>
        /// Walking onto a dropship pad gains it. The final client's help text says so (uielementlanguage 5697,
        /// "Waypoints/Dropships"): "Access to a Dropship Transport is gained by walking across the pad ... A special
        /// travel menu will appear with a list of any available Dropship Transports. Remember, you must first travel
        /// to another map and gain access to a Dropship Transport there before you can use this method of travel!"
        /// The gain is kept silently: manifestation.Recv_WaypointGained has a line for types 1-3 only and the client
        /// knows no dropship waypoint type, so no gain message is sent (GAP-DROPSHIP-GAIN-MESSAGE). The help text's
        /// "when a Dropship is hovering with its transporter beam activated" is not modelled: the pad is always live
        /// (GAP-DROPSHIP-HOVER).
        /// </summary>
        internal void GainDropship(Client client, uint waypointId)
        {
            if (HasGained(client.Player, waypointId, WaypointType.Dropship))
                return;

            var entry = new CharacterTeleporterEntry(client.Player.Id, waypointId, (byte)WaypointType.Dropship);
            client.Player.GainedWaypoints.Add(entry);
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Teleporter, entry);
        }

        /// <summary>
        /// The dropship travel window: the dropship pads this character has gained, grouped by map. It used to list
        /// every pad of every map from level 1 (help text 5697 says the list is the gained ones), and it drew each
        /// map's first pad at (-225.353, 99.597, -70.5246) - the world seed's position for Denzil's Caldera Outpost
        /// Hospital on the boot-camp map - because that literal stood where the pad's own position belonged.
        /// waypointwindow.SetupWaypointLocationRows places each location's widget at the position sent with it, so
        /// every pad now carries its own row's position.
        /// </summary>
        internal Dictionary<uint, MapWaypointInfoList> CreateListOfDropships(Manifestation player)
        {
            var dropships = new Dictionary<uint, MapWaypointInfoList>();

            foreach (var entry in Teleporters)
            {
                var teleporter = entry.Value;

                if (!(teleporter.ObjectData is WaypointInfo teleporterInfo) || teleporterInfo.WaypointType != WaypointType.Dropship)
                    continue;

                if (MapChannelManager.Instance.IsPerCharacterContext(teleporter.MapContextId))
                    continue;

                if (!HasGained(player, teleporterInfo.WaypointId, WaypointType.Dropship))
                    continue;

                if (!dropships.TryGetValue(teleporter.MapContextId, out var map))
                {
                    var instance = new List<MapInstanceInfo> { new MapInstanceInfo(1, teleporter.MapContextId, MapInstanceStatus.Low) };
                    map = new MapWaypointInfoList(teleporter.MapContextId, instance, new List<WaypointInfo>());
                    dropships.Add(teleporter.MapContextId, map);
                }

                map.Waypoints.Add(new WaypointInfo(teleporterInfo.WaypointId, teleporterInfo.Contested, teleporter.Position, WaypointType.Dropship));
            }

            return dropships;
        }
        #endregion
    }
}
