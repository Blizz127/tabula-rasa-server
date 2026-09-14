using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Config;
using Rasa.Configuration;
using Rasa.Configuration.ContextSetup;
using Rasa.Context.World;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Memory;
using Rasa.Packets;
using Rasa.Packets.Protocol;
using Rasa.Repositories.Char;
using Rasa.Repositories.UnitOfWork;
using Rasa.Repositories.World;
using Rasa.Repositories.World.MissionContent;
using Rasa.Services.DbContext;
using Rasa.Structures;
using Rasa.Structures.Content;
using Rasa.Structures.World;

namespace Rasa.Test
{
    /// <summary>
    /// The reconstructed-content layer loads through the real world repositories and withholds
    /// every row it cannot run. All content rows here are synthetic fixtures, not boot-camp data.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MissionContentLoadingTests
    {
        private sealed class TestConfiguration : IDbContextConfigurationService
        {
            private readonly SqliteConnection _connection;
            public TestConfiguration(SqliteConnection connection) => _connection = connection;
            public void Configure(DbContextOptionsBuilder builder, DatabaseConnectionConfiguration configuration)
                => builder.UseSqlite(_connection);
        }

        public class WorldProxy : DispatchProxy
        {
            public SqliteWorldContext Context;
            protected override object Invoke(MethodInfo method, object[] args)
            {
                switch (method.Name)
                {
                    case "get_NpcMissions": return new NpcMissionRepository(Context);
                    case "get_NpcMissionObjectives": return new NpcMissionObjectiveRepository(Context);
                    case "get_NpcMissionRewards": return new NpcMissionRewardRepository(Context);
                    case "get_MissionContent": return new MissionContentRepository(Context);
                    case "Dispose": Context.Dispose(); return null;
                    default: throw new NotSupportedException(method.Name);
                }
            }
        }

        private sealed class Factory : IGameUnitOfWorkFactory
        {
            private readonly SqliteConnection _connection;
            public Factory(SqliteConnection connection) => _connection = connection;
            public ICharUnitOfWork CreateChar() => throw new NotSupportedException();
            public IWorldUnitOfWork CreateWorld()
            {
                var unit = DispatchProxy.Create<IWorldUnitOfWork, WorldProxy>();
                ((WorldProxy)(object)unit).Context = Context(_connection);
                return unit;
            }
        }

        private sealed class References : IContentReferences
        {
            public HashSet<uint> Contexts = new() { 1985, 1220 };
            public Dictionary<uint, (uint Giver, uint[] Objectives)> Missions = new()
            {
                { 900100, (7001, new uint[] { 1, 2 }) },
                { 900200, (7001, new uint[] { 1 }) },
                { 900300, (7001, new uint[] { 1 }) }
            };
            public HashSet<uint> NotOfferable = new() { 900200 };
            public HashSet<uint> Creatures = new() { 7001, 7002 };
            public HashSet<uint> Classes = new() { 26714, 7870 };
            public HashSet<uint> Items = new() { 17131 };
            public HashSet<uint> Logos = new() { 23 };
            public HashSet<uint> LegacyContexts = new() { 1220 };

            public bool MapContextExists(uint mapContextId) => Contexts.Contains(mapContextId);
            public bool MissionExists(uint missionId) => Missions.ContainsKey(missionId);
            public bool ObjectiveExists(uint missionId, uint objectiveId) => Missions.TryGetValue(missionId, out var m) && m.Objectives.Contains(objectiveId);
            public uint MissionGiver(uint missionId) => Missions.TryGetValue(missionId, out var m) ? m.Giver : 0;
            public bool MissionOfferable(uint missionId) => Missions.ContainsKey(missionId) && !NotOfferable.Contains(missionId);
            public bool CreatureExists(uint creatureId) => Creatures.Contains(creatureId);
            public bool EntityClassExists(uint entityClassId) => Classes.Contains(entityClassId);
            public bool ItemTemplateExists(uint itemTemplateId) => Items.Contains(itemTemplateId);
            public bool LogosExists(uint logosId) => Logos.Contains(logosId);
            public bool HasLegacyWorldObjects(uint mapContextId) => LegacyContexts.Contains(mapContextId);
        }

        private sealed class Rows
        {
            public List<ContentMapSettingEntry> MapSettings = new();
            public List<NpcMissionPrerequisiteEntry> Prerequisites = new();
            public List<NpcMissionObjectiveBindingEntry> Bindings = new();
            public List<NpcMissionObjectiveCounterEntry> Counters = new();
            public List<NpcMissionObjectiveTimerEntry> Timers = new();
            public List<NpcMissionObjectiveIndicatorEntry> Indicators = new();
            public List<ContentAreaEntry> Areas = new();
            public List<ContentPlacementEntry> Placements = new();
            public List<ContentConditionEntry> Conditions = new();
            public List<ContentRuleEntry> Rules = new();
            public List<ContentRuleActionEntry> Actions = new();
            public List<ContentItemSetEntry> ItemSets = new();
            public List<ContentLocationEntry> Locations = new();

            public ContentValidation Validate(ContentCapabilities capabilities, References references = null) =>
                new MissionContentCatalog(MapSettings, Prerequisites, Bindings, Counters, Timers, Indicators, Areas, Placements,
                    Conditions, Rules, Actions, ItemSets, Locations).Validate(references ?? new References(), capabilities);
        }

        private static SqliteWorldContext Context(SqliteConnection connection)
            => new SqliteWorldContext(Options.Create(new DatabaseConfiguration()),
                new TestConfiguration(connection), new SqliteDbContextPropertyModifier());

        private static void WithLogger(Action action)
        {
            var oldLogger = Logger.Config;
            if (oldLogger == null) Logger.UpdateConfig(new Logger.LoggerConfig { LogToFile = false });
            try
            {
                action();
            }
            finally
            {
                if (oldLogger == null) typeof(Logger).GetProperty(nameof(Logger.Config)).SetValue(null, null);
            }
        }

        private static string[] Messages(ContentValidation validation, string table) =>
            validation.Gaps.Where(gap => gap.Table == table).Select(gap => $"{gap.Key}: {gap.Message}").ToArray();

        private static string[] Own(ContentValidation validation, string table, uint owner) =>
            validation.Gaps.Where(gap => gap.Table == table && gap.OwnerId == owner).Select(gap => gap.Message).ToArray();

        private static void AssertGap(ContentValidation validation, string table, string key, string message)
        {
            var all = validation.Gaps.Select(gap => gap.ToString()).ToArray();
            Assert.IsTrue(validation.Gaps.Any(gap => gap.Table == table && gap.Key == key && gap.Message == message),
                $"missing {table} {key}: {message}; gaps: {string.Join(" | ", all)}");
        }

        [TestMethod]
        public void EmptyContentLeavesSeededMissionGapsUnchanged()
        {
            WithLogger(() =>
            {
                using var connection = new SqliteConnection("Data Source=:memory:");
                connection.Open();
                using (var context = Context(connection))
                {
                    context.Database.EnsureCreated();
                    // Stored rows of the two seeded missions (NpcMissionPreloader, live rasaworld.db).
                    context.Database.ExecuteSqlRaw("INSERT INTO npc_mission (id, giver_id, reciver_id, level, group_type, category_id, shareable, radio_completeable, comment) VALUES " +
                        "(321, 0, 0, 5, 1, 1, 0, 0, 'Assemble With Lieutenant Perkins'), (429, 100, 101, 3, 2, 2, 1, 1, 'River Recon')");
                }

                var missions = new MissionManager(new Factory(connection));
                missions.LoadMissions();

                // 429's LOGTEXT (missionconversation (429,2)=742) makes Rogers the giver and
                // Witherspoon the receiver; the seed once had them transposed.
                Assert.AreEqual(100u, missions.LoadedMissions[429].MissionGiver);
                Assert.AreEqual(101u, missions.LoadedMissions[429].MissionReciver);

                // 321's real NPCs (Cmd. Sgt. Price, Field Lt. Perkins) have no creature rows, so the
                // seed carries the unknown sentinel rather than 429's borrowed pair.
                Assert.AreEqual(0u, missions.LoadedMissions[321].MissionGiver);
                Assert.AreEqual(0u, missions.LoadedMissions[321].MissionReciver);

                CollectionAssert.AreEqual(new[] { "no objectives" }, missions.LoadedMissions[321].DefinitionGaps());
                CollectionAssert.AreEqual(new[] { "no objectives", "radio completion is not implemented", "mission sharing is not implemented" },
                    missions.LoadedMissions[429].DefinitionGaps());

                var content = new MissionContentManager(new Factory(connection));
                content.Load(null, new References(), missions.LoadedMissions);

                Assert.AreEqual(0, content.Content.Catalog.RowCount);
                Assert.AreEqual(0, content.Content.Gaps.Count);
                Assert.AreEqual(0, content.Content.LiveRules.Count());
                Assert.AreEqual(BootcampEntryMode.Disabled, content.Bootcamp.EntryMode);
                CollectionAssert.AreEqual(new[] { "no objectives" }, missions.LoadedMissions[321].DefinitionGaps());
                CollectionAssert.AreEqual(new[] { "no objectives", "radio completion is not implemented", "mission sharing is not implemented" },
                    missions.LoadedMissions[429].DefinitionGaps());
            });
        }

        [TestMethod]
        public void StoredBindingOfAnUnimplementedKindWithholdsItsMission()
        {
            WithLogger(() =>
            {
                using var connection = new SqliteConnection("Data Source=:memory:");
                connection.Open();
                using (var context = Context(connection))
                {
                    context.Database.EnsureCreated();
                    context.NpcMissionEntries.Add(new NpcMissionEntry { Id = 900100, GiverId = 7001, ReciverId = 7001, Level = 1, GroupType = 1, CategoryId = 1, Comment = "fixture" });
                    context.NpcMissionObjectiveEntries.Add(new NpcMissionObjectiveEntry { MissionId = 900100, ObjectiveId = 1, Ordinal = 1, IsRequired = true, RevealedOnAccept = true, Comment = "" });
                    context.NpcMissionObjectiveConversationEntries.Add(new NpcMissionObjectiveConversationEntry { MissionId = 900100, ObjectiveId = 1, NpcPackageId = 2584, PlayerFlagId = 1 });
                    context.NpcMissionObjectiveBindingEntries.Add(new NpcMissionObjectiveBindingEntry
                        { MissionId = 900100, ObjectiveId = 1, BindingId = 0, Kind = (byte)ObjectiveBindingKind.Kill, CreatureId = 7002, CounterId = 255 });
                    context.SaveChanges();
                }

                var missions = new MissionManager(new Factory(connection));
                missions.LoadMissions();
                Assert.IsTrue(missions.LoadedMissions[900100].IsDispensable);     // the conversation binding alone is complete

                var references = new References();
                references.Missions[900100] = (7001, new uint[] { 1 });
                references.NotOfferable.Clear();
                var content = new MissionContentManager(new Factory(connection)) { Missions = missions };
                content.Load(() => new BootcampConfig(), references, missions.LoadedMissions);

                Assert.AreEqual(1, content.Content.Catalog.RowCount);              // the binding
                CollectionAssert.AreEqual(new[] { "900100/1/0: binding kind Kill is not implemented" },
                    Messages(content.Content, NpcMissionObjectiveBindingEntry.TableName));
                Assert.IsFalse(missions.LoadedMissions[900100].IsDispensable);
                CollectionAssert.Contains(missions.LoadedMissions[900100].DefinitionGaps(),
                    "npc_mission_objective_binding 900100/1/0: binding kind Kill is not implemented");
                Assert.AreEqual(0, content.Content.LiveBindings.Count());

                // Reloading clears gaps from the previous load rather than accumulating them.
                content.Load(() => new BootcampConfig(), references, missions.LoadedMissions);
                Assert.AreEqual(1, missions.LoadedMissions[900100].ContentGaps.Count);
            });
        }

        [TestMethod]
        public void S1ImplementsExactlyTheBootcampInitiationMechanics()
        {
            var implemented = MissionContentRules.Implemented;
            CollectionAssert.AreEquivalent(new[] { ObjectiveBindingKind.AreaEntered, ObjectiveBindingKind.Equip, ObjectiveBindingKind.LootAll, ObjectiveBindingKind.Hit }, implemented.BindingKinds.ToArray());
            CollectionAssert.AreEquivalent(new[]
            {
                ContentRuleEvent.EnteredMap, ContentRuleEvent.MissionAccepted,
                ContentRuleEvent.ObjectiveCompleted, ContentRuleEvent.MissionTurnedIn
            }, implemented.Events.ToArray());
            CollectionAssert.AreEquivalent(new[]
            {
                ContentRuleAction.DispenseRadioMission, ContentRuleAction.OfferMissionAtNpc, ContentRuleAction.GrantLogos,
                ContentRuleAction.ForceConverseGreeting, ContentRuleAction.TutorialNotification
            }, implemented.Actions.ToArray());
            CollectionAssert.AreEquivalent(new[]
            {
                ContentConditionKind.MissionAbsent, ContentConditionKind.MissionStateIs, ContentConditionKind.ObjectiveStateIs
            }, implemented.ConditionKinds.ToArray());
            CollectionAssert.AreEquivalent(new[] { ContentPlacementKind.Creature, ContentPlacementKind.Usable }, implemented.PlacementKinds.ToArray());
            CollectionAssert.AreEquivalent(new[] { ContentPlacementBehavior.Stationary }, implemented.PlacementBehaviors.ToArray());
            CollectionAssert.AreEquivalent(new[] { ContentUsableKind.Container, ContentUsableKind.Destroyable }, implemented.UsableKinds.ToArray());
            CollectionAssert.AreEquivalent(new[] { MapInstancing.Shared }, implemented.Instancing.ToArray());
            Assert.IsFalse(implemented.Prerequisites || implemented.Counters || implemented.Timers || implemented.Indicators ||
                           implemented.PlacementRespawn || implemented.NpcPackageOverride);
            Assert.IsTrue(MissionContentRules.BootcampEntryImplemented);
        }

        [TestMethod]
        public void EveryKindFailsClosedWhenItIsNotImplemented()
        {
            // An empty capability set: every mechanic is withheld, whatever the server implements.
            var implemented = new ContentCapabilities();

            var rows = new Rows();
            rows.MapSettings.Add(new ContentMapSettingEntry { MapContextId = 1985, Instancing = (byte)MapInstancing.PerCharacter });
            rows.Prerequisites.Add(new NpcMissionPrerequisiteEntry { MissionId = 900100, OrGroup = 0, RequiredMissionId = 900100, RequiredState = (byte)MissionState.Failded });
            rows.Counters.Add(new NpcMissionObjectiveCounterEntry { MissionId = 900100, ObjectiveId = 1, CounterId = 0, InitialValue = 0, TargetValue = 1 });
            rows.Timers.Add(new NpcMissionObjectiveTimerEntry { MissionId = 900100, ObjectiveId = 2, LimitSeconds = 126, OnExpire = (byte)ObjectiveTimerExpiry.FailObjectiveAndMission });
            rows.Indicators.Add(new NpcMissionObjectiveIndicatorEntry { MissionId = 900100, ObjectiveId = 2, IndicatorIndex = 0, IndicatorId = 1, Radius = 3 });
            rows.Placements.Add(new ContentPlacementEntry
            {
                Id = 900650, MapContextId = 1985, Kind = (byte)ContentPlacementKind.Usable, EntityClassId = 7870, UsableKind = (byte)ContentUsableKind.Bomb,
                Behavior = (byte)ContentPlacementBehavior.Stationary, InitialState = 113, FuseMs = 5300
            });
            rows.Conditions.Add(new ContentConditionEntry { ConditionId = 900900, Kind = (byte)ContentConditionKind.MissionAbsent, MissionId = 900100 });
            rows.Rules.Add(new ContentRuleEntry { Id = 9001, MapContextId = 1985, Event = (byte)ContentRuleEvent.EnteredMap });
            rows.Actions.Add(new ContentRuleActionEntry { RuleId = 9001, Sequence = 0, Action = (byte)ContentRuleAction.SetAccountSkipBootcamp });

            var validation = rows.Validate(implemented);

            CollectionAssert.AreEquivalent(new[]
            {
                "content_map_setting 1985: PerCharacter instancing is not implemented",
                "npc_mission_prerequisite 900100/0/900100: mission prerequisites are not implemented",
                "npc_mission_objective_counter 900100/1/0: objective counters are not implemented",
                "npc_mission_objective_timer 900100/2: objective timers are not implemented",
                "npc_mission_objective_indicator 900100/2/0: objective indicators are not implemented",
                "content_placement 900650: placement kind Usable is not implemented",
                "content_placement 900650: behavior Stationary is not implemented",
                "content_placement 900650: usable kind Bomb is not implemented",
                "content_condition 900900/0/0: condition kind MissionAbsent is not implemented",
                "content_rule 9001: event EnteredMap is not implemented",
                "content_rule_action 9001/0: action SetAccountSkipBootcamp is not implemented"
            }, validation.Gaps.Select(gap => gap.ToString()).ToArray());

            // The same rows are complete once every mechanic exists.
            Assert.AreEqual(0, rows.Validate(ContentCapabilities.All).Gaps.Count, string.Join(" | ", rows.Validate(ContentCapabilities.All).Gaps));

            CollectionAssert.AreEquivalent(new uint[] { 1985 }, validation.WithheldContexts.ToArray());
            CollectionAssert.AreEquivalent(new uint[] { 900650 }, validation.WithheldPlacements.ToArray());
            CollectionAssert.AreEquivalent(new uint[] { 9001 }, validation.WithheldRules.ToArray());
            Assert.AreEqual(4, validation.MissionGaps[900100].Count);
        }

        [TestMethod]
        public void ClientPostedTutorialsAndUsableStateMachinesComeFromTheClient()
        {
            Assert.AreEqual(23, MissionContentRules.ClientPostedTutorialIds.Count);
            CollectionAssert.AreEquivalent(new uint[] { 2, 110, 185, 186 }, MissionContentRules.UsableStates(ContentUsableKind.Destroyable).ToArray());
            CollectionAssert.AreEquivalent(new uint[] { 113, 114, 115 }, MissionContentRules.UsableStates(ContentUsableKind.Bomb).ToArray());
            CollectionAssert.AreEquivalent(new uint[] { 200, 201, 202 }, MissionContentRules.UsableStates(ContentUsableKind.Container).ToArray());
        }

        [TestMethod]
        public void RuleActionsRequireTheirColumnsAndRejectInventedOnes()
        {
            var rows = new Rows();
            rows.Placements.Add(new ContentPlacementEntry
                { Id = 900660, MapContextId = 1985, Kind = (byte)ContentPlacementKind.Creature, CreatureId = 7002, Behavior = (byte)ContentPlacementBehavior.Stationary });
            rows.Locations.Add(new ContentLocationEntry { Id = 900001, Purpose = (byte)ContentLocationPurpose.NewCharacterStart, MapContextId = 1985 });
            rows.Rules.Add(new ContentRuleEntry { Id = 9002, MapContextId = 1985, Event = (byte)ContentRuleEvent.MissionTurnedIn, MissionId = 900100 });
            rows.Actions.AddRange(new[]
            {
                new ContentRuleActionEntry { RuleId = 9002, Sequence = 0, Action = (byte)ContentRuleAction.TutorialNotification, TutorialId = 7 },
                new ContentRuleActionEntry { RuleId = 9002, Sequence = 1, Action = (byte)ContentRuleAction.TutorialNotification, TutorialId = 10000018, Forced = true },
                new ContentRuleActionEntry { RuleId = 9002, Sequence = 2, Action = (byte)ContentRuleAction.OfferMissionAtNpc, MissionId = 900100, PlacementId = 900660 },
                new ContentRuleActionEntry { RuleId = 9002, Sequence = 3, Action = (byte)ContentRuleAction.GrantLogos, LogosId = 24, LogosProtocol = (byte)LogosGrantProtocol.StoneTabula },
                new ContentRuleActionEntry { RuleId = 9002, Sequence = 4, Action = (byte)ContentRuleAction.TransferToLocation, LocationId = 900001 },
                new ContentRuleActionEntry { RuleId = 9002, Sequence = 5, Action = (byte)ContentRuleAction.ForceConverseGreeting, GreetingId = 1634 },
                new ContentRuleActionEntry { RuleId = 9002, Sequence = 6, Action = (byte)ContentRuleAction.GrantRewards }
            });

            var validation = rows.Validate(ContentCapabilities.All);

            CollectionAssert.AreEqual(new[]
            {
                "9002/0: tutorial 7 is posted by the client itself",
                "9002/1: unexpected forced",
                "9002/2: placement creature is not the mission giver",
                "9002/3: unknown logos",
                "9002/4: needs a transfer destination location",
                "9002/5: forced greeting needs a greeting id and a speaker name id",
                "9002/6: reward grant is empty"
            }, Messages(validation, ContentRuleActionEntry.TableName));
            Assert.AreEqual(7, validation.Gaps.Count);
            Assert.IsTrue(validation.WithheldRules.Contains(9002u));
        }

        [TestMethod]
        public void RulesThatOfferAMissionFollowItsOfferability()
        {
            var rows = new Rows();
            rows.Areas.Add(new ContentAreaEntry { Id = 900601, MapContextId = 1985, Shape = (byte)ContentAreaShape.Sphere, Radius = 0 });
            rows.Bindings.Add(new NpcMissionObjectiveBindingEntry
                { MissionId = 900300, ObjectiveId = 1, BindingId = 0, Kind = (byte)ObjectiveBindingKind.AreaEntered, AreaId = 900601, CounterId = 255 });
            rows.Rules.AddRange(new[]
            {
                new ContentRuleEntry { Id = 9003, MapContextId = 1985, Event = (byte)ContentRuleEvent.EnteredMap },
                new ContentRuleEntry { Id = 9004, MapContextId = 1985, Event = (byte)ContentRuleEvent.EnteredMap },
                new ContentRuleEntry { Id = 9005, MapContextId = 1985, Event = (byte)ContentRuleEvent.EnteredMap }
            });
            rows.Actions.AddRange(new[]
            {
                new ContentRuleActionEntry { RuleId = 9003, Sequence = 0, Action = (byte)ContentRuleAction.DispenseRadioMission, MissionId = 900100, Forced = true },
                new ContentRuleActionEntry { RuleId = 9004, Sequence = 0, Action = (byte)ContentRuleAction.DispenseRadioMission, MissionId = 900200, Forced = true },
                new ContentRuleActionEntry { RuleId = 9005, Sequence = 0, Action = (byte)ContentRuleAction.DispenseRadioMission, MissionId = 900300, Forced = true }
            });

            var validation = rows.Validate(ContentCapabilities.All);

            CollectionAssert.AreEqual(new[] { "radius must be positive" }, Own(validation, ContentAreaEntry.TableName, 900601));
            CollectionAssert.AreEqual(new[] { "npc_mission_objective_binding 900300/1/0: area 900601 is withheld" }, validation.MissionGaps[900300].ToArray());
            CollectionAssert.AreEqual(new[] { "offers mission 900200, which is not offerable" }, Own(validation, ContentRuleActionEntry.TableName, 9004));
            CollectionAssert.AreEqual(new[] { "offers mission 900300, which is not offerable" }, Own(validation, ContentRuleActionEntry.TableName, 9005));
            CollectionAssert.AreEqual(new uint[] { 9003 }, validation.LiveRules.Select(rule => rule.Id).ToArray());
        }

        [TestMethod]
        public void RuleFiltersMustBelongToTheirEvent()
        {
            var rows = new Rows();
            rows.Areas.Add(new ContentAreaEntry { Id = 900600, MapContextId = 1220, Shape = (byte)ContentAreaShape.VerticalCylinder, Radius = 5, HalfHeight = 2 });
            rows.Placements.Add(new ContentPlacementEntry
            {
                Id = 900700, MapContextId = 1985, Kind = (byte)ContentPlacementKind.Usable, EntityClassId = 7870, UsableKind = (byte)ContentUsableKind.Bomb,
                Behavior = (byte)ContentPlacementBehavior.Stationary, InitialState = 113, FuseMs = 5300
            });
            rows.Rules.AddRange(new[]
            {
                new ContentRuleEntry { Id = 9010, MapContextId = 1985, Event = (byte)ContentRuleEvent.MissionAccepted, AreaId = 900600 },
                new ContentRuleEntry { Id = 9011, MapContextId = 1985, Event = (byte)ContentRuleEvent.AreaEntered, AreaId = 900600 },
                new ContentRuleEntry { Id = 9012, MapContextId = 1985, Event = (byte)ContentRuleEvent.ObjectiveCompleted, ObjectiveId = 1 },
                new ContentRuleEntry { Id = 9013, MapContextId = 1985, Event = (byte)ContentRuleEvent.ObjectiveCompleted, MissionId = 900100, ObjectiveId = 9 },
                new ContentRuleEntry { Id = 9014, MapContextId = 1985, Event = (byte)ContentRuleEvent.ObjectiveCompleted, MissionId = 900100 },
                new ContentRuleEntry { Id = 9015, MapContextId = 1985, Event = (byte)ContentRuleEvent.PlacementStateEntered, PlacementId = 900700, StateId = 186 },
                new ContentRuleEntry { Id = 9016, MapContextId = 1985, Event = (byte)ContentRuleEvent.MissionAccepted, StateId = 114 },
                new ContentRuleEntry { Id = 9017, MapContextId = 1985, Event = (byte)ContentRuleEvent.PlacementStateEntered, PlacementId = 900700, StateId = 115 }
            });
            foreach (var rule in rows.Rules)
                rows.Actions.Add(new ContentRuleActionEntry { RuleId = rule.Id, Sequence = 0, Action = (byte)ContentRuleAction.SetFact, FactKey = "k", FactValue = 1 });

            var validation = rows.Validate(ContentCapabilities.All);

            CollectionAssert.AreEqual(new[]
            {
                "9010: filter columns do not match event MissionAccepted",
                "9010: unknown area filter in this context",
                "9011: unknown area filter in this context",
                "9012: objective filter without a mission filter",
                "9013: unknown mission or objective filter",
                "9015: state 186 is not a state of the filtered placement",
                "9016: filter columns do not match event MissionAccepted"
            }, Messages(validation, ContentRuleEntry.TableName));
            CollectionAssert.AreEquivalent(new uint[] { 9014, 9017 }, validation.LiveRules.Select(rule => rule.Id).ToArray());
        }

        [TestMethod]
        public void PresenceConditionsNeedAPerCharacterContextAndLegacyObjectsForbidOne()
        {
            var rows = new Rows();
            rows.MapSettings.Add(new ContentMapSettingEntry { MapContextId = 1220, Instancing = (byte)MapInstancing.PerCharacter });
            rows.Conditions.Add(new ContentConditionEntry { ConditionId = 900900, Kind = (byte)ContentConditionKind.MissionStateIs, MissionId = 900100, State = (uint)MissionState.Active });
            rows.Placements.Add(new ContentPlacementEntry
            {
                Id = 900670, MapContextId = 1985, Kind = (byte)ContentPlacementKind.Creature, CreatureId = 7001,
                Behavior = (byte)ContentPlacementBehavior.Stationary, PresentConditionId = 900900
            });

            var validation = rows.Validate(ContentCapabilities.All);

            CollectionAssert.AreEquivalent(new[]
            {
                "content_placement 900670: present_condition_id requires a per-character context",
                "content_map_setting 1220: spawnpool, footlocker or logos rows exist in a per-character context"
            }, validation.Gaps.Select(gap => gap.ToString()).ToArray());
        }

        [TestMethod]
        public void AWithheldRowWithholdsEverythingThatReferencesIt()
        {
            var rows = new Rows();
            rows.MapSettings.Add(new ContentMapSettingEntry { MapContextId = 1985, Instancing = (byte)MapInstancing.PerCharacter });
            // Roots with gaps of their own.
            rows.Conditions.Add(new ContentConditionEntry { ConditionId = 900901, Kind = (byte)ContentConditionKind.MissionAbsent, MissionId = 999999 });
            rows.ItemSets.Add(new ContentItemSetEntry { ItemSetId = 19851, ItemTemplateId = 99999, Quantity = 1 });
            rows.Locations.Add(new ContentLocationEntry { Id = 900003, Purpose = (byte)ContentLocationPurpose.TransferDestination, MapContextId = 4242 });
            // Dependents with no gap of their own.
            rows.Areas.Add(new ContentAreaEntry { Id = 900611, MapContextId = 1985, Shape = (byte)ContentAreaShape.Sphere, Radius = -1 });
            rows.Placements.AddRange(new[]
            {
                new ContentPlacementEntry
                {
                    Id = 900681, MapContextId = 1985, Kind = (byte)ContentPlacementKind.Creature, CreatureId = 7001,
                    Behavior = (byte)ContentPlacementBehavior.Stationary, PresentConditionId = 900901
                },
                new ContentPlacementEntry
                {
                    Id = 900683, MapContextId = 1985, Kind = (byte)ContentPlacementKind.Usable, EntityClassId = 7870, UsableKind = (byte)ContentUsableKind.Bomb,
                    Behavior = (byte)ContentPlacementBehavior.Stationary, InitialState = 113, FuseMs = 5300, UsableConditionId = 900901
                },
                new ContentPlacementEntry
                {
                    Id = 900684, MapContextId = 1985, Kind = (byte)ContentPlacementKind.Usable, EntityClassId = 26714, UsableKind = (byte)ContentUsableKind.Destroyable,
                    Behavior = (byte)ContentPlacementBehavior.Stationary, InitialState = 2, HitPoints = 10, AlternateState = 186, AlternateStateConditionId = 900901
                }
            });
            rows.Bindings.AddRange(new[]
            {
                new NpcMissionObjectiveBindingEntry { MissionId = 900100, ObjectiveId = 1, BindingId = 0, Kind = (byte)ObjectiveBindingKind.Kill, PlacementId = 900681, CounterId = 255 },
                new NpcMissionObjectiveBindingEntry { MissionId = 900300, ObjectiveId = 1, BindingId = 0, Kind = (byte)ObjectiveBindingKind.Equip, EquipMatch = 2, ItemSetId = 19851, CounterId = 255 }
            });
            rows.Rules.AddRange(new[]
            {
                new ContentRuleEntry { Id = 9020, MapContextId = 1985, Event = (byte)ContentRuleEvent.EnteredMap },
                new ContentRuleEntry { Id = 9021, MapContextId = 1985, Event = (byte)ContentRuleEvent.PlacementDestroyed, PlacementId = 900681 },
                new ContentRuleEntry { Id = 9022, MapContextId = 1985, Event = (byte)ContentRuleEvent.EnteredMap },
                new ContentRuleEntry { Id = 9023, MapContextId = 1985, Event = (byte)ContentRuleEvent.EnteredMap },
                new ContentRuleEntry { Id = 9024, MapContextId = 1985, Event = (byte)ContentRuleEvent.EnteredMap, ConditionId = 900901 },
                new ContentRuleEntry { Id = 9025, MapContextId = 1985, Event = (byte)ContentRuleEvent.EnteredMap },
                new ContentRuleEntry { Id = 9028, MapContextId = 1985, Event = (byte)ContentRuleEvent.AreaEntered, AreaId = 900611 }
            });
            rows.Actions.AddRange(new[]
            {
                new ContentRuleActionEntry { RuleId = 9020, Sequence = 0, Action = (byte)ContentRuleAction.GrantItemSet, ItemSetId = 19851 },
                new ContentRuleActionEntry { RuleId = 9021, Sequence = 0, Action = (byte)ContentRuleAction.SetFact, FactKey = "k", FactValue = 1 },
                new ContentRuleActionEntry { RuleId = 9022, Sequence = 0, Action = (byte)ContentRuleAction.OfferMissionAtNpc, MissionId = 900200, PlacementId = 900681 },
                new ContentRuleActionEntry { RuleId = 9023, Sequence = 0, Action = (byte)ContentRuleAction.TransferToLocation, LocationId = 900003 },
                new ContentRuleActionEntry { RuleId = 9024, Sequence = 0, Action = (byte)ContentRuleAction.SetFact, FactKey = "k", FactValue = 1 },
                new ContentRuleActionEntry { RuleId = 9025, Sequence = 0, Action = (byte)ContentRuleAction.SetFact, FactKey = "k", FactValue = 1 },
                new ContentRuleActionEntry { RuleId = 9028, Sequence = 0, Action = (byte)ContentRuleAction.SetFact, FactKey = "k", FactValue = 1 }
            });
            var references = new References();
            references.NotOfferable.Clear();

            var validation = rows.Validate(ContentCapabilities.All, references);

            CollectionAssert.AreEquivalent(new[]
            {
                "content_condition 900901/0/0: unknown mission",
                "content_item_set 19851/99999: unknown item template",
                "content_location 900003: unknown map context",
                "content_area 900611: radius must be positive"
            }, validation.Gaps.Select(gap => gap.ToString()).ToArray());
            CollectionAssert.AreEquivalent(new uint[] { 900901 }, validation.WithheldConditions.ToArray());
            CollectionAssert.AreEquivalent(new uint[] { 19851 }, validation.WithheldItemSets.ToArray());
            CollectionAssert.AreEquivalent(new uint[] { 900003 }, validation.WithheldLocations.ToArray());
            CollectionAssert.AreEquivalent(new uint[] { 900681, 900683, 900684 }, validation.WithheldPlacements.ToArray());
            CollectionAssert.AreEquivalent(new uint[] { 9020, 9021, 9022, 9023, 9024, 9028 }, validation.WithheldRules.ToArray());
            CollectionAssert.AreEqual(new uint[] { 9025 }, validation.LiveRules.Select(rule => rule.Id).ToArray());
            CollectionAssert.AreEqual(new[] { "npc_mission_objective_binding 900100/1/0: placement 900681 is withheld" }, validation.MissionGaps[900100].ToArray());
            CollectionAssert.AreEqual(new[] { "npc_mission_objective_binding 900300/1/0: item set 19851 is withheld" }, validation.MissionGaps[900300].ToArray());
        }

        [TestMethod]
        public void AWithheldContextWithholdsItsAreasLocationsPlacementsAndRules()
        {
            var rows = new Rows();
            rows.MapSettings.Add(new ContentMapSettingEntry { MapContextId = 1220, Instancing = (byte)MapInstancing.PerCharacter });
            rows.Areas.Add(new ContentAreaEntry { Id = 900610, MapContextId = 1220, Shape = (byte)ContentAreaShape.Sphere, Radius = 4 });
            rows.Locations.Add(new ContentLocationEntry { Id = 900004, Purpose = (byte)ContentLocationPurpose.TransferDestination, MapContextId = 1220 });
            rows.Placements.Add(new ContentPlacementEntry { Id = 900682, MapContextId = 1220, Kind = (byte)ContentPlacementKind.Creature, CreatureId = 7001, Behavior = (byte)ContentPlacementBehavior.Stationary });
            rows.Bindings.Add(new NpcMissionObjectiveBindingEntry { MissionId = 900100, ObjectiveId = 1, BindingId = 0, Kind = (byte)ObjectiveBindingKind.AreaEntered, AreaId = 900610, CounterId = 255 });
            rows.Rules.AddRange(new[]
            {
                new ContentRuleEntry { Id = 9026, MapContextId = 1220, Event = (byte)ContentRuleEvent.MissionAccepted },
                new ContentRuleEntry { Id = 9027, MapContextId = 1985, Event = (byte)ContentRuleEvent.MissionAccepted }
            });
            rows.Actions.AddRange(new[]
            {
                new ContentRuleActionEntry { RuleId = 9026, Sequence = 0, Action = (byte)ContentRuleAction.SetFact, FactKey = "k", FactValue = 1 },
                new ContentRuleActionEntry { RuleId = 9027, Sequence = 0, Action = (byte)ContentRuleAction.TransferToLocation, LocationId = 900004 }
            });

            var validation = rows.Validate(ContentCapabilities.All);

            CollectionAssert.AreEqual(new[] { "content_map_setting 1220: spawnpool, footlocker or logos rows exist in a per-character context" },
                validation.Gaps.Select(gap => gap.ToString()).ToArray());
            CollectionAssert.AreEquivalent(new uint[] { 900610 }, validation.WithheldAreas.ToArray());
            CollectionAssert.AreEquivalent(new uint[] { 900004 }, validation.WithheldLocations.ToArray());
            CollectionAssert.AreEquivalent(new uint[] { 900682 }, validation.WithheldPlacements.ToArray());
            CollectionAssert.AreEquivalent(new uint[] { 9026, 9027 }, validation.WithheldRules.ToArray());
            CollectionAssert.AreEqual(new[] { "npc_mission_objective_binding 900100/1/0: area 900610 is withheld" }, validation.MissionGaps[900100].ToArray());
        }

        [TestMethod]
        public void PlacementsAndBindingsFollowTheClientUsableStateMachines()
        {
            var rows = new Rows();
            rows.ItemSets.Add(new ContentItemSetEntry { ItemSetId = 19852, ItemTemplateId = 17131, Quantity = 1 });
            rows.Placements.AddRange(new[]
            {
                new ContentPlacementEntry
                {
                    Id = 900690, MapContextId = 1985, Kind = (byte)ContentPlacementKind.Usable, EntityClassId = 26714, UsableKind = (byte)ContentUsableKind.Destroyable,
                    Behavior = (byte)ContentPlacementBehavior.Stationary, InitialState = 113, HitPoints = 0
                },
                new ContentPlacementEntry
                {
                    Id = 900691, MapContextId = 1985, Kind = (byte)ContentPlacementKind.Usable, EntityClassId = 7870, UsableKind = (byte)ContentUsableKind.Bomb,
                    Behavior = (byte)ContentPlacementBehavior.CreatureAi, InitialState = 113, FuseMs = 5300, HitPoints = 10
                },
                new ContentPlacementEntry
                {
                    Id = 900692, MapContextId = 1985, Kind = (byte)ContentPlacementKind.Usable, EntityClassId = 26714, UsableKind = (byte)ContentUsableKind.Container,
                    Behavior = (byte)ContentPlacementBehavior.Stationary, InitialState = 424242, LootItemSetId = 19852
                }
            });
            rows.Bindings.Add(new NpcMissionObjectiveBindingEntry
                { MissionId = 900100, ObjectiveId = 1, BindingId = 0, Kind = (byte)ObjectiveBindingKind.PlacementState, PlacementId = 900691, TargetState = 186, CounterId = 255 });
            rows.Bindings.Add(new NpcMissionObjectiveBindingEntry
                { MissionId = 900100, ObjectiveId = 2, BindingId = 0, Kind = (byte)ObjectiveBindingKind.Hit, PlacementId = 900690, ActionId = 1, CounterId = 0 });
            rows.Rules.Add(new ContentRuleEntry { Id = 9030, MapContextId = 1985, Event = (byte)ContentRuleEvent.MissionAccepted });
            rows.Actions.Add(new ContentRuleActionEntry { RuleId = 9030, Sequence = 0, Action = (byte)ContentRuleAction.SetPlacementState, PlacementId = 900691, StateId = 110 });

            var validation = rows.Validate(ContentCapabilities.All);

            CollectionAssert.AreEqual(new[]
            {
                "900690: initial state 113 is not a state of Destroyable",
                "900690: a destroyable placement needs hit points",
                "900691: usable placements must be stationary",
                "900691: unexpected hit_points",
                "900692: initial state 424242 is not a state of Container"
            }, Messages(validation, ContentPlacementEntry.TableName));
            CollectionAssert.AreEqual(new[]
            {
                "900100/1/0: state 186 is not a state of Bomb",
                "900100/2/0: hit by action 1 has no server implementation",
                "900100/2/0: unknown counter"
            }, Messages(validation, NpcMissionObjectiveBindingEntry.TableName));
            CollectionAssert.AreEqual(new[] { "9030/0: state 110 is not a state of Bomb" }, Messages(validation, ContentRuleActionEntry.TableName));
        }

        [TestMethod]
        public void CreaturePlacementsAndConditionKindsNeedImplementedMechanics()
        {
            var rows = new Rows();
            rows.Placements.Add(new ContentPlacementEntry
            {
                Id = 900693, MapContextId = 1985, Kind = (byte)ContentPlacementKind.Creature, CreatureId = 7001,
                Behavior = (byte)ContentPlacementBehavior.CreatureAi, NpcPackageId = 999999, RespawnMs = 5
            });
            rows.Conditions.AddRange(new[]
            {
                new ContentConditionEntry { ConditionId = 900902, Kind = (byte)ContentConditionKind.MissionStateIs, MissionId = 900100, State = (uint)MissionState.Completed },
                new ContentConditionEntry { ConditionId = 900903, Kind = (byte)ContentConditionKind.FactEquals, FactKey = "bootcamp.bomb_planted", Value = 1 }
            });
            var stationaryCreaturesOnly = new ContentCapabilities
            {
                PlacementKinds = new HashSet<ContentPlacementKind> { ContentPlacementKind.Creature },
                PlacementBehaviors = new HashSet<ContentPlacementBehavior> { ContentPlacementBehavior.Stationary },
                ConditionKinds = new HashSet<ContentConditionKind> { ContentConditionKind.MissionStateIs }
            };

            var validation = rows.Validate(stationaryCreaturesOnly);

            CollectionAssert.AreEquivalent(new[]
            {
                "content_placement 900693: behavior CreatureAi is not implemented",
                "content_placement 900693: placement respawn is not implemented",
                "content_placement 900693: npc package override is not implemented",
                "content_condition 900903/0/0: condition kind FactEquals is not implemented"
            }, validation.Gaps.Select(gap => gap.ToString()).ToArray());
            CollectionAssert.AreEquivalent(new uint[] { 900903 }, validation.WithheldConditions.ToArray());
        }

        [TestMethod]
        public void RuleCyclesAreFoundThroughAnyPathAndAnyPlacementFilters()
        {
            var rows = new Rows();
            rows.Placements.AddRange(new[]
            {
                new ContentPlacementEntry
                {
                    Id = 900700, MapContextId = 1985, Kind = (byte)ContentPlacementKind.Usable, EntityClassId = 7870, UsableKind = (byte)ContentUsableKind.Bomb,
                    Behavior = (byte)ContentPlacementBehavior.Stationary, InitialState = 113, FuseMs = 5300
                },
                new ContentPlacementEntry
                {
                    Id = 900701, MapContextId = 1220, Kind = (byte)ContentPlacementKind.Usable, EntityClassId = 7870, UsableKind = (byte)ContentUsableKind.Bomb,
                    Behavior = (byte)ContentPlacementBehavior.Stationary, InitialState = 113, FuseMs = 5300
                },
                new ContentPlacementEntry
                {
                    Id = 900702, MapContextId = 1220, Kind = (byte)ContentPlacementKind.Usable, EntityClassId = 7870, UsableKind = (byte)ContentUsableKind.Bomb,
                    Behavior = (byte)ContentPlacementBehavior.Stationary, InitialState = 113, FuseMs = 5300
                }
            });
            rows.Locations.Add(new ContentLocationEntry { Id = 900002, Purpose = (byte)ContentLocationPurpose.TransferDestination, MapContextId = 1985 });
            rows.Rules.AddRange(new[]
            {
                // A transfer into the context whose entered_map rule transfers again.
                new ContentRuleEntry { Id = 9041, MapContextId = 1985, Event = (byte)ContentRuleEvent.EnteredMap },
                // 9050 -> 9052 -> 9051 -> 9050, and 9050 -> 9051 directly (the path a depth-first walk can miss).
                new ContentRuleEntry { Id = 9050, MapContextId = 1220, Event = (byte)ContentRuleEvent.PlacementStateEntered, PlacementId = 900701, StateId = 113 },
                new ContentRuleEntry { Id = 9051, MapContextId = 1220, Event = (byte)ContentRuleEvent.PlacementStateEntered, PlacementId = 900701, StateId = 114 },
                new ContentRuleEntry { Id = 9052, MapContextId = 1220, Event = (byte)ContentRuleEvent.PlacementStateEntered, PlacementId = 900701, StateId = 115 },
                // Any placement in 1985 (filter 0) setting a 1985 placement's state reaches itself.
                new ContentRuleEntry { Id = 9053, MapContextId = 1985, Event = (byte)ContentRuleEvent.PlacementStateEntered },
                // Not in a cycle: the state filter never matches the state it sets, and a plain event.
                new ContentRuleEntry { Id = 9054, MapContextId = 1985, Event = (byte)ContentRuleEvent.PlacementStateEntered, PlacementId = 900700, StateId = 115 },
                new ContentRuleEntry { Id = 9055, MapContextId = 1985, Event = (byte)ContentRuleEvent.MissionAccepted },
                // Sets its own placement, but to a state its filter does not match.
                new ContentRuleEntry { Id = 9056, MapContextId = 1220, Event = (byte)ContentRuleEvent.PlacementStateEntered, PlacementId = 900702, StateId = 113 }
            });
            rows.Actions.AddRange(new[]
            {
                new ContentRuleActionEntry { RuleId = 9041, Sequence = 0, Action = (byte)ContentRuleAction.TransferToLocation, LocationId = 900002 },
                new ContentRuleActionEntry { RuleId = 9050, Sequence = 0, Action = (byte)ContentRuleAction.SetPlacementState, PlacementId = 900701, StateId = 114 },
                new ContentRuleActionEntry { RuleId = 9050, Sequence = 1, Action = (byte)ContentRuleAction.SetPlacementState, PlacementId = 900701, StateId = 115 },
                new ContentRuleActionEntry { RuleId = 9051, Sequence = 0, Action = (byte)ContentRuleAction.SetPlacementState, PlacementId = 900701, StateId = 113 },
                new ContentRuleActionEntry { RuleId = 9052, Sequence = 0, Action = (byte)ContentRuleAction.SetPlacementState, PlacementId = 900701, StateId = 114 },
                new ContentRuleActionEntry { RuleId = 9053, Sequence = 0, Action = (byte)ContentRuleAction.SetPlacementState, PlacementId = 900700, StateId = 114 },
                new ContentRuleActionEntry { RuleId = 9054, Sequence = 0, Action = (byte)ContentRuleAction.SetPlacementState, PlacementId = 900701, StateId = 113 },
                new ContentRuleActionEntry { RuleId = 9055, Sequence = 0, Action = (byte)ContentRuleAction.SetPlacementState, PlacementId = 900700, StateId = 115 },
                new ContentRuleActionEntry { RuleId = 9056, Sequence = 0, Action = (byte)ContentRuleAction.SetPlacementState, PlacementId = 900702, StateId = 114 }
            });

            var validation = rows.Validate(ContentCapabilities.All);

            CollectionAssert.AreEquivalent(new[] { "9041: rule is part of a cycle", "9050: rule is part of a cycle", "9051: rule is part of a cycle", "9052: rule is part of a cycle", "9053: rule is part of a cycle" },
                Messages(validation, ContentRuleEntry.TableName));
        }

        [TestMethod]
        public void ZeroAndDuplicateIdsNeverSatisfyReferences()
        {
            var rows = new Rows();
            rows.ItemSets.Add(new ContentItemSetEntry { ItemSetId = 0, ItemTemplateId = 17131, Quantity = 1 });
            rows.Conditions.Add(new ContentConditionEntry { ConditionId = 0, Kind = (byte)ContentConditionKind.MissionAbsent, MissionId = 900100 });
            rows.Placements.Add(new ContentPlacementEntry
            {
                Id = 900694, MapContextId = 1985, Kind = (byte)ContentPlacementKind.Usable, EntityClassId = 26714, UsableKind = (byte)ContentUsableKind.Container,
                Behavior = (byte)ContentPlacementBehavior.Stationary, LootItemSetId = 0
            });
            rows.Rules.AddRange(new[]
            {
                new ContentRuleEntry { Id = 9042, MapContextId = 1985, Event = (byte)ContentRuleEvent.MissionAccepted },
                new ContentRuleEntry { Id = 9042, MapContextId = 1985, Event = (byte)ContentRuleEvent.MissionAccepted },
                new ContentRuleEntry { Id = 0, MapContextId = 1985, Event = (byte)ContentRuleEvent.MissionAccepted }
            });
            rows.Actions.Add(new ContentRuleActionEntry { RuleId = 9042, Sequence = 0, Action = (byte)ContentRuleAction.GrantItemSet, ItemSetId = 0 });

            var validation = rows.Validate(ContentCapabilities.All);

            AssertGap(validation, ContentItemSetEntry.TableName, "0", "id 0 means none and cannot be referenced");
            AssertGap(validation, ContentConditionEntry.TableName, "0", "id 0 means none and cannot be referenced");
            AssertGap(validation, ContentRuleEntry.TableName, "0", "id 0 means none and cannot be referenced");
            AssertGap(validation, ContentRuleEntry.TableName, "9042", "duplicate key");
            AssertGap(validation, ContentPlacementEntry.TableName, "900694", "a container needs a known loot item set");
            AssertGap(validation, ContentRuleActionEntry.TableName, "9042/0", "unknown item set");
            Assert.AreEqual(0, validation.Catalog.ItemSets.Count);
            Assert.AreEqual(0, validation.Catalog.Conditions.Count);
        }

        [TestMethod]
        public void S1InitiationRowsGoLiveWhileUnimplementedKindsStayWithheld()
        {
            var rows = new Rows();
            // A mission-1990-shaped definition: an area-bound objective offered by the entered_map rule.
            rows.Areas.Add(new ContentAreaEntry { Id = 900600, MapContextId = 1985, Shape = (byte)ContentAreaShape.Sphere, Radius = 4 });
            rows.Bindings.Add(new NpcMissionObjectiveBindingEntry
                { MissionId = 900100, ObjectiveId = 1, BindingId = 0, Kind = (byte)ObjectiveBindingKind.AreaEntered, AreaId = 900600, CounterId = 255 });
            rows.Conditions.Add(new ContentConditionEntry { ConditionId = 900900, Kind = (byte)ContentConditionKind.MissionAbsent, MissionId = 900100 });
            rows.Rules.Add(new ContentRuleEntry { Id = 9001, MapContextId = 1985, Event = (byte)ContentRuleEvent.EnteredMap, ConditionId = 900900 });
            rows.Actions.Add(new ContentRuleActionEntry { RuleId = 9001, Sequence = 0, Action = (byte)ContentRuleAction.DispenseRadioMission, MissionId = 900100, Forced = true });
            // A binding of a kind S1 does not implement must still be withheld.
            rows.Bindings.Add(new NpcMissionObjectiveBindingEntry
                { MissionId = 900200, ObjectiveId = 1, BindingId = 0, Kind = (byte)ObjectiveBindingKind.Kill, CreatureId = 7001, CounterId = 255 });

            var validation = rows.Validate(MissionContentRules.Implemented);

            CollectionAssert.AreEqual(new[] { "npc_mission_objective_binding 900200/1/0: binding kind Kill is not implemented" },
                validation.MissionGaps[900200].ToArray());
            Assert.AreEqual(0, validation.Gaps.Count(gap => gap.OwnerId == 900100), string.Join(" | ", validation.Gaps));
            CollectionAssert.AreEqual(new uint[] { 9001 }, validation.LiveRules.Select(rule => rule.Id).ToArray());
            Assert.AreEqual(1, validation.LiveBindings.Count());
        }

        [TestMethod]
        public void MaterializerSelectsLiveCreaturePlacementsOfItsContextOnly()
        {
            var rows = new Rows();
            rows.Placements.Add(new ContentPlacementEntry
                { Id = 900650, MapContextId = 1985, Kind = (byte)ContentPlacementKind.Creature, CreatureId = 7001, Behavior = (byte)ContentPlacementBehavior.Stationary });
            rows.Placements.Add(new ContentPlacementEntry
                { Id = 900651, MapContextId = 1220, Kind = (byte)ContentPlacementKind.Creature, CreatureId = 7001, Behavior = (byte)ContentPlacementBehavior.Stationary });
            // Unknown creature: withheld even though its kind and behavior are implemented.
            rows.Placements.Add(new ContentPlacementEntry
                { Id = 900652, MapContextId = 1985, Kind = (byte)ContentPlacementKind.Creature, CreatureId = 999999, Behavior = (byte)ContentPlacementBehavior.Stationary });

            var validation = rows.Validate(MissionContentRules.Implemented);

            CollectionAssert.AreEqual(new uint[] { 900650 },
                ContentMaterializer.PlacementsToSpawn(validation, 1985).Select(placement => placement.Id).ToArray());
            Assert.IsTrue(validation.WithheldPlacements.Any(id => id == 900652));
        }

        [TestMethod]
        public void OfferMissionAtNpcOpensTheGiversOfferWindowWhenInRange()
        {
            WithLogger(() =>
            {
                using var connection = new SqliteConnection("Data Source=:memory:");
                connection.Open();
                using (var context = Context(connection))
                {
                    context.Database.EnsureCreated();
                    context.NpcMissionEntries.Add(new NpcMissionEntry { Id = 900100, GiverId = 7001, ReciverId = 7001, Level = 1, GroupType = 1, CategoryId = 1, Comment = "fixture" });
                    context.NpcMissionObjectiveEntries.Add(new NpcMissionObjectiveEntry { MissionId = 900100, ObjectiveId = 1, Ordinal = 1, IsRequired = true, RevealedOnAccept = true, Comment = "" });
                    context.NpcMissionObjectiveConversationEntries.Add(new NpcMissionObjectiveConversationEntry { MissionId = 900100, ObjectiveId = 1, NpcPackageId = 2584, PlayerFlagId = 1 });
                    context.ContentPlacementEntries.Add(new ContentPlacementEntry
                        { Id = 900650, MapContextId = 1985, Kind = (byte)ContentPlacementKind.Creature, CreatureId = 7001, Behavior = (byte)ContentPlacementBehavior.Stationary });
                    context.ContentRuleEntries.Add(new ContentRuleEntry { Id = 9001, MapContextId = 1985, Event = (byte)ContentRuleEvent.MissionTurnedIn, MissionId = 900100 });
                    context.ContentRuleActionEntries.Add(new ContentRuleActionEntry
                        { RuleId = 9001, Sequence = 0, Action = (byte)ContentRuleAction.OfferMissionAtNpc, MissionId = 900100, PlacementId = 900650 });
                    context.SaveChanges();
                }

                var missions = new MissionManager(new Factory(connection));
                missions.LoadMissions();
                Assert.IsTrue(missions.LoadedMissions[900100].IsDispensable);

                var references = new References();
                references.NotOfferable.Clear();
                var content = new MissionContentManager(new Factory(connection)) { Missions = missions };
                content.Load(() => new BootcampConfig(), references, missions.LoadedMissions);
                Assert.AreEqual(0, content.Content.Gaps.Count, string.Join(" | ", content.Content.Gaps));

                // The giver creature stands in the player's map, within conversation range.
                var creature = new Creature { DbId = 7001, MapContextId = 1985, Position = new System.Numerics.Vector3(10, 0, 10), Npc = new Npc() };
                EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
                EntityManager.Instance.RegisterCreature(creature);
                try
                {
                    var player = new Manifestation { MapContextId = 1985, Position = new System.Numerics.Vector3(12, 0, 10) };
                    var client = new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame, Player = player };

                    var action = content.Content.Catalog.RuleActions[9001].Single();
                    var reaction = new ContentReaction();
                    reaction.Add(new[] { action });
                    content.Present(client, reaction);

                    var messages = DrainCallMethods(client);
                    var converse = messages.Single(message => message.MethodId == GameOpcode.Converse);
                    Assert.AreEqual(creature.EntityId, converse.EntityId);
                }
                finally
                {
                    EntityManager.Instance.UnregisterCreature(creature.EntityId);
                    EntityManager.Instance.UnregisterEntity(creature.EntityId);
                    EntityManager.Instance.UnregisterActor(creature.EntityId);
                }
            });
        }

