using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Structures;

namespace Rasa.Test
{
    [TestClass]
    public class AdrenalineStatsTests
    {
        [DataTestMethod]
        [DataRow(Race.Human, 1)]
        [DataRow(Race.Forean, 15)]
        [DataRow(Race.Brann, 30)]
        [DataRow(Race.Thrax, 50)]
        public void NormalAdrenalineUsesSignaturePercentageUnitsAndDoesNotScaleWithPower(Race race, int level)
        {
            var player = new Manifestation { Race = race, Level = (byte)level };
            foreach (Attributes attribute in Enum.GetValues(typeof(Attributes)))
                player.Attributes[attribute] = new ActorAttributes(attribute, 0, 0, 0, 0, 0);
            for (var slot = 0; slot < 21; slot++)
                player.Inventory.EquippedInventory.Add(0);
            var client = new Client(null, new ClientPacketHandler()) { Player = player };
            var manager = (ManifestationManager)Activator.CreateInstance(typeof(ManifestationManager),
                BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { null }, null);

            manager.UpdateStatsValues(client, true);
            var chi = player.Attributes[Attributes.Chi];
            Assert.AreEqual(1000, chi.NormalMax);
            Assert.AreEqual(1000, chi.CurrentMax);
            Assert.AreEqual(1000, chi.Current);
            var oldPower = player.Attributes[Attributes.Power].CurrentMax;

            chi.Current = 417;
            player.SpentMind = 10;
            manager.UpdateStatsValues(client, false);
            Assert.IsTrue(player.Attributes[Attributes.Power].CurrentMax > oldPower);
            Assert.AreEqual(1000, chi.NormalMax);
            Assert.AreEqual(1000, chi.CurrentMax);
            Assert.AreEqual(417, chi.Current, "A stat recalculation must preserve spent adrenaline.");
        }
    }
}
