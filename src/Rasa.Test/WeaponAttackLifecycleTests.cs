using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Context.Char;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Packets;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.MapChannel.Server.PerformRecovery;
using Rasa.Packets.Protocol;
using Rasa.Repositories.Char;
using Rasa.Repositories.Char.Items;
using Rasa.Repositories.UnitOfWork;
using Rasa.Repositories.World;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.World;

namespace Rasa.Test
{
    [TestClass]
    [DoNotParallelize]
    public class WeaponAttackLifecycleTests
    {
        private sealed class Factory : IGameUnitOfWorkFactory
        {
            private readonly SqliteCharContext _context;
            public Factory(SqliteCharContext context) => _context = context;
            public ICharUnitOfWork CreateChar()
            {
                var work = DispatchProxy.Create<ICharUnitOfWork, WeaponActionLifecycleTests.UnitProxy>();
                ((WeaponActionLifecycleTests.UnitProxy)(object)work).Items = new ItemRepository(_context);
                return work;
            }
            public IWorldUnitOfWork CreateWorld() => throw new NotSupportedException();
        }

        private SqliteConnection _connection;
        private SqliteCharContext _context;
        private WeaponAttackManager _attacks;
        private ManifestationManager _manifestations;
        private Client _client;
        private Item _weapon;
        private Creature _target;
        private readonly List<Creature> _targets = new();
        private MapChannel _map;
        private long _now;
        private const EntityClasses WeaponClass = (EntityClasses)9000011;

        [TestInitialize]
        public void Initialize()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            _context = WeaponReloadPersistenceTests.Context(_connection);
            WeaponReloadPersistenceTests.Seed(_context);
            var factory = new Factory(_context);
            _now = 0;
            _attacks = new WeaponAttackManager(factory, () => _now);
            _manifestations = new ManifestationManager(factory, _attacks);
            _map = new MapChannel { MapInfo = new MapInfo(1220, "test", 1, 1), ClientList = new List<Client>() };
            _client = new Client(null, new ClientPacketHandler())
            {
                State = ClientState.Ingame,
                Player = new Manifestation { Id = 101, Level = 1, State = CharacterState.Normal,
                    MapContextId = 1220, MapChannel = _map, WeaponReady = true, Cells = new uint[1, 1] }
            };
            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(_client,
                new GameAccountEntry { Id = 10, SelectedSlot = 2 });
            _client.Player.Inventory.EquippedInventory.AddRange(new ulong[22]);
            _client.Player.Inventory.PersonalInventory.AddRange(new ulong[250]);
            _client.Player.Inventory.WeaponDrawer.AddRange(new ulong[5]);
            _map.ClientList.Add(_client);
            _map.MapCellInfo.Cells[0] = new MapCell { ClientList = new List<Client> { _client } };
            _weapon = new Item { Id = 1, OwnerId = 101, OwnerSlotId = 0, CurrentAmmo = 5,
                ItemTemplate = new ItemTemplate(new ItemTemplateItemClassEntry { ItemClass = (uint)WeaponClass })
                { WeaponInfo = new WeaponInfo(new ItemTemplateWeaponEntry { AmmoPerShot = 2, Refire = 1, ReloadTime = 1500 }) } };
            EntityManager.Instance.RegisterItem(_weapon.EntityId, _weapon);
            EntityManager.Instance.RegisterEntity(_weapon.EntityId, EntityType.Item);
            EntityClassManager.Instance.LoadedEntityClasses[WeaponClass] =
                new EntityClass((uint)WeaponClass, "test", 0, 0, new List<AugmentationType>(), false)
                { WeaponClassInfo = new WeaponClassInfo(new WeaponClassEntry { AttackActionId = 1,
                    AttackActionArgId = 203, AmmoClassId = 28, MinDamage = 60, MaxDamage = 60,
                    DamageType = 6, DrawActionId = 1, StowActionId = 1, ReloadActionId = 1, ClipSize = 30 }) };
            _client.Player.Inventory.EquippedInventory[13] = _weapon.EntityId;
            _client.Player.Inventory.WeaponDrawer[0] = _weapon.EntityId;
            _target = MakeTarget();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _client.Player.MapChannel = _map;
            _manifestations.StopAutoFire(_client);
            EntityManager.Instance.UnregisterItem(_weapon.EntityId);
            EntityManager.Instance.UnregisterEntity(_weapon.EntityId);
            EntityClassManager.Instance.LoadedEntityClasses.Remove(WeaponClass);
            foreach (var target in _targets)
            {
                EntityManager.Instance.UnregisterCreature(target.EntityId);
                EntityManager.Instance.UnregisterEntity(target.EntityId);
            }
            _targets.Clear();
            _context.Dispose();
            _connection.Dispose();
        }

