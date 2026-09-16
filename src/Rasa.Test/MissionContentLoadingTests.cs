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
                        { MissionId = 900100, ObjectiveId = 1, BindingId = 0, Kind = (byte)ObjectiveBindingKind.UseCompleted, CounterId = 255 });
                    context.SaveChanges();
                }

                var missions = new MissionManager(new Factory(connection));
                missions.LoadMissions();
                Assert.IsTrue(missions.LoadedMissions[900100].IsDispensable);     // the conversation binding alone is complete

                var references = new References();
                references.Missions[900100] = (7001, new uint[] { 1 });
                references.NotOfferable.Clear();
                // Every binding kind is implemented now; a capability set without them stands in for an older build.
                var withoutBindings = new ContentCapabilities();
                var content = new MissionContentManager(new Factory(connection)) { Missions = missions };
                content.Load(() => new BootcampConfig(), references, missions.LoadedMissions, withoutBindings);

                Assert.AreEqual(1, content.Content.Catalog.RowCount);              // the binding
                CollectionAssert.AreEqual(new[] { "900100/1/0: binding kind UseCompleted is not implemented", "900100/1/0: needs a usable placement" },
                    Messages(content.Content, NpcMissionObjectiveBindingEntry.TableName));
                Assert.IsFalse(missions.LoadedMissions[900100].IsDispensable);
                CollectionAssert.Contains(missions.LoadedMissions[900100].DefinitionGaps(),
                    "npc_mission_objective_binding 900100/1/0: binding kind UseCompleted is not implemented");
                Assert.AreEqual(0, content.Content.LiveBindings.Count());

                // Reloading clears gaps from the previous load rather than accumulating them.
                content.Load(() => new BootcampConfig(), references, missions.LoadedMissions, withoutBindings);
                Assert.AreEqual(2, missions.LoadedMissions[900100].ContentGaps.Count);
            });
        }

        [TestMethod]
        public void S1ImplementsExactlyTheBootcampInitiationMechanics()
        {
            var implemented = MissionContentRules.Implemented;
            CollectionAssert.AreEquivalent((ObjectiveBindingKind[])System.Enum.GetValues(typeof(ObjectiveBindingKind)), implemented.BindingKinds.ToArray());
            CollectionAssert.AreEquivalent(new[]
            {
                ContentRuleEvent.EnteredMap, ContentRuleEvent.MissionAccepted,
                ContentRuleEvent.ObjectiveCompleted, ContentRuleEvent.MissionTurnedIn, ContentRuleEvent.AreaEntered,
                ContentRuleEvent.PlacementStateEntered, ContentRuleEvent.ObjectiveFailed, ContentRuleEvent.MissionFailed,
                ContentRuleEvent.ClassSelected
            }, implemented.Events.ToArray());
            CollectionAssert.AreEquivalent(new[]
            {
                ContentRuleAction.DispenseRadioMission, ContentRuleAction.OfferMissionAtNpc, ContentRuleAction.GrantLogos,
                ContentRuleAction.ForceConverseGreeting, ContentRuleAction.TutorialNotification, ContentRuleAction.GrantRewards,
                ContentRuleAction.TransferToLocation, ContentRuleAction.SetAccountSkipBootcamp,
                ContentRuleAction.SetFact, ContentRuleAction.ClearFact, ContentRuleAction.SetPlacementState,
                ContentRuleAction.MoveCreatureToLocation, ContentRuleAction.DamagePlayer
            }, implemented.Actions.ToArray());
            CollectionAssert.AreEquivalent(new[]
            {
                ContentConditionKind.MissionAbsent, ContentConditionKind.MissionStateIs, ContentConditionKind.ObjectiveStateIs,
                ContentConditionKind.FactEquals, ContentConditionKind.HasLogos, ContentConditionKind.CharacterClassIs
            }, implemented.ConditionKinds.ToArray());
            CollectionAssert.AreEquivalent(new[] { ContentPlacementKind.Creature, ContentPlacementKind.Usable }, implemented.PlacementKinds.ToArray());
            CollectionAssert.AreEquivalent(new[] { ContentPlacementBehavior.Stationary, ContentPlacementBehavior.CreatureAi }, implemented.PlacementBehaviors.ToArray());
            CollectionAssert.AreEquivalent(new[] { ContentUsableKind.Container, ContentUsableKind.Destroyable, ContentUsableKind.Bomb, ContentUsableKind.GenericUse, ContentUsableKind.Structure }, implemented.UsableKinds.ToArray());
            CollectionAssert.AreEquivalent(new[] { MapInstancing.Shared, MapInstancing.PerCharacter }, implemented.Instancing.ToArray());
            Assert.IsTrue(implemented.Counters);
            Assert.IsTrue(implemented.Timers && implemented.Indicators);
            Assert.IsFalse(implemented.PlacementRespawn);
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
        public void TimersThatCouldStrandTheCharacterAreWithheld()
        {
            var references = new References();
            references.Missions[1990] = (7001, new uint[] { 1 });

            var rows = new Rows();
            // Fails the mission, and no mission is offered after this one failed.
            rows.Timers.Add(new NpcMissionObjectiveTimerEntry { MissionId = 900300, ObjectiveId = 1, LimitSeconds = 600, OnExpire = (byte)ObjectiveTimerExpiry.FailObjectiveAndMission });
            // Failing only the objective needs no retry, but the client forbids abandoning 1990.
            rows.Timers.Add(new NpcMissionObjectiveTimerEntry { MissionId = 1990, ObjectiveId = 1, LimitSeconds = 60, OnExpire = (byte)ObjectiveTimerExpiry.FailObjective });
            // A retry offered after 900100 fails makes its mission-failing timer valid.
            rows.Prerequisites.Add(new NpcMissionPrerequisiteEntry { MissionId = 900200, OrGroup = 1, RequiredMissionId = 900100, RequiredState = (byte)MissionState.Failded });
            rows.Timers.Add(new NpcMissionObjectiveTimerEntry { MissionId = 900100, ObjectiveId = 2, LimitSeconds = 600, OnExpire = (byte)ObjectiveTimerExpiry.FailObjectiveAndMission });

            var validation = rows.Validate(ContentCapabilities.All, references);

            CollectionAssert.AreEquivalent(new[]
            {
                "npc_mission_objective_timer 900300/1: timed objective 900300/1 can fail the mission but no retry is offered",
                "npc_mission_objective_timer 1990/1: a non-abandonable mission cannot have a timer"
            }, validation.Gaps.Select(gap => gap.ToString()).ToArray());
            CollectionAssert.AreEquivalent(new[] { (900100u, 2u) }, validation.LiveTimers.Select(timer => (timer.MissionId, timer.ObjectiveId)).ToArray());
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
            // A binding the server cannot resolve must still be withheld: a hit by an action without a server implementation.
            rows.Bindings.Add(new NpcMissionObjectiveBindingEntry
                { MissionId = 900200, ObjectiveId = 1, BindingId = 0, Kind = (byte)ObjectiveBindingKind.Hit, CreatureId = 7001, ActionId = 999, CounterId = 255 });

            var validation = rows.Validate(MissionContentRules.Implemented);

            CollectionAssert.AreEqual(new[]
                {
                    "npc_mission_objective_binding 900200/1/0: hit by action 999 has no server implementation"
                },
                validation.MissionGaps[900200].ToArray());
            Assert.AreEqual(0, validation.Gaps.Count(gap => gap.OwnerId == 900100), string.Join(" | ", validation.Gaps));
            CollectionAssert.AreEqual(new uint[] { 9001 }, validation.LiveRules.Select(rule => rule.Id).ToArray());
            Assert.AreEqual(1, validation.LiveBindings.Count());
        }

        /// <summary>
        /// References resolved against a migrated world database: map contexts, creatures, logos and legacy
        /// objects come from its tables, missions from the loaded definitions (as the server's own
        /// LoadedContentReferences does). The entity-class and item-template seed is not replayed in unit tests,
        /// so those two lookups take the client ids the boot-camp rows name.
        /// </summary>
        private sealed class MigratedWorldReferences : IContentReferences
        {
            private readonly HashSet<uint> _contexts;
            public readonly HashSet<uint> Creatures;
            private readonly HashSet<uint> _logos;
            private readonly HashSet<uint> _legacyContexts;
            private readonly IReadOnlyDictionary<uint, Mission> _missions;
            public HashSet<uint> Classes = new();
            public HashSet<uint> Items = new();

            public MigratedWorldReferences(SqliteConnection connection, IReadOnlyDictionary<uint, Mission> missions)
            {
                _missions = missions;
                _contexts = Ids(connection, "SELECT map_context_id FROM map_info");
                Creatures = Ids(connection, "SELECT id FROM creature");
                _logos = Ids(connection, "SELECT id FROM logos");
                _legacyContexts = Ids(connection, "SELECT map_context_id FROM spawnpool UNION SELECT map_context_id FROM footlocker UNION SELECT map_context_id FROM logos");
            }

            private static HashSet<uint> Ids(SqliteConnection connection, string sql)
            {
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                using var reader = command.ExecuteReader();
                var ids = new HashSet<uint>();
                while (reader.Read())
                    ids.Add((uint)reader.GetInt64(0));
                return ids;
            }

            public bool MapContextExists(uint mapContextId) => _contexts.Contains(mapContextId);
            public bool MissionExists(uint missionId) => _missions.ContainsKey(missionId);
            public bool ObjectiveExists(uint missionId, uint objectiveId) => _missions.TryGetValue(missionId, out var m) && m.Objectives.ContainsKey(objectiveId);
            public uint MissionGiver(uint missionId) => _missions.TryGetValue(missionId, out var m) ? m.MissionGiver : 0;
            public bool MissionOfferable(uint missionId) => _missions.TryGetValue(missionId, out var m) && m.IsDispensable;
            public bool CreatureExists(uint creatureId) => Creatures.Contains(creatureId);
            public bool EntityClassExists(uint entityClassId) => Classes.Contains(entityClassId);
            public bool ItemTemplateExists(uint itemTemplateId) => Items.Contains(itemTemplateId);
            public bool LogosExists(uint logosId) => _logos.Contains(logosId);
            public bool HasLegacyWorldObjects(uint mapContextId) => _legacyContexts.Contains(mapContextId);
        }

        [TestMethod]
        public void SeededBootcampContentGoesLiveWithTheImplementedMechanics()
        {
            WithLogger(() =>
            {
                using var connection = new SqliteConnection("Data Source=:memory:");
                connection.Open();
                using (var context = Context(connection))
                {
                    ContentSchemaMigrationTests.CreatePreviousWorld(context, connection);
                    // Seed-data rows the boot-camp content references outside its own migrations (the full world
                    // seed is not replayed): the boot-camp map, the S6 destination map and the Power Logos granted by S1.
                    // 1148 is the Divide, where the four NPCs W3 batch 5 creates stand; 1244 Palisades, which the
                    // Liaison missions' receivers are in.
                    context.Database.ExecuteSqlRaw("INSERT INTO map_info (map_context_id, map_name, map_version, base_region) VALUES (1985, 'adv_bootcamp', 783, 4), (1220, 'adv_foreas_concordia_wilderness', 1556, 0), (1148, 'adv_foreas_concordia_divide', 1584, 10), (1244, 'adv_foreas_concordia_palisades', 1584, 10), (1497, 'adv_foreas_valverde_plateau', 1584, 10), (1304, 'adv_foreas_valverde_pools', 1584, 10), (1454, 'adv_foreas_valverde_marshes', 1584, 10)");
                    context.Database.ExecuteSqlRaw("INSERT INTO logos (id, class_id, map_context_id, pos_x, pos_y, pos_z, name) VALUES (23, 7302, 1220, 1, 2, 3, 'Power')");
                    context.Database.Migrate();
                }

                // The Training Day reward pistols load through the real ItemManager from their migrated itemtemplate and
                // itemtemplate_weapon rows (WildernessArrivalTrainingDay) before the missions build their reward info.
                RewardItemFixtures.SeedOriginalTemplateRows(connection);
                using var rewardItems = new RewardItemFixtures(connection);

                var missions = new MissionManager(new Factory(connection));
                missions.LoadMissions();

                var references = new MigratedWorldReferences(connection, missions.LoadedMissions);
                // S2 usable placements: crate 26714 UsableTreasureDispHumCrateV04, dummies 29365 UsableStatelessHumPracticeDummyV01.
                // S5: Conrad's corpse 21961 UsableStatelessFlightSalvage, bomb 7870 UsableBombHumV01, wreck 24586.
                references.Classes.UnionWith(new uint[] { 26714, 29365, 21961, 7870, 24586 });
                // S2 crate item set 19858.
                references.Items.UnionWith(new uint[] { 13066, 13096, 13156, 13186, 13713 });
                // Kill bindings name world-seed creatures: the Wilderness hub's Proctor Fulgor (76) and Arioch Xanx
                // (77). The real runtime resolves those through CreatureManager.LoadedCreatures; this migrated test
                // world carries only the content's own rows, so the two ids the bindings use are declared here.
                references.Creatures.UnionWith(new uint[] { 76, 77 });

                var content = new MissionContentManager(new Factory(connection)) { Missions = missions };
                content.Load(() => new BootcampConfig(), references, missions.LoadedMissions, MissionContentRules.Implemented);
                var validation = content.Content;

                Assert.AreEqual(0, validation.Gaps.Count, string.Join(" | ", validation.Gaps));
                Assert.IsFalse(validation.WithheldContexts.Contains(1985u));
                Assert.AreEqual(MapInstancing.PerCharacter, validation.Catalog.InstancingFor(1985));

                foreach (var missionId in new uint[] { 1990, 1992, 1994, 1995, 2005, 1526, 2010, 2011 })
                {
                    Assert.IsFalse(validation.MissionGaps.ContainsKey(missionId), $"mission {missionId}: {string.Join(" | ", validation.MissionGaps.GetValueOrDefault(missionId) ?? Array.Empty<string>())}");
                    CollectionAssert.AreEqual(Array.Empty<string>(), missions.LoadedMissions[missionId].DefinitionGaps(), $"mission {missionId}");
                    Assert.IsTrue(missions.LoadedMissions[missionId].IsDispensable, $"mission {missionId}");
                }

                // Capture the Flag: both bindings, the boss counter, both indicators and the prerequisite are attached live.
                var captureTheFlag = missions.LoadedMissions[1994];
                CollectionAssert.AreEquivalent(new[] { (1u, ObjectiveBindingKind.Kill), (2u, ObjectiveBindingKind.AreaEntered) },
                    captureTheFlag.Bindings.Select(binding => (binding.ObjectiveId, (ObjectiveBindingKind)binding.Kind)).ToArray());
                Assert.AreEqual(1, captureTheFlag.Counters[1].Single().TargetValue);
                CollectionAssert.AreEquivalent(new uint[] { 437, 439 }, captureTheFlag.Indicators.Values.SelectMany(list => list).Select(indicator => indicator.IndicatorId).ToArray());
                Assert.AreEqual(1992u, captureTheFlag.Prerequisites.Single().RequiredMissionId);

                var livePlacements = validation.LivePlacements.Select(placement => placement.Id).ToList();
                for (uint id = 198658; id <= 198683; id++)
                    CollectionAssert.Contains(livePlacements, id);
                Assert.IsTrue(validation.LiveRules.Any(rule => rule.Id == 1985005));

                // Calling for Reinforcements: 2 -> 3 -> 1 -> 4, the corpse use and the bomb detonation, the 600 s timer that
                // fails the mission, indicators 435/432 (S5) and 438 (S6), and the prerequisite on 1994.
                var reinforcements = missions.LoadedMissions[1995];
                CollectionAssert.AreEquivalent(new[] { (3u, ObjectiveBindingKind.UseCompleted, 198676u, 0u), (1u, ObjectiveBindingKind.PlacementState, 198677u, 115u) },
                    reinforcements.Bindings.Select(binding => (binding.ObjectiveId, (ObjectiveBindingKind)binding.Kind, binding.PlacementId, binding.TargetState)).ToArray());
                CollectionAssert.AreEquivalent(new uint[] { 2 }, reinforcements.Objectives.Values.Where(objective => objective.RevealedOnAccept == true).Select(objective => objective.ObjectiveId).ToArray());
                CollectionAssert.AreEqual(new uint[] { 3 }, reinforcements.Transitions[2]);
                CollectionAssert.AreEqual(new uint[] { 1 }, reinforcements.Transitions[3]);
                CollectionAssert.AreEqual(new uint[] { 4 }, reinforcements.Transitions[1]);
                Assert.AreEqual((600u, (byte)ObjectiveTimerExpiry.FailObjectiveAndMission), (reinforcements.Timers[1].LimitSeconds, reinforcements.Timers[1].OnExpire));
                Assert.AreEqual(1, reinforcements.Timers.Count);
                CollectionAssert.AreEquivalent(new[] { (1u, 432u, false), (2u, 435u, true), (4u, 438u, true) },
                    reinforcements.Indicators.SelectMany(pair => pair.Value.Select(indicator => (pair.Key, indicator.IndicatorId, indicator.Show3d))).ToArray());
                Assert.AreEqual((1994u, (byte)MissionState.Completed), (reinforcements.Prerequisites.Single().RequiredMissionId, reinforcements.Prerequisites.Single().RequiredState));
                Assert.IsTrue(reinforcements.HasObjectiveConversation(2, 2584, 1) && reinforcements.HasObjectiveConversation(4, 2564, 1));
                Assert.AreEqual((198505u, 198514u), (reinforcements.MissionGiver, reinforcements.MissionReciver));

                // The retry: 1 -> 4 with the same bomb binding and timer; offered after a failed 1995 and again after its own failure.
                var retry = missions.LoadedMissions[2005];
                Assert.AreEqual((1u, ObjectiveBindingKind.PlacementState, 198677u, 115u),
                    retry.Bindings.Select(binding => (binding.ObjectiveId, (ObjectiveBindingKind)binding.Kind, binding.PlacementId, binding.TargetState)).Single());
                Assert.AreEqual((600u, (byte)ObjectiveTimerExpiry.FailObjectiveAndMission), (retry.Timers[1].LimitSeconds, retry.Timers[1].OnExpire));
                CollectionAssert.AreEquivalent(new[] { ((byte)0, 1995u, (byte)MissionState.Failded), ((byte)0, 2005u, (byte)MissionState.NotAssigned), ((byte)1, 1995u, (byte)MissionState.Failded), ((byte)1, 2005u, (byte)MissionState.Failded) },
                    retry.Prerequisites.Select(prerequisite => (prerequisite.OrGroup, prerequisite.RequiredMissionId, prerequisite.RequiredState)).ToArray());
                Assert.IsTrue(retry.HasObjectiveConversation(4, 2564, 1));
                Assert.AreEqual(198514u, retry.MissionReciver);
                Assert.AreEqual(0, retry.Indicators.Count);

                // The usables and their conditions: the corpse while (1995,3) is open, the bomb while the dropship stands and one
                // of the bomb objectives is open (armed again after a rebuild while planted), the wreck open once destroyed.
                var placements = validation.Catalog.Placements;
                Assert.AreEqual(((byte)ContentUsableKind.GenericUse, 44u, 198908u), (placements[198676].UsableKind, placements[198676].InitialState, placements[198676].UsableConditionId));
                Assert.AreEqual(((byte)ContentUsableKind.Bomb, 113u, 114u, 198904u, 1420u, 4930u, 198906u, 198903u),
                    (placements[198677].UsableKind, placements[198677].InitialState, placements[198677].AlternateState, placements[198677].AlternateStateConditionId,
                     placements[198677].WindupMs, placements[198677].FuseMs, placements[198677].PresentConditionId, placements[198677].UsableConditionId));
                Assert.AreEqual(((byte)ContentUsableKind.Structure, 31u, 91u, 198905u), (placements[198678].UsableKind, placements[198678].InitialState, placements[198678].AlternateState, placements[198678].AlternateStateConditionId));
                foreach (var id in new uint[] { 198679, 198680, 198681, 198682 })
                    Assert.AreEqual((198907u, (byte)ContentPlacementBehavior.Stationary), (placements[id].PresentConditionId, placements[id].Behavior), $"placement {id}");
                Assert.AreEqual(((byte)ContentPlacementBehavior.CreatureAi, 0u), (placements[198683].Behavior, placements[198683].PresentConditionId));
                Assert.AreEqual((2584u, 2564u), (placements[198675].NpcPackageId, placements[198679].NpcPackageId));

                // Rogers (BootcampFixRogersTurnIn, GAP-ROGERS): the 1995/2005 receiver stands live in shared Alia Das, always present.
                Assert.AreEqual(MapInstancing.Shared, validation.Catalog.InstancingFor(1220));
                Assert.IsFalse(validation.WithheldContexts.Contains(1220u));
                var aliaDasPlacements = ContentMaterializer.PlacementsToSpawn(validation, 1220).ToDictionary(placement => placement.Id);
                // 199002 (Field Dr. Dawson) and 199003 (Receptive Liaison Brice) were created by W3 batch 5 from
                // TaRapedia's own zone for them, which is the Wilderness, so they stand in the same shared context.
                CollectionAssert.AreEquivalent(new uint[] { 198684, 198685, 199002, 199003 }, aliaDasPlacements.Keys.ToArray());
                var rogers = aliaDasPlacements[198684];
                Assert.AreEqual((198684u, 198514u, 116u, (byte)ContentPlacementBehavior.Stationary, 0u, 0u),
                    (rogers.Id, rogers.CreatureId, rogers.NpcPackageId, rogers.Behavior, rogers.PresentConditionId, rogers.AlternateStateConditionId));
                Assert.AreEqual((855.84, 294.14, 387.4, 4.3633), (rogers.PosX, rogers.PosY, rogers.PosZ, rogers.Rotation));

                // Training Officer Kincaid (WildernessArrivalTrainingDay): the 1526 receiver and class trainer stands live in shared
                // Alia Das at the measured barracks position with the trainer package, always present.
                var kincaid = aliaDasPlacements[198685];
                Assert.AreEqual((198515u, 2588u, (byte)ContentPlacementBehavior.Stationary, 0u, 0u),
                    (kincaid.CreatureId, kincaid.NpcPackageId, kincaid.Behavior, kincaid.PresentConditionId, kincaid.AlternateStateConditionId));
                Assert.AreEqual((765.4, 294.12, 386.05, 1.5708), (kincaid.PosX, kincaid.PosY, kincaid.PosZ, kincaid.Rotation));
                Assert.IsTrue(ClassAdvancement.TrainerNpcPackages.Contains(kincaid.NpcPackageId));

                // Training Day: offered over the radio (no giver), completed by the Kincaid conversation, turned in at him, paying
                // 120 credits and a choice of the two pistols, whose templates load with their weapon rows.
                var trainingDay = missions.LoadedMissions[1526];
                Assert.AreEqual((0u, 198515u, 4u, 10000001u, false, false),
                    (trainingDay.MissionGiver, trainingDay.MissionReciver, trainingDay.MissionConstantData.Level, trainingDay.MissionConstantData.CategoryId,
                     trainingDay.MissionConstantData.Shareable, trainingDay.MissionConstantData.RadioCompletable));
                Assert.IsTrue(trainingDay.HasObjectiveConversation(1, 2588, 1));
                Assert.AreEqual((1u, true, true), (trainingDay.Objectives[1].Ordinal.Value, trainingDay.Objectives[1].IsRequired.Value, trainingDay.Objectives[1].RevealedOnAccept.Value));
                Assert.AreEqual(0, trainingDay.Prerequisites.Count);
                CollectionAssert.AreEqual(Array.Empty<string>(), trainingDay.RewardGaps);
                Assert.AreEqual((120L, 0L, 0L), (trainingDay.RewardCredits, trainingDay.RewardPrestige, trainingDay.RewardExperience));
                Assert.AreEqual(0, trainingDay.OfferedFixedItems.Count);
                CollectionAssert.AreEqual(new[] { (116929u, (EntityClasses)27121, 1u, 3), (116930u, (EntityClasses)27100, 1u, 3) },
                    trainingDay.MissionConstantData.RewardInfo.SelectableReward.Select(item => (item.ItemTemplateId, item.Class, item.Quantity, item.QualityId)).ToArray());
                foreach (var (templateId, refire, altDamage) in new[] { (116929u, 150u, 115u), (116930u, 100u, 122u) })
                {
                    var template = rewardItems.Items.GetItemTemplateById(templateId);
                    Assert.AreEqual((InventoryCategory.Equipment, 3, false, false), (template.InventoryCategory, template.QualityId, template.HasSellableFlag, template.ItemInfo.Tradable));
                    Assert.AreEqual((1, 1), (template.EquipableInfo.SkillId, template.EquipableInfo.SkillLevel));
                    Assert.AreEqual(5, template.ItemInfo.Requirements[RequirementsType.ReqXpLevel]);
                    Assert.IsNotNull(template.WeaponInfo, $"template {templateId} has no weapon row");
                    Assert.AreEqual((0u, 20u, 1u, 0u, 250u, refire), (template.WeaponInfo.AeType, template.WeaponInfo.Range, template.WeaponInfo.AmmoPerShot,
                        template.WeaponInfo.Windup, template.WeaponInfo.Recovery, template.WeaponInfo.Refire));
                    Assert.AreEqual((altDamage, 1u), (template.WeaponInfo.WeaponAltInfo.AltMaxDamage, template.WeaponInfo.WeaponAltInfo.AltDamageType));

                    // A tooltip request for the offered reward writes its weapon, equipable and item tuples without a missing row.
                    using var stream = new System.IO.MemoryStream();
                    using var writer = new PythonWriter(new System.IO.BinaryWriter(stream));
                    new Rasa.Packets.MapChannel.Server.ItemTemplateTooltipInfoPacket(template, EntityClassManager.Instance.GetClassInfo(template.Class)).Write(writer);
                    Assert.IsTrue(stream.Length > 0);
                }

                // The class-gear missions (WildernessClassGear): the Soldier/Specialist load-out, offered on the class
                // choice, turned in at Quartermaster Caufield (emulator creature 132 with the dialogue package 133).
                foreach (var (missionId, categoryId, gear) in new[]
                {
                    (2010u, 10000002u, new[] { 122859u, 122860u, 122862u, 122863u, 122864u, 122865u }),
                    (2011u, 10000003u, new[] { 122866u, 122867u, 122868u, 122869u, 122870u, 122871u })
                })
                {
                    var classGear = missions.LoadedMissions[missionId];
                    Assert.AreEqual((0u, 132u, 5u, categoryId, false, false),
                        (classGear.MissionGiver, classGear.MissionReciver, classGear.MissionConstantData.Level, classGear.MissionConstantData.CategoryId,
                         classGear.MissionConstantData.Shareable, classGear.MissionConstantData.RadioCompletable));
                    Assert.IsTrue(classGear.HasObjectiveConversation(1, 133, 1), $"mission {missionId} turns in at Caufield");
                    Assert.AreEqual((1u, true, true), (classGear.Objectives[1].Ordinal.Value, classGear.Objectives[1].IsRequired.Value, classGear.Objectives[1].RevealedOnAccept.Value));
                    Assert.AreEqual(0, classGear.Prerequisites.Count);
                    CollectionAssert.AreEqual(Array.Empty<string>(), classGear.RewardGaps);
                    Assert.AreEqual((0L, 6), (classGear.RewardCredits, classGear.OfferedFixedItems.Count));
                    CollectionAssert.AreEqual(gear,
                        classGear.MissionConstantData.RewardInfo.FixedReward.FixedItems.Select(item => item.ItemTemplateId).ToArray());
                }

                // The gear pieces load as equipment with their class skill requirement and the level-5 requirement, and
                // the two weapons/tools carry a weapon row; the armor pieces carry an armor value.
                foreach (var templateId in new uint[] { 122859u, 122860u, 122862u, 122863u, 122864u, 122866u, 122867u, 122868u, 122869u, 122870u })
                {
                    var template = rewardItems.Items.GetItemTemplateById(templateId);
                    Assert.AreEqual((InventoryCategory.Equipment, 2, true, true), (template.InventoryCategory, template.QualityId, template.HasSellableFlag, template.ItemInfo.Tradable));
                    Assert.AreEqual(5, template.ItemInfo.Requirements[RequirementsType.ReqXpLevel]);
                    Assert.IsTrue(template.ArmorValue > 0, $"template {templateId} has no armor value");
                }
                Assert.AreEqual((21, 30), (rewardItems.Items.GetItemTemplateById(122859u).EquipableInfo.SkillId, rewardItems.Items.GetItemTemplateById(122866u).EquipableInfo.SkillId));
                Assert.AreEqual((22, 14), (rewardItems.Items.GetItemTemplateById(122865u).EquipableInfo.SkillId, rewardItems.Items.GetItemTemplateById(122871u).EquipableInfo.SkillId));
                foreach (var templateId in new uint[] { 122865u, 122871u })
                {
                    var template = rewardItems.Items.GetItemTemplateById(templateId);
                    Assert.IsNotNull(template.WeaponInfo, $"template {templateId} has no weapon row");
                    Assert.AreEqual((1.0, 1500u, 0u, 1u, 800u, 1u, 800u, 80u), (template.WeaponInfo.AimRate, template.WeaponInfo.ReloadTime,
                        template.WeaponInfo.AeType, template.WeaponInfo.AmmoPerShot, template.WeaponInfo.Windup, template.WeaponInfo.Recovery,
                        template.WeaponInfo.Refire, template.WeaponInfo.Range));
                }

                var conditions = validation.Catalog.Conditions;
                string Terms(uint conditionId) => string.Join(" | ", conditions[conditionId].Select(term =>
                    $"{term.OrGroup}.{term.TermIndex}:{(ContentConditionKind)term.Kind} {term.MissionId}/{term.ObjectiveId}={term.State} {term.FactKey}={term.Value}{(term.Negate ? " not" : "")}"));
                Assert.AreEqual("0.0:ObjectiveStateIs 1995/3=2 =0 | 0.1:ObjectiveStateIs 1995/1=1 =0 | 1.0:ObjectiveStateIs 2005/1=1 =0", Terms(198903));
                Assert.AreEqual("0.0:FactEquals 0/0=0 bootcamp.bomb_planted=1", Terms(198904));
                Assert.AreEqual("0.0:FactEquals 0/0=0 bootcamp.dropship_destroyed=1", Terms(198905));
                Assert.AreEqual("0.0:FactEquals 0/0=0 bootcamp.dropship_destroyed=1 not", Terms(198906));
                Assert.AreEqual("0.0:FactEquals 0/0=0 bootcamp.dropship_destroyed=1", Terms(198907));
                Assert.AreEqual("0.0:ObjectiveStateIs 1995/3=1 =0", Terms(198908));
                Assert.AreEqual("0.0:ObjectiveStateIs 1995/4=2 =0 | 1.0:ObjectiveStateIs 2005/4=2 =0", Terms(198909));
                // The forced Training Day offer on entering Alia Das: objective 4 of 1995 or 2005 completed and no 1526 row yet.
                Assert.AreEqual("0.0:ObjectiveStateIs 1995/4=2 =0 | 0.1:MissionAbsent 1526/0=0 =0 | 1.0:ObjectiveStateIs 2005/4=2 =0 | 1.1:MissionAbsent 1526/0=0 =0", Terms(198910));
                // The class-gear offers: the chosen class (2 Soldier, 3 Specialist) and no row of that mission yet.
                Assert.AreEqual("0.0:CharacterClassIs 0/0=0 =2 | 0.1:MissionAbsent 2010/0=0 =0", Terms(198911));
                Assert.AreEqual("0.0:CharacterClassIs 0/0=0 =3 | 0.1:MissionAbsent 2011/0=0 =0", Terms(198912));

                // The rules: plant and detonation facts, the wreck bursting open, failures bringing the ship back (the D13.4 quirk
                // is kept: abandoning clears nothing), and the exit pad transferring to Alia Das before setting the skip flag.
                var actions = validation.Catalog.RuleActions;
                string Actions(uint ruleId) => string.Join(" | ", actions[ruleId].Select(action =>
                    $"{(ContentRuleAction)action.Action} {action.FactKey}{(action.FactValue != 0 ? "=" + action.FactValue : "")}{(action.PlacementId != 0 ? $" {action.PlacementId}->{action.StateId}" : "")}{(action.LocationId != 0 ? $" {action.LocationId}" : "")}".TrimEnd()));
                foreach (var id in new uint[] { 1985006, 1985007, 1985008, 1985009, 1985010 })
                    Assert.IsTrue(validation.LiveRules.Any(rule => rule.Id == id), $"rule {id}");
                var rules = validation.Catalog.Rules;
                Assert.AreEqual((ContentRuleEvent.PlacementStateEntered, 198677u, 114u), ((ContentRuleEvent)rules[1985006].Event, rules[1985006].PlacementId, rules[1985006].StateId));
                Assert.AreEqual("SetFact bootcamp.bomb_planted=1", Actions(1985006));
                Assert.AreEqual((ContentRuleEvent.PlacementStateEntered, 198677u, 115u), ((ContentRuleEvent)rules[1985007].Event, rules[1985007].PlacementId, rules[1985007].StateId));
                Assert.AreEqual("SetFact bootcamp.dropship_destroyed=1 | ClearFact bootcamp.bomb_planted | SetPlacementState  198678->91 | DamagePlayer", Actions(1985007));
                foreach (var (ruleId, missionId) in new[] { (1985008u, 1995u), (1985009u, 2005u) })
                {
                    Assert.AreEqual((ContentRuleEvent.ObjectiveFailed, missionId, 1u), ((ContentRuleEvent)rules[ruleId].Event, rules[ruleId].MissionId, rules[ruleId].ObjectiveId));
                    Assert.AreEqual("ClearFact bootcamp.dropship_destroyed | ClearFact bootcamp.bomb_planted", Actions(ruleId));
                }
                Assert.IsFalse(rules.Values.Any(rule => (ContentRuleEvent)rule.Event == ContentRuleEvent.MissionAbandoned));

                Assert.AreEqual((ContentRuleEvent.AreaEntered, 198603u, 198909u), ((ContentRuleEvent)rules[1985010].Event, rules[1985010].AreaId, rules[1985010].ConditionId));
                Assert.AreEqual("TransferToLocation  19852 | SetAccountSkipBootcamp", Actions(1985010));
                Assert.IsTrue(validation.LiveRules.Any(rule => rule.Id == 1985011));
                Assert.AreEqual((ContentRuleEvent.EnteredMap, 1220u, 198910u, 0u), ((ContentRuleEvent)rules[1985011].Event, rules[1985011].MapContextId, rules[1985011].ConditionId, rules[1985011].MissionId));
                var offer = actions[1985011].Single();
                Assert.AreEqual((ContentRuleAction.DispenseRadioMission, 1526u, true), ((ContentRuleAction)offer.Action, offer.MissionId, offer.Forced));

                // The class-gear offers: the class_selected event in Alia Das dispenses the matching load-out by force.
                foreach (var (ruleId, missionId, conditionId) in new[] { (1985012u, 2010u, 198911u), (1985013u, 2011u, 198912u) })
                {
                    Assert.IsTrue(validation.LiveRules.Any(rule => rule.Id == ruleId), $"rule {ruleId}");
                    Assert.AreEqual((ContentRuleEvent.ClassSelected, 1220u, conditionId, 0u), ((ContentRuleEvent)rules[ruleId].Event, rules[ruleId].MapContextId, rules[ruleId].ConditionId, rules[ruleId].MissionId));
                    var gearOffer = actions[ruleId].Single();
                    Assert.AreEqual((ContentRuleAction.DispenseRadioMission, missionId, true), ((ContentRuleAction)gearOffer.Action, gearOffer.MissionId, gearOffer.Forced));
                }
                var pad = validation.Catalog.Areas[198603];
                // Vertical cylinder with a 25 m half-height: the pad trigger sits on the player's level like the S1 pair
                // (OD-44), so a height error cannot eat its reach as it could for a sphere.
                Assert.AreEqual(((byte)ContentAreaShape.VerticalCylinder, -225.35, 99.6, -70.52, 12.0, 25.0), (pad.Shape, pad.PosX, pad.PosY, pad.PosZ, pad.Radius, pad.HalfHeight));
                var aliaDas = validation.Catalog.Locations[19852];
                Assert.AreEqual(((byte)ContentLocationPurpose.TransferDestination, 1220u, 884.11, 305.8, 347.81), (aliaDas.Purpose, aliaDas.MapContextId, aliaDas.PosX, aliaDas.PosY, aliaDas.PosZ));
            });
        }

        [TestMethod]
        public void OwnerConditionedPlacementsFollowTheOwnersCommittedStateInTheirInstanceOnly()
        {
            var rows = new Rows();
            rows.MapSettings.Add(new ContentMapSettingEntry { MapContextId = 1985, Instancing = (byte)MapInstancing.PerCharacter });
            rows.Conditions.Add(new ContentConditionEntry
                { ConditionId = 900901, Kind = (byte)ContentConditionKind.ObjectiveStateIs, MissionId = 900100, ObjectiveId = 1, State = (uint)MissionObjectiveState.Completed });
            rows.Placements.Add(new ContentPlacementEntry
            {
                Id = 900760, MapContextId = 1985, Kind = (byte)ContentPlacementKind.Usable, EntityClassId = 7870, UsableKind = (byte)ContentUsableKind.Destroyable,
                Behavior = (byte)ContentPlacementBehavior.Stationary, InitialState = 2, HitPoints = 10, PresentConditionId = 900901
            });
            var validation = rows.Validate(MissionContentRules.Implemented);
            Assert.AreEqual(0, validation.Gaps.Count, string.Join(" | ", validation.Gaps));

            var instance = new MapChannel { MapInfo = new MapInfo(1985, "adv_bootcamp", 783, 4), OwnerCharacterId = 101, InstanceId = 2, ClientList = new List<Client>() };
            var client = new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame };
            client.Player.Id = 101;
            client.Player.MapChannel = instance;
            var mission = new PlayerMission { MissionId = 900100, State = MissionState.Active };
            mission.Objectives[1] = MissionObjectiveState.Incomplete;
            client.Player.Missions[900100] = mission;

            ContentMaterializer.Materialize(instance, validation);
            Assert.AreEqual(0, instance.ContentUsables.Count, "a conditioned placement waits for its owner");
            ContentMaterializer.RefreshPresence(client, validation);
            Assert.AreEqual(0, instance.ContentUsables.Count);

            mission.Objectives[1] = MissionObjectiveState.Completed;
            ContentMaterializer.RefreshPresence(client, validation);
            ContentMaterializer.RefreshPresence(client, validation);
            var spawned = instance.DynamicObjects.Single();
            Assert.AreEqual(900760u, instance.ContentUsables[spawned.EntityId]);

            // Another character standing in someone else's instance never drives its presence.
            var visitor = new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame };
            visitor.Player.Id = 102;
            visitor.Player.MapChannel = instance;
            ContentMaterializer.RefreshPresence(visitor, validation);
            Assert.AreEqual(1, instance.ContentUsables.Count);

            mission.Objectives[1] = MissionObjectiveState.Incomplete;
            ContentMaterializer.RefreshPresence(client, validation);
            Assert.AreEqual(0, instance.ContentUsables.Count);
            Assert.AreEqual(0, instance.DynamicObjects.Count);
            Assert.IsFalse(EntityManager.Instance.DynamicObjects.ContainsKey(spawned.EntityId));
        }

        [TestMethod]
        public void ABombPlantedBeforeTheInstanceWasRebuiltComesBackArmedWithAFreshFuse()
        {
            var rows = new Rows();
            rows.MapSettings.Add(new ContentMapSettingEntry { MapContextId = 1985, Instancing = (byte)MapInstancing.PerCharacter });
            rows.Conditions.Add(new ContentConditionEntry { ConditionId = 900902, Kind = (byte)ContentConditionKind.FactEquals, FactKey = "bootcamp.bomb_planted", Value = 1 });
            rows.Conditions.Add(new ContentConditionEntry { ConditionId = 900903, Kind = (byte)ContentConditionKind.FactEquals, FactKey = "bootcamp.dropship_destroyed", Value = 1, Negate = true });
            rows.Placements.Add(new ContentPlacementEntry
            {
                Id = 900761, MapContextId = 1985, Kind = (byte)ContentPlacementKind.Usable, EntityClassId = 7870, UsableKind = (byte)ContentUsableKind.Bomb,
                Behavior = (byte)ContentPlacementBehavior.Stationary, InitialState = 113, AlternateState = 114, AlternateStateConditionId = 900902,
                FuseMs = 5300, PresentConditionId = 900903
            });
            var validation = rows.Validate(MissionContentRules.Implemented);
            Assert.AreEqual(0, validation.Gaps.Count, string.Join(" | ", validation.Gaps));

            (MapChannel Instance, Client Owner) Enter(int? plantedFact)
            {
                var instance = new MapChannel { MapInfo = new MapInfo(1985, "adv_bootcamp", 783, 4), OwnerCharacterId = 101, InstanceId = 3, ClientList = new List<Client>() };
                var owner = new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame };
                owner.Player.Id = 101;
                owner.Player.MapContextId = 1985;
                owner.Player.MapChannel = instance;
                if (plantedFact is { } value)
                    owner.Player.ContentFacts[(1985, "bootcamp.bomb_planted")] = value;
                ContentMaterializer.Materialize(instance, validation);
                Assert.AreEqual(0, instance.ContentUsables.Count, "an owner-dependent usable waits for its owner");
                ContentMaterializer.RefreshPresence(owner, validation, 10_000);
                return (instance, owner);
            }

            var (fresh, _) = Enter(null);
            var bomb = fresh.DynamicObjects.Single();
            Assert.AreEqual((UseObjectState)113, bomb.StateId);
            Assert.AreEqual(0L, bomb.FuseAt);
            EntityManager.Instance.UnregisterDynamicObject(bomb.EntityId);
            EntityManager.Instance.UnregisterEntity(bomb.EntityId);

            var (rebuilt, _) = Enter(1);
            var armed = rebuilt.DynamicObjects.Single();
            Assert.AreEqual((UseObjectState)114, armed.StateId);
            Assert.AreEqual(15_300L, armed.FuseAt);
            Assert.AreEqual(101u, armed.ArmedByCharacterId);
            EntityManager.Instance.UnregisterDynamicObject(armed.EntityId);
            EntityManager.Instance.UnregisterEntity(armed.EntityId);
        }

        [TestMethod]
        public void ClassSelectedRulesReactToTheChosenClassOnly()
        {
            WithLogger(() =>
            {
                using var connection = new SqliteConnection("Data Source=:memory:");
                connection.Open();
                using (var context = Context(connection))
                {
                    context.Database.EnsureCreated();
                    context.ContentConditionEntries.Add(new ContentConditionEntry { ConditionId = 900930, Kind = (byte)ContentConditionKind.CharacterClassIs, Value = 2 });
                    context.ContentRuleEntries.Add(new ContentRuleEntry { Id = 9040, MapContextId = 1220, Event = (byte)ContentRuleEvent.ClassSelected, ConditionId = 900930 });
                    context.ContentRuleActionEntries.Add(new ContentRuleActionEntry { RuleId = 9040, Sequence = 0, Action = (byte)ContentRuleAction.TutorialNotification, TutorialId = 10000019 });
                    // A class outside 1..15 is withheld.
                    context.ContentConditionEntries.Add(new ContentConditionEntry { ConditionId = 900931, Kind = (byte)ContentConditionKind.CharacterClassIs, Value = 16 });
                    context.SaveChanges();
                }

                var missions = new MissionManager(new Factory(connection));
                missions.LoadMissions();
                var content = new MissionContentManager(new Factory(connection)) { Missions = missions };
                content.Load(() => new BootcampConfig(), new References(), missions.LoadedMissions);
                CollectionAssert.AreEqual(new[] { "content_condition 900931/0/0: unknown character class 16" }, content.Content.Gaps.Select(gap => gap.ToString()).ToArray());

                var client = new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame };
                client.Player.MapContextId = 1220;

                int Fired(uint classId)
                {
                    client.Player.Class = classId;
                    return content.React(client, new ContentEvent(ContentRuleEvent.ClassSelected, 1220)).Count;
                }

                Assert.AreEqual(1, Fired(2));
                Assert.AreEqual(0, Fired(3));
            });
        }

        [TestMethod]
        public void AreaEnteredRulesFireOnceOnEachEntryAndTransfersApplyAfterTheCommit()
        {
            WithLogger(() =>
            {
                using var connection = new SqliteConnection("Data Source=:memory:");
                connection.Open();
                using (var context = Context(connection))
                {
                    context.Database.EnsureCreated();
                    context.ContentAreaEntries.Add(new ContentAreaEntry { Id = 900610, MapContextId = 1985, Shape = (byte)ContentAreaShape.Sphere, PosX = 10, PosY = 0, PosZ = 10, Radius = 3 });
                    context.ContentRuleEntries.Add(new ContentRuleEntry { Id = 9030, MapContextId = 1985, Event = (byte)ContentRuleEvent.AreaEntered, AreaId = 900610 });
                    context.ContentRuleActionEntries.Add(new ContentRuleActionEntry { RuleId = 9030, Sequence = 0, Action = (byte)ContentRuleAction.TutorialNotification, TutorialId = 10000018 });
                    // An exit-pad-shaped rule: its condition becomes true while the player already stands in the area.
                    context.ContentAreaEntries.Add(new ContentAreaEntry { Id = 900611, MapContextId = 1985, Shape = (byte)ContentAreaShape.Sphere, PosX = 50, PosY = 0, PosZ = 50, Radius = 12 });
                    context.ContentConditionEntries.Add(new ContentConditionEntry { ConditionId = 900920, Kind = (byte)ContentConditionKind.FactEquals, FactKey = "test.cleared", Value = 1 });
                    context.ContentRuleEntries.Add(new ContentRuleEntry { Id = 9031, MapContextId = 1985, Event = (byte)ContentRuleEvent.AreaEntered, AreaId = 900611, ConditionId = 900920 });
                    context.ContentRuleActionEntries.Add(new ContentRuleActionEntry { RuleId = 9031, Sequence = 0, Action = (byte)ContentRuleAction.TutorialNotification, TutorialId = 10000019 });
                    context.SaveChanges();
                }

                var missions = new MissionManager(new Factory(connection));
                missions.LoadMissions();
                var content = new MissionContentManager(new Factory(connection)) { Missions = missions };
                content.Load(() => new BootcampConfig(), new References(), missions.LoadedMissions);
                Assert.AreEqual(0, content.Content.Gaps.Count, string.Join(" | ", content.Content.Gaps));

                var map = new MapChannel { MapInfo = new MapInfo(1985, "adv_bootcamp", 783, 4), ClientList = new List<Client>() };
                var client = new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame };
                client.Player.MapChannel = map;
                client.Player.MapContextId = 1985;
                map.ClientList.Add(client);

                int Tutorials()
                {
                    var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(client);
                    var count = 0;
                    while (queue.PopOutgoing() is ProtocolPacket protocol)
                        if (protocol.Message is CallMethodMessage { MethodId: GameOpcode.DisplayPlayerTutorialNotification })
                            count++;
                    return count;
                }

                var path = new[] { (30f, 30f), (20f, 20f), (10f, 11f), (10f, 10f), (11f, 10f), (30f, 30f), (10.5f, 10.5f) };
                var fired = new List<int>();
                foreach (var (x, z) in path)
                {
                    client.Player.Position = new System.Numerics.Vector3(x, 0, z);
                    content.DoWork(map);
                    fired.Add(Tutorials());
                }
                // Enters at the third sample, stays inside, leaves, and a fast crossing back in fires again.
                CollectionAssert.AreEqual(new[] { 0, 0, 1, 0, 0, 0, 1 }, fired);

                // Standing on the pad before the condition holds fires nothing; the first sample after it holds fires once.
                fired.Clear();
                foreach (var (x, z, cleared) in new[] { (50f, 44f, false), (50f, 50f, false), (51f, 50f, true), (50f, 51f, true), (80f, 80f, true), (50f, 50f, true) })
                {
                    if (cleared)
                        client.Player.ContentFacts[(1985, "test.cleared")] = 1;
                    client.Player.Position = new System.Numerics.Vector3(x, 0, z);
                    content.DoWork(map);
                    fired.Add(Tutorials());
                }
                CollectionAssert.AreEqual(new[] { 0, 0, 1, 0, 0, 1 }, fired);

                // A committed transfer and skip flag reach memory and the loading screen only at Apply.
                ContentLocationEntry transferred = null;
                content.Transfer = (_, location) => transferred = location;
                typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(client, new Structures.Char.GameAccountEntry { Id = 10 });
                var reaction = new ContentReaction { Transfer = new ContentLocationEntry { Id = 19852, MapContextId = 1220 }, SkipBootcampGranted = true };
                content.Apply(client, reaction);
                Assert.AreEqual(19852u, transferred.Id);
                Assert.IsTrue(client.AccountEntry.CanSkipBootcamp);
            });
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
