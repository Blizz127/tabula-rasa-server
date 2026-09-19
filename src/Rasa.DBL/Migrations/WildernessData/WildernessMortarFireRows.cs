using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// The Bane Mortars of mission 430 fire back.
    ///
    /// Their weapon, Weapon_Creature_Bane_Mortar_Launcher (10604), attacks with action 411 WEAPON_GROUNDTARGET arg 1.
    /// On the client that action is weapons.groundtarget.GroundTargetAttack, a RocketLauncherAttack and so an ordinary
    /// BaseWeaponAttack at a target entity: the server fires it as any creature weapon attack and resolves it with the
    /// weapon-attack recovery.
    ///   - windup 500 ms, cooldown 1,454 ms (windup plus the 954 ms recovery) and range 60 - the client's
    ///     actiondata.actionArguments (411, 1) = (0, None, 500, 954, 60, 0, 0, 1); Lightning's row carries its
    ///     windup and recovery in the same places;
    ///   - damage 16-32 - analogue (OD-55): creature damage in this server is its own scale, not the weapon class's
    ///     (the AFS mini turret's 2,250 weapon damage is a 10-20 action). The mortar weapon's 3,600 is 1.6 times the
    ///     turret's, so the turret row is scaled by that.
    /// The placements become creature-AI guards: a zero-speed creature never wanders or walks back, so the mortar
    /// stays on its base and fires at whoever comes in reach.
    /// </summary>
    public static class WildernessMortarFireRows
    {
        public const uint MortarAction = 45u;

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "creature_action",
                columns: new[] { "id", "description", "action_id", "action_arg_id", "range_min", "range_max", "cooldown", "windup", "min_damage", "max_damage" },
                values: new object[] { MortarAction, "Bane Mortar ground-target launcher (430)", 411u, 1u, 0.0, 60.0, 1454u, 500u, 16u, 32u });
            migrationBuilder.UpdateData(table: "creature", keyColumn: "id", keyValue: WildernessMortarCreatureRows.BaneMortar, column: "action1", value: MortarAction);
            foreach (var placement in WildernessMortarCreatureRows.Placements)
                migrationBuilder.UpdateData(table: "content_placement", keyColumn: "id", keyValue: placement, column: "behavior", value: (byte)2);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var placement in WildernessMortarCreatureRows.Placements)
                migrationBuilder.UpdateData(table: "content_placement", keyColumn: "id", keyValue: placement, column: "behavior", value: (byte)1);
            migrationBuilder.UpdateData(table: "creature", keyColumn: "id", keyValue: WildernessMortarCreatureRows.BaneMortar, column: "action1", value: 0u);
            migrationBuilder.DeleteData(table: "creature_action", keyColumn: "id", keyValue: MortarAction);
        }
    }
}