        [TestMethod]
        public void OriginalWindupRecoveryAndPersistedAmmoReplaceImmediateUnboundedShots()
        {
            Assert.IsTrue(Start());
            Assert.AreEqual(5u, _weapon.CurrentAmmo);
            Assert.AreEqual(10000, Health);
            Advance(565);
            Assert.AreEqual(5u, _weapon.CurrentAmmo);
            Advance(566);
            Assert.AreEqual(3u, _weapon.CurrentAmmo);
            Assert.AreEqual(9980, Health);
            Assert.AreEqual(0, _target.Attributes[Attributes.Armor].Current);
            Assert.AreEqual(3u, StoredAmmo);
            Assert.AreEqual(1, Drain().OfType<WeaponAttackRecovery>().Count());
            Assert.IsFalse(new ActorActionManager(() => _now).CanBeginAbility(_client.Player));
            Assert.IsFalse(Start());
            Advance(1498);
            Assert.IsFalse(Start());
            Advance(1499);
            Assert.IsNull(_client.Player.CurrentWeaponAttack);
            Assert.IsTrue(Start());
            Advance(2065);
            Assert.AreEqual(1u, _weapon.CurrentAmmo);
            Assert.AreEqual(1u, StoredAmmo);
            Assert.AreEqual(9920, Health);
        }

        [TestMethod]
        public void RequestTargetIsHonoredInsteadOfTheActorsSeparateTrackingTarget()
        {
            var tracked = MakeTarget();
            _client.Player.Target = tracked.EntityId;
            Assert.IsTrue(Start());
            Advance(566);
            Assert.AreEqual(10000, tracked.Attributes[Attributes.Health].Current);
            var recovery = Drain().OfType<WeaponAttackRecovery>().Single();
            CollectionAssert.AreEqual(new[] { _target.EntityId }, HitIds(recovery));
        }

        [DataTestMethod]
        [DataRow("friendly")]
        [DataRow("dead")]
        [DataRow("other_map")]
        [DataRow("missing")]
        [DataRow("none")]
        public void DisallowedTargetBecomesABlindShotWithoutDamagingIt(string kind)
        {
            var request = Request();
            if (kind == "friendly") _target.Faction = Factions.AFS;
            if (kind == "dead") _target.State = CharacterState.Dead;
            if (kind == "other_map") _target.MapContextId = 1148;
            if (kind == "missing") request.TargetId = ulong.MaxValue;
            if (kind == "none") request.TargetId = null;
            Assert.IsTrue(_attacks.TryStart(_client, request));
            Advance(566);
            Assert.AreEqual(10000, Health);
            Assert.AreEqual(3u, _weapon.CurrentAmmo);
            Assert.AreEqual(0, HitIds(Drain().OfType<WeaponAttackRecovery>().Single()).Length);
        }

