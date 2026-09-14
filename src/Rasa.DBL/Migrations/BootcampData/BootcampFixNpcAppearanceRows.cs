using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// The frozen rows of the boot-camp correction migration (<c>BootcampFixNpcAppearance</c>),
    /// inserted and deleted by key only. Both provider migrations call this class, so
    /// <c>SeedMigrationParityTests</c> sees identical operations. Never edit after release.
    ///
    /// The boot-camp NPCs (Major McAllister, Captain Delessio, Corporal Hartmann, Corporal DeSimone)
    /// rendered without head/body equipment: their creature rows carry no creature_appearance rows,
    /// so every equipment slot rendered bare. This seeds the "NPC Clothing Officer v1" set
    /// (entity classes 4021/4022/4023) plus hair 3672, face 24019 and the holstered pistol 27120 —
    /// the exact appearance set of Outpost Commander Rogers (creature 100) in the shipped world
    /// data, an <c>analogue</c> per OD-11 (the boot-camp NPCs' own appearance values are
    /// unrecovered; the footage shows them in AFS officer uniforms).
    /// </summary>
    public static class BootcampFixNpcAppearanceRows
    {
        public const string Migration = "BootcampFixNpcAppearance";

        // (creatureId, slotId, classId, color) — Rogers' officer set.
        private static readonly uint[][] Rows =
        {
            new[] { 198500u, 3u, 4021u, 22120u },
            new[] { 198500u, 13u, 27120u, 1u },
            new[] { 198500u, 14u, 3672u, 14404004u },
            new[] { 198500u, 15u, 4022u, 933202u },
            new[] { 198500u, 16u, 4023u, 13933202u },
            new[] { 198500u, 17u, 24019u, 4286886614u },
            new[] { 198501u, 3u, 4021u, 22120u },
            new[] { 198501u, 13u, 27120u, 1u },
            new[] { 198501u, 14u, 3672u, 14404004u },
            new[] { 198501u, 15u, 4022u, 933202u },
            new[] { 198501u, 16u, 4023u, 13933202u },
            new[] { 198501u, 17u, 24019u, 4286886614u },
            new[] { 198502u, 3u, 4021u, 22120u },
            new[] { 198502u, 13u, 27120u, 1u },
            new[] { 198502u, 14u, 3672u, 14404004u },
            new[] { 198502u, 15u, 4022u, 933202u },
            new[] { 198502u, 16u, 4023u, 13933202u },
            new[] { 198502u, 17u, 24019u, 4286886614u },
            new[] { 198504u, 3u, 4021u, 22120u },
            new[] { 198504u, 13u, 27120u, 1u },
            new[] { 198504u, 14u, 3672u, 14404004u },
            new[] { 198504u, 15u, 4022u, 933202u },
            new[] { 198504u, 16u, 4023u, 13933202u },
            new[] { 198504u, 17u, 24019u, 4286886614u }
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var row in Rows)
                migrationBuilder.InsertData(
                    table: "creature_appearance",
                    columns: new[] { "id", "slot_id", "Class_id", "color" },
                    values: new object[] { row[0], row[1], row[2], row[3] });
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var row in Rows)
                migrationBuilder.DeleteData(
                    table: "creature_appearance",
                    keyColumns: new[] { "id", "slot_id" },
                    keyValues: new object[] { row[0], row[1] });
        }
    }
}
