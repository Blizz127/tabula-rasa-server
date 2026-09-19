using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.ClientData
{
    /// <summary>
    /// Seven <c>entityclass</c> rows put back to what the client's own <c>generated/client/entityclass.pyo</c>
    /// (1.16.5.0 game.zip, sha256 b4e99d0c…) says. A full compare of the 15,823 shared rows on 2026-09-19 found every
    /// mesh, collision role and target flag equal, and these seven different:
    ///
    ///   - four names an old import corrupted by replacing "none", case-insensitively, with "0" (so SignOneway lost
    ///     its "nOne"), and one trailing space the client carries and the import trimmed;
    ///   - 20684 MisCavesofDonn_DyingForean, an NPC to the client (augmentation 52) and a stateless switch (8) here;
    ///   - 28699 DELETEME_BROKEN, which the client gives no augmentations and the server made an equipable item.
    ///     Its one item template (121310) is sold, dropped and rewarded nowhere.
    ///
    /// None of the seven is placed or spawned by the world seed, so nothing in play changes; the rows are corrected so
    /// that anything built on them later starts from the client's truth.
    /// </summary>
    public static class EntityClassClientFidelityRows
    {
        public static readonly (uint Id, string Column, string Client, string Was)[] Rows =
        {
            (21307, "class_name", "UsableItemDispElohLogosNoneV01", "UsableItemDispElohLogos0V01"),
            (22362, "class_name", "ArchElohLogosSignNone", "ArchElohLogosSign0"),
            (22822, "class_name", "ItemElohLogosNone", "ItemElohLogos0"),
            (30475, "class_name", "PropEarthSignOnewayV01", "PropEarthSig0wayV01"),
            (30714, "class_name", "Weapon_Avatar_Sunset_MachineGun_v3_Physical_Sajud_Soljar ", "Weapon_Avatar_Sunset_MachineGun_v3_Physical_Sajud_Soljar"),
            (20684, "aug_list", "52", "8"),
            (28699, "aug_list", "", "4,6"),
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var row in Rows)
                migrationBuilder.UpdateData(table: "entityclass", keyColumn: "id", keyValue: row.Id, column: row.Column, value: row.Client);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var row in Rows)
                migrationBuilder.UpdateData(table: "entityclass", keyColumn: "id", keyValue: row.Id, column: row.Column, value: row.Was);
        }
    }
}
