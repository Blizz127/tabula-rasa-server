using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Memory;
using Rasa.Packets.MapChannel.Server;

namespace Rasa.Test
{
    [TestClass]
    public class WeaponActionDataTests
    {
        [TestMethod]
        public void EntireCatalogMatchesTheOriginalRawLiteralTableFingerprint()
        {
            // SHA-256 of all 135 original rows, statically decoded from actiondata.pyo.
            // Canonical external CSV and raw store offsets are recorded in the evidence doc.
            var canonical = new StringBuilder();
            var count = 0;
            foreach (var action in new[] { ActionId.WeaponStow, ActionId.WeaponDraw, ActionId.WeaponReload })
            {
                for (uint argument = 0; argument <= 100; argument++)
                {
                    if (!WeaponActionData.TryGet(action, argument, out var timing))
                        continue;
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
            }
            Assert.AreEqual(135, WeaponActionData.Count);
            Assert.AreEqual(135, count);
            using var hash = SHA256.Create();
            var digest = Convert.ToHexString(hash.ComputeHash(Encoding.UTF8.GetBytes(canonical.ToString()))).ToLowerInvariant();
            Assert.AreEqual("610c9076aa1ceb2ccc3f19fb83ab402fb797585182cf61c86cc689a0b894bef4", digest);
        }

        [TestMethod]
        public void DrawAndStowUseRecoveryTimeAndPreserveUnusualEntries()
        {
            var draw = WeaponActionData.Get(ActionId.WeaponDraw, 3);
            Assert.AreEqual(0, draw.WindupMilliseconds);
            Assert.IsNull(draw.WindupAnimationFamilyId);
            Assert.AreEqual(900, draw.RecoveryMilliseconds);
            Assert.AreEqual(513, draw.RecoveryAnimationFamilyId);
            Assert.AreEqual(1132, WeaponActionData.Get(ActionId.WeaponStow, 2).RecoveryMilliseconds);
            Assert.AreEqual(2000, WeaponActionData.Get(ActionId.WeaponDraw, 9).ReuseMilliseconds);
            Assert.AreEqual(0, WeaponActionData.Get(ActionId.WeaponDraw, 17).RecoveryMilliseconds);
        }

        [TestMethod]
        public void Reload92RetainsItsDistinctRecoveryInsteadOfBecomingAnOrdinaryReload()
        {
            var ordinary = WeaponActionData.Get(ActionId.WeaponReload, 1);
            Assert.AreEqual(1500, ordinary.WindupMilliseconds);
            Assert.AreEqual(0, ordinary.RecoveryMilliseconds);
            var unusual = WeaponActionData.Get(ActionId.WeaponReload, 92);
            Assert.AreEqual(0, unusual.WindupMilliseconds);
            Assert.AreEqual(3996, unusual.RecoveryMilliseconds);
            Assert.AreEqual(653, unusual.RecoveryAnimationFamilyId);
        }

        [TestMethod]
        public void MissingArgumentsAndUnrelatedActionsDoNotReceiveGuessedTimings()
        {
            Assert.IsFalse(WeaponActionData.TryGet(ActionId.WeaponReload, 7, out _));
            Assert.IsFalse(WeaponActionData.TryGet(ActionId.WeaponDraw, 0, out _));
            Assert.IsFalse(WeaponActionData.TryGet(ActionId.WeaponAttack, 1, out _));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => WeaponActionData.Get(ActionId.WeaponStow, 10));
        }

        [DataTestMethod]
        [DataRow(0U)]
        [DataRow(30U)]
        public void ReloadRecoveryAlwaysCarriesTheNumericAmmoCount(uint ammo)
        {
            var packet = new WeaponReloadRecoveryPacket(14, ammo);
            Assert.AreEqual(GameOpcode.PerformRecovery, packet.Opcode);
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            packet.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(134U, reader.ReadUInt());
            Assert.AreEqual(14U, reader.ReadUInt());
            Assert.AreEqual(ammo, reader.ReadUInt());
            Assert.AreEqual(stream.Length, stream.Position);
        }
    }
}
