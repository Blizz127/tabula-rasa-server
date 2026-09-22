using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// The last eight NPCs TaRapedia documents and this world lacked, and five bodies it puts somewhere else.
    ///
    /// <see cref="TarapediaMissingNpcBatchRows"/> left nine of the wiki's NPCs out because their pages named no
    /// zone that resolved to a map here. Eight of those nine were a parser failure, not a source failure: those
    /// pages write the location row as plain table cells ("| Foreas | Valverde | Marshes | Paludos") where the
    /// rest use wiki links, and the reader only read links. Read properly, all eight name a zone this world
    /// carries, and every one of them lands on the navmesh floor within a metre of the wiki's own Y - the
    /// closest corroboration this source has given yet. The ninth, Commander Elvers, is still out: he belongs to
    /// the boot camp and so to an owner decision.
    ///
    /// Reading the rest of the wiki properly moved four more NPCs out of the "zone unknown" bucket and into
    /// disagreement, and corrected two of our own:
    ///
    ///   * <b>Lieutenant Holloway</b> stood on the Pools map at a height of 862 m. The wiki puts him in Retread
    ///     City in the Marshes, where the floor is 216.8 m and his wiki Y reads 216. That is a map change, and
    ///     the evidence for it is the map's own ground.
    ///   * <b>Rijii</b> was created by the batch above on the Plains, because his zone read "Brann LZ" and the
    ///     only walkable ground at his x,z was there. His page in fact reads Arieki / Torden / Mires / Brann LZ,
    ///     "In the same room as the Brann LZ Waypoint", and the Mires floor at that x,z is 238.28 against his
    ///     wiki Y of 238.5. He moves to the Mires.
    ///   * <b>MP Price</b>, <b>Sergeant Elway</b> and <b>The Director</b> stood 116, 46 and 104 m from the
    ///     wiki's reading, each of which lands on the floor of the zone the same page names.
    ///
    /// Not taken, and worth writing down: <b>Ranger Taavik</b>, <b>Shaman Geli</b>, <b>Sergeant Starling</b> and
    /// <b>Retread McCormick</b> all carry the identical coordinate -268, 216, -812, which is Retread Karl's. The
    /// first three were edited within minutes of each other on 2007-12-15 (revisions 21096, 21097, 21099) and
    /// their pages say Paludos while that coordinate is Retread City. It is one editor's copy-paste, not four
    /// NPCs on one spot, so their positions stay as they are.
    ///
    /// Classes and bodies are analogues on the same terms as the batch above: the class this world already uses
    /// for that species and gender, and a shipped NPC's appearance set. Jumna is an Eloh, and the Eloh appear as
    /// holograms, so he takes NPC_Holographic_Eloh and needs no body.
    /// </summary>
    public static class TarapediaLastNpcsRows
    {
        public const string Migration = "TarapediaLastNpcs";

        /// <summary>The same donor sets TarapediaMissingNpcBatchRows uses.</summary>
        private static readonly System.Collections.Generic.Dictionary<uint, uint[][]> Bodies = new()
        {
            { 3846u, new[] { new[] { 3u, 4021u, 22120u }, new[] { 13u, 27120u, 1u }, new[] { 14u, 3672u, 1u },
                new[] { 15u, 4022u, 13933202u }, new[] { 16u, 4023u, 13933202u }, new[] { 17u, 20824u, 4286886614u } } },
            { 3848u, new[] { new[] { 2u, 4021u, 266553u }, new[] { 14u, 25255u, 266553u }, new[] { 15u, 19296u, 266553u },
                new[] { 16u, 19250u, 266553u }, new[] { 17u, 24019u, 4286886614u } } }
        };

        // id, comment, name id, entity class, faction, level, map, x, floor, z, provenance
        private static readonly object[][] Npcs =
        {
            new object[] { 199074u, "Commander Figgins (Falcon Hold)", 8623u, 3846u, 1u, 37u, 1454u, -186.000, 219.377, 628.000, "Marshes/Falcon Hold, TaRapedia rev 7983 2007-08-29" },
            new object[] { 199075u, "Hermit (Fractured Butte)", 4366u, 3846u, 1u, 36u, 1304u, -430.000, 920.624, 115.000, "Pools/Fractured Butte, TaRapedia rev 36085 2009-02-17" },
            new object[] { 199076u, "Jumna (Retread City Subterranean Chamber)", 9951u, 24591u, 1u, 45u, 1454u, -160.000, 111.832, -902.000, "Marshes/Retread City Subterranean Chamber, TaRapedia rev 35672 2008-11-15" },
            new object[] { 199077u, "Ranger Helka (Paludos)", 8603u, 26833u, 1u, 37u, 1454u, -564.000, 217.411, -53.000, "Marshes/Paludos, TaRapedia rev 35634 2008-11-12; the wiki leaves the level blank; it takes 37, the level its own page gives Shaman Siva, who stands at Paludos with them and is an objective of the same mission, Divided We Fall" },
            new object[] { 199078u, "Research Chemist Horsh (Falcon Hold)", 3142u, 3848u, 1u, 40u, 1454u, -168.000, 219.178, 593.000, "Marshes/Falcon Hold, TaRapedia rev 7916 2007-08-29" },
            new object[] { 199079u, "Retread Karl (Retread City)", 8636u, 3846u, 1u, 36u, 1454u, -268.000, 216.639, -812.000, "Marshes/Retread City, TaRapedia rev 21088 2007-12-15" },
            new object[] { 199080u, "Shaman Siva (Paludos)", 8602u, 26833u, 1u, 37u, 1454u, -291.000, 218.221, 25.000, "Marshes/Paludos, TaRapedia rev 35633 2008-11-12" },
            new object[] { 199081u, "Warrior Veska (Paludos)", 8600u, 26833u, 1u, 37u, 1454u, -38.000, 218.923, 49.000, "Marshes/Paludos, TaRapedia rev 31148 2008-06-10; the wiki leaves the level blank; it takes 37, the level its own page gives Shaman Siva, who stands at Paludos with them and is an objective of the same mission, Divided We Fall" },
        };

        /// <summary>(table, id, the map and x,y,z it had, the map and x,y,z it takes).</summary>
        private static readonly (string Table, uint Id, uint WasMap, double WasX, double WasY, double WasZ,
            uint Map, double X, double Y, double Z)[] Moves =
        {
            ("spawnpool", 510163u, 1454u, -317.600, 218.480, -2.800, 1454u, -289.000, 218.415, 110.000), // MP Price, wiki rev 8312 2007-08-31
            ("spawnpool", 510169u, 1454u, -142.700, 219.280, 609.200, 1454u, -187.000, 219.375, 620.000), // Sergeant Elway, wiki rev 7912 2007-08-29
            ("spawnpool", 510060u, 2028u, -434.200, 532.240, -141.600, 2028u, -537.800, 533.836, -152.500), // The Director, wiki rev 36188 2009-02-27
            ("spawnpool", 510199u, 1304u, -147.700, 861.662, 524.000, 1454u, -285.000, 216.815, -753.000), // Lieutenant Holloway, wiki rev 30771 2008-05-01
            ("content_placement", 199042u, 1764u, 14.600, 385.347, -629.000, 1759u, 14.600, 238.284, -629.000), // Rijii, wiki rev 26031 2008-01-05
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var npc in Npcs)
            {
                var id = (uint)npc[0];

                migrationBuilder.InsertData(
                    table: "creature",
                    columns: new[] { "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed",
                        "walk_speed", "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8" },
                    values: new object[] { id, npc[1], npc[3], npc[4], npc[5], 555u, npc[2], 9u, 5u,
                        0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u });

                migrationBuilder.InsertData(
                    table: "content_placement",
                    columns: new[] { "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id",
                        "usable_kind", "pos_x", "pos_y", "pos_z", "rotation", "behavior", "initial_state", "alternate_state",
                        "alternate_state_condition_id", "windup_ms", "name_override_id", "hit_points", "restore_ms",
                        "fuse_ms", "loot_item_set_id", "respawn_ms", "present_condition_id", "usable_condition_id", "comment" },
                    values: new object[] { id, npc[6], (byte)1, id, 0u, 0u, (byte)0, npc[7], npc[8], npc[9], 0.0,
                        (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, $"{npc[1]} ({npc[10]})" });

                if (!Bodies.TryGetValue((uint)npc[3], out var body))
                    continue;

                foreach (var piece in body)
                    migrationBuilder.InsertData(table: "creature_appearance", columns: new[] { "id", "slot_id", "Class_id", "color" },
                        values: new object[] { id, piece[0], piece[1], piece[2] });
            }

            foreach (var move in Moves)
                Move(migrationBuilder, move.Table, move.Id, move.Map, move.X, move.Y, move.Z);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var move in Moves)
                Move(migrationBuilder, move.Table, move.Id, move.WasMap, move.WasX, move.WasY, move.WasZ);

            foreach (var npc in Npcs)
            {
                var id = (uint)npc[0];

                if (Bodies.TryGetValue((uint)npc[3], out var body))
                    foreach (var piece in body)
                        migrationBuilder.DeleteData(table: "creature_appearance", keyColumns: new[] { "id", "slot_id" },
                            keyValues: new object[] { id, piece[0] });

                migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { id });
                migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { id });
            }
        }

        private static void Move(MigrationBuilder migrationBuilder, string table, uint id, uint map,
            double x, double y, double z)
        {
            migrationBuilder.UpdateData(table: table, keyColumn: "id", keyValue: id, column: "map_context_id", value: map);
            migrationBuilder.UpdateData(table: table, keyColumn: "id", keyValue: id, column: "pos_x", value: x);
            migrationBuilder.UpdateData(table: table, keyColumn: "id", keyValue: id, column: "pos_y", value: y);
            migrationBuilder.UpdateData(table: table, keyColumn: "id", keyValue: id, column: "pos_z", value: z);
        }
    }
}
