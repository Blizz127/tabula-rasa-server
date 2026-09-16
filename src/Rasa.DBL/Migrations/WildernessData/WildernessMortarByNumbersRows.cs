using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// W3 batch 13: "Mortar By Numbers" - the first mission reconstructed from the client map data itself.
    ///
    /// The four objectives are all "Destroy Mortar #N" and the client has no conversations for them, which is why the
    /// conversation batches had to skip the mission. The objects they name are in the map: the client's own
    /// adv_foreas_concordia_wilderness.map carries six intact <c>ArchBaneGenObjMortarlauncherV01</c> launchers (entity
    /// class 7478) and one already-destroyed one, each with the position the map gives it. The four in the northern
    /// group are the mission's targets - the two southern launchers are a separate outpost, and the destroyed one is
    /// the aftermath of the same battle the mission is cleaning up. The placements below therefore carry **original**
    /// positions, not readings.
    ///
    /// What no source gives is a destroyable object's hit points. The camp's practice dummy - the one destroyable the
    /// boot camp already had - uses 100, so these use that value and it is recorded as an analogue under OD-46 rather
    /// than presented as recovered.
    ///
    /// The binding is the kind the camp already uses for a destroying hit (ObjectiveBindingKind.Hit with
    /// destroying_hit_only), so nothing new is needed to make the objectives completable.
    /// </summary>
    public static class WildernessMortarByNumbersRows
    {
        public const string Migration = "WildernessMortarByNumbers";

        public const uint Mission = 430u;
        public const uint Wagner = 104u;

        /// <summary>The mortar launcher entity class, from the client map's own entity list.</summary>
        private const uint MortarClass = 7478u;

        private const byte UsablePlacement = 2;
        private const byte Destroyable = 2;
        private const byte HitBinding = 5;
        private const byte NoCounter = 255;

        /// <summary>The four northern launchers, in the order the map lists them.</summary>
        private static readonly (uint Id, double X, double Y, double Z)[] Mortars =
        {
            (199700u, 360.8661, 218.1929, 95.2629),
            (199701u, 212.7184, 227.3504, 286.1895),
            (199702u, 185.0261, 238.0952, 399.923),
            (199703u, 99.713, 232.3222, 551.5623)
        };

        /// <summary>objective id -> the mortar it targets, in objective order.</summary>
        private static readonly (uint ObjectiveId, uint PlacementId)[] Bindings =
        {
            (3u, 199700u),
            (4u, 199701u),
            (5u, 199702u),
            (6u, 199703u)
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            var values = new object[Mortars.Length, 25];
            for (var i = 0; i < Mortars.Length; i++)
            {
                var (id, x, y, z) = Mortars[i];
                values[i, 0] = id;
                values[i, 1] = 1220u;                       // adv_foreas_concordia_wilderness
                values[i, 2] = UsablePlacement;
                values[i, 3] = 0u;                          // creature
                values[i, 4] = 0u;                          // npc package
                values[i, 5] = MortarClass;
                values[i, 6] = Destroyable;
                values[i, 7] = x;
                values[i, 8] = y;
                values[i, 9] = z;
                values[i, 10] = 0.0;                        // rotation
                values[i, 11] = (byte)1;                    // stationary
                values[i, 12] = 110u;                       // the state a destroyable stands in
                values[i, 13] = 0u;
                values[i, 14] = 0u;
                values[i, 15] = 0u;
                values[i, 16] = 0u;
                values[i, 17] = 100u;                       // hit points (OD-46)
                values[i, 18] = 0u;
                values[i, 19] = 0u;
                values[i, 20] = 0u;
                values[i, 21] = 0u;
                values[i, 22] = 0u;                        // present condition
                values[i, 23] = 0u;                        // usable condition
                values[i, 24] = $"430 mortar {i + 1} (client map position)";
            }

            migrationBuilder.InsertData(
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

            migrationBuilder.InsertData(
                table: "npc_mission",
                columns: new[] { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" },
                values: new object[,]
                {
                    { Mission, Wagner, Wagner, 5u, 1u, 10000001u, false, false, "Mortar By Numbers (W3)" }
                });

            ReplaceObjectives(migrationBuilder);
            AddChain(migrationBuilder);

            var bindings = new object[Bindings.Length, 15];
            for (var i = 0; i < Bindings.Length; i++)
            {
                var (objectiveId, placementId) = Bindings[i];
                bindings[i, 0] = Mission;
                bindings[i, 1] = objectiveId;
                bindings[i, 2] = 0u;
                bindings[i, 3] = HitBinding;
                bindings[i, 4] = 0u;
                bindings[i, 5] = placementId;
                bindings[i, 6] = 0u;
                bindings[i, 7] = 0u;
                bindings[i, 8] = true;                      // a destroying hit
                bindings[i, 9] = false;
                bindings[i, 10] = 0u;
                bindings[i, 11] = 0u;
                bindings[i, 12] = 0u;
                bindings[i, 13] = NoCounter;
                bindings[i, 14] = $"430/{objectiveId} destroy mortar {placementId}";
            }

            migrationBuilder.InsertData(
                table: "npc_mission_objective_binding",
                columns: new[]
                {
                    "mission_id", "objective_id", "binding_id", "kind", "area_id", "placement_id", "creature_id",
                    "action_id", "destroying_hit_only", "equip_match", "item_template_id", "item_set_id",
                    "target_state", "counter_id", "comment"
                },
                values: bindings);

            migrationBuilder.InsertData(
                table: "npc_mission_reward",
                columns: new[] { "id", "type", "credits", "item_template_id", "quantity" },
                values: new object[,]
                {
                    { Mission, 3u, 6000, 0u, 0u },          // experience
                    { Mission, 1u, 900, 0u, 0u }            // credits
                });
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "npc_mission_reward",
                keyColumns: new[] { "id", "type", "item_template_id" },
                keyValues: new object[,] { { Mission, 3u, 0u }, { Mission, 1u, 0u } });

            migrationBuilder.DeleteData(table: "npc_mission_objective_binding",
                keyColumns: new[] { "mission_id", "objective_id", "binding_id" },
                keyValues: new object[,] { { Mission, 3u, 0u }, { Mission, 4u, 0u }, { Mission, 5u, 0u }, { Mission, 6u, 0u } });

            var transitions = new object[3, 3];
            for (var i = 0; i < 3; i++)
            {
                transitions[i, 0] = Mission;
                transitions[i, 1] = Bindings[i].ObjectiveId;
                transitions[i, 2] = Bindings[i + 1].ObjectiveId;
            }

            migrationBuilder.DeleteData(table: "npc_mission_objective_transition",
                keyColumns: new[] { "mission_id", "completed_objective_id", "revealed_objective_id" },
                keyValues: transitions);

            migrationBuilder.DeleteData(table: "npc_mission_objective",
                keyColumns: new[] { "mission_id", "objective_id" },
                keyValues: new object[,] { { Mission, 3u }, { Mission, 4u }, { Mission, 5u }, { Mission, 6u } });

            migrationBuilder.DeleteData(table: "npc_mission",
                keyColumns: new[] { "id" },
                keyValues: new object[] { Mission });

            var keys = new object[Mortars.Length, 1];
            for (var i = 0; i < Mortars.Length; i++)
                keys[i, 0] = Mortars[i].Id;

            migrationBuilder.DeleteData(table: "content_placement",
                keyColumns: new[] { "id" },
                keyValues: keys);
        }

        private static void ReplaceObjectives(MigrationBuilder migrationBuilder)
        {
            // The client skeleton already carries these objectives with all flags NULL, so the rows are replaced.
            foreach (var objectiveId in new[] { 3u, 4u, 5u, 6u })
                migrationBuilder.DeleteData(
                    table: "npc_mission_objective",
                    keyColumns: new[] { "mission_id", "objective_id" },
                    keyValues: new object[] { Mission, objectiveId });

            var values = new object[,]
            {
                { Mission, 3u, "Destroy Mortar #1", true, 1u, true },
                { Mission, 4u, "Destroy Mortar #2", true, 2u, false },
                { Mission, 5u, "Destroy Mortar #3", true, 3u, false },
                { Mission, 6u, "Destroy Mortar #4.", true, 4u, false }
            };

            migrationBuilder.InsertData(
                table: "npc_mission_objective",
                columns: new[] { "mission_id", "objective_id", "comment", "is_required", "ordinal", "revealed_on_accept" },
                values: values);
        }

        private static void AddChain(MigrationBuilder migrationBuilder)
        {
            var values = new object[,]
            {
                { Mission, 3u, 4u },
                { Mission, 4u, 5u },
                { Mission, 5u, 6u }
            };

            migrationBuilder.InsertData(
                table: "npc_mission_objective_transition",
                columns: new[] { "mission_id", "completed_objective_id", "revealed_objective_id" },
                values: values);
        }
    }
}
