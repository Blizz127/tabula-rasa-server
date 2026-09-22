using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// The two props that finish a mission, given a body that can be spoken to.
    ///
    /// The mission audit of 2026-09-21 found three missions a player can accept and never finish: the only
    /// completion route their required objective has is an <c>npc_mission_objective_conversation</c> naming a
    /// dialogue package that nothing in the world carries. Two of the three are not people at all, and this
    /// closes those two. The third, 451/3, is a villager at Dagdha's Urn whose speaker no source names, and the
    /// owner chose not to invent one; it stays in <c>MissionLinkAuditTests.UnboundPackages</c>.
    ///
    /// <b>Why they are creatures and not usables.</b> Both are props in the client, and the obvious shape for a
    /// prop here is a <c>content_placement</c> of kind 2. That shape cannot carry a conversation.
    /// <c>ContentMaterializer.SpawnUsable</c> builds a <c>DynamicObject</c> and never reads
    /// <c>npc_package_id</c>; <c>MissionManager.CompleteNpcObjective</c> resolves the entity the client names
    /// through <c>EntityManager.Creatures</c> and requires a non-null <c>Npc</c>, which a dynamic object never
    /// has; and <c>MissionContentCatalog</c> only counts <c>npc_package_id</c> as a used column on a creature
    /// placement, so a usable row carrying one is thrown out of <c>LivePlacements</c> as "unexpected
    /// npc_package_id" and never materializes at all. A usable can only complete an objective through a
    /// <c>use_completed</c> binding, and that route would lose the recorded line the client prints - which is
    /// the whole of what the player sees happen. So both take the shape the world already uses for a talking
    /// machine: a creature on a class that carries the client's Creature and NPC augmentations both, with an
    /// <c>npc_package</c> row and the package repeated on the placement.
    ///
    /// <b>Mission 442 objective 2, "Analyze the Blood Sample" - the Blood Analyzation Terminal.</b> The client
    /// names this one itself: <c>usablenameoverride</c> 73 is <c>WILDERNESS_TP_BLOOD_ANALYZER</c> and
    /// <c>usablenameoverridelanguage</c> 73 is "Blood Analyzation Terminal" (WILDERNESS_TP = Wilderness / Twin
    /// Pillars). Package 1486 appears exactly once in the whole client <c>objectiveconversation</c> table,
    /// (442,2,1486,1,1) -> missiontext 10573 "ANALYZING... ANALYZATION COMPLETE.", which is a machine's readout
    /// and not a person's line. Objective 1 of the same mission is Medical Assistant Duncan pointing at it:
    /// "see that analyzer over there? It's pretty easy to use. Knock yourself out." (missiontext 1524).
    ///
    /// Its position is the client's own "Twin Pillars Hospital" marker, <c>uimapmarker</c> text 229 at
    /// (-125.89, 220.75, -468.62) on marker context 1378, which is this world's map context 1220 - the same
    /// marker table's "Pinhole Falls Caverns" is the exact position of content placement 199910. It is
    /// corroborated from the other side: Duncan himself stands 2.7 m away at (-127.996, 220.762, -466.996)
    /// (spawnpool 203), which is what "over there" means and puts the terminal in his room.
    ///
    /// <b>Mission 1186 objective 1, "Find Eloh Artifacts" - the Eloh obelisk in the Kardash Atta Colony.</b>
    /// Package 1300's own text, missiontext 10665, ends "The obelisk is too large to move on your own", so the
    /// thing the player addresses is the obelisk. <b>Its position is inferred, not sourced.</b> The client has
    /// the string "Obelisk" as <c>uimapmarkertextlanguage</c> 410, but no <c>uimapmarker</c> row references it,
    /// so it gives no coordinate. The nearest thing to a source is marker text 889 "Eloh Retreat" at
    /// (-111.49, 191.83, 322.62) on context 1773 (adv_arieki_torden_plains_attacolony, the id this world uses
    /// as well), the only Eloh-named place inside the colony; the audit rated it medium confidence. If a better
    /// source for the obelisk ever turns up, this row moves.
    ///
    /// <b>The classes are analogues, on the terms of OD-45 and of <see cref="TarapediaMachineClassRows"/>.</b>
    /// The client has a class for each prop - <c>UsableTwoStateBloodAnalyzer</c> (9742) and
    /// <c>NPCElohObeliskV01</c> (30138), and the second is literally an obelisk carrying the NPC augmentation -
    /// but neither carries the Creature augmentation, which is what <c>CreatureManager.CreateFromTemplate</c>
    /// needs to build an actor. That is the failure TarapediaMachineClass already fixed once: "Creature with
    /// dbId = 199512, don't have creature Augmentation". So the terminal takes NPC_Hominis_Machina (10642), the
    /// world's own talking machine and the class the Operations Mainframe and the Computer Access Terminal
    /// already stand on, and the obelisk takes NPC_Eloh_Holgram_Large (24883) - the Eloh appear as holograms
    /// (which is why Jumna is on NPC_Holographic_Eloh), 10665 has the player see "the image of an Eloh,
    /// beckoning you", and "Large" is the one that reads as an obelisk rather than a person.
    ///
    /// Names are the closest the client's creature name table has, because neither prop's real name is in it:
    /// 10169 "Computer Terminal" (the Research Outpost Alpha terminal's own name) and 10598 "An Ancient Eloh".
    /// "Blood Analyzation Terminal" lives only in the usable name override table, which a creature's
    /// <c>name_id</c> cannot reach - <c>CreatureInfoPacket</c> looks its id up in <c>creaturename</c>. Levels
    /// are the room's: 10 for the terminal, which is Duncan's, and 35 for the obelisk, which is mission 1186's
    /// and the level of the colony's own Computer Access Terminal. Health and speeds are the world seed's
    /// figures for a quest NPC (555 hp, run 9, walk 5); both stand still.
    /// </summary>
    /// <summary>
    /// Both Y values are the navmesh floor under the marker's own x,z, probed 2026-09-21: the terminal at
    /// 220.642 against a marker height of 220.75, and the obelisk at 191.75 against 191.83. The obelisk's was
    /// the one in doubt - the only other placement on that map, the Computer Access Terminal, stands at 272.35,
    /// 80 m above - and the probe settles it: the colony has walkable ground at the marker and none at all at
    /// the terminal's height, so the two are simply on different levels.
    /// </summary>
    public static class MissionPropSpeakersRows
    {
        public const string Migration = "MissionPropSpeakers";

        /// <summary>The Blood Analyzation Terminal in the Twin Pillars hospital (mission 442 objective 2).</summary>
        public const uint BloodAnalyzer = 199912u;

        /// <summary>The Eloh obelisk in the Kardash Atta Colony (mission 1186 objective 1).</summary>
        public const uint ElohObelisk = 199913u;

        /// <summary>The client's completion package for 442/2 - the analyser's readout, missiontext 10573.</summary>
        public const uint BloodAnalyzerPackage = 1486u;

        /// <summary>The client's completion package for 1186/1 - the obelisk's vision, missiontext 10665.</summary>
        public const uint ElohObeliskPackage = 1300u;

        /// <summary>NPC_Hominis_Machina: the talking machine of TarapediaMachineClass.</summary>
        private const uint TalkingMachine = 10642u;

        /// <summary>NPC_Eloh_Holgram_Large: an Eloh large enough to stand for the obelisk.</summary>
        private const uint LargeElohHologram = 24883u;

        private const uint Wilderness = 1220u;
        private const uint AttaColony = 1773u;

        private const byte CreaturePlacement = 1;
        private const byte Stationary = 1;

        /// <summary>
        /// (id, comment, class, name id, level, map, x, y, z, package, the package row's comment, the
        /// placement's comment). The Y of each row is the marker's own height and still has to be seated on the
        /// navmesh floor of that map.
        /// </summary>
        private static readonly object[][] Props =
        {
            new object[]
            {
                BloodAnalyzer, "Blood Analyzation Terminal (Twin Pillars)", TalkingMachine, 10169u, 10u,
                Wilderness, -125.89, 220.642, -468.62, BloodAnalyzerPackage,
                "Blood Analyzation Terminal (client package 1486)",
                "Blood Analyzation Terminal (client marker 229)"
            },
            new object[]
            {
                ElohObelisk, "Eloh obelisk (Kardash Atta Colony)", LargeElohHologram, 10598u, 35u,
                AttaColony, -111.49, 191.75, 322.62, ElohObeliskPackage,
                "Eloh obelisk (client package 1300)",
                "Eloh obelisk (Eloh Retreat marker 889, inferred)"
            }
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var prop in Props)
            {
                var id = (uint)prop[0];
                var package = (uint)prop[9];

                migrationBuilder.InsertData(
                    table: "creature",
                    columns: new[] { "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed",
                        "walk_speed", "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8" },
                    values: new object[] { id, prop[1], prop[2], 1u, prop[4], 555u, prop[3], 9u, 5u,
                        0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u });

                migrationBuilder.InsertData(
                    table: "npc_package",
                    columns: new[] { "id", "package_id", "comment" },
                    values: new object[] { id, package, prop[10] });

                migrationBuilder.InsertData(
                    table: "content_placement",
                    columns: new[] { "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id",
                        "usable_kind", "pos_x", "pos_y", "pos_z", "rotation", "behavior", "initial_state", "alternate_state",
                        "alternate_state_condition_id", "windup_ms", "name_override_id", "hit_points", "restore_ms",
                        "fuse_ms", "loot_item_set_id", "respawn_ms", "present_condition_id", "usable_condition_id", "comment" },
                    values: new object[] { id, prop[5], CreaturePlacement, id, package, 0u, (byte)0,
                        prop[6], prop[7], prop[8], 0.0, Stationary, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, prop[11] });
            }
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var prop in Props)
            {
                var id = (uint)prop[0];

                migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { id });
                migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { id });
                migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { id });
            }
        }
    }
}
