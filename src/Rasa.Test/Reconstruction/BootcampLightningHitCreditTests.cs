using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Migrations.BootcampData;

namespace Rasa.Test.Reconstruction
{
    [TestClass]
    public class BootcampLightningHitCreditTests
    {
        [TestMethod]
        public void MigrationChangesOnlyLightningHitRequirementAndRestoresPreviousRule()
        {
            foreach (var rollback in new[] { false, true })
            {
                var builder = new MigrationBuilder(SeedMigrationParity.SqliteProvider);
                if (rollback) BootcampLightningHitCreditRows.DeleteData(builder);
                else BootcampLightningHitCreditRows.InsertData(builder);

                Assert.AreEqual(1, builder.Operations.Count);
                var update = (UpdateDataOperation)builder.Operations.Single();
                Assert.AreEqual("npc_mission_objective_binding", update.Table);
                CollectionAssert.AreEqual(new[] { "mission_id", "objective_id", "binding_id" }, update.KeyColumns);
                CollectionAssert.AreEqual(new object[] { 1992u, 8u, (byte)0 }, update.KeyValues.Cast<object>().ToArray());
                CollectionAssert.AreEqual(new[] { "INTEGER", "INTEGER", "INTEGER" }, update.KeyColumnTypes);
                CollectionAssert.AreEqual(new[] { "destroying_hit_only" }, update.Columns);
                CollectionAssert.AreEqual(new[] { "INTEGER" }, update.ColumnTypes);
                Assert.AreEqual(rollback, update.Values[0, 0]);
            }
        }
    }
}
