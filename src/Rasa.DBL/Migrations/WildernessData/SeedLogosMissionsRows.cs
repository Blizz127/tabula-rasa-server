using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// The Logos mission set: 23 missions a Receptive Liaison hands out, each one asking for the Logos
    /// information held by a shrine this world already places.
    ///
    /// These are the whole class of unseeded missions that needed nothing built. Every one has a single
    /// completion route the server already implements - <c>ObjectiveBindingKind.LogosRecovered</c> (8) against
    /// a row of the world seed's own <c>logos</c> table, fired by DynamicObjectManager.LogosRecovery when the
    /// player activates the shrine - and every giver is also the receiver and is already a placed creature
    /// with a conversation package of its own. So nothing here invents an NPC, a placement, an area, a loot
    /// row or a counter.
    ///
    /// <b>Where each value comes from.</b>
    ///
    ///   * <b>Mission id, objective id and objective text: the client</b> (original tier). The rows already
    ///     exist in <c>npc_mission_objective</c> from MissionClientObjectiveSkeleton, read out of the retail
    ///     1.16.5.0 <c>generated/client/missionobjective.pyo</c>, and carry NULL ordinal, is_required and
    ///     revealed_on_accept. Each is deleted and re-inserted here with those three set and the client's own
    ///     comment kept byte for byte; DeleteData puts the NULL-flag row back.
    ///   * <b>Giver and receiver: this world.</b> Each mission's TaRapedia page names a Receptive Liaison, and
    ///     that creature is already placed: Brice 199003 and Noonan 199004 in the Divide (map 1148), Ridout
    ///     199405 on the Plateau (1497), Sage 199504 on the Torden Incline (1761), Repp 199505 on the Plains
    ///     (1764), Maddox 199600 in the Torden Mires (1759). The Liaison who gives the mission is also the one
    ///     who takes it back - that is what the client's own text says ("Return when you have acquired the
    ///     Logos ...") and there is no second speaker to bind.
    ///   * <b>The shrine: this world.</b> Each binding's placement_id is a <c>logos</c> row id, because a
    ///     shrine is a Logos dynamic object and not a content placement (see WildernessHubReceptiveReception,
    ///     which is mission 1069's binding of the same kind). Every shrine below sits on the same map context
    ///     as its Liaison, so no mission sends the player off its own map.
    ///   * <b>Credits and experience: TaRapedia</b> (community, dated, so inferred). The 1.16.5.0 client has
    ///     no reward table at all. These are the same source, and the same extraction pass, as the figures
    ///     QuestWiringFixes used for 429 and 1390, where all 53 XP and 55 credit values this world already
    ///     carried matched the wiki exactly.
    ///   * <b>Reward items: none.</b> Each page's only listed reward is the Logos itself, which the shrine
    ///     grants; no reward item is asserted, so the item-name gap (GAP-MISSION-REWARD-ITEMS) does not
    ///     touch this set.
    ///
    /// <b>Level is an analogue, not a finding.</b> The client has no mission-level field - missionconversation,
    /// missionobjective, missiongrouptype, campaign and the language tables were all checked and none carries
    /// one. TaRapedia states a level for exactly one mission of this set, 1659 (level 30), and that value is
    /// used. For the other 22 no source states a level, so each takes <b>the level of the Liaison who gives
    /// it</b>: Brice 20, Noonan 10, Ridout 38 (unused - 1659 has the wiki's 30), Sage 32, Repp 30, Maddox 35.
    /// That is a labelled analogue under OD-45 and not evidence: across the 17 seedable missions where both a
    /// wiki level and a giver level are known the two differ by up to 8 (median 1), and 1659 is itself such a
    /// case - Ridout is 38 and the page says 30. Nothing in the server gates on npc_mission.level; it is sent
    /// to the client for display in the mission log, so an analogue here is cosmetic and cannot strand a
    /// character. Noonan's 10 is the visible oddity of the batch: his two missions sit in the same Divide as
    /// Brice's level-20 ones. It is recorded as-is rather than smoothed, because smoothing it would be a guess.
    ///
    /// <b>Ordinals and the reveal rule are inferred</b>, as everywhere else: the client carries none. 22 of
    /// the 23 missions have one objective, which takes ordinal 1, required, revealed on acceptance. 1750
    /// "Logos: Clarity and Death" is the one with two (19 Clarity, 20 Death) and both are revealed on
    /// acceptance with ordinals 1 and 2, with no transition between them - the mission names both Logos and
    /// nothing orders the two shrines, so making one wait on the other would assert an order no source gives.
    ///
    /// <b>Each mission was checked against the loader before it was written here</b> (Mission.DefinitionGaps
    /// and MissionContentCatalog.Validate): every mission has at least one objective; every objective has an
    /// ordinal, a required flag and a revealed flag; every required objective is revealed on acceptance, so
    /// none is unreachable; every required objective carries a completion binding; every binding is of an
    /// implemented kind with a non-zero shrine id; and every reward row is a single positive currency row of
    /// its type. None of these missions has an objective conversation, a counter, a timer, an indicator, an
    /// area, a prerequisite or an item reward, so none of the rules governing those can withhold them.
    ///
    /// <b>Zone names below are the map context's</b>, not the file names of earlier batches: 199405 Ridout
    /// was seeded by MiresConversationNpc but stands on 1497, the Plateau, and 199600 Maddox by
    /// InclineConversationNpc but stands on 1759, the Mires. The map context ids are the authority.
    /// </summary>
    public static class SeedLogosMissionsRows
    {
        public const string Migration = "SeedLogosMissions";

        /// <summary>The Wilderness general category, as every other reconstructed mission uses.</summary>
        private const uint GeneralCategory = 10000001u;

        private const uint CreditsReward = 1u;
        private const uint ExperienceReward = 3u;

        /// <summary>ObjectiveBindingKind.LogosRecovered; placement_id carries a `logos` row id.</summary>
        private const byte LogosRecoveredBinding = 8;
        private const byte NoCounter = 255;

        private const uint Brice = 199003u, Noonan = 199004u, Ridout = 199405u,
            Sage = 199504u, Repp = 199505u, Maddox = 199600u;

        /// <summary>
        /// (mission, the Liaison who both gives and receives it, level, credits, experience, comment).
        /// Level is TaRapedia's for 1659 and the giver's own level - a labelled analogue - for the rest.
        /// Credits and experience are TaRapedia's throughout.
        /// </summary>
        private static readonly (uint Mission, uint Liaison, uint Level, uint Credits, uint Xp, string Comment)[] Missions =
        {
            (1645u, Brice, 20u, 1200u, 6000u, "Logos: Friend (Divide)"),
            (1648u, Brice, 20u, 1200u, 6000u, "Logos: Life (Divide)"),
            (1649u, Noonan, 10u, 200u, 7000u, "Logos: Negative (Divide)"),
            (1650u, Noonan, 10u, 200u, 7500u, "Logos: Summon (Divide)"),
            (1651u, Brice, 20u, 1200u, 6000u, "Logos: Transform (Divide)"),
            (1659u, Ridout, 30u, 4950u, 100000u, "Logos: Vortex (Plateau)"),
            (1661u, Sage, 32u, 2400u, 12000u, "Logos: Create (Incline)"),
            (1663u, Sage, 32u, 2500u, 12500u, "Logos: Hide (Incline)"),
            (1664u, Sage, 32u, 2500u, 12500u, "Logos: Near (Incline)"),
            (1665u, Sage, 32u, 2500u, 12500u, "Logos: Teleport (Incline)"),
            (1750u, Sage, 32u, 5200u, 54000u, "Logos: Clarity and Death (Incline)"),
            (1666u, Maddox, 35u, 2800u, 15500u, "Logos: Destruction (Mires)"),
            (1667u, Maddox, 35u, 2800u, 15500u, "Logos: Effect (Mires)"),
            (1668u, Maddox, 35u, 2800u, 15500u, "Logos: Location (Mires)"),
            (1669u, Maddox, 35u, 2800u, 15500u, "Logos: Many (Mires)"),
            (1670u, Maddox, 35u, 2800u, 15500u, "Logos: Return (Mires)"),
            (1671u, Maddox, 35u, 2800u, 15500u, "Logos: Spirit (Mires)"),
            (1654u, Repp, 30u, 2200u, 11000u, "Logos: Add (Plains)"),
            (1655u, Repp, 30u, 2200u, 11000u, "Logos: Container (Plains)"),
            (1657u, Repp, 30u, 2200u, 11000u, "Logos: Looking (Plains)"),
            (1658u, Repp, 30u, 2200u, 11000u, "Logos: Repair (Plains)"),
            (1749u, Repp, 30u, 2200u, 11000u, "Logos: Lightning (Plains)"),
            (1814u, Repp, 30u, 2200u, 11000u, "Logos: Weak (Plains)")
        };

        /// <summary>
        /// (mission, objective, ordinal, the `logos` row the shrine is, its name, the client's own objective
        /// text). Objective id and text are the client's; the ordinal is inferred; the logos row is this
        /// world's, and each one sits on its Liaison's map context.
        /// </summary>
        private static readonly (uint Mission, uint Objective, uint Ordinal, uint Logos, string Name, string Text)[] Objectives =
        {
            (1645u, 9u, 1u, 14u, "Friend", "Acquire Logos Information: Friend"),
            (1648u, 12u, 1u, 43u, "Life", "Acquire Logos Information: Life"),
            (1649u, 13u, 1u, 33u, "Negative", "Acquire Logos Information: Negative"),
            (1650u, 14u, 1u, 52u, "Summon", "Acquire Logos Information: Summon"),
            (1651u, 15u, 1u, 331u, "Transform", "Acquire Logos Information: Transform"),
            (1659u, 18u, 1u, 48u, "Vortex", "Acquire Logos Information: Vortex"),
            (1661u, 20u, 1u, 32u, "Create", "Acquire Logos Information: Create"),
            (1663u, 21u, 1u, 188u, "Hide", "Acquire Logos Information: Hide"),
            (1664u, 22u, 1u, 373u, "Near", "Acquire Logos Information: Near"),
            (1665u, 23u, 1u, 384u, "Teleport", "Acquire Logos Information: Teleport"),
            (1750u, 19u, 1u, 107u, "Clarity", "Acquire Logos Information: Clarity"),
            (1750u, 20u, 2u, 125u, "Death", "Acquire Logos Information: Death"),
            (1666u, 24u, 1u, 131u, "Destruction", "Acquire Logos Information: Destruction"),
            (1667u, 25u, 1u, 146u, "Effect", "Acquire Logos Information: Effect"),
            (1668u, 26u, 1u, 214u, "Location", "Acquire Logos Information: Location"),
            (1669u, 27u, 1u, 20u, "Many", "Acquire Logos Information: Many"),
            (1670u, 28u, 1u, 25u, "Return", "Acquire Logos Information: Return"),
            (1671u, 29u, 1u, 300u, "Spirit", "Acquire Logos Information: Spirit"),
            (1654u, 17u, 1u, 393u, "Add", "Acquire Logos Information: Add"),
            (1655u, 18u, 1u, 117u, "Container", "Acquire Logos Information: Container"),
            (1657u, 20u, 1u, 216u, "Looking", "Acquire Logos Information: Looking"),
            (1658u, 21u, 1u, 275u, "Repair", "Acquire Logos Information: Repair"),
            (1749u, 20u, 1u, 31u, "Lightning", "Acquire Logos Information: Lightning"),
            (1814u, 18u, 1u, 347u, "Weak", "Acquire Logos Information: Weak")
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            // ── npc_mission ──
            // group_type 1, shareable and radio_completeable false: the values every reconstructed mission
            // of this world carries, none of which the client records.
            var missions = new object[Missions.Length, 9];
            for (var i = 0; i < Missions.Length; i++)
            {
                missions[i, 0] = Missions[i].Mission;
                missions[i, 1] = Missions[i].Liaison;
                missions[i, 2] = Missions[i].Liaison;
                missions[i, 3] = Missions[i].Level;
                missions[i, 4] = (byte)1;
                missions[i, 5] = GeneralCategory;
                missions[i, 6] = false;
                missions[i, 7] = false;
                missions[i, 8] = Missions[i].Comment;
            }

            migrationBuilder.InsertData(
                table: "npc_mission",
                columns: new[] { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" },
                values: missions);

            // ── npc_mission_objective ──
            // The client skeleton holds each row with NULL ordinal, is_required and revealed_on_accept; it is
            // replaced by the same row with the three set. The comment stays the client's own text.
            foreach (var objective in Objectives)
                migrationBuilder.DeleteData(
                    table: "npc_mission_objective",
                    keyColumns: new[] { "mission_id", "objective_id" },
                    keyValues: new object[] { objective.Mission, objective.Objective });

            var objectives = new object[Objectives.Length, 6];
            for (var i = 0; i < Objectives.Length; i++)
            {
                objectives[i, 0] = Objectives[i].Mission;
                objectives[i, 1] = Objectives[i].Objective;
                objectives[i, 2] = Objectives[i].Text;
                objectives[i, 3] = true;
                objectives[i, 4] = Objectives[i].Ordinal;
                objectives[i, 5] = true;
            }

            migrationBuilder.InsertData(
                table: "npc_mission_objective",
                columns: new[] { "mission_id", "objective_id", "comment", "is_required", "ordinal", "revealed_on_accept" },
                values: objectives);

            // No npc_mission_objective_transition rows: every objective of this set is revealed on
            // acceptance, so nothing reveals anything else. 1750's two shrines are unordered by any source.

            // ── the shrine bindings ──
            var bindings = new object[Objectives.Length, 15];
            for (var i = 0; i < Objectives.Length; i++)
            {
                bindings[i, 0] = Objectives[i].Mission;
                bindings[i, 1] = Objectives[i].Objective;
                bindings[i, 2] = (byte)0;
                bindings[i, 3] = LogosRecoveredBinding;
                bindings[i, 4] = 0u;
                bindings[i, 5] = Objectives[i].Logos;
                bindings[i, 6] = 0u;
                bindings[i, 7] = 0u;
                bindings[i, 8] = false;
                bindings[i, 9] = (byte)0;
                bindings[i, 10] = 0u;
                bindings[i, 11] = 0u;
                bindings[i, 12] = 0u;
                bindings[i, 13] = NoCounter;
                bindings[i, 14] = $"{Objectives[i].Mission}/{Objectives[i].Objective} the {Objectives[i].Name} shrine";
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

            // ── rewards ──
            // Credits and experience only; the Logos itself is granted by the shrine, not by a reward row.
            var rewards = new object[Missions.Length * 2, 5];
            for (var i = 0; i < Missions.Length; i++)
            {
                rewards[i * 2, 0] = Missions[i].Mission;
                rewards[i * 2, 1] = ExperienceReward;
                rewards[i * 2, 2] = (int)Missions[i].Xp;
                rewards[i * 2, 3] = 0u;
                rewards[i * 2, 4] = 0u;

                rewards[i * 2 + 1, 0] = Missions[i].Mission;
                rewards[i * 2 + 1, 1] = CreditsReward;
                rewards[i * 2 + 1, 2] = (int)Missions[i].Credits;
                rewards[i * 2 + 1, 3] = 0u;
                rewards[i * 2 + 1, 4] = 0u;
            }

            migrationBuilder.InsertData(
                table: "npc_mission_reward",
                columns: new[] { "id", "type", "credits", "item_template_id", "quantity" },
                values: rewards);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            var rewardKeys = new object[Missions.Length * 2, 3];
            for (var i = 0; i < Missions.Length; i++)
            {
                rewardKeys[i * 2, 0] = Missions[i].Mission;
                rewardKeys[i * 2, 1] = ExperienceReward;
                rewardKeys[i * 2, 2] = 0u;

                rewardKeys[i * 2 + 1, 0] = Missions[i].Mission;
                rewardKeys[i * 2 + 1, 1] = CreditsReward;
                rewardKeys[i * 2 + 1, 2] = 0u;
            }

            migrationBuilder.DeleteData(
                table: "npc_mission_reward",
                keyColumns: new[] { "id", "type", "item_template_id" },
                keyValues: rewardKeys);

            var bindingKeys = new object[Objectives.Length, 3];
            for (var i = 0; i < Objectives.Length; i++)
            {
                bindingKeys[i, 0] = Objectives[i].Mission;
                bindingKeys[i, 1] = Objectives[i].Objective;
                bindingKeys[i, 2] = (byte)0;
            }

            migrationBuilder.DeleteData(
                table: "npc_mission_objective_binding",
                keyColumns: new[] { "mission_id", "objective_id", "binding_id" },
                keyValues: bindingKeys);

            // Put back the client skeleton rows, with the client's own comments and the three NULL flags.
            var objectiveKeys = new object[Objectives.Length, 2];
            for (var i = 0; i < Objectives.Length; i++)
            {
                objectiveKeys[i, 0] = Objectives[i].Mission;
                objectiveKeys[i, 1] = Objectives[i].Objective;
            }

            migrationBuilder.DeleteData(
                table: "npc_mission_objective",
                keyColumns: new[] { "mission_id", "objective_id" },
                keyValues: objectiveKeys);

            var skeleton = new object[Objectives.Length, 6];
            for (var i = 0; i < Objectives.Length; i++)
            {
                skeleton[i, 0] = Objectives[i].Mission;
                skeleton[i, 1] = Objectives[i].Objective;
                skeleton[i, 2] = Objectives[i].Text;
                skeleton[i, 3] = null;
                skeleton[i, 4] = null;
                skeleton[i, 5] = null;
            }

            migrationBuilder.InsertData(
                table: "npc_mission_objective",
                columns: new[] { "mission_id", "objective_id", "comment", "is_required", "ordinal", "revealed_on_accept" },
                values: skeleton);

            var missionKeys = new object[Missions.Length];
            for (var i = 0; i < Missions.Length; i++)
                missionKeys[i] = Missions[i].Mission;

            migrationBuilder.DeleteData(table: "npc_mission", keyColumn: "id", keyValues: missionKeys);
        }
    }
}
