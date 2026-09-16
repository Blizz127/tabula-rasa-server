using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Crafting.Client;
    using Packets.Crafting.Server;
    using Packets.MapChannel.Server;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.World;
    using System.IO;

    /// <summary>
    /// Crafting stations (Kraftwerks, to the client) and the crafting requests made at them.
    ///
    /// The station is a usable dynamic object. Using it is what opens the crafting window: the
    /// client's Kraftwerks augmentation posts UI_CRAFTINGSTATION_ACTIVATE on Recv_Use for the actor
    /// who used it. Every request the window then makes names the station and is answered on it:
    /// CraftingStatus carries the player's jobs at that station, CraftingSuccess/Failure the outcome
    /// the window reports. The client resets its job list when it uses a station, so a status
    /// message follows every use.
    ///
    /// This is the first step of the crafting roadmap: stations exist, the window opens, requests
    /// are read and answered. The recipes themselves (fabrication, roadmap 3.2) come next, and until
    /// then every request is declined with a failure the window can show.
    /// </summary>
    public class KraftwerksManager
    {
        private static KraftwerksManager _instance;
        private static readonly object InstanceLock = new object();

        /// <summary>The use arg the client sends for a station: usabledata[9595][0]. UseObject recovery is dispatched on it.</summary>
        public const uint UseObjectArgId = 5;

        /// <summary>Windup the client is told for a use; the window opens as soon as Recv_Use arrives, so this only paces the animation.</summary>
        public const int UseWindupMs = 100;

        /// <summary>shared/gameconstants.py MAX_INTERACTION_RANGE; a request from further away than this is not honoured.</summary>
        public const float InteractionRange = 5.0f;

        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;

        /// <summary>Every station by database id, live object included.</summary>
        private readonly Dictionary<uint, Station> _stations = new Dictionary<uint, Station>();

        /// <summary>Jobs per (station entity, character), for CraftingStatus.</summary>
        private readonly Dictionary<(ulong station, uint character), List<CraftingJob>> _jobs = new Dictionary<(ulong, uint), List<CraftingJob>>();

        public class Station
        {
            public KraftwerksEntry Entry;
            public DynamicObject Object;
        }

        public static KraftwerksManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new KraftwerksManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        private KraftwerksManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
        }

        public IEnumerable<Station> Stations => _stations.Values;

        public bool TryGet(uint id, out Station station) => _stations.TryGetValue(id, out station);

        #region Loading and placing

        /// <summary>Loads the kraftwerks table into the maps. Runs after MapChannelInit; the objects enter the world from the dynamic-object worker.</summary>
        public void KraftwerksInit()
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var entries = unitOfWork.Kraftwerks.GetKraftwerks();
            var placed = 0;

            foreach (var entry in entries)
                if (Place(entry) != null)
                    placed++;

            Logger.WriteLog(LogType.Initialize, $"Loaded {placed} crafting stations ({entries.Count} rows)");
        }

        /// <summary>
        /// Builds the live object for a row and puts it in its map's list. Null when the map is not
        /// loaded: that is logged, not raised as a map error, because it is not something a GM can
        /// put right in game - the seven seeded stations on the two wargame maps wait for those maps
        /// to be added to map_info.
        /// </summary>
        private Station Place(KraftwerksEntry entry)
        {
            if (!MapChannelManager.Instance.MapChannelArray.TryGetValue(entry.MapContextId, out var mapChannel))
            {
                Logger.WriteLog(LogType.Initialize, $"  kraftwerks {entry.Id} ({entry.Comment}) is on map {entry.MapContextId}, which is not loaded; skipped");
                return null;
            }

            var station = new Station
            {
                Entry = entry,
                Object = new DynamicObject
                {
                    Position = entry.Position,
                    Rotation = entry.Rotation,
                    MapContextId = entry.MapContextId,
                    EntityClassId = (EntityClasses)entry.ClassId,
                    DynamicObjectType = DynamicObjectType.Kraftwerks,
                    StateId = UseObjectState.CrafterState0,
                    WindupTime = UseWindupMs,
                    Comment = entry.Comment
                }
            };

            _stations[entry.Id] = station;
            mapChannel.Kraftwerks[entry.Id] = station.Object;

            return station;
        }

        /// <summary>Takes a station out of the world and its map's list; the row is untouched.</summary>
        private void Unplace(Station station)
        {
            if (MapChannelManager.Instance.MapChannelArray.TryGetValue(station.Entry.MapContextId, out var mapChannel))
            {
                if (station.Object.IsInWorld)
                    CellManager.Instance.RemoveFromWorld(mapChannel, station.Object);

                mapChannel.Kraftwerks.Remove(station.Entry.Id);
            }

            _stations.Remove(station.Entry.Id);
        }

        /// <summary>From DynamicObjectWorker: puts stations that are not in the world yet into it.</summary>
        internal void Worker(MapChannel mapChannel)
        {
            foreach (var station in mapChannel.Kraftwerks.Values)
            {
                if (station.IsInWorld)
                    continue;

                CellManager.Instance.AddToWorld(mapChannel, station);
                station.IsInWorld = true;
            }
        }

        #endregion

        #region Using a station

        /// <summary>RequestUseObject on a station: the windup, the Use the client opens its window on, and the player's jobs there.</summary>
        internal void Use(Client client, DynamicObject station, ActionId actionId, uint actionArgId)
        {
            client.CallMethod(client.Player.EntityId, new PerformWindupPacket(PerformType.TwoArgs, actionId, actionArgId));
            client.CallMethod(station.EntityId, new UsePacket(client.Player.EntityId, station.StateId, UseWindupMs));
            client.Player.MapChannel.PerformRecovery.Add(new ActionData(client.Player, actionId, actionArgId, UseWindupMs));
            station.TriggeredByPlayers.Add(client);

            SendStatus(client, station);
        }

        /// <summary>PerformRecovery for UseObject arg 5: the use animation is over; leave the station as it was.</summary>
        internal void UseRecovery(MapChannel mapChannel, ActionData action)
        {
            foreach (var station in mapChannel.Kraftwerks.Values)
            {
                var user = station.TriggeredByPlayers.FirstOrDefault(c => c.Player == action.Actor);

                if (user == null)
                    continue;

                station.TriggeredByPlayers.Remove(user);

                if (!action.IsInrerrupted)
                    CellManager.Instance.CellCallMethod(station, new UsableInfoPacket(station.IsEnabled, station.StateId, 0, station.WindupTime, station.ActivateMission));

                return;
            }
        }

        private List<CraftingJob> JobsFor(Client client, DynamicObject station)
        {
            return _jobs.TryGetValue((station.EntityId, client.Player.Id), out var jobs) ? jobs : new List<CraftingJob>();
        }

        private void SendStatus(Client client, DynamicObject station)
        {
            client.CallMethod(station.EntityId, new CraftingStatusPacket(client.Player.EntityId, JobsFor(client, station)));
        }

        /// <summary>
        /// The station a request names, when it is one and the player is close enough to use it.
        /// Null otherwise; the caller has nothing sensible to answer on, so it answers nothing.
        /// </summary>
        private DynamicObject StationFor(Client client, ulong kraftwerksId, string request)
        {
            if (!EntityManager.Instance.TryGetObject(kraftwerksId, out var station) || station.DynamicObjectType != DynamicObjectType.Kraftwerks)
            {
                Logger.WriteLog(LogType.Debug, $"{request} from {client.Player.FamilyName} names {kraftwerksId}, which is not a crafting station");
                return null;
            }

            if (Vector3.Distance(client.Player.Position, station.Position) > InteractionRange + 2f)
            {
                Logger.WriteLog(LogType.Debug, $"{request} from {client.Player.FamilyName} at {Vector3.Distance(client.Player.Position, station.Position):0.#} m from station {kraftwerksId}");
                return null;
            }

            return station;
        }

        /// <summary>
        /// Every crafting request, until the recipes land (roadmap 3.2): the window is told the
        /// request failed so it re-enables its buttons, and the player is told why.
        /// </summary>
        private void Decline(Client client, ulong kraftwerksId, string request, string what)
        {
            var station = StationFor(client, kraftwerksId, request);

            if (station == null)
                return;

            SendStatus(client, station);
            client.CallMethod(station.EntityId, CraftingResultPacket.Failure(client.Player.EntityId));
            CommunicatorManager.Instance.SystemMessage(client, $"{what} is not available on this server yet.");
        }

        /// <summary>
        /// Fabrication, the 1.16.5 window's form: RequestCraftItemNew(kraftwerksId, recipeItemEntityId, craftingPage).
        /// The recipe is the schematic *item* the player is carrying, so the request names an entity and the schematic
        /// is consumed with the inputs.
        /// </summary>
        internal void RequestCraftItemNew(Client client, RequestCraftItemNewPacket packet)
        {
            var schematic = EntityManager.Instance.GetItem(packet.RecipeItemId);

            if (schematic?.ItemTemplate == null || !Owns(client.Player, schematic.EntityId))
            {
                Decline(client, packet.KraftwerksId, "RequestCraftItemNew", "That schematic");
                return;
            }

            Fabricate(client, packet.KraftwerksId, schematic.ItemTemplate.ItemTemplateId, packet.CraftingPage, schematic, "RequestCraftItemNew");
        }

        /// <summary>
        /// Fabrication, the older window's form: RequestCraftItem(kraftwerksId, recipeTemplateId). No schematic item is
        /// consumed, because the request names the recipe itself rather than an item carrying it.
        /// </summary>
        internal void RequestCraftItem(Client client, RequestCraftItemPacket packet)
            => Fabricate(client, packet.KraftwerksId, packet.RecipeTemplateId, 0, null, "RequestCraftItem");

        /// <summary>
        /// Starts a job: the recipe's own inputs are consumed, its level is required, and the station is told how long
        /// it will take (the recipe's KraftwerksTimeSeconds, which the client counts down from the status message).
        /// </summary>
        private void Fabricate(Client client, ulong kraftwerksId, uint recipeTemplateId, uint craftingPage, Item schematic, string request)
        {
            var station = StationFor(client, kraftwerksId, request);

            if (station == null)
                return;

            var recipe = CraftingData.ForTemplate(recipeTemplateId);

            if (recipe == null)
            {
                Refuse(client, station, "That is not a schematic this station knows how to make.");
                return;
            }

            if (client.Player.Level < recipe.MinimumPlayerLevel)
            {
                Refuse(client, station, $"That schematic needs level {recipe.MinimumPlayerLevel}.");
                return;
            }

            // Every input has to be present before anything is consumed, so a recipe the player cannot afford leaves
            // their materials where they were.
            var plan = PlanInputs(client.Player, recipe);

            if (plan == null)
            {
                Refuse(client, station, "The station is missing materials for that schematic.");
                return;
            }

            foreach (var (item, count) in plan)
                if (!InventoryManager.Instance.ReduceStackCount(client, InventoryType.Personal, item, count))
                {
                    Refuse(client, station, "The station could not take the materials for that schematic.");
                    return;
                }

            if (schematic != null && !InventoryManager.Instance.ReduceStackCount(client, InventoryType.Personal, schematic, 1))
            {
                Refuse(client, station, "The station could not take that schematic.");
                return;
            }

            var key = (station.EntityId, client.Player.Id);

            if (!_jobs.TryGetValue(key, out var jobs))
                _jobs[key] = jobs = new List<CraftingJob>();

            jobs.Add(new CraftingJob
            {
                ResultItemId = EntityManager.Instance.GetEntityId,
                ResultClassId = recipe.ResultEntityClassId,
                ResultItemTemplateId = recipe.ResultItemTemplateId,
                Count = recipe.ResultItemAmount,
                TimeLeftSeconds = recipe.KraftwerksTimeSeconds,
                CraftingPage = craftingPage,
                RecipeTemplateId = recipeTemplateId,
                StartedAtMs = nowMs()
            });

            SendStatus(client, station);
            client.CallMethod(station.EntityId, CraftingResultPacket.Success(client.Player.EntityId));
        }

        /// <summary>
        /// Collects a finished job: RequestRetrieveFinishedCraftItem(kraftwerksId, resultItemId). The client sends this
        /// when its countdown reaches zero, and the station checks its own clock before handing anything over.
        /// </summary>
        internal void RequestRetrieveFinishedCraftItem(Client client, RequestRetrieveFinishedCraftItemPacket packet)
        {
            var station = StationFor(client, packet.KraftwerksId, "RequestRetrieveFinishedCraftItem");

            if (station == null)
                return;

            var job = JobsFor(client, station).FirstOrDefault(candidate => candidate.ResultItemId == packet.ItemId);

            if (job == null || !IsFinished(job))
            {
                Refuse(client, station, "That is not ready yet.");
                return;
            }

            Deliver(client, station, job);
        }

        /// <summary>Collects everything that is ready: RequestRetrieveAllFinishedItems.</summary>
        internal void RequestRetrieveAllFinishedItems(Client client, RequestRetrieveAllFinishedItemsPacket packet)
        {
            var station = StationFor(client, packet.KraftwerksId, "RequestRetrieveAllFinishedItems");

            if (station == null)
                return;

            foreach (var job in JobsFor(client, station).Where(IsFinished).ToList())
                Deliver(client, station, job);

            SendStatus(client, station);
        }

        /// <summary>
        /// Salvaging, module extraction, integration and upgrading are not implemented: their costs are in the client's
        /// crafting table (MimeogelCostToExtract and its siblings), but the only one of the four whose item side the
        /// emulator has is fabrication, so these still decline with a message the window can show
        /// (GAP-W3-CRAFTING-MODULES).
        /// </summary>
        internal void RequestSalvageItem(Client client, RequestSalvageItemPacket packet) => Decline(client, packet.KraftwerksId, "RequestSalvageItem", "Salvage");
        internal void RequestExtractModule(Client client, RequestExtractModulePacket packet) => Decline(client, packet.KraftwerksId, "RequestExtractModule", "Module extraction");
        internal void RequestIntegrateItem(Client client, RequestIntegrateItemPacket packet) => Decline(client, packet.KraftwerksId, "RequestIntegrateItem", "Module integration");
        internal void RequestUpgradeItem(Client client, RequestUpgradeItemPacket packet) => Decline(client, packet.KraftwerksId, "RequestUpgradeItem", "Module upgrade");

        /// <summary>
        /// Hands one job's result over. The item is made here rather than when the job starts, so a job that is never
        /// collected holds no item row: a full inventory refuses the collection and the job stays where it is.
        /// </summary>
        private void Deliver(Client client, DynamicObject station, CraftingJob job)
        {
            var item = ItemManager.Instance.CreateFromTemplateId(job.ResultItemTemplateId, job.Count);

            if (item == null || InventoryManager.Instance.AddItemToInventory(client, item) == null)
            {
                Refuse(client, station, "There is no room for that in your inventory.");
                return;
            }

            JobsFor(client, station).Remove(job);
            SendStatus(client, station);
            client.CallMethod(station.EntityId, CraftingResultPacket.Success(client.Player.EntityId));
        }

        private static bool IsFinished(CraftingJob job)
            => Environment.TickCount64 - job.StartedAtMs >= (long)(job.TimeLeftSeconds * 1000);

        private static long nowMs() => Environment.TickCount64;

        /// <summary>
        /// What a recipe needs from the player's own inventory, or null when something is short. Counted by item
        /// template, so several stacks of one material count as a single pool.
        /// </summary>
        private static List<(Item Item, uint Count)> PlanInputs(Manifestation player, CraftingData.Recipe recipe)
        {
            var plan = new List<(Item, uint)>();

            foreach (var input in recipe.Inputs)
            {
                var remaining = input.Quantity;

                foreach (var entityId in player.Inventory.PersonalInventory)
                {
                    if (remaining == 0)
                        break;

                    var item = EntityManager.Instance.GetItem(entityId);

                    if (item?.ItemTemplate == null || item.ItemTemplate.ItemTemplateId != input.ItemTemplateId || item.StackSize == 0)
                        continue;

                    var take = Math.Min(remaining, item.StackSize);
                    plan.Add((item, take));
                    remaining -= take;
                }

                if (remaining > 0)
                    return null;
            }

            return plan;
        }

        private static bool Owns(Manifestation player, ulong entityId)
            => player.Inventory.PersonalInventory.Contains(entityId);

        /// <summary>Refuses a request the way the window expects: status, failure, and a line for the player.</summary>
        private void Refuse(Client client, DynamicObject station, string reason)
        {
            SendStatus(client, station);
            client.CallMethod(station.EntityId, CraftingResultPacket.Failure(client.Player.EntityId));
            CommunicatorManager.Instance.SystemMessage(client, reason);
        }

        #endregion

        #region GM editing

        /// <summary>Creates a row and a live station at the position and facing given. Null when the insert failed or the map is not loaded.</summary>
        public Station Add(uint mapContextId, Vector3 position, double rotation, string comment)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var entry = new KraftwerksEntry
            {
                ClassId = KraftwerksEntry.DefaultClassId,
                MapContextId = mapContextId,
                PosX = position.X,
                PosY = position.Y,
                PosZ = position.Z,
                Rotation = rotation,
                Comment = comment ?? string.Empty
            };

            if (unitOfWork.Kraftwerks.AddKraftwerks(entry) == 0)
                return null;

            return Place(entry);
        }

        /// <summary>Moves or turns a station: the row is updated, the object is rebuilt in place so clients see it move.</summary>
        public bool Move(Station station, Vector3 position, double rotation)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var entry = station.Entry;
            entry.PosX = position.X;
            entry.PosY = position.Y;
            entry.PosZ = position.Z;
            entry.Rotation = rotation;

            if (!unitOfWork.Kraftwerks.UpdateKraftwerks(entry))
                return false;

            // A fresh object: RemoveFromWorld frees the entity id, so the old one cannot be re-added.
            Unplace(station);
            Place(entry);

            return true;
        }

        public bool SetComment(Station station, string comment)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            station.Entry.Comment = comment ?? string.Empty;
            station.Object.Comment = station.Entry.Comment;

            return unitOfWork.Kraftwerks.UpdateKraftwerks(station.Entry);
        }

        public bool Delete(Station station)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();

            if (!unitOfWork.Kraftwerks.DeleteKraftwerks(station.Entry.Id))
                return false;

            Unplace(station);

            return true;
        }

        /// <summary>The stations on a map, nearest to the position first.</summary>
        public List<Station> OnMap(uint mapContextId, Vector3 position)
        {
            return _stations.Values.Where(s => s.Entry.MapContextId == mapContextId)
                                   .OrderBy(s => Vector3.Distance(s.Entry.Position, position))
                                   .ToList();
        }

        #endregion
    }
}
