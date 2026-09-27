using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// The ordinary creatures of the Divide (1148) and Concordia Palisades (1244), and the eight missions that were held only
    /// because those creatures were missing (owner decision OD-161, 2026-09-27: footage first, then text, then labelled
    /// stand-ins).
    ///
    /// <b>Why.</b> Before this the only ambient population in the world seed was the Wilderness's. Every Divide and Palisades
    /// pool held a named NPC, a vendor or an upstream boss, so every kill or creature-drop objective on the two zones could
    /// be accepted but not finished (GAP-DIVIDE-AMBIENT-POPULATION, GAP-PALISADES-AMBIENT-POPULATION).
    ///
    /// <b>Evidence</b> (every field's tier and citation: docs/evidence/bootcamp-d11-reconstruction-manifest.json, migration
    /// ConcordiaAmbientPopulations; readings, probes and sources: docs/evidence/concordia-ambient-populations-20260927.json).
    ///   * Species. The final client's Targets of Opportunity name the zones' own: Divide 1582 "Kill 40 Xanx", "Kill 200 Thrax
    ///     soldiers", "20 Hominis Machina", "50 Amoeboids"; Palisades 1809 (the D12 version) "Kill 200 Fithik", "Kill 40 Hunters",
    ///     with 1630's "Kill 5 Stalkers" dropped. The mission logs and objectives name the rest (Filchers, Caretakers, Warnets,
    ///     the Class IV Stalker, Cumbria Boargar, the Uherum Fithik).
    ///   * Footage. Two Divide recordings (zuMHH5CxRj0, FireEagleEyes, uploaded 2007-12-24, read from its cited stills;
    ///     3nZ7eca_vRA, Lankist, 2007-11-09) show Xanx Venomspitters, Thrax Infantry Privates and Privates First Class, a Bane
    ///     dropship landing Thrax on the battlefield and groups of three Thrax at a Bane base. Rank names and species are read
    ///     from them; no level is legible at 480x360, and neither shows where on the map a fight is. No Palisades footage
    ///     after D12 is cached (the Raisuly series is listed for download).
    ///   * Places. TaRapedia readings (dated revisions), the BradyGames guide (2007-10), Queen Stazzle's nest sightings and the
    ///     final client map: the Xanx cave (cavern entrance and crystal room), six Xanx nests, the Uherum Pass tunnel pieces,
    ///     the Eloh obelisk north-west of the Cumbria ruins, the zones' own map labels. Heights are the navmesh floor minus
    ///     0.276 m and every pool is reached from the zone's main waypoint.
    ///   * Levels, health, attacks, most counts and the respawn are labelled analogues (OD-162, OD-163, OD-164).
    ///
    /// <b>Seeded.</b> Twelve creature rows (1148001-1148007, 1244001-1244005), 22 spawn pools (1148200-1148213, 1244200-1244207)
    /// and the Uherum Pass exit area 1244500. Missions 358 Filcher Frenzy, 371 Dissections: Part II, 372 Dissections: Part III,
    /// 774 The Tallest and 755 Careless on the Divide; 342 Noise Pollution and 1808 Clear Your Uherum on Palisades; 368
    /// Searching for Acceptance gets its Warnet kill count (its conversation row on Kogari is withheld in
    /// MissionRedirectConversations). Medical Officer Mayes (199052) gets client package 172. Every amount is a TaRapedia
    /// reading first recorded before Update 1.4 and labelled so; no item reward is seeded (all lists are pre-1.4).
    ///
    /// <b>Held.</b> Hominis Machina and Amoeboid pools (no place recorded), Predators (no final-era evidence), 370 Dissections
    /// and the other missions whose givers are not in the world, 368's five-minute timer (a timed failure needs a retry
    /// path), 1808's Start/Middle/End triggers (optional and unrevealed), every Palisades mission with another blocker.
    /// </summary>
    public static class ConcordiaAmbientPopulationsRows
    {
        public const string Migration = "ConcordiaAmbientPopulations";

        public const uint Divide = 1148u, Palisades = 1244u;

        // ── creatures ──
        public const uint XanxVenomspitter = 1148001u, ThraxPrivate = 1148002u, ThraxPrivateFirstClass = 1148003u, Caretaker = 1148004u,
            Filcher = 1148005u, DivideWarnet = 1148006u, ClassFourStalker = 1148007u;
        public const uint CumbriaBoargar = 1244001u, PalisadesWarnet = 1244002u, Fithik = 1244003u, Hunter = 1244004u, ThraxTechnician = 1244005u;

        // ── pools ──
        public const uint XanxCaveA = 1148200u, XanxCaveB = 1148201u, XanxSouthOfBase = 1148202u, XanxNests = 1148203u, FrontLines = 1148204u,
            BaneForwardCommand = 1148205u, HydroPlant = 1148206u, FilcherCentralTrench = 1148207u, FilcherWesternTrench = 1148208u,
            FilcherCrossroads = 1148209u, WarnetNestNorth = 1148210u, WarnetNestSouth = 1148211u, StalkerFoxtrotBridge = 1148212u,
            StalkerForwardBase = 1148213u;
        public const uint ObeliskBoargar = 1244200u, HightowerValley = 1244201u, UherumMiddle = 1244202u, UherumBend = 1244203u,
            UherumEast = 1244204u, FithikTrench = 1244205u, SkiveBase = 1244206u, ClearcutField = 1244207u;

        public const uint UherumExit = 1244500u;

        // ── NPCs, bosses and items the missions use ──
        public const uint Sebastian = 199000u, Kerr = 510116u, Mayes = 199052u, Sherman = 510103u, Kibner = 510102u,
            YormaBrown = 199088u, Tayros = 199110u, RottingSal = 520017u, Shahrbaraz = 520044u;
        public const uint MayesPackage = 172u;
        public const uint XanxSkinSample = 435u, HominisMachinaScraping = 436u, CaretakerFluid = 2526u, StalkerScraps = 2537u;

        public const uint FilcherFrenzy = 358u, DissectionsTwo = 371u, DissectionsThree = 372u, Careless = 755u, TheTallest = 774u,
            NoisePollution = 342u, ClearYourUherum = 1808u, SearchingForAcceptance = 368u;

        /// <summary>OD-164: the world seed's respawn (units of 100 ms after a pool's last creature dies), as OD-136.</summary>
        public const uint PoolRespawn = 20u;

        /// <summary>The Class IV Stalker "spawns ... every 15 minutes" (TaRapedia The Tallest rev 29384): 9,000 x 100 ms.</summary>
        public const uint StalkerRespawn = 9000u;

        private const uint GeneralCategory = 10000001u;
        private const byte Credits = 1, Experience = 3, Completed = 4;
        private const byte AreaEntered = 1, Kill = 6, ItemCollected = 9;
        private const byte NoCounter = 255;

        /// <summary>(id, comment, class, level, hp, name id, action1, action2); faction 0, run 9, walk 5.</summary>
        public static readonly (uint Id, string Comment, uint Class, uint Level, uint Hp, uint NameId, uint Action1, uint Action2)[] Creatures =
        {
            (XanxVenomspitter, "Xanx Venomspitter (Divide)", 7510u, 12u, 600u, 464u, 31u, 0u),
            (ThraxPrivate, "Thrax Infantry Private (Divide)", 29769u, 12u, 555u, 7676u, 33u, 0u),
            (ThraxPrivateFirstClass, "Thrax Infantry PFC (Divide)", 29769u, 12u, 555u, 7677u, 33u, 0u),
            (Caretaker, "Caretaker (Divide)", 9244u, 12u, 1000u, 0u, 16u, 0u),
            (Filcher, "Filcher (Divide)", 6421u, 12u, 555u, 0u, 21u, 0u),
            (DivideWarnet, "Warnet Soldier (Divide)", 6262u, 12u, 555u, 460u, 1u, 0u),
            (ClassFourStalker, "Class IV Stalker (Divide)", 3781u, 12u, 600u, 7693u, 2u, 0u),
            (CumbriaBoargar, "Boargar (Cumbria, Palisades)", 6031u, 15u, 555u, 0u, 4u, 0u),
            (PalisadesWarnet, "Warnet Soldier (Palisades)", 6262u, 15u, 555u, 460u, 1u, 0u),
            (Fithik, "Fithik (Palisades)", 4313u, 15u, 1000u, 0u, 2u, 0u),
            (Hunter, "Hunter (Palisades)", 10166u, 15u, 400u, 0u, 9u, 0u),
            (ThraxTechnician, "Thrax Technician (Palisades)", 7043u, 15u, 555u, 9106u, 9u, 0u)
        };

        /// <summary>(id, map, anim type (1 = Bane dropship), respawn, x, y, z, up to six (creature, count)); min = max.</summary>
        public static readonly (uint Id, uint Map, byte Anim, uint Respawn, double X, double Y, double Z, (uint Creature, byte Count)[] Slots)[] Pools =
        {
            // The Divide
            (XanxCaveA, Divide, 0, PoolRespawn, 725.0, 97.634, 0.0, new[] { (XanxVenomspitter, (byte)2) }),
            (XanxCaveB, Divide, 0, PoolRespawn, 740.0, 96.719, -20.0, new[] { (XanxVenomspitter, (byte)2) }),
            (XanxSouthOfBase, Divide, 0, PoolRespawn, -57.7, 99.976, 199.5, new[] { (XanxVenomspitter, (byte)2) }),
            (XanxNests, Divide, 0, PoolRespawn, 494.9, 76.832, 733.3, new[] { (XanxVenomspitter, (byte)2) }),
            (FrontLines, Divide, 1, PoolRespawn, -0.29, 69.575, -232.48, new[] { (ThraxPrivate, (byte)3), (Caretaker, (byte)1) }),
            (BaneForwardCommand, Divide, 0, PoolRespawn, 8.95, 115.641, -642.49, new[] { (ThraxPrivateFirstClass, (byte)3), (Caretaker, (byte)1) }),
            (HydroPlant, Divide, 0, PoolRespawn, -260.8, 57.943, 118.25, new[] { (ThraxPrivate, (byte)2), (Caretaker, (byte)2) }),
            (FilcherCentralTrench, Divide, 0, PoolRespawn, -121.87, 84.784, -355.63, new[] { (Filcher, (byte)3) }),
            (FilcherWesternTrench, Divide, 0, PoolRespawn, -365.13, 61.924, -202.95, new[] { (Filcher, (byte)3) }),
            (FilcherCrossroads, Divide, 0, PoolRespawn, 383.25, 116.336, 334.46, new[] { (Filcher, (byte)3) }),
            (WarnetNestNorth, Divide, 0, PoolRespawn, 689.0, 65.444, 248.8, new[] { (DivideWarnet, (byte)3) }),
            (WarnetNestSouth, Divide, 0, PoolRespawn, 706.0, 59.764, 212.5, new[] { (DivideWarnet, (byte)3) }),
            (StalkerFoxtrotBridge, Divide, 0, StalkerRespawn, 5.0, 52.393, -5.0, new[] { (ClassFourStalker, (byte)1) }),
            (StalkerForwardBase, Divide, 0, StalkerRespawn, -58.0, 82.473, -432.0, new[] { (ClassFourStalker, (byte)1) }),
            // Concordia Palisades
            (ObeliskBoargar, Palisades, 0, PoolRespawn, -602.3, 178.805, 824.6, new[] { (CumbriaBoargar, (byte)4) }),
            (HightowerValley, Palisades, 0, PoolRespawn, 60.0, 113.235, 50.0, new[] { (PalisadesWarnet, (byte)3) }),
            (UherumMiddle, Palisades, 0, PoolRespawn, 240.0, 139.413, -760.0, new[] { (Fithik, (byte)3) }),
            (UherumBend, Palisades, 0, PoolRespawn, 296.0, 140.592, -708.0, new[] { (Fithik, (byte)3) }),
            (UherumEast, Palisades, 0, PoolRespawn, 336.0, 130.923, -660.0, new[] { (Fithik, (byte)3) }),
            (FithikTrench, Palisades, 0, PoolRespawn, 233.83, 81.482, -297.72, new[] { (Fithik, (byte)3) }),
            (SkiveBase, Palisades, 0, PoolRespawn, 668.38, 132.714, -689.2, new[] { (Hunter, (byte)2) }),
            (ClearcutField, Palisades, 0, PoolRespawn, 411.0, 122.37, -554.0, new[] { (ThraxTechnician, (byte)2) })
        };

        /// <summary>(id, map, x, y, z, radius, half height, comment): a vertical cylinder at Uherum Pass's east mouth.</summary>
        public static readonly (uint Id, uint Map, double X, double Y, double Z, double Radius, double HalfHeight, string Comment)[] Areas =
        {
            (UherumExit, Palisades, 392.0, 127.9, -616.0, 12.0, 10.0, "1808/2 Uherum Pass east mouth")
        };

        /// <summary>(creature, client package, comment).</summary>
        public static readonly (uint Creature, uint Package, string Comment)[] Packages =
        {
            (Mayes, MayesPackage, "Medical Officer Mayes (client package 172)")
        };

        /// <summary>(mission, giver, receiver, level, comment). group 1, general category, not shareable or radio-completable.</summary>
        public static readonly (uint Id, uint Giver, uint Receiver, uint Level, string Comment)[] Missions =
        {
            (FilcherFrenzy, Sebastian, Sebastian, 12u, "Filcher Frenzy (Divide)"),
            (DissectionsTwo, Kerr, Mayes, 10u, "Dissections: Part II (Divide)"),
            (DissectionsThree, Mayes, Mayes, 10u, "Dissections: Part III (Divide)"),
            (TheTallest, Kibner, Kibner, 10u, "The Tallest (Divide)"),
            (Careless, Sherman, Sherman, 10u, "Careless (Divide)"),
            (NoisePollution, YormaBrown, YormaBrown, 15u, "Noise Pollution (Palisades)"),
            (ClearYourUherum, Tayros, Tayros, 15u, "Clear Your Uherum (Palisades)")
        };

        /// <summary>(mission, client objective, ordinal, required, revealed on acceptance, the client's objective text).</summary>
        public static readonly (uint Mission, uint Objective, uint Ordinal, bool Required, bool Revealed, string Text)[] Objectives =
        {
            (FilcherFrenzy, 1u, 1u, true, true, "Filchers Killed"),
            (DissectionsTwo, 1u, 1u, true, true, "Gather Xanx Samples"),
            (DissectionsTwo, 2u, 2u, true, false, "Deliver Samples to Specialist Kerr"),
            (DissectionsTwo, 3u, 3u, true, false, "Deliver Samples to Medical Officer Mayes"),
            (DissectionsThree, 1u, 1u, true, true, "Collect a sample of skin from Rotting Sal"),
            (DissectionsThree, 3u, 2u, true, false, "Return to Medical Officer Mayes"),
            (TheTallest, 2u, 1u, true, true, "Acquire 2 Stalker Pieces"),
            (Careless, 2u, 1u, true, true, "Acquire 8 Caretaker Samples"),
            (NoisePollution, 1u, 1u, true, true, "Kill Cumbria Boargar"),
            (NoisePollution, 2u, 2u, true, true, "Kill Shahrbaraz"),
            (ClearYourUherum, 1u, 1u, true, true, "Kill Uherum Fithik"),
            (ClearYourUherum, 2u, 2u, true, true, "Traverse Uherum Pass"),
            (ClearYourUherum, 3u, 3u, false, false, "Start"),
            (ClearYourUherum, 4u, 4u, false, false, "Middle"),
            (ClearYourUherum, 5u, 5u, false, false, "End")
        };

        /// <summary>371 and 372 reveal their deliveries after the samples, in the client's order.</summary>
        public static readonly (uint Mission, uint Completed, uint Revealed)[] Transitions =
        {
            (DissectionsTwo, 1u, 2u),
            (DissectionsTwo, 2u, 3u),
            (DissectionsThree, 1u, 3u)
        };

        /// <summary>Bindings: mission, objective, kind, area, creature, item template, drop chance %, counter, comment.</summary>
        public static readonly (uint Mission, uint Objective, byte Kind, uint Area, uint Creature, uint Item, double Drop, byte Counter, string Comment)[] Bindings =
        {
            (FilcherFrenzy, 1u, Kill, 0u, Filcher, 0u, 0.0, 0, "358/1 kill Filchers"),
            (DissectionsTwo, 1u, ItemCollected, 0u, XanxVenomspitter, XanxSkinSample, 50.0, 0, "371/1 Xanx Skin Sample"),
            (DissectionsThree, 1u, ItemCollected, 0u, RottingSal, HominisMachinaScraping, 30.0, 0, "372/1 Hominis Machina Scraping from Rotting Sal"),
            (TheTallest, 2u, ItemCollected, 0u, ClassFourStalker, StalkerScraps, 100.0, 0, "774/2 Stalker Scraps"),
            (Careless, 2u, ItemCollected, 0u, Caretaker, CaretakerFluid, 50.0, 0, "755/2 Caretaker Fluid"),
            (NoisePollution, 1u, Kill, 0u, CumbriaBoargar, 0u, 0.0, 0, "342/1 kill Cumbria Boargar"),
            (NoisePollution, 2u, Kill, 0u, Shahrbaraz, 0u, 0.0, 0, "342/2 kill Shahrbaraz"),
            (ClearYourUherum, 1u, Kill, 0u, Fithik, 0u, 0.0, 0, "1808/1 kill Uherum Fithik"),
            (ClearYourUherum, 2u, AreaEntered, UherumExit, 0u, 0u, 0.0, NoCounter, "1808/2 reach Uherum Pass east mouth"),
            (SearchingForAcceptance, 1u, Kill, 0u, PalisadesWarnet, 0u, 0.0, 0, "368/1 kill Warnets")
        };

        /// <summary>Counters: mission, objective, counter id, target.</summary>
        public static readonly (uint Mission, uint Objective, uint Counter, int Target)[] Counters =
        {
            (FilcherFrenzy, 1u, 0u, 10),
            (DissectionsTwo, 1u, 0u, 5),
            (DissectionsThree, 1u, 0u, 1),
            (TheTallest, 2u, 0u, 2),
            (Careless, 2u, 0u, 8),
            (NoisePollution, 1u, 0u, 8),
            (NoisePollution, 2u, 0u, 1),
            (ClearYourUherum, 1u, 0u, 20),
            (SearchingForAcceptance, 1u, 0u, 5)
        };

        /// <summary>TaRapedia's amounts, all first recorded before Update 1.4 (GAP-REWARD-ERA); no item lists.</summary>
        public static readonly (uint Mission, byte Type, int Amount)[] Rewards =
        {
            (FilcherFrenzy, Experience, 14000), (FilcherFrenzy, Credits, 2100),
            (DissectionsTwo, Experience, 14000), (DissectionsTwo, Credits, 2100),
            (DissectionsThree, Credits, 2800),
            (TheTallest, Experience, 4000), (TheTallest, Credits, 1650),
            (Careless, Credits, 300),
            (NoisePollution, Experience, 17000), (NoisePollution, Credits, 2550),
            (ClearYourUherum, Experience, 20000), (ClearYourUherum, Credits, 3000)
        };

        /// <summary>372 follows 371 and 755 follows 774 (TaRapedia). 371's own 370 is not seeded (OD-165).</summary>
        public static readonly (uint Mission, uint Required, string Comment)[] Prerequisites =
        {
            (DissectionsThree, DissectionsTwo, "Dissections: Part II completed"),
            (Careless, TheTallest, "The Tallest completed")
        };

        private static readonly string[] CreatureColumns =
            { "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed", "walk_speed",
              "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8" };
        private static readonly string[] CreatureTypes =
            { "INTEGER", "varchar(50)", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "REAL", "REAL",
              "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" };
        private static readonly string[] PoolColumns =
        {
            "id", "mode", "anim_type", "respown_time", "pos_x", "pos_y", "pos_z", "rotation", "map_context_id",
            "creature_1_Id", "creature_1_min_count", "creature_1_max_count", "creature_2_Id", "creature_2_min_count", "creature_2_max_count",
            "creature_3_Id", "creature_3_min_count", "creature_3_max_count", "creature_4_Id", "creature_4_min_count", "creature_4_max_count",
            "creature_5_Id", "creature_5_min_count", "creature_5_max_count", "creature_6_Id", "creature_6_min_count", "creature_6_max_count"
        };
        private static readonly string[] PoolTypes =
            new[] { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "REAL", "REAL", "REAL", "REAL", "INTEGER" }.Concat(Integers(18)).ToArray();
        private static readonly string[] AreaColumns = { "id", "map_context_id", "shape", "pos_x", "pos_y", "pos_z", "radius", "half_height", "comment" };
        private static readonly string[] AreaTypes = { "INTEGER", "INTEGER", "INTEGER", "REAL", "REAL", "REAL", "REAL", "REAL", "varchar(50)" };
        private static readonly string[] PackageColumns = { "id", "package_id", "comment" };
        private static readonly string[] PackageTypes = { "INTEGER", "INTEGER", "varchar(50)" };
        private static readonly string[] MissionColumns =
            { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" };
        private static readonly string[] MissionTypes = Integers(8).Concat(new[] { "varchar(50)" }).ToArray();
        private static readonly string[] ObjectiveColumns = { "mission_id", "objective_id", "comment", "is_required", "ordinal", "revealed_on_accept" };
        private static readonly string[] ObjectiveTypes = { "INTEGER", "INTEGER", "varchar(100)", "INTEGER", "INTEGER", "INTEGER" };
        private static readonly string[] TransitionColumns = { "mission_id", "completed_objective_id", "revealed_objective_id" };
        private static readonly string[] BindingColumns =
        {
            "mission_id", "objective_id", "binding_id", "kind", "area_id", "placement_id", "creature_id", "action_id",
            "destroying_hit_only", "equip_match", "item_template_id", "drop_chance", "item_set_id", "target_state", "counter_id", "comment"
        };
        private static readonly string[] BindingTypes = Integers(11).Concat(new[] { "double" }).Concat(Integers(3)).Concat(new[] { "varchar(50)" }).ToArray();
        private static readonly string[] CounterColumns = { "mission_id", "objective_id", "counter_id", "initial_value", "target_value" };
        private static readonly string[] RewardColumns = { "id", "type", "credits", "item_template_id", "quantity" };
        private static readonly string[] RewardKeyColumns = { "id", "type", "item_template_id" };
        private static readonly string[] PrerequisiteColumns = { "mission_id", "or_group", "required_mission_id", "required_state", "comment" };
        private static readonly string[] PrerequisiteKeyColumns = { "mission_id", "or_group", "required_mission_id" };

        /// <summary>The missions whose client objective rows this migration replaces (368's rows are already set).</summary>
        private static IEnumerable<(uint Mission, uint Objective, string Text)> SkeletonRows
            => Objectives.Select(o => (o.Mission, o.Objective, o.Text));

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            // ── the populations ──
            InsertTyped(migrationBuilder, "creature", CreatureColumns, CreatureTypes,
                Rows(Creatures.Select(c => new object[] { c.Id, c.Comment, c.Class, 0u, c.Level, c.Hp, c.NameId, 9.0, 5.0,
                    c.Action1, c.Action2, 0u, 0u, 0u, 0u, 0u, 0u })));
            InsertTyped(migrationBuilder, "spawnpool", PoolColumns, PoolTypes, Rows(Pools.Select(PoolRow)));
            InsertTyped(migrationBuilder, "content_area", AreaColumns, AreaTypes,
                Rows(Areas.Select(a => new object[] { a.Id, a.Map, (byte)2, a.X, a.Y, a.Z, a.Radius, a.HalfHeight, a.Comment })));
            InsertTyped(migrationBuilder, "npc_package", PackageColumns, PackageTypes,
                Rows(Packages.Select(p => new object[] { p.Creature, p.Package, p.Comment })));

            // ── the missions ──
            InsertTyped(migrationBuilder, "npc_mission", MissionColumns, MissionTypes,
                Rows(Missions.Select(m => new object[] { m.Id, m.Giver, m.Receiver, m.Level, (byte)1, GeneralCategory, false, false, m.Comment })));

            // The client skeleton rows are replaced by the same rows with the three flags set.
            DeleteTyped(migrationBuilder, "npc_mission_objective", new[] { "mission_id", "objective_id" }, Integers(2),
                Rows(SkeletonRows.Select(o => new object[] { o.Mission, o.Objective })));
            InsertTyped(migrationBuilder, "npc_mission_objective", ObjectiveColumns, ObjectiveTypes,
                Rows(Objectives.Select(o => new object[] { o.Mission, o.Objective, o.Text, o.Required, o.Ordinal, o.Revealed })));

            InsertTyped(migrationBuilder, "npc_mission_objective_transition", TransitionColumns, Integers(3),
                Rows(Transitions.Select(t => new object[] { t.Mission, t.Completed, t.Revealed })));

            InsertTyped(migrationBuilder, "npc_mission_objective_binding", BindingColumns, BindingTypes,
                Rows(Bindings.Select(b => new object[]
                {
                    b.Mission, b.Objective, (byte)0, b.Kind, b.Area, 0u, b.Creature, 0u, false, (byte)0, b.Item, b.Drop, 0u, 0u, b.Counter, b.Comment
                })));

            InsertTyped(migrationBuilder, "npc_mission_objective_counter", CounterColumns, Integers(5),
                Rows(Counters.Select(c => new object[] { c.Mission, c.Objective, c.Counter, 0, c.Target })));

            InsertTyped(migrationBuilder, "npc_mission_reward", RewardColumns, Integers(5),
                Rows(Rewards.Select(r => new object[] { r.Mission, r.Type, r.Amount, 0u, 0u })));

            InsertTyped(migrationBuilder, "npc_mission_prerequisite", PrerequisiteColumns, new[] { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "varchar(50)" },
                Rows(Prerequisites.Select(p => new object[] { p.Mission, (byte)0, p.Required, Completed, p.Comment })));
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            DeleteTyped(migrationBuilder, "npc_mission_prerequisite", PrerequisiteKeyColumns, Integers(3),
                Rows(Prerequisites.Select(p => new object[] { p.Mission, (byte)0, p.Required })));
            DeleteTyped(migrationBuilder, "npc_mission_reward", RewardKeyColumns, Integers(3),
                Rows(Rewards.Select(r => new object[] { r.Mission, r.Type, 0u })));
            DeleteTyped(migrationBuilder, "npc_mission_objective_counter", new[] { "mission_id", "objective_id", "counter_id" }, Integers(3),
                Rows(Counters.Select(c => new object[] { c.Mission, c.Objective, c.Counter })));
            DeleteTyped(migrationBuilder, "npc_mission_objective_binding", new[] { "mission_id", "objective_id", "binding_id" }, Integers(3),
                Rows(Bindings.Select(b => new object[] { b.Mission, b.Objective, (byte)0 })));
            DeleteTyped(migrationBuilder, "npc_mission_objective_transition", TransitionColumns, Integers(3),
                Rows(Transitions.Select(t => new object[] { t.Mission, t.Completed, t.Revealed })));

            // The client skeleton rows come back with their text and all three server flags unknown (NULL).
            DeleteTyped(migrationBuilder, "npc_mission_objective", new[] { "mission_id", "objective_id" }, Integers(2),
                Rows(SkeletonRows.Select(o => new object[] { o.Mission, o.Objective })));
            InsertTyped(migrationBuilder, "npc_mission_objective", ObjectiveColumns, ObjectiveTypes,
                Rows(SkeletonRows.Select(o => new object[] { o.Mission, o.Objective, o.Text, null, null, null })));

            DeleteTyped(migrationBuilder, "npc_mission", new[] { "id" }, Integers(1), Rows(Missions.Select(m => new object[] { m.Id })));

            DeleteTyped(migrationBuilder, "npc_package", new[] { "id" }, Integers(1), Rows(Packages.Select(p => new object[] { p.Creature })));
            DeleteTyped(migrationBuilder, "content_area", new[] { "id" }, Integers(1), Rows(Areas.Select(a => new object[] { a.Id })));
            DeleteTyped(migrationBuilder, "spawnpool", new[] { "id" }, Integers(1), Rows(Pools.Select(p => new object[] { p.Id })));
            DeleteTyped(migrationBuilder, "creature", new[] { "id" }, Integers(1), Rows(Creatures.Select(c => new object[] { c.Id })));
        }

        private static object[] PoolRow((uint Id, uint Map, byte Anim, uint Respawn, double X, double Y, double Z, (uint Creature, byte Count)[] Slots) pool)
        {
            var row = new List<object> { pool.Id, (byte)0, pool.Anim, pool.Respawn, pool.X, pool.Y, pool.Z, 0.0, pool.Map };
            for (var i = 0; i < 6; i++)
            {
                if (i < pool.Slots.Length)
                    row.AddRange(new object[] { pool.Slots[i].Creature, pool.Slots[i].Count, pool.Slots[i].Count });
                else
                    row.AddRange(new object[] { 0u, (byte)0, (byte)0 });
            }
            return row.ToArray();
        }

        private static string[] Integers(int count) => Enumerable.Repeat("INTEGER", count).ToArray();

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
