using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Context.Char;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Packets;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Protocol;
using Rasa.Repositories.Char;
using Rasa.Repositories.Char.Items;
using Rasa.Repositories.Char.CharacterInventory;
using Rasa.Repositories.UnitOfWork;
using Rasa.Repositories.World;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.World;

namespace Rasa.Test
{
    [TestClass]
    [DoNotParallelize]
    public class WeaponActionLifecycleTests
    {
        public class UnitProxy : DispatchProxy
        {
            public IItemRepository Items;
            public ICharacterInventoryRepository Inventory;
            protected override object Invoke(MethodInfo method, object[] args)
                => method.Name == "get_Items" ? Items :
                    method.Name == "get_CharacterInventories" ? Inventory :
                    method.Name == "Dispose" || method.Name == "Complete" || method.Name == "Reject"
                        ? null : throw new NotSupportedException(method.Name);
        }
        private sealed class Factory : IGameUnitOfWorkFactory
        {
            private readonly SqliteCharContext _context;
            public Factory(SqliteCharContext context) => _context = context;
            public ICharUnitOfWork CreateChar()
            {
                var unit = DispatchProxy.Create<ICharUnitOfWork, UnitProxy>();
                ((UnitProxy)(object)unit).Items = new ItemRepository(_context);
                ((UnitProxy)(object)unit).Inventory = new CharacterInventoryRepository(_context);
                return unit;
            }
            public IWorldUnitOfWork CreateWorld() => throw new NotSupportedException();
        }

        private SqliteConnection _connection;
        private SqliteCharContext _context;
        private WeaponActionManager _manager;
        private Client _client;
        private MapChannel _map;
        private Item _weapon, _first, _second;
        private long _now;
        private const EntityClasses WeaponClass = (EntityClasses)9000001;
        private const EntityClasses AmmoClass = (EntityClasses)9000002;

        [TestInitialize]
        public void Initialize()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            _context = WeaponReloadPersistenceTests.Context(_connection);
            WeaponReloadPersistenceTests.Seed(_context);
            _now = 0;
            _manager = new WeaponActionManager(new Factory(_context), () => _now);
            var map = new MapChannel { MapInfo = new MapInfo(1220, "Test", 1, 1), ClientList = new List<Client>() };
            _map = map;
            _client = new Client(null, new ClientPacketHandler())
            {
                State = ClientState.Ingame,
                Player = new Manifestation { Id = 101, MapChannel = map, MapContextId = 1220,
                    State = CharacterState.Normal, Cells = new uint[1, 1], WeaponReady = true }
            };
            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(_client,
                new GameAccountEntry { Id = 10, SelectedSlot = 2 });
            _client.Player.Inventory.PersonalInventory.AddRange(new ulong[250]);
            _client.Player.Inventory.EquippedInventory.AddRange(new ulong[22]);
            _client.Player.Inventory.WeaponDrawer.AddRange(new ulong[5]);
            map.ClientList.Add(_client);
            map.MapCellInfo.Cells[0] = new MapCell { ClientList = new List<Client> { _client } };
            EntityClassManager.Instance.LoadedEntityClasses[WeaponClass] =
                new EntityClass((uint)WeaponClass, "test", 0, 0, new List<AugmentationType>(), false)
                {
                    WeaponClassInfo = new WeaponClassInfo(new WeaponClassEntry { Id = 1,
                        ClipSize = 30, AmmoClassId = (uint)AmmoClass, DrawActionId = 1,
                        StowActionId = 1, ReloadActionId = 1 })
                };
            _weapon = MakeItem(1, WeaponClass, 1, 0);
            _weapon.CurrentAmmo = 5;
            _weapon.ItemTemplate.WeaponInfo = new WeaponInfo(new ItemTemplateWeaponEntry
                { ReloadTime = 1000, Refire = 250, AmmoPerShot = 1 });
            _first = MakeItem(2, AmmoClass, 7, 50);
            _second = MakeItem(3, AmmoClass, 25, 51);
            _client.Player.Inventory.EquippedInventory[13] = _weapon.EntityId;
            _client.Player.Inventory.WeaponDrawer[0] = _weapon.EntityId;
            _client.Player.Inventory.PersonalInventory[50] = _first.EntityId;
            _client.Player.Inventory.PersonalInventory[51] = _second.EntityId;
        }

        [TestCleanup]
        public void Cleanup()
        {
            _client.Player.MapChannel = _map;
            ManifestationManager.Instance.StopAutoFire(_client);
            foreach (var item in new[] { _weapon, _first, _second })
            {
                EntityManager.Instance.UnregisterItem(item.EntityId);
                EntityManager.Instance.UnregisterEntity(item.EntityId);
            }
            EntityClassManager.Instance.LoadedEntityClasses.Remove(WeaponClass);
            _context.Dispose();
            _connection.Dispose();
        }