        [TestMethod]
        public void ReusedTargetIdentityDoesNotReceiveTheAcceptedShot()
        {
            Assert.IsTrue(Start());
            EntityManager.Instance.UnregisterCreature(_target.EntityId);
            EntityManager.Instance.UnregisterEntity(_target.EntityId);
            var replacement = MakeTarget(_target.EntityId);
            Advance(566);
            Assert.AreEqual(10000, replacement.Attributes[Attributes.Health].Current);
            Assert.AreEqual(3u, StoredAmmo);
            Assert.AreEqual(0, HitIds(Drain().OfType<WeaponAttackRecovery>().Single()).Length);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void WindupInterruptionDoesNotChargeOrHitAndOnlyManualRequestsNeedCleanup(bool manual)
        {
            Assert.IsTrue(_attacks.TryStart(_client, Request(), manual));
            Drain();
            Assert.IsFalse(_attacks.Interrupt(_client, ActionId.WeaponAttack, 204));
            Assert.IsTrue(_attacks.Interrupt(_client, ActionId.WeaponAttack, 203));
            var packets = Drain();
            Assert.AreEqual(1, packets.OfType<ActionInterruptPacket>().Count());
            Assert.AreEqual(manual ? 1 : 0, packets.OfType<UserActionFailedPacket>().Count());
            Advance(566);
            Assert.AreEqual(5u, _weapon.CurrentAmmo);
            Assert.AreEqual(5u, StoredAmmo);
            Assert.AreEqual(10000, Health);
        }

        [TestMethod]
        public void InterruptionAfterResolutionKeepsAmmoDebitAndSharedActionReuse()
        {
            Info.WeaponAttackArgId = 3;
            Assert.IsTrue(Start());
            Drain();
            Assert.IsTrue(_attacks.Interrupt(_client, ActionId.WeaponAttack, 3));
            Assert.AreEqual(0, Drain().OfType<UserActionFailedPacket>().Count());
            Info.WeaponAttackArgId = 1;
            Advance(909);
            Assert.IsFalse(Start());
            Assert.AreEqual(3u, StoredAmmo);
            Advance(910);
            Assert.IsTrue(Start());
            Assert.AreEqual(1u, StoredAmmo);
        }

        [TestMethod]
        public void NoReuseFlagKeepsTheBusyIntervalWithoutInventingATimer()
        {
            Info.WeaponAttackArgId = 66;
            Assert.IsTrue(Start());
            Assert.IsFalse(_client.Player.AbilityReuseDeadlines.ContainsKey(ActionId.WeaponAttack));
            Advance(1499);
            Assert.IsFalse(Start());
            Advance(1500);
            Assert.IsTrue(Start());
        }

        [TestMethod]
        public void MeleeKeepsItsOriginalShortTimingAllowsMovementAndConsumesNoAmmo()
        {
            Info.WeaponAttackActionId = ActionId.WeaponMelee;
            Info.WeaponAttackArgId = 32;
            Assert.IsTrue(Start());
            _client.Player.Position = new Vector3(2, 0, 3);
            Advance(3);
            Assert.AreEqual(10000, Health);
            Advance(4);
            Assert.AreEqual(9980, Health);
            Assert.AreEqual(5u, _weapon.CurrentAmmo);
            Assert.AreEqual(5u, StoredAmmo);
            Advance(9);
            Assert.IsNull(_client.Player.CurrentWeaponAttack);
            Assert.IsFalse(Start());
            Advance(11);
            Assert.IsTrue(Start());
        }

        [DataTestMethod]
        [DataRow("pair")]
        [DataRow("alt")]
        [DataRow("location")]
        [DataRow("holstered")]
        [DataRow("jam")]
        [DataRow("ammo")]
        public void InvalidManualRequestDoesNotBecomeADifferentWeaponAction(string kind)
        {
            var request = Request();
            if (kind == "pair") request.ActionArgId = 1;
            if (kind == "alt") request.IsAltAction = true;
            if (kind == "location") request.TargetLocation = (1d, 2d, 3d);
            if (kind == "holstered") _client.Player.WeaponReady = false;
            if (kind == "jam") _weapon.IsJammed = true;
            if (kind == "ammo") _weapon.CurrentAmmo = 1;
            Assert.IsFalse(_attacks.TryStart(_client, request));
            Assert.IsNull(_client.Player.CurrentWeaponAttack);
            Assert.IsNull(_client.Player.CurrentWeaponAction);
            Assert.AreEqual(5u, StoredAmmo);
            Assert.AreEqual(10000, Health);
            var packets = Drain();
            Assert.AreEqual(1, packets.OfType<ActionFailedPacket>().Count());
            Assert.AreEqual(1, packets.OfType<UserActionFailedPacket>().Count());
            Assert.AreEqual(0, packets.OfType<WeaponAttackRecovery>().Count());
        }

        [DataTestMethod]
        [DataRow("weapon")]
        [DataRow("dead")]
        [DataRow("map")]
        [DataRow("stored_ammo")]
        [DataRow("stored_owner")]
        public void ChangedExecutionStateFailsWithoutAmmoOrDamage(string kind)
        {
            Assert.IsTrue(Start());
            if (kind == "weapon") _client.Player.Inventory.EquippedInventory[13] = 0;
            if (kind == "dead") _client.Player.State = CharacterState.Dead;
            if (kind == "map") _client.Player.MapChannel = new MapChannel();
            if (kind == "stored_ammo") _context.Database.ExecuteSqlRaw("UPDATE items SET ammo_count = 4 WHERE item_id = 1");
            if (kind == "stored_owner") _context.Database.ExecuteSqlRaw("UPDATE character_inventory SET character_id = 202 WHERE item_id = 1");
            Advance(566);
            Assert.IsNull(_client.Player.CurrentWeaponAttack);
            Assert.AreEqual(5u, _weapon.CurrentAmmo);
            Assert.AreEqual(kind == "stored_ammo" ? 4u : 5u, StoredAmmo);
            Assert.AreEqual(10000, Health);
            Assert.AreEqual(0, Drain().OfType<WeaponAttackRecovery>().Count());
        }

        [TestMethod]
        public void LongUpdateResolvesOnlyOnceAndRetainsPredictedDeadline()
        {
            Assert.IsTrue(Start());
            Advance(10000);
            Assert.IsNull(_client.Player.CurrentWeaponAttack);
            Assert.AreEqual(1499L, _client.Player.AbilityReuseDeadlines[ActionId.WeaponAttack]);
            Assert.AreEqual(3u, StoredAmmo);
            Assert.AreEqual(1, Drain().OfType<WeaponAttackRecovery>().Count());
            Advance(10001);
            Assert.AreEqual(3u, StoredAmmo);
            Assert.AreEqual(0, Drain().Count);
        }

        [TestMethod]
        public void AutofireUsesOriginalRecoveryInsteadOfTheTemplatePlaceholderRefire()
        {
            Info.WeaponAttackArgId = 1;
            _client.Player.Target = _target.EntityId;
            _manifestations.StartAutoFire(_client, 0);
            Assert.AreEqual(3u, StoredAmmo);
            for (var time = 100; time < 500; time += 100)
            {
                _now = time;
                _manifestations.AutoFireTimerDoWork(100);
                Assert.AreEqual(3u, StoredAmmo);
            }
            _now = 500;
            _manifestations.AutoFireTimerDoWork(100);
            Assert.AreEqual(1u, StoredAmmo);
            Assert.AreEqual(2, Drain().OfType<WeaponAttackRecovery>().Count());
            _manifestations.StopAutoFire(_client);
            _now = 1000;
            _manifestations.AutoFireTimerDoWork(500);
            Assert.AreEqual(1u, StoredAmmo);
        }

        [TestMethod]
        public void FirstAutofireRepeatUsesItsDeadlineWithoutFallingIntoBusyPolling()
        {
            Info.WeaponAttackActionId = ActionId.WeaponMelee;
            Info.WeaponAttackArgId = 3; // 200 ms windup + 133 recovery + 250 reuse.
            _client.Player.Target = _target.EntityId;
            _manifestations.StartAutoFire(_client, 0);
            for (var time = 50; time <= 550; time += 50)
            {
                Advance(time);
                _manifestations.AutoFireTimerDoWork(50);
            }
            Assert.IsNull(_client.Player.CurrentWeaponAttack);
            Assert.AreEqual(1, Drain().OfType<WeaponAttackRecovery>().Count());
            Advance(600); // First server interval at/after the original 583 ms deadline.
            _manifestations.AutoFireTimerDoWork(50);
            Assert.IsNotNull(_client.Player.CurrentWeaponAttack);
            Assert.AreEqual(800L, _client.Player.CurrentWeaponAttack.WindupEndsAt);
            Assert.AreEqual(5u, StoredAmmo);
        }

        private WeaponClassInfo Info => WeaponActionManager.ClassInfo(_weapon);
        private int Health => _target.Attributes[Attributes.Health].Current;
        private uint StoredAmmo => _context.ItemEntries.AsNoTracking().Single(i => i.ItemId == 1).AmmoCount;
        private bool Start() => _attacks.TryStart(_client, Request());
        private RequestWeaponAttackPacket Request() => new()
            { ActionId = Info.WeaponAttackActionId, ActionArgId = (int)Info.WeaponAttackArgId, TargetId = _target.EntityId };
        private void Advance(long now) { _now = now; _attacks.Update(_client, now); }
        private Creature MakeTarget(ulong? id = null)
        {
            var target = new Creature { MapContextId = 1220, Level = 1, Faction = Factions.Bane,
                State = CharacterState.Normal, Cells = new uint[1, 1] };
            if (id.HasValue) target.EntityId = id.Value;
            target.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 40, 40, 40, 0, 0);
            target.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 10000, 10000, 10000, 0, 0);
            EntityManager.Instance.RegisterCreature(target);
            EntityManager.Instance.RegisterEntity(target.EntityId, EntityType.Creature);
            _targets.Add(target);
            return target;
        }
        private List<ServerPythonPacket> Drain()
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_client);
            var packets = new List<ServerPythonPacket>();
            while (queue.PopOutgoing() is ProtocolPacket protocol)
                if (protocol.Message is CallMethodMessage call && call.Packet is ServerPythonPacket packet)
                    packets.Add(packet);
            return packets;
        }
        private static ulong[] HitIds(WeaponAttackRecovery recovery)
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            recovery.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(6, reader.ReadTuple());
            reader.ReadInt(); reader.ReadInt();
            var count = reader.ReadList();
            return Enumerable.Range(0, count).Select(_ => reader.ReadULong()).ToArray();
        }
    }
}
