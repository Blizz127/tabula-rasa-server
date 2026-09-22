using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// The fifteen <c>entityclass</c> rows this server had to invent a name for, given the name the client
    /// puts on them.
    ///
    /// The client's <c>generated/client/entityclass.pyo</c> (1.16.5.0 game.zip, 2009-02-09) has 15,823 rows
    /// and this world has 15,838: fifteen classes are referenced by item templates the retail server shipped
    /// but carry no entityclass row in the client at all. Whoever built the seed filled the name column with
    /// <c>Missing_ItemClassId_1/15</c> … <c>15/15</c>, which is not a name anything ever had.
    ///
    /// The client can name every one of them. <c>generated/client/language/english/
    /// physicalentityclassnamelanguage.pyo</c> - the table <c>clientlanguagemanager.GetEntityClassName</c>
    /// reads, and therefore what a player sees on a tooltip, in a loot line or in a crafting slot - has an
    /// entry for all fifteen, and those entries are what is written here. Two of them (6331, 20987) and three
    /// more (6251, 20759, 20781) are retail's own test records, still carrying the placeholder text the
    /// content tools left; they are copied verbatim, because reproducing what the client says includes
    /// reproducing what it says badly.
    ///
    /// Nothing is placed, sold, looted, rewarded or crafted from these fifteen, so no line of dialogue
    /// changes. What changes is that <c>ItemManager.DefaultInventoryCategory</c> can no longer read the
    /// invented label: its mission rule matched any name beginning "Mis", so "Missing_…" filed Rifle Ammo
    /// (3180) and the Botany Kit (4327) as mission items on the strength of a server typo. The rule itself was
    /// tightened at the same time to the client's own three shapes, so a label like this could not do it
    /// again.
    ///
    /// <c>EntityClassPreloader</c> carries the same fifteen names for new databases, the way
    /// <see cref="Rasa.Migrations.ClientData.EntityClassClientFidelityRows"/> and its seed do.
    /// </summary>
    public static class ClientNamesForItemsRows
    {
        public const string Migration = "ClientNamesForItems";

        /// <summary>(class id, the client's own display name, the label this server invented).</summary>
        public static readonly (uint Id, string Client, string Was)[] Rows =
        {
            (3180, "Rifle Ammo", "Missing_ItemClassId_1/15"),
            (3600, "Combat Suit Paint Tier 3 Soldier", "Missing_ItemClassId_2/15"),
            (3734, "Melanotan for Caucasians", "Missing_ItemClassId_3/15"),
            (3741, "Combat Suit Paint Tier 3 Specialist", "Missing_ItemClassId_4/15"),
            (3743, "Combat Suit Paint Tier 2 Soldier", "Missing_ItemClassId_5/15"),
            (3744, "Combat Suit Paint Tier 2 Specialist", "Missing_ItemClassId_6/15"),
            (3745, "Combat Suit Paint Tier 4 Soldier", "Missing_ItemClassId_7/15"),
            (3746, "Combat Suit Paint Tier 4 Specialist", "Missing_ItemClassId_8/15"),
            (4086, "Melanotan for Africans", "Missing_ItemClassId_9/15"),
            (4327, "Botany Kit", "Missing_ItemClassId_10/15"),
            (6251, "Your favorite text sucks.", "Missing_ItemClassId_11/15"),
            (6331, "RCH_TestRecord has no display text", "Missing_ItemClassId_12/15"),
            (20759, "New record for rholtrop", "Missing_ItemClassId_13/15"),
            (20781, "New record for rholtrop", "Missing_ItemClassId_14/15"),
            (20987, "RCH_ThisBeTestDataHere has no display text.", "Missing_ItemClassId_15/15")
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var row in Rows)
                migrationBuilder.UpdateData(table: "entityclass", keyColumn: "id", keyValue: row.Id,
                    column: "class_name", value: row.Client);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var row in Rows)
                migrationBuilder.UpdateData(table: "entityclass", keyColumn: "id", keyValue: row.Id,
                    column: "class_name", value: row.Was);
        }
    }
}
