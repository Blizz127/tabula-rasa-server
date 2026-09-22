using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Memory;
using Rasa.Structures;
using Rasa.Structures.Content;
using Rasa.Structures.World;

namespace Rasa.Test
{
    /// <summary>
    /// The map and radar markers an objective carries, derived from where the world already says that objective
    /// is finished (MissionMapIndicators). The client draws one MISSION_INDICATOR widget per entry of the
    /// objective's indicatorList and hides it the moment the objective completes
    /// (mapwindow.HandleUpdateMissionIndicators / radarwindow, 1.16.5.0), so everything here is about what goes
    /// into that list.
    /// </summary>
    [TestClass]
    public class MissionMapIndicatorTests
    {
        private const uint MissionId = 900100;
        private const uint Map = 1220;
        private const uint OtherMap = 1221;

        private static Mission Definition(params uint[] objectiveIds)
        {
            var mission = new Mission(new NpcMissionEntry { Id = MissionId, GiverId = 7001, ReciverId = 7002 });

            foreach (var objectiveId in objectiveIds)
                mission.Objectives[objectiveId] = new MissionObjectiveDefinition
                {
                    ObjectiveId = objectiveId,
                    Ordinal = objectiveId,
                    IsRequired = true,
                    RevealedOnAccept = objectiveId == objectiveIds[0]
                };

            return mission;
        }

        private static NpcMissionObjectiveBindingEntry Binding(uint objectiveId, ObjectiveBindingKind kind,
            uint areaId = 0, uint placementId = 0, uint creatureId = 0)
        {
            return new NpcMissionObjectiveBindingEntry
            {
                MissionId = MissionId,
                ObjectiveId = objectiveId,
                BindingId = 1,
                Kind = (byte)kind,
                AreaId = areaId,
                PlacementId = placementId,
                CreatureId = creatureId
            };
        }

        private static ContentPlacementEntry Placement(uint id, uint creatureId, double x, double z, uint map = Map, uint package = 0)
        {
            return new ContentPlacementEntry
            {
                Id = id,
                MapContextId = map,
                Kind = (byte)ContentPlacementKind.Creature,
                CreatureId = creatureId,
                NpcPackageId = package,
                PosX = x,
                PosY = 100,
                PosZ = z
            };
        }

        private static SpawnPoolEntry Pool(uint id, uint creatureId, double x, double z, uint map = Map)
        {
            return new SpawnPoolEntry { Id = id, MapContextId = map, Creature1Id = creatureId, PosX = x, PosY = 100, PosZ = z };
        }

        [TestMethod]
        public void AConversationObjectiveIsMarkedWhereTheCreatureCarryingItsPackageStands()
        {
            var mission = Definition(1);
            mission.ObjectiveConversations.Add(new MissionObjectiveConversation { ObjectiveId = 1, NpcPackageId = 550, ConvoType = 1 });

            var world = MissionIndicatorWorld.Build(
                null,
                new[] { Placement(10, 7003, 40, -60) },
                null,
                null,
                new[] { new NpcPackageEntry { Id = 7003, PackageId = 550 } });

            var marker = MissionMapIndicators.Derive(mission, world)[1].Single();

            Assert.AreEqual(MissionIndicatorSource.Conversation, marker.Source);
            Assert.AreEqual((Map, 40f, -60f), (marker.MapContextId, marker.Position.X, marker.Position.Z));
            // No label id: the client captions the marker with the objective's own text instead
            // (mapwindow.OnMissionMarkerHighlighted line 797).
            Assert.IsNull(marker.IndicatorId);
            // Map and radar only, as the stored boot-camp rows are; bShow3DEffect is what spawns the world beam
            // (missionlog._UpdateIndicators line 579).
            Assert.IsFalse(marker.Show3DEffect);
            Assert.AreEqual(0, marker.Radius);
        }

