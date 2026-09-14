using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Packets.Mission.Server;
using Rasa.Structures.World;

namespace Rasa.Test
{
    /// <summary>
    /// Objective counters and map indicators travel inside the mission info: the tracker shows a counter from the
    /// moment its objective is revealed ("Boss Eliminated: 0 / 1", A4-21) and after a relog, and the map marks the
    /// objective's indicators (A5-26). Missions are synthetic.
    /// </summary>
    public partial class MissionLogTests
    {
        [TestMethod]
        public void RevealedObjectivesCarryTheirCountersAndIndicators()
        {
            var definition = _missions.LoadedMissions[MissionId];
            definition.Counters[5] = new() { new NpcMissionObjectiveCounterEntry { MissionId = MissionId, ObjectiveId = 5, CounterId = 0, InitialValue = 0, TargetValue = 1 } };
            definition.Indicators[5] = new()
            {
                new NpcMissionObjectiveIndicatorEntry { MissionId = MissionId, ObjectiveId = 5, IndicatorIndex = 0, IndicatorId = 437, PosX = 95.1, PosY = 109.25, PosZ = 150.8, Radius = 0, Show3d = false }
            };

            Accept();
            var objective = Drain().OfType<MissionGainedPacket>().Single().MissionInfo.ObjectivesList.Single();
            var counter = objective.CounterDict.Single();
            Assert.AreEqual((0u, 0, 0, 1), (counter.Key, counter.Value.Count, counter.Value.InitialCount, counter.Value.TargetCount));
            var indicator = objective.IndicatorList.Single();
            Assert.AreEqual((437u, 95.1f, 109.25f, 150.8f, false), (indicator.IndicatorId, indicator.Position.X, indicator.Position.Y, indicator.Position.Z, indicator.Show3DEffect));

            // A saved counter value replaces the initial value; objectives without rows carry neither.
            _client.Player.Missions[MissionId].Counters[(5, 0)] = 1;
            CompleteObjective(_scout, 5);
            var objectives = Drain().OfType<ObjectiveRevealedPacket>().Single().MissionInfo.ObjectivesList.ToDictionary(o => o.ObjectiveId);
            Assert.AreEqual(1, objectives[5].CounterDict[0].Count);
            Assert.AreEqual(1, objectives[5].IndicatorList.Count);
            Assert.AreEqual(0, objectives[4].CounterDict.Count + objectives[4].IndicatorList.Count);
        }
    }
}
