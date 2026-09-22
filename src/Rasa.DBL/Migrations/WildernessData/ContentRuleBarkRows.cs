using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    using Structures.World;

    /// <summary>
    /// The camp's first spoken line: Major McAllister's bark 852, and the column the content layer needs to ask
    /// for a bark at all (ContentRuleAction.PlayBark = 16, content_rule_action.bark_id).
    ///
    /// <b>ORIGINAL.</b> The line, its voice and its length are the client's own, unaltered:
    /// <c>generated/client/bark.pyo</c> row 852 = <c>(49815, 10000)</c>; <c>stringtable.pyo</c>[49815] =
    /// <c>boot_camp_major_mcallister_bark.ogg</c>; <c>audiosetdata.pyo</c> audioSet 2777 = "Bark English Male
    /// Bootcamp McAllister"; <c>barklanguage.pyo</c>[852] = "Soldier! If you're done communing with your alien
    /// buddies, we could use a hand saving our asses around here!" (identical for a male and a female recruit).
    /// The server sends none of that: <c>Bark = 411</c> carries the id alone and the client resolves the rest.
    ///
    /// <b>INFERRED, high confidence - who says it.</b> The asset names McAllister and the audio set names the boot
    /// camp; creature 198500 / placement 198650 is the only McAllister this world has and he stands in that camp.
    /// No surviving table binds a bark id to a creature - that table lived on the original server - so the
    /// binding is this project's, not the client's.
    ///
    /// <b>INFERRED, medium confidence - when he says it.</b> No recovered data records any bark's trigger, and the
    /// client has none of its own: <c>Recv_Bark</c> is the only entry point and nothing calls it locally, so every
    /// bark in the retail world was a server push on a schedule that did not survive. The line's own text names
    /// its moment - "if you're done communing with your alien buddies" is mission 1990, the two Eloh holograms of
    /// rules 1985002 and 1985003 - so it is placed in the beat between the last hologram and the turn-in that
    /// rule 1985004 answers.
    ///
    /// It is the recruit's <i>return</i> that is used, not the objective's completion, and the client's own
    /// numbers force that: a bark plays at 2 m / 20 m and the overhead bubble is culled at 20 m
    /// (creature.pyo Recv_Bark line 117, overheadwindow.pyo kOverheadDisplayMaxDistance). Hologram 2 is at
    /// (387.83, 112.75, 6.71) and McAllister at (387.2, 125.57, 53.3) - 48.3 m apart - so a bark fired the instant
    /// objective 2 completed would be a packet the client renders as nothing. The rule therefore fires when the
    /// recruit walks back inside McAllister's earshot with 1990 still in hand:
    ///
    ///   * <b>content_area 198604</b> - a sphere on McAllister's own position, radius <b>20 m</b>. The radius is
    ///     not invented: it is the client's bark range, so entering the area and being able to hear him are the
    ///     same event.
    ///   * <b>content_condition 198913</b> - 1990's objective 2 is completed <i>and</i> 1990 is still Active. The
    ///     second term closes the line after the turn-in: the recruit walks past McAllister all through S2-S5 and
    ///     an area rule re-arms every time it is left.
    ///   * <b>content_rule 1985016</b> - area_entered on 198604, and one action: placement 198650 says bark 852.
    ///
    /// What a recruit hears: after the second hologram, walking back up the bridge, McAllister shouts the line
    /// once from about 20 m out, the bubble stands over him for the 10 s the client's table gives it, and the
    /// 1990 turn-in and the 1992 offer follow as they already do.
    /// </summary>
    public static class ContentRuleBarkRows
    {
        public const string Migration = "ContentRuleBark";

        public const string BarkIdColumn = "bark_id";

        /// <summary>bark.pyo row 852, 10 000 ms, boot_camp_major_mcallister_bark.ogg (ORIGINAL).</summary>
        public const uint McAllisterBark = 852u;

        public const uint EarshotArea = 198604u;
        public const uint ReturnedCondition = 198913u;
        public const uint BarkRule = 1985016u;

        private const uint Bootcamp = 1985u;
        private const uint McAllisterPlacement = 198650u;
        private const uint Mission1990 = 1990u;
        private const uint HologramObjective = 2u;

        private const byte Sphere = 1;
        private const byte AreaEntered = 10;
        private const byte PlayBark = 16;
        private const byte MissionStateIs = 1;
        private const byte ObjectiveStateIs = 2;
        private const uint ObjectiveCompleted = 2;   // MissionObjectiveState.Completed
        private const uint MissionActive = 0;        // MissionState.Active - accepted and not yet turned in

        /// <summary>McAllister's placement position (BootcampS1InitiationRows), which the earshot sphere is centred on.</summary>
        private const double McAllisterX = 387.2;
        private const double McAllisterY = 125.57;
        private const double McAllisterZ = 53.3;

        /// <summary>The client's bark range: creature.pyo Recv_Bark line 117 overwrites the clip's own falloff with (2.0, 20.0).</summary>
        public const double EarshotRadius = 20.0;

        public static void InsertData(MigrationBuilder migrationBuilder, string barkIdColumnType)
        {
            // Which of the client's 859 bark rows a play_bark action says. 0 for every other action, so every
            // existing row is unchanged.
            migrationBuilder.AddColumn<uint>(
                name: BarkIdColumn,
                table: ContentRuleActionEntry.TableName,
                type: barkIdColumnType,
                nullable: false,
                defaultValue: 0u);

            // ── content_area ──
            // {"id": 198604} McAllister's earshot: inferred (the trigger); the radius is the client's own 20 m.
            migrationBuilder.InsertData(
                table: "content_area",
                columns: new[] { "id", "map_context_id", "shape", "pos_x", "pos_y", "pos_z", "radius", "half_height", "comment" },
                values: new object[]
                {
                    EarshotArea, Bootcamp, Sphere, McAllisterX, McAllisterY, McAllisterZ, EarshotRadius, 0.0,
                    "McAllister earshot (inferred)"
                });

            // ── content_condition ──
            // {"condition_id": 198913} the holograms are behind the recruit and 1990 is not yet turned in: inferred.
            migrationBuilder.InsertData(
                table: "content_condition",
                columns: new[] { "condition_id", "or_group", "term_index", "kind", "mission_id", "objective_id", "state", "fact_key", "value", "negate" },
                values: new object[,]
                {
                    { ReturnedCondition, (byte)0, (byte)0, ObjectiveStateIs, Mission1990, HologramObjective, ObjectiveCompleted, "", 0, false },
                    { ReturnedCondition, (byte)0, (byte)1, MissionStateIs, Mission1990, 0u, MissionActive, "", 0, false }
                });

            // ── content_rule ──
            // {"id": 1985016} back in earshot after the holograms -> McAllister barks: inferred (event and area).
            migrationBuilder.InsertData(
                table: "content_rule",
                columns: new[] { "id", "map_context_id", "event", "mission_id", "objective_id", "area_id", "placement_id", "state_id", "condition_id", "comment" },
                values: new object[]
                {
                    BarkRule, Bootcamp, AreaEntered, 0u, 0u, EarshotArea, 0u, 0u, ReturnedCondition,
                    "back in earshot after 1990 -> McAllister barks"
                });

            // ── content_rule_action ──
            // {"rule_id": 1985016, "sequence": 0} McAllister says bark 852: original line and speaker identity,
            // inferred binding to placement 198650.
            migrationBuilder.InsertData(
                table: "content_rule_action",
                columns: new[]
                {
                    "rule_id", "sequence", "action", "mission_id", "forced", "greeting_id", "npc_name_id", "tutorial_id",
                    "logos_id", "logos_protocol", "experience", "credits", "item_set_id", "placement_id", "state_id",
                    "fact_key", "fact_value", "location_id", "audio_set_id", BarkIdColumn, "comment"
                },
                values: new object[]
                {
                    BarkRule, (byte)0, PlayBark, 0u, false, 0u, 0u, 0u, 0u, (byte)0, 0u, 0, 0u, McAllisterPlacement, 0u,
                    "", 0, 0u, 0u, McAllisterBark, "McAllister bark 852"
                });
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "content_rule_action",
                keyColumns: new[] { "rule_id", "sequence" },
                keyValues: new object[] { BarkRule, (byte)0 });

            migrationBuilder.DeleteData(
                table: "content_rule",
                keyColumns: new[] { "id" },
                keyValues: new object[] { BarkRule });

            migrationBuilder.DeleteData(
                table: "content_condition",
                keyColumns: new[] { "condition_id", "or_group", "term_index" },
                keyValues: new object[,] { { ReturnedCondition, (byte)0, (byte)0 }, { ReturnedCondition, (byte)0, (byte)1 } });

            migrationBuilder.DeleteData(
                table: "content_area",
                keyColumns: new[] { "id" },
                keyValues: new object[] { EarshotArea });

            migrationBuilder.DropColumn(name: BarkIdColumn, table: ContentRuleActionEntry.TableName);
        }
    }
}
