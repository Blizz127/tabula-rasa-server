using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// Concordia Palisades (map 1244): the five missions of the 2026-09-27 Palisades dossier
    /// (research/20260927-palisades-dossiers) that can be finished in this world as it stands, with the dossier's
    /// conservative defaults (OD-153 to OD-160, the dossier's OD-P1 to OD-P7 plus the named-boss respawn).
    ///
    /// <b>What is seeded.</b>
    ///   * <b>1812 Logos: True</b> and <b>1813 Logos: Through</b>: Receptive Liaison Arizpe (199085) gives and takes back,
    ///     as he does 1652 Ground (LiaisonLogosMissions). One client objective each (18, 19), bound by LogosRecovered to
    ///     the world's logos rows 333 True and 322 Through, the client's logosstone constants, each on a final-map
    ///     ArchElohLogosDispenserBase and joined to the Palisades navmesh. 9,000 experience and 1,800 credits each,
    ///     TaRapedia's amounts first written 2008-01-05: pre-1.4, labelled (OD-159). Arizpe keeps his pre-D12 reading in
    ///     the ruins of the old research facility (OD-153, GAP-NEW-CUMBRIA-ARIZPE).
    ///   * <b>1988 A Spiritual Pilgrimage</b>: Warden Brocail (199102) gives and takes back (the D10 live note and the
    ///     client log). Objectives 1-3 bound by LogosRecovered to Knowledge 208, Man 219 and Planet 266. 20,000
    ///     experience, 3,000 credits and a choice of 122719/122720/122721, all post-1.4 (TaRapedia rev 31548,
    ///     2008-07-29). The Logos doors TaRapedia describes are not in the map or the emulator (GAP-PALISADES-LOGOS-DOORS);
    ///     the Man shrine's gorge floor is a navmesh island, which binds NPCs only (GAP-PALISADES-MAN-CAVE-NAVMESH).
    ///   * <b>2014 Crash Course</b> (added at D12): Base Cmdr. Matlin (199053) gives and takes back. Objective 1 completes
    ///     on Derac Bensen Corman's client package 136 and objective 6 on Matlin's 1214, so the two get npc_package rows;
    ///     objective 2 is Executor Gantic's Datapad (template 123352, the only template of item class 29949
    ///     MisPalisadesCrashCourseDatapad), dropped by Gantic (199054). Objectives 2 and 6 are revealed in the client's
    ///     order (1 -> 2 -> 6). TaRapedia records 0 experience and 0 credits, so no amount row is seeded
    ///     (GAP-2014-REWARD-ZERO); the post-D12 item choice 130322/130323/130324 is. The companion "Conner" is left out
    ///     (GAP-2014-CONNER-COMPANION).
    ///   * <b>1795 Bloody Booty</b>: Sergeant Mullen (510135) gives and takes back. Barbrix's Boot (template 118803, item
    ///     class 28451 MisPalisadesItemBaneboot) dropped by Barbrix (199095). 19,000 experience and 2,850 credits,
    ///     pre-1.4 (OD-159); the pre-1.4 item list is not seeded.
    ///   * Mission levels: the giver's level (OD-155, the OD-100/OD-140 rule), an analogue: Arizpe 25, Brocail 15, Matlin
    ///     30 and Mullen 20 (TaRapedia's NPC pages; Mullen's world row still says the upstream 10).
    ///   * Gantic and Barbrix keep their harmless stats (OD-156: the kill completes without them fighting back,
    ///     GAP-PALISADES-CREATURE-STATS), and their placements respawn 60 s after death, so every player on the shared
    ///     map can finish 2014 and 1795 (OD-160, the OD-48/OD-55 delay; no source records the original).
    ///   * <b>Package 134</b> (Corporal Orton's: 331 objective 1, 337 objective 3) moves from the upstream duplicate
    ///     510196 on Valverde Pools to the Palisades Orton 199086; the duplicate's spawnpool stops drawing. Client
    ///     missiontext 163 puts Orton "from Cumbria Research" (OD-158). Down puts both back.
    ///   * <b>Teleporter 624</b>'s server-side description "I thnk Viands Village" becomes the client's own label for that
    ///     waypoint, "Waypoint: Viands Village" (uimapmarker 133182640965832, 3.2 m from it).
    ///
    /// <b>Held</b> (docs/evidence/bootcamp-d11-reconstruction-manifest.json omissions): 1799, 1800 and 1801 (Skive Base
    /// holds no Bane, OD-154), 337 (Kaven Corman stands in the ruins, OD-153), the 366 prerequisite on 1988 (a change to
    /// a live mission, left for the owner), and every mission the dossier holds. 368 is not changed (OD-157). Aldrin
    /// keeps spawning: the D12 note removed his missions, not him (OD-153).
    /// </summary>
    public static class PalisadesDossierMissionsRows
    {
        public const string Migration = "PalisadesDossierMissions";

        public const uint Palisades = 1244u;

        public const uint LogosTrue = 1812u;
        public const uint LogosThrough = 1813u;
        public const uint SpiritualPilgrimage = 1988u;
        public const uint CrashCourse = 2014u;
        public const uint BloodyBooty = 1795u;

        public const uint Arizpe = 199085u;
        public const uint Brocail = 199102u;
        public const uint Matlin = 199053u;
        public const uint Derac = 199087u;
        public const uint Gantic = 199054u;
        public const uint Mullen = 510135u;
        public const uint Barbrix = 199095u;
        public const uint Orton = 199086u;
        public const uint OrtonOnThePools = 510196u;

        public const uint DeracPackage = 136u;
        public const uint MatlinPackage = 1214u;
        public const uint OrtonPackage = 134u;

        public const uint GanticDatapad = 123352u;
        public const uint BarbrixBoot = 118803u;

        /// <summary>OD-160: the respawn delay of the two named bosses (OD-48/OD-55's 60 s).</summary>
        public const uint NamedBossRespawnMs = 60000u;

        public const uint ViandsVillageWaypoint = 624u;
        public const string ViandsVillageWas = "I thnk Viands Village";
        public const string ViandsVillageLabel = "Waypoint: Viands Village";

        private const uint GeneralCategory = 10000001u;
        private const byte Credits = 1;
        private const byte Experience = 3;
        private const byte SelectableItem = 5;
        private const byte LogosRecoveredBinding = 8;
        private const byte ItemCollectedBinding = 9;
        private const byte NoCounter = 255;

        private static readonly string[] MissionColumns =
            { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" };
        private static readonly string[] MissionTypes =
            { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "varchar(50)" };
        private static readonly string[] ObjectiveColumns =
            { "mission_id", "objective_id", "comment", "is_required", "ordinal", "revealed_on_accept" };
        private static readonly string[] ObjectiveTypes = { "INTEGER", "INTEGER", "varchar(100)", "INTEGER", "INTEGER", "INTEGER" };
        private static readonly string[] TransitionColumns = { "mission_id", "completed_objective_id", "revealed_objective_id" };
        private static readonly string[] BindingColumns =
        {
            "mission_id", "objective_id", "binding_id", "kind", "area_id", "placement_id", "creature_id", "action_id",
            "destroying_hit_only", "equip_match", "item_template_id", "drop_chance", "item_set_id", "target_state", "counter_id", "comment"
        };
        private static readonly string[] BindingTypes =
        {
            "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER",
            "INTEGER", "INTEGER", "INTEGER", "double", "INTEGER", "INTEGER", "INTEGER", "varchar(50)"
        };
        private static readonly string[] CounterColumns = { "mission_id", "objective_id", "counter_id", "initial_value", "target_value" };
        private static readonly string[] RewardColumns = { "id", "type", "credits", "item_template_id", "quantity" };
        private static readonly string[] RewardKeyColumns = { "id", "type", "item_template_id" };
        private static readonly string[] PackageColumns = { "id", "package_id", "comment" };
        private static readonly string[] PackageTypes = { "INTEGER", "INTEGER", "varchar(50)" };

        /// <summary>(creature, client package, comment): the packages this migration binds.</summary>
        public static readonly (uint Creature, uint Package, string Comment)[] Packages =
        {
            (Derac, DeracPackage, "Derac Bensen Corman (client package 136)"),
            (Matlin, MatlinPackage, "Base Cmdr. Matlin (client package 1214)"),
            (Orton, OrtonPackage, "Corporal Orton (client package 134)")
        };

        /// <summary>The upstream package row of the Pools duplicate, removed here and restored on rollback.</summary>
        public static readonly object[] OrtonOnThePoolsPackage = { OrtonOnThePools, OrtonPackage, "Corporal Orton" };

        /// <summary>(mission, giver, receiver, level (analogue, OD-155), comment).</summary>
        public static readonly (uint Id, uint Giver, uint Receiver, uint Level, string Comment)[] Missions =
        {
            (LogosTrue, Arizpe, Arizpe, 25u, "Logos: True (Palisades)"),
            (LogosThrough, Arizpe, Arizpe, 25u, "Logos: Through (Palisades)"),
            (SpiritualPilgrimage, Brocail, Brocail, 15u, "A Spiritual Pilgrimage (Palisades)"),
            (CrashCourse, Matlin, Matlin, 30u, "Crash Course (Palisades)"),
            (BloodyBooty, Mullen, Mullen, 20u, "Bloody Booty (Palisades)")
        };

        /// <summary>(mission, client objective, ordinal, revealed on acceptance, the client's objective text). All required.</summary>
        public static readonly (uint Mission, uint Objective, uint Ordinal, bool Revealed, string Text)[] Objectives =
        {
            (LogosTrue, 18u, 1u, true, "Acquire Logos Information: True"),
            (LogosThrough, 19u, 1u, true, "Acquire Logos Information: Through"),
            (SpiritualPilgrimage, 1u, 1u, true, "Recover the Knowledge Logos"),
            (SpiritualPilgrimage, 2u, 2u, true, "Recover the Man Logos"),
            (SpiritualPilgrimage, 3u, 3u, true, "Recover the Planet Logos"),
            (CrashCourse, 1u, 1u, true, "Talk to Derac"),
            (CrashCourse, 2u, 2u, false, "Kill Gantic And Retrieve His Datapad"),
            (CrashCourse, 6u, 3u, false, "Return to Commander Matlin"),
            (BloodyBooty, 1u, 1u, true, "Collect Barbrix's Boot")
        };

        /// <summary>2014 reveals its steps in the client's order: Derac, then Gantic, then Matlin.</summary>
        public static readonly (uint Mission, uint Completed, uint Revealed)[] Transitions =
        {
            (CrashCourse, 1u, 2u),
            (CrashCourse, 2u, 6u)
        };

        /// <summary>(mission, objective, the `logos` row of the shrine, its word). The client's logosstone constants on 1244.</summary>
        public static readonly (uint Mission, uint Objective, uint Logos, string Word)[] Shrines =
        {
            (LogosTrue, 18u, 333u, "True"),
            (LogosThrough, 19u, 322u, "Through"),
            (SpiritualPilgrimage, 1u, 208u, "Knowledge"),
            (SpiritualPilgrimage, 2u, 219u, "Man"),
            (SpiritualPilgrimage, 3u, 266u, "Planet")
        };

        /// <summary>(mission, objective, creature that drops it, item template, drop chance %, comment). Counter 0, one item.</summary>
        public static readonly (uint Mission, uint Objective, uint Creature, uint Item, double Chance, string Comment)[] Drops =
        {
            (CrashCourse, 2u, Gantic, GanticDatapad, 100.0, "2014/2 Executor Gantic's Datapad"),
            (BloodyBooty, 1u, Barbrix, BarbrixBoot, 100.0, "1795/1 Barbrix's Boot")
        };

        /// <summary>Experience and credits (TaRapedia; 1812/1813/1795 pre-1.4, 1988 post-1.4). 2014 has none on purpose.</summary>
        public static readonly (uint Mission, byte Type, int Amount)[] Amounts =
        {
            (LogosTrue, Experience, 9000),
            (LogosTrue, Credits, 1800),
            (LogosThrough, Experience, 9000),
            (LogosThrough, Credits, 1800),
            (SpiritualPilgrimage, Experience, 20000),
            (SpiritualPilgrimage, Credits, 3000),
            (BloodyBooty, Experience, 19000),
            (BloodyBooty, Credits, 2850)
        };

        /// <summary>Post-1.4 choose-one item rewards, each a single consecutive client template run.</summary>
        public static readonly (uint Mission, uint Template)[] Items =
        {
            (SpiritualPilgrimage, 122719u),
            (SpiritualPilgrimage, 122720u),
            (SpiritualPilgrimage, 122721u),
            (CrashCourse, 130322u),
            (CrashCourse, 130323u),
            (CrashCourse, 130324u)
        };

        /// <summary>The named-boss placements (TarapediaMissingNpcBatch) that respawn after OD-160's delay.</summary>
        public static readonly uint[] RespawningPlacements = { Gantic, Barbrix };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            InsertTyped(migrationBuilder, "npc_package", PackageColumns, PackageTypes,
                Rows(Packages.Select(p => new object[] { p.Creature, p.Package, p.Comment })));

            // The Pools duplicate of Corporal Orton gives up package 134 and stops spawning; its rows stay.
            DeleteTyped(migrationBuilder, "npc_package", new[] { "id" }, new[] { "INTEGER" }, Rows(new[] { new object[] { OrtonOnThePools } }));
            SpawnpoolCounts(migrationBuilder, 0);

            // group_type 1, general category, not shareable, not radio-completable: the reconstructed-mission values.
            InsertTyped(migrationBuilder, "npc_mission", MissionColumns, MissionTypes,
                Rows(Missions.Select(m => new object[] { m.Id, m.Giver, m.Receiver, m.Level, (byte)1, GeneralCategory, false, false, m.Comment })));

            // The client skeleton rows are replaced by the same rows with the three flags set.
            DeleteTyped(migrationBuilder, "npc_mission_objective", new[] { "mission_id", "objective_id" }, new[] { "INTEGER", "INTEGER" },
                Rows(Objectives.Select(o => new object[] { o.Mission, o.Objective })));
            InsertTyped(migrationBuilder, "npc_mission_objective", ObjectiveColumns, ObjectiveTypes,
                Rows(Objectives.Select(o => new object[] { o.Mission, o.Objective, o.Text, true, o.Ordinal, o.Revealed })));

            InsertTyped(migrationBuilder, "npc_mission_objective_transition", TransitionColumns, new[] { "INTEGER", "INTEGER", "INTEGER" },
                Rows(Transitions.Select(t => new object[] { t.Mission, t.Completed, t.Revealed })));

            InsertTyped(migrationBuilder, "npc_mission_objective_binding", BindingColumns, BindingTypes,
                Rows(Shrines.Select(s => new object[]
                {
                    s.Mission, s.Objective, (byte)0, LogosRecoveredBinding, 0u, s.Logos, 0u, 0u, false, (byte)0, 0u, 0d, 0u, 0u,
                    NoCounter, $"{s.Mission}/{s.Objective} the {s.Word} shrine"
                }).Concat(Drops.Select(d => new object[]
                {
                    d.Mission, d.Objective, (byte)0, ItemCollectedBinding, 0u, 0u, d.Creature, 0u, false, (byte)0, d.Item, d.Chance, 0u, 0u,
                    (byte)0, d.Comment
                }))));

            InsertTyped(migrationBuilder, "npc_mission_objective_counter", CounterColumns, Integers(5),
                Rows(Drops.Select(d => new object[] { d.Mission, d.Objective, 0u, 0, 1 })));

            InsertTyped(migrationBuilder, "npc_mission_reward", RewardColumns, Integers(5),
                Rows(Amounts.Select(a => new object[] { a.Mission, a.Type, a.Amount, 0u, 0u })
                    .Concat(Items.Select(i => new object[] { i.Mission, SelectableItem, 0, i.Template, 1u }))));

            foreach (var placement in RespawningPlacements)
                PlacementRespawn(migrationBuilder, placement, NamedBossRespawnMs);

            TeleporterDescription(migrationBuilder, ViandsVillageLabel);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            TeleporterDescription(migrationBuilder, ViandsVillageWas);

            foreach (var placement in RespawningPlacements)
                PlacementRespawn(migrationBuilder, placement, 0u);

            DeleteTyped(migrationBuilder, "npc_mission_reward", RewardKeyColumns, Integers(3),
                Rows(Amounts.Select(a => new object[] { a.Mission, a.Type, 0u })
                    .Concat(Items.Select(i => new object[] { i.Mission, SelectableItem, i.Template }))));

            DeleteTyped(migrationBuilder, "npc_mission_objective_counter", new[] { "mission_id", "objective_id", "counter_id" }, Integers(3),
                Rows(Drops.Select(d => new object[] { d.Mission, d.Objective, 0u })));

            DeleteTyped(migrationBuilder, "npc_mission_objective_binding", new[] { "mission_id", "objective_id", "binding_id" }, Integers(3),
                Rows(Shrines.Select(s => new object[] { s.Mission, s.Objective, (byte)0 })
                    .Concat(Drops.Select(d => new object[] { d.Mission, d.Objective, (byte)0 }))));

            DeleteTyped(migrationBuilder, "npc_mission_objective_transition", TransitionColumns, Integers(3),
                Rows(Transitions.Select(t => new object[] { t.Mission, t.Completed, t.Revealed })));

            // The client skeleton rows come back with their text and all three server flags unknown (NULL).
            DeleteTyped(migrationBuilder, "npc_mission_objective", new[] { "mission_id", "objective_id" }, new[] { "INTEGER", "INTEGER" },
                Rows(Objectives.Select(o => new object[] { o.Mission, o.Objective })));
            InsertTyped(migrationBuilder, "npc_mission_objective", ObjectiveColumns, ObjectiveTypes,
                Rows(Objectives.Select(o => new object[] { o.Mission, o.Objective, o.Text, null, null, null })));

            DeleteTyped(migrationBuilder, "npc_mission", new[] { "id" }, new[] { "INTEGER" },
                Rows(Missions.Select(m => new object[] { m.Id })));

            SpawnpoolCounts(migrationBuilder, 1);
            InsertTyped(migrationBuilder, "npc_package", PackageColumns, PackageTypes, Rows(new[] { OrtonOnThePoolsPackage }));

            DeleteTyped(migrationBuilder, "npc_package", new[] { "id" }, new[] { "INTEGER" },
                Rows(Packages.Select(p => new object[] { p.Creature })));
        }

        /// <summary>Spawnpool 510196's first slot: 0/0 retires the Pools Orton, 1/1 is MissionNpcSpawnpoolPreloader's value.</summary>
        private static void SpawnpoolCounts(MigrationBuilder builder, byte count)
        {
            builder.UpdateData("spawnpool", new[] { "id" }, new object[] { OrtonOnThePools },
                new[] { "creature_1_min_count", "creature_1_max_count" }, new object[] { count, count });
            var update = (UpdateDataOperation)builder.Operations.Last();
            update.KeyColumnTypes = new[] { "INTEGER" };
            update.ColumnTypes = new[] { "INTEGER", "INTEGER" };
        }

        private static void PlacementRespawn(MigrationBuilder builder, uint placement, uint respawnMs)
        {
            builder.UpdateData("content_placement", new[] { "id" }, new object[] { placement },
                new[] { "respawn_ms" }, new object[] { respawnMs });
            var update = (UpdateDataOperation)builder.Operations.Last();
            update.KeyColumnTypes = new[] { "INTEGER" };
            update.ColumnTypes = new[] { "INTEGER" };
        }

        private static void TeleporterDescription(MigrationBuilder builder, string description)
        {
            builder.UpdateData("teleporter", new[] { "id" }, new object[] { ViandsVillageWaypoint },
                new[] { "description" }, new object[] { description });
            var update = (UpdateDataOperation)builder.Operations.Last();
            update.KeyColumnTypes = new[] { "INTEGER" };
            update.ColumnTypes = new[] { "varchar(64)" };
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
