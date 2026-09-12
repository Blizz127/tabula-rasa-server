using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
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
                    case "GetItemTemplates": return new List<ItemTemplateEntry>();
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
    }
}
