using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// Pravus Research Facility (context 1430, levels 8-10): the operation's population, its boss, the Machina production,
    /// its NPC fixes and four of its missions, from the 2026-09-26 Wilderness instance dossier
    /// (research/20260926-instance-dossiers-wilderness, dossiers.json rows and gaps).
    ///
    /// <b>Why.</b> Before this the map held four stationary NPCs and the three hospital pools: an operation with nothing in
    /// it. Squad instancing (MissionContextSquadInstancing) clones a context's spawn pools and content placements into
    /// every squad copy, so each squad now meets the population below in its own copy.
    ///
    /// <b>Evidence.</b> The only footage is A4udsM0rcLo (Zorlac, uploaded 2009-01-21; capture date unknown), one run from
    /// the AFS Preparation Camp through the Frontlines, the entrance tunnel, the Interior Halls and Control Rooms into
    /// the Production Chamber, read at 1 fps (footage events P1-001..P1-019). Types and levels come from its target
    /// frames (observed); region names from its minimap; positions from the original 1.16.5.0 map geometry (the control
    /// room module at 224,8,40, the chamber centred on 224,8,168, the three comm dishes) or, for the groups, the region
    /// the minimap names, measured at +/-10 m (entrance) to +/-60 m (hill route); heights are the rebuilt navmesh floor
    /// minus 0.276 m. Everything the footage does not show is labelled in docs/evidence/bootcamp-d11-reconstruction-
    /// manifest.json (migration PravusResearchInstance) and recorded as a gap.
    ///
    /// <b>Seeded.</b>
    ///   * Ten creature rows 1430001-1430010. Name ids are the client's (creaturenamelanguage) where the target frame shows a
    ///     rank name; classes are the client entity classes the display names resolve to (inferred); hit points, attacks and
    ///     movement rates are the world seed's counterparts, analogues under OD-135.
    ///   * Five spawn pools 1430200-1430204 on the instance map: the Frontlines assault (drawn by Bane dropship, as the footage
    ///     shows), the hill route, the Interior Halls, the Control Rooms and the chamber escort. Counts are the fewest
    ///     individuals the footage shows at once, not original counts; the respawn time is the world seed's 20 (OD-136).
    ///   * The entrance guards 1430101-1430103 (Thrax Infantry Trainees carrying the Level 1 Keypass), Overseer Tarmok
    ///     1430110 (level 9 boss, Control Rooms), the Production Fueling Capsule 1430120 (client class 9272, destroyable)
    ///     and the Prototype Forean Machina production 1430121, an ordinary level-8 creature that comes back 2.5 s after it
    ///     dies (OD-137), the six Living Infestations 1430130-1430135 on the three comm dishes, and Ranger Nylla's second
    ///     stand on the Control Rooms gangplank 1430100 (OD-138).
    ///   * NPC fixes: Information Spec. Johnson (199016) moves from his pre-1.7 Ellatha /loc to beside Perkins, where the 1.7
    ///     live note put him, and carries package 106; Ranger Nylla (199017) carries 420; Lt. Cmdr. Parsons (119) carries 450
    ///     and keeps his position (GAP-PARSONS-POSITION); Perkins' placement comment loses its stale "GUESS".
    ///   * Missions 593 The Escapist, 575 The Means of Production (objectives 1-3; 4 and 5 stay unrevealed and optional
    ///     until their content exists), 323 Pirate Radio and 924 Logos: Communication, Control, Machine.
    ///
    /// <b>Held.</b> 574 Machinations (no Council Elder Baruhi and no Forean Machina in the Wilderness world data), and with it
    /// 593's prerequisite on 574 (OD-139); 575/4 (Maulis) and 575/5 (the ambush); 1449 objective 40; Overseer Prion; the
    /// entrance force field; the Frontlines' AFS soldiers; the capsule stopping the production. Never: a Juggernaut or
    /// Predators (removed at D11).
    /// </summary>
    public static class PravusResearchInstanceRows
    {
        public const string Migration = "PravusResearchInstance";

        public const uint Pravus = 1430u;

        public const uint PrototypeMachina = 1430001u, Trainee = 1430002u, Rifleman = 1430003u, Technician = 1430004u,
            ShieldDrone = 1430005u, Tarmok = 1430006u, HominisMachina = 1430007u, ForeanMachina = 1430008u,
            ForeanGunner = 1430009u, EntranceTrainee = 1430010u;

        public const uint NyllaGangplank = 1430100u, Guard1 = 1430101u, Guard2 = 1430102u, Guard3 = 1430103u,
            TarmokPlacement = 1430110u, Capsule = 1430120u, Production = 1430121u;

        public const uint FrontlinesPool = 1430200u, HillPool = 1430201u, HallsPool = 1430202u, ControlPool = 1430203u,
            ChamberPool = 1430204u;

        public const uint ChamberArea = 1430500u;

        public const uint Johnson = 199016u, Nylla = 199017u, Perkins = 199008u, Parsons = 119u, Standley = 134u;
        public const uint JohnsonPackage = 106u, NyllaPackage = 420u, ParsonsPackage = 450u;

        public const uint TheEscapist = 593u, MeansOfProduction = 575u, PirateRadio = 323u, CommunicationControlMachine = 924u;

        /// <summary>The Level 1 Access Keypass (item class 20000013; 11505 is the lower of its two templates).</summary>
        public const uint Keypass = 11505u;

        /// <summary>OD-137: the Prototype placement comes back this long after each death.</summary>
        public const uint ProductionRespawnMs = 2500u;

        private const uint GeneralCategory = 10000001u;
        private const byte Credits = 1, Experience = 3, Completed = 4;
        private const byte CreaturePlacement = 1, UsablePlacement = 2, Destroyable = 2, Stationary = 1, CreatureAi = 2;
        private const byte AreaEntered = 1, Hit = 5, LogosRecovered = 8, ItemCollected = 9;
        private const byte NoCounter = 255;
        private const uint Intact = 110u;

        /// <summary>The creatures: id, comment, class, level, hp, name id, action1 (faction 0, run 9, walk 5).</summary>
        public static readonly (uint Id, string Comment, uint Class, uint Level, uint Hp, uint NameId, uint Action)[] Creatures =
        {
            (PrototypeMachina, "Prototype Forean Machina (Pravus)", 6236u, 8u, 555u, 5290u, 18u),
            (Trainee, "Thrax Infantry Trainee (Pravus)", 29769u, 8u, 555u, 7675u, 33u),
            (Rifleman, "Thrax Rifleman (Pravus)", 4047u, 8u, 555u, 0u, 2u),
            (Technician, "Thrax Technician (Pravus)", 7043u, 8u, 555u, 9106u, 9u),
            (ShieldDrone, "Shield Drone (Pravus)", 7233u, 8u, 750u, 438u, 42u),
            (Tarmok, "Overseer Tarmok (Pravus boss)", 10504u, 9u, 1000u, 10571u, 41u),
            (HominisMachina, "Hominis Machina (Pravus)", 3868u, 9u, 555u, 0u, 18u),
            (ForeanMachina, "Forean Machina (Pravus Frontlines)", 6236u, 9u, 555u, 0u, 18u),
            (ForeanGunner, "Forean Gunner, hostile (Pravus)", 6239u, 9u, 555u, 0u, 2u),
            (EntranceTrainee, "Thrax Infantry Trainee (Pravus entrance)", 29769u, 8u, 555u, 7675u, 33u)
        };

        /// <summary>The ambient pools: id, anim type (1 = Bane dropship), x, y, z, up to six (creature, count), comment.</summary>
        public static readonly (uint Id, byte Anim, double X, double Y, double Z, (uint Creature, byte Count)[] Slots)[] Pools =
        {
            (FrontlinesPool, 1, -116.42, 26.075, 1.57, new[] { (ForeanMachina, (byte)2), (HominisMachina, (byte)3), (Rifleman, (byte)3), (Trainee, (byte)2), (ForeanGunner, (byte)2) }),
            (HillPool, 0, 12.0, 44.661, -20.0, new[] { (HominisMachina, (byte)2), (Rifleman, (byte)2), (Trainee, (byte)3) }),
            (HallsPool, 0, 224.0, 9.61, -24.0, new[] { (Rifleman, (byte)2), (Technician, (byte)3), (Trainee, (byte)2), (ShieldDrone, (byte)1) }),
            // 10 m west of the module centre: the centre is the Stasis Chamber's top, an island of the navmesh.
            (ControlPool, 0, 214.0, 7.797, 40.0, new[] { (Technician, (byte)4), (Trainee, (byte)1) }),
            (ChamberPool, 0, 224.0, 8.297, 168.0, new[] { (Rifleman, (byte)3), (Trainee, (byte)3) })
        };

        /// <summary>OD-136: the world seed's respawn time (units of 100 ms after the pool's last creature dies).</summary>
        public const uint PoolRespawn = 20u;

        /// <summary>The placements (all on 1430): id, kind, creature, package, entity class, usable kind, x, y, z, behavior,
        /// initial state, hit points, respawn ms, comment.</summary>
        public static readonly (uint Id, byte Kind, uint Creature, uint Package, uint Class, byte Usable, double X, double Y, double Z,
            byte Behavior, uint State, uint HitPoints, uint RespawnMs, string Comment)[] Placements =
        {
            (NyllaGangplank, CreaturePlacement, Nylla, NyllaPackage, 0u, 0, 190.0, 17.628, 120.0, Stationary, 0u, 0u, 0u, "Ranger Nylla (Control Rooms gangplank)"),
            (Guard1, CreaturePlacement, EntranceTrainee, 0u, 0u, 0, 165.0, 41.811, -8.0, CreatureAi, 0u, 0u, 0u, "Entrance guard 1 (keypass Trainee)"),
            (Guard2, CreaturePlacement, EntranceTrainee, 0u, 0u, 0, 161.0, 41.811, -5.0, CreatureAi, 0u, 0u, 0u, "Entrance guard 2 (keypass Trainee)"),
            (Guard3, CreaturePlacement, EntranceTrainee, 0u, 0u, 0, 161.0, 41.811, -11.0, CreatureAi, 0u, 0u, 0u, "Entrance guard 3 (keypass Trainee)"),
            (TarmokPlacement, CreaturePlacement, Tarmok, 0u, 0u, 0, 224.0, 9.865, 60.0, CreatureAi, 0u, 0u, 0u, "Overseer Tarmok (Control Rooms boss)"),
            (Capsule, UsablePlacement, 0u, 0u, 9272u, Destroyable, 224.0, 8.297, 168.0, Stationary, Intact, 1000u, 0u, "Production Fueling Capsule (575/2)"),
            (Production, CreaturePlacement, PrototypeMachina, 0u, 0u, 0, 224.0, 8.297, 168.0, CreatureAi, 0u, 0u, ProductionRespawnMs, "Prototype Forean Machina production"),
            (1430130u, UsablePlacement, 0u, 0u, 6225u, Destroyable, -0.4, 47.688, 298.7, Stationary, Intact, 100u, 0u, "Living Infestation 1, North dish (323)"),
            (1430131u, UsablePlacement, 0u, 0u, 6225u, Destroyable, -5.4, 47.688, 298.7, Stationary, Intact, 100u, 0u, "Living Infestation 2, North dish (323)"),
            (1430132u, UsablePlacement, 0u, 0u, 6225u, Destroyable, -52.1, 31.261, 163.0, Stationary, Intact, 100u, 0u, "Living Infestation 1, Northwest dish (323)"),
            (1430133u, UsablePlacement, 0u, 0u, 6225u, Destroyable, -57.1, 31.261, 163.0, Stationary, Intact, 100u, 0u, "Living Infestation 2, Northwest dish (323)"),
            (1430134u, UsablePlacement, 0u, 0u, 6225u, Destroyable, -44.8, 35.061, -224.1, Stationary, Intact, 100u, 0u, "Living Infestation 1, South dish (323)"),
            (1430135u, UsablePlacement, 0u, 0u, 6225u, Destroyable, -49.8, 35.061, -224.1, Stationary, Intact, 100u, 0u, "Living Infestation 2, South dish (323)")
        };

        /// <summary>The creature packages: creature, package, comment.</summary>
        public static readonly (uint Id, uint Package, string Comment)[] Packages =
        {
            (Johnson, JohnsonPackage, "Information Spec. Johnson (client package 106)"),
            (Nylla, NyllaPackage, "Ranger Nylla (client package 420)"),
            (Parsons, ParsonsPackage, "Lt. Cmdr. Parsons (client package 450)")
        };

        /// <summary>Johnson beside Perkins (the 1.7 live note), and where the world had him.</summary>
        public static readonly (double X, double Y, double Z, string Comment) JohnsonNow =
            (-73.0, 30.55, 179.0, "Information Spec. Johnson (beside Perkins, 1.7)");
        public static readonly (double X, double Y, double Z, string Comment) JohnsonWas =
            (-83.0, 33.47 - 0.276, -132.0, "Information Spec. Johnson (Pravus Research) (Ellatha -83, 33, -132, navmesh floor)");

        public const string PerkinsComment = "Field Lt. Perkins (Pravus, Ellatha /loc)";
        public const string PerkinsWasComment =
            "Field Lt. Perkins (Pravus, north-west fortification) (GUESS: no /loc; the mission says the north-west fortification, and this is the raised walkable ground 40 m west and 20 m north of Price)";

        /// <summary>The missions: id, giver, receiver, level, shareable, comment.</summary>
        public static readonly (uint Id, uint Giver, uint Receiver, uint Level, bool Shareable, string Comment)[] Missions =
        {
            (TheEscapist, Parsons, Nylla, 8u, false, "The Escapist (Wilderness to Pravus)"),
            (MeansOfProduction, Nylla, Nylla, 8u, true, "The Means of Production (Pravus)"),
            (PirateRadio, Johnson, Johnson, 8u, false, "Pirate Radio (Pravus)"),
            (CommunicationControlMachine, Standley, Standley, 10u, false, "Logos: Communication, Control, Machine")
        };

        /// <summary>The client's objective rows with the three server flags: mission, objective, text, required, ordinal, revealed.</summary>
        public static readonly (uint Mission, uint Objective, string Text, bool Required, uint Ordinal, bool Revealed)[] Objectives =
        {
            (TheEscapist, 1u, "Locate Nylla.", true, 1u, true),
            (MeansOfProduction, 3u, "ITEM - Obtain Level 1 Access Keypass", true, 1u, true),
            (MeansOfProduction, 1u, "Find the source of the Forean Machina", true, 2u, true),
            (MeansOfProduction, 2u, "Destroy Fueling Capsule", true, 3u, true),
            // Held: optional and never revealed until Maulis and the ambush exist (GAP-PRAVUS-MAULIS, GAP-PRAVUS-AMBUSH).
            (MeansOfProduction, 4u, "BONUS - Speak with Ranger Maulis in the Prison Cells", false, 4u, false),
            (MeansOfProduction, 5u, "Ambush! Eliminate all remaining Bane in the Machina Chamber", false, 5u, false),
            (PirateRadio, 309u, "Destroy Bane infestation on North communication dish", true, 1u, true),
            (PirateRadio, 310u, "Destroy Bane infestation on Northwest communication dish", true, 2u, true),
            (PirateRadio, 311u, "Destroy Bane infestation on South communication dish", true, 3u, true),
            (CommunicationControlMachine, 6u, "Acquire Logos Information: Communication", true, 1u, true),
            (CommunicationControlMachine, 7u, "Acquire Logos Information: Control", true, 2u, true),
            (CommunicationControlMachine, 8u, "Acquire Logos Information: Machine", true, 3u, true)
        };

        /// <summary>Bindings: mission, objective, binding id, kind, area, placement (or logos row), creature, destroying hit only,
        /// item template, drop chance, counter, comment.</summary>
        public static readonly (uint Mission, uint Objective, byte BindingId, byte Kind, uint Area, uint Placement, uint Creature,
            bool Destroying, uint Item, double Drop, byte Counter, string Comment)[] Bindings =
        {
            (MeansOfProduction, 3u, 0, ItemCollected, 0u, 0u, EntranceTrainee, false, Keypass, 100.0, 0, "575/3 keypass from the entrance Trainees"),
            (MeansOfProduction, 1u, 0, AreaEntered, ChamberArea, 0u, 0u, false, 0u, 0.0, NoCounter, "575/1 enter the Production Chamber"),
            (MeansOfProduction, 2u, 0, Hit, 0u, Capsule, 0u, true, 0u, 0.0, NoCounter, "575/2 destroy the Fueling Capsule"),
            (PirateRadio, 309u, 0, Hit, 0u, 1430130u, 0u, true, 0u, 0.0, 2, "323/309 North dish infestation 1"),
            (PirateRadio, 309u, 1, Hit, 0u, 1430131u, 0u, true, 0u, 0.0, 2, "323/309 North dish infestation 2"),
            (PirateRadio, 310u, 0, Hit, 0u, 1430132u, 0u, true, 0u, 0.0, 1, "323/310 Northwest dish infestation 1"),
            (PirateRadio, 310u, 1, Hit, 0u, 1430133u, 0u, true, 0u, 0.0, 1, "323/310 Northwest dish infestation 2"),
            (PirateRadio, 311u, 0, Hit, 0u, 1430134u, 0u, true, 0u, 0.0, 0, "323/311 South dish infestation 1"),
            (PirateRadio, 311u, 1, Hit, 0u, 1430135u, 0u, true, 0u, 0.0, 0, "323/311 South dish infestation 2"),
            (CommunicationControlMachine, 6u, 0, LogosRecovered, 0u, 51u, 0u, false, 0u, 0.0, NoCounter, "924/6 the Communication shrine"),
            (CommunicationControlMachine, 7u, 0, LogosRecovered, 0u, 41u, 0u, false, 0u, 0.0, NoCounter, "924/7 the Control shrine"),
            (CommunicationControlMachine, 8u, 0, LogosRecovered, 0u, 34u, 0u, false, 0u, 0.0, NoCounter, "924/8 the Machine shrine")
        };

        /// <summary>Counters: mission, objective, counter id (the client's counter index carrying the text), target.</summary>
        public static readonly (uint Mission, uint Objective, uint Counter, int Target)[] Counters =
        {
            (MeansOfProduction, 3u, 0u, 1),
            (PirateRadio, 309u, 2u, 2),
            (PirateRadio, 310u, 1u, 2),
            (PirateRadio, 311u, 0u, 2)
        };

        /// <summary>TaRapedia's amounts, all recorded before Update 1.4 (GAP-REWARD-ERA); no item lists.</summary>
        public static readonly (uint Mission, byte Type, int Amount)[] Rewards =
        {
            (TheEscapist, Experience, 4000), (TheEscapist, Credits, 1200),
            (MeansOfProduction, Experience, 16000), (MeansOfProduction, Credits, 2000),
            (PirateRadio, Experience, 8000), (PirateRadio, Credits, 1200),
            (CommunicationControlMachine, Experience, 22000), (CommunicationControlMachine, Credits, 3000)
        };

        /// <summary>575 follows 593 (TaRapedia rev 15146). 593's own prerequisite 574 is held (OD-139).</summary>
        public static readonly (uint Mission, uint Required, string Comment)[] Prerequisites =
        {
            (MeansOfProduction, TheEscapist, "The Escapist completed")
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
            // ── the population ──
            InsertTyped(migrationBuilder, "creature", CreatureColumns, CreatureTypes,
                Rows(Creatures.Select(c => new object[] { c.Id, c.Comment, c.Class, 0u, c.Level, c.Hp, c.NameId, 9.0, 5.0,
                    c.Action, 0u, 0u, 0u, 0u, 0u, 0u, 0u })));

            InsertTyped(migrationBuilder, "spawnpool", PoolColumns, PoolTypes, Rows(Pools.Select(PoolRow)));

            InsertTyped(migrationBuilder, "content_area", AreaColumns, AreaTypes, Rows(new[]
            {
                // a vertical cylinder over the chamber floor (the room module spans about +/-47 m)
                new object[] { ChamberArea, Pravus, (byte)2, 224.0, 8.297, 168.0, 40.0, 20.0, "575/1 the Production Chamber" }
            }));

            InsertTyped(migrationBuilder, "content_placement", PlacementColumns, PlacementTypes,
                Rows(Placements.Select(p => new object[] { p.Id, Pravus, p.Kind, p.Creature, p.Package, p.Class, p.Usable, p.X, p.Y, p.Z,
                    0.0, p.Behavior, p.State, 0u, 0u, 0u, 0u, p.HitPoints, 0u, 0u, 0u, p.RespawnMs, 0u, 0u, 0u, p.Comment })));

            // ── the NPC fixes ──
            InsertTyped(migrationBuilder, "npc_package", new[] { "id", "package_id", "comment" }, new[] { "INTEGER", "INTEGER", "varchar(50)" },
                Rows(Packages.Select(p => new object[] { p.Id, p.Package, p.Comment })));
            UpdatePlacement(migrationBuilder, Johnson, new[] { "pos_x", "pos_y", "pos_z", "npc_package_id", "comment" },
                new[] { "REAL", "REAL", "REAL", "INTEGER", "varchar(50)" },
                new object[] { JohnsonNow.X, JohnsonNow.Y, JohnsonNow.Z, JohnsonPackage, JohnsonNow.Comment });
            UpdatePlacement(migrationBuilder, Perkins, new[] { "comment" }, new[] { "varchar(50)" }, new object[] { PerkinsComment });

            // ── the missions ──
            InsertTyped(migrationBuilder, "npc_mission", MissionColumns, MissionTypes,
                Rows(Missions.Select(m => new object[] { m.Id, m.Giver, m.Receiver, m.Level, 1u, GeneralCategory, m.Shareable, false, m.Comment })));

            DeleteTyped(migrationBuilder, "npc_mission_objective", new[] { "mission_id", "objective_id" }, Integers(2),
                Rows(Objectives.Select(o => new object[] { o.Mission, o.Objective })));
            InsertTyped(migrationBuilder, "npc_mission_objective", ObjectiveColumns, ObjectiveTypes,
                Rows(Objectives.Select(o => new object[] { o.Mission, o.Objective, o.Text, o.Required, o.Ordinal, o.Revealed })));

            InsertTyped(migrationBuilder, "npc_mission_objective_binding", BindingColumns, BindingTypes,
                Rows(Bindings.Select(b => new object[]
                {
                    b.Mission, b.Objective, b.BindingId, b.Kind, b.Area, b.Placement, b.Creature, 0u, b.Destroying, (byte)0, b.Item, b.Drop,
                    0u, 0u, b.Counter, b.Comment
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
                Rows(Bindings.Select(b => new object[] { b.Mission, b.Objective, b.BindingId })));

            // The client skeleton rows come back with their text and all three server flags unknown (NULL).
            DeleteTyped(migrationBuilder, "npc_mission_objective", new[] { "mission_id", "objective_id" }, Integers(2),
                Rows(Objectives.Select(o => new object[] { o.Mission, o.Objective })));
            InsertTyped(migrationBuilder, "npc_mission_objective", ObjectiveColumns, ObjectiveTypes,
                Rows(Objectives.Select(o => new object[] { o.Mission, o.Objective, o.Text, null, null, null })));

            DeleteTyped(migrationBuilder, "npc_mission", new[] { "id" }, Integers(1), Rows(Missions.Select(m => new object[] { m.Id })));

            UpdatePlacement(migrationBuilder, Perkins, new[] { "comment" }, new[] { "varchar(50)" }, new object[] { PerkinsWasComment });
            UpdatePlacement(migrationBuilder, Johnson, new[] { "pos_x", "pos_y", "pos_z", "npc_package_id", "comment" },
                new[] { "REAL", "REAL", "REAL", "INTEGER", "varchar(50)" },
                new object[] { JohnsonWas.X, JohnsonWas.Y, JohnsonWas.Z, 0u, JohnsonWas.Comment });
            DeleteTyped(migrationBuilder, "npc_package", new[] { "id" }, Integers(1), Rows(Packages.Select(p => new object[] { p.Id })));

            DeleteTyped(migrationBuilder, "content_placement", new[] { "id" }, Integers(1), Rows(Placements.Select(p => new object[] { p.Id })));
            DeleteTyped(migrationBuilder, "content_area", new[] { "id" }, Integers(1), Rows(new[] { new object[] { ChamberArea } }));
            DeleteTyped(migrationBuilder, "spawnpool", new[] { "id" }, Integers(1), Rows(Pools.Select(p => new object[] { p.Id })));
            DeleteTyped(migrationBuilder, "creature", new[] { "id" }, Integers(1), Rows(Creatures.Select(c => new object[] { c.Id })));
        }

        private static object[] PoolRow((uint Id, byte Anim, double X, double Y, double Z, (uint Creature, byte Count)[] Slots) pool)
        {
            var row = new List<object> { pool.Id, (byte)0, pool.Anim, PoolRespawn, pool.X, pool.Y, pool.Z, 0.0, Pravus };
            for (var i = 0; i < 6; i++)
            {
                if (i < pool.Slots.Length)
                    row.AddRange(new object[] { pool.Slots[i].Creature, pool.Slots[i].Count, pool.Slots[i].Count });
                else
                    row.AddRange(new object[] { 0u, (byte)0, (byte)0 });
            }
            return row.ToArray();
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
