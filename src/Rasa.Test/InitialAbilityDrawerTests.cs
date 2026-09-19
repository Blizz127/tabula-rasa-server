using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Structures;

namespace Rasa.Test
{
    [TestClass]
    public class InitialAbilityDrawerTests
    {
        [TestMethod]
        public void ARecruitStartsWithLightningInTheFirstSlotAndSprintInTheSecond()
        {
            var tray = SkillTraining.InitialRecruitAbilityDrawer;
            CollectionAssert.AreEqual(new[] { 0, 1 }, tray.Select(entry => entry.Slot).ToArray());
            Assert.AreEqual(194, tray.Single(entry => entry.Slot == 0).AbilityId, "Lightning, footage A2-011");
            Assert.AreEqual(401, tray.Single(entry => entry.Slot == 1).AbilityId, "Sprint, footage A3-068");

            // Every ability in the tray belongs to a skill the Recruit already has, so the tray never shows
            // something the character cannot use.
            var known = SkillTraining.CreateInitialRecruitSkills().Values.Select(skill => skill.AbilityId).ToList();
            foreach (var (_, abilityId) in tray)
                CollectionAssert.Contains(known, abilityId);
        }
    }
}
