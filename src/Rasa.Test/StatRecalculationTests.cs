using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Test
{
    [TestClass]
    [DoNotParallelize]
    public class StatRecalculationTests
    {
        [TestMethod]
        public void RecalculationSetsPrimaryAttributesAndPreservesSpentResources()
        {
            const EntityClasses armorClass = (EntityClasses)9000003;
            var armor = new Item { ItemTemplate = new ItemTemplate(new ItemTemplateItemClassEntry
                { ItemClass = (uint)armorClass }) };
            EntityManager.Instance.RegisterItem(armor.EntityId, armor);
            EntityClassManager.Instance.LoadedEntityClasses[armorClass] =
                new EntityClass((uint)armorClass, "test armor", 0, 0, new List<AugmentationType>(), false)
                { ArmorClassInfo = new ArmorClassInfo(new ArmorClassEntry { MinDamageAbsorbed = 10000, MaxDamageAbsorbed = 10000, RegenRate = 5 }) };
            try
            {
                var player = new Manifestation { Level = 1, Race = Race.Human };
                foreach (Attributes attribute in Enum.GetValues(typeof(Attributes)))
                    player.Attributes[attribute] = new ActorAttributes(attribute, 0, 0, 0, 0, 0);
                player.Inventory.EquippedInventory.AddRange(new ulong[22]);
                player.Inventory.EquippedInventory[1] = armor.EntityId;
                var client = new Client(null, new ClientPacketHandler()) { Player = player };
                ManifestationManager.Instance.UpdateStatsValues(client, true);
                foreach (var attribute in new[] { Attributes.Body, Attributes.Mind, Attributes.Spirit })
                {
                    Assert.IsTrue(player.Attributes[attribute].Current > 0);
                    Assert.AreEqual(player.Attributes[attribute].CurrentMax, player.Attributes[attribute].Current);
                }
                player.Attributes[Attributes.Health].Current = 91;
                player.Attributes[Attributes.Armor].Current = 73;
                player.Attributes[Attributes.Power].Current = 42;
                player.Attributes[Attributes.Chi].Current = 417;
                player.SpentBody = 1;
                player.SpentMind = 1;
                ManifestationManager.Instance.UpdateStatsValues(client, false);
                Assert.AreEqual(73, player.Attributes[Attributes.Armor].Current);
                Assert.AreEqual(91, player.Attributes[Attributes.Health].Current);
                Assert.AreEqual(42, player.Attributes[Attributes.Power].Current);
                Assert.AreEqual(417, player.Attributes[Attributes.Chi].Current);
                Assert.AreEqual(player.Attributes[Attributes.Body].CurrentMax, player.Attributes[Attributes.Body].Current);
                Assert.AreEqual(player.Attributes[Attributes.Mind].CurrentMax, player.Attributes[Attributes.Mind].Current);
            }
            finally
            {
                EntityManager.Instance.UnregisterItem(armor.EntityId);
                EntityClassManager.Instance.LoadedEntityClasses.Remove(armorClass);
            }
        }
    }
}
