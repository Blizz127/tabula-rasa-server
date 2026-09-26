using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Packets.Mission.Server;
using Rasa.Structures;

namespace Rasa.Test
{
    public partial class MissionLogTests
    {
        [TestMethod]
        public void EarlyAdmissionSnapshotDoesNotReconcileDefinitionsOrExpireSavedTimers()
        {
            UseTimerClock();
            TimeObjective(MissionId, 5, 10, ObjectiveTimerExpiry.FailObjectiveAndMission);
            Accept();
            Drain();
            _nowMs += 20_000;
            _missions.LoadedMissions[MissionId].Objectives[99] = new MissionObjectiveDefinition
            {
                ObjectiveId = 99, Ordinal = 99, IsRequired = false, RevealedOnAccept = true
            };

            var snapshot = _missions.CreateMissionStatusSnapshot(_client);
            Assert.AreEqual(MissionState.Active, snapshot.MissionStatusDict[MissionId].MissionState);
            Assert.AreEqual(1, snapshot.MissionStatusDict[MissionId].ObjectivesList.Count);
            Assert.AreEqual(MissionState.Active, _client.Player.Missions[MissionId].State);
            Assert.IsFalse(_client.Player.Missions[MissionId].Objectives.ContainsKey(99));
            Assert.AreEqual((uint)MissionState.Active, Saved().Mission.MissionState);
            Assert.AreEqual(1, Saved().Objectives.Count);
            Assert.AreEqual(0, Drain().Count, "Snapshot construction cannot send lifecycle or content events.");

            // The normal post-controller lifecycle still performs both operations.
            _missions.SendMissionStatusInfo(_client);
            Assert.AreEqual(MissionState.Failded, _client.Player.Missions[MissionId].State);
            Assert.IsTrue(_client.Player.Missions[MissionId].Objectives.ContainsKey(99));
            Assert.AreEqual((uint)MissionState.Failded, Saved().Mission.MissionState);
            var late = Drain().OfType<MissionStatusInfoPacket>().Single();
            Assert.IsFalse(late.MissionStatusDict.ContainsKey(MissionId));
            _missions.CreateMissionStatusSnapshot(_client);
            Assert.AreEqual(0, Drain().Count);
        }
    }
}
