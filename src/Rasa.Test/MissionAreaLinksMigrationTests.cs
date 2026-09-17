using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Configuration;
using Rasa.Configuration.ContextSetup;
using Rasa.Context.World;
using Rasa.Migrations.WildernessData;
using Rasa.Services.DbContext;

namespace Rasa.Test
{
    /// <summary>
    /// MissionAreaLinks (W3 batch 16) re-ties the cross-zone missions to the NPCs and maps the original had, and
    /// rolls back to exactly the collapsed state the zone batches left.
    /// </summary>
    [TestClass]
    public class MissionAreaLinksMigrationTests
    {
        private const string PreviousMigration = "20260918040000_WildernessEscortMilpas";

        private sealed class TestConfiguration : IDbContextConfigurationService
        {
            private readonly SqliteConnection _connection;
            public TestConfiguration(SqliteConnection connection) => _connection = connection;
            public void Configure(DbContextOptionsBuilder builder, DatabaseConnectionConfiguration configuration)
                => builder.UseSqlite(_connection);
        }

        private static SqliteWorldContext World(SqliteConnection connection)
            => new SqliteWorldContext(Options.Create(new DatabaseConfiguration()), new TestConfiguration(connection), new SqliteDbContextPropertyModifier());

        private static long Scalar(SqliteConnection connection, string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            return (long)command.ExecuteScalar();
        }

        [TestMethod]
        public void EveryCorrectedMissionIsGivenAndReceivedByASpawnedCreatureOnTheRightMap()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = World(connection);
            context.Database.Migrate();

            foreach (var (missionId, giverId, receiverId) in MissionAreaLinksRows.Corrected)
            {
                Assert.AreEqual(giverId, (ulong)Scalar(connection, $"SELECT giver_id FROM npc_mission WHERE id = {missionId}"), $"giver of {missionId}");
                Assert.AreEqual(receiverId, (ulong)Scalar(connection, $"SELECT reciver_id FROM npc_mission WHERE id = {missionId}"), $"receiver of {missionId}");
                foreach (var creatureId in new[] { giverId, receiverId })
                    Assert.AreEqual(1, Scalar(connection, $"SELECT COUNT(*) FROM content_placement WHERE kind = 1 AND creature_id = {creatureId}"),
                        $"creature {creatureId} of mission {missionId} is placed exactly once");
            }

            // The hand-off givers stand in the zone the mission is given in, the receivers in the next one.
            Assert.AreEqual(1148, Scalar(connection, "SELECT map_context_id FROM content_placement WHERE creature_id = 199004"), "Noonan: Divide");
            Assert.AreEqual(1148, Scalar(connection, "SELECT map_context_id FROM content_placement WHERE creature_id = 199005"), "Franz: Divide");
            Assert.AreEqual(1497, Scalar(connection, "SELECT map_context_id FROM content_placement WHERE creature_id = 199206"), "Bosley: Plateau");
            Assert.AreEqual(1764, Scalar(connection, "SELECT map_context_id FROM content_placement WHERE creature_id = 199505"), "Repp: Plains");
            Assert.AreEqual(1761, Scalar(connection, "SELECT map_context_id FROM content_placement WHERE creature_id = 199602"), "Parsons: Incline");
            Assert.AreEqual(1761, Scalar(connection, "SELECT map_context_id FROM content_placement WHERE creature_id = 199603"), "Gerry: Incline");
            Assert.AreEqual(1148, Scalar(connection, "SELECT map_context_id FROM content_placement WHERE id = 199002"), "Dawson moved to the Divide");
            Assert.AreEqual(1148, Scalar(connection, "SELECT map_context_id FROM content_placement WHERE id = 199003"), "Brice moved to the Divide");
            Assert.AreEqual(1220, Scalar(connection, "SELECT map_context_id FROM content_placement WHERE creature_id = 101"), "Witherspoon: Wilderness");
            Assert.AreEqual(1220, Scalar(connection, "SELECT map_context_id FROM content_placement WHERE creature_id = 38"), "Moawi: Wilderness");

