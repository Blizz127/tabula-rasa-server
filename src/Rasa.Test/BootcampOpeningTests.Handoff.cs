using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Packets.Mission.Server;
using Rasa.Repositories.Char.CharacterMission;
using Rasa.Structures;
using Rasa.Structures.Char;

namespace Rasa.Test
{
    public partial class BootcampOpeningTests
    {
        [TestMethod]
        public void SeededGearingHandoffRequiresHartmannThenPaysDeSimoneOnceAndUnlocksCaptureTheFlag()
        {
            const uint gearing = 1992;
            const uint captureTheFlag = 1994;
            const uint targetDummy = 198653;
            _instance = new MapChannel
            {
                MapInfo = new MapInfo(Camp, "adv_bootcamp", MapVersion, 4),
                OwnerCharacterId = CharacterId, InstanceId = 7, ClientList = new List<Client>()
            };
            _client = new Client(_factory, new ClientPacketHandler()) { State = ClientState.Ingame };
            _client.Player = new Manifestation
            {
                Id = CharacterId, Level = 2, Experience = 1250,
                MapContextId = Camp, MapChannel = _instance
            };
            _client.Player.Credits[CurencyType.Credits] = 100;
            _client.Player.Credits[CurencyType.Prestige] = 0;
            _client.Player.Inventory.PersonalInventory.AddRange(new ulong[250]);
            _instance.ClientList.Add(_client);
            var hartmann = PlaceHandoffNpc(198656);
            var deSimone = PlaceHandoffNpc(198657);

            // Start at the final combat lesson. Definitions, transitions, conversation
            // packages and rewards come from the migrated world, not test replacements.
            var mission = new PlayerMission { MissionId = gearing, State = MissionState.Active };
            foreach (var objective in new uint[] { 4, 1, 2, 5, 6, 3, 9 })
                mission.Objectives[objective] = MissionObjectiveState.Completed;
            mission.Objectives[8] = MissionObjectiveState.Incomplete;
            _client.Player.Missions[gearing] = mission;
            using (var context = CharContext(_charConnection))
            {
                var character = context.CharacterEntries.Single(row => row.Id == CharacterId);
                character.Experience = 1250;
                character.Credit = 100;
                new CharacterMissionRepository(context).Add(
                    new CharacterMissionEntry(CharacterId, gearing, (uint)MissionState.Active, 0),
                    mission.Objectives.Select(entry => new CharacterMissionObjectiveEntry
                    {
                        CharacterId = CharacterId, MissionId = gearing,
                        ObjectiveId = entry.Key, Status = (uint)entry.Value
                    }));
                context.SaveChanges();
            }

            _client.Player.Position = deSimone.Position;
            _missions.AssignNpcMission(_client, deSimone.EntityId, captureTheFlag);
            _missions.CompleteNpcMission(_client, deSimone.EntityId, gearing, null);
            Assert.IsFalse(_client.Player.Missions.ContainsKey(captureTheFlag));
            Assert.AreEqual(MissionState.Active, mission.State);

            // Exercise the actual seeded hit binding. This test uses a destroying
            // Lightning hit; it does not establish that original retail required a kill.
            _content.OnContentUsableHit(_client, targetDummy, 1, true);
            Assert.AreEqual(MissionObjectiveState.Incomplete, mission.Objectives[8]);
            Assert.IsFalse(mission.Objectives.ContainsKey(7));
            _content.OnContentUsableHit(_client, targetDummy, 194, true);
            Assert.AreEqual(MissionObjectiveState.Completed, mission.Objectives[8]);
            Assert.AreEqual(MissionObjectiveState.Incomplete, mission.Objectives[7]);
            Assert.IsFalse(mission.IsCompleteable(_missions.LoadedMissions[gearing]));
            Drain(_client);

            // Reload the mission rows as login does: Hartmann's final topic must
            // survive interruption between Lightning and the conversation.
            ReloadHandoffMissions();
            mission = _client.Player.Missions[gearing];
            var topics = new Dictionary<ConversationType, object>();
            _missions.AddMissionConversation(_client, hartmann, topics);
            var topic = ((List<CompleteableObjectives>)topics[ConversationType.ObjectiveComplete])
                .Single(row => row.MissionId == gearing);
            Assert.AreEqual((7, 1), (topic.ObjectiveId, topic.PlayerFlagId));

            _client.Player.Position = deSimone.Position;
            _missions.CompleteNpcObjective(_client, deSimone.EntityId, gearing, 7, 1);
            Assert.AreEqual(MissionObjectiveState.Incomplete, mission.Objectives[7],
                "The receiver cannot stand in for Hartmann's original conversation package 2563.");
            _client.Player.Position = hartmann.Position;
            _missions.CompleteNpcObjective(_client, hartmann.EntityId, gearing, 7, 1);
            Assert.AreEqual(MissionObjectiveState.Completed, mission.Objectives[7]);
            Assert.IsTrue(mission.IsCompleteable(_missions.LoadedMissions[gearing]));
            var completedTopic = Drain(_client);
            Assert.AreEqual(1, completedTopic.OfType<ObjectiveCompletedPacket>()
                .Count(packet => packet.MissionId == gearing && packet.ObjectiveId == 7));
            Assert.IsTrue(completedTopic.OfType<MissionCompleteablePacket>()
                .Any(packet => packet.MissionId == gearing && packet.IsCompleteable));

            // Being the final objective NPC does not make Hartmann the receiver.
            _missions.CompleteNpcMission(_client, hartmann.EntityId, gearing, null);
            Assert.AreEqual(MissionState.Active, mission.State);
            Assert.AreEqual(1250u, _client.Player.Experience);
            Assert.AreEqual(100, _client.Player.Credits[CurencyType.Credits]);
            ReloadHandoffMissions();
            topics.Clear();
            _missions.AddMissionConversation(_client, deSimone, topics);
            Assert.IsTrue(((Dictionary<uint, RewardInfo>)topics[ConversationType.MissionComplete]).ContainsKey(gearing));
            Assert.IsFalse(topics.TryGetValue(ConversationType.MissionDispense, out var pendingOffers) &&
                ((Dictionary<uint, MissionInfo>)pendingOffers).ContainsKey(captureTheFlag),
                "Capture the Flag requires rewarded Gearing Up, not merely completed objectives.");

            _client.Player.Position = deSimone.Position;
            _missions.CompleteNpcMission(_client, deSimone.EntityId, gearing, null);
            Assert.AreEqual(MissionState.Completed, _client.Player.Missions[gearing].State);
            Assert.AreEqual(2500u, _client.Player.Experience);
            Assert.AreEqual(300, _client.Player.Credits[CurencyType.Credits]);
            var paid = Drain(_client);
            Assert.AreEqual(1, paid.OfType<MissionCompletedPacket>().Count(packet => packet.MissionId == gearing));
            Assert.AreEqual(1, paid.OfType<MissionRewardedPacket>().Count(packet => packet.MissionId == gearing));
            using (var context = CharContext(_charConnection))
            {
                var character = context.CharacterEntries.Single(row => row.Id == CharacterId);
                Assert.AreEqual(2500u, character.Experience);
                Assert.AreEqual(300, character.Credit);
                Assert.AreEqual((uint)MissionState.Completed,
                    context.CharacterMissionEntries.Single(row => row.MissionId == gearing).MissionState);
            }

            ReloadHandoffMissions();
            _missions.CompleteNpcMission(_client, deSimone.EntityId, gearing, null);
            Assert.AreEqual(2500u, _client.Player.Experience);
            Assert.AreEqual(300, _client.Player.Credits[CurencyType.Credits]);
            Assert.IsFalse(Drain(_client).OfType<MissionRewardedPacket>().Any());
            topics.Clear();
            _missions.AddMissionConversation(_client, deSimone, topics);
            Assert.IsTrue(((Dictionary<uint, MissionInfo>)topics[ConversationType.MissionDispense]).ContainsKey(captureTheFlag));
            _missions.AssignNpcMission(_client, deSimone.EntityId, captureTheFlag);
            Assert.AreEqual(MissionState.Active, _client.Player.Missions[captureTheFlag].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, _client.Player.Missions[captureTheFlag].Objectives[4]);
            // The footage cuts over the offer UI; eligibility is tested without
            // requiring an invented automatic conversation or automatic acceptance.
        }

        private Creature PlaceHandoffNpc(uint placementId)
        {
            var placement = _content.Content.Catalog.Placements[placementId];
            using var context = WorldContext(_worldConnection);
            var creature = new Creature(context.CreatureEntries.Single(row => row.Id == placement.CreatureId))
            {
                DbId = placement.CreatureId, ContentPlacementId = placementId,
                MapContextId = Camp, MapChannel = _instance,
                Position = new Vector3((float)placement.PosX, (float)placement.PosY, (float)placement.PosZ),
                Npc = new Npc { NpcPackageId = placement.NpcPackageId }
            };
            CellManager.Instance.AddToWorld(_instance, creature);
            _npcs.Add(creature);
            return creature;
        }

        private void ReloadHandoffMissions()
        {
            using var context = CharContext(_charConnection);
            _client.Player.Missions = _missions.LoadPlayerMissions(
                new CharacterMissionRepository(context), AccountId, 1, CharacterId);
        }
    }
}
