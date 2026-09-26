using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// Two world-seed teleporter rows typed as local teleporters (type 1) that are hospital points, re-typed
    /// to the seed's hospital type (5) now that type-1 pads are gained by walking onto them
    /// (DynamicObjectManager.DynamicObjectProximityWorker, 2026-09-26 client defects). Left at type 1, walking
    /// through either hospital would gain a "local waypoint" the client cannot name.
    ///
    /// Evidence (client 1.16.5.0, game.zip; docs/retail-accuracy.md, 2026-09-26 client defects):
    ///   595 'MIS_INDRACAVERNS_GRAVEYARD' (map 1823) and 597 (map 1977) stand 0.0 m from uimapmarker HOSPITAL
    ///   markers 134419591467905 and 134419591467275 ('AFS Field Medic', type 3) - the markers HospitalCatalog
    ///   already offers as those maps' hospitals (graveyard 36) - and from no LOCAL_TELEPORTER (type 9) marker;
    ///   waypointlanguage has no entry 595 or 597 (its original ids end at 533). Tier: original for the marker
    ///   match, inferred for the type (the seed's own name says graveyard). The rows keep their ids and
    ///   positions; a type-5 row is inert on the server (hospitals come from HospitalCatalog).
    ///
    /// Not changed: 491/492 (wargame map 2374, named in waypointlanguage, also on hospital markers) and 64
    /// "Waypoint: Treeback Ridge" (map 1397, no marker): GAP-LOCAL-TELEPORTER-TYPES.
    /// </summary>
    public static class LocalTeleporterGraveyardsRows
    {
        private const byte LocalTeleporter = 1;
        private const byte Hospital = 5;

        public static readonly uint[] Ids = { 595u, 597u };

        public static void InsertData(MigrationBuilder migrationBuilder) => SetType(migrationBuilder, Hospital);
        public static void DeleteData(MigrationBuilder migrationBuilder) => SetType(migrationBuilder, LocalTeleporter);

        private static void SetType(MigrationBuilder migrationBuilder, byte type)
        {
            foreach (var id in Ids)
            {
                migrationBuilder.UpdateData(table: "teleporter", keyColumn: "id", keyValue: id,
                    column: "type", value: type);
                var operation = (UpdateDataOperation)migrationBuilder.Operations.Last();
                operation.KeyColumnTypes = new[] { "INTEGER" };
                operation.ColumnTypes = new[] { "INTEGER" };
            }
        }
    }
}
