using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Test
{
    [TestClass]
    public class EquipmentRequirementTests
    {
        [DataTestMethod]
        [DataRow(1, 200, 0L)]
        [DataRow(2, 200, 1L)]
        [DataRow(0, 200, 0L)]
        [DataRow(-1, 200, -1L)]
        [DataRow(201, 200, 100L)]
        [DataRow(400, 200, 200L)]
        [DataRow(0, 0, 100L)]
        [DataRow(0, -1, 100L)]
        [DataRow(int.MaxValue, 1, 214748364700L)]
        public void ConditionKeepsOriginalIntegerFloorAndUnclampedValues(int current, int maximum, long expected)
            => Assert.AreEqual(expected, EquipmentRequirements.GetCondition(current, maximum));

        [TestMethod]
        public void UnspecifiedDurabilityHasOriginalFullCondition()
        {
            Assert.AreEqual(100L, EquipmentRequirements.GetCondition(null, 200));
            Assert.AreEqual(100L, EquipmentRequirements.GetCondition(0, null));
        }

        [TestMethod]
        public void RequirementBoundariesUseCurrentAttributesAndPositiveSkillMinimumOnly()
        {
            var actor = new Manifestation { Level = 10, Race = Race.Forean };
            var item = new Item { CurrentHitPoints = 50, ItemTemplate = new ItemTemplate(new ItemTemplateItemClassEntry()) };
            var info = item.ItemTemplate.ItemInfo;
            info.Requirements[RequirementsType.ReqXpLevel] = 10;
            info.Requirements[RequirementsType.ReqXpLevelMax] = 10;
            info.Requirements[RequirementsType.ReqBody] = 20;
            info.Requirements[RequirementsType.ReqMind] = 20;
            info.Requirements[RequirementsType.ReqSpirit] = 20;
            info.RaceReq = 2;
            foreach (var attribute in new[] { Attributes.Body, Attributes.Mind, Attributes.Spirit })
                actor.Attributes[attribute] = new ActorAttributes(attribute, 100, 100, 20, 0, 0);
            item.ItemTemplate.EquipableInfo = new EquipableInfo(50, 0);
            var itemClass = new ItemClassInfo(new ItemClassEntry { MaxHitPoints = 100 });
            Assert.AreEqual(EquipmentRequirementFailure.None, EquipmentRequirements.Check(actor, item, itemClass));
            actor.Level = 9;
            Assert.AreEqual(EquipmentRequirementFailure.MinimumLevel, EquipmentRequirements.Check(actor, item, itemClass));
            actor.Level = 11;
            Assert.AreEqual(EquipmentRequirementFailure.MaximumLevel, EquipmentRequirements.Check(actor, item, itemClass));
            actor.Level = 10;
            foreach (var pair in new[] { (Attributes.Body, EquipmentRequirementFailure.Body),
                (Attributes.Mind, EquipmentRequirementFailure.Mind), (Attributes.Spirit, EquipmentRequirementFailure.Spirit) })
            {
                actor.Attributes[pair.Item1].Current = 19;
                Assert.AreEqual(pair.Item2, EquipmentRequirements.Check(actor, item, itemClass));
                actor.Attributes[pair.Item1].Current = 20;
            }
            actor.Race = Race.Human;
            Assert.AreEqual(EquipmentRequirementFailure.Race, EquipmentRequirements.Check(actor, item, itemClass));
            actor.Race = Race.Forean;
            item.ItemTemplate.EquipableInfo.SkillLevel = 1;
            Assert.AreEqual(EquipmentRequirementFailure.Skill, EquipmentRequirements.Check(actor, item, itemClass));
            actor.Skills[(SkillId)50] = new SkillsData((SkillId)50, 1);
            Assert.AreEqual(EquipmentRequirementFailure.None, EquipmentRequirements.Check(actor, item, itemClass));
            item.CurrentHitPoints = 0;
            Assert.AreEqual(EquipmentRequirementFailure.Broken, EquipmentRequirements.Check(actor, item, itemClass));
            item.CurrentHitPoints = -1;
            Assert.AreEqual(EquipmentRequirementFailure.None, EquipmentRequirements.Check(actor, item, itemClass));
        }
    }
}