        [TestMethod]
        public void APlacementsOwnPackageOverridesTheNpcPackageTable()
        {
            var mission = Definition(1);
            mission.ObjectiveConversations.Add(new MissionObjectiveConversation { ObjectiveId = 1, NpcPackageId = 550 });

            // CreatureManager.CreatePlacedCreature lets a placement carry its own package; that placement is the
            // creature the player talks to, so it is marked even though the npc_package row names another body.
            var world = MissionIndicatorWorld.Build(
                null,
                new[] { Placement(10, 7009, 5, 5, package: 550), Placement(11, 7003, 400, 400) },
                null,
                null,
                new[] { new NpcPackageEntry { Id = 7003, PackageId = 550 } });

            var markers = MissionMapIndicators.Derive(mission, world)[1];

            Assert.AreEqual(5f, markers.First().Position.X);
            Assert.AreEqual(2, markers.Count);
        }

        [TestMethod]
        public void AnAreaObjectiveIsMarkedAtTheTriggerCentreWithItsRadius()
        {
            var mission = Definition(1);
            mission.Bindings.Add(Binding(1, ObjectiveBindingKind.AreaEntered, areaId: 30));

            var world = MissionIndicatorWorld.Build(
                new[] { new ContentAreaEntry { Id = 30, MapContextId = Map, PosX = 10, PosY = 20, PosZ = 30, Radius = 25 } },
                null, null, null, null);

            var marker = MissionMapIndicators.Derive(mission, world)[1].Single();

            Assert.AreEqual(MissionIndicatorSource.Area, marker.Source);
            // Over 1, so the map draws the circle the player has to reach (OnMissionMarkerHighlighted line 789).
            Assert.AreEqual(25, marker.Radius);
        }

        [TestMethod]
        public void ALogosObjectiveIsMarkedAtTheShrine()
        {
            var mission = Definition(1);
            // A shrine is not a content placement: LogosRecovered carries the logos row id in placement_id.
            mission.Bindings.Add(Binding(1, ObjectiveBindingKind.LogosRecovered, placementId: 23));

            var world = MissionIndicatorWorld.Build(null, null,
                new[] { new LogosEntry { Id = 23, MapContextId = Map, PosX = 1, PosY = 2, PosZ = 3, Name = "Attack" } },
                null, null);

            var marker = MissionMapIndicators.Derive(mission, world)[1].Single();

            Assert.AreEqual(MissionIndicatorSource.Logos, marker.Source);
            Assert.AreEqual((1f, 2f, 3f), (marker.Position.X, marker.Position.Y, marker.Position.Z));
        }

        [TestMethod]
        public void AUseObjectiveIsMarkedAtTheUsable()
        {
            var mission = Definition(1);
            mission.Bindings.Add(Binding(1, ObjectiveBindingKind.UseCompleted, placementId: 40));

            var world = MissionIndicatorWorld.Build(null,
                new[] { new ContentPlacementEntry { Id = 40, MapContextId = Map, Kind = (byte)ContentPlacementKind.Usable, PosX = 7, PosZ = 8 } },
                null, null, null);

            var marker = MissionMapIndicators.Derive(mission, world)[1].Single();

            Assert.AreEqual(MissionIndicatorSource.Placement, marker.Source);
            Assert.AreEqual((7f, 8f), (marker.Position.X, marker.Position.Z));
        }

        [TestMethod]
        public void AKillObjectiveIsMarkedOnlyWhereThisWorldSpawnsItsTargets()
        {
            var unspawned = Definition(1);
            unspawned.Bindings.Add(Binding(1, ObjectiveBindingKind.Kill, creatureId: 8100));

            // No pool holds 8100, so the objective is left unmarked rather than pointed somewhere invented.
            Assert.AreEqual(0, MissionMapIndicators.Derive(unspawned, MissionIndicatorWorld.Empty).Count);

            var mission = Definition(1);
            mission.Bindings.Add(Binding(1, ObjectiveBindingKind.Kill, creatureId: 8100));

            var pools = Enumerable.Range(0, MissionMapIndicators.MaxKillMarkers + 4)
                .Select(index => Pool((uint)(600 + index), 8100, index * 50, 0))
                .ToList();

            var markers = MissionMapIndicators.Derive(mission, MissionIndicatorWorld.Build(null, null, null, pools, null))[1];

            Assert.AreEqual(MissionIndicatorSource.Kill, markers[0].Source);
            // A creature that spawns in more places than the cap is ambient, not a landmark.
            Assert.AreEqual(MissionMapIndicators.MaxKillMarkers, markers.Count);
        }

