using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Packets.Mission.Server;
using Rasa.Repositories.Char.CharacterMission;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.World;

namespace Rasa.Test
{
    /// <summary>
    /// Objective timers (build plan S5, wall-clock mode OD-5) and the failure/retry flow: a timer starts when its
    /// objective is revealed, the client receives whole seconds that never reach 0 while the objective is open,
    /// expiry fails the objective and then the mission, and only a prerequisite naming the failed mission offers it
    /// again. Missions are synthetic.
    /// </summary>
    public partial class MissionLogTests
    {
        private const uint RetryMissionId = 7002;
        private long _nowMs;

        private void UseTimerClock()
        {
            _nowMs = 1_700_000_000_000;
            _missions.NowMs = () => _nowMs;
        }

        private void TimeObjective(uint missionId, uint objectiveId, uint seconds, ObjectiveTimerExpiry expiry) =>
            _missions.LoadedMissions[missionId].Timers[objectiveId] = new NpcMissionObjectiveTimerEntry
            {
                MissionId = missionId, ObjectiveId = objectiveId, LimitSeconds = seconds, OnExpire = (byte)expiry
            };

        // The 2005 shape: offered by the giver once the timed mission failed, and again after its own failure.
        private Mission AddRetryMission()
        {
            var retry = new Mission(new NpcMissionEntry { Id = RetryMissionId, GiverId = GiverDbId, ReciverId = ReceiverDbId, Level = 1, GroupType = 1, CategoryId = 10000044, Comment = "retry fixture" });
            retry.Objectives[1] = new MissionObjectiveDefinition { ObjectiveId = 1, Ordinal = 1, IsRequired = true, RevealedOnAccept = true };
            retry.ObjectiveConversations.Add(new MissionObjectiveConversation { ObjectiveId = 1, NpcPackageId = ScoutPackage, PlayerFlagId = 1 });
            retry.Prerequisites.Add(new NpcMissionPrerequisiteEntry { MissionId = RetryMissionId, OrGroup = 1, RequiredMissionId = MissionId, RequiredState = (byte)MissionState.Failded });
            retry.Prerequisites.Add(new NpcMissionPrerequisiteEntry { MissionId = RetryMissionId, OrGroup = 1, RequiredMissionId = RetryMissionId, RequiredState = (byte)MissionState.NotAssigned });
            retry.Prerequisites.Add(new NpcMissionPrerequisiteEntry { MissionId = RetryMissionId, OrGroup = 2, RequiredMissionId = MissionId, RequiredState = (byte)MissionState.Failded });
            retry.Prerequisites.Add(new NpcMissionPrerequisiteEntry { MissionId = RetryMissionId, OrGroup = 2, RequiredMissionId = RetryMissionId, RequiredState = (byte)MissionState.Failded });
            MissionManager.BuildRewardInfo(retry);
            retry.RefreshDispenseObjectives();
            _missions.LoadedMissions[RetryMissionId] = retry;
            return retry;
        }

        private List<uint> AvailableAtGiver() =>
            _missions.TryGetConversationStatus(_client, _giver, out var status, out var data) && status == ConversationStatus.Available ? data : new List<uint>();

        private CharacterMissionObjectiveEntry SavedObjective(uint missionId, uint objectiveId)
        {
            using var context = WeaponReloadPersistenceTests.Context(_connection);
            return context.CharacterMissionObjectiveEntries.Single(o => o.CharacterId == CharacterId && o.MissionId == missionId && o.ObjectiveId == objectiveId);
        }

