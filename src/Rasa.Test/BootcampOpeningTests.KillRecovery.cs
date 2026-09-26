using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Packets.Mission.Server;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.Content;

namespace Rasa.Test
{
    public partial class BootcampOpeningTests
    {
        private const uint CaptureTheFlag = 1994;

        [TestMethod]
        public void AllyFinishingTheFlagBossCreditsThePrivateMapOwnerBeforeOneShotDespawn()
        {
            PrepareFlagBossProgress(0);
            _instance = new MapChannel
            {
                MapInfo = new MapInfo(Camp, "adv_bootcamp", MapVersion, 4),
                OwnerCharacterId = CharacterId,
                ClientList = new System.Collections.Generic.List<Client> { _client }
            };
            using var world = WorldContext(_worldConnection);
            var boss = new Creature(world.CreatureEntries.Single(row => row.Id == 198506))
            {
                DbId = 198506,
                ContentPlacementId = 198659,
                MapContextId = Camp,
                State = CharacterState.Normal
            };

            // A Forean creature stands in for an AFS ally delivering the final blow;
            // the killer is a creature, so player rewards must not be inferred.
            var ally = new Creature { Faction = Factions.AFS, Cells = new uint[0, 0] };
            CreatureManager.Instance.HandleCreatureKill(_instance, boss, ally);

            Assert.AreEqual(CharacterState.Dead, boss.State);
            Assert.IsTrue(_instance.DefeatedContentPlacements.Contains(198659));
            Assert.AreEqual(MissionObjectiveState.Completed, _client.Player.Missions[CaptureTheFlag].Objectives[1]);
            Assert.AreEqual(1, _client.Player.Missions[CaptureTheFlag].Counters[(1, 0)]);
            Assert.AreEqual(0u, _client.Player.Experience);
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void SeededFlagBossCounterAndObjectiveCommitTogether(bool failObjectiveCommit)
        {
            PrepareFlagBossProgress(0);
            var attemptedCompletion = false;
            _factory.FailComplete = context =>
            {
                var completesBoss = context.ChangeTracker.Entries<CharacterMissionObjectiveEntry>().Any(entry =>
                    entry.Entity.MissionId == CaptureTheFlag && entry.Entity.ObjectiveId == 1 &&
                    entry.Entity.Status == (uint)MissionObjectiveState.Completed);
                attemptedCompletion |= completesBoss;
                return completesBoss && failObjectiveCommit;
            };
            using var world = WorldContext(_worldConnection);
            var boss = new Creature(world.CreatureEntries.Single(row => row.Id == 198506))
            {
                DbId = 198506, ContentPlacementId = 198659, State = CharacterState.Dead
            };
            _missions.OnCreatureKilled(_client, boss);
            Assert.IsTrue(attemptedCompletion, "The regression must reach the objective save, not fail earlier in the fixture.");

            using var persisted = _factory.CreateChar();
            var counter = persisted.CharacterMissions.GetCounters(CharacterId).Single(row => row.MissionId == CaptureTheFlag);
            var objective = persisted.CharacterMissions.GetObjectives(CharacterId).Single(row => row.MissionId == CaptureTheFlag && row.ObjectiveId == 1);
            var progress = _client.Player.Missions[CaptureTheFlag];
            var packets = Drain(_client);
            if (failObjectiveCommit)
            {
                Assert.AreEqual(0, counter.Value, "A failed objective commit must not leave the dead boss persisted as 1/1 while incomplete.");
                Assert.AreEqual((uint)MissionObjectiveState.Incomplete, objective.Status);
                Assert.AreEqual(0, progress.Counters[(1, 0)]);
                Assert.IsFalse(progress.Objectives.ContainsKey(3));
                Assert.IsFalse(packets.Any(packet => packet is UpdateObjectiveCounterPacket || packet is ObjectiveCompletedPacket || packet is ObjectiveRevealedPacket));
                Assert.IsFalse(new ContentState(_client.Player).Evaluate(_content.Content.Catalog.Conditions[198902]),
                    "Youngblood must not appear before a successful boss completion.");
            }
            else
            {
                Assert.AreEqual(1, counter.Value);
                Assert.AreEqual((uint)MissionObjectiveState.Completed, objective.Status);
                Assert.AreEqual(MissionObjectiveState.Incomplete, progress.Objectives[3]);
                var counterIndex = packets.FindIndex(packet => packet is UpdateObjectiveCounterPacket);
                var completeIndex = packets.FindIndex(packet => packet is ObjectiveCompletedPacket);
                var revealIndex = packets.FindIndex(packet => packet is ObjectiveRevealedPacket);
                Assert.IsTrue(counterIndex >= 0 && counterIndex < completeIndex && completeIndex < revealIndex,
                    "Keep the existing counter, objective-completed, objective-revealed packet order.");
                Assert.IsTrue(new ContentState(_client.Player).Evaluate(_content.Content.Catalog.Conditions[198902]));
                Assert.AreEqual(MissionState.Active, progress.State, "The Youngblood conversation and payout remain pending.");
                Assert.AreEqual(0u, _client.Player.Experience, "Boss objective completion must not pay the later mission reward.");
            }
        }

        [TestMethod]
        public void SeededFlagBossSatisfiedCounterRecoversOnReconnectExactlyOnce()
        {
            // The old split commit could leave this state after the boss died: counter1/1,
            // objective still incomplete. Load it through the real admission repository.
            PrepareFlagBossProgress(1);
            _missions.ReconcilePlayerMissions(_client);
            var progress = _client.Player.Missions[CaptureTheFlag];
            Assert.AreEqual(MissionObjectiveState.Completed, progress.Objectives[1]);
            Assert.AreEqual(MissionObjectiveState.Incomplete, progress.Objectives[3]);
            Assert.AreEqual(1, progress.Counters[(1, 0)], "Recovery must not count a second kill.");
            Assert.AreEqual(MissionState.Active, progress.State);
            Assert.AreEqual(0u, _client.Player.Experience);
            Assert.IsTrue(new ContentState(_client.Player).Evaluate(_content.Content.Catalog.Conditions[198902]));
            Assert.AreEqual(1, Drain(_client).OfType<ObjectiveCompletedPacket>().Count());

            ReloadFlagBossProgress();
            _missions.ReconcilePlayerMissions(_client);
            Assert.AreEqual(MissionObjectiveState.Completed, _client.Player.Missions[CaptureTheFlag].Objectives[1]);
            Assert.IsFalse(Drain(_client).Any(packet => packet is ObjectiveCompletedPacket || packet is ObjectiveRevealedPacket),
                "A later reconnect must not repeat completion or rewards.");
        }

        private void PrepareFlagBossProgress(int counter)
        {
            var definition = _missions.LoadedMissions[CaptureTheFlag];
            Assert.AreEqual(198659u, definition.Bindings.Single(binding => binding.ObjectiveId == 1).PlacementId);
            Assert.AreEqual(1, definition.Counters[1].Single().TargetValue);
            using (var unit = _factory.CreateChar())
            {
                unit.CharacterMissions.Add(new CharacterMissionEntry(CharacterId, CaptureTheFlag, (uint)MissionState.Active), new[]
                {
                    new CharacterMissionObjectiveEntry(CharacterId, CaptureTheFlag, 4, (uint)MissionObjectiveState.Completed),
                    new CharacterMissionObjectiveEntry(CharacterId, CaptureTheFlag, 2, (uint)MissionObjectiveState.Completed),
                    new CharacterMissionObjectiveEntry(CharacterId, CaptureTheFlag, 1, (uint)MissionObjectiveState.Incomplete)
                });
                unit.CharacterMissions.UpsertCounter(CharacterId, CaptureTheFlag, 1, 0, counter);
                unit.Complete();
            }
            _client = new Client(_factory, new ClientPacketHandler()) { State = ClientState.Ingame };
            _client.Player = new Manifestation { Id = CharacterId, Level = 2, MapContextId = Camp };
            ReloadFlagBossProgress();
        }

        private void ReloadFlagBossProgress()
        {
            using var unit = _factory.CreateChar();
            _client.Player.Missions = _missions.LoadPlayerMissions(unit.CharacterMissions, AccountId, 1, CharacterId);
        }
    }
}