        [TestMethod]
        public void AnEquipObjectiveIsLeftUnmarked()
        {
            var mission = Definition(1);
            mission.Bindings.Add(Binding(1, ObjectiveBindingKind.Equip));

            Assert.AreEqual(0, MissionMapIndicators.Derive(mission, MissionIndicatorWorld.Empty).Count);
        }

        [TestMethod]
        public void TheSamePositionIsNotMarkedTwice()
        {
            var mission = Definition(1);
            mission.ObjectiveConversations.Add(new MissionObjectiveConversation { ObjectiveId = 1, NpcPackageId = 550, ConvoType = 1 });
            mission.ObjectiveConversations.Add(new MissionObjectiveConversation { ObjectiveId = 1, NpcPackageId = 550, ConvoType = 2 });

            // The creature stands in a placement and in a pool at the same spot (a world seed and its content copy).
            var world = MissionIndicatorWorld.Build(null,
                new[] { Placement(10, 7003, 40, -60) },
                null,
                new[] { Pool(600, 7003, 40.5, -60.2) },
                new[] { new NpcPackageEntry { Id = 7003, PackageId = 550 } });

            Assert.AreEqual(1, MissionMapIndicators.Derive(mission, world)[1].Count);
        }

        [TestMethod]
        public void OnlyTheMarkersOfThePlayersOwnMapAreSent()
        {
            // The client's indicator tuple carries no map and mapwindow._PlaceWidget places every marker with the
            // open map's offset and scale, so a marker for another map would be drawn at a wrong spot on this one.
            var definition = Definition(1);
            definition.DerivedIndicators[1] = new List<MissionIndicator>
            {
                new MissionIndicator { MapContextId = Map, Position = new System.Numerics.Vector3(1, 0, 1) },
                new MissionIndicator { MapContextId = OtherMap, Position = new System.Numerics.Vector3(2, 0, 2) }
            };

            var progress = new PlayerMission { MissionId = MissionId, State = MissionState.Active };
            progress.Objectives[1] = MissionObjectiveState.Incomplete;

            var here = progress.ToMissionInfo(definition, 0, Map).ObjectivesList.Single().IndicatorList;
            Assert.AreEqual(1, here.Count);
            Assert.AreEqual(Map, here.Single().MapContextId);

            // 0 means the map is not known and filters nothing.
            Assert.AreEqual(2, progress.ToMissionInfo(definition, 0).ObjectivesList.Single().IndicatorList.Count);
        }

        [TestMethod]
        public void AStoredIndicatorRowWinsOverTheDerivedOnes()
        {
            var definition = Definition(1);
            definition.Indicators[1] = new List<NpcMissionObjectiveIndicatorEntry>
            {
                new NpcMissionObjectiveIndicatorEntry { MissionId = MissionId, ObjectiveId = 1, IndicatorIndex = 0, IndicatorId = 430, PosX = 388.95, PosY = 131.3, PosZ = -26.28 }
            };
            definition.DerivedIndicators[1] = new List<MissionIndicator> { new MissionIndicator { MapContextId = Map } };

            var progress = new PlayerMission { MissionId = MissionId, State = MissionState.Active };
            progress.Objectives[1] = MissionObjectiveState.Incomplete;

            var indicator = progress.ToMissionInfo(definition, 0, Map).ObjectivesList.Single().IndicatorList.Single();

            Assert.AreEqual((uint?)430u, indicator.IndicatorId);
            Assert.AreEqual(388.95f, indicator.Position.X);
        }

