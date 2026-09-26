using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// Two seeded values the official live patch notes contradict (notes audit of 2026-09-26,
    /// research/20260926-notes-audit/findings.json, every live note from 1.4 to D16.5).
    ///
    /// <b>2016 A Mystery Unearthed, level 20 to 50.</b> The Deployment 14 live notes (2008-11-11) say: 'Mission: "A
    /// Mystery Unearthed" - new level 50 mission offered by NPC Archaeologist Wynne Topper at Twin Pillars on Concordia:
    /// Wilderness.' PlateauConversationNpc had seeded 20, the Plateau zone band, because TaRapedia records no level;
    /// the note gives the level itself, so the value is original. The same note puts the giver at Twin Pillars in the
    /// Wilderness (Topper, 199009, since TarapediaMissingNpcs), so the row's comment loses its "(Plateau)" label; the
    /// mission only ends at General Bailey in Fort Defiance on the Plateau.
    ///
    /// <b>682/3 Childhood's End, shared kill credit.</b> The 1.6 live notes (2008-03-26): "Fixed Wilderness: Childhood's
    /// End so that it no longer matters who kills the Xanx, just that they are killed." The D8 live notes (2008-05-19):
    /// "Players will now receive credit for killing the Xanx in \"Childhood's End\" on Wilderness, even if they advanced
    /// to this objective at the same time as another player." Both notes name this one mission, so the rule is set on
    /// its kill binding (shared_kill_credit, MissionSharedKillCredit) and not made general: every other kill binding
    /// keeps killer-only credit. The flag is original; its reach - every character on the same map channel with 3
    /// active - is the server's reading of "just that they are killed" (the notes give no radius; see the manifest).
    ///
    /// Down restores the zone-band level, the old comment and killer-only credit.
    /// </summary>
    public static class OfficialNotesCorrectionsRows
    {
        public const string Migration = "OfficialNotesCorrections";

        public const uint MysteryUnearthed = 2016u;
        public const uint MysteryLevel = 50u, MysteryLevelWas = 20u;
        public const string MysteryComment = "A Mystery Unearthed (Wilderness)";
        public const string MysteryCommentWas = "A Mystery Unearthed (Plateau)";

        public const uint ChildhoodsEnd = 682u;
        public const uint XanxObjective = 3u;

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            Mystery(migrationBuilder, MysteryLevel, MysteryComment);
            SharedCredit(migrationBuilder, true);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            SharedCredit(migrationBuilder, false);
            Mystery(migrationBuilder, MysteryLevelWas, MysteryCommentWas);
        }

        private static void Mystery(MigrationBuilder migrationBuilder, uint level, string comment)
        {
            migrationBuilder.UpdateData(table: "npc_mission", keyColumn: "id", keyValue: MysteryUnearthed, column: "level", value: level);
            migrationBuilder.UpdateData(table: "npc_mission", keyColumn: "id", keyValue: MysteryUnearthed, column: "comment", value: comment);
        }

        private static void SharedCredit(MigrationBuilder migrationBuilder, bool shared)
            => migrationBuilder.UpdateData(
                table: "npc_mission_objective_binding",
                keyColumns: new[] { "mission_id", "objective_id", "binding_id" },
                keyValues: new object[] { ChildhoodsEnd, XanxObjective, (byte)0 },
                column: "shared_kill_credit",
                value: shared);
    }
}
