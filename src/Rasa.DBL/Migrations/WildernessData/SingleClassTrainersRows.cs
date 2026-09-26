using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// One class trainer per hub, as the final live game had them: the 38 per-class trainers retired, and Training
    /// Officer Stratton standing on the client's "Class Trainer: Daghda's Urn" marker (OD-95). The evidence, decoded
    /// from the 1.16.5.0 client, is <c>docs/evidence/class-trainer-evidence.json</c>.
    ///
    /// <b>Why the per-class trainers go.</b> Add_class_trainers (d717ed1, the InfiniteRasa data pass) seeded 38
    /// creatures 501001-501038 - "Soldier Trainer: Alia Das", "Commando Trainer: Twin Pillars" and so on - from the
    /// client's per-class trainer dialogue groups and marker texts, six of them in a ring 3-6 m round Kincaid. That is
    /// the pre-D12 model. The official notes of 18 September 2008 (D12, reproduced on TaRapedia "Updates/2008-09-18",
    /// rev 35871) say: "Individual trainers for each class have been replaced by one single trainer on maps that had
    /// trainers previously." The final client agrees. Its live Wilderness map (context 1220, template 1378) carries one
    /// TRAINER marker per hub - 980 "Class Trainer: Alia Das", 981 "Class Trainer: Twin Pillars", 987 "Class Trainer:
    /// Daghda's Urn" - and the Divide and Palisades one each, 513 "Trainer: Foreas Base" and 1096 "Trainer: New
    /// Cumbria". The per-class markers 977/978 "Soldier/Specialist Trainer: Alia Das" survive only in template 2269, the
    /// unused wargame copy adv_wargame_foreas_concordia_wilderness_revisited (context 2265, no level range). The generic
    /// trainer line 337 says "I'm here to train you, regardless of your specialty". The 38 rows carry no npc_package
    /// and the server recognises no class trainer without one, so they could neither talk nor train: they only stood in
    /// the way. Their pools stop drawing (0/0), the way SolisCavernsPlacement retired pool 92; the creature,
    /// appearance and pool rows stay, so Down only restores the counts. The Fort Defiance, Torden Mires and Ashen
    /// Desert rings go for the same reason; their hubs' own single trainers are unrecovered (GAP-LATER-HUB-TRAINERS).
    ///
    /// <b>Training Officer Stratton (creature and placement 199604).</b> TaRapedia's Daghda's Urn page lists
    /// "Training Officer Stratton" among its contacts in revision 35503 (Chromecat77, 2008-11-04), seven weeks after
    /// D12, and the final client's creature names carry him as 10606 "Training Officer Stratton", in the block
    /// 10603-10610 that also holds 10604 "Training Officer Kincaid". The name id is original; the attribution to
    /// Daghda's Urn is TaRapedia's (inferred, high). He stands on marker 987 "Class Trainer: Daghda's Urn" at the
    /// marker's own x, y, z (-599.685, 276.605, 871.195), as Kincaid stands 0.6 m from marker 980 (inferred); the
    /// navmesh surface there is 276.718 and the path from the Daghda's Urn waypoint is complete (82.8 m). No source
    /// shows him, so his class 3846, the body Kincaid wears, his 1000 hp, his level 8 and his facing 0 are analogues
    /// under OD-95 (GAP-DAGHDA-TRAINER-PRESENTATION). His client conversation package is not recovered either: the
    /// client needs none to train (npc.CanTrain reads only CONVO_TYPE_TRAINING, and the Converse action appears for any
    /// conversation status but NONE), so he carries none and ClassAdvancement recognises him by creature id.
    ///
    /// <b>Not placed.</b> No source names the single trainer of Twin Pillars, Foreas Base or New Cumbria
    /// (GAP-HUB-TRAINER-IDENTITY-TWIN-PILLARS, -FOREAS-BASE, -NEW-CUMBRIA). The client's unassigned "Training Officer"
    /// names (Lebowicz, Buckmaster, Delany, Walker, Howell) are candidates, not evidence, and are not used.
    /// </summary>
    public static class SingleClassTrainersRows
    {
        public const string Migration = "SingleClassTrainers";

        /// <summary>The per-class trainer pools of Add_class_trainers, each drawing its own creature once.</summary>
        public const uint PerClassFirst = 501001u, PerClassLast = 501038u;

        public const uint Stratton = 199604u;
        public const uint StrattonName = 10606u;
        public const uint Wilderness = 1220u;

        /// <summary>Marker 987 "Class Trainer: Daghda's Urn", client uimapmarker staticmarkers[1378].</summary>
        public const double StrattonX = -599.685, StrattonY = 276.605, StrattonZ = 871.195;

        /// <summary>Kincaid's body (creature 198515 as AppearanceSlotRepair left it): (slot, class, colour), OD-95.</summary>
        public static readonly uint[][] Body =
        {
            new[] { 2u, 4021u, 22120u },
            new[] { 13u, 27120u, 1u },
            new[] { 14u, 3672u, 14404004u },
            new[] { 15u, 4023u, 13933202u },
            new[] { 16u, 4022u, 933202u },
            new[] { 17u, 24019u, 4286886614u }
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            for (var id = PerClassFirst; id <= PerClassLast; id++)
                Counts(migrationBuilder, id, 0);

            // {"id": 199604} Training Officer Stratton: original: name_id; analogue (OD-95): class_id, level, max_hp;
            // inferred: faction, run_speed, walk_speed, action1
            migrationBuilder.InsertData(
                table: "creature",
                columns: new[] { "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed", "walk_speed", "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8" },
                values: new object[] { Stratton, "Training Officer Stratton", 3846u, 1u, 8u, 1000u, StrattonName, 0.0, 0.0, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u });

            // {"id": 199604} Training Officer Stratton (Daghda's Urn): inferred: pos_x, pos_y, pos_z (marker 987), kind,
            // creature_id, behavior; analogue (OD-95): rotation; no npc_package_id (unrecovered, optional)
            migrationBuilder.InsertData(
                table: "content_placement",
                columns: new[] { "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id", "usable_kind", "pos_x", "pos_y", "pos_z", "rotation", "behavior", "initial_state", "alternate_state", "alternate_state_condition_id", "windup_ms", "name_override_id", "hit_points", "restore_ms", "fuse_ms", "loot_item_set_id", "respawn_ms", "present_condition_id", "usable_condition_id", "comment" },
                values: new object[] { Stratton, Wilderness, (byte)1, Stratton, 0u, 0u, (byte)0, StrattonX, StrattonY, StrattonZ, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Training Officer Stratton (Class Trainer: Daghda's Urn marker)" });

            foreach (var piece in Body)
                migrationBuilder.InsertData(table: "creature_appearance", columns: new[] { "id", "slot_id", "Class_id", "color" },
                    values: new object[] { Stratton, piece[0], piece[1], piece[2] });
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var piece in Body)
                migrationBuilder.DeleteData(table: "creature_appearance", keyColumns: new[] { "id", "slot_id" },
                    keyValues: new object[] { Stratton, piece[0] });

            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { Stratton });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { Stratton });

            for (var id = PerClassFirst; id <= PerClassLast; id++)
                Counts(migrationBuilder, id, 1);
        }

        private static void Counts(MigrationBuilder migrationBuilder, uint id, byte count)
        {
            migrationBuilder.UpdateData(table: "spawnpool", keyColumn: "id", keyValue: id, column: "creature_1_min_count", value: count);
            migrationBuilder.UpdateData(table: "spawnpool", keyColumn: "id", keyValue: id, column: "creature_1_max_count", value: count);
        }
    }
}
