using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// Eighteen missions were handed out or turned in at the wrong NPC, found by auditing every seeded mission
    /// against its TaRapedia page (2026-09-19).
    ///
    /// The client's tables do not record who gives or rewards a mission - that is why these were reconstructed in
    /// the first place, each from the NPC whose dialogue package the client binds an objective to, or from the NPC
    /// the last objective's text names. Both are inferences about a different thing: the package that completes an
    /// objective is not necessarily the hand that gives the mission. TaRapedia's mission infoboxes record
    /// MissionGiver and RewardGiver directly, from play, and where the two disagree the page is the better source.
    ///
    /// Mission 1407 is the plainest case: its own manifest entry reads "TaRapedia's infobox names Outpost Commander
    /// Rogers as the giver", and the row seeded Council Elder Moawi. Rogers is creature 198514 here - the world
    /// seed's own Rogers (100) sits in a spawn pool at TaRapedia's coordinates with a count of zero, so he never
    /// appears.
    ///
    /// Every NPC named below already stands in the world. Twenty-three more mismatches name NPCs the world does not
    /// have; they stay as they are (GAP-W3-TARAPEDIA-GIVERS).
    /// </summary>
    public static class TarapediaGiverAuditRows
    {
        public const string Migration = "TarapediaGiverAudit";

        // mission, column, old creature, new creature
        private static readonly object[][] Rows =
        {
            new object[] { 366u, "giver_id", 199100u, 199103u },   // Warden Lagori
            new object[] { 367u, "giver_id", 199101u, 199104u },   // Warden Kahlee
            new object[] { 413u, "giver_id", 199104u, 199101u },   // Ranger Urialia
            new object[] { 421u, "reciver_id", 120u, 116u },   // Information Spec. Saviours
            new object[] { 444u, "reciver_id", 140u, 94u },   // Dr. Eleanor Corman
            new object[] { 451u, "reciver_id", 93u, 91u },   // Council Advisor Todae
            new object[] { 682u, "reciver_id", 91u, 92u },   // Council Luminary Doyan
            new object[] { 698u, "reciver_id", 113u, 94u },   // Dr. Eleanor Corman
            new object[] { 977u, "reciver_id", 199402u, 199408u },   // Major Ston
            new object[] { 1040u, "giver_id", 199202u, 199206u },   // Colonel Bosley
            new object[] { 1040u, "reciver_id", 199202u, 199206u },   // Colonel Bosley
            new object[] { 1119u, "reciver_id", 199503u, 199506u },   // Surveyor Miras
            new object[] { 1122u, "giver_id", 199503u, 199506u },   // Surveyor Miras
            new object[] { 1125u, "giver_id", 199502u, 199503u },   // Engineer Tralos
            new object[] { 1183u, "reciver_id", 199502u, 199507u },   // Sargeant Phenix
            new object[] { 1390u, "giver_id", 42u, 43u },   // Warrior Apirka
            new object[] { 1407u, "giver_id", 38u, 198514u },   // Outpost Commander Rogers
            new object[] { 1904u, "reciver_id", 199303u, 199302u },   // Retread Lou
        };

        public static void InsertData(MigrationBuilder migrationBuilder) => Apply(migrationBuilder, 3);

        public static void DeleteData(MigrationBuilder migrationBuilder) => Apply(migrationBuilder, 2);

        private static void Apply(MigrationBuilder migrationBuilder, int valueIndex)
        {
            foreach (var row in Rows)
                migrationBuilder.UpdateData(table: "npc_mission", keyColumn: "id", keyValue: row[0],
                    column: (string)row[1], value: row[valueIndex]);
        }
    }
}
