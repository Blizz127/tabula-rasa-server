using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Packets.MapChannel.Client;
using Rasa.Structures;
using Rasa.Structures.Content;
using Rasa.Structures.World;

namespace Rasa.Test
{
    public partial class MissionLogTests
    {
        [DataTestMethod]
        [DataRow(1, 2U, 8U, 11U, 1393U, 1392U)]
        [DataRow(2, 3U, 4U, 10U, 1392U, 1393U)]
        public void EthicalChoiceRevealsOnlyItsRouteAndGatesTheMatchingReport(
            int choiceIndex, uint answer, uint escort, uint final, uint report, uint otherReport)
        {
            var apirka = Npc(43, 112, new Vector3(101, 10, 100));
            var quillas = Npc(114, 1646, new Vector3(100, 10, 101));
            _map.MapCellInfo.Cells[7].CreatureList.Add(apirka);
            _map.MapCellInfo.Cells[7].CreatureList.Add(quillas);
            var definition = new Mission(new NpcMissionEntry
            {
                Id = 1390, GiverId = 43, ReciverId = 43, Level = 1, GroupType = 1, CategoryId = 10000001,
                Comment = "Conscientious Objector"
            });
            foreach (var (id, ordinal, required, revealed) in new[]
            {
                (1u,1u,true,true), (2u,2u,false,false), (3u,2u,false,false),
                (4u,3u,false,false), (8u,3u,false,false), (10u,4u,false,false), (11u,4u,false,false)
            })
                definition.Objectives[id] = new MissionObjectiveDefinition
                { ObjectiveId = id, Ordinal = ordinal, IsRequired = required, RevealedOnAccept = revealed };
            foreach (var type in new[] { 3u, 4u, 5u })
                definition.ObjectiveConversations.Add(new MissionObjectiveConversation
                { ObjectiveId = 1, NpcPackageId = 1646, PlayerFlagId = 1, ConvoType = type });
            foreach (var id in new[] { 2u, 3u })
                definition.ObjectiveConversations.Add(new MissionObjectiveConversation
                { ObjectiveId = id, NpcPackageId = 1646, PlayerFlagId = 1, ConvoType = 1 });
            foreach (var id in new[] { 10u, 11u })
                definition.ObjectiveConversations.Add(new MissionObjectiveConversation
                { ObjectiveId = id, NpcPackageId = 112, PlayerFlagId = 1, ConvoType = 1 });
            foreach (var id in new[] { 4u, 8u })
                definition.Bindings.Add(new NpcMissionObjectiveBindingEntry
                { MissionId = 1390, ObjectiveId = id, Kind = (byte)ObjectiveBindingKind.AreaEntered });
            definition.Transitions[1] = new List<uint> { 2, 3 };
            definition.Transitions[2] = new List<uint> { 8 };
            definition.Transitions[3] = new List<uint> { 4 };
            definition.Transitions[4] = new List<uint> { 10 };
            definition.Transitions[8] = new List<uint> { 11 };
            definition.RefreshDispenseObjectives();
            _missions.LoadedMissions[1390] = definition;

            Accept(apirka, 1390);
            var progress = _client.Player.Missions[1390];
            var topics = new Dictionary<ConversationType, object>();
            _missions.AddMissionConversation(_client, quillas, topics);
            Assert.IsTrue(topics.ContainsKey(ConversationType.ObjectiveChoice));
            Assert.IsFalse(topics.ContainsKey(ConversationType.ObjectiveComplete));

            // A plain completion or an absent choice cannot bypass the selected path.
            _missions.CompleteNpcObjective(_client, quillas.EntityId, 1390, 1, 1);
            _npcs.PerformNPCChoice(_client, new PerformNPCChoicePacket
            { EntityId = quillas.EntityId, MissionId = 1390, ObjectiveId = 1, PlayerFlagId = 1, ChoiceIdx = 3 });
            Assert.AreEqual(MissionObjectiveState.Incomplete, progress.Objectives[1]);

            _npcs.PerformNPCChoice(_client, new PerformNPCChoicePacket
            { EntityId = quillas.EntityId, MissionId = 1390, ObjectiveId = 1, PlayerFlagId = 1, ChoiceIdx = choiceIndex });
            Assert.AreEqual(MissionObjectiveState.Completed, progress.Objectives[1]);
            Assert.AreEqual(MissionObjectiveState.Incomplete, progress.Objectives[answer]);
            Assert.IsFalse(progress.Objectives.ContainsKey(answer == 2 ? 3u : 2u));
            _missions.ReconcilePlayerMissions(_client);
            Assert.IsFalse(progress.Objectives.ContainsKey(answer == 2 ? 3u : 2u));

            _missions.CompleteNpcObjective(_client, quillas.EntityId, 1390, answer, 1);
            Assert.AreEqual(MissionObjectiveState.Incomplete, progress.Objectives[escort]);
            Assert.IsFalse(progress.IsCompleteable(definition));
            _missions.CompleteBoundObjective(_client, 1390, escort, ObjectiveBindingKind.AreaEntered);
            Assert.AreEqual(MissionObjectiveState.Incomplete, progress.Objectives[final]);
            _missions.CompleteNpcObjective(_client, apirka.EntityId, 1390, final, 1);
            Assert.IsTrue(progress.IsCompleteable(definition));
            _missions.CompleteNpcMission(_client, apirka.EntityId, 1390, null);
            Assert.AreEqual(MissionState.Completed, progress.State);
            Assert.IsTrue(MissionBranchRules.PartTwoAvailable(_client.Player, report));
            Assert.IsFalse(MissionBranchRules.PartTwoAvailable(_client.Player, otherReport));

            foreach (var reportId in new[] { 1392u, 1393u })
            {
                var followup = new Mission(new NpcMissionEntry
                { Id = reportId, GiverId = 43, ReciverId = 100, Level = 1, GroupType = 1, CategoryId = 10000001 });
                followup.Objectives[1] = new MissionObjectiveDefinition
                { ObjectiveId = 1, Ordinal = 1, IsRequired = true, RevealedOnAccept = true };
                followup.ObjectiveConversations.Add(new MissionObjectiveConversation
                { ObjectiveId = 1, NpcPackageId = 116, PlayerFlagId = 1, ConvoType = 1 });
                followup.Prerequisites.Add(new NpcMissionPrerequisiteEntry
                { MissionId = reportId, OrGroup = 0, RequiredMissionId = 1390, RequiredState = 4 });
                followup.RefreshDispenseObjectives();
                _missions.LoadedMissions[reportId] = followup;
            }
            topics.Clear();
            _missions.AddMissionConversation(_client, apirka, topics);
            var offered = (Dictionary<uint, MissionInfo>)topics[ConversationType.MissionDispense];
            Assert.IsTrue(offered.ContainsKey(report));
            Assert.IsFalse(offered.ContainsKey(otherReport));
        }
    }
}
