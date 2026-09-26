using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// Crater Lake Research Facility (context 1721, a per-squad instance since MissionContextSquadInstancing): the
    /// parts of the 2026-09-26 instance dossier (research/20260926-instance-dossiers-wilderness, Crater Lake section)
    /// that stand on navmesh floor joined to the instance's hospital marker and exit marker by a complete path.
    ///
    /// <b>What is seeded.</b>
    ///   * <b>Captain Velns (199061)</b>, already placed by TarapediaMissingNpcBatch, gets an <c>npc_package</c> row for
    ///     client package 23: objectiveconversation (450, 4, 23) is The Dead Live's only completion line, missiontext 5982
    ///     "Casper? Let me look him up in the computer." Package original; that Velns speaks it is inferred (450/4 is
    ///     "Speak to Velns.", and 960's finishing text names "Velns").
    ///   * <b>450 The Dead Live</b>: Dr. Franja Corman (world-seed 107, Ranja Gorge) gives it and Velns receives it (the
    ///     client log "speak with Captain Velns"; TTH 12625). One client objective, 4, required and revealed on
    ///     acceptance (inferred). 900 credits (TaRapedia rev 14229, 2007-11-10; Ellatha agrees), labelled pre-1.4; no
    ///     experience row, because no source records one (GAP-CLRF-450-XP). TTH names a starter, "Hoping for the Best",
    ///     that is not a mission of the 1.16.5.0 client, so no prerequisite is seeded (OD-144).
    ///   * <b>Hominis Machina Lt. Casper</b>: the emulator-lineage spawnpool 520012 stops drawing (counts 0/0; Down puts
    ///     1/1 back) and content placement 1721100 stands his existing creature row at the same Ellatha /loc
    ///     (-272, 168, -54), y the navmesh floor. A placement is objective-bound content: it is materialized once per squad
    ///     copy and is not respawned inside a copy (OD-142, GAP-INSTANCE-RESET-TIMER).
    ///   * <b>Overseer Tyryd</b>: creature 1721001 (client name 7002; class 10502 Bane_Thrax_Technician_Boss for Ellatha's
    ///     "Thrax Technician"; level 9, Ellatha) at TaRapedia's supply-pen-key /loc (-160, 90, -10), placement 1721101,
    ///     guarding his spot. Health, attack and movement are analogues (OD-141).
    ///   * <b>960 Logos: Movement, Around, Chaos</b>: Receptive Liaison Standley (134) gives and takes it back, as the
    ///     Liaison Logos missions do; objectives 4/5/6 bound by LogosRecovered to the world's logos rows 50 Movement,
    ///     45 Around and 4 Chaos, all on 1721 (the client's logosstone constants). 18,000 experience and 1,500 credits
    ///     (TaRapedia rev 14536, 2007-11-12; Ellatha 1,500), pre-1.4. Level: Standley's 10 (OD-140). Around and Chaos
    ///     stood in the supply pen behind a force field 1056/4 opened; neither 1056 nor the field is seeded, so they are
    ///     ungated here (OD-143, GAP-CLRF-PEN-FORCEFIELD).
    ///
    /// <b>What is held, and why</b> (docs/evidence/crater-lake-research-facility.json):
    ///   * <b>1055 Destroying the Evidence</b> and its four speakers: the Processing Center terminal (TTH 124.3, -27.3) is
    ///     on the destroyed HQ's upper floors and the Biological Lab terminal (102.3, -89.6) inside the greenhouse; both
    ///     are navmesh islands no path from the entrance reaches (GAP-CLRF-INTERIOR-NAVMESH). The Observation Center
    ///     reading (-118.4, -76.0) lands on valley floor at y 71.3 with no building, against TTH's own "trail leading up"
    ///     (GAP-CLRF-DT3-POSITION). Seeding 1055 with speakers missing would make a mission that cannot be finished, so
    ///     the reachable Bane CommLink terminal is held with it.
    ///   * <b>1065 Bending the Rules</b> and its exit area: TaRapedia's prerequisite is the unseeded 1056, which the
    ///     content loader rejects, and dropping the prerequisite would invent the ungated escort (GAP-CLRF-1056-CHAIN).
    ///   * <b>The radar dish</b> (class 6273, a TwoStateSwitch): content usables implement no two-state machine, and its
    ///     only use is 1054/2, which is held with 1054 (GAP-CLRF-RADAR-DISH-TWOSTATE).
    ///   * 489, 1054, the ambient population, the three unnamed D11 bosses and the D11 treasure crates, as the dossier.
    /// Daniel Corman (199062) is already placed by TarapediaMissingNpcBatch on reachable floor and needs nothing here.
    /// </summary>
    public static class WildernessCraterLakeResearchFacilityRows
    {
        public const string Migration = "WildernessCraterLakeResearchFacility";

        public const uint CraterLake = 1721u;

        public const uint TheDeadLive = 450u;
        public const uint LogosMovementAroundChaos = 960u;

        public const uint FranjaCorman = 107u;
        public const uint Standley = 134u;
        public const uint Velns = 199061u;
        public const uint VelnsPackage = 23u;

        /// <summary>The emulator-lineage spawnpool (and creature row) of Hominis Machina Lt. Casper.</summary>
        public const uint Casper = 520012u;
        public const uint CasperPlacement = 1721100u;

        public const uint Tyryd = 1721001u;
        public const uint TyrydPlacement = 1721101u;

        private const uint GeneralCategory = 10000001u;
        private const byte Credits = 1;
        private const byte Experience = 3;
        private const byte CreaturePlacement = 1;
        private const byte CreatureAi = 2;
        private const byte LogosRecoveredBinding = 8;
        private const byte NoCounter = 255;

        private static readonly string[] CreatureColumns =
            { "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed", "walk_speed",
              "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8" };
        private static readonly string[] CreatureTypes =
            { "INTEGER", "varchar(50)", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "REAL", "REAL",
              "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" };
        private static readonly string[] PlacementColumns =
            { "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id", "usable_kind", "pos_x", "pos_y",
              "pos_z", "rotation", "behavior", "initial_state", "alternate_state", "alternate_state_condition_id", "windup_ms",
              "name_override_id", "hit_points", "restore_ms", "fuse_ms", "loot_item_set_id", "respawn_ms", "present_condition_id",
              "usable_condition_id", "comment", "escort_mission_id" };
        private static readonly string[] PlacementTypes =
            { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "REAL", "REAL",
              "REAL", "REAL", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER",
              "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER",
              "INTEGER", "varchar(50)", "INTEGER" };
        private static readonly string[] MissionColumns =
            { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" };
        private static readonly string[] MissionTypes =
            { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "varchar(50)" };
        private static readonly string[] ObjectiveColumns =
            { "mission_id", "objective_id", "comment", "is_required", "ordinal", "revealed_on_accept" };
        private static readonly string[] ObjectiveTypes = { "INTEGER", "INTEGER", "varchar(100)", "INTEGER", "INTEGER", "INTEGER" };
        private static readonly string[] BindingColumns =
        {
            "mission_id", "objective_id", "binding_id", "kind", "area_id", "placement_id", "creature_id", "action_id",
            "destroying_hit_only", "equip_match", "item_template_id", "drop_chance", "item_set_id", "target_state", "counter_id", "comment"
        };
        private static readonly string[] BindingTypes =
        {
            "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER",
            "INTEGER", "INTEGER", "INTEGER", "double", "INTEGER", "INTEGER", "INTEGER", "varchar(50)"
        };
        private static readonly string[] RewardColumns = { "id", "type", "credits", "item_template_id", "quantity" };
        private static readonly string[] RewardKeyColumns = { "id", "type", "item_template_id" };

        /// <summary>
        /// Overseer Tyryd: name 7002 and class 10502 (client), faction 0 and level 9 (Ellatha); 1000 hp, action 29 and
        /// run 9 / walk 0 are analogues (OD-141).
        /// </summary>
        public static readonly object[] TyrydCreature =
            { Tyryd, "Overseer Tyryd (Crater Lake Research Facility)", 10502u, 0u, 9u, 1000u, 7002u, 9.0, 0.0,
              29u, 0u, 0u, 0u, 0u, 0u, 0u, 0u };

        /// <summary>(id, creature, x, y, z, comment). Both guard their spot (creature AI), face +Z, and do not respawn in a copy.</summary>
        public static readonly (uint Id, uint Creature, double X, double Y, double Z, string Comment)[] Placements =
        {
            (CasperPlacement, Casper, -272.0, 168.763, -54.0, "Hominis Machina Lt. Casper (per squad copy)"),
            (TyrydPlacement, Tyryd, -160.0, 93.18, -10.0, "Overseer Tyryd (supply pen key /loc)")
        };

        /// <summary>(mission, giver, receiver, level (analogue, OD-140), comment).</summary>
        public static readonly (uint Id, uint Giver, uint Receiver, uint Level, string Comment)[] Missions =
        {
            (TheDeadLive, FranjaCorman, Velns, 10u, "The Dead Live (Crater Lake)"),
            (LogosMovementAroundChaos, Standley, Standley, 10u, "Logos: Movement, Around, Chaos (Crater Lake)")
        };

        /// <summary>(mission, client objective, ordinal, the client's objective text). Every one required and revealed on acceptance.</summary>
        public static readonly (uint Mission, uint Objective, uint Ordinal, string Text)[] Objectives =
        {
            (TheDeadLive, 4u, 1u, "Speak to Velns."),
            (LogosMovementAroundChaos, 4u, 1u, "Retrieve Logos Information: Movement"),
            (LogosMovementAroundChaos, 5u, 2u, "Retrieve Logos Information: Around"),
            (LogosMovementAroundChaos, 6u, 3u, "Retrieve Logos Information: Chaos")
        };

        /// <summary>(mission, objective, the `logos` row of the shrine, its word). The rows are the client's logosstone constants on map 1721.</summary>
        public static readonly (uint Mission, uint Objective, uint Logos, string Word)[] Shrines =
        {
            (LogosMovementAroundChaos, 4u, 50u, "Movement"),
            (LogosMovementAroundChaos, 5u, 45u, "Around"),
            (LogosMovementAroundChaos, 6u, 4u, "Chaos")
        };

        /// <summary>TaRapedia's pre-1.4 amounts that no source contradicts. 450 has no experience row on purpose.</summary>
        public static readonly (uint Mission, byte Type, int Amount)[] Rewards =
        {
            (TheDeadLive, Credits, 900),
            (LogosMovementAroundChaos, Experience, 18000),
            (LogosMovementAroundChaos, Credits, 1500)
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            // Casper's shared spawnpool stops drawing; his placement below is materialized in every squad copy instead.
            SpawnpoolCounts(migrationBuilder, 0);

            InsertTyped(migrationBuilder, "creature", CreatureColumns, CreatureTypes, Rows(new[] { TyrydCreature }));

            InsertTyped(migrationBuilder, "content_placement", PlacementColumns, PlacementTypes,
                Rows(Placements.Select(p => new object[]
                {
                    p.Id, CraterLake, CreaturePlacement, p.Creature, 0u, 0u, (byte)0, p.X, p.Y,
                    p.Z, 0.0, CreatureAi, 0u, 0u, 0u, 0u,
                    0u, 0u, 0u, 0u, 0u, 0u, 0u,
                    0u, p.Comment, 0u
                })));

            InsertTyped(migrationBuilder, "npc_package", new[] { "id", "package_id", "comment" }, new[] { "INTEGER", "INTEGER", "varchar(50)" },
                Rows(new[] { new object[] { Velns, VelnsPackage, "Captain Velns (client package 23)" } }));

            // group_type 1, general category, not shareable, not radio-completable: the reconstructed-mission values.
            InsertTyped(migrationBuilder, "npc_mission", MissionColumns, MissionTypes,
                Rows(Missions.Select(m => new object[] { m.Id, m.Giver, m.Receiver, m.Level, (byte)1, GeneralCategory, false, false, m.Comment })));

            // The client skeleton rows are replaced by the same rows with the three flags set.
            DeleteTyped(migrationBuilder, "npc_mission_objective", new[] { "mission_id", "objective_id" }, new[] { "INTEGER", "INTEGER" },
                Rows(Objectives.Select(o => new object[] { o.Mission, o.Objective })));
            InsertTyped(migrationBuilder, "npc_mission_objective", ObjectiveColumns, ObjectiveTypes,
                Rows(Objectives.Select(o => new object[] { o.Mission, o.Objective, o.Text, true, o.Ordinal, true })));

            // 450/4 completes through the client's own objectiveconversation row (450, 4, 23); 960's three through the shrines.
            InsertTyped(migrationBuilder, "npc_mission_objective_binding", BindingColumns, BindingTypes,
                Rows(Shrines.Select(s => new object[]
                {
                    s.Mission, s.Objective, (byte)0, LogosRecoveredBinding, 0u, s.Logos, 0u, 0u, false, (byte)0, 0u, 0d, 0u, 0u,
                    NoCounter, $"{s.Mission}/{s.Objective} the {s.Word} shrine"
                })));

            InsertTyped(migrationBuilder, "npc_mission_reward", RewardColumns, new[] { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" },
                Rows(Rewards.Select(r => new object[] { r.Mission, r.Type, r.Amount, 0u, 0u })));
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            DeleteTyped(migrationBuilder, "npc_mission_reward", RewardKeyColumns, new[] { "INTEGER", "INTEGER", "INTEGER" },
                Rows(Rewards.Select(r => new object[] { r.Mission, r.Type, 0u })));

            DeleteTyped(migrationBuilder, "npc_mission_objective_binding", new[] { "mission_id", "objective_id", "binding_id" }, new[] { "INTEGER", "INTEGER", "INTEGER" },
                Rows(Shrines.Select(s => new object[] { s.Mission, s.Objective, (byte)0 })));

            // The client skeleton rows come back with their text and all three server flags unknown (NULL).
            DeleteTyped(migrationBuilder, "npc_mission_objective", new[] { "mission_id", "objective_id" }, new[] { "INTEGER", "INTEGER" },
                Rows(Objectives.Select(o => new object[] { o.Mission, o.Objective })));
            InsertTyped(migrationBuilder, "npc_mission_objective", ObjectiveColumns, ObjectiveTypes,
                Rows(Objectives.Select(o => new object[] { o.Mission, o.Objective, o.Text, null, null, null })));

            DeleteTyped(migrationBuilder, "npc_mission", new[] { "id" }, new[] { "INTEGER" },
                Rows(Missions.Select(m => new object[] { m.Id })));

            DeleteTyped(migrationBuilder, "npc_package", new[] { "id" }, new[] { "INTEGER" }, Rows(new[] { new object[] { Velns } }));

            DeleteTyped(migrationBuilder, "content_placement", new[] { "id" }, new[] { "INTEGER" },
                Rows(Placements.Select(p => new object[] { p.Id })));

            DeleteTyped(migrationBuilder, "creature", new[] { "id" }, new[] { "INTEGER" }, Rows(new[] { new object[] { Tyryd } }));

            SpawnpoolCounts(migrationBuilder, 1);
        }

        /// <summary>Spawnpool 520012's first slot: 0/0 retires it, 1/1 is BossSpawnpoolPreloader's value.</summary>
        private static void SpawnpoolCounts(MigrationBuilder builder, byte count)
        {
            builder.UpdateData("spawnpool", new[] { "id" }, new object[] { Casper },
                new[] { "creature_1_min_count", "creature_1_max_count" }, new object[] { count, count });
            var update = (UpdateDataOperation)builder.Operations.Last();
            update.KeyColumnTypes = new[] { "INTEGER" };
            update.ColumnTypes = new[] { "INTEGER", "INTEGER" };
        }

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