        [TestMethod]
        public void OfferMissionAtNpcIsSkippedWhenThePlayerIsOutOfConversationRange()
        {
            WithLogger(() =>
            {
                using var connection = new SqliteConnection("Data Source=:memory:");
                connection.Open();
                using (var context = Context(connection))
                {
                    context.Database.EnsureCreated();
                    context.NpcMissionEntries.Add(new NpcMissionEntry { Id = 900100, GiverId = 7001, ReciverId = 7001, Level = 1, GroupType = 1, CategoryId = 1, Comment = "fixture" });
                    context.NpcMissionObjectiveEntries.Add(new NpcMissionObjectiveEntry { MissionId = 900100, ObjectiveId = 1, Ordinal = 1, IsRequired = true, RevealedOnAccept = true, Comment = "" });
                    context.NpcMissionObjectiveConversationEntries.Add(new NpcMissionObjectiveConversationEntry { MissionId = 900100, ObjectiveId = 1, NpcPackageId = 2584, PlayerFlagId = 1 });
                    context.ContentPlacementEntries.Add(new ContentPlacementEntry
                        { Id = 900650, MapContextId = 1985, Kind = (byte)ContentPlacementKind.Creature, CreatureId = 7001, Behavior = (byte)ContentPlacementBehavior.Stationary });
                    context.ContentRuleEntries.Add(new ContentRuleEntry { Id = 9001, MapContextId = 1985, Event = (byte)ContentRuleEvent.MissionTurnedIn, MissionId = 900100 });
                    context.ContentRuleActionEntries.Add(new ContentRuleActionEntry
                        { RuleId = 9001, Sequence = 0, Action = (byte)ContentRuleAction.OfferMissionAtNpc, MissionId = 900100, PlacementId = 900650 });
                    context.SaveChanges();
                }

                var missions = new MissionManager(new Factory(connection));
                missions.LoadMissions();

                var references = new References();
                references.NotOfferable.Clear();
                var content = new MissionContentManager(new Factory(connection)) { Missions = missions };
                content.Load(() => new BootcampConfig(), references, missions.LoadedMissions);

                var creature = new Creature { DbId = 7001, MapContextId = 1985, Position = new System.Numerics.Vector3(10, 0, 10), Npc = new Npc() };
                EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
                EntityManager.Instance.RegisterCreature(creature);
                try
                {
                    var player = new Manifestation { MapContextId = 1985, Position = new System.Numerics.Vector3(100, 0, 100) };
                    var client = new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame, Player = player };

                    var action = content.Content.Catalog.RuleActions[9001].Single();
                    var reaction = new ContentReaction();
                    reaction.Add(new[] { action });
                    content.Present(client, reaction);

                    Assert.AreEqual(0, DrainCallMethods(client).Count);
                }
                finally
                {
                    EntityManager.Instance.UnregisterCreature(creature.EntityId);
                    EntityManager.Instance.UnregisterEntity(creature.EntityId);
                    EntityManager.Instance.UnregisterActor(creature.EntityId);
                }
            });
        }

        private static List<CallMethodMessage> DrainCallMethods(Client client)
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(client);
            var messages = new List<CallMethodMessage>();
            while (queue.PopOutgoing() is ProtocolPacket protocol)
                messages.Add((CallMethodMessage)protocol.Message);
            return messages;
        }
    }
}