        [TestMethod]
        public void TimersStartOnRevealSendWholeSecondsThatNeverReachZeroAndStopOnCompletion()
        {
            UseTimerClock();
            TimeObjective(MissionId, 5, 126, ObjectiveTimerExpiry.FailObjective);
            TimeObjective(MissionId, 4, 600, ObjectiveTimerExpiry.FailObjective);

            Accept();
            var gained = Drain().OfType<MissionGainedPacket>().Single();
            Assert.AreEqual(126u, gained.MissionInfo.ObjectivesList.Single().TimeRemaining);
            var saved = SavedObjective(MissionId, 5);
            Assert.AreEqual((126_000L, _nowMs, false), (saved.TimerRemainingMs, saved.TimerAnchorMs, saved.TimerDisarmed));

            // 0.5 s and 1 ms before the deadline: still one second, never the client's "expired" 0.
            foreach (var beforeDeadline in new[] { 500L, 1L })
            {
                _nowMs = 1_700_000_000_000 + 126_000 - beforeDeadline;
                _missions.SendMissionStatusInfo(_client);
                Assert.AreEqual(1u, Drain().OfType<MissionStatusInfoPacket>().Single().MissionStatusDict[MissionId].ObjectivesList.Single().TimeRemaining);
            }

            _nowMs = 1_700_000_000_000 + 10_250;
            _missions.SendMissionStatusInfo(_client);
            Assert.AreEqual(116u, Drain().OfType<MissionStatusInfoPacket>().Single().MissionStatusDict[MissionId].ObjectivesList.Single().TimeRemaining);

            // Completed in time: the timer is cleared and the revealed objective's own timer starts now.
            CompleteObjective(_scout, 5);
            var revealed = Drain().OfType<ObjectiveRevealedPacket>().Single();
            var objectives = revealed.MissionInfo.ObjectivesList.ToDictionary(o => o.ObjectiveId);
            Assert.IsNull(objectives[5].TimeRemaining);
            Assert.AreEqual(600u, objectives[4].TimeRemaining);
            Assert.IsNull(SavedObjective(MissionId, 5).TimerAnchorMs);
            Assert.IsNull(SavedObjective(MissionId, 5).TimerRemainingMs);
            Assert.AreEqual((600_000L, _nowMs), (SavedObjective(MissionId, 4).TimerRemainingMs.Value, SavedObjective(MissionId, 4).TimerAnchorMs.Value));
            Assert.IsFalse(_client.Player.Missions[MissionId].Timers.ContainsKey(5));

            // Long past objective 5's old deadline nothing fails.
            _nowMs += 300_000;
            _missions.ExpireObjectiveTimers(_map);
            Assert.IsFalse(Drain().OfType<ObjectiveFailedPacket>().Any());
        }

        [TestMethod]
        public void ExpiryFailsTheObjectiveThenTheMissionAndOnlyTheRetryIsOfferedAgain()
        {
            UseTimerClock();
            TimeObjective(MissionId, 5, 126, ObjectiveTimerExpiry.FailObjectiveAndMission);
            AddRetryMission();
            TimeObjective(RetryMissionId, 1, 600, ObjectiveTimerExpiry.FailObjectiveAndMission);
            CollectionAssert.AreEqual(new[] { MissionId }, AvailableAtGiver());

            Accept();
            Drain();
            CollectionAssert.AreEqual(new uint[0], AvailableAtGiver());

            // Out of time but before the tick: completion is refused and the expiry step decides.
            _nowMs += 126_000;
            _now += 126;
            CompleteObjective(_scout, 5);
            Assert.IsFalse(Drain().OfType<ObjectiveCompletedPacket>().Any());

            _missions.ExpireObjectiveTimers(_map);
            var packets = Drain();
            var objectiveFailed = packets.FindIndex(p => p is ObjectiveFailedPacket);
            var missionFailed = packets.FindIndex(p => p is MissionFailedPacket);
            Assert.IsTrue(objectiveFailed >= 0 && objectiveFailed < missionFailed, $"{objectiveFailed} {missionFailed}");
            Assert.AreEqual((MissionId, 5u), (((ObjectiveFailedPacket)packets[objectiveFailed]).MissionId, ((ObjectiveFailedPacket)packets[objectiveFailed]).ObjectiveId));
            Assert.AreEqual(MissionId, ((MissionFailedPacket)packets[missionFailed]).MissionId);

            var (mission, objectives, _) = Saved();
            Assert.AreEqual((uint)MissionState.Failded, mission.MissionState);
            Assert.AreEqual(_now, mission.ChangeTime);
            Assert.AreEqual((uint)MissionObjectiveState.Failed, objectives.Single().Status);
            Assert.IsNull(objectives.Single().TimerAnchorMs);
            Assert.AreEqual(MissionState.Failded, _client.Player.Missions[MissionId].State);

            // Once only, and a failed mission leaves the client's log for good.
            _missions.ExpireObjectiveTimers(_map);
            Assert.IsFalse(Drain().Any());
            _missions.SendMissionStatusInfo(_client);
            Assert.IsFalse(Drain().OfType<MissionStatusInfoPacket>().Single().MissionStatusDict.ContainsKey(MissionId));

            // The failed mission is not offered again, its retry is; accepting the timed mission itself is refused.
            CollectionAssert.AreEqual(new[] { RetryMissionId }, AvailableAtGiver());
            Accept();
            Assert.IsFalse(Drain().OfType<MissionGainedPacket>().Any());

            Accept(missionId: RetryMissionId);
            Assert.AreEqual(600u, Drain().OfType<MissionGainedPacket>().Single().MissionInfo.ObjectivesList.Single().TimeRemaining);
            CollectionAssert.AreEqual(new uint[0], AvailableAtGiver());

            // The retry fails too, and its self-retry group offers it once more; acceptance replaces the failed row.
            _nowMs += 600_000;
            _missions.ExpireObjectiveTimers(_map);
            Assert.AreEqual(1, Drain().OfType<MissionFailedPacket>().Count());
            CollectionAssert.AreEqual(new[] { RetryMissionId }, AvailableAtGiver());

            _nowMs += 5_000;
            Accept(missionId: RetryMissionId);
            Assert.AreEqual(1, Drain().OfType<MissionGainedPacket>().Count());
            using var context = WeaponReloadPersistenceTests.Context(_connection);
            var retryRow = context.CharacterMissionEntries.Single(m => m.CharacterId == CharacterId && m.MissionId == RetryMissionId);
            Assert.AreEqual((uint)MissionState.Active, retryRow.MissionState);
            var retryObjective = context.CharacterMissionObjectiveEntries.Single(o => o.CharacterId == CharacterId && o.MissionId == RetryMissionId);
            Assert.AreEqual(((uint)MissionObjectiveState.Incomplete, 600_000L, _nowMs), (retryObjective.Status, retryObjective.TimerRemainingMs.Value, retryObjective.TimerAnchorMs.Value));
            Assert.AreEqual(MissionState.Active, _client.Player.Missions[RetryMissionId].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, _client.Player.Missions[RetryMissionId].Objectives[1]);
        }

