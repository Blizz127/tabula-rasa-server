using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// The three Divide operations - Minos Caverns (context 1347), Timora Mines (1348) and Torcastra Prison (1349) - from the
    /// 2026-09-26 Divide instance dossier (research/20260926-instance-dossiers-divide, dossiers.json rows, missions and gaps):
    /// the NPCs the dossier places, the three emulator-lineage pools that stood on markers moved to their sources' positions,
    /// Hamilton's package, the Timora bosses, and the four missions that can now be finished.
    ///
    /// <b>Why.</b> Squad instancing (MissionContextSquadInstancing) clones a context's spawn pools and content placements into
    /// every squad copy. Kearney and Pastre were pools stacked on the hospital marker (510118 3.7 m beside the medic, 510119 on
    /// it) and Hamilton a pool on a vendor marker (510120) with the prisoner computer bank's package 1527. Each becomes a
    /// content placement at the position its source records; the pool stops drawing (0/0, the SingleClassTrainers pattern) and
    /// Down restores 1/1. Minos Caverns and Timora Mines were navmesh islands; the rebuilt navmeshes (Rasa.NavMesh
    /// data/map_build_settings.csv for Minos, data/terrain_cuts.csv for Timora) join every seeded position to the instance's
    /// arrival, which is what lets Tyler walk the 392 escort and the Timora bosses guard the Fuel Egress block.
    ///
    /// <b>Evidence and tiers</b> (every field in docs/evidence/bootcamp-d11-reconstruction-manifest.json, migration
    /// DivideOperationsInstances; the probes in docs/evidence/divide-operations-instances-20260927.json):
    ///   * Names are the client's creaturenamelanguage ids (original). Positions are Ten Ton Hammer /loc readings (TTH 13591
    ///     Minos 2007-10-18, 13803 Timora 2007-10-24, 15113 Torcastra 2007-11-28; inferred) or, for Pastre, Morrow and the
    ///     Overseers, the dossier's hall-probe measurements (measured, +/-6 m and +/-40 m); heights are the rebuilt navmesh
    ///     floor minus 0.276 m (measured). No guide records a rework note after these dates.
    ///   * Classes, levels, hit points, attacks and speeds nobody records are labelled analogues under OD-145: the AFS NPCs
    ///     wear Kearney's own row and clothing (class 3846, 1000 hp, standing; Ferme Captain Velns' female body 3848),
    ///     Horlo Q'uoa's (7035), the Timora bosses the world's Thrax Overseers (class 10504, 1000 hp, action 41, run 9),
    ///     Torqua the world's Bane Hunter Boss (class 10327, 2000 hp, action 35, run 9). Torqua's level 19 and Horlo's 12
    ///     are TaRapedia's (inferred).
    ///   * Mission levels are the operation's band minimum (inferred, GAP-MISSION-LEVEL), as the Pravus batch took them.
    ///     392's 14,000 experience and 2,100 credits are TaRapedia readings first recorded before Update 1.4 (GAP-REWARD-ERA,
    ///     era pre-1.4); 340, 1905 and 792 have no reward source and carry no reward rows.
    ///
    /// <b>Seeded.</b>
    ///   * Timora: Kearney 1348100 (TTH /loc), Pastre 1348101, Field Ranger Morrow 1348102 (new creature 1348001, the 403
    ///     giver), Field Ranger Sanchez 1348103 (1348002, 1905's receiver), the Wardmaster 1348120, two Overseers
    ///     1348121-1348122 and the Warden 1348123 (1348003-1348005, 792's targets; the Warden in the Command Center the
    ///     rebuilt navmesh now reaches).
    ///   * Minos: Scout Horlo 1347100 (1347001, package 2380); Tyler (199006) takes the Escort behavior for 392 and the
    ///     escort ends at the Field Medic, area 1347500.
    ///   * Torcastra: Lt. Cisco 1349100 at the entrance (1349001, package 1156), Airman Hamilton 1349102 in the Research Ward
    ///     stasis tube with his own package 1526 and the tube 1349103 (client class 7197 UsableInertDestBaneStasisChamber,
    ///     destroyable, hit points an analogue, OD-150), Overseer Torqua 1349104 (1349002), Ranger Ferme 1349105 (1349003,
    ///     package 193).
    ///   * Missions 340 Report to Field Ranger Kearney (Simpson 199950 to Kearney), 1905 Central Bound Patrol (Kearney to
    ///     Sanchez, after 340; objective 1, the escort, optional and unrevealed until its party is identified, OD-146), 792
    ///     Climbing the Corporate Ladder (Pastre) and 392 Cave Extraction (Tyler; offered without its 383 prerequisite, which is
    ///     not seeded, OD-147).
    ///
    /// <b>Held</b> (gaps in the manifest). 403 and its drills (the detonator item and the bomb's fuse are unrecorded, and a
    /// bomb with no mission is an invented use), 404, 1276 (its giver Jayjack is not in the world and the Eloh projector's
    /// speaker class is unrecorded), 384, 391, 397 (O'Toole is not in the world; the guards, the door and the Cell Block
    /// Warden are unrecorded), 594 (garrisons, gatekeeper, gate and timer), 356 (its prerequisite 594 is held; the computer
    /// bank is unplaced), 1860 (neither container class is evidenced, GAP-MINOS-CHEST-CLASS; its prerequisite 1859 is not
    /// seeded) and with it 1861 (objectives 2-5 unrecorded). Torqua carries no drop until 1860 exists. Q'uoa keeps package
    /// 2345 (GAP-QUOA-PACKAGE: his own package is unrecorded). No ambient population (GAP-DIVIDE-AMBIENT-POPULATION).
    /// </summary>
    public static class DivideOperationsInstancesRows
    {
        public const string Migration = "DivideOperationsInstances";

        public const uint Minos = 1347u, Timora = 1348u, Torcastra = 1349u;

        // ── creatures ──
        public const uint Horlo = 1347001u;
        public const uint Morrow = 1348001u, Sanchez = 1348002u, Wardmaster = 1348003u, Overseer = 1348004u, Warden = 1348005u;
        public const uint Cisco = 1349001u, Torqua = 1349002u, Ferme = 1349003u;

        /// <summary>The emulator-lineage pools (and creature rows) that become placements.</summary>
        public const uint Kearney = 510118u, Pastre = 510119u, Hamilton = 510120u;

        public const uint Tyler = 199006u, Simpson = 199950u;

        // ── placements, area ──
        public const uint HorloPlacement = 1347100u;
        public const uint KearneyPlacement = 1348100u, PastrePlacement = 1348101u, MorrowPlacement = 1348102u, SanchezPlacement = 1348103u;
        public const uint WardmasterPlacement = 1348120u, OverseerPlacement1 = 1348121u, OverseerPlacement2 = 1348122u, WardenPlacement = 1348123u;
        public const uint CiscoPlacement = 1349100u, HamiltonPlacement = 1349102u, StasisTube = 1349103u, TorquaPlacement = 1349104u,
            FermePlacement = 1349105u;

        /// <summary>392's destination: the Minos Caverns Field Medic at the way in (HospitalCatalog 39, client marker 133620727551304).</summary>
        public const uint CaveEntranceArea = 1347500u;

        // ── packages ──
        public const uint HorloPackage = 2380u, CiscoPackage = 1156u, FermePackage = 193u;
        public const uint HamiltonPackage = 1526u, PrisonerComputerBankPackage = 1527u;

        // ── missions ──
        public const uint ReportToKearney = 340u, CentralBoundPatrol = 1905u, CorporateLadder = 792u, CaveExtraction = 392u;

        /// <summary>UsableInertDestBaneStasisChamber (augmentation 41 InertDestroyable).</summary>
        public const uint StasisChamberClass = 7197u;

        /// <summary>OD-150: the stasis tube's hit points (analogue, the Pravus capsule's).</summary>
        public const uint StasisTubeHitPoints = 1000u;

        private const uint GeneralCategory = 10000001u;
        private const byte Credits = 1, Experience = 3, Completed = 4;
        private const byte CreaturePlacement = 1, UsablePlacement = 2, Destroyable = 2, Stationary = 1, CreatureAi = 2, EscortBehavior = 3;
        private const byte AreaEntered = 1, Kill = 6;
        private const byte NoCounter = 255;
        private const byte Sphere = 1;
        private const uint Intact = 110u;

        /// <summary>The creatures: id, comment, class, faction, level, hp, name id, run, walk, action1.</summary>
        public static readonly (uint Id, string Comment, uint Class, byte Faction, uint Level, uint Hp, uint NameId, double Run, double Walk, uint Action)[] Creatures =
        {
            (Horlo, "Scout Horlo (Minos Caverns)", 7035u, 1, 12u, 1000u, 10340u, 0.0, 0.0, 0u),
            (Morrow, "Field Ranger Morrow (Timora Mines)", 3846u, 1, 13u, 1000u, 3047u, 0.0, 0.0, 0u),
            (Sanchez, "Field Ranger Sanchez (Timora Mines)", 3846u, 1, 13u, 1000u, 3046u, 0.0, 0.0, 0u),
            (Wardmaster, "Wardmaster (Timora Mines boss)", 10504u, 0, 16u, 1000u, 20000013u, 9.0, 0.0, 41u),
            (Overseer, "Overseer (Timora Mines boss)", 10504u, 0, 16u, 1000u, 20000011u, 9.0, 0.0, 41u),
            (Warden, "Warden (Timora Mines boss)", 10504u, 0, 16u, 1000u, 20000012u, 9.0, 0.0, 41u),
            (Cisco, "Lt. Cisco (Torcastra Prison)", 3846u, 1, 15u, 1000u, 5272u, 0.0, 0.0, 0u),
            (Torqua, "Overseer Torqua (Torcastra Prison boss)", 10327u, 0, 19u, 2000u, 10332u, 9.0, 0.0, 35u),
            (Ferme, "Ranger Ferme (Torcastra Prison)", 3848u, 1, 15u, 1000u, 3034u, 0.0, 0.0, 0u)
        };

        /// <summary>The placements: id, map, kind, creature, entity class, usable kind, x, y, z, behavior, initial state,
        /// hit points, escort mission, comment. Heights are the rebuilt navmesh floor minus 0.276 m.</summary>
        public static readonly (uint Id, uint Map, byte Kind, uint Creature, uint Class, byte Usable, double X, double Y, double Z,
            byte Behavior, uint State, uint HitPoints, string Comment)[] Placements =
        {
            (HorloPlacement, Minos, CreaturePlacement, Horlo, 0u, 0, -47.0, 29.381, 19.0, Stationary, 0u, 0u, "Scout Horlo (TTH /loc)"),
            (KearneyPlacement, Timora, CreaturePlacement, Kearney, 0u, 0, -136.9, 205.465, 518.2, Stationary, 0u, 0u, "Field Ranger Kearney (TTH /loc)"),
            (PastrePlacement, Timora, CreaturePlacement, Pastre, 0u, 0, -143.39, 205.465, 515.06, Stationary, 0u, 0u, "Sergeant Pastre (behind Kearney)"),
            (MorrowPlacement, Timora, CreaturePlacement, Morrow, 0u, 0, -133.25, 205.465, 512.8, Stationary, 0u, 0u, "Field Ranger Morrow (across from Pastre)"),
            (SanchezPlacement, Timora, CreaturePlacement, Sanchez, 0u, 0, 56.6, 145.465, 50.0, Stationary, 0u, 0u, "Field Ranger Sanchez (TTH /loc)"),
            (WardmasterPlacement, Timora, CreaturePlacement, Wardmaster, 0u, 0, -241.9, 217.9, 289.3, CreatureAi, 0u, 0u, "Wardmaster (bunker, TTH /loc)"),
            (OverseerPlacement1, Timora, CreaturePlacement, Overseer, 0u, 0, 178.47, 165.43, -7.78, CreatureAi, 0u, 0u, "Overseer 1 (Fuel Bore Cavern)"),
            (OverseerPlacement2, Timora, CreaturePlacement, Overseer, 0u, 0, 182.32, 165.839, -7.2, CreatureAi, 0u, 0u, "Overseer 2 (Fuel Bore Cavern)"),
            (WardenPlacement, Timora, CreaturePlacement, Warden, 0u, 0, 470.5, 231.477, -200.06, CreatureAi, 0u, 0u, "Warden (Command Center)"),
            (CiscoPlacement, Torcastra, CreaturePlacement, Cisco, 0u, 0, -268.4, 13.396, 308.11, Stationary, 0u, 0u, "Lt. Cisco (prison entrance)"),
            (HamiltonPlacement, Torcastra, CreaturePlacement, Hamilton, 0u, 0, -137.0, 99.396, -550.0, Stationary, 0u, 0u, "Airman Hamilton (Research Ward tube)"),
            (StasisTube, Torcastra, UsablePlacement, 0u, StasisChamberClass, Destroyable, -137.0, 99.396, -550.0, Stationary, Intact, StasisTubeHitPoints, "Hamilton's stasis tube (Research Ward)"),
            (TorquaPlacement, Torcastra, CreaturePlacement, Torqua, 0u, 0, 124.0, 70.197, -101.0, CreatureAi, 0u, 0u, "Overseer Torqua (TTH /loc)"),
            (FermePlacement, Torcastra, CreaturePlacement, Ferme, 0u, 0, -72.5, 101.574, -694.1, Stationary, 0u, 0u, "Ranger Ferme (TTH /loc)")
        };

        /// <summary>The new creatures' packages: creature, package, comment.</summary>
        public static readonly (uint Id, uint Package, string Comment)[] Packages =
        {
            (Horlo, HorloPackage, "Scout Horlo (client package 2380)"),
            (Cisco, CiscoPackage, "Lt. Cisco (client package 1156)"),
            (Ferme, FermePackage, "Ranger Ferme (client package 193)")
        };

        /// <summary>
        /// The bodies of the new AFS NPCs (creature, slot, class, colour): NPC_Human_Swapset_Male/Female is assembled from
        /// clothing pieces and renders bare without them (ContentNpcAppearance). Morrow, Sanchez and Cisco wear Kearney's
        /// shipped set (MissionNpcAppearancePreloader 510118), Ferme Captain Velns'; analogues under OD-145, no source shows
        /// their gear (GAP-DIVIDE-NPC-APPEARANCE).
        /// </summary>
        public static readonly (uint Creature, uint Slot, uint Class, uint Color)[] Appearance =
            new[] { Morrow, Sanchez, Cisco }.SelectMany(id => new (uint, uint, uint, uint)[]
                {
                    (id, 2u, 4021u, 4294934528u), (id, 13u, 6271u, 1u), (id, 14u, 9781u, 4278655809u),
                    (id, 15u, 4023u, 4294934528u), (id, 16u, 4022u, 4294934528u), (id, 17u, 24008u, 4286690539u)
                })
            .Concat(new (uint, uint, uint, uint)[]
                {
                    (Ferme, 2u, 4021u, 266553u), (Ferme, 14u, 25255u, 266553u), (Ferme, 15u, 19296u, 266553u),
                    (Ferme, 16u, 19250u, 266553u), (Ferme, 17u, 24019u, 4286886614u)
                })
            .ToArray();

        /// <summary>The pools that become placements (0/0 up, 1/1 down).</summary>
        public static readonly uint[] RetiredPools = { Kearney, Pastre, Hamilton };

        /// <summary>The cave entrance: the Field Medic marker, a 10 m sphere as the Milpas escort's destinations are.</summary>
        public static readonly (double X, double Y, double Z, double Radius) CaveEntrance = (-0.995, 30.711, 112.288, 10.0);

        /// <summary>The missions: id, giver, receiver, level, comment.</summary>
        public static readonly (uint Id, uint Giver, uint Receiver, uint Level, string Comment)[] Missions =
        {
            (ReportToKearney, Simpson, Kearney, 13u, "Report to Field Ranger Kearney (Timora)"),
            (CentralBoundPatrol, Kearney, Sanchez, 13u, "Central Bound Patrol (Timora)"),
            (CorporateLadder, Pastre, Pastre, 13u, "Climbing the Corporate Ladder (Timora)"),
            (CaveExtraction, Tyler, Tyler, 11u, "Cave Extraction (Minos)")
        };

        /// <summary>The client's objective rows with the three server flags: mission, objective, text, required, ordinal, revealed.</summary>
        public static readonly (uint Mission, uint Objective, string Text, bool Required, uint Ordinal, bool Revealed)[] Objectives =
        {
            (ReportToKearney, 2u, "Report to Field Ranger Kearney in the Timora Mines", true, 1u, true),
            (CentralBoundPatrol, 2u, "Report to Field Ranger Kearney in the Timora Mines", true, 1u, true),
            // Held (OD-146): the escort party is unrecorded (GAP-TIMORA-ESCORT-SOLDIERS); optional and never revealed until it is.
            (CentralBoundPatrol, 1u, "Escort the soldiers to the lower mines.", false, 2u, false),
            (CorporateLadder, 1u, "Eliminate the Warden", true, 1u, true),
            (CorporateLadder, 2u, "Assassinate the Overseers", true, 2u, true),
            (CorporateLadder, 3u, "Assassinate the Wardmaster", true, 3u, true),
            (CaveExtraction, 1u, "Escort Taylor to the Cave Entrance", true, 1u, true)
        };

        /// <summary>Bindings: mission, objective, kind, area, creature, counter, comment. 340/2 and 1905/2 complete through the
        /// client's own objectiveconversation rows on Kearney's package 158 and need none.</summary>
        public static readonly (uint Mission, uint Objective, byte Kind, uint Area, uint Creature, byte Counter, string Comment)[] Bindings =
        {
            (CorporateLadder, 1u, Kill, 0u, Warden, 0, "792/1 kill the Warden"),
            (CorporateLadder, 2u, Kill, 0u, Overseer, 0, "792/2 kill the two Overseers"),
            (CorporateLadder, 3u, Kill, 0u, Wardmaster, 0, "792/3 kill the Wardmaster"),
            (CaveExtraction, 1u, AreaEntered, CaveEntranceArea, 0u, NoCounter, "392/1 reach the Field Medic with Tyler")
        };

        /// <summary>Counters: mission, objective, counter id (the client's counter index carrying the text), target.</summary>
        public static readonly (uint Mission, uint Objective, uint Counter, int Target)[] Counters =
        {
            (CorporateLadder, 1u, 0u, 1),
            (CorporateLadder, 2u, 0u, 2),
            (CorporateLadder, 3u, 0u, 1)
        };

        /// <summary>TaRapedia's amounts for 392, recorded before Update 1.4 (GAP-REWARD-ERA); no item list.</summary>
        public static readonly (uint Mission, byte Type, int Amount)[] Rewards =
        {
            (CaveExtraction, Experience, 14000), (CaveExtraction, Credits, 2100)
        };

        /// <summary>1905 is 340's continuance (TaRapedia rev 30014). 392's 383 is held (OD-147).</summary>
        public static readonly (uint Mission, uint Required, string Comment)[] Prerequisites =
        {
            (CentralBoundPatrol, ReportToKearney, "Report to Field Ranger Kearney completed")
        };


        private static readonly string[] CreatureColumns =
            { "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed", "walk_speed",
              "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8" };
        private static readonly string[] CreatureTypes =
            { "INTEGER", "varchar(50)", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "REAL", "REAL",
              "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" };
        private static readonly string[] PlacementColumns =
            { "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id", "usable_kind", "pos_x", "pos_y", "pos_z",
              "rotation", "behavior", "initial_state", "alternate_state", "alternate_state_condition_id", "windup_ms", "name_override_id",
              "hit_points", "restore_ms", "fuse_ms", "loot_item_set_id", "respawn_ms", "present_condition_id", "usable_condition_id",
              "escort_mission_id", "comment" };
        private static readonly string[] PlacementTypes =
            { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "REAL", "REAL", "REAL",
              "REAL", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER",
              "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER",
              "INTEGER", "varchar(50)" };
        private static readonly string[] AreaColumns = { "id", "map_context_id", "shape", "pos_x", "pos_y", "pos_z", "radius", "half_height", "comment" };
        private static readonly string[] AreaTypes = { "INTEGER", "INTEGER", "INTEGER", "REAL", "REAL", "REAL", "REAL", "REAL", "varchar(50)" };
        private static readonly string[] MissionColumns =
            { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" };
        private static readonly string[] MissionTypes = Integers(8).Concat(new[] { "varchar(50)" }).ToArray();
        private static readonly string[] ObjectiveColumns = { "mission_id", "objective_id", "comment", "is_required", "ordinal", "revealed_on_accept" };
        private static readonly string[] ObjectiveTypes = { "INTEGER", "INTEGER", "varchar(100)", "INTEGER", "INTEGER", "INTEGER" };
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

        private static string[] Integers(int count) => Enumerable.Repeat("INTEGER", count).ToArray();

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            // ── the marker-stacked pools stop drawing; their creatures stand as placements below ──
            foreach (var pool in RetiredPools)
                PoolCounts(migrationBuilder, pool, 0);

            InsertTyped(migrationBuilder, "creature", CreatureColumns, CreatureTypes,
                Rows(Creatures.Select(c => new object[] { c.Id, c.Comment, c.Class, c.Faction, c.Level, c.Hp, c.NameId, c.Run, c.Walk,
                    c.Action, 0u, 0u, 0u, 0u, 0u, 0u, 0u })));

            InsertTyped(migrationBuilder, "creature_appearance", new[] { "id", "slot_id", "Class_id", "color" }, Integers(4),
                Rows(Appearance.Select(a => new object[] { a.Creature, a.Slot, a.Class, a.Color })));

            InsertTyped(migrationBuilder, "npc_package", new[] { "id", "package_id", "comment" }, new[] { "INTEGER", "INTEGER", "varchar(50)" },
                Rows(Packages.Select(p => new object[] { p.Id, p.Package, p.Comment })));

            // Hamilton's own package: 1527 is the prisoner computer bank's (356/4 "Uploading prisoner data").
            UpdatePackage(migrationBuilder, Hamilton, HamiltonPackage);

            InsertTyped(migrationBuilder, "content_area", AreaColumns, AreaTypes, Rows(new[]
            {
                new object[] { CaveEntranceArea, Minos, Sphere, CaveEntrance.X, CaveEntrance.Y, CaveEntrance.Z, CaveEntrance.Radius, 0.0,
                    "392/1 the Minos Caverns Field Medic" }
            }));

            InsertTyped(migrationBuilder, "content_placement", PlacementColumns, PlacementTypes,
                Rows(Placements.Select(p => new object[] { p.Id, p.Map, p.Kind, p.Creature, 0u, p.Class, p.Usable, p.X, p.Y, p.Z,
                    0.0, p.Behavior, p.State, 0u, 0u, 0u, 0u, p.HitPoints, 0u, 0u, 0u, 0u, 0u, 0u, 0u, p.Comment })));

            // Tyler walks with the player on 392.
            UpdatePlacement(migrationBuilder, Tyler, new[] { "behavior", "escort_mission_id" }, new[] { "INTEGER", "INTEGER" },
                new object[] { EscortBehavior, CaveExtraction });

            // ── the missions ──
            InsertTyped(migrationBuilder, "npc_mission", MissionColumns, MissionTypes,
                Rows(Missions.Select(m => new object[] { m.Id, m.Giver, m.Receiver, m.Level, (byte)1, GeneralCategory, false, false, m.Comment })));

            DeleteTyped(migrationBuilder, "npc_mission_objective", new[] { "mission_id", "objective_id" }, Integers(2),
                Rows(Objectives.Select(o => new object[] { o.Mission, o.Objective })));
            InsertTyped(migrationBuilder, "npc_mission_objective", ObjectiveColumns, ObjectiveTypes,
                Rows(Objectives.Select(o => new object[] { o.Mission, o.Objective, o.Text, o.Required, o.Ordinal, o.Revealed })));

            InsertTyped(migrationBuilder, "npc_mission_objective_binding", BindingColumns, BindingTypes,
                Rows(Bindings.Select(b => new object[]
                {
                    b.Mission, b.Objective, (byte)0, b.Kind, b.Area, 0u, b.Creature, 0u, false, (byte)0, 0u, 0.0, 0u, 0u, b.Counter, b.Comment
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

            // The client skeleton rows come back with their text and all three server flags unknown (NULL).
            DeleteTyped(migrationBuilder, "npc_mission_objective", new[] { "mission_id", "objective_id" }, Integers(2),
                Rows(Objectives.Select(o => new object[] { o.Mission, o.Objective })));
            InsertTyped(migrationBuilder, "npc_mission_objective", ObjectiveColumns, ObjectiveTypes,
                Rows(Objectives.Select(o => new object[] { o.Mission, o.Objective, o.Text, null, null, null })));

            DeleteTyped(migrationBuilder, "npc_mission", new[] { "id" }, Integers(1), Rows(Missions.Select(m => new object[] { m.Id })));

            UpdatePlacement(migrationBuilder, Tyler, new[] { "behavior", "escort_mission_id" }, new[] { "INTEGER", "INTEGER" },
                new object[] { Stationary, 0u });

            DeleteTyped(migrationBuilder, "content_placement", new[] { "id" }, Integers(1), Rows(Placements.Select(p => new object[] { p.Id })));
            DeleteTyped(migrationBuilder, "content_area", new[] { "id" }, Integers(1), Rows(new[] { new object[] { CaveEntranceArea } }));

            UpdatePackage(migrationBuilder, Hamilton, PrisonerComputerBankPackage);
            DeleteTyped(migrationBuilder, "npc_package", new[] { "id" }, Integers(1), Rows(Packages.Select(p => new object[] { p.Id })));
            DeleteTyped(migrationBuilder, "creature_appearance", new[] { "id", "slot_id" }, Integers(2),
                Rows(Appearance.Select(a => new object[] { a.Creature, a.Slot })));
            DeleteTyped(migrationBuilder, "creature", new[] { "id" }, Integers(1), Rows(Creatures.Select(c => new object[] { c.Id })));

            foreach (var pool in RetiredPools)
                PoolCounts(migrationBuilder, pool, 1);
        }

        /// <summary>A pool's first slot: 0/0 retires it, 1/1 is the value the world had.</summary>
        private static void PoolCounts(MigrationBuilder builder, uint pool, byte count)
        {
            builder.UpdateData("spawnpool", new[] { "id" }, new object[] { pool },
                new[] { "creature_1_min_count", "creature_1_max_count" }, new object[] { count, count });
            var update = (UpdateDataOperation)builder.Operations.Last();
            update.KeyColumnTypes = new[] { "INTEGER" };
            update.ColumnTypes = new[] { "INTEGER", "INTEGER" };
        }

        private static void UpdatePackage(MigrationBuilder builder, uint creature, uint package)
        {
            builder.UpdateData("npc_package", new[] { "id" }, new object[] { creature }, new[] { "package_id" }, new object[] { package });
            var update = (UpdateDataOperation)builder.Operations.Last();
            update.KeyColumnTypes = new[] { "INTEGER" };
            update.ColumnTypes = new[] { "INTEGER" };
        }

        private static void UpdatePlacement(MigrationBuilder builder, uint id, string[] columns, string[] types, object[] values)
        {
            builder.UpdateData("content_placement", new[] { "id" }, new object[] { id }, columns, values);
            var update = (UpdateDataOperation)builder.Operations.Last();
            update.KeyColumnTypes = new[] { "INTEGER" };
            update.ColumnTypes = types;
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
