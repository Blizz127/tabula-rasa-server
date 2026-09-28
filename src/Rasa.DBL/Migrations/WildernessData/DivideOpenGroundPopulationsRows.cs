using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// The Concordia Divide's (1148) open-ground wildlife and Bane groups the ConcordiaAmbientPopulations batch left out
    /// (owner, 2026-09-28, playing on the Divide: "world seems empty").
    ///
    /// <b>Evidence</b> (every field's tier and citation: docs/evidence/bootcamp-d11-reconstruction-manifest.json, migration
    /// DivideOpenGroundPopulations; probes and sources: docs/evidence/ambient-respawn-divide-populations-20260928.json).
    ///   * No final-era footage shows a Divide open-ground fight (research/20260927-footage-study), so places come from text with
    ///     coordinates or a named landmark: TaRapedia and Ten Ton Hammer /loc lines, the BradyGames guide (its leaf-116 map read
    ///     through a fit to the Logos coordinates it prints) and the final 1.16.5.0 map's Bane barracks, sandbags and wrecks. The
    ///     Divide has no recorded rebuild (spot adjustments 2008-04-28, fewer Amoeboids D11, dynamic spawners D14), so the
    ///     launch-era text is used with its date as <c>inferred</c> places, never as an analogue (OD-187).
    ///   * Six places where only the species and area are known are labelled <c>analogue</c> inside that area (OD-188).
    ///   * Levels are analogues in TaRapedia's band 12-18 (north 12, the Hominis patrol 15, the south-east 16, OD-189); counts
    ///     the earlier Divide pools' or the fewest the text reads (OD-190); health, attacks and movement same-class world rows
    ///     (OD-191); the respawn is the owner's staggered 60-300 s (OD-186).
    ///   * Heights are the navmesh floor minus 0.276 m and every pool is reached from the Foreas Base waypoint.
    ///
    /// <b>Seeded.</b> Creatures 1148008-1148012 (Boargar, Hominis Machina, south-east Thrax PFC, Caretaker, Warnet); pools
    /// 1148214-1148234, 68 creatures.
    ///
    /// <b>Held.</b> Predators (flying), Amoeboids (no place; a random component since D11), the D14 dynamic spawners, the Hominis
    /// patrol's officers, Thrax Officers, Purgas Valley and the south-west (no creature recorded) (GAP-DIVIDE-UNPLACED-SPECIES).
    /// </summary>
    public static class DivideOpenGroundPopulationsRows
    {
        public const string Migration = "DivideOpenGroundPopulations";

        public const uint Divide = 1148u;

        // ── GENERATED DATA (tools/build.py) ──
        /// <summary>(id, comment, class, level, hp, name id, action1); faction 0, run 9, walk 5.</summary>
        public static readonly (uint Id, string Comment, uint Class, uint Level, uint Hp, uint NameId, uint Action1)[] Creatures =
        {
            (1148008u, "Boargar (Divide)", 6031u, 12u, 555u, 0u, 4u),
            (1148009u, "Hominis Machina (Divide)", 3868u, 15u, 555u, 0u, 2u),
            (1148010u, "Thrax Infantry PFC L16 (south Divide)", 29769u, 16u, 555u, 7677u, 33u),
            (1148011u, "Caretaker L16 (south Divide)", 9244u, 16u, 1000u, 0u, 16u),
            (1148012u, "Warnet Soldier L16 (south-east Divide)", 6262u, 16u, 555u, 460u, 1u)
        };

        /// <summary>(id, anim type, respawn (100 ms), x, y, z, up to six (creature, count)); map 1148, mode 0, min = max.</summary>
        public static readonly (uint Id, byte Anim, uint Respawn, double X, double Y, double Z, (uint Creature, byte Count)[] Slots)[] Pools =
        {
            // Boargar at the downed airship (Makin' Bacon) (inferred place)
            (1148214u, 0, 2900u, 95.0, 110.429, 300.0, new[] { (1148008u, (byte)3) }),
            // Xanx by the Calla fronds east of Foxtrot (inferred place)
            (1148215u, 0, 1100u, 440.0, 59.236, 75.0, new[] { (1148001u, (byte)2) }),
            // Xanx at the War cave north of Crossroads (inferred place)
            (1148216u, 0, 1800u, 358.0, 127.879, 653.0, new[] { (1148001u, (byte)3) }),
            // Xanx by the Bane terraform kits, north-east (inferred place)
            (1148217u, 0, 2500u, 377.3, 110.124, 852.5, new[] { (1148001u, (byte)2) }),
            // Filchers near the War-cave Xanx (inferred place)
            (1148218u, 0, 700u, 385.0, 107.6, 605.0, new[] { (1148005u, (byte)3) }),
            // Warnets at the Benefactor Valley nests (inferred place)
            (1148219u, 0, 1400u, 625.0, 60.769, 200.0, new[] { (1148006u, (byte)3) }),
            // Warnets at the Enlighten shrine (inferred place)
            (1148220u, 0, 2100u, 619.4, 81.363, -13.0, new[] { (1148006u, (byte)3) }),
            // Warnets at the Minos Caverns entrance (inferred place)
            (1148221u, 0, 2800u, 640.0, 162.093, -124.0, new[] { (1148012u, (byte)3) }),
            // Warnets and Thrax on the road south-east of Delta (analogue place)
            (1148222u, 0, 1000u, 600.0, 121.997, -360.0, new[] { (1148012u, (byte)2), (1148010u, (byte)2) }),
            // Bane patrol on the road north of Minos (analogue place)
            (1148223u, 0, 1700u, 600.0, 69.734, 60.0, new[] { (1148002u, (byte)3), (1148004u, (byte)1) }),
            // Thrax at the Bane sandbags outside Foxtrot (inferred place)
            (1148224u, 0, 2400u, 9.0, 53.248, -152.0, new[] { (1148002u, (byte)3) }),
            // Thrax around Rotting Sal's hill (inferred place)
            (1148225u, 0, 600u, -79.6, 69.342, -152.8, new[] { (1148003u, (byte)3) }),
            // Bane troops at Pogonos More (measured place)
            (1148226u, 0, 1300u, 328.0, 77.075, -436.0, new[] { (1148010u, (byte)3), (1148011u, (byte)1) }),
            // The Meat Grinder's escort (inferred place)
            (1148227u, 0, 2000u, 543.0, 48.86, -532.0, new[] { (1148010u, (byte)5), (1148011u, (byte)2) }),
            // Bane Forward Command, east barracks (inferred place)
            (1148228u, 0, 2700u, 138.0, 106.272, -639.0, new[] { (1148010u, (byte)3), (1148011u, (byte)1) }),
            // Bane Forward Command, west barracks (inferred place)
            (1148229u, 0, 900u, -116.0, 115.98, -631.0, new[] { (1148010u, (byte)3) }),
            // Hominis escort at the Thoria Das fire pit (inferred place)
            (1148230u, 0, 1600u, 130.0, 158.099, 850.0, new[] { (1148009u, (byte)4) }),
            // Thrax south-east of Foreas Base (analogue place)
            (1148231u, 0, 2300u, 150.0, 136.373, 400.0, new[] { (1148002u, (byte)3), (1148004u, (byte)1) }),
            // Boargar in the Northwestern Highlands (analogue place)
            (1148232u, 0, 3000u, -698.6, 163.124, 701.4, new[] { (1148008u, (byte)2) }),
            // Boargar east of Nidu Dav (analogue place)
            (1148233u, 0, 1200u, -401.6, 124.124, 418.8, new[] { (1148008u, (byte)2) }),
            // Filchers on the Thoria Das road (analogue place)
            (1148234u, 0, 1900u, 150.0, 136.251, 720.0, new[] { (1148005u, (byte)2) })
        };
        // ── END GENERATED DATA ──

        private static readonly string[] CreatureColumns =
            { "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed", "walk_speed",
              "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8" };
        private static readonly string[] CreatureTypes =
            { "INTEGER", "varchar(50)", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "REAL", "REAL",
              "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" };
        private static readonly string[] PoolColumns =
        {
            "id", "mode", "anim_type", "respown_time", "pos_x", "pos_y", "pos_z", "rotation", "map_context_id",
            "creature_1_Id", "creature_1_min_count", "creature_1_max_count", "creature_2_Id", "creature_2_min_count", "creature_2_max_count",
            "creature_3_Id", "creature_3_min_count", "creature_3_max_count", "creature_4_Id", "creature_4_min_count", "creature_4_max_count",
            "creature_5_Id", "creature_5_min_count", "creature_5_max_count", "creature_6_Id", "creature_6_min_count", "creature_6_max_count"
        };
        private static readonly string[] PoolTypes =
            new[] { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "REAL", "REAL", "REAL", "REAL", "INTEGER" }.Concat(Integers(18)).ToArray();

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            InsertTyped(migrationBuilder, "creature", CreatureColumns, CreatureTypes,
                Rows(Creatures.Select(c => new object[] { c.Id, c.Comment, c.Class, 0u, c.Level, c.Hp, c.NameId, 9.0, 5.0,
                    c.Action1, 0u, 0u, 0u, 0u, 0u, 0u, 0u })));
            InsertTyped(migrationBuilder, "spawnpool", PoolColumns, PoolTypes, Rows(Pools.Select(PoolRow)));
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            DeleteTyped(migrationBuilder, "spawnpool", new[] { "id" }, Integers(1), Rows(Pools.Select(p => new object[] { p.Id })));
            DeleteTyped(migrationBuilder, "creature", new[] { "id" }, Integers(1), Rows(Creatures.Select(c => new object[] { c.Id })));
        }

        private static object[] PoolRow((uint Id, byte Anim, uint Respawn, double X, double Y, double Z, (uint Creature, byte Count)[] Slots) pool)
        {
            var row = new List<object> { pool.Id, (byte)0, pool.Anim, pool.Respawn, pool.X, pool.Y, pool.Z, 0.0, Divide };
            for (var i = 0; i < 6; i++)
            {
                if (i < pool.Slots.Length)
                    row.AddRange(new object[] { pool.Slots[i].Creature, pool.Slots[i].Count, pool.Slots[i].Count });
                else
                    row.AddRange(new object[] { 0u, (byte)0, (byte)0 });
            }
            return row.ToArray();
        }

        private static string[] Integers(int count) => Enumerable.Repeat("INTEGER", count).ToArray();

        private static object[,] Rows(IEnumerable<object[]> rows)
        {
            var list = rows.ToList();
            var values = new object[list.Count, list.Count == 0 ? 0 : list[0].Length];
            for (var row = 0; row < list.Count; row++)
                for (var column = 0; column < list[row].Length; column++)
                    values[row, column] = list[row][column];
            return values;
        }

        // EF Core 5 does not retain ColumnTypes through its positional InsertData overload; attach them so both
        // providers generate SQL without a target model (the TordenConversationMissions pattern).
        private static void InsertTyped(MigrationBuilder builder, string table, string[] columns, string[] types, object[,] values)
        {
            builder.InsertData(table, columns, values);
            ((InsertDataOperation)builder.Operations.Last()).ColumnTypes = types;
        }

        private static void DeleteTyped(MigrationBuilder builder, string table, string[] columns, string[] types, object[,] values)
        {
            builder.DeleteData(table, columns, values);
            ((DeleteDataOperation)builder.Operations.Last()).KeyColumnTypes = types;
        }
    }
}
