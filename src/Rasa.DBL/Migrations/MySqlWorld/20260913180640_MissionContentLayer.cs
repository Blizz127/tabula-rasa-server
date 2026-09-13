using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    public partial class MissionContentLayer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "content_area",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false),
                    map_context_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    shape = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    pos_x = table.Column<double>(type: "double", nullable: false),
                    pos_y = table.Column<double>(type: "double", nullable: false),
                    pos_z = table.Column<double>(type: "double", nullable: false),
                    radius = table.Column<double>(type: "double", nullable: false),
                    half_height = table.Column<double>(type: "double", nullable: false),
                    comment = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_area", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "content_condition",
                columns: table => new
                {
                    condition_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    or_group = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    term_index = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    kind = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    mission_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    objective_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    state = table.Column<uint>(type: "int unsigned", nullable: false),
                    fact_key = table.Column<string>(type: "varchar(64)", nullable: false),
                    value = table.Column<int>(type: "int", nullable: false),
                    negate = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_condition", x => new { x.condition_id, x.or_group, x.term_index });
                });

            migrationBuilder.CreateTable(
                name: "content_item_set",
                columns: table => new
                {
                    item_set_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    item_template_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    quantity = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_item_set", x => new { x.item_set_id, x.item_template_id });
                });

            migrationBuilder.CreateTable(
                name: "content_location",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false),
                    purpose = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    map_context_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    pos_x = table.Column<double>(type: "double", nullable: false),
                    pos_y = table.Column<double>(type: "double", nullable: false),
                    pos_z = table.Column<double>(type: "double", nullable: false),
                    rotation = table.Column<double>(type: "double", nullable: false),
                    comment = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_location", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "content_map_setting",
                columns: table => new
                {
                    map_context_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    instancing = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    comment = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_map_setting", x => x.map_context_id);
                });

            migrationBuilder.CreateTable(
                name: "content_placement",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false),
                    map_context_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    kind = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    creature_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    npc_package_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    entity_class_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    usable_kind = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    pos_x = table.Column<double>(type: "double", nullable: false),
                    pos_y = table.Column<double>(type: "double", nullable: false),
                    pos_z = table.Column<double>(type: "double", nullable: false),
                    rotation = table.Column<double>(type: "double", nullable: false),
                    behavior = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    initial_state = table.Column<uint>(type: "int unsigned", nullable: false),
                    alternate_state = table.Column<uint>(type: "int unsigned", nullable: false),
                    alternate_state_condition_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    windup_ms = table.Column<uint>(type: "int unsigned", nullable: false),
                    name_override_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    hit_points = table.Column<uint>(type: "int unsigned", nullable: false),
                    restore_ms = table.Column<uint>(type: "int unsigned", nullable: false),
                    fuse_ms = table.Column<uint>(type: "int unsigned", nullable: false),
                    loot_item_set_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    respawn_ms = table.Column<uint>(type: "int unsigned", nullable: false),
                    present_condition_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    usable_condition_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    comment = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_placement", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "content_rule",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false),
                    map_context_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    @event = table.Column<byte>(name: "event", type: "tinyint unsigned", nullable: false),
                    mission_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    objective_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    area_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    placement_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    state_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    condition_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    comment = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_rule", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "content_rule_action",
                columns: table => new
                {
                    rule_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    sequence = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    action = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    mission_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    forced = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    greeting_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    npc_name_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    tutorial_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    logos_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    logos_protocol = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    experience = table.Column<uint>(type: "int unsigned", nullable: false),
                    credits = table.Column<int>(type: "int", nullable: false),
                    item_set_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    placement_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    state_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    fact_key = table.Column<string>(type: "varchar(64)", nullable: false),
                    fact_value = table.Column<int>(type: "int", nullable: false),
                    location_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    audio_set_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    comment = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_rule_action", x => new { x.rule_id, x.sequence });
                });

            migrationBuilder.CreateTable(
                name: "npc_mission_objective_binding",
                columns: table => new
                {
                    mission_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    objective_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    binding_id = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    kind = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    area_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    placement_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    creature_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    action_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    destroying_hit_only = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    equip_match = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    item_template_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    item_set_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    target_state = table.Column<uint>(type: "int unsigned", nullable: false),
                    counter_id = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    comment = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_mission_objective_binding", x => new { x.mission_id, x.objective_id, x.binding_id });
                });

            migrationBuilder.CreateTable(
                name: "npc_mission_objective_counter",
                columns: table => new
                {
                    mission_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    objective_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    counter_id = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    initial_value = table.Column<int>(type: "int", nullable: false),
                    target_value = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_mission_objective_counter", x => new { x.mission_id, x.objective_id, x.counter_id });
                });

            migrationBuilder.CreateTable(
                name: "npc_mission_objective_indicator",
                columns: table => new
                {
                    mission_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    objective_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    indicator_index = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    indicator_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    pos_x = table.Column<double>(type: "double", nullable: false),
                    pos_y = table.Column<double>(type: "double", nullable: false),
                    pos_z = table.Column<double>(type: "double", nullable: false),
                    radius = table.Column<double>(type: "double", nullable: false),
                    show_3d = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_mission_objective_indicator", x => new { x.mission_id, x.objective_id, x.indicator_index });
                });

            migrationBuilder.CreateTable(
                name: "npc_mission_objective_timer",
                columns: table => new
                {
                    mission_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    objective_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    limit_seconds = table.Column<uint>(type: "int unsigned", nullable: false),
                    on_expire = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_mission_objective_timer", x => new { x.mission_id, x.objective_id });
                });

            migrationBuilder.CreateTable(
                name: "npc_mission_prerequisite",
                columns: table => new
                {
                    mission_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    or_group = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    required_mission_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    required_state = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    comment = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_mission_prerequisite", x => new { x.mission_id, x.or_group, x.required_mission_id });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "content_area");

            migrationBuilder.DropTable(
                name: "content_condition");

            migrationBuilder.DropTable(
                name: "content_item_set");

            migrationBuilder.DropTable(
                name: "content_location");

            migrationBuilder.DropTable(
                name: "content_map_setting");

            migrationBuilder.DropTable(
                name: "content_placement");

            migrationBuilder.DropTable(
                name: "content_rule");

            migrationBuilder.DropTable(
                name: "content_rule_action");

            migrationBuilder.DropTable(
                name: "npc_mission_objective_binding");

            migrationBuilder.DropTable(
                name: "npc_mission_objective_counter");

            migrationBuilder.DropTable(
                name: "npc_mission_objective_indicator");

            migrationBuilder.DropTable(
                name: "npc_mission_objective_timer");

            migrationBuilder.DropTable(
                name: "npc_mission_prerequisite");
        }
    }
}
