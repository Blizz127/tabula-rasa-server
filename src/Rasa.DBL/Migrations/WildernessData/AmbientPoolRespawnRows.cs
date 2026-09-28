using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// The respawn delay of the hostile ambient pools: the 45 pools of the 2026-09-27 population batches
    /// (ConcordiaAmbientPopulations: Divide 1148200-1148211, Palisades 1244200-1244207; FootageAmbientPopulations: Palisades
    /// 1244208-1244213, Torden Plains 1764200-1764210, Torden Mires 1759200-1759207) and the 83 hostile ambient pools of the
    /// Wilderness (1220) world seed. The new Divide pools (DivideOpenGroundPopulations) are seeded with the same rule.
    ///
    /// <b>Why.</b> The 2026-09-27 pools took the world seed's respown_time 20 (2 s after a pool's last creature dies) as an
    /// analogue (OD-164, OD-178); the Wilderness world seed carries 20/25/30/50 (2-5 s), the emulator's development values. The
    /// owner, playing on the Divide on 2026-09-28: "they respawn really fast".
    ///
    /// <b>Decision</b> (OD-186, owner, 2026-09-28 ~14:42 CDT, relayed by the Devbox Coordinator): "60-300 s, not 60-240, staggered
    /// so pools don't sync", for hostile ambient creature pools only. Friendly and service NPC pools (vendors, trainers,
    /// hospitals, mission givers, companions) are not touched, and neither are bosses: the Wilderness 50 s boss pools
    /// (156-159, 520065), the named bosses (Overseers Glognar 81, Phlegg 82, Rankash 91, Graal with his escort 169, the Fithik
    /// Hive Monarch 168), the Divide Targets of Opportunity bosses, and the two 15-minute Class IV Stalkers. Pools 1, 2, 3 and
    /// 132 draw nothing (counts 0) and are left as they are.
    ///
    /// <b>No jitter.</b> The server holds one fixed delay per pool, counted from the pool's last death, and has no per-spawn
    /// jitter. So the stagger is a deterministic spread by pool id: 60 + 10 x ((id x 7) mod 25) s. All 25 steps from 60 to 300 s
    /// are used, and pools with consecutive ids never share a delay.
    ///
    /// <b>Evidence</b> (change entries and citations: docs/evidence/bootcamp-d11-reconstruction-manifest.json, migration
    /// AmbientPoolRespawn; the data points and the Wilderness include/exclude list:
    /// docs/evidence/ambient-respawn-divide-populations-20260928.json). Footage never shows a refill. A final-era Torden Mires
    /// Thrax group was not back 36 +/- 4 s after it died (C2rGwo6fLw0, 2009-02-27); Executor Gantic's site stayed empty for 109 s.
    /// Final-era TaRapedia text gives about 5 minutes (Plateau Miasma bulls, a Palisades patrol) and "fairly quickly" for others.
    /// No source times one pool, so the values are inferred within the owner's range.
    ///
    /// <b>Down</b> restores each pool's previous value exactly (the Old column).
    /// </summary>
    public static class AmbientPoolRespawnRows
    {
        public const string Migration = "AmbientPoolRespawn";

        // ── GENERATED DATA (tools/build.py) ──
        /// <summary>(pool id, old, new) respown_time in 100 ms units; old is the 2026-09-27 batches' world-seed analogue.</summary>
        public static readonly (uint Id, uint Old, uint New)[] Pools =
        {
            (1148200u, 20u, 600u),
            (1148201u, 20u, 1300u),
            (1148202u, 20u, 2000u),
            (1148203u, 20u, 2700u),
            (1148204u, 20u, 900u),
            (1148205u, 20u, 1600u),
            (1148206u, 20u, 2300u),
            (1148207u, 20u, 3000u),
            (1148208u, 20u, 1200u),
            (1148209u, 20u, 1900u),
            (1148210u, 20u, 2600u),
            (1148211u, 20u, 800u),
            (1244200u, 20u, 600u),
            (1244201u, 20u, 1300u),
            (1244202u, 20u, 2000u),
            (1244203u, 20u, 2700u),
            (1244204u, 20u, 900u),
            (1244205u, 20u, 1600u),
            (1244206u, 20u, 2300u),
            (1244207u, 20u, 3000u),
            (1244208u, 20u, 1200u),
            (1244209u, 20u, 1900u),
            (1244210u, 20u, 2600u),
            (1244211u, 20u, 800u),
            (1244212u, 20u, 1500u),
            (1244213u, 20u, 2200u),
            (1764200u, 20u, 600u),
            (1764201u, 20u, 1300u),
            (1764202u, 20u, 2000u),
            (1764203u, 20u, 2700u),
            (1764204u, 20u, 900u),
            (1764205u, 20u, 1600u),
            (1764206u, 20u, 2300u),
            (1764207u, 20u, 3000u),
            (1764208u, 20u, 1200u),
            (1764209u, 20u, 1900u),
            (1764210u, 20u, 2600u),
            (1759200u, 20u, 600u),
            (1759201u, 20u, 1300u),
            (1759202u, 20u, 2000u),
            (1759203u, 20u, 2700u),
            (1759204u, 20u, 900u),
            (1759205u, 20u, 1600u),
            (1759206u, 20u, 2300u),
            (1759207u, 20u, 3000u),
            (37u, 25u, 1500u),
            (38u, 25u, 2200u),
            (39u, 25u, 2900u),
            (40u, 25u, 1100u),
            (41u, 25u, 1800u),
            (42u, 25u, 2500u),
            (43u, 25u, 700u),
            (44u, 25u, 1400u),
            (45u, 25u, 2100u),
            (46u, 25u, 2800u),
            (47u, 25u, 1000u),
            (48u, 25u, 1700u),
            (52u, 25u, 2000u),
            (54u, 25u, 900u),
            (55u, 20u, 1600u),
            (56u, 25u, 2300u),
            (57u, 25u, 3000u),
            (58u, 20u, 1200u),
            (60u, 20u, 2600u),
            (61u, 20u, 800u),
            (69u, 25u, 1400u),
            (70u, 25u, 2100u),
            (73u, 50u, 1700u),
            (74u, 50u, 2400u),
            (75u, 50u, 600u),
            (76u, 50u, 1300u),
            (77u, 50u, 2000u),
            (79u, 50u, 900u),
            (80u, 50u, 1600u),
            (83u, 50u, 1200u),
            (85u, 50u, 2600u),
            (88u, 50u, 2200u),
            (94u, 25u, 1400u),
            (96u, 20u, 2800u),
            (97u, 20u, 1000u),
            (98u, 20u, 1700u),
            (99u, 20u, 2400u),
            (102u, 20u, 2000u),
            (103u, 20u, 2700u),
            (104u, 20u, 900u),
            (105u, 20u, 1600u),
            (108u, 20u, 1200u),
            (109u, 20u, 1900u),
            (110u, 20u, 2600u),
            (111u, 20u, 800u),
            (112u, 20u, 1500u),
            (118u, 20u, 700u),
            (121u, 20u, 2800u),
            (122u, 20u, 1000u),
            (123u, 20u, 1700u),
            (124u, 20u, 2400u),
            (125u, 20u, 600u),
            (126u, 20u, 1300u),
            (127u, 20u, 2000u),
            (128u, 20u, 2700u),
            (129u, 20u, 900u),
            (130u, 20u, 1600u),
            (131u, 20u, 2300u),
            (133u, 20u, 1200u),
            (137u, 20u, 1500u),
            (138u, 20u, 2200u),
            (139u, 20u, 2900u),
            (140u, 20u, 1100u),
            (141u, 20u, 1800u),
            (142u, 20u, 2500u),
            (143u, 20u, 700u),
            (144u, 20u, 1400u),
            (145u, 20u, 2100u),
            (146u, 20u, 2800u),
            (147u, 20u, 1000u),
            (148u, 20u, 1700u),
            (151u, 20u, 1300u),
            (152u, 20u, 2000u),
            (153u, 20u, 2700u),
            (154u, 20u, 900u),
            (160u, 30u, 2600u),
            (161u, 30u, 800u),
            (162u, 30u, 1500u),
            (163u, 30u, 2200u),
            (164u, 30u, 2900u),
            (165u, 30u, 1100u),
            (166u, 30u, 1800u),
            (167u, 30u, 2500u)
        };
        // ── END GENERATED DATA ──

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var pool in Pools)
                Update(migrationBuilder, pool.Id, pool.New);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var pool in Pools.Reverse())
                Update(migrationBuilder, pool.Id, pool.Old);
        }

        /// <summary>respown_time is INTEGER (units of 100 ms, counted from the pool's last death).</summary>
        private static void Update(MigrationBuilder builder, uint id, uint value)
        {
            builder.UpdateData(table: "spawnpool", keyColumn: "id", keyValue: id, column: "respown_time", value: value);
            var update = (UpdateDataOperation)builder.Operations.Last();
            update.KeyColumnTypes = new[] { "INTEGER" };
            update.ColumnTypes = new[] { "INTEGER" };
        }
    }
}
