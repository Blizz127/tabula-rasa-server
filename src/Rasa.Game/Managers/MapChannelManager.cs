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
        private readonly object _channelRegistryLock = new object();
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
            PopulateContextCopy = PopulateCopy;
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
            lock (_channelRegistryLock)
                return MapChannelArray[contextId];
        }

        #region Instances

        /// <summary>Every instance channel (per-character, per-squad, shared copies) by instance id (never 0 or 1).</summary>
        public readonly Dictionary<uint, MapChannel> InstanceChannels = new();

        private readonly Dictionary<uint, MapChannel> _characterInstances = new();
        private readonly List<MapChannel> _instancesToDestroy = new();
        private uint _lastInstanceId = 1;

        /// <summary>Whether a context gives each character its own instance (content_map_setting).</summary>
        public Func<uint, bool> IsPerCharacterContext { get; set; } = contextId =>
            MissionContentManager.Instance.Content.Catalog.InstancingFor(contextId) == MapInstancing.PerCharacter;

        /// <summary>
        /// Whether a context gives each squad its own copy (content_map_setting instancing 2, the final client's
        /// MISSIONCONTEXT maps; migration MissionContextSquadInstancing).
        /// </summary>
        public Func<uint, bool> IsSquadContext { get; set; } = contextId =>
            MissionContentManager.Instance.Content.Catalog.InstancingFor(contextId) == MapInstancing.PerSquad;

        /// <summary>Fills a new per-character instance with its context's content placements (S3).</summary>
        public Action<MapChannel> PopulateInstance { get; set; } = channel =>
            ContentMaterializer.Materialize(channel, MissionContentManager.Instance.Content);

        /// <summary>
        /// Fills a new squad instance or shared copy with everything its context's primary channel was given at
        /// startup: spawn pools, Logos shrines, teleporters, lockboxes, map links, the navmesh and the content
        /// placements. TaRapedia 'Operation' rev 32773 (2008-09-04): "the server creates an identical copy of the zone".
        /// </summary>
        public Action<MapChannel> PopulateContextCopy { get; set; }

        /// <summary>
        /// How long an emptied squad instance its squad (or solo owner) can still re-enter is kept before it is
        /// destroyed (OD-126). Dated evidence says an empty instance is not reset at once - official notes 2007-11-29
        /// "re-enters it before the instance resets", 2008-08-22 "leaves the instance (allowing it to reset)" - and
        /// TaRapedia 'Operation' rev 32773 (2008-09-04, a player): "You need to be out of the instance for a set period
        /// of time for it to fully reset. I've found that ten minutes works." 600 000 ms is that observed upper bound
        /// (measured, 0-10 min); the original timer is unrecovered (GAP-INSTANCE-RESET-TIMER).
        /// </summary>
        public long SquadInstanceEmptyLingerMs { get; set; } = 600_000;

        /// <summary>
        /// Players one copy of a shared context holds before another numbered copy is opened (OD-127); null keeps a
        /// context to its one primary channel. No client table or dated source gives the original capacity
        /// (GAP-SHARED-COPY-CAPACITY), so no context is capped by default.
        /// </summary>
        public Func<uint, int?> SharedCopyCapacity { get; set; } = _ => null;

        /// <summary>Every live channel, shared contexts first; a snapshot safe across instance creation and removal.</summary>
        public List<MapChannel> Channels()
        {
            lock (_channelRegistryLock)
                return MapChannelArray.Values.Concat(InstanceChannels.Values).ToList();
        }

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
        /// The channel a character enters for a context: the shared channel; for a per-character context a new
        /// private instance (build plan S3, OD-2), releasing the character's previous one; for a per-squad context
        /// the copy its squad already has, else a new one bound to that squad - or, outside a squad, to the
        /// character. Null for an unknown context.
        ///
        /// A copy stays bound to whoever created it. A solo player who invites someone while inside keeps their own
        /// copy, and the invitee who then enters gets the squad's copy; the leader reaches it only by leaving and
        /// re-entering - the D10 and D13 live known issue ("after the squad leader exits and re-enters the
        /// instance, he will be placed in the same instance as the invited character"), kept as documented (OD-125).
        /// </summary>
        public MapChannel ChannelForEntry(uint characterId, uint contextId, uint partyId = 0)
        {
            lock (_channelRegistryLock)
            {
                if (!MapChannelArray.TryGetValue(contextId, out var shared))
                    return null;

                if (IsPerCharacterContext(contextId))
                    return PerCharacterInstance(characterId, shared);

                if (IsSquadContext(contextId))
                    return SquadInstance(characterId, partyId, shared);

                return shared;
            }
        }

        private MapChannel PerCharacterInstance(uint characterId, MapChannel shared)
        {
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

            // OD-2 (approved 2026-09-14): monotonic instance ids, which Wonkavate has carried since S3.
            channel.Ordinal = channel.InstanceId;

            InstanceChannels.Add(channel.InstanceId, channel);
            _characterInstances[characterId] = channel;
            PopulateInstance(channel);

            Logger.WriteLog(LogType.Debug, $"Created instance {channel.InstanceId} of context {shared.MapInfo.MapContextId} for character {characterId}");
            return channel;
        }

        private MapChannel SquadInstance(uint characterId, uint partyId, MapChannel shared)
        {
            var contextId = shared.MapInfo.MapContextId;
            var existing = InstanceChannels.Values.FirstOrDefault(channel =>
                channel.IsSquadInstance && channel.MapInfo.MapContextId == contextId &&
                (partyId != 0 ? channel.OwnerPartyId == partyId : channel.OwnerPartyId == null && channel.OwnerSoloCharacterId == characterId));

            if (existing != null)
            {
                // Re-entered before it was reset: the same copy, with whatever was killed still dead.
                _instancesToDestroy.Remove(existing);
                existing.EmptySince = 0;
                return existing;
            }

            var channel = new MapChannel
            {
                MapInfo = shared.MapInfo,
                InstanceId = ++_lastInstanceId,
                IsSquadInstance = true,
                OwnerPartyId = partyId != 0 ? partyId : null,
                OwnerSoloCharacterId = partyId == 0 ? characterId : null,
                Ordinal = NextOrdinal(contextId),
                // client shared/gameconstants.pyo MAX_PARTY_SIZE = 6: one squad.
                PlayerLimit = 6,
                ClientList = new List<Client>()
            };

            InstanceChannels.Add(channel.InstanceId, channel);
            PopulateContextCopy(channel);

            Logger.WriteLog(LogType.Debug, $"Created squad instance {channel.InstanceId} ({channel.Ordinal}) of context {contextId} for " +
                (partyId != 0 ? $"squad {partyId}" : $"character {characterId}"));
            return channel;
        }

        /// <summary>The lowest copy number no live channel of the context holds (a shared context's primary is 1).</summary>
        private uint NextOrdinal(uint contextId)
        {
            var used = new HashSet<uint>(InstanceChannels.Values
                .Where(channel => channel.MapInfo?.MapContextId == contextId && !channel.IsPrivateInstance)
                .Select(channel => channel.Ordinal));

            if (!IsSquadContext(contextId) && !IsPerCharacterContext(contextId))
                used.Add(1);

            uint ordinal = 1;
            while (used.Contains(ordinal))
                ordinal++;

            return ordinal;
        }

        /// <summary>The primary channel and numbered copies of a shared context, lowest number first.</summary>
        public List<MapChannel> CopiesOf(uint contextId)
        {
            lock (_channelRegistryLock)
            {
                var copies = new List<MapChannel>();

                if (MapChannelArray.TryGetValue(contextId, out var primary))
                    copies.Add(primary);

                copies.AddRange(InstanceChannels.Values
                    .Where(channel => channel.IsSharedCopy && channel.MapInfo?.MapContextId == contextId)
                    .OrderBy(channel => channel.Ordinal));

                return copies;
            }
        }

        /// <summary>
        /// Opens another numbered copy of a shared context (OD-127). The final client names such copies itself -
        /// "Empire Sector: The Last Stand(1)" to "(5)" on the shutdown night, which players called "Earth 1".."Earth 5".
        /// </summary>
        public MapChannel OpenSharedCopy(uint contextId)
        {
            lock (_channelRegistryLock)
            {
                if (!MapChannelArray.TryGetValue(contextId, out var shared) || IsSquadContext(contextId) || IsPerCharacterContext(contextId))
                    return null;

                var channel = new MapChannel
                {
                    MapInfo = shared.MapInfo,
                    InstanceId = ++_lastInstanceId,
                    IsSharedCopy = true,
                    Ordinal = NextOrdinal(contextId),
                    PlayerLimit = shared.PlayerLimit,
                    ClientList = new List<Client>()
                };

                InstanceChannels.Add(channel.InstanceId, channel);
                PopulateContextCopy(channel);

                Logger.WriteLog(LogType.Debug, $"Opened copy {channel.Ordinal} (instance {channel.InstanceId}) of shared context {contextId}");
                return channel;
            }
        }

        /// <summary>The channel for an id the client sent back (SelectWaypoint, SelectInstance); null when unknown.</summary>
        public MapChannel ChannelByMapInstanceId(uint mapInstanceId)
        {
            lock (_channelRegistryLock)
            {
                if (mapInstanceId > MapChannel.InstanceMapIdBase)
                    return InstanceChannels.TryGetValue(mapInstanceId - MapChannel.InstanceMapIdBase, out var instance) ? instance : null;

                return MapChannelArray.TryGetValue(mapInstanceId, out var shared) ? shared : null;
            }
        }

        /// <summary>
        /// The population status the waypoint window and the instance chooser show beside a copy (client
        /// shared/gameconstants.pyo POPULATION_LOW 1 .. POPULATION_FULL 4). Only FULL is derived, at a configured
        /// capacity (OD-127); the original thresholds for MEDIUM and HIGH are unrecovered (GAP-SHARED-COPY-CAPACITY).
        /// </summary>
        public MapInstanceStatus StatusOf(MapChannel channel)
        {
            var capacity = channel?.MapInfo == null ? null : SharedCopyCapacity(channel.MapInfo.MapContextId);
            return capacity is int limit && Occupancy(channel) >= limit ? MapInstanceStatus.Full : MapInstanceStatus.Low;
        }

        private static int Occupancy(MapChannel channel) => channel.ClientList.Count + channel.QueuedClients.Count;

        /// <summary>Queues an instance with nobody in or entering it for destruction after the tick.</summary>
        public void ReleaseInstanceIfEmpty(MapChannel channel)
        {
            lock (_channelRegistryLock)
            {
                if (channel?.IsInstance == true && channel.ClientList.Count == 0 && channel.QueuedClients.Count == 0 &&
                    !_instancesToDestroy.Contains(channel))
                {
                    channel.EmptySince = _getMonotonicMilliseconds();
                    _instancesToDestroy.Add(channel);
                }
            }
        }

        /// <summary>
        /// Destroys the queued instances that are still empty: their creatures (with their corpses'
        /// loot) and objects leave the entity tables and free their ids, and the channel is dropped.
        /// A squad instance its owner can still re-enter waits out <see cref="SquadInstanceEmptyLingerMs"/> first.
        /// Runs after the map worker, never while it iterates.
        /// </summary>
        public void DestroyQueuedInstances()
        {
            lock (_channelRegistryLock)
            {
                if (_instancesToDestroy.Count == 0)
                    return;

                var now = _getMonotonicMilliseconds();
                var waiting = new List<MapChannel>();

                foreach (var channel in _instancesToDestroy.ToList())
                {
                    if (channel.ClientList.Count > 0 || channel.QueuedClients.Count > 0)
                    {
                        channel.EmptySince = 0;
                        continue;
                    }

                    var rejoinable = channel.IsSquadInstance && (channel.OwnerPartyId != null || channel.OwnerSoloCharacterId != null);
                    if (rejoinable && now - channel.EmptySince < SquadInstanceEmptyLingerMs)
                    {
                        waiting.Add(channel);
                        continue;
                    }

                    Destroy(channel);
                }

                _instancesToDestroy.Clear();
                _instancesToDestroy.AddRange(waiting);
            }
        }

        private void Destroy(MapChannel channel)
        {
            foreach (var cell in channel.MapCellInfo.Cells.Values)
                foreach (var creature in cell.CreatureList.ToList())
                    CellManager.Instance.RemoveCreatureFromWorld(channel, creature);

            var objects = channel.DynamicObjects
                .Concat(channel.Teleporters.Values)
                .Concat(channel.FootLockers.Values)
                .Concat(channel.ControlPoints.Values)
                .Distinct()
                .ToList();

            foreach (var dynamicObject in objects)
            {
                EntityManager.Instance.UnregisterEntity(dynamicObject.EntityId);
                EntityManager.Instance.UnregisterDynamicObject(dynamicObject.EntityId);
                EntityManager.Instance.FreeEntity(dynamicObject.EntityId);
            }

            foreach (var loot in channel.LootDispensers.Keys.ToList())
                EntityManager.Instance.FreeEntity(loot);

            // A Bane dropship still bringing an instance pool's creatures has nowhere to land.
            DynamicObjectManager.Instance.DropChannelDropships(channel);

            channel.DynamicObjects.Clear();
            channel.Teleporters.Clear();
            channel.FootLockers.Clear();
            channel.ControlPoints.Clear();
            channel.ContentUsables.Clear();
            channel.LootDispensers.Clear();
            channel.SpawnPools?.Clear();
            channel.MapCellInfo.Cells.Clear();

            InstanceChannels.Remove(channel.InstanceId);
            if (channel.OwnerCharacterId is uint owner &&
                _characterInstances.TryGetValue(owner, out var current) && ReferenceEquals(current, channel))
                _characterInstances.Remove(owner);

            Logger.WriteLog(LogType.Debug, $"Destroyed instance {channel.InstanceId} of context {channel.MapInfo?.MapContextId}");
        }

        private void PopulateCopy(MapChannel channel)
        {
            var contextId = channel.MapInfo.MapContextId;

            if (TryFindByContextId(contextId, out var primary))
            {
                channel.NavMesh = primary.NavMesh;
                DynamicObjectManager.Instance.CopyStaticObjects(primary, channel);
            }

            foreach (var link in MapLinkManager.Instance.Links.Where(link => link.MapContextId == contextId).ToList())
                CellManager.Instance.AddToWorld(channel, link);

            channel.SpawnPools = SpawnPoolManager.Instance.LoadedSpawnPools.Values
                .Where(pool => pool.MapContextId == contextId)
                .Select(pool => pool.CopyFor(channel))
                .ToList();

            ContentMaterializer.Materialize(channel, MissionContentManager.Instance.Content);
        }

        /// <summary>
        /// Takes a player out of the squad instance they are in, back to the map they entered it from (see
        /// <see cref="Manifestation.InstanceReturn"/>). With <paramref name="noLongerInSquad"/> the client is told
        /// why (PM 1058 PM_BOOTED_FROM_MAP). False when they are not in a squad instance or have nowhere to go.
        /// </summary>
        public bool ReturnFromSquadInstance(Client client, bool noLongerInSquad)
        {
            var player = client?.Player;
            var channel = player?.MapChannel;

            if (channel?.IsSquadInstance != true || client.State != ClientState.Ingame)
                return false;

            var destination = ReturnPointFor(player, channel.MapInfo.MapContextId);

            if (destination == null)
            {
                Logger.WriteLog(LogType.Error, $"{player.FamilyName} cannot leave instance {channel.InstanceId} of context {channel.MapInfo.MapContextId}: no return point");
                return false;
            }

            var (mapContextId, position, rotation) = destination.Value;

            if (!ChangeMap(client, mapContextId, position, rotation))
                return false;

            if (noLongerInSquad)
                client.CallMethod(SysEntity.CommunicatorId, new Packets.Communicator.Server.DisplayClientMessagePacket(
                    PlayerMessage.PmBootedFromMap, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));

            return true;
        }

        /// <summary>
        /// Where a player leaving an instance goes: the instance's own exit link to the map they came from (the
        /// arrival point the link was built with), else the spot they left that map from, else - after a restart
        /// lost it - the instance's first exit link. TaRapedia 'Operation': "you can only leave them towards the
        /// same zone you entered from".
        /// </summary>
        private static (uint, Vector3, float)? ReturnPointFor(Manifestation player, uint instanceContextId)
        {
            var exits = MapLinkManager.Instance.Links
                .Where(link => link.MapContextId == instanceContextId && link.Enabled && link.DestMapContextId != instanceContextId)
                .OrderBy(link => link.Id)
                .ToList();

            if (player.InstanceReturn is { } previous)
            {
                var exit = exits.FirstOrDefault(link => link.DestMapContextId == previous.MapContextId);
                return exit != null ? (exit.DestMapContextId, exit.DestPosition, exit.DestRotation) : previous;
            }

            var first = exits.FirstOrDefault();
            return first != null ? (first.DestMapContextId, first.DestPosition, first.DestRotation) : null;
        }

        /// <summary>
        /// A squad member has left or been kicked (PartyManager): out of the squad's instance with PM 1058. The
        /// client warned them first (party.OnLeaveParty: PM 946 "You will have to leave the current map." while
        /// SquadMemberList said partyExclusiveMap).
        /// </summary>
        public void SquadMemberRemoved(Client client, uint partyId)
        {
            var channel = client?.Player?.MapChannel;

            if (channel?.IsSquadInstance == true && channel.OwnerPartyId == partyId)
                ReturnFromSquadInstance(client, true);
        }

        /// <summary>
        /// A squad has disbanded: its copies can no longer be joined (a recycled squad id must not find them), and
        /// everyone inside is sent out - PM 945 "All squad members will be kicked out of the current map."
        /// </summary>
        public void SquadDisbanded(uint partyId, IEnumerable<Client> members)
        {
            var orphaned = new List<MapChannel>();

            lock (_channelRegistryLock)
                foreach (var channel in InstanceChannels.Values.Where(channel => channel.IsSquadInstance && channel.OwnerPartyId == partyId))
                {
                    channel.OwnerPartyId = null;
                    orphaned.Add(channel);
                }

            foreach (var member in members)
                if (orphaned.Contains(member?.Player?.MapChannel))
                    ReturnFromSquadInstance(member, true);
        }

        /// <summary>A squad id that has gone out of use without its members leaving (a merge): its copies can no longer be joined.</summary>
        public void SquadRetired(uint partyId)
        {
            lock (_channelRegistryLock)
                foreach (var channel in InstanceChannels.Values.Where(channel => channel.IsSquadInstance && channel.OwnerPartyId == partyId))
                    channel.OwnerPartyId = null;
        }

        #endregion

        #region Shared copies: the instance chooser

        /// <summary>
        /// The copies of a shared context offered to a player entering it, with their status; a new copy is opened
        /// first when every copy is at the configured capacity (OD-127). More than one means the client chooses;
        /// a full one stays listed, and choosing it is answered with PM 934 "You cannot go to that map at this
        /// time. Please select another map."
        /// </summary>
        public List<MapChannel> EntryCandidates(uint contextId)
        {
            var copies = CopiesOf(contextId);

            if (copies.Count > 0 && copies.All(channel => StatusOf(channel) == MapInstanceStatus.Full) && OpenSharedCopy(contextId) is { } fresh)
                copies.Add(fresh);

            return copies;
        }

        /// <summary>
        /// clientmethod.Recv_ChooseInstanceList ("display an instance list the user can choose from to go to a
        /// shared map"): the zone change waits for SelectInstance or SelectInstanceCancel. Live notes 2007-07-24:
        /// "Zoning into shared world maps will give you a choice of instances to go to if there's more than available."
        /// </summary>
        private void OfferInstanceChoice(Client client, uint contextId, Vector3 position, float rotation, List<MapChannel> candidates)
        {
            client.PendingInstanceChoice = (contextId, position, rotation);
            client.CallMethod(SysEntity.ClientMethodId, new ChooseInstanceListPacket(candidates
                .Select(channel => new ChooseInstanceListPacket.Entry(channel.Ordinal, channel.MapInstanceId, channel.MapInfo.MapContextId, StatusOf(channel)))
                .ToList()));
        }

        /// <summary>clientmethod.OnGotoInstance: SelectInstance(mapId, startGroup) - "which instance they've chosen to go to".</summary>
        public void SelectInstance(Client client, uint mapInstanceId)
        {
            if (client.PendingInstanceChoice is not { } pending || client.State != ClientState.Ingame)
                return;

            client.PendingInstanceChoice = null;
            var target = ChannelByMapInstanceId(mapInstanceId);

            if (target?.MapInfo?.MapContextId != pending.MapContextId || (target.IsInstance && !target.IsSharedCopy))
            {
                Logger.WriteLog(LogType.Debug, $"SelectInstance: {client.Player?.FamilyName} chose unknown copy {mapInstanceId} of context {pending.MapContextId}");
                return;
            }

            if (StatusOf(target) == MapInstanceStatus.Full)
            {
                client.CallMethod(SysEntity.CommunicatorId, new Packets.Communicator.Server.DisplayClientMessagePacket(
                    PlayerMessage.PmYouCannotGoToThatMapAtThisTimeRetry, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
                return;
            }

            ChangeMap(client, pending.MapContextId, pending.Position, pending.Rotation, target);
        }

        /// <summary>clientmethod.OnGotoInstanceCancel: "Tell the server we no longer want to zone out" - the player stays.</summary>
        public void SelectInstanceCancel(Client client) => client.PendingInstanceChoice = null;

        #endregion

        public bool TryFindByContextId(uint contextId, out MapChannel mapChannel)
        {
            lock (_channelRegistryLock)
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
                lock (_channelRegistryLock)
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

            // Unanswered duel challenges, bout clocks, and duellists who have walked away from
            // each other. Server-wide, like the squad sweep above: a duel is between two clients,
            // not a property of a map.
            WargameManager.Instance.Worker();

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

                    // despawn timers, and minions whose master has gone (InfiniteRasa 492954a)
                    MinionManager.Instance.Worker(mapChannel, delta);

                    // regeneration, and players whose combat has lapsed
                    ManifestationManager.Instance.RegenWorker(mapChannel, delta);

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

                        // auction listings whose duration has run out
                        AuctionHouseManager.Instance.ExpireDue();

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
            if (!client.AwaitingMapLoaded
                || (client.State != ClientState.Loading && client.State != ClientState.Teleporting))
            {
                Logger.WriteLog(LogType.Security,
                    $"AccountId = {client.AccountEntry?.Id} sent MapLoaded in state {client.State} with {(client.AwaitingMapLoaded ? "a" : "no")} map load pending; ignored.");
                return;
            }

            client.AwaitingMapLoaded = false;

            if (client.State == ClientState.Teleporting)
            {
                var dropship = new Dropship(Factions.AFS, DropshipType.Teleporter, client);
                var mapChannel = ChannelForEntry(client.Player.Id, client.LoadingMap, PartyManager.Instance.PartyOf(client)?.Id ?? 0);

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
                   mapInstance.Ordinal,
                   mapInstance.MapInfo.MapVersion,
                    client.Player.Position,
                   (float)client.Player.Rotation
               ));

            client.State = ClientState.Loading;
            client.State = ClientState.Loading;
            client.AwaitingMapLoaded = true;
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
        /// <param name="target">
        /// A specific channel of the context (a chosen shared copy, a summoner's instance); null resolves it:
        /// the squad's instance, a character's own instance, or - for a shared context with more than one copy -
        /// the client's instance chooser, which holds the move until SelectInstance.
        /// </param>
        public bool ChangeMap(Client client, uint mapContextId, Vector3 position, float orientation, MapChannel target = null)
        {
            if (client.Player == null || client.State != ClientState.Ingame)
                return false;

            if (!MapChannelArray.ContainsKey(mapContextId))
                return false;

            if (target != null && target.MapInfo?.MapContextId != mapContextId)
                return false;

            if (target == null && !IsSquadContext(mapContextId) && !IsPerCharacterContext(mapContextId))
            {
                var candidates = EntryCandidates(mapContextId);

                if (candidates.Count > 1)
                {
                    OfferInstanceChoice(client, mapContextId, position, orientation, candidates);
                    return true;
                }

                target = candidates.FirstOrDefault();
            }

            client.PendingInstanceChoice = null;

            // Read before DetachFromMap, which clears the squad id until the player is back in the world.
            var partyId = PartyManager.Instance.PartyOf(client)?.Id ?? 0;
            var previous = client.Player.MapChannel;

            client.CallMethod(SysEntity.ClientMethodId, new PreWonkavatePacket());
            client.State = ClientState.Loading;

            // Out of the old map while the player still points at it: entities, cells, the
            // managers that track it, and the old ClientList.
            DetachFromMap(client);

            // The shared channel, or a fresh private instance for a per-character context, or the squad's copy.
            var mapChannel = target ?? ChannelForEntry(client.Player.Id, mapContextId, partyId);

            // Where leaving a squad instance goes back to: the map they entered it from.
            if (!mapChannel.IsSquadInstance)
                client.Player.InstanceReturn = null;
            else if (previous?.IsSquadInstance != true && previous?.MapInfo != null)
                client.Player.InstanceReturn = (previous.MapInfo.MapContextId, client.Player.Position, (float)client.Player.Rotation);

            // What MapLoaded reads back when the client is ready: the map channel it adds the
            // player to, and the position the cell matrix is built from.
            client.Player.MapChannel = mapChannel;
            client.Player.MapContextId = mapContextId;
            client.Player.Position = position;
            client.Player.Rotation = orientation;
            client.LoadingMap = mapContextId;

            var packet = new WonkavatePacket(
                mapChannel.MapInfo.MapContextId,
                mapChannel.Ordinal,
                mapChannel.MapInfo.MapVersion,
                position,
                orientation);

            client.CallMethod(SysEntity.CurrentInputStateId, packet);
            client.AwaitingMapLoaded = true;
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Position, packet);
            mapChannel.ClientList.Add(client);
            mapChannel.EmptySince = 0;

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
            // map change, and MissileLaunch refuses cross-map targets, so drop it here. The
            // announced aim goes with it: nobody on the next map has been told anything, and a
            // stale value would suppress the TargetId for a genuine re-target to the same entity.
            player.Target = 0;
            player.AnnouncedTarget = 0;

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
            // Before the player leaves the cells, while their minions can still be told to go: "Player-controlled
            // subordinates will teleport with their masters, but not change maps." Leaving the map is leaving them
            // behind, so they are dismissed, not orphaned. (InfiniteRasa 492954a)
            MinionManager.Instance.DismissAll(client);

            // Before the cells let go of them: ending a duel takes the wargame data back off both
            // bodies, and that is a cell broadcast from this player's own cells.
            WargameManager.Instance.RemovePlayer(client);

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
