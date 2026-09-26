using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// The eleven "Logos:" missions the 2026-09-26 segment-3 audit found unseeded (SEG3-LOGOS-MISSIONS-AND-SKIP):
    /// 1633 Attack, 1634 Target, 1635 Here, 1638 Enhance, 1639 Power, 1640 Area, 1643 Backward, 1644 Defend,
    /// 1646 Give, 1647 Increase and 1652 Ground. Each asks for the Logos held by one shrine this world already places,
    /// and each is seeded the way SeedLogosMissions seeded the other 23: one objective bound by LogosRecovered (8) to a
    /// row of the world's own `logos` table, the Liaison who gives it also takes it back, no reward item.
    ///
    /// <b>The givers are four Liaisons, not one.</b> The audit listed all eleven under Receptive Liaison Langerman.
    /// TaRapedia's own pages, in every revision up to the post-D11 ones of 2008-09-16/25, and its NPC pages give four
    /// givers, and the shrines agree with them map for map:
    ///   * Langerman (world-seed creature 133, Alia Das, map 1220): 1633 Attack, 1638 Enhance, 1639 Power, 1640 Area.
    ///     1638's own client opening says "I am Receptive Liaison Langerman".
    ///   * Standley (world-seed creature 134, Twin Pillars, map 1220): 1634 Target, 1635 Here.
    ///   * Noonan (content NPC 199004, Foreas Base, Divide 1148): 1643 Backward, 1644 Defend, 1646 Give, 1647 Increase.
    ///   * Arizpe (content NPC 199085, Cumbria Research Facility, Palisades 1244): 1652 Ground.
    /// Langerman's other four missions of TaRapedia's list (907 Damage, 909 Time, 911 Mind, 921 Projectile) and
    /// Standley's 908/912/923/924/960 are not in this batch's list; they are recorded, unseeded, in the manifest gaps.
    ///
    /// <b>Where each value comes from</b> (docs/evidence/bootcamp-d11-reconstruction-manifest.json, migration
    /// LiaisonLogosMissions, and docs/evidence/liaison-logos-missions.json):
    ///   * Mission, objective id and text: the 1.16.5.0 client (original). The rows exist in npc_mission_objective from
    ///     MissionClientObjectiveSkeleton with NULL flags; they are replaced with the flags set and the client's text
    ///     kept byte for byte, and DeleteData puts the NULL-flag rows back. None of the eleven has an
    ///     objectiveconversation row, so no redirect line can complete anything.
    ///   * Shrine: the world's `logos` row whose id is the client's own logosstone constant for that word (ATTACK 2,
    ///     TARGET 49, HERE 53, ENHANCE 10, POWER 23, AREA 1, BACKWARD 3, DEFEND 7, GIVE 15, INCREASE 18, GROUND 46),
    ///     on the giver's own map context. Original ids; the binding itself is inferred.
    ///   * Giver and receiver: inferred from TaRapedia (above) and the client text ("Return when you have acquired the
    ///     Logos ..."); one Liaison both gives and takes back, as in SeedLogosMissions.
    ///   * Experience and credits: TaRapedia's recorded amounts, inferred. Each was first written in Oct-Nov 2007 and
    ///     carried unchanged into the pages' latest revisions (2008-09-16/25, after D11, formatting edits only), so the
    ///     reading itself is pre-Update-1.4. A credit amount is seeded only where no other source contradicts it:
    ///     Ellatha's early-2008 database gives different credits for Attack (800), Target (900), Enhance (500), Power
    ///     (500) and Area (600), and agrees only for Here (900). The Divide pages carry 200 credits entered at page
    ///     creation by the editor whose creation-time credits on the six checkable Liaison pages were each contradicted
    ///     later, so they are left out as weak. Five pages record no experience ("?"). What is left out is a GAP.
    ///   * Reward items: none. The shrine grants the Logos itself (GAP-MISSION-REWARD-ITEMS, reward-items review).
    ///   * Prerequisites: 1639 and 1640 wait on 1069 Receptive Reception, TaRapedia's Requirement since rev 29585 /
    ///     29583 (2008-03-07) and still in the post-D11 revisions; 1069 is seeded. No other page names one.
    ///   * Level: no source gives one (GAP-MISSION-LEVEL). As SeedLogosMissions did, each takes its giver's level, an
    ///     analogue under OD-100: Langerman 15 as the final-era footage shows him (B3-024; his creature row still says
    ///     10), Standley 10, Noonan 10 and Arizpe 25 from their creature rows.
    ///   * Group type, category, share and radio flags: the values every reconstructed mission carries.
    ///
    /// <b>Dependency.</b> Langerman (133) stands on the Redshirt class 29423, which the client cannot converse with.
    /// MissingMissionGivers (20260926150000, the missing-npcs batch) moves him to an NPC swapset class for 1741; this
    /// migration does not touch creature 133 and relies on that one running first. Until it does, his four missions
    /// load and are offerable server-side but the client cannot open his dialogue.
    /// </summary>
    public static class LiaisonLogosMissionsRows
    {
        public const string Migration = "LiaisonLogosMissions";

        public const uint Langerman = 133u, Standley = 134u, Noonan = 199004u, Arizpe = 199085u;
        public const uint ReceptiveReception = 1069u;

        private const uint GeneralCategory = 10000001u;
        private const byte Credits = 1;
        private const byte Experience = 3;
        private const byte Completed = 4;

        /// <summary>ObjectiveBindingKind.LogosRecovered; placement_id carries a `logos` row id.</summary>
        private const byte LogosRecoveredBinding = 8;
        private const byte NoCounter = 255;

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
            "destroying_hit_only", "equip_match", "item_template_id", "item_set_id", "target_state", "counter_id", "comment"
        };
        private static readonly string[] BindingTypes =
        {
            "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER",
            "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "varchar(50)"
        };
        private static readonly string[] RewardColumns = { "id", "type", "credits", "item_template_id", "quantity" };
        private static readonly string[] RewardKeyColumns = { "id", "type", "item_template_id" };
        private static readonly string[] PrerequisiteColumns = { "mission_id", "or_group", "required_mission_id", "required_state", "comment" };
        private static readonly string[] PrerequisiteKeyColumns = { "mission_id", "or_group", "required_mission_id" };

        /// <summary>(mission, the Liaison who gives and receives it, level (analogue, OD-100), comment).</summary>
        public static readonly (uint Id, uint Liaison, uint Level, string Comment)[] Missions =
        {
            (1633u, Langerman, 15u, "Logos: Attack (Wilderness)"),
            (1638u, Langerman, 15u, "Logos: Enhance (Wilderness)"),
            (1639u, Langerman, 15u, "Logos: Power (Wilderness)"),
            (1640u, Langerman, 15u, "Logos: Area (Wilderness)"),
            (1634u, Standley, 10u, "Logos: Target (Wilderness)"),
            (1635u, Standley, 10u, "Logos: Here (Wilderness)"),
            (1643u, Noonan, 10u, "Logos: Backward (Divide)"),
            (1644u, Noonan, 10u, "Logos: Defend (Divide)"),
            (1646u, Noonan, 10u, "Logos: Give (Divide)"),
            (1647u, Noonan, 10u, "Logos: Increase (Divide)"),
            (1652u, Arizpe, 25u, "Logos: Ground (Palisades)")
        };

        /// <summary>
        /// (mission, client objective id, the `logos` row of the shrine, its word, the client's objective text).
        /// One objective each: ordinal 1, required, revealed on acceptance (inferred, as in SeedLogosMissions).
        /// </summary>
        public static readonly (uint Mission, uint Objective, uint Logos, string Name, string Text)[] Objectives =
        {
            (1633u, 3u, 2u, "Attack", "Acquire Logos Information: Attack"),
            (1638u, 2u, 10u, "Enhance", "Acquire Logos Information: Enhance"),
            (1639u, 6u, 23u, "Power", "Acquire Logos Information: Power"),
            (1640u, 4u, 1u, "Area", "Acquire Logos Information: Area"),
            (1634u, 4u, 49u, "Target", "Acquire Logos Information: Target"),
            (1635u, 2u, 53u, "Here", "Acquire Logos Information: Here"),
            (1643u, 7u, 3u, "Backward", "Acquire Logos Information: Backward"),
            (1644u, 8u, 7u, "Defend", "Acquire Logos Information: Defend"),
            (1646u, 10u, 15u, "Give", "Acquire Logos Information: Give"),
            (1647u, 11u, 18u, "Increase", "Acquire Logos Information: Increase"),
            (1652u, 16u, 46u, "Ground", "Acquire Logos Information: Ground")
        };

        /// <summary>
        /// TaRapedia's amounts that survive reconciliation (the amount lives in `credits` for either type). Missing on
        /// purpose: the contradicted credits of 1633, 1634, 1638, 1639 and 1640, the weak Divide credits, and the
        /// experience no page records for 1643, 1644, 1646, 1647 and 1652.
        /// </summary>
        public static readonly (uint Mission, byte Type, int Amount)[] Rewards =
        {
            (1633u, Experience, 4000),
            (1638u, Experience, 2500),
            (1639u, Experience, 2500),
            (1640u, Experience, 3000),
            (1634u, Experience, 4500),
            (1635u, Experience, 4500),
            (1635u, Credits, 900),
            (1652u, Credits, 1800)
        };

        /// <summary>TaRapedia's Requirement=[[Receptive Reception]] (Dashiva "Fix req", 2008-03-07; kept after D11).</summary>
        public static readonly (uint Mission, uint Required, string Comment)[] Prerequisites =
        {
            (1639u, ReceptiveReception, "Receptive Reception completed"),
            (1640u, ReceptiveReception, "Receptive Reception completed")
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            // group_type 1, general category, not shareable, not radio-completable: the reconstructed-mission values.
            InsertTyped(migrationBuilder, "npc_mission", MissionColumns, MissionTypes,
                Rows(Missions.Select(m => new object[] { m.Id, m.Liaison, m.Liaison, m.Level, 1u, GeneralCategory, false, false, m.Comment })));

            // The client skeleton rows are replaced by the same rows with the three flags set.
            DeleteTyped(migrationBuilder, "npc_mission_objective", new[] { "mission_id", "objective_id" }, new[] { "INTEGER", "INTEGER" },
                Rows(Objectives.Select(o => new object[] { o.Mission, o.Objective })));
            InsertTyped(migrationBuilder, "npc_mission_objective", ObjectiveColumns, ObjectiveTypes,
                Rows(Objectives.Select(o => new object[] { o.Mission, o.Objective, o.Text, true, 1u, true })));

            // One shrine binding per objective; no transition rows, since every objective is revealed on acceptance.
            InsertTyped(migrationBuilder, "npc_mission_objective_binding", BindingColumns, BindingTypes,
                Rows(Objectives.Select(o => new object[]
                {
                    o.Mission, o.Objective, (byte)0, LogosRecoveredBinding, 0u, o.Logos, 0u, 0u, false, (byte)0, 0u, 0u, 0u,
                    NoCounter, $"{o.Mission}/{o.Objective} the {o.Name} shrine"
                })));

            InsertTyped(migrationBuilder, "npc_mission_reward", RewardColumns, new[] { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" },
                Rows(Rewards.Select(r => new object[] { r.Mission, r.Type, r.Amount, 0u, 0u })));

            InsertTyped(migrationBuilder, "npc_mission_prerequisite", PrerequisiteColumns, new[] { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "varchar(50)" },
                Rows(Prerequisites.Select(p => new object[] { p.Mission, (byte)0, p.Required, Completed, p.Comment })));
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            DeleteTyped(migrationBuilder, "npc_mission_prerequisite", PrerequisiteKeyColumns, new[] { "INTEGER", "INTEGER", "INTEGER" },
                Rows(Prerequisites.Select(p => new object[] { p.Mission, (byte)0, p.Required })));

            DeleteTyped(migrationBuilder, "npc_mission_reward", RewardKeyColumns, new[] { "INTEGER", "INTEGER", "INTEGER" },
                Rows(Rewards.Select(r => new object[] { r.Mission, r.Type, 0u })));

            DeleteTyped(migrationBuilder, "npc_mission_objective_binding", new[] { "mission_id", "objective_id", "binding_id" }, new[] { "INTEGER", "INTEGER", "INTEGER" },
                Rows(Objectives.Select(o => new object[] { o.Mission, o.Objective, (byte)0 })));

            // The client skeleton rows come back with their text and all three server flags unknown (NULL).
            DeleteTyped(migrationBuilder, "npc_mission_objective", new[] { "mission_id", "objective_id" }, new[] { "INTEGER", "INTEGER" },
                Rows(Objectives.Select(o => new object[] { o.Mission, o.Objective })));
            InsertTyped(migrationBuilder, "npc_mission_objective", ObjectiveColumns, ObjectiveTypes,
                Rows(Objectives.Select(o => new object[] { o.Mission, o.Objective, o.Text, null, null, null })));

            DeleteTyped(migrationBuilder, "npc_mission", new[] { "id" }, new[] { "INTEGER" },
                Rows(Missions.Select(m => new object[] { m.Id })));
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
