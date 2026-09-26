using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Structures;

namespace Rasa.Test
{
    public partial class BootcampOpeningTests
    {
        [TestMethod]
        public void WildernessMissionOffersFollowTheHistoricalReceptiveAndApirkaChain()
        {
            var player = new Manifestation { Level = 5, MapContextId = 1220 };
            var receptive = _missions.LoadedMissions[1069];
            var alliances = _missions.LoadedMissions[479];
            var lurking = _missions.LoadedMissions[427];

            Assert.IsFalse(_missions.PrerequisitesSatisfied(player, receptive),
                "Receptive Reception requires Too Close For Comfort.");
            Assert.IsFalse(_missions.PrerequisitesSatisfied(player, alliances),
                "Forming Alliances requires Receptive Reception.");
            Assert.IsFalse(_missions.PrerequisitesSatisfied(player, lurking),
                "Caufield must not offer Lurking In The Shadows after class gear alone.");

            player.Missions[1407] = new PlayerMission { MissionId = 1407, State = MissionState.Completed };
            Assert.IsTrue(_missions.PrerequisitesSatisfied(player, receptive));
            Assert.IsFalse(_missions.PrerequisitesSatisfied(player, alliances));
            Assert.IsFalse(_missions.PrerequisitesSatisfied(player, lurking));

            player.Missions[1069] = new PlayerMission { MissionId = 1069, State = MissionState.Completed };
            Assert.IsTrue(_missions.PrerequisitesSatisfied(player, alliances));
            Assert.IsFalse(_missions.PrerequisitesSatisfied(player, lurking));

            player.Missions[479] = new PlayerMission { MissionId = 479, State = MissionState.Active };
            Assert.IsFalse(_missions.PrerequisitesSatisfied(player, lurking),
                "Collecting hearts does not satisfy the completed mission prerequisite.");
            player.Missions[479].State = MissionState.Completed;
            Assert.IsTrue(_missions.PrerequisitesSatisfied(player, lurking));
        }
    }
}
