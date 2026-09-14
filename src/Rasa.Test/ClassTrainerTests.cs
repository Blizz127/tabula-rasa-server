using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Context.Char;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Packets;
using Rasa.Packets.Communicator.Server;
using Rasa.Packets.Manifestation.Server;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Protocol;
using Rasa.Repositories.Char;
using Rasa.Repositories.Char.Character;
using Rasa.Repositories.UnitOfWork;
using Rasa.Repositories.World;
using Rasa.Structures;
using Rasa.Structures.Char;

namespace Rasa.Test
{
    /// <summary>
    /// Tier advancement (research/20260914-class-trainer): a Recruit is held at level 4 with its experience
    /// credited; reaching the gate sends PM 663, makes the tier selection pending and grants a clone credit once;
    /// the class trainer offers Train only at the gate; SelectNewCharacterClass near a trainer changes the class
    /// to an immediate child and releases the withheld level.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ClassTrainerTests
    {
        private const uint CharacterId = 101;

        public class UnitProxy : DispatchProxy
        {
            public SqliteCharContext Context;
            protected override object Invoke(MethodInfo method, object[] args)
            {
                switch (method.Name)
                {
                    case "get_Characters": return new CharacterRepository(Context);
                    case "Complete": Context.SaveChanges(); return null;
                    case "Dispose": Context.Dispose(); return null;
                    default: throw new NotSupportedException(method.Name);
                }
            }
        }

        private sealed class Factory : IGameUnitOfWorkFactory
        {
            private readonly SqliteConnection _connection;
            public Factory(SqliteConnection connection) => _connection = connection;
            public ICharUnitOfWork CreateChar()
            {
                var unit = DispatchProxy.Create<ICharUnitOfWork, UnitProxy>();
                ((UnitProxy)(object)unit).Context = WeaponReloadPersistenceTests.Context(_connection);
                return unit;
            }
            public IWorldUnitOfWork CreateWorld() => throw new NotSupportedException();
        }

        private SqliteConnection _connection;
        private Factory _factory;
        private CharacterManager _realCharacters;
        private Client _client;
        private MapChannel _map;
        private Creature _trainer;
        private Logger.LoggerConfig _oldLogger;

        [TestInitialize]
        public void Initialize()
        {
            _oldLogger = Logger.Config;
            if (_oldLogger == null) Logger.UpdateConfig(new Logger.LoggerConfig { LogToFile = false });
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            _factory = new Factory(_connection);
            using (var context = WeaponReloadPersistenceTests.Context(_connection))
            {
                context.Database.EnsureCreated();
                context.GameAccountEntries.Add(new GameAccountEntry { Id = 10, Name = "Fixture", FamilyName = "Fixture", Email = "fixture@example.invalid" });
                context.CharacterEntries.Add(new CharacterEntry
                {
                    Id = CharacterId, AccountId = 10, Slot = 1, Name = "Recruit", Level = 3, Experience = 10500, Class = 1,
                    MapContextId = 1220, CreatedAt = DateTime.UtcNow, LastPvPClan = DateTime.UtcNow
                });
                context.SaveChanges();
            }

            var field = typeof(CharacterManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            _realCharacters = (CharacterManager)field.GetValue(null);
            field.SetValue(null, Activator.CreateInstance(typeof(CharacterManager), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new object[] { _factory }, null));

            _map = new MapChannel { MapInfo = new MapInfo(1220, "wilderness", 1556, 0), ClientList = new List<Client>() };
            _client = new Client(_factory, new ClientPacketHandler()) { State = ClientState.Ingame };
            _client.Player = new Manifestation
            {
                Id = CharacterId, Level = 3, Experience = 10500, Class = 1, MapContextId = 1220, MapChannel = _map,
                Position = new Vector3(765.4f, 294.1f, 386.0f), Cells = new uint[,] { { 7 } }
            };
            foreach (Attributes attribute in Enum.GetValues(typeof(Attributes)))
                _client.Player.Attributes[attribute] = new ActorAttributes(attribute, 100, 100, 100, 0, 0);
            for (var slot = 0; slot < 22; slot++)
                _client.Player.Inventory.EquippedInventory.Add(0);
            _map.ClientList.Add(_client);
            _map.MapCellInfo.Cells[7] = new MapCell { ClientList = new List<Client> { _client } };

            _trainer = new Creature
            {
                DbId = 122000, MapContextId = 1220, MapChannel = _map, Position = new Vector3(765.4f, 294.1f, 390.0f), Level = 8,
                State = CharacterState.Idle, Cells = new uint[,] { { 7 } }, Npc = new Npc { NpcPackageId = 2588 }
            };
            EntityManager.Instance.RegisterEntity(_trainer.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(_trainer);
        }

        [TestCleanup]
        public void Cleanup()
        {
            typeof(CharacterManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, _realCharacters);
            EntityManager.Instance.UnregisterCreature(_trainer.EntityId);
            EntityManager.Instance.UnregisterEntity(_trainer.EntityId);
            if (_oldLogger == null) typeof(Logger).GetProperty(nameof(Logger.Config)).SetValue(null, null);
            _connection.Dispose();
        }

        private List<(ulong EntityId, PythonPacket Packet)> Drain()
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_client);
            var packets = new List<(ulong, PythonPacket)>();
            while (queue.PopOutgoing() is ProtocolPacket packet)
                if (packet.Message is CallMethodMessage message)
                    packets.Add((message.EntityId, message.Packet));
            return packets;
        }

        private CharacterEntry Saved()
        {
            using var context = WeaponReloadPersistenceTests.Context(_connection);
            return context.CharacterEntries.Single(c => c.Id == CharacterId);
        }

        private void Gain(uint experience)
        {
            _client.Player.Experience += experience;
            ManifestationManager.Instance.NotifyExperienceGained(_client, experience);
            // The fixture has no class data, so the level-up stat recalculation leaves no health; stay alive.
            _client.Player.Attributes[Attributes.Health].Current = 100;
        }

        [TestMethod]
        public void ARecruitIsHeldAtFourAndReachingTheGateAnnouncesItOnce()
        {
            // 10,500 + 33,500 = 44,000: past level 4 (24,000) and level 5 (43,000).
            Gain(33_500);
            var packets = Drain().Select(entry => entry.Packet).ToList();

            Assert.AreEqual(4, _client.Player.Level);
            Assert.AreEqual(44_000u, _client.Player.Experience);
            CollectionAssert.AreEqual(new byte[] { 4 }, packets.OfType<LevelUpPacket>().Select(p => p.Level).ToArray());
            var levelLine = packets.FindIndex(p => p is DisplayClientMessagePacket { MsgId: PlayerMessage.PmLevelIncreased });
            var gateLine = packets.FindIndex(p => p is DisplayClientMessagePacket { MsgId: PlayerMessage.PmCharacterClassesAvailable });
            Assert.IsTrue(levelLine >= 0 && levelLine < gateLine, $"{levelLine} {gateLine}");
            CollectionAssert.AreEqual(new uint[] { 2, 3 }, packets.OfType<AvailableCharacterClassesPacket>().Single().ClassIds.ToArray());
            Assert.AreEqual(GameOpcode.AvailableCharacterClasses, packets.OfType<AvailableCharacterClassesPacket>().Single().Opcode);
            Assert.AreEqual(1u, packets.OfType<CloneCreditsPacket>().Single().CloneCredits);
            Assert.AreEqual((4, 1u), ((int)Saved().Level, Saved().CloneCredits));

            // Experience keeps accruing while held, without another announcement or credit.
            Gain(5_000);
            packets = Drain().Select(entry => entry.Packet).ToList();
            Assert.AreEqual(4, _client.Player.Level);
            Assert.IsFalse(packets.OfType<LevelUpPacket>().Any() || packets.OfType<CloneCreditsPacket>().Any() ||
                           packets.OfType<DisplayClientMessagePacket>().Any(p => p.MsgId == PlayerMessage.PmCharacterClassesAvailable));
            Assert.AreEqual(1u, _client.Player.CloneCredits);
        }

        [TestMethod]
        public void TheTrainerOffersTrainingOnlyAtTheGate()
        {
            var npcs = new NpcManager(_factory, new MissionManager(_factory));

            TrainingConverse Training()
            {
                npcs.RequestNpcConverse(_client, new RequestNPCConversePacket { EntityId = _trainer.EntityId });
                return (TrainingConverse)Drain().Select(entry => entry.Packet).OfType<ConversePacket>().Single().ConvoDataDict[ConversationType.Training];
            }

            var early = Training();
            Assert.AreEqual((false, ClassAdvancement.DialogNotReady), (early.CanTrain, early.DialogId));

            Gain(33_500);
            Drain();
            var ready = Training();
            Assert.AreEqual((true, ClassAdvancement.DialogCanTrain), (ready.CanTrain, ready.DialogId));

            npcs.UpdateConversationStatus(_client, _trainer);
            Assert.AreEqual(ConversationStatus.Train, Drain().Select(entry => entry.Packet).OfType<NPCConversationStatusPacket>().Single().ConvoStatusId);

            Assert.AreEqual(ClassAdvancement.DialogFinalTier, ClassAdvancement.DialogFor(8, 49, 99_999_999));
        }

        [TestMethod]
        public void TrainingNearATrainerChangesTheClassAndReleasesTheWithheldLevel()
        {
            // Not at the gate yet.
            ManifestationManager.Instance.SelectNewCharacterClass(_client, 2);
            Assert.AreEqual(1u, _client.Player.Class);

            Gain(33_500);
            Drain();

            // A grandchild, or too far from the trainer, is refused.
            ManifestationManager.Instance.SelectNewCharacterClass(_client, 4);
            _client.Player.Position = new Vector3(700f, 294f, 386f);
            ManifestationManager.Instance.SelectNewCharacterClass(_client, 2);
            Assert.AreEqual((1u, (byte)4), (_client.Player.Class, _client.Player.Level));
            Assert.IsFalse(Drain().Any(entry => entry.Packet is CharacterClassPacket));

            _client.Player.Position = new Vector3(765.4f, 294.1f, 386.0f);
            ManifestationManager.Instance.SelectNewCharacterClass(_client, 2);
            var packets = Drain().Select(entry => entry.Packet).ToList();

            Assert.AreEqual((2u, (byte)5), (_client.Player.Class, _client.Player.Level));
            var classChange = packets.FindIndex(p => p is CharacterClassPacket);
            var levelUp = packets.FindIndex(p => p is LevelUpPacket);
            Assert.IsTrue(classChange >= 0 && classChange < levelUp, $"{classChange} {levelUp}");
            Assert.AreEqual(0, packets.OfType<AvailableCharacterClassesPacket>().Single().ClassIds.Count);
            var levelLine = packets.OfType<DisplayClientMessagePacket>().Single(p => p.MsgId == PlayerMessage.PmLevelIncreased);
            Assert.AreEqual("5", levelLine.Args["level"]);
            Assert.AreEqual((2u, (byte)5), (Saved().Class, Saved().Level));

            // Soldier is held at 14 next; training again now is refused.
            ManifestationManager.Instance.SelectNewCharacterClass(_client, 4);
            Assert.AreEqual(2u, _client.Player.Class);
        }
    }
}