        [TestMethod]
        public void WallClockTimersRunOutWhileOfflineAndFailSilentlyBeforeTheLogIsSent()
        {
            UseTimerClock();
            TimeObjective(MissionId, 5, 126, ObjectiveTimerExpiry.FailObjective);
            Accept();
            Drain();
            var original = _client.Player.Missions;

            // Offline for three minutes.
            _nowMs += 180_000;
            using (var context = WeaponReloadPersistenceTests.Context(_connection))
            {
                var reloaded = new MissionManager(_factory, () => _now) { NowMs = () => _nowMs };
                reloaded.LoadedMissions[MissionId] = _missions.LoadedMissions[MissionId];
                _client.Player.Missions = reloaded.LoadPlayerMissions(new CharacterMissionRepository(context), AccountId, Slot, CharacterId);
                Assert.AreEqual(1_700_000_000_000L, _client.Player.Missions[MissionId].Timers[5].AnchorMs);

                reloaded.SendMissionStatusInfo(_client);
            }

            var packets = Drain();
            Assert.IsFalse(packets.OfType<ObjectiveFailedPacket>().Any() || packets.OfType<MissionFailedPacket>().Any());
            var objective = packets.OfType<MissionStatusInfoPacket>().Single().MissionStatusDict[MissionId].ObjectivesList.Single();
            Assert.AreEqual((uint)MissionObjectiveState.Failed, objective.ObjectiveStatus);
            Assert.IsNull(objective.TimeRemaining);
            Assert.AreEqual((uint)MissionState.Active, Saved().Mission.MissionState, "on_expire 1 fails the objective only");
            Assert.AreEqual((uint)MissionObjectiveState.Failed, Saved().Objectives.Single().Status);
            _client.Player.Missions = original;
        }

        [TestMethod]
        public void ADisarmedTimerKeepsCountingForTheClientButNeverFails()
        {
            UseTimerClock();
            TimeObjective(MissionId, 5, 126, ObjectiveTimerExpiry.FailObjectiveAndMission);
            Accept();
            Drain();
            _client.Player.Missions[MissionId].Timers[5].Disarmed = true;

            _nowMs += 200_000;
            _missions.ExpireObjectiveTimers(_map);
            Assert.IsFalse(Drain().Any());
            _missions.SendMissionStatusInfo(_client);
            Assert.AreEqual(1u, Drain().OfType<MissionStatusInfoPacket>().Single().MissionStatusDict[MissionId].ObjectivesList.Single().TimeRemaining);

            // A disarmed objective still completes (the bomb detonating after the countdown's end).
            CompleteObjective(_scout, 5);
            Assert.AreEqual(1, Drain().OfType<ObjectiveCompletedPacket>().Count());
        }
    }
}
