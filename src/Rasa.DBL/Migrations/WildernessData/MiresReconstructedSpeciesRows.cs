using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// W3 batch 16: the Mires missions whose targets the client names but no surviving data places.
    ///
    /// 955 Lasher Virus (Professor Long), 956 Hooves of the Magmonix (Dr. Robertson) and 976 Restraining Order
    /// (Colonel Li Hua) each ask for a species the client carries as an entity class - Lasher 7477, Magmonix 6338,
    /// Stalker 3781 - that nothing in the world holds. The class, its name and the mission's own numbers are original:
    /// the objective text gives the count (10 claws, 12 hooves, 3 stalkers) and the entity class table names the
    /// species and marks it as a living creature (augmentation 1, not an item, object or NPC).
    ///
    /// What no source records is **where a species lives**. OD-48 covers that: each species is placed as a cluster of
    /// specimens around the mission's own area - the position its own sources give the NPC who asks for the samples -
    /// with one more specimen than the objective needs so the mission can be completed, and a respawn so the cluster
    /// recovers. Those positions are analogues and the manifest says so per row; the health of a reconstructed
    /// creature is an analogue too, taken from the world's comparable Bane creatures.
    ///
    /// The givers themselves come from the OD-45 pipeline: name id from the client's creature name table (original),
    /// level and position from TaRapedia's page for each of them (inferred, dated), appearance the class the AFS
    /// NPCs already use (analogue).
    /// </summary>
    public static class MiresReconstructedSpeciesRows
    {
        public const string Migration = "MiresReconstructedSpecies";

        private const uint Mires = 1759u;

        private const uint ProfessorLong = 199800u;
        private const uint DrRobertson = 199801u;
        private const uint ColonelLiHua = 199802u;

        private const uint Lasher = 199810u;
        private const uint Magmonix = 199811u;
        private const uint Stalker = 199812u;

        /// <summary>Robertson's dialogue package, which 955's second objective is completed through.</summary>
        private const uint RobertsonPackage = 1048u;

        private const uint AfsHuman = 3846u;
        private const byte KillBinding = 6;
        private const uint RespawnMs = 60000u;

        /// <summary>The three givers: name id (client), level and position (TaRapedia).</summary>
        private static readonly (uint Id, string Name, uint NameId, uint Level, double X, double Y, double Z, uint Package)[] Npcs =
        {
            (ProfessorLong, "Professor Long", 8661u, 32u, 653.0, 225.0, 373.0, 0u),
            (DrRobertson, "Dr. Robertson", 8669u, 32u, 658.0, 224.0, 374.0, RobertsonPackage),
            (ColonelLiHua, "Colonel Li Hua", 8666u, 28u, 260.0, 229.0, 686.0, 0u)
        };

        /// <summary>objective id -> the specimens it counts, in objective order.</summary>
        private static readonly (uint MissionId, uint ObjectiveId, uint Species, uint Target)[] Counts =
        {
            (955u, 7u, Lasher, 10u),
            (956u, 8u, Magmonix, 12u),
            (976u, 1u, Stalker, 3u)
        };

        private static readonly (uint MissionId, uint Giver, uint Receiver, uint Level, string Name, int Xp, int Credits)[] Missions =
        {
            (955u, ProfessorLong, DrRobertson, 30u, "Lasher Virus", 29000, 4000),
            (956u, DrRobertson, DrRobertson, 30u, "Hooves of the Magmonix", 29000, 4050),
            (976u, ColonelLiHua, ColonelLiHua, 30u, "Restraining Order", 62000, 5600)
        };

        /// <summary>Every specimen placement the clusters below create, so the rollback can name them.</summary>
        private static readonly List<(uint Id, uint Species, double X, double Y, double Z, string Note)> Specimens = BuildSpecimens();

        private static List<(uint Id, uint Species, double X, double Y, double Z, string Note)> BuildSpecimens()
        {
            var specimens = new List<(uint, uint, double, double, double, string)>();
            AddCluster(specimens, Lasher, 199820u, 12, 653.0, 225.0, 373.0, "Professor Long's area");
            AddCluster(specimens, Magmonix, 199840u, 14, 658.0, 224.0, 374.0, "Dr. Robertson's area");
            AddCluster(specimens, Stalker, 199860u, 6, 260.0, 229.0, 686.0, "Colonel Li Hua's area");
            return specimens;
        }

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            InsertNpcs(migrationBuilder);
            InsertSpecies(migrationBuilder);
            InsertMissions(migrationBuilder);
            InsertCounts(migrationBuilder);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "npc_mission_reward",
                keyColumns: new[] { "id", "type", "item_template_id" },
                keyValues: new object[,]
                {
                    { 955u, 3u, 0u }, { 955u, 1u, 0u },
                    { 956u, 3u, 0u }, { 956u, 1u, 0u },
                    { 976u, 3u, 0u }, { 976u, 1u, 0u }
                });

            migrationBuilder.DeleteData(table: "npc_mission_objective_counter",
                keyColumns: new[] { "mission_id", "objective_id", "counter_id" },
                keyValues: new object[,] { { 955u, 7u, 0u }, { 956u, 8u, 0u }, { 976u, 1u, 0u } });

            migrationBuilder.DeleteData(table: "npc_mission_objective_binding",
                keyColumns: new[] { "mission_id", "objective_id", "binding_id" },
                keyValues: new object[,] { { 955u, 7u, 0u }, { 956u, 8u, 0u }, { 976u, 1u, 0u } });

            migrationBuilder.DeleteData(table: "npc_mission_objective_transition",
                keyColumns: new[] { "mission_id", "completed_objective_id", "revealed_objective_id" },
                keyValues: new object[,] { { 955u, 7u, 8u } });

            DeleteObjectives(migrationBuilder);

            migrationBuilder.DeleteData(table: "npc_mission",
                keyColumns: new[] { "id" }, keyValues: new object[,] { { 955u }, { 956u }, { 976u } });

            DeletePlacements(migrationBuilder);
        }

        private static void InsertNpcs(MigrationBuilder migrationBuilder)
        {
            var creatures = new object[Npcs.Length, 17];
            var placements = new object[Npcs.Length, 25];
            for (var i = 0; i < Npcs.Length; i++)
            {
                var npc = Npcs[i];
                creatures[i, 0] = npc.Id;
                creatures[i, 1] = npc.Name;
                creatures[i, 2] = AfsHuman;
                creatures[i, 3] = 1u;                    // AFS
                creatures[i, 4] = npc.Level;
                creatures[i, 5] = 750u;                  // the world's AFS NPCs use 750
                creatures[i, 6] = npc.NameId;
                for (var j = 7; j < 17; j++)
                    creatures[i, j] = 0u;
                creatures[i, 7] = 9u;                    // run speed
                creatures[i, 8] = 5u;                    // walk speed

                placements[i, 0] = npc.Id;
                placements[i, 1] = Mires;
                placements[i, 2] = (byte)1;              // creature
                placements[i, 3] = npc.Id;
                placements[i, 4] = npc.Package;
                placements[i, 5] = 0u;
                placements[i, 6] = (byte)0;
                placements[i, 7] = npc.X;
                placements[i, 8] = npc.Y;
                placements[i, 9] = npc.Z;
                placements[i, 10] = 0.0;
                placements[i, 11] = (byte)1;            // stationary
                for (var j = 12; j < 23; j++)
                    placements[i, j] = 0u;
                placements[i, 22] = 0u;                  // present condition
                placements[i, 23] = 0u;                  // usable condition
                placements[i, 24] = $"{npc.Name} (TaRapedia /loc)";
            }

            migrationBuilder.InsertData(
                table: "creature",
                columns: new[] { "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed",
                    "walk_speed", "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8" },
                values: creatures);

            InsertPlacements(migrationBuilder, placements);
        }

        private static void InsertSpecies(MigrationBuilder migrationBuilder)
        {
            // One creature row per species, then a cluster of specimens around the NPC who asks for them.
            migrationBuilder.InsertData(
                table: "creature",
                columns: new[] { "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed",
                    "walk_speed", "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8" },
                values: new object[,]
                {
                    { Lasher, "Lasher (client class 7477)", 7477u, 0u, 30u, 600u, 0u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { Magmonix, "Magmonix (client class 6338)", 6338u, 0u, 30u, 600u, 0u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { Stalker, "Stalker (client class 3781)", 3781u, 0u, 30u, 600u, 0u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u }
                });

            var specimens = Specimens;
            var values = new object[specimens.Count, 25];
            for (var i = 0; i < specimens.Count; i++)
            {
                var (id, species, x, y, z, note) = specimens[i];
                values[i, 0] = id;
                values[i, 1] = Mires;
                values[i, 2] = (byte)1;
                values[i, 3] = species;
                values[i, 4] = 0u;
                values[i, 5] = 0u;
                values[i, 6] = (byte)0;
                values[i, 7] = x;
                values[i, 8] = y;
                values[i, 9] = z;
                values[i, 10] = 0.0;
                values[i, 11] = (byte)2;                 // creature AI
                for (var j = 12; j < 21; j++)
                    values[i, j] = 0u;
                values[i, 21] = RespawnMs;
                values[i, 22] = 0u;
                values[i, 23] = 0u;
                values[i, 24] = $"{note} (OD-48 analogue)";
            }

            InsertPlacements(migrationBuilder, values);
        }

        /// <summary>A ring of specimens around the area the mission's own sources put the species in (OD-48).</summary>
        private static void AddCluster(List<(uint, uint, double, double, double, string)> specimens, uint species,
            uint firstId, int count, double x, double y, double z, string note)
        {
            for (var i = 0; i < count; i++)
            {
                var angle = 2 * Math.PI * i / count;
                // tight enough to stay on the ground the mission area stands on: the NPC's own position is walkable
                // (the world position audit checks it) and a few metres around it is the same terrace.
                var radius = 5.0 + 3.0 * (i % 4);
                specimens.Add(((uint)(firstId + i), species,
                    Math.Round(x + radius * Math.Cos(angle), 3),
                    y,
                    Math.Round(z + radius * Math.Sin(angle), 3),
                    note));
            }
        }

        private static void InsertPlacements(MigrationBuilder migrationBuilder, object[,] values)
            => migrationBuilder.InsertData(
                table: "content_placement",
                columns: new[]
                {
                    "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id", "usable_kind",
                    "pos_x", "pos_y", "pos_z", "rotation", "behavior", "initial_state", "alternate_state",
                    "alternate_state_condition_id", "windup_ms", "name_override_id", "hit_points", "restore_ms",
                    "fuse_ms", "loot_item_set_id", "respawn_ms", "present_condition_id", "usable_condition_id",
                    "comment"
                },
                values: values);

        private static void InsertMissions(MigrationBuilder migrationBuilder)
        {
            var missions = new object[Missions.Length, 9];
            for (var i = 0; i < Missions.Length; i++)
            {
                var m = Missions[i];
                missions[i, 0] = m.MissionId;
                missions[i, 1] = m.Giver;
                missions[i, 2] = m.Receiver;
                missions[i, 3] = m.Level;
                missions[i, 4] = 1u;
                missions[i, 5] = 10000001u;
                missions[i, 6] = false;
                missions[i, 7] = false;
                missions[i, 8] = $"{m.Name} (Mires)";
            }

            migrationBuilder.InsertData(
                table: "npc_mission",
                columns: new[] { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" },
                values: missions);

            // 955 has two objectives: collect the claws, then report to Robertson.
            SendObjective(migrationBuilder, 955u, 7u, "Acquire 10 Lasher Claws", 1u, true);
            SendObjective(migrationBuilder, 955u, 8u, "Speak With Dr. Robertson", 2u, false);
            SendObjective(migrationBuilder, 956u, 8u, "Acquire 12 Magmonix Hooves", 1u, true);
            SendObjective(migrationBuilder, 976u, 1u, "Kill 3 Stalkers", 1u, true);

            migrationBuilder.InsertData(
                table: "npc_mission_objective_transition",
                columns: new[] { "mission_id", "completed_objective_id", "revealed_objective_id" },
                values: new object[,] { { 955u, 7u, 8u } });

            migrationBuilder.InsertData(
                table: "npc_mission_reward",
                columns: new[] { "id", "type", "credits", "item_template_id", "quantity" },
                values: new object[,]
                {
                    { 955u, 3u, 29000, 0u, 0u }, { 955u, 1u, 4000, 0u, 0u },
                    { 956u, 3u, 29000, 0u, 0u }, { 956u, 1u, 4050, 0u, 0u },
                    { 976u, 3u, 62000, 0u, 0u }, { 976u, 1u, 5600, 0u, 0u }
                });
        }

        private static void InsertCounts(MigrationBuilder migrationBuilder)
        {
            var bindings = new object[Counts.Length, 15];
            var counters = new object[Counts.Length, 5];
            for (var i = 0; i < Counts.Length; i++)
            {
                var (missionId, objectiveId, species, target) = Counts[i];
                bindings[i, 0] = missionId;
                bindings[i, 1] = objectiveId;
                bindings[i, 2] = 0u;
                bindings[i, 3] = KillBinding;
                for (var j = 4; j < 6; j++)
                    bindings[i, j] = 0u;
                bindings[i, 6] = species;
                bindings[i, 7] = 0u;
                bindings[i, 8] = false;
                bindings[i, 9] = false;
                for (var j = 10; j < 13; j++)
                    bindings[i, j] = 0u;
                bindings[i, 13] = (byte)0;
                bindings[i, 14] = $"{missionId}/{objectiveId} kill {species} x{target}";

                counters[i, 0] = missionId;
                counters[i, 1] = objectiveId;
                counters[i, 2] = 0u;
                counters[i, 3] = 0u;
                counters[i, 4] = target;
            }

            migrationBuilder.InsertData(
                table: "npc_mission_objective_binding",
                columns: new[]
                {
                    "mission_id", "objective_id", "binding_id", "kind", "area_id", "placement_id", "creature_id",
                    "action_id", "destroying_hit_only", "equip_match", "item_template_id", "item_set_id", "target_state",
                    "counter_id", "comment"
                },
                values: bindings);

            migrationBuilder.InsertData(
                table: "npc_mission_objective_counter",
                columns: new[] { "mission_id", "objective_id", "counter_id", "initial_value", "target_value" },
                values: counters);
        }

        private static void SendObjective(MigrationBuilder migrationBuilder, uint missionId, uint objectiveId, string text,
            uint ordinal, bool revealed)
        {
            migrationBuilder.DeleteData(table: "npc_mission_objective",
                keyColumns: new[] { "mission_id", "objective_id" },
                keyValues: new object[] { missionId, objectiveId });

            migrationBuilder.InsertData(
                table: "npc_mission_objective",
                columns: new[] { "mission_id", "objective_id", "comment", "is_required", "ordinal", "revealed_on_accept" },
                values: new object[,] { { missionId, objectiveId, text, true, ordinal, revealed } });
        }

        private static void DeleteObjectives(MigrationBuilder migrationBuilder)
        {
            var keys = new object[,] { { 955u, 7u }, { 955u, 8u }, { 956u, 8u }, { 976u, 1u } };
            migrationBuilder.DeleteData(table: "npc_mission_objective",
                keyColumns: new[] { "mission_id", "objective_id" }, keyValues: keys);

            // and put the client's own NULL-flag rows back
            migrationBuilder.InsertData(
                table: "npc_mission_objective",
                columns: new[] { "mission_id", "objective_id", "comment", "is_required", "ordinal", "revealed_on_accept" },
                values: new object[,]
                {
                    { 955u, 7u, "Acquire 10 Lasher Claws", null, null, null },
                    { 955u, 8u, "Speak With Dr. Robertson", null, null, null },
                    { 956u, 8u, "Acquire 12 Magmonix Hooves", null, null, null },
                    { 976u, 1u, "Kill 3 Stalkers", null, null, null }
                });
        }

        private static void DeletePlacements(MigrationBuilder migrationBuilder)
        {
            var ids = new List<object>();
            foreach (var npc in Npcs)
                ids.Add(npc.Id);
            ids.Add(Lasher);
            ids.Add(Magmonix);
            ids.Add(Stalker);

            // and the specimen placements, which reference a species rather than carrying one
            foreach (var specimen in Specimens)
                ids.Add(specimen.Id);

            var keys = new object[ids.Count, 1];
            for (var i = 0; i < ids.Count; i++)
                keys[i, 0] = ids[i];

            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: keys);

            var creatureKeys = new object[ids.Count, 1];
            for (var i = 0; i < ids.Count; i++)
                creatureKeys[i, 0] = ids[i];
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: creatureKeys);
        }
    }
}
