using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// Mission 430 "Mortar By Numbers": the four Bane mortars become the creatures the client can target, instead of
    /// destroyable launchers it cannot.
    ///
    /// Batch 13 placed the mortars as destroyables of the class the client map gives the launchers, 7478
    /// ArchBaneGenObjMortarlauncherBaseV01. To the client that class is scenery: no augmentations and target_flag 0,
    /// so it can be neither targeted nor damaged, and the mission could not be finished. TaRapedia's "Mortar" page
    /// (rev. 18949, 2007-12-08) says what the originals were: "An automated Bane mortar turret. Like all
    /// fortifications, these have exceptionally heavy armor for their level" - fortifications, which in the client
    /// are creatures (the Emplacement_* turret classes carry Creature and Harvestable and are targetable).
    ///
    /// No client class names a mortar creature, so the creature is reconstructed:
    ///   - name 8186 "Bane Mortar" - original, the client's creature-name table;
    ///   - class 7482 Emplacement_Bane_Turret_Standard - analogue (OD-55): the Bane fortification creature class, whose
    ///     mesh 19366 is the neighbour of the destroyed launcher's 19367;
    ///   - weapon 10604 Weapon_Creature_Bane_Mortar_Launcher - original, the client's creature mortar weapon;
    ///   - level 5, the mission's level - inferred; 1,500 hit points, the only fortification creature the world has
    ///     (AFS_Turret_Mini, level 4) - analogue (OD-55); a 60 s respawn so every player on the shared map can do the
    ///     mission - analogue (OD-48).
    /// The positions stay the client map's own launcher positions: the base is map scenery, the turret stands on it.
    ///
    /// The mortar's own fire is not built: its weapon attacks with action 411 WEAPON_GROUNDTARGET, which creature
    /// AI does not perform, so the placements are stationary (GAP-W3-430-MORTAR-FIRE).
    /// </summary>
    public static class WildernessMortarCreatureRows
    {
        public const uint BaneMortar = 199720u;
        public const uint MortarClass = 7482u;
        public const uint MortarName = 8186u;
        public const uint MortarWeaponClass = 10604u;
        public const uint LauncherClass = 7478u;
        public const byte WeaponSlot = 13;
        public static readonly uint[] Placements = { 199700u, 199701u, 199702u, 199703u };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "creature",
                columns: new[] { "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed", "walk_speed",
                    "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8" },
                values: new object[] { BaneMortar, "Bane Mortar (430, OD-55 reconstruction)", MortarClass, 0u, 5u, 1500u, MortarName,
                    0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u });

            migrationBuilder.InsertData(
                table: "creature_appearance",
                columns: new[] { "id", "slot_id", "Class_id", "color" },
                values: new object[] { BaneMortar, (uint)WeaponSlot, MortarWeaponClass, 0u });

            foreach (var placement in Placements)
            {
                Set(migrationBuilder, placement, "kind", (byte)1);
                Set(migrationBuilder, placement, "creature_id", BaneMortar);
                Set(migrationBuilder, placement, "entity_class_id", 0u);
                Set(migrationBuilder, placement, "usable_kind", (byte)0);
                Set(migrationBuilder, placement, "initial_state", 0u);
                Set(migrationBuilder, placement, "hit_points", 0u);
                Set(migrationBuilder, placement, "respawn_ms", 60000u);
            }

            for (var i = 0; i < Placements.Length; i++)
                Bind(migrationBuilder, (uint)(3 + i), kind: 6, destroyingHitOnly: false);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            for (var i = 0; i < Placements.Length; i++)
                Bind(migrationBuilder, (uint)(3 + i), kind: 5, destroyingHitOnly: true);

            foreach (var placement in Placements)
            {
                Set(migrationBuilder, placement, "kind", (byte)2);
                Set(migrationBuilder, placement, "creature_id", 0u);
                Set(migrationBuilder, placement, "entity_class_id", LauncherClass);
                Set(migrationBuilder, placement, "usable_kind", (byte)2);
                Set(migrationBuilder, placement, "initial_state", 110u);
                Set(migrationBuilder, placement, "hit_points", 100u);
                Set(migrationBuilder, placement, "respawn_ms", 0u);
            }

            migrationBuilder.DeleteData(table: "creature_appearance", keyColumns: new[] { "id", "slot_id" },
                keyValues: new object[] { BaneMortar, (uint)WeaponSlot });
            migrationBuilder.DeleteData(table: "creature", keyColumn: "id", keyValue: BaneMortar);
        }

        private static void Set(MigrationBuilder migrationBuilder, uint placement, string column, object value)
            => migrationBuilder.UpdateData(table: "content_placement", keyColumn: "id", keyValue: placement, column: column, value: value);

        private static void Bind(MigrationBuilder migrationBuilder, uint objective, byte kind, bool destroyingHitOnly)
        {
            var keys = new[] { "mission_id", "objective_id", "binding_id" };
            var values = new object[] { 430u, objective, (byte)0 };
            migrationBuilder.UpdateData(table: "npc_mission_objective_binding", keyColumns: keys, keyValues: values, column: "kind", value: kind);
            migrationBuilder.UpdateData(table: "npc_mission_objective_binding", keyColumns: keys, keyValues: values, column: "destroying_hit_only", value: destroyingHitOnly);
        }
    }
}
