using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.World;

namespace Rasa.Test
{
    public partial class MissionLogTests
    {
        [DataTestMethod]
        [DataRow(1861u, Race.Forean)]
        [DataRow(1851u, Race.Brann)]
        [DataRow(1899u, Race.Thrax)]
        public void HybridUnlockCommitsWithTurnInAndRetriesAfterFailure(uint missionId, Race race)
        {
            // Synthetic completable objective: this verifies the payout path, not the mission's content.
            var definition = new Mission(new NpcMissionEntry
                { Id = missionId, GiverId = GiverDbId, ReciverId = ReceiverDbId, Level = 3, GroupType = 1, CategoryId = 10000044 });
            definition.Objectives[1] = new MissionObjectiveDefinition
                { ObjectiveId = 1, Ordinal = 1, IsRequired = true, RevealedOnAccept = true };
            definition.ObjectiveConversations.Add(new MissionObjectiveConversation
                { ObjectiveId = 1, NpcPackageId = 208, PlayerFlagId = 1 });
            definition.RefreshDispenseObjectives();
            _missions.LoadedMissions[missionId] = definition;
            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(_client, new GameAccountEntry { Id = AccountId });
            var progress = new PlayerMission { MissionId = missionId, State = MissionState.Active };
            progress.Objectives[1] = MissionObjectiveState.Completed;
            _client.Player.Missions[missionId] = progress;
            using (var context = WeaponReloadPersistenceTests.Context(_connection))
            {
                context.CharacterMissionEntries.Add(new CharacterMissionEntry(CharacterId, missionId, (uint)MissionState.Active));
                context.SaveChanges();
            }

            _factory.FailNextComplete = true;
            _missions.CompleteNpcMission(_client, _receiver.EntityId, missionId, null);
            using (var context = WeaponReloadPersistenceTests.Context(_connection))
            {
                Assert.AreEqual(0, context.AccountRaceUnlockEntries.Count());
                Assert.AreEqual((uint)MissionState.Active, context.CharacterMissionEntries.Single().MissionState);
            }
            _missions.CompleteNpcMission(_client, _receiver.EntityId, missionId, null);
            _missions.CompleteNpcMission(_client, _receiver.EntityId, missionId, null);
            using var reloaded = WeaponReloadPersistenceTests.Context(_connection);
            Assert.AreEqual((AccountId, (byte)race), (reloaded.AccountRaceUnlockEntries.Single().AccountId, reloaded.AccountRaceUnlockEntries.Single().RaceId));
            Assert.AreEqual((uint)MissionState.Completed, reloaded.CharacterMissionEntries.Single().MissionState);
        }
    }
}
