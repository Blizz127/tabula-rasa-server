using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test
{
    using Rasa.Data;
    using Rasa.Packets.Mission.Server;
    using Rasa.Structures;

    /// <summary>
    /// Turning a mission in over the radio: missionlog.pyo offers that for a definition whose radio_completeable flag is
    /// set, and the request carries the same (missionId, selectionIdx, rating) tuple the NPC turn-in does - without the
    /// NPC. These tests run against the same harness as the rest of the mission-log lifecycle.
    /// </summary>
    public partial class MissionLogTests
    {
        /// <summary>The fixture mission, but flagged radio completeable the way the seed flags 429 and the W1/W2 ones.</summary>
        private Mission RadioDefinition()
        {
            var definition = Definition();
            definition.MissionConstantData.RadioCompletable = true;
            _missions.LoadedMissions[MissionId] = definition;
            return definition;
        }

        private void RadioComplete(int? selection = null)
            => _missions.CompleteRadioMission(_client, MissionId, selection);

        [TestMethod]
        public void RadioCompletionPaysOutWithoutTheReceiver()
        {
            RadioDefinition();
            Accept();
            CompleteObjective(_scout, 5);
            CompleteObjective(_giver, 4);
            Drain();

            RadioComplete();

            // The mission is completed and paid exactly as the NPC turn-in pays it, with no receiver involved.
            var saved = Saved();
            Assert.AreEqual((uint)MissionState.Completed, saved.Mission.MissionState);
            Assert.AreEqual(290, saved.Character.Credit);
            Assert.AreEqual(1005u, saved.Character.Experience);

            var packets = Drain();
            Assert.AreEqual(1, packets.OfType<MissionCompletedPacket>().Count());
            Assert.AreEqual(1, packets.OfType<MissionRewardedPacket>().Count());
            Assert.AreEqual(0, packets.OfType<MissionGainedPacket>().Count());
        }

        [TestMethod]
        public void AMissionThatIsNotRadioCompleteableCannotBeTurnedInByRadio()
        {
            // No flag: the client would not offer the radio step, so the request is not one it could make.
            Definition();
            Accept();
            CompleteObjective(_scout, 5);
            CompleteObjective(_giver, 4);
            Drain();

            RadioComplete();

            Assert.IsTrue(Saved().Mission == null || Saved().Mission.MissionState == (uint)MissionState.Active);
            Assert.AreEqual(40, Saved().Character.Credit);
            Assert.AreEqual(0, Drain().OfType<MissionCompletedPacket>().Count());
        }

        [TestMethod]
        public void RadioCompletionStillNeedsEveryRequiredObjective()
        {
            RadioDefinition();
            Accept();
            CompleteObjective(_scout, 5);   // objective 4 is still incomplete
            Drain();

            RadioComplete();

            Assert.AreEqual((uint)MissionState.Active, Saved().Mission.MissionState);
            Assert.AreEqual(40, Saved().Character.Credit);
            Assert.AreEqual(0, Drain().OfType<MissionCompletedPacket>().Count());
        }

        [TestMethod]
        public void TheRewardStepCannotPayTwice()
        {
            // RewardRadioMission carries the same tuple as the completion step, so both go through the radio turn-in.
            // The second call must do nothing, because the mission is no longer completeable.
            RadioDefinition();
            Accept();
            CompleteObjective(_scout, 5);
            CompleteObjective(_giver, 4);
            Drain();

            RadioComplete();
            Drain();
            RadioComplete();

            Assert.AreEqual(290, Saved().Character.Credit);
            Assert.AreEqual(1005u, Saved().Character.Experience);
            Assert.AreEqual(0, Drain().OfType<MissionCompletedPacket>().Count());
        }
    }
}
