using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{

    using Structures.World;

    public class NpcPackagePrelader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, NpcPackageEntry.TableName, typeof(NpcPackageEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            // Original client dialogue binds Rogers to package 116. Package 726
            // belongs to River Recon's dying Forean (see river-recon-client-evidence.md).
            yield return new object[] { 100, 116, "test" };
            yield return new object[] { 101, 208, "test" };
        }
    }
}