        [TestMethod]
        public void ReloadUsesAllStacksOnceAtWindupCompletionAndPersistsTheSameCounts()
        {
            Assert.IsTrue(RequestReload());
            Assert.IsFalse(RequestReload());
            Assert.IsFalse(ManifestationManager.Instance.PlayerTryFireWeapon(_client));
            Advance(999);
            Assert.AreEqual(5u, _weapon.CurrentAmmo);
            Assert.AreEqual(7u, _first.StackSize);
            Assert.IsFalse(Drain().OfType<WeaponReloadRecoveryPacket>().Any());
            Advance(1000);
            Assert.AreEqual(30u, _weapon.CurrentAmmo);
            Assert.AreEqual(0u, _first.StackSize);
            Assert.AreEqual(7u, _second.StackSize);
            Assert.AreEqual(0ul, _client.Player.Inventory.PersonalInventory[50]);
            Assert.AreEqual(1, Drain().OfType<WeaponReloadRecoveryPacket>().Count());
            Assert.AreEqual(30u, _context.ItemEntries.AsNoTracking().Single(i => i.ItemId == 1).AmmoCount);
            Advance(10000);
            Assert.AreEqual(0, Drain().Count);
            Assert.AreEqual(7u, _second.StackSize);
        }

        [TestMethod]
        public void PartialReloadPreservesExistingRoundsAndUsesAvailableAmmunition()
        {
            _client.Player.Inventory.PersonalInventory[51] = 0;
            Assert.IsTrue(RequestReload());
            Advance(1000);
            Assert.AreEqual(12u, _weapon.CurrentAmmo);
            Assert.AreEqual(25u, _second.StackSize);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void WithdrawnAndMovedAmmunitionKeepsCharacterOwnershipAndReloadsFromItsNewSlot(bool fromHome)
        {
            if (fromHome)
            {
                _second.OwnerId = 0;
                _context.Database.ExecuteSqlRaw(
                    "UPDATE character_inventory SET character_id = 0, invenotry_type = 2 WHERE item_id = 3");
            }
            _client.Player.Inventory.PersonalInventory[51] = 0;
            var inventory = (InventoryManager)Activator.CreateInstance(typeof(InventoryManager),
                BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { new Factory(_context) }, null);
            inventory.AddItemBySlot(_client, InventoryType.Personal, _second.EntityId, 60, true);
            Assert.AreEqual(101u, _second.OwnerId);
            Assert.AreEqual(60u, _second.OwnerSlotId);
            _context.ChangeTracker.Clear();
            Assert.IsTrue(RequestReload());
            Advance(1000);
            Assert.AreEqual(30u, _weapon.CurrentAmmo);
            using var reloaded = WeaponReloadPersistenceTests.Context(_connection);
            var location = reloaded.CharacterInventoryEntries.Find(3u);
            Assert.AreEqual(10u, location.AccountId);
            Assert.AreEqual(101u, location.CharacterId);
            Assert.AreEqual(1u, location.InventoryType);
            Assert.AreEqual(60u, location.SlotId);
            Assert.AreEqual(7u, reloaded.ItemEntries.Find(3u).StackSize);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void InterruptedReloadDoesNotConsumeAmmoAndOnlyManualRequestsNeedPendingCleanup(bool requested)
        {
            Assert.IsTrue(_manager.Request(_client, ActionId.WeaponReload, requested));
            Drain();
            Assert.IsTrue(_manager.Interrupt(_client, ActionId.WeaponReload, 1));
            var packets = Drain();
            Assert.AreEqual(1, packets.OfType<ActionInterruptPacket>().Count());
            Assert.AreEqual(requested ? 1 : 0, packets.OfType<UserActionFailedPacket>().Count());
            Advance(1000);
            Assert.AreEqual(5u, _weapon.CurrentAmmo);
            Assert.AreEqual(7u, _first.StackSize);
            Assert.IsNull(_client.Player.CurrentWeaponAction);
        }

        [DataTestMethod]
        [DataRow("weapon")]
        [DataRow("dead")]
        [DataRow("map")]
        [DataRow("stale_stack")]
        public void ChangedEligibilityCancelsWithoutConsumingAnyStack(string changed)
        {
            Assert.IsTrue(RequestReload());
            if (changed == "weapon") _client.Player.Inventory.EquippedInventory[13] = 0;
            if (changed == "dead") _client.Player.State = CharacterState.Dead;
            if (changed == "map") _client.Player.MapChannel = new MapChannel();
            if (changed == "stale_stack") _context.Database.ExecuteSqlRaw("UPDATE items SET stack_size = 24 WHERE item_id = 3");
            Advance(1000);
            Assert.AreEqual(5u, _weapon.CurrentAmmo);
            Assert.AreEqual(7u, _first.StackSize);
            Assert.AreEqual(25u, _second.StackSize);
            Assert.AreEqual(5u, _context.ItemEntries.AsNoTracking().Single(i => i.ItemId == 1).AmmoCount);
            Assert.AreEqual(7u, _context.ItemEntries.AsNoTracking().Single(i => i.ItemId == 2).StackSize);
            Assert.IsNull(_client.Player.CurrentWeaponAction);
        }

        [TestMethod]
        public void FullOrNoReserveMagazineRejectsWithoutDiscardingLoadedRounds()
        {
            _client.Player.Inventory.PersonalInventory[50] = 0;
            _client.Player.Inventory.PersonalInventory[51] = 0;
            Assert.IsFalse(RequestReload());
            Assert.AreEqual(5u, _weapon.CurrentAmmo);
            _weapon.CurrentAmmo = 30;
            Assert.IsFalse(RequestReload());
            Assert.AreEqual(30u, _weapon.CurrentAmmo);
        }

        [TestMethod]
        public void HolsteredReloadIsRejectedAndMovementDoesNotCancelAnArmedReload()
        {
            _client.Player.WeaponReady = false;
            Assert.IsFalse(RequestReload());
            _client.Player.WeaponReady = true;
            Assert.IsTrue(RequestReload());
            _client.Player.Position = new System.Numerics.Vector3(10, 2, 3);
            _client.Player.IsRunning = true;
            Advance(1000);
            Assert.AreEqual(30u, _weapon.CurrentAmmo);
        }

        [TestMethod]
        public void AbilityCanInterruptReloadButNotDrawAndWrongPairDoesNotCancel()
        {
            Assert.IsTrue(RequestReload());
            Assert.IsTrue(ActorActionManager.Instance.CanBeginAbility(_client.Player));
            Assert.IsFalse(_manager.Interrupt(_client, ActionId.WeaponReload, 2));
            Assert.IsNotNull(_client.Player.CurrentWeaponAction);
            _manager.InterruptForAbility(_client);
            Assert.IsNull(_client.Player.CurrentWeaponAction);
            Assert.AreEqual(5u, _weapon.CurrentAmmo);
            _client.Player.WeaponReady = false;
            Assert.IsTrue(_manager.Request(_client, ActionId.WeaponDraw, true));
            _manager.InterruptForAbility(_client);
            Assert.IsNotNull(_client.Player.CurrentWeaponAction);
            Assert.IsFalse(ActorActionManager.Instance.CanBeginAbility(_client.Player));
        }

        [TestMethod]
        public void DrawUsesOriginalZeroWindupAndFullRecoveryBeforeFiring()
        {
            _client.Player.WeaponReady = false;
            Assert.IsTrue(_manager.Request(_client, ActionId.WeaponDraw, true));
            Assert.AreEqual(1, Drain().OfType<PerformRecoveryPacket>().Count());
            Advance(1499);
            Assert.IsFalse(_client.Player.WeaponReady);
            Assert.IsFalse(ActorActionManager.Instance.CanBeginAbility(_client.Player));
            Assert.IsFalse(ManifestationManager.Instance.PlayerTryFireWeapon(_client));
            Advance(1500);
            Assert.IsTrue(_client.Player.WeaponReady);
            Assert.IsNull(_client.Player.CurrentWeaponAction);
        }

        [TestMethod]
        public void DrawNineKeepsOriginalExtraReuseAndSharesItAcrossArguments()
        {
            WeaponActionManager.ClassInfo(_weapon).DrawActionId = 9;
            _client.Player.WeaponReady = false;
            Assert.IsTrue(_manager.Request(_client, ActionId.WeaponDraw, true));
            Assert.AreEqual(3333L, Drain().OfType<ActionReuseTimesPacket>().Single().ReuseTimes.Single().RemainingMilliseconds);
            Advance(1333);
            Assert.IsNull(_client.Player.CurrentWeaponAction);
            WeaponActionManager.ClassInfo(_weapon).DrawActionId = 1;
            Assert.IsFalse(_manager.Request(_client, ActionId.WeaponDraw, true));
            Advance(3333);
            Assert.IsTrue(_manager.Request(_client, ActionId.WeaponDraw, true));
        }

        [TestMethod]
        public void ReloadNinetyTwoRetainsRecoveryAndInterruptionAfterResolutionKeepsItsAmmo()
        {
            WeaponActionManager.ClassInfo(_weapon).ReloadActionId = 92;
            Assert.IsTrue(RequestReload());
            Advance(1000);
            Assert.AreEqual(30u, _weapon.CurrentAmmo);
            Assert.IsNotNull(_client.Player.CurrentWeaponAction);
            Assert.AreEqual(4996L, _client.Player.CurrentWeaponAction.RecoveryEndsAt);
            Drain();
            Assert.IsTrue(_manager.Interrupt(_client, ActionId.WeaponReload, 92));
            Assert.AreEqual(0, Drain().OfType<UserActionFailedPacket>().Count());
            Assert.AreEqual(30u, _weapon.CurrentAmmo);
            Assert.AreEqual(7u, _second.StackSize);
        }

        [TestMethod]
        public void LateReloadResolutionStartsItsUnpredictedRecoveryWhenAmmoActuallyArrives()
        {
            WeaponActionManager.ClassInfo(_weapon).ReloadActionId = 92;
            Assert.IsTrue(RequestReload());
            Advance(4000);
            Assert.AreEqual(30u, _weapon.CurrentAmmo);
            Assert.AreEqual(7996L, _client.Player.CurrentWeaponAction.RecoveryEndsAt);
            Assert.AreEqual(7996L, _client.Player.AbilityReuseDeadlines[ActionId.WeaponReload]);
            Advance(4996);
            Assert.IsNotNull(_client.Player.CurrentWeaponAction);
            Advance(7996);
            Assert.IsNull(_client.Player.CurrentWeaponAction);
        }

        [TestMethod]
        public void EligibleStowInterruptsReloadBeforeAnyAmmunitionTransfer()
        {
            Assert.IsTrue(RequestReload());
            Assert.IsTrue(_manager.Request(_client, ActionId.WeaponStow, true));
            Assert.AreEqual(ActionId.WeaponStow, _client.Player.CurrentWeaponAction.ActionId);
            Advance(1500);
            Assert.IsFalse(_client.Player.WeaponReady);
            Assert.AreEqual(5u, _weapon.CurrentAmmo);
            Assert.AreEqual(7u, _first.StackSize);
        }

        [TestMethod]
        public void AutoFireWithHolsteredWeaponRegistersOneContinuingSequence()
        {
            _client.Player.WeaponReady = false;
            ManifestationManager.Instance.StartAutoFire(_client, 0);
            var execution = _client.Player.CurrentWeaponAction;
            Assert.IsNotNull(execution);
            Assert.AreEqual(ActionId.WeaponDraw, execution.ActionId);
            Assert.IsFalse(execution.ClientRequested);
            ManifestationManager.Instance.StartAutoFire(_client, 0);
            var timers = (List<AutoFireTimer>)typeof(ManifestationManager)
                .GetField("AutoFire", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.AreEqual(1, timers.Count(t => t.Client == _client));
            ManifestationManager.Instance.AutoFireTimerDoWork(100);
            Assert.AreSame(execution, _client.Player.CurrentWeaponAction);
            Assert.AreEqual(1, timers.Count(t => t.Client == _client));
            ManifestationManager.Instance.StopAutoFire(_client);
            Assert.AreEqual(0, timers.Count(t => t.Client == _client));
        }

        [TestMethod]
        public void GlobalAutoFireClockAdvancesOnceRegardlessOfOccupiedMapCount()
        {
            var maps = new MapChannelManager(null);
            maps.MapChannelArray[1220] = _map;
            var other = new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame };
            var otherMap = new MapChannel { ClientList = new List<Client> { other } };
            other.Player.MapChannel = otherMap;
            maps.MapChannelArray[1148] = otherMap;
            ManifestationManager.Instance.RegisterAutoFire(_client);
            var timers = (List<AutoFireTimer>)typeof(ManifestationManager)
                .GetField("AutoFire", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            var timer = timers.Single(t => t.Client == _client);
            timer.Delay = 10000;
            maps.MapChannelWorker(40);
            maps.MapChannelWorker(60);
            Assert.AreEqual(9900L, timer.Delay);
            Assert.AreEqual(9900L, timer.MaxAliveTime);
        }

        private Item MakeItem(uint id, EntityClasses itemClass, uint stack, uint slot)
        {
            var item = new Item { Id = id, OwnerId = 101, OwnerSlotId = slot, StackSize = stack,
                ItemTemplate = new ItemTemplate(new ItemTemplateItemClassEntry
                    { ItemTemplateId = id, ItemClass = (uint)itemClass }) };
            EntityManager.Instance.RegisterItem(item.EntityId, item);
            EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
            return item;
        }
        private bool RequestReload() => _manager.Request(_client, ActionId.WeaponReload, true);
        private void Advance(long now) { _now = now; _manager.Update(_client); }
        private List<ServerPythonPacket> Drain()
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_client);
            var packets = new List<ServerPythonPacket>();
            while (queue.PopOutgoing() is ProtocolPacket protocol)
                if (protocol.Message is CallMethodMessage call) packets.Add((ServerPythonPacket)call.Packet);
            return packets;
        }
    }
}
