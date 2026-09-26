using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Migrations.BootcampData;

namespace Rasa.Test
{
    public partial class ContentSchemaMigrationTests
    {
        [TestMethod]
        public void PadHoldDataOperationsSpecifyTypesForUnmappedContentTables()
        {
            // The deployed Game base has no EF entity mapping for these data tables.
            // SQLite's migration generator needs explicit types even though a newer
            // worktree model can run the same migration without them.
            var up = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            BootcampReinforcementPadHoldRows.Apply(up);
            Assert.AreEqual(9, up.Operations.Count);
            Assert.IsTrue(up.Operations.OfType<DeleteDataOperation>()
                .All(operation => operation.KeyColumnTypes != null &&
                    operation.KeyColumnTypes.Length == operation.KeyColumns.Length));

            var down = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            BootcampReinforcementPadHoldRows.Revert(down);
            Assert.AreEqual(8, down.Operations.Count);
            Assert.IsTrue(down.Operations.OfType<InsertDataOperation>()
                .All(operation => operation.ColumnTypes != null &&
                    operation.ColumnTypes.Length == operation.Columns.Length));
        }
    }
}
