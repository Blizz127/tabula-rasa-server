using System;
using System.Collections.Generic;
using System.IO;
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
using Rasa.Memory;
using Rasa.Packets;
using Rasa.Packets.Game.Server;
using Rasa.Packets.Inventory.Client;
using Rasa.Packets.Inventory.Server;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Protocol;
using Rasa.Repositories.Char;
using Rasa.Repositories.Char.Character;
using Rasa.Repositories.Char.CharacterAppearance;
using Rasa.Repositories.Char.CharacterInventory;
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
    public partial class InventorySessionTests
    {
        public class UnitProxy : DispatchProxy
        {
            public SqliteCharContext Context;
            protected override object Invoke(MethodInfo method, object[] args)
            {
                switch (method.Name)
                {
                    case "get_Items": return new ItemRepository(Context);
                    case "get_CharacterInventories": return new CharacterInventoryRepository(Context);
                    case "get_Characters": return new CharacterRepository(Context);
                    case "get_CharacterAppearances": return new CharacterAppearanceRepository(Context);
                    case "Complete": Context.SaveChanges(); return null;
                    case "Reject": Context.ChangeTracker.Clear(); return null;
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

        private const uint WeaponTemplate = 9100101;
        private const uint AmmoTemplate = 9100102;
        private const EntityClasses WeaponClass = (EntityClasses)9100201;
        private const EntityClasses AmmoClass = (EntityClasses)9100202;
        private SqliteConnection _connection;
        private Factory _factory;
        private InventoryManager _inventory;
        private ManifestationManager _manifestation;
        private object _oldManifestation;
        private HashSet<ulong> _existingItems;
        private Logger.LoggerConfig _oldLoggerConfig;

        [TestInitialize]
        public void Initialize()
        {
            _existingItems = EntityManager.Instance.Items.Keys.ToHashSet();
            _oldLoggerConfig = Logger.Config;
            if (_oldLoggerConfig == null)
                Logger.UpdateConfig(new Logger.LoggerConfig { LogToFile = false });
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            _factory = new Factory(_connection);
            _inventory = Construct<InventoryManager>(_factory);
            _manifestation = Construct<ManifestationManager>(_factory);
            var singleton = typeof(ManifestationManager).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
            _oldManifestation = singleton.GetValue(null);
            singleton.SetValue(null, _manifestation);

            var weaponTemplate = new ItemTemplate(new ItemTemplateItemClassEntry
                { ItemTemplateId = WeaponTemplate, ItemClass = (uint)WeaponClass })
            {
                WeaponInfo = new WeaponInfo(new ItemTemplateWeaponEntry
                    { ReloadTime = 1000, AmmoPerShot = 1 }),
                InventoryCategory = InventoryCategory.Equipment
            };
            var ammoTemplate = new ItemTemplate(new ItemTemplateItemClassEntry
                { ItemTemplateId = AmmoTemplate, ItemClass = (uint)AmmoClass })
                { InventoryCategory = InventoryCategory.Consumable };
            var weaponClass = new EntityClass((uint)WeaponClass, "fixture weapon", 0, 0, new List<AugmentationType>(), false)
            {
                ItemClassInfo = new ItemClassInfo(new ItemClassEntry { MaxHitPoints = 100, StackSize = 1 }),
                EquipableClassInfo = new EquipableClassInfo(EquipmentData.Weapon),
                WeaponClassInfo = new WeaponClassInfo(new WeaponClassEntry
                    { ClipSize = 30, AmmoClassId = (uint)AmmoClass, ReloadActionId = 1 })
            };
            weaponClass.ItemTemplates.Add(WeaponTemplate, weaponTemplate);
            var ammoClass = new EntityClass((uint)AmmoClass, "fixture ammo", 0, 0, new List<AugmentationType>(), false)
                { ItemClassInfo = new ItemClassInfo(new ItemClassEntry { MaxHitPoints = 1, StackSize = 100 }) };
            ammoClass.ItemTemplates.Add(AmmoTemplate, ammoTemplate);
            EntityClassManager.Instance.LoadedEntityClasses.Add(WeaponClass, weaponClass);
            EntityClassManager.Instance.LoadedEntityClasses.Add(AmmoClass, ammoClass);
            ItemManager.Instance.ItemTemplateItemClass.Add(WeaponTemplate, WeaponClass);
            ItemManager.Instance.ItemTemplateItemClass.Add(AmmoTemplate, AmmoClass);

            using var context = Context();
            context.Database.EnsureCreated();
            context.GameAccountEntries.Add(new GameAccountEntry
                { Id = 10, FamilyName = "Fixture", Name = "Fixture", Email = "fixture@example.invalid" });
            context.CharacterEntries.AddRange(
                new CharacterEntry { Id = 101, AccountId = 10, Slot = 2, Name = "First", ActiveWeapon = 1 },
                new CharacterEntry { Id = 102, AccountId = 10, Slot = 3, Name = "Second", ActiveWeapon = 0 });
            AddItem(context, 1, 10, 101, InventoryType.Personal, 1, WeaponTemplate, 1, 12);
            AddItem(context, 2, 10, 0, InventoryType.HomeInventory, 3, AmmoTemplate, 27);
            AddItem(context, 3, 10, 102, InventoryType.Personal, 0, AmmoTemplate, 11);
            AddItem(context, 4, 20, 201, InventoryType.Personal, 0, AmmoTemplate, 22);
            AddItem(context, 5, 10, 101, InventoryType.WeaponDrawerInventory, 1, WeaponTemplate, 1, 7);
            AddItem(context, 6, 10, 101, InventoryType.WeaponDrawerInventory, 4, WeaponTemplate, 1, 19);
            AddItem(context, 7, 10, 101, InventoryType.Personal, 50, AmmoTemplate, 40);
            AddItem(context, 8, 10, 101, InventoryType.WeaponDrawerInventory, 0, WeaponTemplate, 1, 3);
            AddItem(context, 9, 10, 102, InventoryType.WeaponDrawerInventory, 0, WeaponTemplate, 1, 21);
            AddItem(context, 10, 10, 101, InventoryType.HomeInventory, 9, AmmoTemplate, 5);
            AddItem(context, 11, 10, 0, InventoryType.Personal, 9, AmmoTemplate, 5);
            AddItem(context, 12, 10, 101, InventoryType.HiddenInventory, 0, AmmoTemplate, 5);
            AddItem(context, 13, 20, 0, InventoryType.HomeInventory, 3, AmmoTemplate, 5);
            context.SaveChanges();
        }

        [TestCleanup]
        public void Cleanup()
        {
            typeof(ManifestationManager).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, _oldManifestation);
            foreach (var entityId in EntityManager.Instance.Items.Keys.Except(_existingItems).ToArray())
            {
                EntityManager.Instance.UnregisterItem(entityId);
                EntityManager.Instance.UnregisterEntity(entityId);
            }
            EntityClassManager.Instance.LoadedEntityClasses.Remove(WeaponClass);
            EntityClassManager.Instance.LoadedEntityClasses.Remove(AmmoClass);
            ItemManager.Instance.ItemTemplateItemClass.Remove(WeaponTemplate);
            ItemManager.Instance.ItemTemplateItemClass.Remove(AmmoTemplate);
            if (_oldLoggerConfig == null)
                typeof(Logger).GetProperty(nameof(Logger.Config)).SetValue(null, null);
            _connection.Dispose();
        }

        [TestMethod]
        public void RepositoryScopesCharacterAndSharedHomeBeforeMaterialization()
        {
            using var context = Context();
            var repository = new CharacterInventoryRepository(context);
            CollectionAssert.AreEqual(new uint[] { 1, 7, 2, 8, 5, 6 }, repository.GetItems(10, 101).Select(i => i.ItemId).ToArray());
            CollectionAssert.AreEqual(new uint[] { 3, 2, 9 }, repository.GetItems(10, 102).Select(i => i.ItemId).ToArray());
            CollectionAssert.AreEqual(new uint[] { 2 }, repository.GetItems(10, 0).Select(i => i.ItemId).ToArray());
            CollectionAssert.AreEqual(new uint[] { 4, 13 }, repository.GetItems(20, 201).Select(i => i.ItemId).ToArray());
        }

        [TestMethod]
        public void LoginPublishesOnlyOwnedItemsInStableOrderAndBindsTheSelectedDrawer()
        {
            var client = Login();
            var packets = Drain(client);
            var creates = packets.OfType<CreatePhysicalEntityPacket>().ToArray();
            CollectionAssert.AreEqual(new uint[] { 1, 7, 2, 8, 5, 6 }, creates.Select(p => Item(p.EntityId).Id).ToArray());
            Assert.AreEqual(6, EntityManager.Instance.Items.Keys.Except(_existingItems).Count());
            foreach (var added in packets.OfType<InventoryAddItemPacket>())
                Assert.IsTrue(packets.FindIndex(p => p is CreatePhysicalEntityPacket created && created.EntityId == added.EntityId)
                    < packets.IndexOf(added));
            Assert.AreEqual(5u, Item(client.Player.Inventory.EquippedInventory[13]).Id);
            Assert.AreEqual(7u, Item(client.Player.Inventory.EquippedInventory[13]).CurrentAmmo);
            Assert.AreEqual(2u, Item(client.Player.Inventory.HomeInventory[3]).Id);
            Assert.AreEqual(0u, Item(client.Player.Inventory.HomeInventory[3]).OwnerId);
        }

        [DataTestMethod]
        [DataRow(2)]
        [DataRow(255)]
        public void EmptyOrInvalidPersistedSelectionNeverBorrowsAnotherDrawer(int selected)
        {
            using (var context = Context())
            {
                context.CharacterEntries.Find(101u).ActiveWeapon = (byte)selected;
                context.SaveChanges();
            }
            var client = Login();
            Assert.AreEqual(0ul, client.Player.Inventory.EquippedInventory[13]);
            Assert.AreEqual((byte)selected, client.Player.ActiveWeapon);
            Assert.IsNull(WeaponActionManager.CurrentWeapon(client));
        }

        [TestMethod]
        public void FreshSessionRetainsInstanceDataAndSharedHomeWithoutOtherCharacterItems()
        {
            var first = Login();
            var before = PublishedInstances(Drain(first));
            var relogged = Login();
            CollectionAssert.AreEqual(before, PublishedInstances(Drain(relogged)));
            var secondCharacter = Login(102);
            CollectionAssert.AreEqual(new uint[] { 3, 2, 9 }, Drain(secondCharacter)
                .OfType<CreatePhysicalEntityPacket>().Select(p => Item(p.EntityId).Id).ToArray());
            Assert.AreEqual(9u, Item(secondCharacter.Player.Inventory.EquippedInventory[13]).Id);
        }

        [TestMethod]
        public void MapReloadRetiresOldEntitiesAndRepublishesFixedInventoryWithSelectedWeapon()
        {
            var client = Login();
            var original = PublishedInstances(Drain(client));
            var oldItems = EntityManager.Instance.Items.Values.Where(i => !_existingItems.Contains(i.EntityId)).ToArray();
            var oldWeapon = WeaponActionManager.CurrentWeapon(client);
            oldWeapon.IsJammed = true;
            oldWeapon.CammeraProfile = 4;

            _inventory.InitCharacterInventory(client);

            var packets = Drain(client);
            CollectionAssert.AreEqual(original, PublishedInstances(packets));
            Assert.AreEqual(6, packets.OfType<DestroyPhysicalEntityPacket>().Count());
            Assert.AreEqual(6, packets.OfType<DestroyPhysicalEntityPacket>().Select(p => p.EntityId).Distinct().Count());
            Assert.IsTrue(packets.FindLastIndex(p => p is DestroyPhysicalEntityPacket)
                < packets.FindIndex(p => p is CreatePhysicalEntityPacket));
            Assert.AreEqual(250, client.Player.Inventory.PersonalInventory.Count);
            Assert.AreEqual(480, client.Player.Inventory.HomeInventory.Count);
            Assert.AreEqual(22, client.Player.Inventory.EquippedInventory.Count);
            Assert.AreEqual(5, client.Player.Inventory.WeaponDrawer.Count);
            Assert.AreEqual(6, EntityManager.Instance.Items.Keys.Except(_existingItems).Count());
            Assert.IsFalse(oldItems.Any(i => EntityManager.Instance.Items.Values.Contains(i)));
            var reloadedWeapon = WeaponActionManager.CurrentWeapon(client);
            Assert.AreEqual(5u, reloadedWeapon.Id);
            Assert.AreEqual(7u, reloadedWeapon.CurrentAmmo);
            Assert.IsTrue(reloadedWeapon.IsJammed);
            Assert.AreEqual(4, reloadedWeapon.CammeraProfile);
            Assert.AreEqual(0u, Item(client.Player.Inventory.HomeInventory[3]).OwnerId);

            using (var context = Context())
            {
                context.CharacterInventoryEntries.Remove(context.CharacterInventoryEntries.Find(7u));
                context.SaveChanges();
            }
            _inventory.InitCharacterInventory(client);
            Assert.AreEqual(5, Drain(client).OfType<CreatePhysicalEntityPacket>().Count());
            Assert.AreEqual(0ul, client.Player.Inventory.PersonalInventory[50]);
            Assert.AreEqual(5, EntityManager.Instance.Items.Keys.Except(_existingItems).Count());
            Assert.AreEqual(250, client.Player.Inventory.PersonalInventory.Count);
            Assert.AreEqual(5u, WeaponActionManager.CurrentWeapon(client).Id);
        }

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(255)]
        public void InventoryLoadReconcilesStaleWeaponAppearanceBeforePublication(int selected)
        {
            var client = Login();
            var secondaryHue = new Color(5, 6, 7, 8);
            client.Player.ActiveWeapon = (byte)selected;
            client.Player.WeaponReady = true;
            client.Player.AppearanceData[EquipmentData.Weapon] = new AppearanceData
            {
                SlotId = EquipmentData.Weapon, Class = 999999,
                Color = new Color(1, 2, 3, 4), Hue2 = secondaryHue
            };
            Drain(client);

            _inventory.InitCharacterInventory(client);

            var appearance = client.Player.AppearanceData[EquipmentData.Weapon];
            var hasWeapon = selected == 1;
            Assert.AreEqual(hasWeapon ? (uint)WeaponClass : 0u, appearance.Class);
            Assert.AreEqual(hasWeapon, client.Player.WeaponReady);
            Assert.AreSame(secondaryHue, appearance.Hue2);
            if (hasWeapon)
                Assert.AreEqual(WeaponActionManager.CurrentWeapon(client).Color, appearance.Color.Hue);
            var equipment = ReadEquipment(new EquipmentInfoPacket(client.Player.Inventory.EquippedInventory));
            Assert.AreEqual(hasWeapon, equipment.ContainsKey(13));
            using var context = Context();
            Assert.AreEqual(0, context.CharacterAppearanceEntries.Count());
        }

        [TestMethod]
        public void BrokenRowsDoNotCreateUnplacedEntitiesOrSuppressLaterValidItems()
        {
            using (var context = Context())
            {
                context.CharacterInventoryEntries.Add(new CharacterInventoryEntry(10, 101, 1, 0, 14));
                AddItem(context, 15, 10, 101, InventoryType.Personal, 2, 9999999, 1);
                AddItem(context, 16, 10, 101, InventoryType.Personal, 250, AmmoTemplate, 1);
                AddItem(context, 17, 10, 101, InventoryType.Personal, 1, AmmoTemplate, 1);
                context.SaveChanges();
            }
            var client = Login();
            CollectionAssert.AreEqual(new uint[] { 1, 7, 2, 8, 5, 6 }, Drain(client)
                .OfType<CreatePhysicalEntityPacket>().Select(p => Item(p.EntityId).Id).ToArray());
            using var reloaded = Context();
            Assert.AreEqual(17, reloaded.CharacterInventoryEntries.Count());
        }

        [TestMethod]
        public void DrawerAddAndRemoveOnlyChangeEquipmentWhenTheActiveSlotChanges()
        {
            var client = Login();
            var active = client.Player.Inventory.EquippedInventory[13];
            _inventory.RemoveItemBySlot(client, InventoryType.WeaponDrawerInventory, 4);
            Assert.AreEqual(active, client.Player.Inventory.EquippedInventory[13]);
            _inventory.AddItemBySlot(client, InventoryType.WeaponDrawerInventory, client.Player.Inventory.PersonalInventory[1], 4, false);
            Assert.AreEqual(active, client.Player.Inventory.EquippedInventory[13]);
            _inventory.RemoveItemBySlot(client, InventoryType.WeaponDrawerInventory, 1);
            Assert.AreEqual(0ul, client.Player.Inventory.EquippedInventory[13]);
        }

        [TestMethod]
        public void SelectingEmptyDrawerClearsWeaponAndPersistsSelectionAcrossLogin()
        {
            var client = Login();
            _manifestation.RefreshArmedWeapon(client, 0);
            var weapon = WeaponActionManager.CurrentWeapon(client);
            client.Player.CurrentWeaponAction = Reload(client, weapon);
            Drain(client);
            _manifestation.RequestArmWeapon(client, 2);
            Assert.AreEqual(0ul, client.Player.Inventory.EquippedInventory[13]);
            Assert.AreEqual(0u, client.Player.AppearanceData[EquipmentData.Weapon].Class);
            Assert.IsFalse(client.Player.WeaponReady);
            Assert.IsNull(client.Player.CurrentWeaponAction);
            Assert.AreEqual(7u, weapon.CurrentAmmo);
            var packets = Drain(client);
            Assert.AreEqual(1, packets.OfType<ActionInterruptPacket>().Count());
            Assert.AreEqual(0, ReadEquipment(packets.OfType<EquipmentInfoPacket>().Single()).Count);
            using var context = Context();
            Assert.AreEqual((byte)2, context.CharacterEntries.Find(101u).ActiveWeapon);
            var relogged = Login();
            Assert.AreEqual((byte)2, relogged.Player.ActiveWeapon);
            Assert.AreEqual(0ul, relogged.Player.Inventory.EquippedInventory[13]);
        }

        [TestMethod]
        public void SelectingSameWeaponPreservesReloadWhileQueuedSwitchesKeepSeparateEquipmentSnapshots()
        {
            var client = Login();
            var active = WeaponActionManager.CurrentWeapon(client);
            var reload = Reload(client, active);
            client.Player.CurrentWeaponAction = reload;
            Drain(client);
            _manifestation.RequestArmWeapon(client, 1);
            Assert.AreSame(reload, client.Player.CurrentWeaponAction);
            var first = Drain(client).OfType<EquipmentInfoPacket>().Single();
            _manifestation.RequestArmWeapon(client, 4);
            var second = Drain(client).OfType<EquipmentInfoPacket>().Single();
            Assert.AreEqual(active.EntityId, ReadEquipment(first)[13]);
            Assert.AreEqual(client.Player.Inventory.WeaponDrawer[4], ReadEquipment(second)[13]);
            CollectionAssert.AreEqual(Serialize(first), Serialize(first));
            Assert.IsNull(client.Player.CurrentWeaponAction);
        }

        [DataTestMethod]
        [DataRow(4)]
        [DataRow(2)]
        public void MovingTheActiveDrawerRefreshesEquipmentAndPreservesLocationsOnRelog(int destination)
        {
            var client = Login();
            var oldActive = WeaponActionManager.CurrentWeapon(client);
            var destinationWeapon = client.Player.Inventory.WeaponDrawer[destination];
            client.Player.CurrentWeaponAction = Reload(client, oldActive);
            Drain(client);
            _inventory.WeaponDrawerInventory_MoveItem(client,
                new WeaponDrawerInventory_MoveItemPacket { SrcSlot = 1, DestSlot = (uint)destination });
            Assert.AreEqual(destinationWeapon, client.Player.Inventory.EquippedInventory[13]);
            Assert.AreEqual(oldActive.EntityId, client.Player.Inventory.WeaponDrawer[destination]);
            Assert.IsNull(client.Player.CurrentWeaponAction);
            Assert.AreEqual(1, Drain(client).OfType<EquipmentInfoPacket>().Count());
            using var context = Context();
            Assert.AreEqual((uint)destination, context.CharacterInventoryEntries.Find(5u).SlotId);
            var relogged = Login();
            Assert.AreEqual(5u, Item(relogged.Player.Inventory.WeaponDrawer[destination]).Id);
            Assert.AreEqual(destinationWeapon == 0 ? 0u : 6u,
                Item(relogged.Player.Inventory.EquippedInventory[13])?.Id ?? 0);
        }

        [TestMethod]
        public void EquippingAnInactiveDrawerDoesNotReplaceTheActiveWeaponOrCancelReload()
        {
            var client = Login();
            var active = WeaponActionManager.CurrentWeapon(client);
            var reload = Reload(client, active);
            client.Player.CurrentWeaponAction = reload;
            Drain(client);
            _inventory.RequestEquipWeapon(client,
                new RequestEquipWeaponPacket { InventoryType = InventoryType.Personal, SrcSlot = 1, DestSlot = 4 });
            Assert.AreEqual(active.EntityId, client.Player.Inventory.EquippedInventory[13]);
            Assert.AreSame(reload, client.Player.CurrentWeaponAction);
            Assert.AreEqual(1u, Item(client.Player.Inventory.WeaponDrawer[4]).Id);
            Assert.IsFalse(Drain(client).OfType<EquipmentInfoPacket>().Any());
            var relogged = Login();
            Assert.AreEqual(5u, Item(relogged.Player.Inventory.EquippedInventory[13]).Id);
            Assert.AreEqual(1u, Item(relogged.Player.Inventory.WeaponDrawer[4]).Id);
            Assert.AreEqual(6u, Item(relogged.Player.Inventory.PersonalInventory[1]).Id);
        }

        [TestMethod]
        public void UnequippingActiveWeaponIntoEmptyPersonalSlotClearsSelectionWithoutConsumingAmmo()
        {
            var client = Login();
            var weapon = WeaponActionManager.CurrentWeapon(client);
            client.Player.CurrentWeaponAction = Reload(client, weapon);
            Drain(client);
            _inventory.RequestEquipWeapon(client,
                new RequestEquipWeaponPacket { InventoryType = InventoryType.Personal, SrcSlot = 2, DestSlot = 1 });
            Assert.AreEqual(0ul, client.Player.Inventory.EquippedInventory[13]);
            Assert.AreEqual(weapon.EntityId, client.Player.Inventory.PersonalInventory[2]);
            Assert.IsNull(client.Player.CurrentWeaponAction);
            Assert.IsFalse(client.Player.WeaponReady);
            Assert.AreEqual(7u, weapon.CurrentAmmo);
            var relogged = Login();
            Assert.AreEqual(0ul, relogged.Player.Inventory.EquippedInventory[13]);
            Assert.AreEqual(5u, Item(relogged.Player.Inventory.PersonalInventory[2]).Id);
            Assert.AreEqual(7u, Item(relogged.Player.Inventory.PersonalInventory[2]).CurrentAmmo);
        }

        [TestMethod]
        public void QueuedAppearanceKeepsItsSlotsClassAndBothColorsAfterMutation()
        {
            var appearance = new AppearanceData { SlotId = EquipmentData.Weapon, Class = (uint)WeaponClass,
                Color = new Color(1, 2, 3, 4), Hue2 = new Color(5, 6, 7, 8) };
            var data = new Dictionary<EquipmentData, AppearanceData> { [EquipmentData.Weapon] = appearance };
            var packet = new AppearanceDataPacket(data);
            var original = Serialize(packet);
            appearance.SlotId = EquipmentData.Helmet;
            appearance.Class = 0;
            appearance.Color.Red = 9;
            appearance.Hue2.Blue = 10;
            data.Clear();
            CollectionAssert.AreEqual(original, Serialize(packet));
            CollectionAssert.AreEqual(original, Serialize(packet));
            using var stream = new MemoryStream(original);
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(1, reader.ReadTuple());
            Assert.AreEqual(1, reader.ReadDictionary());
            Assert.AreEqual((int)EquipmentData.Weapon, reader.ReadInt());
            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual((uint)WeaponClass, reader.ReadUInt());
            Assert.AreEqual(new Color(1, 2, 3, 4).Hue, reader.ReadStruct<Color>().Hue);
            Assert.AreEqual(new Color(5, 6, 7, 8).Hue, reader.ReadStruct<Color>().Hue);
            Assert.AreEqual(stream.Length, stream.Position);
        }

        private static T Construct<T>(Factory factory) => (T)Activator.CreateInstance(typeof(T),
            BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { factory }, null);
        private SqliteCharContext Context() => WeaponReloadPersistenceTests.Context(_connection);
        private static Item Item(ulong id) => EntityManager.Instance.GetItem(id);
        private static void AddItem(SqliteCharContext context, uint id, uint account, uint character,
            InventoryType type, uint slot, uint template, uint stack, uint ammo = 0)
        {
            context.ItemEntries.Add(new ItemEntry { ItemId = id, ItemTemplateId = template, StackSize = stack,
                AmmoCount = ammo, CurrentHitPoints = 82, Color = 12345, CrafterName = "Fixture" });
            context.CharacterInventoryEntries.Add(new CharacterInventoryEntry(account, character, (uint)type, slot, id));
        }
        private Client Login(uint characterId = 101)
        {
            using var context = Context();
            var character = context.CharacterEntries.AsNoTracking().Single(c => c.Id == characterId);
            var map = new MapChannel { MapInfo = new MapInfo(1220, "Fixture", 1, 1), ClientList = new List<Client>() };
            var client = new Client(null, new ClientPacketHandler())
            {
                State = ClientState.Ingame,
                Player = new Manifestation { Id = characterId, ActiveWeapon = character.ActiveWeapon,
                    AppearanceData = new Dictionary<EquipmentData, AppearanceData>(),
                    MapChannel = map, MapContextId = 1220, Cells = new uint[1, 1], State = CharacterState.Normal, WeaponReady = true }
            };
            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(client,
                new GameAccountEntry { Id = 10, SelectedSlot = character.Slot });
            map.ClientList.Add(client);
            map.MapCellInfo.Cells[0] = new MapCell { ClientList = new List<Client> { client } };
            _inventory.InitCharacterInventory(client);
            return client;
        }
        private static WeaponActionExecution Reload(Client client, Item weapon) =>
            new WeaponActionExecution(ActionId.WeaponReload, 1, weapon, client.Player.MapChannel, 1000, 1000, 1000, true);
        private static string[] PublishedInstances(IEnumerable<ServerPythonPacket> packets) => packets
            .OfType<CreatePhysicalEntityPacket>().Select(p => Item(p.EntityId))
            .Select(i => $"{i.Id}:{i.ItemTemplateId}:{i.OwnerId}:{i.OwnerSlotId}:{i.StackSize}:{i.CurrentAmmo}:{i.CurrentHitPoints}:{i.Color}:{i.Crafter}").ToArray();
        private static List<ServerPythonPacket> Drain(Client client)
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(client);
            var packets = new List<ServerPythonPacket>();
            while (queue.PopOutgoing() is ProtocolPacket protocol)
                if (protocol.Message is CallMethodMessage call) packets.Add((ServerPythonPacket)call.Packet);
            return packets;
        }
        private static byte[] Serialize(ServerPythonPacket packet)
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            packet.Write(writer);
            return stream.ToArray();
        }
        private static Dictionary<uint, ulong> ReadEquipment(EquipmentInfoPacket packet)
        {
            using var stream = new MemoryStream(Serialize(packet));
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(1, reader.ReadTuple());
            var count = reader.ReadList();
            var equipment = new Dictionary<uint, ulong>();
            for (var i = 0; i < count; i++)
            {
                Assert.AreEqual(2, reader.ReadTuple());
                equipment.Add(reader.ReadUInt(), reader.ReadULong());
            }
            Assert.AreEqual(stream.Length, stream.Position);
            return equipment;
        }
    }
}
