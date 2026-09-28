using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;

namespace Rasa.Test
{
    [TestClass]
    public class WithdrawnMissionsTests
    {
        // Live 2026-09-28: character 9's saved mission 767 (Mighty Miasma, withdrawn per the official Deployment 11
        // live notes; WildernessMunsonWithdrawalRows) logged at Error on every login, indistinguishable from an
        // actual missing-definition defect. This documents which ids are known-withdrawn rather than broken.
        [TestMethod]
        public void MightyMiasmaIsTheOneKnownWithdrawnMission()
        {
            Assert.IsTrue(WithdrawnMissions.IsWithdrawn(767));
            Assert.IsFalse(WithdrawnMissions.IsWithdrawn(766));
            Assert.IsFalse(WithdrawnMissions.IsWithdrawn(768));
            Assert.IsFalse(WithdrawnMissions.IsWithdrawn(0));
        }
    }
}
