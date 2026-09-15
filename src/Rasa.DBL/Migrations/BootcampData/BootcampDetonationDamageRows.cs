using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// The bomb blast hurts the recruit who armed it: rule 1985007 (the bomb detonating) gains a
    /// DamagePlayer action taking 21 off.
    ///
    /// Evidence: the footage shows "-21" leaving the health bar at the blast (B1-049) and the 2005 text
    /// "Not yourself!" (21566) implies self-damage; the value 21 is read straight off that health line, so it
    /// is `observed`, not inferred. The action runs after the rule that opens the wreck, so the recruit takes
    /// it as the dropship goes up.
    /// </summary>
    public static class BootcampDetonationDamageRows
    {
        public const string Migration = "BootcampDetonationDamage";

        private const uint BombDetonationRule = 1985007u;
        private const byte DamageActionSequence = 3;
        private const byte DamagePlayer = 15;

        /// <summary>The damage the footage's "-21" health line records.</summary>
        private const int DetonationDamage = 21;

        private static readonly string[] Columns =
        {
            "rule_id", "sequence", "action", "mission_id", "forced", "greeting_id", "npc_name_id", "tutorial_id",
            "logos_id", "logos_protocol", "experience", "credits", "item_set_id", "placement_id", "state_id",
            "fact_key", "fact_value", "location_id", "audio_set_id", "damage", "comment"
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "content_rule_action",
                columns: Columns,
                values: new object[]
                {
                    BombDetonationRule, DamageActionSequence, DamagePlayer, 0u, false, 0u, 0u, 0u, 0u, (byte)0, 0u, 0,
                    0u, 0u, 0u, string.Empty, 0, 0u, 0u, DetonationDamage, "blast hurts the recruit who armed it"
                });
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "content_rule_action",
                keyColumns: new[] { "rule_id", "sequence" },
                keyValues: new object[] { BombDetonationRule, DamageActionSequence });
        }
    }
}