        [TestMethod]
        public void ADerivedIndicatorIsWrittenWithNoLabelId()
        {
            // indicatorList entry = (position, radius, indicatorId, bShow3DEffect); indicatorId None makes the
            // client caption the marker with BuildObjectiveNameText(missionId, objectiveId) instead of a label
            // out of its missionobjectiveindicator table (mapwindow.OnMissionMarkerHighlighted lines 795-800).
            var info = new MissionInfo();
            info.ObjectivesList.Add(new MissionObjective
            {
                ObjectiveId = 1,
                IndicatorList = new List<MissionIndicator>
                {
                    new MissionIndicator { Position = new System.Numerics.Vector3(1, 2, 3), Radius = 0, IndicatorId = null, Show3DEffect = false }
                }
            });

            var bytes = Written(info);
            var labelled = new MissionInfo();
            labelled.ObjectivesList.Add(new MissionObjective
            {
                ObjectiveId = 1,
                IndicatorList = new List<MissionIndicator>
                {
                    new MissionIndicator { Position = new System.Numerics.Vector3(1, 2, 3), Radius = 0, IndicatorId = 430, Show3DEffect = false }
                }
            });

            // Same tuple shape, one field apart: the None is a single struct byte where the label is a written long.
            var withLabel = Written(labelled);
            Assert.IsTrue(bytes.Length < withLabel.Length, $"{bytes.Length} is not shorter than {withLabel.Length}");
            Assert.AreEqual((byte)PythonStruct.None, bytes[bytes.Length - 2], "the indicatorId field is not a None struct");
        }

        private static byte[] Written(MissionInfo info)
        {
            using var stream = new MemoryStream();
            using var binary = new BinaryWriter(stream);
            var writer = new PythonWriter(binary);
            writer.WriteStruct(info);
            binary.Flush();
            return stream.ToArray();
        }

