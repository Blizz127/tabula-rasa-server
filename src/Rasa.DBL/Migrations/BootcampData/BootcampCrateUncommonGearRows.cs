using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// The boot-camp supply crate's four armour pieces moved from the common Motor Assist row to the uncommon one
    /// the footage shows.
    ///
    /// The original tooltip for the crate's gloves reads "Telaract Motor Assist Armor Gloves | Body Armor: 28 |
    /// Regen Rate: 1 per sec | Motor Assist Armor 1: Novice | Min Level: 1" (7Lrst9SG3pk A3-034, t=319.6). Body armour
    /// is the armour class's absorption / 10, and of every Motor Assist gloves template on the server exactly one
    /// absorbs 281 and has no level requirement: 15803, Armor_T1_MotorAssist_V06_UNC_Gloves_01_to_02 (the V04, V05
    /// and V07 rows require levels 22, 15 and 49). The seeded 13096 was the common row - 234, "Body Armor: 23" -
    /// and requires level 15 by the client's own requirement data; the seeded boots, 13066, require level 30.
    ///
    /// No tooltip of the boots, legs or vest is shown. They are taken from the same uncommon band as the gloves,
    /// the level-1 row without a level requirement: boots 12209 (V06), legs 26879 (V05) and vest 12208 (V05) - V06
    /// legs and vest require levels 8 and 34. That band is inferred from the gloves; the maker names (Astra,
    /// Teleract, Hellstrom) are module prefixes the client composes at run time and do not select a template.
    /// </summary>
    public static class BootcampCrateUncommonGearRows
    {
        public const uint CrateItemSet = 19858u;

        public static readonly (uint Was, uint Now)[] Swaps =
        {
            (13066u, 12209u),   // boots
            (13096u, 15803u),   // gloves - the observed tooltip
            (13156u, 26879u),   // legs
            (13186u, 12208u),   // vest
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var (was, now) in Swaps)
                Swap(migrationBuilder, was, now);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var (was, now) in Swaps)
                Swap(migrationBuilder, now, was);
        }

        private static void Swap(MigrationBuilder migrationBuilder, uint from, uint to)
        {
            migrationBuilder.DeleteData(
                table: "content_item_set",
                keyColumns: new[] { "item_set_id", "item_template_id" },
                keyValues: new object[] { CrateItemSet, from });
            migrationBuilder.InsertData(
                table: "content_item_set",
                columns: new[] { "item_set_id", "item_template_id", "quantity" },
                values: new object[] { CrateItemSet, to, 1u });
        }
    }
}
