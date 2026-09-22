using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// The objectives that completed at the wrong NPC, the log entries that read in the wrong order, and four
    /// other wiring defects - from an audit of all 77 seeded missions against the client's own mission text.
    ///
    /// The live report that started this work ("commander rogers is giving me quests to go to outpost commander
    /// rogers") turned out to be one case of a pattern. Where a mission's objective is completed by talking to
    /// someone, this world binds that conversation to an NPC package, and in six missions the package sat on the
    /// wrong body - usually the giver's, so the mission asked the player to report back to the person who had
    /// just sent them out. The client says otherwise in each case, and in four of them the NPC it names is
    /// already here and already placed with no package of its own:
    ///
    ///   * <b>347</b> Field Test: package 67 off Shaman Horea onto <b>Base Guard Kapler</b> (199010).
    ///   * <b>640</b>: package 1095 off Colonel Whitaker onto the <b>Operations Mainframe</b> (199511).
    ///   * <b>940</b>: package 1062 off Sgt. Jeansonne onto <b>Sgt. Ricardo</b> (199412).
    ///   * <b>1063</b>: package 1119 off Xenori onto <b>Lieutenant Liu</b> (199510).
    ///
    /// A placement's npc_package_id wins over an npc_package row, so each of those moves both, and the stale
    /// npc_package rows go with them. Two more packages were duplicated rather than misplaced: 732 sat on Field
    /// Dr. Dawson as well as Agent Franz, who is the one mission 796 names, and npc_package 510085 repeated the
    /// Whitaker binding. npc_package 100 is a row on a duplicate Rogers creature carrying the "test" comment;
    /// the real Rogers (198514) already carries package 116 on his placement.
    ///
    /// <b>Objective order.</b> The mission log sorts by ordinal, so an ordinal that disagrees with the client's
    /// own conversation chain shows the player the steps out of order. Five missions did: 332, 682, 955, 969 and
    /// 1992. The first four are swaps of two neighbouring steps, each with its transition rewritten to match;
    /// 1992's ordinals contradicted its own transition chain (4 -> 1 -> 2 -> 5 -> 6 -> 3 -> 9 -> 8 -> 7) rather
    /// than any outside source, so three ordinals move to agree with the chain this world already carries.
    ///
    /// <b>1904</b> Gun Control Part IV was received by Retread Lou while its own log, objective and conversation
    /// package 950 all name Retread <b>Duvall</b> (199303). Its comment was a byte-for-byte copy of 1868's, so it
    /// now says which branch it is.
    ///
    /// <b>Rewards.</b> 429 paid nothing; TaRapedia gives it 1,000 credits and 10,000 XP, and every one of the 53
    /// XP figures and 55 credit figures this world already carries matches that source exactly, which is what
    /// makes it usable here. 1390 was missing its 2,000 XP the same way. Item rewards are not touched: the wiki
    /// names reward items by manufacturer ("Olympia Reflective Armor Vest") and no client table maps a
    /// manufacturer to a template, so 305 of 400 reward lines cannot be resolved to an item at all. See
    /// GAP-MISSION-REWARD-ITEMS.
    /// </summary>
    public static class QuestWiringFixesRows
    {
        public const string Migration = "QuestWiringFixes";

        /// <summary>(package, the placement it leaves, the placement it moves to, the npc_package row to drop).</summary>
        private static readonly (uint Package, uint From, uint To, uint StaleRow)[] Conversations =
        {
            (67u, 199001u, 199010u, 199001u),     // 347: Shaman Horea -> Base Guard Kapler
            (1095u, 199500u, 199511u, 199500u),   // 640: Colonel Whitaker -> the Operations Mainframe
            (1062u, 199400u, 199412u, 199400u),   // 940: Sgt. Jeansonne -> Sgt. Ricardo
            (1119u, 199501u, 199510u, 199501u)    // 1063: Xenori -> Lieutenant Liu
        };

        /// <summary>npc_package rows that duplicate a binding another body already carries.</summary>
        private static readonly (uint Id, uint Package)[] Duplicates =
        {
            (510085u, 1095u),   // a second Whitaker row for the mainframe's package
            (100u, 116u)        // a duplicate Rogers creature, comment "test"; 198514 carries 116 on its placement
        };

        /// <summary>796: package 732 belongs to Agent Franz (199005), not to Field Dr. Dawson as well.</summary>
        public const uint Dawson = 199002u, DawsonPackage = 732u;

        /// <summary>(mission, objective, the ordinal it had, the ordinal it takes).</summary>
        private static readonly (uint Mission, uint Objective, uint Was, uint Now)[] Ordinals =
        {
            (332u, 2u, 2u, 3u), (332u, 3u, 3u, 2u),
            (682u, 4u, 3u, 4u), (682u, 5u, 4u, 3u),
            (955u, 7u, 1u, 2u), (955u, 8u, 2u, 1u),
            (969u, 1u, 1u, 2u), (969u, 3u, 2u, 1u),
            (1992u, 3u, 4u, 6u), (1992u, 5u, 5u, 4u), (1992u, 6u, 6u, 5u)
        };

        /// <summary>(mission, the transition it had, the transition it takes) - completed objective, revealed objective.</summary>
        private static readonly (uint Mission, uint WasCompleted, uint WasRevealed, uint NowCompleted, uint NowRevealed)[] Transitions =
        {
            (332u, 1u, 2u, 1u, 3u), (332u, 2u, 3u, 3u, 2u),
            (682u, 3u, 4u, 3u, 5u), (682u, 4u, 5u, 5u, 4u),
            (955u, 7u, 8u, 8u, 7u),
            (969u, 1u, 3u, 3u, 1u), (969u, 3u, 4u, 1u, 4u)
        };

        public const uint GunControlFour = 1904u, RetreadLou = 199302u, RetreadDuvall = 199303u;
        public const string GunControlFourWas = "Gun Control - Part IV (Marshes)";
        public const string GunControlFourNow = "Gun Control - Part IV, Duvall branch (Marshes)";

        /// <summary>(mission, reward type, amount). Type 1 is credits, 3 experience.</summary>
        private static readonly (uint Mission, uint Type, int Amount)[] Currency =
        {
            (429u, 1u, 1000), (429u, 3u, 10000), (1390u, 3u, 2000)
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var row in Conversations)
            {
                migrationBuilder.Sql($"update content_placement set npc_package_id = 0 where id = {row.From} and npc_package_id = {row.Package};");
                migrationBuilder.Sql($"update content_placement set npc_package_id = {row.Package} where id = {row.To} and npc_package_id = 0;");
                migrationBuilder.Sql($"delete from npc_package where id = {row.StaleRow} and package_id = {row.Package};");
            }

            foreach (var row in Duplicates)
                migrationBuilder.Sql($"delete from npc_package where id = {row.Id} and package_id = {row.Package};");

            migrationBuilder.Sql($"update content_placement set npc_package_id = 0 where id = {Dawson} and npc_package_id = {DawsonPackage};");
            migrationBuilder.Sql($"delete from npc_package where id = {Dawson} and package_id = {DawsonPackage};");

            foreach (var row in Ordinals)
                Ordinal(migrationBuilder, row.Mission, row.Objective, row.Now);

            foreach (var row in Transitions)
                Transition(migrationBuilder, row.Mission, row.WasCompleted, row.WasRevealed, row.NowCompleted, row.NowRevealed);

            migrationBuilder.Sql($"update npc_mission set reciver_id = {RetreadDuvall}, comment = '{GunControlFourNow}' where id = {GunControlFour};");

            foreach (var row in Currency)
                migrationBuilder.InsertData(table: "npc_mission_reward",
                    columns: new[] { "id", "type", "credits", "item_template_id", "quantity" },
                    values: new object[] { row.Mission, row.Type, row.Amount, 0u, 0u });
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var row in Currency)
                migrationBuilder.Sql($"delete from npc_mission_reward where id = {row.Mission} and type = {row.Type} and credits = {row.Amount};");

            migrationBuilder.Sql($"update npc_mission set reciver_id = {RetreadLou}, comment = '{GunControlFourWas}' where id = {GunControlFour};");

            foreach (var row in Transitions)
                Transition(migrationBuilder, row.Mission, row.NowCompleted, row.NowRevealed, row.WasCompleted, row.WasRevealed);

            foreach (var row in Ordinals)
                Ordinal(migrationBuilder, row.Mission, row.Objective, row.Was);

            migrationBuilder.Sql($"update content_placement set npc_package_id = {DawsonPackage} where id = {Dawson} and npc_package_id = 0;");
            migrationBuilder.InsertData(table: "npc_package", columns: new[] { "id", "package_id", "comment" },
                values: new object[] { Dawson, DawsonPackage, "restored by rollback" });

            foreach (var row in Duplicates)
                migrationBuilder.InsertData(table: "npc_package", columns: new[] { "id", "package_id", "comment" },
                    values: new object[] { row.Id, row.Package, "restored by rollback" });

            foreach (var row in Conversations)
            {
                migrationBuilder.InsertData(table: "npc_package", columns: new[] { "id", "package_id", "comment" },
                    values: new object[] { row.StaleRow, row.Package, "restored by rollback" });
                migrationBuilder.Sql($"update content_placement set npc_package_id = 0 where id = {row.To} and npc_package_id = {row.Package};");
                migrationBuilder.Sql($"update content_placement set npc_package_id = {row.Package} where id = {row.From} and npc_package_id = 0;");
            }
        }

        private static void Ordinal(MigrationBuilder migrationBuilder, uint mission, uint objective, uint ordinal)
            => migrationBuilder.Sql($"update npc_mission_objective set ordinal = {ordinal} where mission_id = {mission} and objective_id = {objective};");

        private static void Transition(MigrationBuilder migrationBuilder, uint mission, uint wasCompleted, uint wasRevealed,
            uint nowCompleted, uint nowRevealed)
            => migrationBuilder.Sql("update npc_mission_objective_transition set completed_objective_id = " +
                $"{nowCompleted}, revealed_objective_id = {nowRevealed} where mission_id = {mission} and " +
                $"completed_objective_id = {wasCompleted} and revealed_objective_id = {wasRevealed};");
    }
}
