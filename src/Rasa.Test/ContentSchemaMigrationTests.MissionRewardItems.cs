using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test
{
    public partial class ContentSchemaMigrationTests
    {
        /// <summary>
        /// The approved reward-item list of the 2026-09-26 review (research/20260926-reward-items/REVIEW.md), written
        /// out independently of MissionRewardItemsRows so the seed is checked against the list, not against itself.
        /// </summary>
        internal static readonly (uint Mission, (uint Template, uint Quantity)[] Items, int Credits, int Experience)[] ApprovedMissionRewardItems =
        {
            (1541, new[] { (120418u, 1u), (120419u, 1u), (120420u, 1u), (120421u, 1u) }, 5100, 55000),
            (1673, new[] { (120101u, 1u), (120102u, 1u), (120103u, 1u), (120104u, 1u) }, 3800, 75000),
            (1040, new[] { (120382u, 1u), (120383u, 1u), (120384u, 1u), (120385u, 1u) }, 6600, 100000),
            (983, new[] { (120804u, 1u), (120805u, 1u), (120806u, 1u) }, 4200, 31000),
            (970, new[] { (45059u, 2u), (118897u, 2u), (111037u, 4u), (111027u, 4u) }, 3100, 20000),
            (1068, new[] { (45059u, 2u), (118906u, 2u), (111017u, 4u), (111027u, 4u) }, 3300, 25000),
            (1863, new[] { (45062u, 2u), (118898u, 2u), (111048u, 4u), (45446u, 4u) }, 5400, 65000)
        };

        private static void AssertMissionRewardItems(SqliteConnection connection, bool present)
        {
            foreach (var (mission, items, credits, experience) in ApprovedMissionRewardItems)
            {
                // The credit and experience rows of the zone batches are untouched either way.
                Assert.AreEqual(1L, Scalar(connection, $"SELECT COUNT(*) FROM npc_mission_reward WHERE id = {mission} AND type = 1 AND credits = {credits} AND item_template_id = 0"), $"{mission} credits");
                Assert.AreEqual(1L, Scalar(connection, $"SELECT COUNT(*) FROM npc_mission_reward WHERE id = {mission} AND type = 3 AND credits = {experience} AND item_template_id = 0"), $"{mission} experience");
                // No fixed item and nothing but the approved templates.
                Assert.AreEqual(0L, Scalar(connection, $"SELECT COUNT(*) FROM npc_mission_reward WHERE id = {mission} AND type = 4"), $"{mission} fixed items");
                Assert.AreEqual(present ? items.Length : 0, Scalar(connection, $"SELECT COUNT(*) FROM npc_mission_reward WHERE id = {mission} AND type = 5"), $"{mission} selectable items");

                if (!present)
                    continue;

                // Offered in the wiki's order: the rows come back in insertion order.
                var seeded = new List<(uint, uint)>();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT item_template_id, quantity, credits FROM npc_mission_reward WHERE id = {mission} AND type = 5 ORDER BY rowid";
                    using var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        Assert.AreEqual(0L, reader.GetInt64(2), $"{mission}: an item row carries no amount");
                        seeded.Add(((uint)reader.GetInt64(0), (uint)reader.GetInt64(1)));
                    }
                }
                CollectionAssert.AreEqual(items, seeded.ToArray(), $"{mission} reward items");
            }

            // The mission levels are left alone (GAP-MISSION-LEVEL records the contradiction with the reward levels).
            Assert.AreEqual(7L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE (id IN (1541, 1040, 970, 1068) AND level = 20) OR (id IN (1673, 1863) AND level = 25) OR (id = 983 AND level = 30)"));
        }
    }
}
