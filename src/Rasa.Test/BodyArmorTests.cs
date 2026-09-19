using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Test
{
    [TestClass]
    [DoNotParallelize]
    public class BodyArmorTests
    {
        private static Item Armor(EntityClasses itemClass, uint absorbed, int seeded = 0)
        {
            EntityClassManager.Instance.LoadedEntityClasses[itemClass] =
                new EntityClass((uint)itemClass, "test armor", 0, 0, new List<AugmentationType>(), false)
                { ArmorClassInfo = new ArmorClassInfo(new ArmorClassEntry { MinDamageAbsorbed = absorbed, MaxDamageAbsorbed = absorbed, RegenRate = 1 }) };
            return new Item { ItemTemplate = new ItemTemplate(new ItemTemplateItemClassEntry
                { ItemClass = (uint)itemClass }) { ArmorValue = seeded } };
        }

        [TestMethod]
        public void ArmorIsTheClientsAbsorptionOverTenTruncated()
        {
            const EntityClasses gloves = (EntityClasses)9000011;
            try
            {
                // Original tooltips: 281 -> 28 (footage A3-034) and 736 -> 73 (Pulsar Reflective gloves, which
                // rounding would show as 74).
                Assert.AreEqual(28, ManifestationManager.BodyArmor(Armor(gloves, 281)));
                Assert.AreEqual(73, ManifestationManager.BodyArmor(Armor(gloves, 736)));
                Assert.AreEqual(23, ManifestationManager.BodyArmor(Armor(gloves, 235)));
            }
            finally
            {
                EntityClassManager.Instance.LoadedEntityClasses.Remove(gloves);
            }
        }

        [TestMethod]
        public void TheInfiniteRasaArmorValueIsIgnored()
        {
            // itemtemplate_armor came from Infinite Rasa and has no client counterpart; the client's class decides.
            const EntityClasses chest = (EntityClasses)9000012;
            try
            {
                Assert.AreEqual(28, ManifestationManager.BodyArmor(Armor(chest, 281, 55)));
            }
            finally
            {
                EntityClassManager.Instance.LoadedEntityClasses.Remove(chest);
            }
        }

        [TestMethod]
        public void AnItemWithoutAnArmorClassGivesNothing()
        {
            var item = new Item { ItemTemplate = new ItemTemplate(new ItemTemplateItemClassEntry
                { ItemClass = 9000013 }) };
            Assert.AreEqual(0, ManifestationManager.BodyArmor(item));
            Assert.AreEqual(0, ManifestationManager.BodyArmor(null));
        }
    }
}