        /// <summary>
        /// What the repository's own world holds: how many objectives the derivation can point at. Inconclusive
        /// without rasaworld.db, the way the other world audits are.
        /// </summary>
        [TestMethod]
        public void TheWorldsOwnObjectivesAreMostlyPlaceable()
        {
            using var connection = OpenWorld();
            var world = MissionIndicatorWorld.Build(
                Rows(connection, "SELECT id, map_context_id, pos_x, pos_y, pos_z, radius FROM content_area",
                    reader => new ContentAreaEntry
                    {
                        Id = (uint)reader.GetInt64(0), MapContextId = (uint)reader.GetInt64(1),
                        PosX = reader.GetDouble(2), PosY = reader.GetDouble(3), PosZ = reader.GetDouble(4), Radius = reader.GetDouble(5)
                    }),
                Rows(connection, "SELECT id, map_context_id, kind, creature_id, npc_package_id, pos_x, pos_y, pos_z FROM content_placement",
                    reader => new ContentPlacementEntry
                    {
                        Id = (uint)reader.GetInt64(0), MapContextId = (uint)reader.GetInt64(1), Kind = (byte)reader.GetInt64(2),
                        CreatureId = (uint)reader.GetInt64(3), NpcPackageId = (uint)reader.GetInt64(4),
                        PosX = reader.GetDouble(5), PosY = reader.GetDouble(6), PosZ = reader.GetDouble(7)
                    }),
                Rows(connection, "SELECT id, map_context_id, pos_x, pos_y, pos_z FROM logos",
                    reader => new LogosEntry
                    {
                        Id = (uint)reader.GetInt64(0), MapContextId = (uint)reader.GetInt64(1),
                        PosX = reader.GetDouble(2), PosY = reader.GetDouble(3), PosZ = reader.GetDouble(4)
                    }),
                Rows(connection, "SELECT id, map_context_id, pos_x, pos_y, pos_z, creature_1_Id, creature_2_Id, creature_3_Id, creature_4_Id, creature_5_Id, creature_6_Id FROM spawnpool",
                    reader => new SpawnPoolEntry
                    {
                        Id = (uint)reader.GetInt64(0), MapContextId = (uint)reader.GetInt64(1),
                        PosX = reader.GetDouble(2), PosY = reader.GetDouble(3), PosZ = reader.GetDouble(4),
                        Creature1Id = (uint)reader.GetInt64(5), Creature2Id = (uint)reader.GetInt64(6), Creature3Id = (uint)reader.GetInt64(7),
                        Creature4Id = (uint)reader.GetInt64(8), Creature5Id = (uint)reader.GetInt64(9), Creature6Id = (uint)reader.GetInt64(10)
                    }),
                Rows(connection, "SELECT id, package_id FROM npc_package",
                    reader => new NpcPackageEntry { Id = (uint)reader.GetInt64(0), PackageId = (uint)reader.GetInt64(1) }));

            var missions = new Dictionary<uint, Mission>();

            foreach (var id in Rows(connection, "SELECT id FROM npc_mission", reader => (uint)reader.GetInt64(0)))
                missions[id] = new Mission(new NpcMissionEntry { Id = id });

            foreach (var row in Rows(connection, "SELECT mission_id, objective_id FROM npc_mission_objective",
                         reader => ((uint)reader.GetInt64(0), (uint)reader.GetInt64(1))))
                if (missions.TryGetValue(row.Item1, out var mission))
                    mission.Objectives[row.Item2] = new MissionObjectiveDefinition { ObjectiveId = row.Item2 };

            foreach (var row in Rows(connection, "SELECT mission_id, objective_id, npc_package_id FROM npc_mission_objective_conversation",
                         reader => ((uint)reader.GetInt64(0), (uint)reader.GetInt64(1), (uint)reader.GetInt64(2))))
                if (missions.TryGetValue(row.Item1, out var mission) && mission.Objectives.ContainsKey(row.Item2))
                    mission.ObjectiveConversations.Add(new MissionObjectiveConversation { ObjectiveId = row.Item2, NpcPackageId = row.Item3 });

            foreach (var binding in Rows(connection,
                         "SELECT mission_id, objective_id, binding_id, kind, area_id, placement_id, creature_id FROM npc_mission_objective_binding",
                         reader => new NpcMissionObjectiveBindingEntry
                         {
                             MissionId = (uint)reader.GetInt64(0), ObjectiveId = (uint)reader.GetInt64(1), BindingId = (byte)reader.GetInt64(2),
                             Kind = (byte)reader.GetInt64(3), AreaId = (uint)reader.GetInt64(4), PlacementId = (uint)reader.GetInt64(5),
                             CreatureId = (uint)reader.GetInt64(6)
                         }))
                if (missions.TryGetValue(binding.MissionId, out var mission))
                    mission.Bindings.Add(binding);

            var objectives = missions.Values.Sum(mission => mission.Objectives.Count);
            var placed = missions.Values.Sum(mission => MissionMapIndicators.Derive(mission, world).Count);

            // 2026-09-22, 77 missions and 133 objectives: 8 could not be placed - one equip objective (nothing in
            // the world holds a position for what the player wears), one objective with neither a conversation nor
            // a binding, three conversation packages no creature in this world carries
            // (GAP-W3-UNBOUND-CONVERSATION-PACKAGE) and three kill bindings whose creature this world never spawns.
            // Nine of the placed ones also have a stored indicator row, which wins over the derived markers when
            // the mission info is written; this counts what the derivation can reach, not what is sent.
            // The floor is a regression guard, not the measurement.
            Assert.IsTrue(objectives >= 133, $"the world lost objectives: {objectives}");
            Assert.IsTrue(placed >= objectives - 12, $"{placed} of {objectives} objectives could be placed");
        }

        private static List<T> Rows<T>(SqliteConnection connection, string sql, Func<SqliteDataReader, T> read)
        {
            var rows = new List<T>();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            using var reader = command.ExecuteReader();

            while (reader.Read())
                rows.Add(read(reader));

            return rows;
        }

        private static SqliteConnection OpenWorld()
        {
            var directory = AppContext.BaseDirectory;
            while (directory != null && !Directory.Exists(Path.Combine(directory, "navmesh")))
                directory = Path.GetDirectoryName(directory.TrimEnd(Path.DirectorySeparatorChar));

            var path = directory == null ? null : Path.Combine(directory, "rasaworld.db");
            if (path == null || !File.Exists(path))
                Assert.Inconclusive("rasaworld.db is not next to the navmesh folder; this audit reads the repository's world database");

            var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly");
            connection.Open();
            return connection;
        }
    }
}
