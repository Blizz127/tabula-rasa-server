using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// The ambient creatures of Torden Plains (1764) and Torden Mires (1759), six more Palisades (1244) groups, the Palisades
    /// analogues the same footage replaces, and 1067 Can't Survive Without My Radio, which the Mires comm officers make
    /// finishable (owner decisions OD-161/OD-176, 2026-09-27: footage first, then text, then labelled stand-ins).
    ///
    /// <b>Why.</b> Before this the Plains and the Mires held only service NPCs, named NPCs and upstream bosses, so nothing
    /// could be hunted there; the Palisades pools of ConcordiaAmbientPopulations carried analogue levels, names and one analogue
    /// place because no post-D12 footage was cached.
    ///
    /// <b>Evidence</b> (every field's tier and citation: docs/evidence/bootcamp-d11-reconstruction-manifest.json, migration
    /// FootageAmbientPopulations; registrations, probes and sources: docs/evidence/footage-ambient-populations-20260927.json).
    ///   * Footage. The Raisuly series (French client, 1440x1080, recorded 2008-11-22 to 2009-02-27 per title, after Palisades
    ///     D12 and the Plains D13.4 rebuild; Mires has no recorded rebuild): #10 Nsz75UOlZz0, #12 IDAug5iUa9c, #16 rCt23ux-kyU,
    ///     #17 C2rGwo6fLw0. Species and level are read by eye from each target frame (observed, footage events RS10/RS12/RS16/RS17);
    ///     names are the client's creaturenamelanguage ids of the French names (original).
    ///   * Places. The in-game radar, template-matched to the zone's radar mosaic whose origin is fitted to the 1.16.5.0 map
    ///     statics, gives the player's position (+/-15 m); the group fought is within radar range, so each pool stands at the
    ///     centroid of the registrations of its fight, +/-50 m (measured). Heights are the navmesh floor minus 0.276 m, and every
    ///     pool is reached from its zone's main waypoint (Hightower, Irendas Penal Colony, Baylor Base).
    ///   * Counts are the fewest the footage shows (OD-177); health, attacks, movement and respawn are labelled world-seed
    ///     analogues (OD-178).
    ///
    /// <b>Replaced.</b> 1244001 Boargar 15 -> 18 and name 7805 (Mature Forest Boargar); 1244002 Warnet 15 -> 16 and 460 -> 7957
    /// (Irate Warnet Soldier); 1244004 Hunter 15 -> 16 and 8249 (Hunter Corporal); 1244005 Technician 15 -> 17 and 9106 -> 8057
    /// (Thrax Technician PFC); pool 1244201 from the analogue (60, 50) east of Hightower to the filmed Forean Ruins group
    /// (419.2, 368.8), one Warnet (OD-179).
    ///
    /// <b>Seeded.</b> Creatures 1244006-1244014, 1764001-1764016 and 1759001-1759006; pools 1244208-1244213, 1764200-1764210 and
    /// 1759200-1759207. Mission 1067 (Major Ston, Fort Haroun): the Lightbender and Caretaker Communicators (templates 45012,
    /// 45013) drop from the two comm officers (OD-61's 50%); 66,000 XP and 5,800 credits, TaRapedia's post-1.4 reading; level 30
    /// (OD-181); offered without 975 Flight Salvage, which is unseeded (OD-180).
    ///
    /// <b>Held.</b> Species with no registered position (Plains Corporals, Medico, Crab Mine, Light Ordnance, Predator, the
    /// Shield Drone; the Mires Iapyx Warrant Officers; Abyss, Plateau, Howling Maw), Overseer Qraal, 952, 1587, the Targets of
    /// Opportunity, 1067's item reward.
    /// </summary>
    public static class FootageAmbientPopulationsRows
    {
        public const string Migration = "FootageAmbientPopulations";

        public const uint Palisades = 1244u, Plains = 1764u, Mires = 1759u;

        public const uint MajorSton = 199408u;
        public const uint LightbenderCommOfficer = 1759006u, CaretakerCommOfficer = 1759005u;
        public const uint LightbenderCommunicator = 45012u, CaretakerCommunicator = 45013u;
        public const uint CantSurviveWithoutMyRadio = 1067u;

        /// <summary>OD-178: the world seed's respawn (100 ms units after a pool's last creature dies), as OD-164.</summary>
        public const uint PoolRespawn = 20u;

        private const uint GeneralCategory = 10000001u;
        private const byte Credits = 1, Experience = 3;
        private const byte ItemCollected = 9;

        // ── GENERATED DATA (tools/build.py) ──
        /// <summary>(id, comment, class, level, hp, name id, action1); faction 0, run 9, walk 5.</summary>
        public static readonly (uint Id, string Comment, uint Class, uint Level, uint Hp, uint NameId, uint Action1)[] Creatures =
        {
            (1244006u, "Selenous Treemite (Cumbria Weald)", 6040u, 16u, 555u, 7861u, 4u),
            (1244007u, "Malonic Treemite (Cumbria Weald)", 6040u, 15u, 555u, 7860u, 4u),
            (1244008u, "Thrax Infantry PFC L17 (Palisades)", 29769u, 17u, 555u, 7677u, 33u),
            (1244009u, "Thrax Infantry PFC L16 (Palisades)", 29769u, 16u, 555u, 7677u, 33u),
            (1244010u, "Thrax Infantry PFC L15 (Palisades)", 29769u, 15u, 555u, 7677u, 33u),
            (1244011u, "Thrax Infantry Private (Palisades)", 29769u, 15u, 555u, 7676u, 33u),
            (1244012u, "Caretaker Butcher (Palisades)", 9244u, 17u, 1000u, 8219u, 16u),
            (1244013u, "Caretaker Healer (Palisades)", 9244u, 15u, 1000u, 362u, 16u),
            (1244014u, "Alpha Beast Howler (Palisades)", 7336u, 16u, 555u, 8153u, 4u),
            (1764001u, "Flaregasher L25 (Plains)", 7338u, 25u, 555u, 9372u, 4u),
            (1764002u, "Flaregasher L20 (Plains)", 7338u, 20u, 555u, 9372u, 4u),
            (1764003u, "Strider L20 (Plains)", 9804u, 20u, 600u, 0u, 2u),
            (1764004u, "Strider L23 (Plains)", 9804u, 23u, 600u, 0u, 2u),
            (1764005u, "Hunter Sergeant (Plains)", 10166u, 22u, 400u, 8250u, 9u),
            (1764006u, "Thrax Infantry Specialist (Plains)", 29769u, 22u, 555u, 7678u, 33u),
            (1764007u, "Fiend Pup Howler (Plains)", 7336u, 22u, 555u, 8154u, 4u),
            (1764008u, "Beam Manta Wafter L21 (Plains)", 6441u, 21u, 555u, 8018u, 23u),
            (1764009u, "Beam Manta Wafter L22 (Plains)", 6441u, 22u, 555u, 8018u, 23u),
            (1764010u, "Atta Harvester Gatherer (Plains)", 7078u, 25u, 555u, 8071u, 1u),
            (1764011u, "Atta Harvester Feeder L23 (Plains)", 7078u, 23u, 555u, 8070u, 1u),
            (1764012u, "Atta Harvester Feeder L21 (Plains)", 7078u, 21u, 555u, 8070u, 1u),
            (1764013u, "Atta Soldier Conscript (Plains)", 7081u, 24u, 555u, 8095u, 1u),
            (1764014u, "Thrax Technician Specialist (Plains)", 7043u, 21u, 555u, 8058u, 9u),
            (1764015u, "Atta Harvester L20 (Plains)", 7078u, 20u, 555u, 0u, 1u),
            (1764016u, "Atta Harvester Feeder L24 (Plains)", 7078u, 24u, 555u, 8070u, 1u),
            (1759001u, "Thrax Technician Sergeant (Mires)", 7043u, 29u, 555u, 8060u, 9u),
            (1759002u, "Thrax Scavenger L30 (Mires)", 20768u, 30u, 555u, 8746u, 1u),
            (1759003u, "Thrax Scavenger L28 (Mires)", 20768u, 28u, 555u, 8746u, 1u),
            (1759004u, "Kael Master Sergeant (Mires)", 4046u, 29u, 555u, 7728u, 2u),
            (1759005u, "Caretaker Comm Officer (Mires)", 9244u, 29u, 1000u, 8851u, 16u),
            (1759006u, "Lightbender Comm Officer (Mires)", 7120u, 30u, 555u, 8852u, 30u)
        };

        /// <summary>(id, map, x, y, z, up to six (creature, count)); anim 0, respawn 20, min = max.</summary>
        public static readonly (uint Id, uint Map, double X, double Y, double Z, (uint Creature, byte Count)[] Slots)[] Pools =
        {
            // Treemites, Cumbria Weald
            (1244208u, 1244u, -258.3, 173.927, 857.2, new[] { (1244006u, (byte)2), (1244007u, (byte)2) }),
            // Mature Forest Boargar, Cumbria Weald
            (1244209u, 1244u, -130.7, 124.323, 470.2, new[] { (1244001u, (byte)1) }),
            // Bane squad, Cumbria Weald (-174, 525)
            (1244210u, 1244u, -174.0, 136.07, 525.2, new[] { (1244008u, (byte)1), (1244005u, (byte)1), (1244012u, (byte)1) }),
            // Thrax with Executor Gantic
            (1244211u, 1244u, -64.2, 118.727, 597.0, new[] { (1244008u, (byte)1) }),
            // Hunter, Thrax and Howler, Cumbria Weald
            (1244212u, 1244u, -58.3, 141.964, 720.4, new[] { (1244004u, (byte)1), (1244009u, (byte)1), (1244014u, (byte)1) }),
            // Thrax and Healer, Cumbria Weald (-224, 769)
            (1244213u, 1244u, -224.4, 142.807, 768.7, new[] { (1244011u, (byte)1), (1244013u, (byte)1), (1244009u, (byte)1), (1244010u, (byte)1) }),
            // Flaregashers, Geyser Chimney Basin
            (1764200u, 1764u, -123.9, 435.165, 120.9, new[] { (1764002u, (byte)1), (1764001u, (byte)1) }),
            // Strider, Geyser Chimney Basin
            (1764201u, 1764u, -71.8, 435.478, 216.8, new[] { (1764003u, (byte)1) }),
            // Bane group, Geyser Chimney Basin
            (1764202u, 1764u, -67.9, 438.129, 232.3, new[] { (1764005u, (byte)1), (1764006u, (byte)1), (1764007u, (byte)1) }),
            // Beam Manta, Lightning Fields
            (1764203u, 1764u, -244.5, 430.81, 557.8, new[] { (1764008u, (byte)1) }),
            // Flaregasher, Lightning Fields
            (1764204u, 1764u, -158.7, 420.324, 610.8, new[] { (1764002u, (byte)1) }),
            // Beam Manta, east of Lightning Fields
            (1764205u, 1764u, -88.0, 430.907, 575.0, new[] { (1764009u, (byte)1) }),
            // Atta, Atta Territory
            (1764206u, 1764u, -538.1, 446.54, 114.3, new[] { (1764010u, (byte)1), (1764011u, (byte)1), (1764012u, (byte)1), (1764013u, (byte)1) }),
            // Thrax Technicians, west Plains ridge
            (1764207u, 1764u, -691.3, 471.854, 136.0, new[] { (1764014u, (byte)2) }),
            // Strider, Firefall Point
            (1764208u, 1764u, -852.7, 448.198, -110.3, new[] { (1764004u, (byte)1) }),
            // Atta Harvester, south Atta Territory
            (1764209u, 1764u, -669.5, 429.724, -235.5, new[] { (1764015u, (byte)1) }),
            // Atta Harvester Feeder, south Atta Territory
            (1764210u, 1764u, -672.0, 427.347, -307.5, new[] { (1764016u, (byte)1) }),
            // Thrax pair, east Mires (810, 222)
            (1759200u, 1759u, 810.3, 261.407, 221.8, new[] { (1759001u, (byte)1), (1759002u, (byte)1) }),
            // Kael, east Mires (842, 89)
            (1759201u, 1759u, 842.1, 233.484, 88.5, new[] { (1759004u, (byte)1) }),
            // Kael, east Mires (817, -19)
            (1759202u, 1759u, 816.9, 234.774, -18.8, new[] { (1759004u, (byte)1) }),
            // Kael and Scavenger (701, -60)
            (1759203u, 1759u, 700.7, 235.084, -60.0, new[] { (1759004u, (byte)1), (1759003u, (byte)1) }),
            // Thrax Scavengers (671, -61)
            (1759204u, 1759u, 671.3, 235.084, -60.8, new[] { (1759003u, (byte)2) }),
            // Comm Officer squad (598, -53)
            (1759205u, 1759u, 597.8, 235.848, -53.2, new[] { (1759005u, (byte)1), (1759002u, (byte)1), (1759004u, (byte)1) }),
            // Caretaker Comm Officer (532, -35)
            (1759206u, 1759u, 532.1, 240.854, -35.1, new[] { (1759005u, (byte)1) }),
            // Lightbender Comm Officer (478, -73)
            (1759207u, 1759u, 478.3, 234.013, -73.1, new[] { (1759006u, (byte)1) })
        };

        /// <summary>The previous batch's analogues the footage replaces: (table, id, column, old, new).</summary>
        public static readonly (string Table, uint Id, string Column, double Old, double New)[] Updates =
        {
            ("creature", 1244001u, "level", 15.0, 18.0),
            ("creature", 1244001u, "name_id", 0.0, 7805.0),
            ("creature", 1244002u, "level", 15.0, 16.0),
            ("creature", 1244002u, "name_id", 460.0, 7957.0),
            ("creature", 1244004u, "level", 15.0, 16.0),
            ("creature", 1244004u, "name_id", 0.0, 8249.0),
            ("creature", 1244005u, "level", 15.0, 17.0),
            ("creature", 1244005u, "name_id", 9106.0, 8057.0),
            ("spawnpool", 1244201u, "pos_x", 60.0, 419.2),
            ("spawnpool", 1244201u, "pos_y", 113.235, 109.767),
            ("spawnpool", 1244201u, "pos_z", 50.0, 368.8),
            ("spawnpool", 1244201u, "creature_1_min_count", 3.0, 1.0),
            ("spawnpool", 1244201u, "creature_1_max_count", 3.0, 1.0)
        };
        // ── END GENERATED DATA ──

        /// <summary>1067: (objective, ordinal, the client's objective text); both required and revealed on acceptance.</summary>
        public static readonly (uint Objective, uint Ordinal, string Text)[] Objectives =
        {
            (1u, 1u, "Lightbender Communicator"),
            (2u, 2u, "Caretaker Comm")
        };

        /// <summary>1067: (objective, creature, item template, drop chance %, comment).</summary>
        public static readonly (uint Objective, uint Creature, uint Item, double Drop, string Comment)[] Bindings =
        {
            (1u, LightbenderCommOfficer, LightbenderCommunicator, 50.0, "1067/1 Lightbender Communicator"),
            (2u, CaretakerCommOfficer, CaretakerCommunicator, 50.0, "1067/2 Caretaker Communicator")
        };

        /// <summary>1067: TaRapedia's amounts, first recorded 2008-09-16 (post-1.4); no item (only "Modifications").</summary>
        public static readonly (byte Type, int Amount)[] Rewards =
        {
            (Experience, 66000), (Credits, 5800)
        };

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
        private static readonly string[] MissionColumns =
            { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" };
        private static readonly string[] MissionTypes = Integers(8).Concat(new[] { "varchar(50)" }).ToArray();
        private static readonly string[] ObjectiveColumns = { "mission_id", "objective_id", "comment", "is_required", "ordinal", "revealed_on_accept" };
        private static readonly string[] ObjectiveTypes = { "INTEGER", "INTEGER", "varchar(100)", "INTEGER", "INTEGER", "INTEGER" };
        private static readonly string[] BindingColumns =
        {
            "mission_id", "objective_id", "binding_id", "kind", "area_id", "placement_id", "creature_id", "action_id",
            "destroying_hit_only", "equip_match", "item_template_id", "drop_chance", "item_set_id", "target_state", "counter_id", "comment"
        };
        private static readonly string[] BindingTypes = Integers(11).Concat(new[] { "double" }).Concat(Integers(3)).Concat(new[] { "varchar(50)" }).ToArray();
        private static readonly string[] CounterColumns = { "mission_id", "objective_id", "counter_id", "initial_value", "target_value" };
        private static readonly string[] RewardColumns = { "id", "type", "credits", "item_template_id", "quantity" };
        private static readonly string[] RewardKeyColumns = { "id", "type", "item_template_id" };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            // ── the populations ──
            InsertTyped(migrationBuilder, "creature", CreatureColumns, CreatureTypes,
                Rows(Creatures.Select(c => new object[] { c.Id, c.Comment, c.Class, 0u, c.Level, c.Hp, c.NameId, 9.0, 5.0,
                    c.Action1, 0u, 0u, 0u, 0u, 0u, 0u, 0u })));
            InsertTyped(migrationBuilder, "spawnpool", PoolColumns, PoolTypes, Rows(Pools.Select(PoolRow)));

            // ── the previous batch's analogues the footage replaces ──
            foreach (var update in Updates)
                UpdateTyped(migrationBuilder, update.Table, update.Id, update.Column, update.New);

            // ── 1067 ──
            InsertTyped(migrationBuilder, "npc_mission", MissionColumns, MissionTypes, Rows(new[]
            {
                new object[] { CantSurviveWithoutMyRadio, MajorSton, MajorSton, 30u, (byte)1, GeneralCategory, false, false, "Can't Survive Without My Radio (Mires)" }
            }));
            // The client skeleton rows are replaced by the same rows with the three flags set.
            DeleteTyped(migrationBuilder, "npc_mission_objective", new[] { "mission_id", "objective_id" }, Integers(2),
                Rows(Objectives.Select(o => new object[] { CantSurviveWithoutMyRadio, o.Objective })));
            InsertTyped(migrationBuilder, "npc_mission_objective", ObjectiveColumns, ObjectiveTypes,
                Rows(Objectives.Select(o => new object[] { CantSurviveWithoutMyRadio, o.Objective, o.Text, true, o.Ordinal, true })));
            InsertTyped(migrationBuilder, "npc_mission_objective_binding", BindingColumns, BindingTypes,
                Rows(Bindings.Select(b => new object[]
                {
                    CantSurviveWithoutMyRadio, b.Objective, (byte)0, ItemCollected, 0u, 0u, b.Creature, 0u, false, (byte)0, b.Item, b.Drop, 0u, 0u, (byte)0, b.Comment
                })));
            InsertTyped(migrationBuilder, "npc_mission_objective_counter", CounterColumns, Integers(5),
                Rows(Bindings.Select(b => new object[] { CantSurviveWithoutMyRadio, b.Objective, 0u, 0, 1 })));
            InsertTyped(migrationBuilder, "npc_mission_reward", RewardColumns, Integers(5),
                Rows(Rewards.Select(r => new object[] { CantSurviveWithoutMyRadio, r.Type, r.Amount, 0u, 0u })));
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            DeleteTyped(migrationBuilder, "npc_mission_reward", RewardKeyColumns, Integers(3),
                Rows(Rewards.Select(r => new object[] { CantSurviveWithoutMyRadio, r.Type, 0u })));
            DeleteTyped(migrationBuilder, "npc_mission_objective_counter", new[] { "mission_id", "objective_id", "counter_id" }, Integers(3),
                Rows(Bindings.Select(b => new object[] { CantSurviveWithoutMyRadio, b.Objective, 0u })));
            DeleteTyped(migrationBuilder, "npc_mission_objective_binding", new[] { "mission_id", "objective_id", "binding_id" }, Integers(3),
                Rows(Bindings.Select(b => new object[] { CantSurviveWithoutMyRadio, b.Objective, (byte)0 })));
            // The client skeleton rows come back with their text and all three server flags unknown (NULL).
            DeleteTyped(migrationBuilder, "npc_mission_objective", new[] { "mission_id", "objective_id" }, Integers(2),
                Rows(Objectives.Select(o => new object[] { CantSurviveWithoutMyRadio, o.Objective })));
            InsertTyped(migrationBuilder, "npc_mission_objective", ObjectiveColumns, ObjectiveTypes,
                Rows(Objectives.Select(o => new object[] { CantSurviveWithoutMyRadio, o.Objective, o.Text, null, null, null })));
            DeleteTyped(migrationBuilder, "npc_mission", new[] { "id" }, Integers(1), Rows(new[] { new object[] { CantSurviveWithoutMyRadio } }));

            foreach (var update in Updates.Reverse())
                UpdateTyped(migrationBuilder, update.Table, update.Id, update.Column, update.Old);

            DeleteTyped(migrationBuilder, "spawnpool", new[] { "id" }, Integers(1), Rows(Pools.Select(p => new object[] { p.Id })));
            DeleteTyped(migrationBuilder, "creature", new[] { "id" }, Integers(1), Rows(Creatures.Select(c => new object[] { c.Id })));
        }

        private static object[] PoolRow((uint Id, uint Map, double X, double Y, double Z, (uint Creature, byte Count)[] Slots) pool)
        {
            var row = new List<object> { pool.Id, (byte)0, (byte)0, PoolRespawn, pool.X, pool.Y, pool.Z, 0.0, pool.Map };
            for (var i = 0; i < 6; i++)
            {
                if (i < pool.Slots.Length)
                    row.AddRange(new object[] { pool.Slots[i].Creature, pool.Slots[i].Count, pool.Slots[i].Count });
                else
                    row.AddRange(new object[] { 0u, (byte)0, (byte)0 });
            }
            return row.ToArray();
        }

        /// <summary>Position columns are REAL; creature levels, name ids and slot counts are INTEGER.</summary>
        private static void UpdateTyped(MigrationBuilder builder, string table, uint id, string column, double value)
        {
            var real = column.StartsWith("pos_");
            builder.UpdateData(table: table, keyColumn: "id", keyValue: id, column: column, value: real ? value : (object)(uint)value);
            var update = (UpdateDataOperation)builder.Operations.Last();
            update.KeyColumnTypes = new[] { "INTEGER" };
            update.ColumnTypes = new[] { real ? "REAL" : "INTEGER" };
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
