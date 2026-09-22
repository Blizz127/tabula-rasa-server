using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// The Staging Point's own NPCs, brought back to the Staging Point; and the CELLAR Arena's hospital, moved
    /// onto the map the arena is on.
    ///
    /// <b>The Staging Point.</b> Captain McShay, Field Technician McCain and Field Commander Twitty are upstream
    /// spawn pools named after a place they do not stand in: they sit at x 739-741 in the Palisades, about 150 m
    /// from the Staging Point. Two independent things say so. This world's own <c>teleporter</c> rows - client
    /// data, not ours - put the Staging Point hospital at 868.4, -220.3 and its waypoint at 897.3, -233.1. And
    /// codex-tr.net, a period pin-map recovered from the Internet Archive, puts the three of them at 892/-224,
    /// 887/-216 and 884/-215, which is 10 to 25 m from those fixtures. A community map agreeing with our own
    /// original fixtures against an upstream position with no provenance is enough to move them. Y is the navmesh
    /// floor, as always; codex records no heights at all.
    ///
    /// codex-tr.net is readable again because its coordinate encoding was worked out: the site stored game
    /// coordinates multiplied by ten, so <c>pos_x = locW / 10</c> and <c>pos_z = locS / 10</c> in world metres.
    /// The check that proves it: 21 of the 22 Logos stones it pins on those two maps agree with our own
    /// <c>logos</c> rows to under 2 m.
    ///
    /// <b>The arena hospital.</b> "Hospital: CELLAR Arena Medic" sat on map context 2259 at 24.9, 40.0, 114.79
    /// while the medic that defines it - spawn pool 500006, the CELLAR Arena Medic herself - stands at those
    /// exact coordinates on 20000009, and the client's own marker for it (uimapmarker 134419591463366, UI map key
    /// 2232, text 1222) says 20000009 too. Map 2259 carries nothing else whatsoever: no spawn, no placement, no
    /// map link. So the arena had no respawn point and 2259 had a hospital for a room nobody is in.
    /// </summary>
    public static class CodexPlacementFixesRows
    {
        public const string Migration = "CodexPlacementFixes";

        public const uint ArenaHospital = 480u;
        public const uint WargameIndoorArena = 2259u, AfsArena = 20000009u;

        /// <summary>(pool, the x,y,z it had, the x,z codex gives it, the navmesh floor there).</summary>
        private static readonly (uint Id, double WasX, double WasY, double WasZ, double X, double Z, double Floor)[] StagingPoint =
        {
            (510121u, 741.4, 150.0, -267.6, 892.0, -224.0, 123.235),   // Captain McShay
            (510127u, 739.4, 152.4, -271.1, 887.0, -216.0, 124.766),   // Field Technician McCain
            (510125u, 739.4, 151.7, -264.1, 884.0, -215.0, 124.953)    // Field Commander Twitty
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var row in StagingPoint)
                Move(migrationBuilder, row.Id, row.X, row.Floor, row.Z);

            migrationBuilder.UpdateData(table: "teleporter", keyColumn: "id", keyValue: ArenaHospital,
                column: "map_context_id", value: AfsArena);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(table: "teleporter", keyColumn: "id", keyValue: ArenaHospital,
                column: "map_context_id", value: WargameIndoorArena);

            foreach (var row in StagingPoint)
                Move(migrationBuilder, row.Id, row.WasX, row.WasY, row.WasZ);
        }

        private static void Move(MigrationBuilder migrationBuilder, uint id, double x, double y, double z)
        {
            migrationBuilder.UpdateData(table: "spawnpool", keyColumn: "id", keyValue: id, column: "pos_x", value: x);
            migrationBuilder.UpdateData(table: "spawnpool", keyColumn: "id", keyValue: id, column: "pos_y", value: y);
            migrationBuilder.UpdateData(table: "spawnpool", keyColumn: "id", keyValue: id, column: "pos_z", value: z);
        }
    }
}
