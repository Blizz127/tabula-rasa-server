using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Rasa.Config;
using Rasa.Context.Char;
using Rasa.Context.World;
using Rasa.Configuration;
using Rasa.Configuration.ContextSetup;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Packets;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.LootDispenser.Client;
using Rasa.Packets.LootDispenser.Server;
using Rasa.Packets.Manifestation.Server;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Mission.Server;
using Rasa.Packets.Protocol;
using Rasa.Repositories.Char;
using Rasa.Repositories.Char.Character;
using Rasa.Repositories.Char.Items;
using Rasa.Repositories.Char.CharacterInventory;
using Rasa.Repositories.Char.CharacterMission;
using Rasa.Repositories.UnitOfWork;
using Rasa.Repositories.World;
using Rasa.Repositories.World.MissionContent;
using Rasa.Services.DbContext;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.Content;
using Rasa.Structures.World;

namespace Rasa.Test
{
    /// <summary>
    /// Mission-log lifecycle against the recovered client contract: accept at the
    /// giver, complete objectives through NPC conversations bound by package id,
    /// turn in at the receiver, abandon, and restore after reconnect. Mission
    /// content here is synthetic; no retail mission data is asserted.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public partial class MissionLogTests
    {
        private const uint AccountId = 10;
        private const uint CharacterId = 101;
        private const byte Slot = 1;
        private const uint MapId = 1220;
        private const uint GiverDbId = 100;
        private const uint ReceiverDbId = 200;
        private const uint ScoutPackage = 2584;
        private const uint MissionId = 7001;

        public class UnitProxy : DispatchProxy
        {
            public SqliteCharContext Context;
            public Func<bool> FailComplete;
            protected override object Invoke(MethodInfo method, object[] args)
            {
                switch (method.Name)
                {
                    case "get_Characters": return new CharacterRepository(Context);
                    case "get_CharacterMissions": return new CharacterMissionRepository(Context);
                    case "get_Items": return new ItemRepository(Context);
                    case "get_CharacterInventories": return new CharacterInventoryRepository(Context);
                    case "Complete":
                        if (FailComplete != null && FailComplete()) throw new InvalidOperationException("injected save failure");
                        Context.SaveChanges();
                        return null;
                    case "Dispose": Context.Dispose(); return null;
                    default: throw new NotSupportedException(method.Name);
                }
            }
        }

        public class WorldUnitProxy : DispatchProxy
        {
            public SqliteWorldContext Context;
            protected override object Invoke(MethodInfo method, object[] args)
            {
                switch (method.Name)
                {
                    case "get_MissionContent": return new MissionContentRepository(Context);
                    case "Dispose": Context.Dispose(); return null;
                    default: throw new NotSupportedException(method.Name);
                }
            }
        }

        private sealed class Factory : IGameUnitOfWorkFactory
        {
            private readonly SqliteConnection _connection;
            private readonly SqliteConnection _worldConnection;
            public bool FailNextComplete;
            public Factory(SqliteConnection connection, SqliteConnection worldConnection)
            {
                _connection = connection;
                _worldConnection = worldConnection;
            }
            public ICharUnitOfWork CreateChar()
            {
                var unit = DispatchProxy.Create<ICharUnitOfWork, UnitProxy>();
                var proxy = (UnitProxy)(object)unit;
                proxy.Context = WeaponReloadPersistenceTests.Context(_connection);
                proxy.FailComplete = () =>
                {
                    if (!FailNextComplete) return false;
                    FailNextComplete = false;
                    return true;
                };
                return unit;
            }
            public IWorldUnitOfWork CreateWorld()
            {
                var unit = DispatchProxy.Create<IWorldUnitOfWork, WorldUnitProxy>();
                ((WorldUnitProxy)(object)unit).Context = WorldContext(_worldConnection);
                return unit;
            }
        }

        private sealed class WorldTestConfiguration : IDbContextConfigurationService
        {
            private readonly SqliteConnection _connection;
            public WorldTestConfiguration(SqliteConnection connection) => _connection = connection;
            public void Configure(DbContextOptionsBuilder builder, DatabaseConnectionConfiguration configuration)
                => builder.UseSqlite(_connection);
        }

        private static SqliteWorldContext WorldContext(SqliteConnection connection)
            => new SqliteWorldContext(Options.Create(new DatabaseConfiguration()),
                new WorldTestConfiguration(connection), new SqliteDbContextPropertyModifier());

        private SqliteConnection _connection;
    private SqliteConnection _worldConnection;
        private Factory _factory;
        private MissionManager _missions;
        private NpcManager _npcs;
        private Client _client;
        private Creature _giver;
        private Creature _receiver;
        private Creature _scout;
        private MapChannel _map;
        private uint _now;
        private Logger.LoggerConfig _oldLogger;
        private readonly List<Creature> _registered = new();

        [TestInitialize]
        public void Initialize()
        {
            _oldLogger = Logger.Config;
            if (_oldLogger == null) Logger.UpdateConfig(new Logger.LoggerConfig { LogToFile = false });
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            _worldConnection = new SqliteConnection("Data Source=:memory:");
            _worldConnection.Open();
            _factory = new Factory(_connection, _worldConnection);
            _now = 1_700_000_000;
            _missions = new MissionManager(_factory, () => _now);
            // The singleton chain (DynamicObjectManager → MissionManager.Instance.Content)
            // must reach this test's mission manager and content manager.
            typeof(MissionManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)
                ?.SetValue(null, _missions);
            _npcs = new NpcManager(_factory, _missions);
            using (var context = WeaponReloadPersistenceTests.Context(_connection))
            {
                context.Database.EnsureCreated();
                context.GameAccountEntries.Add(new GameAccountEntry { Id = AccountId, Name = "Fixture", FamilyName = "Fixture", Email = "fixture@example.invalid" });
                context.CharacterEntries.Add(new CharacterEntry
                {
                    Id = CharacterId, AccountId = AccountId, Slot = Slot, Name = "Recruit", Level = 1, Credit = 40, Prestige = 0, Experience = 5,
                    MapContextId = MapId, CreatedAt = DateTime.UtcNow, LastPvPClan = DateTime.UtcNow
                });
                context.SaveChanges();
            }
            _client = new Client(_factory, new ClientPacketHandler()) { State = ClientState.Ingame };
            _map = new MapChannel { MapInfo = new MapInfo(MapId, "test", 1, 1), ClientList = new List<Client>() };
            _map.ClientList.Add(_client);
            _client.Player = new Manifestation
            {
                Id = CharacterId, Level = 1, Experience = 5, MapContextId = MapId, MapChannel = _map,
                Position = new Vector3(100f, 10f, 100f), Cells = new uint[,] { { 7 } }
            };
            _client.Player.Credits[CurencyType.Credits] = 40;
            _client.Player.Credits[CurencyType.Prestige] = 0;
            _giver = Npc(GiverDbId, 116, new Vector3(102f, 10f, 100f));
            _receiver = Npc(ReceiverDbId, 208, new Vector3(97f, 10f, 100f));
            _scout = Npc(300, ScoutPackage, new Vector3(100f, 10f, 103f));
            _map.MapCellInfo.Cells[7] = new MapCell { CreatureList = { _giver, _receiver, _scout } };
            _missions.LoadedMissions[MissionId] = Definition();
        }

        [TestCleanup]
        public void Cleanup()
        {
            typeof(MissionManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)
                ?.SetValue(null, null);
            foreach (var creature in _registered)
            {
                EntityManager.Instance.UnregisterCreature(creature.EntityId);
                EntityManager.Instance.UnregisterEntity(creature.EntityId);
            }
            if (_equippedItem != null)
                EntityManager.Instance.UnregisterItem(_equippedItem.EntityId);
            if (_crateObject != null)
            {
                EntityManager.Instance.UnregisterDynamicObject(_crateObject.EntityId);
                _crateObject = null;
            }
            if (_realItemManager != null)
            {
                typeof(ItemManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, _realItemManager);
                _realItemManager = null;
            }
            if (_realInventoryManager != null)
            {
                typeof(InventoryManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, _realInventoryManager);
                _realInventoryManager = null;
            }
            RestoreTemplates();
            if (_oldLogger == null) typeof(Logger).GetProperty(nameof(Logger.Config)).SetValue(null, null);
            _connection.Dispose();
            _worldConnection.Dispose();
        }

        // Giver 100 offers the mission; objective 5 completes at any NPC of package 2584
        // and reveals objective 4; objective 4 completes at the giver's package 116;
        // receiver 200 turns it in for 250 credits and 1,000 XP.
        private static Mission Definition()
        {
            var mission = new Mission(new NpcMissionEntry
            {
                Id = MissionId, GiverId = GiverDbId, ReciverId = ReceiverDbId, Level = 3, GroupType = 1, CategoryId = 10000044, Comment = "fixture"
            });
            mission.Objectives[5] = new MissionObjectiveDefinition { ObjectiveId = 5, Ordinal = 1, IsRequired = true, RevealedOnAccept = true };
            mission.Objectives[4] = new MissionObjectiveDefinition { ObjectiveId = 4, Ordinal = 2, IsRequired = true, RevealedOnAccept = false };
            mission.ObjectiveConversations.Add(new MissionObjectiveConversation { ObjectiveId = 5, NpcPackageId = ScoutPackage, PlayerFlagId = 1 });
            mission.ObjectiveConversations.Add(new MissionObjectiveConversation { ObjectiveId = 4, NpcPackageId = 116, PlayerFlagId = 1 });
            mission.Transitions[5] = new List<uint> { 4 };
            mission.Rewards.Add(new NpcMissionRewardEntry { Id = MissionId, Type = (byte)NpcMissionRewardType.Credits, Credits = 250 });
            mission.Rewards.Add(new NpcMissionRewardEntry { Id = MissionId, Type = (byte)NpcMissionRewardType.Experience, Credits = 1000 });
            MissionManager.BuildRewardInfo(mission);
            mission.RefreshDispenseObjectives();
            return mission;
        }

        private Creature Npc(uint dbId, uint package, Vector3 position)
        {
            var creature = new Creature
            {
                DbId = dbId, MapContextId = MapId, Position = position, Level = 1, Faction = Factions.AFS,
                State = CharacterState.Idle, Cells = new uint[,] { { 7 } }, Npc = new Npc { NpcPackageId = package }
            };
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            _registered.Add(creature);
            return creature;
        }

        private void Accept(Creature npc = null, uint missionId = MissionId)
            => _npcs.AssignNPCMission(_client, new AssignNPCMissionPacket { NpcEntityId = (npc ?? _giver).EntityId, MissionId = missionId });

        private void CompleteObjective(Creature npc, uint objectiveId, uint flag = 1)
            => _npcs.CompleteNPCObjective(_client, new CompleteNPCObjectivePacket { EntityId = npc.EntityId, MissionId = MissionId, ObjectiveId = objectiveId, PlayerFlagId = flag });

        private void CompleteMission(Creature npc = null, int? selection = null)
            => _npcs.CompleteNPCMission(_client, new CompleteNPCMissionPacket { EntityId = (npc ?? _receiver).EntityId, MissionId = MissionId, SelectionIdx = selection });

        private List<PythonPacket> Drain() => DrainAddressed().Select(entry => entry.Packet).ToList();

        private List<(ulong EntityId, PythonPacket Packet)> DrainAddressed()
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_client);
            var packets = new List<(ulong, PythonPacket)>();
            while (queue.PopOutgoing() is ProtocolPacket packet)
            {
                var message = (CallMethodMessage)packet.Message;
                packets.Add((message.EntityId, message.Packet));
            }
            return packets;
        }

        private (CharacterMissionEntry Mission, List<CharacterMissionObjectiveEntry> Objectives, CharacterEntry Character) Saved()
        {
            using var context = WeaponReloadPersistenceTests.Context(_connection);
            return (context.CharacterMissionEntries.SingleOrDefault(m => m.CharacterId == CharacterId && m.MissionId == MissionId),
                context.CharacterMissionObjectiveEntries.Where(o => o.CharacterId == CharacterId && o.MissionId == MissionId).OrderBy(o => o.ObjectiveId).ToList(),
                context.CharacterEntries.Single(c => c.Id == CharacterId));
        }

        [TestMethod]
        public void AcceptingAtTheGiverPersistsRevealedObjectivesAndSendsMissionGained()
        {
            Accept();
            var addressed = DrainAddressed();
            var packets = addressed.Select(entry => entry.Packet).ToList();
            var gained = packets.OfType<MissionGainedPacket>().Single();
            Assert.AreEqual(MissionId, gained.MissionId);
            Assert.AreEqual(MissionState.Active, gained.MissionInfo.MissionState);
            Assert.IsFalse(gained.MissionInfo.Completeable);
            Assert.AreEqual((int)_now, gained.MissionInfo.ChangeTime);
            Assert.AreEqual(10000044u, gained.MissionInfo.MissionConstantData.CategoryId);
            Assert.AreEqual(250u, gained.MissionInfo.MissionConstantData.RewardInfo.FixedReward.Credits[CurencyType.Credits]);
            var objective = gained.MissionInfo.ObjectivesList.Single();
            Assert.AreEqual(5u, objective.ObjectiveId);
            Assert.AreEqual((uint)MissionObjectiveState.Incomplete, objective.ObjectiveStatus);
            Assert.AreEqual(1u, objective.Ordinal);
            Assert.IsTrue(objective.IsRequired);
            Assert.IsNull(objective.TimeRemaining);

            var (mission, objectives, _) = Saved();
            Assert.AreEqual((uint)MissionState.Active, mission.MissionState);
            Assert.AreEqual(_now, mission.ChangeTime);
            Assert.AreEqual(1, objectives.Count);
            Assert.AreEqual(5u, objectives[0].ObjectiveId);
            Assert.AreEqual((uint)MissionObjectiveState.Incomplete, objectives[0].Status);

            // Visible related NPCs are refreshed at once: the giver stops advertising
            // the mission and the scout now offers objective 5.
            NPCConversationStatusPacket StatusFor(Creature creature) => addressed
                .Where(entry => entry.EntityId == creature.EntityId).Select(entry => entry.Packet)
                .OfType<NPCConversationStatusPacket>().Single();
            Assert.AreEqual(ConversationStatus.None, StatusFor(_giver).ConvoStatusId);
            Assert.AreEqual(ConversationStatus.ObjectivComplete, StatusFor(_scout).ConvoStatusId);
            CollectionAssert.AreEqual(new List<uint> { MissionId }, StatusFor(_scout).Data);
            Assert.AreEqual(ConversationStatus.None, StatusFor(_receiver).ConvoStatusId);
        }

        private readonly Dictionary<ulong, NPCConversationStatusPacket> _statusByEntity = new();

        private void CaptureStatuses()
        {
            _statusByEntity.Clear();
            foreach (var creature in new[] { _giver, _receiver, _scout })
            {
                _npcs.UpdateConversationStatus(_client, creature);
                _statusByEntity[creature.EntityId] = Drain().OfType<NPCConversationStatusPacket>().Single();
            }
        }

        [TestMethod]
        public void ConversationStatusFollowsThePlayersLog()
        {
            CaptureStatuses();
            Assert.AreEqual(ConversationStatus.Available, _statusByEntity[_giver.EntityId].ConvoStatusId);
            Assert.AreEqual(ConversationStatus.None, _statusByEntity[_receiver.EntityId].ConvoStatusId);
            Assert.AreEqual(ConversationStatus.None, _statusByEntity[_scout.EntityId].ConvoStatusId);

            Accept();
            Drain();
            CaptureStatuses();
            Assert.AreEqual(ConversationStatus.None, _statusByEntity[_giver.EntityId].ConvoStatusId);
            Assert.AreEqual(ConversationStatus.ObjectivComplete, _statusByEntity[_scout.EntityId].ConvoStatusId);

            CompleteObjective(_scout, 5);
            Drain();
            CaptureStatuses();
            Assert.AreEqual(ConversationStatus.None, _statusByEntity[_scout.EntityId].ConvoStatusId);
            Assert.AreEqual(ConversationStatus.ObjectivComplete, _statusByEntity[_giver.EntityId].ConvoStatusId);
            Assert.AreEqual(ConversationStatus.None, _statusByEntity[_receiver.EntityId].ConvoStatusId);

            CompleteObjective(_giver, 4);
            Drain();
            CaptureStatuses();
            Assert.AreEqual(ConversationStatus.MissionComplete, _statusByEntity[_receiver.EntityId].ConvoStatusId);
            CollectionAssert.AreEqual(new List<uint> { MissionId }, _statusByEntity[_receiver.EntityId].Data);

            CompleteMission();
            Drain();
            CaptureStatuses();
            Assert.IsTrue(_statusByEntity.Values.All(s => s.ConvoStatusId == ConversationStatus.None));
        }

        [TestMethod]
        public void ConverseOffersOnlyTopicsThePlayerCanUse()
        {
            _npcs.RequestNpcConverse(_client, new RequestNPCConversePacket { EntityId = _giver.EntityId });
            var offer = Drain().OfType<ConversePacket>().Single();
            var dispense = (Dictionary<uint, MissionInfo>)offer.ConvoDataDict[ConversationType.MissionDispense];
            Assert.AreEqual(MissionId, dispense.Keys.Single());
            Assert.AreEqual(5u, dispense[MissionId].ObjectivesList.Single().ObjectiveId);
            Assert.IsFalse(offer.ConvoDataDict.ContainsKey(ConversationType.ObjectiveComplete));

            Accept();
            Drain();
            _npcs.RequestNpcConverse(_client, new RequestNPCConversePacket { EntityId = _scout.EntityId });
            var talk = Drain().OfType<ConversePacket>().Single();
            var objectives = (List<CompleteableObjectives>)talk.ConvoDataDict[ConversationType.ObjectiveComplete];
            Assert.AreEqual(((int)MissionId, 5, 1), (objectives.Single().MissionId, objectives.Single().ObjectiveId, objectives.Single().PlayerFlagId));
            Assert.IsFalse(talk.ConvoDataDict.ContainsKey(ConversationType.MissionDispense));

            _npcs.RequestNpcConverse(_client, new RequestNPCConversePacket { EntityId = _giver.EntityId });
            var again = Drain().OfType<ConversePacket>().Single();
            Assert.IsFalse(again.ConvoDataDict.ContainsKey(ConversationType.MissionDispense));

            // The Converse dictionary must serialize with the objective triples under key 6.
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            talk.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(1, reader.ReadTuple());
            Assert.AreEqual(1, reader.ReadDictionary());
            Assert.AreEqual((int)ConversationType.ObjectiveComplete, reader.ReadInt());
            Assert.AreEqual(1, reader.ReadList());
            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual((int)MissionId, reader.ReadInt());
            Assert.AreEqual(5, reader.ReadInt());
            Assert.AreEqual(1, reader.ReadInt());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void AcceptanceIsRefusedForWrongNpcRangeDuplicatesUnknownMissionsAndFullLogs()
        {
            Accept(_receiver);
            Accept(missionId: 9999);
            _client.Player.Position = new Vector3(120f, 10f, 100f);
            Accept();
            Assert.IsFalse(Drain().OfType<MissionGainedPacket>().Any());
            Assert.IsNull(Saved().Mission);
            Assert.AreEqual(0, _client.Player.Missions.Count);

            _client.Player.Position = new Vector3(100f, 10f, 100f);
            _client.State = ClientState.CharacterSelection;
            Accept();
            Assert.IsFalse(Drain().OfType<MissionGainedPacket>().Any());
            _client.State = ClientState.Ingame;

            // Thirty active missions the client can see fill the log.
            for (var i = 1u; i <= MissionRules.MaxMissionCount; i++)
            {
                _missions.LoadedMissions[8000 + i] = new Mission(new NpcMissionEntry { Id = 8000 + i, Comment = "log filler" });
                _client.Player.Missions[8000 + i] = new PlayerMission { MissionId = 8000 + i, State = MissionState.Active };
            }
            Accept();
            Assert.IsFalse(Drain().OfType<MissionGainedPacket>().Any());
            Assert.IsNull(Saved().Mission);

            // Saved missions without a definition are not sent to the client, so they do not count.
            for (var i = 1u; i <= MissionRules.MaxMissionCount; i++)
                _missions.LoadedMissions.Remove(8000 + i);
            Accept();
            Assert.AreEqual(1, Drain().OfType<MissionGainedPacket>().Count());
            Assert.IsNotNull(Saved().Mission);

            Accept();
            Assert.IsFalse(Drain().OfType<MissionGainedPacket>().Any());
            Assert.AreEqual(MissionRules.MaxMissionCount + 1, _client.Player.Missions.Count);
        }

        [TestMethod]
        public void ObjectivesCompleteOnlyThroughTheirBoundPackageAndRevealTheNext()
        {
            Accept();
            Drain();

            CompleteObjective(_giver, 5);            // wrong package for objective 5
            CompleteObjective(_scout, 5, flag: 2);   // wrong player flag
            CompleteObjective(_scout, 4);            // not revealed yet
            _client.Player.Position = new Vector3(100f, 10f, 120f);
            CompleteObjective(_scout, 5);            // out of range
            _client.Player.Position = new Vector3(100f, 10f, 100f);
            Assert.IsFalse(Drain().OfType<ObjectiveCompletedPacket>().Any());
            Assert.AreEqual(MissionObjectiveState.Incomplete, _client.Player.Missions[MissionId].Objectives[5]);

            _now += 60;
            CompleteObjective(_scout, 5);
            var packets = Drain();
            var completed = packets.OfType<ObjectiveCompletedPacket>().Single();
            Assert.AreEqual((MissionId, 5u), (completed.MissionId, completed.ObjectiveId));
            var revealed = packets.OfType<ObjectiveRevealedPacket>().Single();
            Assert.AreEqual(4u, revealed.ObjectiveId);
            CollectionAssert.AreEqual(new uint[] { 5, 4 }, revealed.MissionInfo.ObjectivesList.Select(o => o.ObjectiveId).ToArray());
            CollectionAssert.AreEqual(new[] { (uint)MissionObjectiveState.Completed, (uint)MissionObjectiveState.Incomplete },
                revealed.MissionInfo.ObjectivesList.Select(o => o.ObjectiveStatus).ToArray());
            Assert.AreEqual((int)_now, revealed.MissionInfo.ChangeTime);
            Assert.IsFalse(revealed.MissionInfo.Completeable);
            Assert.IsFalse(packets.OfType<MissionCompleteablePacket>().Any());

            var (mission, objectives, _) = Saved();
            Assert.AreEqual(_now, mission.ChangeTime);
            CollectionAssert.AreEqual(new uint[] { 4, 5 }, objectives.Select(o => o.ObjectiveId).ToArray());
            CollectionAssert.AreEqual(new[] { (uint)MissionObjectiveState.Incomplete, (uint)MissionObjectiveState.Completed }, objectives.Select(o => o.Status).ToArray());

            CompleteObjective(_scout, 5);            // already completed
            Assert.IsFalse(Drain().OfType<ObjectiveCompletedPacket>().Any());

            CompleteObjective(_giver, 4);
            packets = Drain();
            Assert.AreEqual(4u, packets.OfType<ObjectiveCompletedPacket>().Single().ObjectiveId);
            var completeable = packets.OfType<MissionCompleteablePacket>().Single();
            Assert.AreEqual(((int)MissionId, true), (completeable.MissionId, completeable.IsCompleteable));
            Assert.IsFalse(packets.OfType<ObjectiveRevealedPacket>().Any());
        }

        [TestMethod]
        public void TurnInPaysExactlyOnceAndPersistsCompletionWithTheReward()
        {
            Accept();
            CompleteMission();                       // nothing completed yet
            CompleteObjective(_scout, 5);
            CompleteObjective(_giver, 4);
            Drain();
            CompleteMission(_giver);                 // wrong NPC
            _client.Player.Position = new Vector3(100f, 10f, 120f);
            CompleteMission();                       // out of range
            _client.Player.Position = new Vector3(100f, 10f, 100f);
            Assert.IsFalse(Drain().OfType<MissionCompletedPacket>().Any());
            Assert.AreEqual(40, _client.Player.Credits[CurencyType.Credits]);

            _now += 30;
            CompleteMission();
            var packets = Drain();
            Assert.AreEqual(MissionId, packets.OfType<MissionCompletedPacket>().Single().MissionId);
            Assert.AreEqual(MissionId, packets.OfType<MissionRewardedPacket>().Single().MissionId);
            var credits = packets.OfType<UpdateCreditsPacket>().Single();
            Assert.AreEqual((CurencyType.Credits, 290), (credits.Type, credits.Amount));
            Assert.AreEqual(1, packets.OfType<ExperienceChangedPacket>().Count());
            Assert.AreEqual(290, _client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(1005u, _client.Player.Experience);
            Assert.AreEqual(MissionState.Completed, _client.Player.Missions[MissionId].State);

            var (mission, _, character) = Saved();
            Assert.AreEqual((uint)MissionState.Completed, mission.MissionState);
            Assert.AreEqual(_now, mission.ChangeTime);
            Assert.AreEqual(290, character.Credit);
            Assert.AreEqual(1005u, character.Experience);
            Assert.AreEqual(0, character.Prestige);

            CompleteMission();                       // replay
            Accept();                                // completed missions are not re-offered
            packets = Drain();
            Assert.IsFalse(packets.OfType<MissionRewardedPacket>().Any());
            Assert.IsFalse(packets.OfType<MissionGainedPacket>().Any());
            Assert.AreEqual(290, Saved().Character.Credit);
        }

        private ItemTemplate RegisterTemplate(uint templateId, EntityClasses classId, InventoryCategory category = InventoryCategory.Misc, int quality = 3)
        {
            _oldClasses[classId] = EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(classId, out var old) ? old : null;
            _oldTemplates[templateId] = ItemManager.Instance.ItemTemplateItemClass.TryGetValue(templateId, out var previous) ? previous : (EntityClasses?)null;
            var itemClass = new EntityClass((uint)classId, "reward fixture", 0, 0, new List<AugmentationType>(), false)
                { ItemClassInfo = new ItemClassInfo(new ItemClassEntry { MaxHitPoints = 1, StackSize = 10 }) };
            var template = new ItemTemplate(new ItemTemplateItemClassEntry { ItemTemplateId = templateId, ItemClass = (uint)classId })
                { InventoryCategory = category, QualityId = quality };
            itemClass.ItemTemplates.Add(templateId, template);
            EntityClassManager.Instance.LoadedEntityClasses[classId] = itemClass;
            ItemManager.Instance.ItemTemplateItemClass[templateId] = classId;
            return template;
        }

        private readonly Dictionary<EntityClasses, EntityClass> _oldClasses = new();
        private readonly Dictionary<uint, EntityClasses?> _oldTemplates = new();

        private void RestoreTemplates()
        {
            foreach (var pair in _oldClasses)
                if (pair.Value == null) EntityClassManager.Instance.LoadedEntityClasses.Remove(pair.Key);
                else EntityClassManager.Instance.LoadedEntityClasses[pair.Key] = pair.Value;
            foreach (var pair in _oldTemplates)
                if (pair.Value.HasValue) ItemManager.Instance.ItemTemplateItemClass[pair.Key] = pair.Value.Value;
                else ItemManager.Instance.ItemTemplateItemClass.Remove(pair.Key);
        }

        [TestMethod]
        public void ItemRewardsAreValidatedForDisplayButWithheldUntilDeliveryIsAtomic()
        {
            RegisterTemplate(900501, (EntityClasses)900601, quality: 4);
            RegisterTemplate(900502, (EntityClasses)900602);
            RegisterTemplate(900503, (EntityClasses)900603, category: 0);
            try
            {
                Accept();
                CompleteObjective(_scout, 5);
                CompleteObjective(_giver, 4);
                Drain();

                var definition = _missions.LoadedMissions[MissionId];
                definition.Rewards.Add(new NpcMissionRewardEntry { Id = MissionId, Type = (byte)NpcMissionRewardType.FixedItem, ItemTemplateId = 900501, Quantity = 1 });
                definition.Rewards.Add(new NpcMissionRewardEntry { Id = MissionId, Type = (byte)NpcMissionRewardType.SelectableItem, ItemTemplateId = 900502, Quantity = 10 });
                MissionManager.BuildRewardInfo(definition);
                CollectionAssert.AreEqual(new[] { "item reward delivery is not implemented" }, definition.DefinitionGaps());
                var fixedItem = definition.MissionConstantData.RewardInfo.FixedReward.FixedItems.Single();
                Assert.AreEqual((900501u, (EntityClasses)900601, 1u, 4), (fixedItem.ItemTemplateId, fixedItem.Class, fixedItem.Quantity, fixedItem.QualityId));
                CollectionAssert.AreEqual(new uint[] { 900502 }, definition.MissionConstantData.RewardInfo.SelectableReward.Select(r => r.ItemTemplateId).ToArray());
                CollectionAssert.AreEqual(new uint[] { 900502 }, definition.OfferedSelectableRewards.Select(r => r.ItemTemplateId).ToArray());

                // A mission already in the log does not pay out once its definition is incomplete.
                CompleteMission(selection: 0);
                CompleteMission();
                Assert.IsFalse(Drain().OfType<MissionCompletedPacket>().Any());
                Assert.AreEqual((uint)MissionState.Active, Saved().Mission.MissionState);
                Assert.AreEqual(40, Saved().Character.Credit);

                definition.Rewards.Add(new NpcMissionRewardEntry { Id = MissionId, Type = (byte)NpcMissionRewardType.SelectableItem, ItemTemplateId = 900599, Quantity = 1 });
                definition.Rewards.Add(new NpcMissionRewardEntry { Id = MissionId, Type = (byte)NpcMissionRewardType.FixedItem, ItemTemplateId = 900503, Quantity = 1 });
                definition.Rewards.Add(new NpcMissionRewardEntry { Id = MissionId, Type = (byte)NpcMissionRewardType.FixedItem, ItemTemplateId = 900501, Quantity = 11 });
                MissionManager.BuildRewardInfo(definition);
                CollectionAssert.AreEqual(new[]
                {
                    "item reward delivery is not implemented",
                    "reward item template 900599 is unknown",
                    "reward item template 900503 has no inventory placement",
                    "reward item template 900501 quantity 11 is outside 1..10"
                }, definition.RewardGaps);
                // Whatever is shown stays aligned with what a selection index would resolve to.
                CollectionAssert.AreEqual(definition.MissionConstantData.RewardInfo.SelectableReward.Select(r => r.ItemTemplateId).ToArray(),
                    definition.OfferedSelectableRewards.Select(r => r.ItemTemplateId).ToArray());
            }
            finally
            {
                RestoreTemplates();
            }
        }

        [TestMethod]
        public void SavedMissionsWithNothingRequiredNeverBecomeCompleteable()
        {
            Accept();
            Drain();
            var definition = _missions.LoadedMissions[MissionId];
            foreach (var objective in definition.Objectives.Values)
                objective.IsRequired = false;
            CollectionAssert.AreEqual(new[] { "no required objective" }, definition.DefinitionGaps());
            Assert.IsFalse(_client.Player.Missions[MissionId].IsCompleteable(definition));

            CaptureStatuses();
            Assert.AreNotEqual(ConversationStatus.MissionComplete, _statusByEntity[_receiver.EntityId].ConvoStatusId);
            CompleteMission();
            Assert.IsFalse(Drain().OfType<MissionCompletedPacket>().Any());
            Assert.AreEqual(40, Saved().Character.Credit);
        }

        [TestMethod]
        public void RadioAndShareRequestsDecodeWithoutChangingState()
        {
            foreach (var (opcode, type) in new[]
            {
                (GameOpcode.AssignRadioMission, typeof(AssignRadioMissionPacket)),
                (GameOpcode.AssignSharedMission, typeof(AssignSharedMissionPacket)),
                (GameOpcode.CompleteRadioMission, typeof(CompleteRadioMissionPacket)),
                (GameOpcode.DeclineSharedMission, typeof(DeclineSharedMissionPacket)),
                (GameOpcode.RewardRadioMission, typeof(RewardRadioMissionPacket)),
                (GameOpcode.ShareMission, typeof(ShareMissionPacket))
            })
                Assert.AreEqual(type, Client.GetPacketType(opcode));

            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, System.Text.Encoding.UTF8, true)))
            {
                writer.WriteTuple(3);
                writer.WriteUInt(429);
                writer.WriteInt(2);
                writer.WriteNoneStruct();
                writer.WriteTuple(2);
                writer.WriteULong(0x1234);
                writer.WriteUInt(429);
            }
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            new CompleteRadioMissionPacket().Read(reader);
            new AssignSharedMissionPacket().Read(reader);
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void CurrencyRewardsAreShownAndPaidFromOneNormalizedValue()
        {
            var definition = _missions.LoadedMissions[MissionId];
            Assert.AreEqual((250L, 0L, 1000L), (definition.RewardCredits, definition.RewardPrestige, definition.RewardExperience));
            Assert.AreEqual(250u, definition.MissionConstantData.RewardInfo.FixedReward.Credits[CurencyType.Credits]);

            var duplicate = Definition();
            duplicate.Rewards.Add(new NpcMissionRewardEntry { Id = MissionId, Type = (byte)NpcMissionRewardType.Credits, Credits = 50 });
            MissionManager.BuildRewardInfo(duplicate);
            CollectionAssert.AreEqual(new[] { "more than one Credits reward row" }, duplicate.DefinitionGaps());
            Assert.AreEqual(250u, duplicate.MissionConstantData.RewardInfo.FixedReward.Credits[CurencyType.Credits]);
            Assert.AreEqual(250L, duplicate.RewardCredits);

            var negative = Definition();
            negative.Rewards.Clear();
            negative.Rewards.Add(new NpcMissionRewardEntry { Id = MissionId, Type = (byte)NpcMissionRewardType.Prestige, Credits = -20 });
            negative.Rewards.Add(new NpcMissionRewardEntry { Id = MissionId, Type = 9 });
            MissionManager.BuildRewardInfo(negative);
            CollectionAssert.AreEqual(new[] { "Prestige reward amount -20 is not positive", "unknown reward type 9" }, negative.DefinitionGaps());
            Assert.AreEqual(0L, negative.RewardPrestige);
            Assert.IsFalse(negative.MissionConstantData.RewardInfo.FixedReward.Credits.ContainsKey(CurencyType.Prestige));

            // A payout that would overflow the balance is refused before anything is committed.
            Accept();
            CompleteObjective(_scout, 5);
            CompleteObjective(_giver, 4);
            Drain();
            _client.Player.Credits[CurencyType.Credits] = int.MaxValue - 100;
            CompleteMission();
            Assert.IsFalse(Drain().OfType<MissionCompletedPacket>().Any());
            Assert.AreEqual(MissionState.Active, _client.Player.Missions[MissionId].State);
            Assert.AreEqual((uint)MissionState.Active, Saved().Mission.MissionState);
            Assert.AreEqual(40, Saved().Character.Credit);
        }

        [TestMethod]
        public void UnimplementedRadioAndShareFlagsKeepDefinitionsUnoffered()
        {
            var radio = Definition();
            radio.MissionConstantData.RadioCompletable = true;
            CollectionAssert.AreEqual(new[] { "radio completion is not implemented" }, radio.DefinitionGaps());
            var shared = Definition();
            shared.MissionConstantData.Shareable = true;
            CollectionAssert.AreEqual(new[] { "mission sharing is not implemented" }, shared.DefinitionGaps());
        }

        [TestMethod]
        public void CompleteabilityIsAnnouncedBeforeARevealReplacesTheEntry()
        {
            var definition = _missions.LoadedMissions[MissionId];
            definition.Objectives[4].IsRequired = false;   // 5 is the last required objective and reveals optional 4
            Accept();
            Drain();
            CompleteObjective(_scout, 5);
            var packets = Drain();
            var completed = packets.FindIndex(p => p is ObjectiveCompletedPacket);
            var completeable = packets.FindIndex(p => p is MissionCompleteablePacket);
            var revealed = packets.FindIndex(p => p is ObjectiveRevealedPacket);
            Assert.IsTrue(completed >= 0 && completed < completeable && completeable < revealed, $"{completed} {completeable} {revealed}");
            Assert.IsTrue(((ObjectiveRevealedPacket)packets[revealed]).MissionInfo.Completeable);

            // Completing the optional objective does not announce completeability again.
            CompleteObjective(_giver, 4);
            packets = Drain();
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count());
            Assert.IsFalse(packets.OfType<MissionCompleteablePacket>().Any());
        }

        [TestMethod]
        public void ChangedDefinitionsRevealMissingObjectivesForSavedProgress()
        {
            Accept();
            CompleteObjective(_scout, 5);
            Drain();
            var definition = _missions.LoadedMissions[MissionId];
            definition.Objectives[6] = new MissionObjectiveDefinition { ObjectiveId = 6, Ordinal = 3, IsRequired = true, RevealedOnAccept = true };
            definition.Objectives[7] = new MissionObjectiveDefinition { ObjectiveId = 7, Ordinal = 4, IsRequired = true, RevealedOnAccept = false };
            definition.ObjectiveConversations.Add(new MissionObjectiveConversation { ObjectiveId = 6, NpcPackageId = ScoutPackage, PlayerFlagId = 1 });
            definition.ObjectiveConversations.Add(new MissionObjectiveConversation { ObjectiveId = 7, NpcPackageId = ScoutPackage, PlayerFlagId = 1 });
            definition.Transitions[5].Add(7);

            _missions.SendMissionStatusInfo(_client);
            var addressed = DrainAddressed();
            var info = addressed.Select(entry => entry.Packet).OfType<MissionStatusInfoPacket>().Single().MissionStatusDict[MissionId];
            // The scout was introduced before reconciliation; its marker is refreshed for objectives 6 and 7.
            var scoutStatus = addressed.Where(entry => entry.EntityId == _scout.EntityId).Select(entry => entry.Packet).OfType<NPCConversationStatusPacket>().Single();
            Assert.AreEqual(ConversationStatus.ObjectivComplete, scoutStatus.ConvoStatusId);
            CollectionAssert.AreEqual(new uint[] { 5, 4, 6, 7 }, info.ObjectivesList.Select(o => o.ObjectiveId).ToArray());
            CollectionAssert.AreEqual(new uint[] { 4, 5, 6, 7 }, Saved().Objectives.Select(o => o.ObjectiveId).ToArray());
            Assert.AreEqual((uint)MissionObjectiveState.Incomplete, Saved().Objectives.Single(o => o.ObjectiveId == 7).Status);

            _missions.SendMissionStatusInfo(_client);
            Drain();
            Assert.AreEqual(4, Saved().Objectives.Count);
        }

        [TestMethod]
        public void ArrivingDropshipPassengersCanTalkButDepartingOnesCannot()
        {
            _client.State = ClientState.Teleporting;
            _client.LoadingMap = MapId;
            _map.ClientList.Add(_client);
            _npcs.RequestNpcConverse(_client, new RequestNPCConversePacket { EntityId = _giver.EntityId });
            Assert.AreEqual(1, Drain().OfType<ConversePacket>().Count());

            _client.LoadingMap = 1985;
            _npcs.RequestNpcConverse(_client, new RequestNPCConversePacket { EntityId = _giver.EntityId });
            Accept();
            Assert.IsFalse(Drain().Any());

            // The GM teleport leaves Disconected set on a player it puts back in game.
            _client.State = ClientState.Ingame;
            _client.Player.Disconected = true;
            _npcs.RequestNpcConverse(_client, new RequestNPCConversePacket { EntityId = _giver.EntityId });
            Assert.AreEqual(1, Drain().OfType<ConversePacket>().Count());

            _client.State = ClientState.CharacterSelection;
            _npcs.RequestNpcConverse(_client, new RequestNPCConversePacket { EntityId = _giver.EntityId });
            Assert.IsFalse(Drain().Any());
        }

        [TestMethod]
        public void ReconnectRestoresTheSavedLogThroughMissionStatusInfo()
        {
            Accept();
            CompleteObjective(_scout, 5);
            Drain();

            using var context = WeaponReloadPersistenceTests.Context(_connection);
            var reloaded = new MissionManager(_factory, () => _now);
            reloaded.LoadedMissions[MissionId] = _missions.LoadedMissions[MissionId];
            var restored = reloaded.LoadPlayerMissions(new CharacterMissionRepository(context), AccountId, Slot, CharacterId);
            Assert.AreEqual(1, restored.Count);
            Assert.AreEqual(MissionState.Active, restored[MissionId].State);
            Assert.AreEqual(MissionObjectiveState.Completed, restored[MissionId].Objectives[5]);
            Assert.AreEqual(MissionObjectiveState.Incomplete, restored[MissionId].Objectives[4]);

            var original = _client.Player.Missions;
            _client.Player.Missions = restored;
            reloaded.SendMissionStatusInfo(_client);
            var status = Drain().OfType<MissionStatusInfoPacket>().Single();
            var info = status.MissionStatusDict[MissionId];
            Assert.AreEqual(MissionState.Active, info.MissionState);
            Assert.IsFalse(info.Completeable);
            CollectionAssert.AreEqual(new uint[] { 5, 4 }, info.ObjectivesList.Select(o => o.ObjectiveId).ToArray());
            CollectionAssert.AreEqual(new uint[] { 1, 2 }, info.ObjectivesList.Select(o => o.Ordinal).ToArray());
            Assert.AreEqual(original[MissionId].ChangeTime, restored[MissionId].ChangeTime);
        }

        [TestMethod]
        public void AbandonRemovesTheMissionUnlessTheClientForbidsIt()
        {
            Accept();
            Drain();
            _npcs.AbandonMission(_client, new AbandonMissionPacket { MissionId = MissionId });
            Assert.AreEqual(MissionId, Drain().OfType<MissionDiscardedPacket>().Single().MissionId);
            Assert.IsFalse(_client.Player.Missions.ContainsKey(MissionId));
            var (mission, objectives, _) = Saved();
            Assert.IsNull(mission);
            Assert.AreEqual(0, objectives.Count);

            Accept();
            Assert.AreEqual(1, Drain().OfType<MissionGainedPacket>().Count());

            foreach (var fixedId in MissionRules.NonAbandonableMissions)
            {
                _client.Player.Missions[fixedId] = new PlayerMission { MissionId = fixedId, State = MissionState.Active };
                _npcs.AbandonMission(_client, new AbandonMissionPacket { MissionId = fixedId });
                Assert.IsTrue(_client.Player.Missions.ContainsKey(fixedId));
            }
            Assert.IsFalse(Drain().OfType<MissionDiscardedPacket>().Any());
        }

        [TestMethod]
        public void SaveFailuresLeaveMemoryAndClientUnchanged()
        {
            _factory.FailNextComplete = true;
            Accept();
            Assert.IsFalse(Drain().OfType<MissionGainedPacket>().Any());
            Assert.AreEqual(0, _client.Player.Missions.Count);
            Assert.IsNull(Saved().Mission);

            Accept();
            Drain();
            _factory.FailNextComplete = true;
            CompleteObjective(_scout, 5);
            Assert.IsFalse(Drain().OfType<ObjectiveCompletedPacket>().Any());
            Assert.AreEqual(MissionObjectiveState.Incomplete, _client.Player.Missions[MissionId].Objectives[5]);
            Assert.AreEqual((uint)MissionObjectiveState.Incomplete, Saved().Objectives.Single().Status);

            CompleteObjective(_scout, 5);
            CompleteObjective(_giver, 4);
            Drain();
            _factory.FailNextComplete = true;
            CompleteMission();
            Assert.IsFalse(Drain().OfType<MissionCompletedPacket>().Any());
            Assert.AreEqual(40, _client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(MissionState.Active, _client.Player.Missions[MissionId].State);
            Assert.AreEqual(40, Saved().Character.Credit);
        }

        [TestMethod]
        public void IncompleteDefinitionsAreNeitherAdvertisedNorAccepted()
        {
            // Mirrors the unvalidated 321/429 seeds: giver and receiver but no objective data.
            var bare = new Mission(new NpcMissionEntry { Id = 429, GiverId = GiverDbId, ReciverId = ReceiverDbId, Level = 3, GroupType = 2, CategoryId = 2, Comment = "River Recon" });
            _missions.LoadedMissions.Clear();
            _missions.LoadedMissions[429] = bare;
            CollectionAssert.AreEqual(new[] { "no objectives" }, bare.DefinitionGaps());

            CaptureStatuses();
            Assert.IsTrue(_statusByEntity.Values.All(s => s.ConvoStatusId == ConversationStatus.None));
            _npcs.RequestNpcConverse(_client, new RequestNPCConversePacket { EntityId = _giver.EntityId });
            Assert.IsFalse(Drain().OfType<ConversePacket>().Single().ConvoDataDict.ContainsKey(ConversationType.MissionDispense));
            Accept(missionId: 429);
            Assert.IsFalse(Drain().OfType<MissionGainedPacket>().Any());
            Assert.AreEqual(0, _client.Player.Missions.Count);

            var unbound = Definition();
            unbound.ObjectiveConversations.RemoveAll(c => c.ObjectiveId == 4);
            CollectionAssert.AreEqual(new[] { "objective 4 has no completion binding" }, unbound.DefinitionGaps());
            var unreachable = Definition();
            unreachable.Transitions.Clear();
            CollectionAssert.AreEqual(new[] { "required objective 4 is never revealed" }, unreachable.DefinitionGaps());
            Assert.IsTrue(Definition().IsDispensable);
        }

        [TestMethod]
        public void PlayerFlagsAreSentAsAList()
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            new PlayerFlagsPacket(new List<uint> { 1, 7 }).Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(1, reader.ReadTuple());
            Assert.AreEqual(2, reader.ReadList());
            Assert.AreEqual(1u, reader.ReadUInt());
            Assert.AreEqual(7u, reader.ReadUInt());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow(0)]
        [DataRow(2)]
        public void CompleteNpcMissionReadsRewardSelectionAsOptionalInteger(int? selection)
        {
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, System.Text.Encoding.UTF8, true)))
            {
                writer.WriteTuple(4);
                writer.WriteULong(0x1234);
                writer.WriteUInt(429);
                if (selection.HasValue) writer.WriteInt(selection.Value); else writer.WriteNoneStruct();
                writer.WriteNoneStruct();
            }
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            var packet = new CompleteNPCMissionPacket();
            packet.Read(reader);
            Assert.AreEqual((0x1234UL, 429u), (packet.EntityId, packet.MissionId));
            Assert.AreEqual(selection, packet.SelectionIdx);
            Assert.IsNull(packet.Rating);
            Assert.AreEqual(stream.Length, stream.Position);
        }

        private sealed class ContentReferences : IContentReferences
        {
            public bool MapContextExists(uint mapContextId) => true;
            public bool MissionExists(uint missionId) => true;
            public bool ObjectiveExists(uint missionId, uint objectiveId) => true;
            public uint MissionGiver(uint missionId) => GiverDbId;
            public bool MissionOfferable(uint missionId) => true;
            public bool CreatureExists(uint creatureId) => true;
            public bool EntityClassExists(uint entityClassId) => true;
            public bool ItemTemplateExists(uint itemTemplateId) => true;
            public bool LogosExists(uint logosId) => true;
            public bool HasLegacyWorldObjects(uint mapContextId) => false;
        }

        private const uint EquipMissionId = 7002;
        private const uint EquipTemplateId = 123001;
        private Item _equippedItem;

        // Objective 2 of mission 7002 completes through an Equip content binding
        // matching any equipped item; no conversation is bound.
        private static Mission EquipDefinition()
        {
            var mission = new Mission(new NpcMissionEntry
            {
                Id = EquipMissionId, GiverId = GiverDbId, ReciverId = ReceiverDbId, Level = 1, GroupType = 1, CategoryId = 10000032, Comment = "equip fixture"
            });
            mission.Objectives[2] = new MissionObjectiveDefinition { ObjectiveId = 2, Ordinal = 1, IsRequired = true, RevealedOnAccept = true };
            mission.RefreshDispenseObjectives();
            return mission;
        }

        private MissionContentManager LoadEquipContent()
        {
            using (var context = WorldContext(_worldConnection))
            {
                context.Database.EnsureCreated();
                context.NpcMissionObjectiveBindingEntries.Add(new NpcMissionObjectiveBindingEntry
                    { MissionId = EquipMissionId, ObjectiveId = 2, BindingId = 0, Kind = (byte)ObjectiveBindingKind.Equip, EquipMatch = 0, CounterId = 255 });
                context.SaveChanges();
            }

            var content = new MissionContentManager(_factory) { Missions = _missions };
            content.Load(() => new BootcampConfig(), new ContentReferences(), _missions.LoadedMissions);
            _missions.Content = content;
            return content;
        }

        [TestMethod]
        public void EquipBindingCompletesAnObjectiveForTheCurrentlyEquippedItem()
        {
            _missions.LoadedMissions[EquipMissionId] = EquipDefinition();
            var content = LoadEquipContent();

            Accept(missionId: EquipMissionId);
            Assert.AreEqual(MissionObjectiveState.Incomplete, _client.Player.Missions[EquipMissionId].Objectives[2]);

            // Nothing equipped yet: the level-triggered check is a no-op.
            content.OnEquipCommitted(_client);
            Assert.AreEqual(MissionObjectiveState.Incomplete, _client.Player.Missions[EquipMissionId].Objectives[2]);

            _client.Player.Inventory.EquippedInventory.AddRange(new ulong[22]);
            _equippedItem = new Item { ItemTemplateId = EquipTemplateId };
            EntityManager.Instance.RegisterItem(_equippedItem.EntityId, _equippedItem);
            _client.Player.Inventory.EquippedInventory[1] = _equippedItem.EntityId;

            content.OnEquipCommitted(_client);

            Assert.AreEqual(MissionObjectiveState.Completed, _client.Player.Missions[EquipMissionId].Objectives[2]);
            var packets = Drain();
            Assert.IsTrue(packets.Any(packet => packet is ObjectiveCompletedPacket completed && completed.MissionId == EquipMissionId && completed.ObjectiveId == 2),
                "the objective completion was not announced");

            // Idempotent: a second check after the drain sends no duplicate completion.
            content.OnEquipCommitted(_client);
            Assert.AreEqual(0, Drain().Count(packet => packet is ObjectiveCompletedPacket));

            using var context = WeaponReloadPersistenceTests.Context(_connection);
            var row = context.CharacterMissionObjectiveEntries.Single(entry =>
                entry.CharacterId == CharacterId && entry.MissionId == EquipMissionId && entry.ObjectiveId == 2);
            Assert.AreEqual((uint)MissionObjectiveState.Completed, row.Status);
        }

        [TestMethod]
        public void EquipBindingSurvivesReconnectThroughReconciliation()
        {
            _missions.LoadedMissions[EquipMissionId] = EquipDefinition();
            var content = LoadEquipContent();

            Accept(missionId: EquipMissionId);
            Assert.AreEqual(MissionObjectiveState.Incomplete, _client.Player.Missions[EquipMissionId].Objectives[2]);

            // The item was equipped in a previous session: on the next world entry
            // SendMissionStatusInfo reconciles the saved log and the level-triggered
            // check completes the objective from what is worn, with no new equip.
            _client.Player.Inventory.EquippedInventory.AddRange(new ulong[22]);
            _equippedItem = new Item { ItemTemplateId = EquipTemplateId };
            EntityManager.Instance.RegisterItem(_equippedItem.EntityId, _equippedItem);
            _client.Player.Inventory.EquippedInventory[1] = _equippedItem.EntityId;

            _missions.SendMissionStatusInfo(_client);

            Assert.AreEqual(MissionObjectiveState.Completed, _client.Player.Missions[EquipMissionId].Objectives[2]);
        }

        private const uint CrateMissionId = 7003;
        private const uint CratePlacementId = 900650;
        private const uint CrateTemplateA = 123101;
        private const uint CrateTemplateB = 123102;
        private const ulong CrateObjectId = 0x5100;
        private DynamicObject _crateObject;

        // Objective 1 of mission 7003 completes through a loot_all binding on a
        // container placement holding two item-set rows.
        private static Mission CrateDefinition()
        {
            var mission = new Mission(new NpcMissionEntry
            {
                Id = CrateMissionId, GiverId = GiverDbId, ReciverId = ReceiverDbId, Level = 1, GroupType = 1, CategoryId = 10000032, Comment = "crate fixture"
            });
            mission.Objectives[1] = new MissionObjectiveDefinition { ObjectiveId = 1, Ordinal = 1, IsRequired = true, RevealedOnAccept = true };
            mission.RefreshDispenseObjectives();
            return mission;
        }

        private ItemManager _realItemManager;
        private object _realInventoryManager;

        private MissionContentManager LoadCrateContent()
        {
            // Item creation must write to this test's char database; the swap must
            // happen before RegisterTemplate so the templates land in the swapped manager.
            _realItemManager = typeof(ItemManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as ItemManager;
            var itemManager = typeof(ItemManager).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                new[] { typeof(IGameUnitOfWorkFactory) }, null).Invoke(new object[] { _factory });
            typeof(ItemManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, itemManager);
            var realInventoryManager = typeof(InventoryManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
            var inventoryManager = typeof(InventoryManager).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                new[] { typeof(IGameUnitOfWorkFactory) }, null).Invoke(new object[] { _factory });
            typeof(InventoryManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, inventoryManager);
            _realInventoryManager = realInventoryManager;
            _client.Player.Inventory.PersonalInventory.AddRange(new ulong[250]);
            typeof(Client).GetProperty("AccountEntry").SetValue(_client,
                new GameAccountEntry { Id = AccountId, Name = "Fixture", FamilyName = "Fixture", Email = "fixture@example.invalid" });
            RegisterTemplate(CrateTemplateA, (EntityClasses)900701);
            RegisterTemplate(CrateTemplateB, (EntityClasses)900702);
            using (var context = WorldContext(_worldConnection))
            {
                context.Database.EnsureCreated();
                context.NpcMissionObjectiveBindingEntries.Add(new NpcMissionObjectiveBindingEntry
                    { MissionId = CrateMissionId, ObjectiveId = 1, BindingId = 0, Kind = (byte)ObjectiveBindingKind.LootAll, PlacementId = CratePlacementId, CounterId = 255 });
                context.ContentPlacementEntries.Add(new ContentPlacementEntry
                {
                    Id = CratePlacementId, MapContextId = MapId, Kind = (byte)ContentPlacementKind.Usable,
                    EntityClassId = 26714, UsableKind = (byte)ContentUsableKind.Container,
                    Behavior = (byte)ContentPlacementBehavior.Stationary, InitialState = 200, LootItemSetId = 900500
                });
                context.ContentItemSetEntries.Add(new ContentItemSetEntry { ItemSetId = 900500, ItemTemplateId = CrateTemplateA, Quantity = 1 });
                context.ContentItemSetEntries.Add(new ContentItemSetEntry { ItemSetId = 900500, ItemTemplateId = CrateTemplateB, Quantity = 1 });
                context.SaveChanges();
            }

            var content = new MissionContentManager(_factory) { Missions = _missions };
            content.Load(() => new BootcampConfig(), new ContentReferences(), _missions.LoadedMissions);
            _missions.Content = content;
            return content;
        }

        private DynamicObject CrateObject(MissionContentManager content)
        {
            var validation = content.Content;
            var placement = validation.Catalog.Placements[CratePlacementId];
            var mapChannel = _client.Player.MapChannel ??= _map;
            var usable = new DynamicObject
            {
                EntityId = CrateObjectId,
                EntityClassId = (EntityClasses)placement.EntityClassId,
                Position = new Vector3(100f, 10f, 100f),
                MapContextId = MapId,
                DynamicObjectType = DynamicObjectType.ContentUsable,
                StateId = (UseObjectState)placement.InitialState
            };
            mapChannel.DynamicObjects.Add(usable);
            mapChannel.ContentUsables[usable.EntityId] = CratePlacementId;
            EntityManager.Instance.RegisterDynamicObject(usable);
            _crateObject = usable;
            return usable;
        }

        private void UseCrate(MissionContentManager content, DynamicObject usable)
        {
            var packet = new RequestUseObjectPacket { ActionId = ActionId.UseObject, ActionArgId = 1, EntityId = usable.EntityId };
            _client.Player.CurrentAction = (int)ActionId.UseObject;
            DynamicObjectManager.Instance.RequestUseObjectPacket(_client, packet);
            // The windup elapses: run the recovery for the queued action.
            var action = _client.Player.MapChannel.PerformRecovery.Single(entry => entry.Actor == _client.Player);
            action.PassedTime = action.WaitTime;
            ActorActionManager.Instance.PerformRecovery(_client.Player.MapChannel, action);
            _client.Player.MapChannel.PerformRecovery.Remove(action);
            _client.Player.CurrentAction = 0;
        }

        private List<LootItem> CrateLootItems()
        {
            var packets = DrainAddressed();
            var lootInfo = packets.Select(entry => entry.Packet).OfType<LootInfoPacket>().Single();
            return lootInfo.LootItems;
        }

        [TestMethod]
        public void LootAllFromContentContainerGrantsItemsAndCompletesTheObjective()
        {
            _missions.LoadedMissions[CrateMissionId] = CrateDefinition();
            var content = LoadCrateContent();
            var usable = CrateObject(content);

            Accept(missionId: CrateMissionId);
            Assert.AreEqual(MissionObjectiveState.Incomplete, _client.Player.Missions[CrateMissionId].Objectives[1]);

            UseCrate(content, usable);
            var lootItems = CrateLootItems();
            Assert.AreEqual(2, lootItems.Count);

            content.RequestLootAllFromContentContainer(_client, usable.EntityId);

            Assert.AreEqual(MissionObjectiveState.Completed, _client.Player.Missions[CrateMissionId].Objectives[1]);
            Assert.AreEqual(2, _client.Player.Inventory.PersonalInventory.Count(slot => slot != 0));
            var packets = Drain();
            Assert.IsTrue(packets.Any(packet => packet is ObjectiveCompletedPacket completed && completed.MissionId == CrateMissionId && completed.ObjectiveId == 1));

            using var context = WeaponReloadPersistenceTests.Context(_connection);
            var row = context.CharacterMissionObjectiveEntries.Single(entry =>
                entry.CharacterId == CharacterId && entry.MissionId == CrateMissionId && entry.ObjectiveId == 1);
            Assert.AreEqual((uint)MissionObjectiveState.Completed, row.Status);
        }

        [TestMethod]
        public void LootAllIsRefusedWhenTheInventoryCannotTakeEverything()
        {
            _missions.LoadedMissions[CrateMissionId] = CrateDefinition();
            var content = LoadCrateContent();
            var usable = CrateObject(content);

            Accept(missionId: CrateMissionId);
            UseCrate(content, usable);

            // Fill the Misc category (the crate templates' category): nothing can be granted.
            var fillerTemplate = RegisterTemplate(130000, (EntityClasses)900801);
            var filler = new Item { ItemTemplateId = 130000, ItemTemplate = fillerTemplate };
            EntityManager.Instance.RegisterItem(filler.EntityId, filler);
            for (var i = 200; i < 250; i++)
                _client.Player.Inventory.PersonalInventory[i] = filler.EntityId;

            content.RequestLootAllFromContentContainer(_client, usable.EntityId);

            Assert.AreEqual(MissionObjectiveState.Incomplete, _client.Player.Missions[CrateMissionId].Objectives[1]);
            Assert.AreEqual(0, _client.Player.Inventory.PersonalInventory.Count(slot => slot != 0 && slot != filler.EntityId));
        }

        [TestMethod]
        public void SecondLootAllIsRefused()
        {
            _missions.LoadedMissions[CrateMissionId] = CrateDefinition();
            var content = LoadCrateContent();
            var usable = CrateObject(content);

            Accept(missionId: CrateMissionId);
            UseCrate(content, usable);
            content.RequestLootAllFromContentContainer(_client, usable.EntityId);
            Assert.AreEqual(MissionObjectiveState.Completed, _client.Player.Missions[CrateMissionId].Objectives[1]);
            var afterFirst = _client.Player.Inventory.PersonalInventory.Count(slot => slot != 0);

            content.RequestLootAllFromContentContainer(_client, usable.EntityId);

            Assert.AreEqual(afterFirst, _client.Player.Inventory.PersonalInventory.Count(slot => slot != 0));
        }
    }
}
