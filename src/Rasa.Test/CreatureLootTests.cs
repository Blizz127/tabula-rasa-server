using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test
{
    using Rasa.Data;
    using Rasa.Structures;
    using Rasa.Structures.World;

    /// <summary>
    /// The creature loot roll. The seven rows that survived are the only loot data in existence, so what matters is
    /// that the roll reads them the way the original table's columns meant: chance is a percentage, the stack size is
    /// drawn between the two bounds, and every row is rolled on its own.
    /// </summary>
    [TestClass]
    public class CreatureLootTests
    {
        private static CreatureLootData Data(params CreatureLootEntry[] rows)
        {
            var data = new CreatureLootData();
            foreach (var row in rows)
                data.Add(row);
            return data;
        }

        private static CreatureLootEntry Row(uint item, double chance, uint min, uint max)
            => new CreatureLootEntry { ItemTemplateId = item, Chance = chance, StacksizeMin = min, StacksizeMax = max };

        [TestMethod]
        public void AChanceOfAHundredAlwaysDropsAndAZeroChanceNeverDoes()
        {
            var always = CreatureLoot.Roll(Data(Row(28, 100.0, 1, 1)), () => 0.9999, (min, max) => min);
            var never = CreatureLoot.Roll(Data(Row(28, 0.0, 1, 1)), () => 0.0, (min, max) => min);

            Assert.AreEqual(1, always.Count);
            Assert.AreEqual(28u, always[0].ItemTemplateId);
            Assert.AreEqual(0, never.Count);
        }

        [TestMethod]
        public void TheChanceIsAPercentageOfTheRoll()
        {
            // The trainee footsoldier's ammunition row is 12%: a roll at 11.99% drops, at 12% does not.
            var drops = CreatureLoot.Roll(Data(Row(28, 12.0, 1, 35)), () => 0.1199, (min, max) => min);
            var misses = CreatureLoot.Roll(Data(Row(28, 12.0, 1, 35)), () => 0.12, (min, max) => min);

            Assert.AreEqual(1, drops.Count);
            Assert.AreEqual(0, misses.Count);
        }

        [TestMethod]
        public void TheStackSizeIsDrawnBetweenTheOriginalBounds()
        {
            // 1-35 like the original row: the draw is asked for that range, and a single-value row is not drawn at all.
            (int Min, int Max) asked = (0, 0);
            var drops = CreatureLoot.Roll(Data(Row(28, 100.0, 1, 35)), () => 0.0, (min, max) =>
            {
                asked = (min, max);
                return max - 1;
            });

            Assert.AreEqual((1, 36), asked);
            Assert.AreEqual(35u, drops[0].Count);

            var single = CreatureLoot.Roll(Data(Row(13066, 100.0, 1, 1)), () => 0.0, (min, max) => 0);
            Assert.AreEqual(1u, single[0].Count);
        }

        [TestMethod]
        public void EveryRowIsRolledOnItsOwnChance()
        {
            // The real table: 12% cartridges, 5% med packs, five armour pieces at 0.5% each. A sequence of rolls that
            // passes the first two and fails the armour leaves exactly the cartridges and the med pack.
            var rows = Data(Row(28, 12.0, 1, 35), Row(44917, 5.0, 1, 3),
                Row(13066, 0.5, 1, 1), Row(13096, 0.5, 1, 1), Row(13126, 0.5, 1, 1),
                Row(13156, 0.5, 1, 1), Row(13186, 0.5, 1, 1));

            var sequence = new Queue<double>(new[] { 0.01, 0.01, 0.9, 0.9, 0.9, 0.9, 0.9 });
            var drops = CreatureLoot.Roll(rows, () => sequence.Dequeue(), (min, max) => min);

            CollectionAssert.AreEqual(new uint[] { 28u, 44917u }, drops.ConvertAll(drop => drop.ItemTemplateId));
        }

        [TestMethod]
        public void ACreatureWithNoRowsRollsNothingHere()
        {
            // The dispenser keeps its stand-in drop for these (GAP-CREATURE-LOOT); this roll is only the table.
            Assert.AreEqual(0, CreatureLoot.Roll(null, () => 0.0, (min, max) => min).Count);
            Assert.AreEqual(0, CreatureLoot.Roll(new CreatureLootData(), () => 0.0, (min, max) => min).Count);
        }
    }
}