            // Each objective conversation of the corrected missions completes through a package one of its NPCs carries -
            // except the two where the client's own table names a package no creature in this world carries, and measuring
            // that exactly is the point of this loop rather than letting it pass quietly.
            //
            // 422 ("Miner Difficulties") completes through package 213 and 429 ("River Recon") through 726.
            // Object (1) of objectiveconversation holds {(422, 1, 213, 1, 1): [1276]} and {(429, 1, 726, 1, 1)}, and neither
            // package is bound to any creature: the world seed's 155 Wilderness creatures plus the ones the batches created
            // do not include their owners. Both greetings also identify someone other than the officer these missions were
            // assigned to - 213's is a Forean's "our kind were not born of this world, but now it is our only home" and
            // 726's is a drill instructor's "you've managed to piss off pretty much everybody around here" - while Rogers
            // carries 116. Until the NPC behind each package is identified or created, neither mission can be completed
            // in game: GAP-W3-UNBOUND-CONVERSATION-PACKAGE.
            var unbound = new List<uint>();
            foreach (var (missionId, giverId, receiverId) in MissionAreaLinksRows.Corrected)
            {
                var carried = new[] { giverId, receiverId }
                    .Select(creature => Scalar(connection, $"SELECT COALESCE((SELECT npc_package_id FROM content_placement WHERE creature_id = {creature} AND npc_package_id <> 0), (SELECT package_id FROM npc_package WHERE id = {creature}), 0)"))
                    .ToHashSet();

                if (Scalar(connection, $"SELECT COUNT(*) FROM npc_mission_objective_conversation WHERE mission_id = {missionId} AND npc_package_id NOT IN ({string.Join(",", carried)}) AND npc_package_id NOT IN (SELECT package_id FROM npc_package)") > 0)
                    unbound.Add(missionId);
            }

            CollectionAssert.AreEquivalent(new uint[] { 422u, 429u }, unbound,
                "only these two corrected missions complete through a package no creature carries");

            // Brice speaks with his own package on both rows; Noonan has the one 1743 completes through.
            Assert.AreEqual(2050, Scalar(connection, "SELECT package_id FROM npc_package WHERE id = 199003"));
            Assert.AreEqual(2050, Scalar(connection, "SELECT npc_package_id FROM content_placement WHERE id = 199003"));
            Assert.AreEqual(2051, Scalar(connection, "SELECT package_id FROM npc_package WHERE id = 199004"));

            // The new NPCs carry the client's name ids.
            Assert.AreEqual(10013, Scalar(connection, "SELECT name_id FROM creature WHERE id = 199004"));
            Assert.AreEqual(6945, Scalar(connection, "SELECT name_id FROM creature WHERE id = 199005"));
            Assert.AreEqual(8618, Scalar(connection, "SELECT name_id FROM creature WHERE id = 199206"));
            Assert.AreEqual(9940, Scalar(connection, "SELECT name_id FROM creature WHERE id = 199505"));
            Assert.AreEqual(10270, Scalar(connection, "SELECT name_id FROM creature WHERE id = 199602"));
            Assert.AreEqual(10271, Scalar(connection, "SELECT name_id FROM creature WHERE id = 199603"));
        }

        [TestMethod]
        public void RollbackRestoresTheCollapsedGiversAndRemovesTheNewNpcs()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = World(connection);
            context.Database.Migrate();

            context.GetService<IMigrator>().Migrate(PreviousMigration);

            foreach (var (missionId, giverId, receiverId) in MissionAreaLinksRows.Previous)
            {
                Assert.AreEqual(giverId, (ulong)Scalar(connection, $"SELECT giver_id FROM npc_mission WHERE id = {missionId}"), $"giver of {missionId}");
                Assert.AreEqual(receiverId, (ulong)Scalar(connection, $"SELECT reciver_id FROM npc_mission WHERE id = {missionId}"), $"receiver of {missionId}");
            }

            Assert.AreEqual(0, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id IN (199004, 199005, 199206, 199505, 199602, 199603)"));
            Assert.AreEqual(0, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id IN (199004, 199005, 199206, 199505, 199602, 199603, 198686, 198687)"));
            Assert.AreEqual(0, Scalar(connection, "SELECT COUNT(*) FROM npc_package WHERE id IN (199004, 199005, 199206, 199505, 199602, 199603)"));
            Assert.AreEqual(1220, Scalar(connection, "SELECT map_context_id FROM content_placement WHERE id = 199002"));
            Assert.AreEqual(1220, Scalar(connection, "SELECT map_context_id FROM content_placement WHERE id = 199003"));
            Assert.AreEqual(2051, Scalar(connection, "SELECT package_id FROM npc_package WHERE id = 199003"));
            Assert.AreEqual(2051, Scalar(connection, "SELECT npc_package_id FROM content_placement WHERE id = 199003"));

            context.Database.Migrate();
            Assert.AreEqual(199206, Scalar(connection, "SELECT giver_id FROM npc_mission WHERE id = 1068"), "re-applying restores the batch");
        }
    }
}
