using System.Collections.Generic;

namespace Rasa.Structures
{
    using Game;

    public class MapChannel
    {
        // ToDo
        public MapInfo MapInfo { get; set; }

        /// <summary>1 for a shared context; a server-lifetime id above 1 for a private instance.</summary>
        public uint InstanceId { get; set; } = 1;

        /// <summary>The character a private instance belongs to; null for a shared context.</summary>
        public uint? OwnerCharacterId { get; set; }

        public bool IsPrivateInstance => OwnerCharacterId.HasValue;

        /// <summary>A copy of a per-squad context (a final-client MISSIONCONTEXT map).</summary>
        public bool IsSquadInstance { get; set; }

        /// <summary>
        /// The squad a squad instance was created for, or null for one created by a character outside a squad
        /// (<see cref="OwnerSoloCharacterId"/>) and for one whose squad has since disbanded, which nobody can join.
        /// </summary>
        public uint? OwnerPartyId { get; set; }

        /// <summary>The character that created a squad instance while outside a squad.</summary>
        public uint? OwnerSoloCharacterId { get; set; }

        /// <summary>A further numbered copy of a shared context, beside the primary channel (OD-127).</summary>
        public bool IsSharedCopy { get; set; }

        /// <summary>Any channel other than a shared context's primary one; such a channel is destroyed once empty.</summary>
        public bool IsInstance => IsPrivateInstance || IsSquadInstance || IsSharedCopy;

        /// <summary>
        /// The copy's number within its context, shown by the client after the map name ("Name(2)") on the loading
        /// screen (wonkavatorwindow._UpdateLoadingScreen line 408, from Wonkavate's instanceId) and the map window
        /// header (mapwindow._UpdateMapName line 1772), and sent as the ordinal of ChooseInstanceList and of the
        /// waypoint window's map instance rows. 1 for a shared primary channel.
        /// </summary>
        public uint Ordinal { get; set; } = 1;

        /// <summary>
        /// The id the client echoes back in SelectWaypoint and SelectInstance: the context id for a shared primary
        /// channel (the waypoint window's existing convention), otherwise <see cref="InstanceMapIdBase"/> plus the
        /// instance id, so no instance can be mistaken for a context.
        /// </summary>
        public uint MapInstanceId => IsInstance ? InstanceMapIdBase + InstanceId : MapInfo?.MapContextId ?? 0;

        public const uint InstanceMapIdBase = 0x40000000;

        /// <summary>Monotonic milliseconds at which the instance last became empty; 0 while occupied.</summary>
        public long EmptySince { get; set; }

        /// <summary>This channel's own spawn pools (an instance's copies); null for a shared primary channel, which runs the context's.</summary>
        public List<SpawnPool> SpawnPools { get; set; }
        // timers
        //public int TimerClientEffectUpdate { get; set; }
        //public int TimerMissileUpdate { get; set; }
        //public int TimerDynObjUpdate { get; set; }
        public long MapChannelElapsed { get; set; }
        /// <summary>Milliseconds since this map's creatures last ran BehaviorManager.CreatureThink.</summary>
        public long ControllerElapsed { get; set; }
        //public int TimerPlayerUpdate { get; set; }
        // player
        public int PlayerLimit { get; set; }
        public List<Client> ClientList { get; set; }
        // queue
        public readonly Queue<Client> QueuedClients = new Queue<Client>();
        // action
        public readonly List<ActionData> PerformRecovery = new List<ActionData>();
        // cell
        public MapCellInfo MapCellInfo = new MapCellInfo();

        /// <summary>The map's navmesh, or null when no navmesh/&lt;map&gt;.nav was built for it. See NavMeshManager.</summary>
        public Navigation.NavMeshQuery NavMesh { get; set; }
        // effect
        public int CurrentEffectId { get; set; } // increases with every spawned game effect
        /// <summary>Creatures carrying a timed game effect (a stun, a knockback), so the effect can expire.</summary>
        public HashSet<Creature> CreaturesWithEffects { get; } = new HashSet<Creature>();

        // Dynamic Object List
        public List<DynamicObject> DynamicObjects = new List<DynamicObject>();

        // Dictionary<uniqueControlPointId, dataAboutdynamicObject> ControlPoints
        public Dictionary<uint, DynamicObject> ControlPoints = new Dictionary<uint, DynamicObject>();

        // Dictionary<uniqueFootlockerId, dataAboutdynamicObject> Footlockers
        public Dictionary<uint, DynamicObject> FootLockers = new Dictionary<uint, DynamicObject>();

        // Dictionary<uniqueTeleporterId, dataAboutdynamicObject> Teleporters
        public Dictionary<uint,DynamicObject> Teleporters = new Dictionary<uint, DynamicObject>();

        // Dictionary<dynamicObjectEntityId, content placement id> for reconstructed-content usables.
        public Dictionary<ulong, uint> ContentUsables = new Dictionary<ulong, uint>();

        /// <summary>
        /// Content creatures waiting to come back: placement id -> the tick they are due on. A placement's respawn_ms
        /// is the game's own field, so a dead creature whose placement asks for a respawn is queued here and the
        /// channel's tick materializes it again (placement respawn).
        /// </summary>
        public Dictionary<uint, long> ContentRespawns = new Dictionary<uint, long>();

        /// <summary>
        /// Creature placements killed without a respawn delay. Refreshing a mission's
        /// presence condition must not recreate one after its corpse is removed; a
        /// rebuilt map channel starts with a fresh set.
        /// </summary>
        public HashSet<uint> DefeatedContentPlacements = new HashSet<uint>();

        public void RecordContentPlacementDeath(uint placementId, uint respawnMs, long now)
        {
            if (respawnMs == 0)
            {
                ContentRespawns.Remove(placementId);
                DefeatedContentPlacements.Add(placementId);
            }
            else
            {
                DefeatedContentPlacements.Remove(placementId);
                ContentRespawns[placementId] = now + respawnMs;
            }
        }

        public bool CanMaterializeContentPlacement(uint placementId) =>
            !DefeatedContentPlacements.Contains(placementId) && !ContentRespawns.ContainsKey(placementId);

        /// <summary>Crafting stations by kraftwerks row id; see KraftwerksManager.</summary>
        public Dictionary<uint, DynamicObject> Kraftwerks = new Dictionary<uint, DynamicObject>();

        // Dictionary<uniqueLootDispenserId, dataAboutLootDispenser> LootDispensers
        public Dictionary<ulong, LootDispenser> LootDispensers = new Dictionary<ulong, LootDispenser>();

        // Missiles on this mapChannel
        public List<Missile> QueuedMissiles = new List<Missile>();
    }
}
