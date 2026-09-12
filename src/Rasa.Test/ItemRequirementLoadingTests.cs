using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Packets.MapChannel.Server;
using Rasa.Repositories.Char;
using Rasa.Repositories.UnitOfWork;
using Rasa.Repositories.World;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Test
{
    [TestClass]
    [DoNotParallelize]
    public class ItemRequirementLoadingTests
    {
        public class EquipmentProxy : DispatchProxy
        {
            public List<ItemTemplateItemClassEntry> Templates;
            public List<ItemTemplateRequirementEntry> Generic;
            public List<ItemTemplateRequirementSkillEntry> Skills;
            public List<ItemTemplateRequirementRaceEntry> Races;
            public List<ItemTemplateEntry> ItemTemplates = new();
            protected override object Invoke(MethodInfo method, object[] args)
            {
                switch (method.Name)
                {
                    case "GetItemTemplateClasses": return Templates;
                    case "GetRequirementsGeneric": return Generic;
                    case "GetRequirementsSkill": return Skills;
                    case "GetRequirementsRace": return Races;
                    case "GetItemResistances": return new List<ItemTemplateResistanceEntry>();
                    case "GetWeaponItems": return new List<ItemTemplateWeaponEntry>();
                    case "GetArmorItems": return new List<ItemTemplateArmorEntry>();
                    case "GetItemTemplates": return ItemTemplates;
                    default: throw new NotSupportedException(method.Name);
                }
            }
        }
        public class WorldProxy : DispatchProxy
        {
            public IEquipmentRepository Equipment;
            protected override object Invoke(MethodInfo method, object[] args)
                => method.Name == "get_Equipment" ? Equipment : method.Name == "Dispose" ? null
                    : throw new NotSupportedException(method.Name);
        }
        private sealed class Factory : IGameUnitOfWorkFactory
        {
            private readonly IWorldUnitOfWork _world;
            public Factory(IWorldUnitOfWork world) => _world = world;
            public ICharUnitOfWork CreateChar() => throw new NotSupportedException();
            public IWorldUnitOfWork CreateWorld() => _world;
        }

        private readonly Dictionary<EntityClasses, EntityClass> _previousClasses = new();
        private Logger.LoggerConfig _previousLogger;
        private ItemManager _manager;
        private EquipmentProxy _records;

        [TestInitialize]
        public void Initialize()
        {
            _previousLogger = Logger.Config;
            if (_previousLogger == null)
                Logger.UpdateConfig(new Logger.LoggerConfig { LogToFile = false });
            // 6048 -> template 145 and level one are original records. Other IDs
            // and race/skill requirements below are deliberate fixture controls.
            foreach (var id in new uint[] { 6048, 9000302, 9000303 })
            {
                var key = (EntityClasses)id;
                EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(key, out var previous);
                _previousClasses.Add(key, previous);
                EntityClassManager.Instance.LoadedEntityClasses[key] =
                    new EntityClass(id, "fixture", 0, 0, new List<AugmentationType>(), false);
            }
            var equipment = DispatchProxy.Create<IEquipmentRepository, EquipmentProxy>();
            var records = (EquipmentProxy)(object)equipment;
            _records = records;
            records.Templates = new List<ItemTemplateItemClassEntry>
            {
                new ItemTemplateItemClassEntry { ItemTemplateId = 145, ItemClass = 6048 },
                new ItemTemplateItemClassEntry { ItemTemplateId = 500001, ItemClass = 6048 },
                new ItemTemplateItemClassEntry { ItemTemplateId = 6048, ItemClass = 9000302 },
                new ItemTemplateItemClassEntry { ItemTemplateId = 500002, ItemClass = 9000303 }
            };
            records.Generic = new List<ItemTemplateRequirementEntry>
            {
                new ItemTemplateRequirementEntry { Id = 6048, RequirementType = 1, RequirementValue = 1 },
                new ItemTemplateRequirementEntry { Id = 9000399, RequirementType = 2, RequirementValue = 9 }
            };
            records.Skills = new List<ItemTemplateRequirementSkillEntry>
                { new ItemTemplateRequirementSkillEntry { Id = 145, SkillId = 50, SkillLevel = 2 } };
            records.Races = new List<ItemTemplateRequirementRaceEntry>
                { new ItemTemplateRequirementRaceEntry { Id = 145, RaceId = 2 } };
            var world = DispatchProxy.Create<IWorldUnitOfWork, WorldProxy>();
            ((WorldProxy)(object)world).Equipment = equipment;
            _manager = (ItemManager)Activator.CreateInstance(typeof(ItemManager),
                BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { new Factory(world) }, null);
        }

        [TestCleanup]
        public void Cleanup()
        {
            foreach (var previous in _previousClasses)
                if (previous.Value == null)
                    EntityClassManager.Instance.LoadedEntityClasses.Remove(previous.Key);
                else
                    EntityClassManager.Instance.LoadedEntityClasses[previous.Key] = previous.Value;
            _previousClasses.Clear();
            if (_previousLogger == null)
                typeof(Logger).GetProperty(nameof(Logger.Config)).SetValue(null, null);
        }

        [TestMethod]
        public void OriginalClassRequirementReachesEveryMappedTemplateWithoutNumericIdCollision()
        {
            _manager.LoadItemTemplates();
            Assert.AreEqual(1, _manager.GetItemTemplateById(145).ItemInfo.Requirements[RequirementsType.ReqXpLevel]);
            Assert.AreEqual(1, _manager.GetItemTemplateById(500001).ItemInfo.Requirements[RequirementsType.ReqXpLevel]);
            Assert.AreEqual(1, _manager.GetItemTemplateById(145).ItemInfo.Requirements.Count);
            Assert.AreEqual(0, _manager.GetItemTemplateById(6048).ItemInfo.Requirements.Count);
            Assert.AreEqual(0, _manager.GetItemTemplateById(500002).ItemInfo.Requirements.Count);
        }

        [TestMethod]
        public void RaceAndSkillRequirementsKeepTheirIndependentTemplateKeys()
        {
            _manager.LoadItemTemplates();
            var template = _manager.GetItemTemplateById(145);
            Assert.AreEqual(2, template.ItemInfo.RaceReq);
            Assert.AreEqual(50, template.EquipableInfo.SkillId);
            Assert.AreEqual(2, template.EquipableInfo.SkillLevel);
            Assert.AreEqual(0, _manager.GetItemTemplateById(500001).ItemInfo.RaceReq);
            Assert.IsNull(_manager.GetItemTemplateById(500001).EquipableInfo);
            Assert.IsNull(_manager.GetItemTemplateById(6048).EquipableInfo);
        }

        [DataTestMethod]
        [DataRow(0)]
        [DataRow(1)]
        public void LoadedTradeFlagAgreesBetweenItemStateAndTooltip(int notTradable)
            => AssertLoadedTradeFlag((byte)notTradable);

        [TestMethod]
        public void MissingTemplateFlagRowKeepsTheExistingNegativeWireDefault()
            => AssertLoadedTradeFlag(null);

        private void AssertLoadedTradeFlag(byte? storedFlag)
        {
            if (storedFlag.HasValue)
                _records.ItemTemplates.Add(new ItemTemplateEntry { Id = 145, NotTradableFlag = storedFlag.Value });
            var notTradable = storedFlag.GetValueOrDefault() != 0;
            _manager.LoadItemTemplates();
            var template = _manager.GetItemTemplateById(145);
            Assert.AreEqual(!notTradable, template.ItemInfo.Tradable);
            var entityClass = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)6048];
            entityClass.ItemClassInfo = new ItemClassInfo(new ItemClassEntry { MaxHitPoints = 160 });
            entityClass.Augmentations.Add(AugmentationType.Item);

            using var itemStream = new MemoryStream();
            using var itemWriter = new PythonWriter(new BinaryWriter(itemStream));
            new ItemInfoPacket(new Item { ItemTemplate = template, CurrentHitPoints = 100 }, entityClass).Write(itemWriter);
            itemStream.Position = 0;
            using var itemReader = new PythonReader(new BinaryReader(itemStream));
            Assert.AreEqual(15, itemReader.ReadTuple());
            itemReader.ReadInt(); itemReader.ReadInt(); itemReader.ReadNoneStruct(); itemReader.ReadUInt();
            for (var i = 0; i < 4; i++) itemReader.ReadBool();
            Assert.AreEqual(0, itemReader.ReadList());
            Assert.AreEqual(0, itemReader.ReadList());
            itemReader.ReadInt(); itemReader.ReadBool();
            Assert.AreEqual(notTradable, itemReader.ReadBool());

            using var tooltipStream = new MemoryStream();
            using var tooltipWriter = new PythonWriter(new BinaryWriter(tooltipStream));
            new ItemTemplateTooltipInfoPacket(template, entityClass).Write(tooltipWriter);
            tooltipStream.Position = 0;
            using var tooltipReader = new PythonReader(new BinaryReader(tooltipStream));
            Assert.AreEqual(3, tooltipReader.ReadTuple());
            Assert.AreEqual(145u, tooltipReader.ReadUInt());
            Assert.AreEqual(6048u, tooltipReader.ReadUInt());
            Assert.AreEqual(1, tooltipReader.ReadDictionary());
            Assert.AreEqual((int)AugmentationType.Item, tooltipReader.ReadInt());
            Assert.AreEqual(6, tooltipReader.ReadTuple());
            Assert.AreEqual(notTradable, tooltipReader.ReadBool());
        }
    }
}
