using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{

    using Structures.World;

    public class ItemTemplateArmorPrelaoder : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, ItemTemplateArmorEntry.TableName, typeof(ItemTemplateArmorEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            // armor_value is round(armorclass.min_damage_absorbed / 10) for the item's
            // class, reached through itemtemplate_itemclass. The last three rows were
            // previously shifted by one position (47 dropped, 0 appended), which
            // contradicted both itemclass.max_hp and NewCharacterTests.
            yield return new object[] { 13066, 35 };
            yield return new object[] { 13096, 23 };
            yield return new object[] { 13126, 47 };
            yield return new object[] { 13156, 59 };
            yield return new object[] { 13186, 70 };

        }
    }
}
