using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// The instance doors and battlefield links the final client draws but the server never had
    /// (research/20260926-instances, batch 2 "instance travel and death").
    ///
    /// Why: <c>MapLinkPreloader</c> built a door only where the client has a marker at both ends - the entrance
    /// (uimapmarker.maplinkmarkers type 7 INSTANCE_ENTRANCE) on the parent map and the exit (type 8) inside the
    /// instance, which is also where the door arrives. Four live operations have the entrance marker and no exit
    /// marker, so they had no link either way and could not be entered at all: Warnet Caverns 1384 (Palisades),
    /// Ustor Yard 1502 (Plateau), Sanctus Grotto 1823 and The Refuge 2156 (Descent). The Last Stand 2375 has two
    /// exit markers to the CELLAR and no link, and the CELLAR's north-end marker still names the D15 Edmund Range
    /// map 2361 ("Edmund Range OLD", adv_wargame_provinggroundsv002), which D16 replaced with 2374.
    ///
    /// Evidence and tiers (every field in docs/evidence/bootcamp-d11-reconstruction-manifest.json, rows map_link
    /// 199250-199261; client tables decoded statically, client 1.16.5.0):
    ///   * Entrance triggers: the client's own marker - original. uimapmarker.maplinkmarkers[1402] 133182640964138
    ///     (Warnet), [1654] 134269267734139 (Ustor Yard), [2047] 134419591472545 (Sanctus Grotto) and
    ///     134419591466163 (The Refuge); [2378] 134419591466020/-021 (the Last Stand's exits); [2232]
    ///     134419591463502 (the CELLAR's north end, which the client points at template 2365 = context 2361).
    ///   * Arrivals inside the four operations: the instance's entrance hospital marker (factionedmarkers) - inferred.
    ///     Every guide describes the medic at the way in: Ten Ton Hammer Warnet Caverns 2008-01-24 "As you enter
    ///     Warnet Caverns, you'll first see the Field Medic"; Ten Ton Hammer Ustor Yard 2008-03-21 "Upon entering the
    ///     instance you'll find a local waypoint generator and a Field Medic"; the Sanctus Grotto dev journal
    ///     (Massively 2008-02-28) "make the instance end near the entrance". Warnet's two "First Aid Station" markers
    ///     are told apart by the world seed's hospital rows 130 "Warnet Caverns Entrance" and 131 "Warnet Caverns
    ///     Research Area" (131 stands on marker ...009; 130's |x| and z are marker ...010's, the sign of x flipped,
    ///     and the navmesh has no floor at 130's own point) and by graveyardlanguage 48/49 "Warnet Caverns Entrance"
    ///     / "Research Area". The Refuge has one hospital marker ("Eloh Sanctuary"), at a navmesh dead end open to the
    ///     east only; its "AFS Field Medic" vendor marker stands 190 m away on the lake floor (low confidence, OD-130).
    ///   * Exits: the trigger stands on the arrival point, as the client places the exit marker of every door the
    ///     preloader built (48 instance doors); arrival back on the parent's entrance marker - inferred (OD-130).
    ///   * Arrival yaw: the preloader's rule, atan2(x - cx, z - cz) about the destination's gamecontextuimapinfo
    ///     centre, which reproduces every preloader row (e.g. 1347 -0.7466, 1430 -2.4402) - inferred.
    ///   * Radii and kind: the preloader's conventions (instance doors 6 m kind 1, CELLAR hub pads 4 m) - inferred.
    ///   * The Last Stand's two exits arrive on the CELLAR's one link marker with no destination (134419591463417,
    ///     the centre of the ring of zone pads, the only unpaired CELLAR link marker) - inferred (OD-134). The way
    ///     into 2375 stays GAP-LAST-STAND-ENTRY; these rows only give its exits somewhere to go.
    ///   * The CELLAR's north end goes to Edmund Range 2374: D15.7 live 2008-12-13 "CELLAR Arena: Location has been
    ///     expanded, with an entrance to the Edmund Range wargame map at the north end"; D16/D16.4 live 2009-02-09
    ///     "Edmund Range Training Grounds has been enlarged" and D16.3 "the D15 Edmund Range map ... the new version".
    ///     The arrival is the client's "Staging Area" region label on 2374 (staticmarkers 134419591466233), where the
    ///     map's class trainer and vendors stand and where D15.7 sends the losing team - inferred. 2374 has no client
    ///     link marker at all, so its way back is the arrival point (analogue, OD-131; GAP-EDMUND-EXIT).
    ///
    /// Not built: Ustor Yard's western exit to the Maligo bridge (GAP-USTOR-WEST-EXIT); Eloh Vale 2084, which has no
    /// door - it is entered by the dropship missions 1198 "Dropship Teleport" / 1199 "Eloh Vale Transport" / 1429
    /// "Invade Eloh Vale" and left from a terminal that calls a dropship (GAP-ELOH-VALE-DROPSHIP-ENTRY).
    /// Ids 199250-199299 are this batch's block. Both provider migrations call this class. Never edit after release.
    /// </summary>
    public static class InstanceTravelLinksRows
    {
        public const string Migration = "InstanceTravelLinks";

        public const byte Border = 0, Instance = 1;

        public sealed class Row
        {
            public readonly uint Id;
            public readonly uint MapContextId;
            public readonly double X, Y, Z, Radius;
            public readonly uint DestMapContextId;
            public readonly double DestX, DestY, DestZ, DestRotation;
            public readonly byte Kind;
            public readonly string Comment;

            public Row(uint id, uint map, double x, double y, double z, double radius, uint destMap, double destX, double destY,
                double destZ, double destRotation, byte kind, string comment)
            {
                Id = id;
                MapContextId = map;
                X = x;
                Y = y;
                Z = z;
                Radius = radius;
                DestMapContextId = destMap;
                DestX = destX;
                DestY = destY;
                DestZ = destZ;
                DestRotation = destRotation;
                Kind = kind;
                Comment = comment;
            }
        }

        public static readonly string[] Columns =
        {
            "id", "map_context_id", "pos_x", "pos_y", "pos_z", "radius", "dest_map_context_id", "dest_pos_x", "dest_pos_y",
            "dest_pos_z", "dest_rotation", "kind", "enabled", "comment"
        };

        public static readonly Row[] Rows =
        {
            // Warnet Caverns: Palisades entrance marker 133182640964138 <-> the entrance "First Aid Station" 133779641346010.
            new Row(199250u, 1244u, 769.5072, 154.9576, 797.6298, 6.0, 1384u, -120.2371, 97.1975, 45.1418, -0.9443, Instance, "palisades -> palisades_warnetcaverns"),
            new Row(199251u, 1384u, -120.2371, 97.1975, 45.1418, 6.0, 1244u, 769.5072, 154.9576, 797.6298, 0.7189, Instance, "palisades_warnetcaverns -> palisades"),
            // Ustor Yard: Plateau entrance marker 134269267734139 <-> "Ustor Yard Field Medic" 134290742448949.
            new Row(199252u, 1497u, 361.1624, 433.7641, -556.0029, 6.0, 1502u, 356.0609, 169.2913, 0.8273, 1.6167, Instance, "plateau -> plateau_ustoryard"),
            new Row(199253u, 1502u, 356.0609, 169.2913, 0.8273, 6.0, 1497u, 361.1624, 433.7641, -556.0029, 2.553, Instance, "plateau_ustoryard -> plateau"),
            // Sanctus Grotto: Descent entrance marker 134419591472545 <-> "AFS Field Medic" 134419591467905.
            new Row(199254u, 2047u, -570.3525, 629.9304, 718.3977, 6.0, 1823u, 358.1881, 212.8376, -134.2409, 2.428, Instance, "descent -> plateau_sanctusgrotto"),
            new Row(199255u, 1823u, 358.1881, 212.8376, -134.2409, 6.0, 2047u, -570.3525, 629.9304, 718.3977, -0.671, Instance, "plateau_sanctusgrotto -> descent"),
            // The Refuge: Descent entrance marker 134419591466163 <-> "Eloh Sanctuary" 134419591464201.
            new Row(199256u, 2047u, 251.4895, 146.2857, 224.7548, 6.0, 2156u, -23.7891, 10.9411, 56.0241, 1.2426, Instance, "descent -> descent_therefuge"),
            new Row(199257u, 2156u, -23.7891, 10.9411, 56.0241, 6.0, 2047u, 251.4895, 146.2857, 224.7548, 0.8415, Instance, "descent_therefuge -> descent"),
            // The Last Stand's two exit markers 134419591466020/-021 -> the CELLAR's unpaired centre marker 134419591463417.
            new Row(199258u, 2375u, -285.7884, 208.9081, -706.0602, 6.0, 20000009u, -0.1481, 36.5, -19.8979, -0.0037, Border, "manhattan_01_shared -> afs_arena"),
            new Row(199259u, 2375u, -1.9697, 193.9143, 486.6048, 6.0, 20000009u, -0.1481, 36.5, -19.8979, -0.0037, Border, "manhattan_01_shared -> afs_arena"),
            // The CELLAR's north end 134419591463502 <-> Edmund Range's "Staging Area" 134419591466233 (the way back: OD-131).
            new Row(199260u, 20000009u, 9.2683, 40.5004, 136.06, 4.0, 2374u, -64.0, 359.5212, -365.75, 3.1159, Border, "afs_arena -> wargame_edmundrange2"),
            new Row(199261u, 2374u, -64.0, 359.5212, -365.75, 4.0, 20000009u, 9.2683, 40.5004, 136.06, 0.0472, Border, "wargame_edmundrange2 -> afs_arena"),
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var row in Rows)
                migrationBuilder.InsertData(
                    table: "map_link",
                    columns: Columns,
                    values: new object[]
                    {
                        row.Id, row.MapContextId, row.X, row.Y, row.Z, row.Radius, row.DestMapContextId, row.DestX, row.DestY,
                        row.DestZ, row.DestRotation, row.Kind, (byte)1, row.Comment
                    });
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            for (var i = Rows.Length - 1; i >= 0; i--)
                migrationBuilder.DeleteData(table: "map_link", keyColumn: "id", keyValue: Rows[i].Id);
        }
    }
}
