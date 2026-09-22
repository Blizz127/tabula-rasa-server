using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// Eight NPCs whose dialogue no creature carried, and Captain Reyko given his own
    /// (GAP-W3-UNBOUND-CONVERSATION-PACKAGE, GAP-W3-MISASSIGNED-PACKAGE).
    ///
    /// Each objective below completes through a package the client's objectiveconversation table names, and the
    /// speaker is named by the mission's own text or objectives. Every position is TaRapedia's own /loc, and each
    /// reading lands within half a metre of the navmesh floor under it, so Y is that floor less the world's spawn
    /// offset:
    ///
    ///   382/1  (177)  Recon Officer Tyler        Minos Caverns, "at loc: 58, -30, -49"      ground -30.47
    ///   969/3  (1065) Field Sgt. Kalinowski      Fort Haroun, 565, 232, 355                  ground 232.16
    ///   969/4  (1092) Mohindra                   Fort Haroun main room, 603, 224, 342        ground 224.16
    ///   977/3  (1051) Major Ston                 Fort Haroun, 571, 224, 333                  ground 224.16
    ///   1040/2 (1118) Beta Squad Cmdr Petrie     Martyr's Canyon, -147, 408, -882            ground 408.43
    ///   1040/3 (1117) Delta Squad Cmdr Locke     Maligo Creek, 164, 323, -771                ground 322.83
    ///   1119/1 (1203) Surveyor Miras             Kardash Atta Colony, 4, 273, -200           ground 272.63
    ///   1183/1 (1273) Sergeant Phenix            Phanin Research Facility, -11, -8, 8        ground -7.50
    ///
    /// Which commander answers which of Incommunicado's objectives is inferred from their order: the mission names
    /// "Alpha Squad Commander Carville, Beta Squad Commander Petrie and Delta Squad Commander Locke", and objective 1
    /// is already Carvelle's (package 1116), so 2 is Beta and 3 is Delta. Find Me A Rock's objectives - "Ask Sirth",
    /// "Ask Rohish", "Ask Major Ston" - name 1051's speaker, whose line answers for the paperwork at Fort Haroun.
    ///
    /// Captain Reyko carried 1213, the Irendas console's line ("This console appears to have an active
    /// communications system. You should be able to contact Captain Reyko from here"), which cannot be his. His own
    /// is 1200, which completes 1113/7, 1125/1, 1310/1 and the 20000003 mission. Moving him leaves 1112/1 with no
    /// speaker until the console is dispensed as a usable, and no source gives its position - the owner chose that
    /// trade (2026-09-19).
    ///
    /// Classes and hit points are OD-45 analogues: the AFS human NPC class 3846 with the shipped officer appearance
    /// set (ContentNpcAppearance), and for the two Brann, Redshirt_Brann_Male (7253), which carries its own mesh.
    /// Levels are TaRapedia's own except Tyler's, whose page gives none and whose mission (382) is level 10.
    /// </summary>
    public static class QuestNpcDialogueBatchRows
    {
        public const string Migration = "QuestNpcDialogueBatch";

        public const uint Tyler = 199006u, Petrie = 199207u, Locke = 199208u, Kalinowski = 199406u,
            Mohindra = 199407u, Ston = 199408u, Miras = 199506u, Phenix = 199507u;

        public const uint Reyko = 199502u;
        public const uint ReykoPackage = 1200u, ConsolePackage = 1213u;

        /// <summary>Field Lt. Brody, whose placement still carried Captain Fransisco's package after DevilsDenFransisco.</summary>
        public const uint Brody = 199105u, BrodyPackage = 145u, FransiscoPackageOnBrody = 550u;

        private const uint MinosCaverns = 1347u, Plateau = 1497u, Mires = 1759u, AttaColony = 1773u, PhaninFacility = 2034u;
        private const uint AfsHuman = 3846u, BrannMale = 7253u;
        private const uint Stationary = 1u;

        // creature id, comment, class, level, name id, package, map, x, y (floor + OriginalSpawnOffset), z, where
        private static readonly object[][] Npcs =
        {
            new object[] { Tyler, "Recon Officer Tyler (Minos Caverns)", AfsHuman, 10u, 3017u, 177u, MinosCaverns, 58.0, -30.471642 + Offset, -49.0, "TaRapedia Retrieval for Recon: \"at loc: 58, -30, -49\"" },
            new object[] { Kalinowski, "Field Sgt. Kalinowski (Fort Haroun)", AfsHuman, 27u, 8686u, 1065u, Mires, 565.0, 232.16043 + Offset, 355.0, "TaRapedia /loc, Fort Haroun side room" },
            new object[] { Mohindra, "Mohindra (Fort Haroun main room)", BrannMale, 30u, 8688u, 1092u, Mires, 603.0, 224.16043 + Offset, 342.0, "TaRapedia /loc, Fort Haroun main room" },
            new object[] { Ston, "Major Ston (Fort Haroun)", AfsHuman, 31u, 8672u, 1051u, Mires, 571.0, 224.16043 + Offset, 333.0, "TaRapedia /loc, west of the Fort Haroun waypoint" },
            new object[] { Petrie, "Beta Squad Commander Petrie (Martyr's Canyon)", AfsHuman, 35u, 8848u, 1118u, Plateau, -147.0, 408.4313 + Offset, -882.0, "TaRapedia /loc, Martyr's Canyon" },
            new object[] { Locke, "Delta Squad Commander Locke (Maligo Creek)", AfsHuman, 35u, 8849u, 1117u, Plateau, 164.0, 322.8284 + Offset, -771.0, "TaRapedia /loc, Maligo Creek" },
            new object[] { Miras, "Surveyor Miras (Kardash Atta Colony)", BrannMale, 23u, 9010u, 1203u, AttaColony, 4.0, 272.62616 + Offset, -200.0, "TaRapedia /loc, Research Outpost Alpha" },
            new object[] { Phenix, "Sergeant Phenix (Phanin Research Facility)", AfsHuman, 25u, 9141u, 1273u, PhaninFacility, -11.0, -7.499996 + Offset, 8.0, "TaRapedia /loc, just inside the entrance" }
        };

        private const double Offset = WorldPlacementFloorSnapRows.OriginalSpawnOffset;

        /// <summary>The officer set of ContentNpcAppearance, for the ones on the swapset class.</summary>
        private static readonly uint[][] Officer =
        {
            new[] { 2u, 4021u, 22120u }, new[] { 13u, 27120u, 1u }, new[] { 14u, 3672u, 14404004u },
            new[] { 16u, 4022u, 933202u }, new[] { 15u, 4023u, 13933202u }, new[] { 17u, 24019u, 4286886614u }
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var npc in Npcs)
            {
                var id = (uint)npc[0];
                migrationBuilder.InsertData(
                    table: "creature",
                    columns: new[] { "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed", "walk_speed",
                        "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8" },
                    values: new object[] { id, npc[1], npc[2], 1u, npc[3], 555u, npc[4], 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u });

                migrationBuilder.InsertData(
                    table: "npc_package",
                    columns: new[] { "id", "package_id", "comment" },
                    values: new object[] { id, npc[5], $"{npc[1]} (client conversation package {npc[5]})" });

                migrationBuilder.InsertData(
                    table: "content_placement",
                    columns: new[] { "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id", "usable_kind",
                        "pos_x", "pos_y", "pos_z", "rotation", "behavior", "initial_state", "alternate_state", "alternate_state_condition_id",
                        "windup_ms", "name_override_id", "hit_points", "restore_ms", "fuse_ms", "loot_item_set_id", "respawn_ms",
                        "present_condition_id", "usable_condition_id", "comment" },
                    values: new object[] { id, npc[6], (byte)1, id, npc[5], 0u, (byte)0, npc[7], npc[8], npc[9], 0.0,
                        (byte)Stationary, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, $"{npc[1]} ({npc[10]}, navmesh floor)" });

                if ((uint)npc[2] != AfsHuman)
                    continue;

                foreach (var piece in Officer)
                    migrationBuilder.InsertData(
                        table: "creature_appearance",
                        columns: new[] { "id", "slot_id", "Class_id", "color" },
                        values: new object[] { id, piece[0], piece[1], piece[2] });
            }

            // Both rows: CreatureManager takes the placement's package when it is set, so the npc_package row alone
            // leaves the creature speaking the old one. DevilsDenFransisco changed only the row, which left Field Lt.
            // Brody still carrying Captain Fransisco's 550 in play; his placement is corrected here too.
            migrationBuilder.UpdateData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { Reyko },
                column: "package_id", value: ReykoPackage);
            migrationBuilder.UpdateData(table: "content_placement", keyColumns: new[] { "creature_id" }, keyValues: new object[] { Reyko },
                column: "npc_package_id", value: ReykoPackage);
            migrationBuilder.UpdateData(table: "content_placement", keyColumns: new[] { "creature_id" }, keyValues: new object[] { Brody },
                column: "npc_package_id", value: BrodyPackage);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { Reyko },
                column: "package_id", value: ConsolePackage);
            migrationBuilder.UpdateData(table: "content_placement", keyColumns: new[] { "creature_id" }, keyValues: new object[] { Reyko },
                column: "npc_package_id", value: ConsolePackage);
            migrationBuilder.UpdateData(table: "content_placement", keyColumns: new[] { "creature_id" }, keyValues: new object[] { Brody },
                column: "npc_package_id", value: FransiscoPackageOnBrody);

            foreach (var npc in Npcs)
            {
                var id = (uint)npc[0];
                if ((uint)npc[2] == AfsHuman)
                    foreach (var piece in Officer)
                        migrationBuilder.DeleteData(table: "creature_appearance", keyColumns: new[] { "id", "slot_id" }, keyValues: new object[] { id, piece[0] });

                migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { id });
                migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { id });
                migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { id });
            }
        }
    }
}
