using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;

namespace Rasa.Test
{
    [TestClass]
    public class WeaponAttackDataTests
    {
        [TestMethod]
        public void EveryOriginalPrimaryAttackRowMatchesTheRawLiteralFingerprint()
        {
            // External canonical CSV: original raw LOAD_CONST/STORE_SUBSCR values,
            // ordered action, argument, then all eight actionArguments fields.
            var canonical = new StringBuilder();
            var count = 0;
            foreach (var action in new[] { ActionId.WeaponAttack, ActionId.WeaponMelee })
                for (uint argument = 0; argument <= 1000; argument++)
                    if (WeaponAttackData.TryGet(action, argument, out var timing))
                    {
                        count++;
                        canonical.AppendFormat(CultureInfo.InvariantCulture,
                            "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}\n",
                            (int)action, argument, timing.WindupMilliseconds,
                            timing.WindupAnimationFamilyId?.ToString(CultureInfo.InvariantCulture) ?? "None",
                            timing.RecoveryMilliseconds,
                            timing.RecoveryAnimationFamilyId?.ToString(CultureInfo.InvariantCulture) ?? "None",
                            timing.MaxRange, timing.ReuseMilliseconds,
                            timing.Preload ? 1 : 0, timing.StartReuseTimerOnPerform ? 1 : 0);
                    }
            Assert.AreEqual(198, WeaponAttackData.Count);
            Assert.AreEqual(198, count);
            using var hash = SHA256.Create();
            var digest = Convert.ToHexString(hash.ComputeHash(Encoding.UTF8.GetBytes(canonical.ToString()))).ToLowerInvariant();
            Assert.AreEqual("d7c012fc12003fb92e5ebdf8e9a4962eac4c495f1a295d58e81d7e61476f882e", digest);
        }

        [TestMethod]
        public void OriginalWindupRecoveryAndReuseRemainDistinct()
        {
            var attack = WeaponAttackData.Get(ActionId.WeaponAttack, 3);
            Assert.AreEqual(0, attack.WindupMilliseconds);
            Assert.AreEqual(660, attack.RecoveryMilliseconds);
            Assert.AreEqual(250, attack.ReuseMilliseconds);
            var melee = WeaponAttackData.Get(ActionId.WeaponMelee, 3);
            Assert.AreEqual(200, melee.WindupMilliseconds);
            Assert.AreEqual(133, melee.RecoveryMilliseconds);
            Assert.AreEqual(250, melee.ReuseMilliseconds);
            Assert.AreEqual(4, melee.MaxRange);
        }

        [TestMethod]
        public void ExceptionalOriginalRowsArePreservedWithoutNormalizingThem()
        {
            var noPredictedReuse = WeaponAttackData.Get(ActionId.WeaponAttack, 66);
            Assert.IsFalse(noPredictedReuse.StartReuseTimerOnPerform);
            Assert.AreEqual(1500, noPredictedReuse.RecoveryMilliseconds);
            var unusual = WeaponAttackData.Get(ActionId.WeaponMelee, 32);
            Assert.AreEqual(4, unusual.WindupMilliseconds);
            Assert.AreEqual(5, unusual.RecoveryMilliseconds);
            Assert.AreEqual(2, unusual.ReuseMilliseconds);
            Assert.IsTrue(unusual.StartReuseTimerOnPerform);
        }

        [TestMethod]
        public void SpecializedActionsAndAbsentArgumentsReceiveNoFallback()
        {
            Assert.IsFalse(WeaponAttackData.TryGet(ActionId.WeaponAttack, 2, out _));
            Assert.IsFalse(WeaponAttackData.TryGet(ActionId.WeaponMelee, 15, out _));
            Assert.IsFalse(WeaponAttackData.TryGet((ActionId)140, 1, out _));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => WeaponAttackData.Get(ActionId.WeaponDraw, 1));
        }
    }
}
