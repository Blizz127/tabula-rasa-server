using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Memory;
using Rasa.Packets.Inventory.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Protocol;
using Rasa.Repositories.Char.CharacterLockbox;
using Rasa.Structures.Char;

namespace Rasa.Test
{
    public partial class InventorySessionTests
    {
        private Client CreditClient(int wallet = 1000, int bank = 1000)
        {
            using (var context = Context())
            {
                context.CharacterEntries.Find(101u).Credit = wallet;
                context.CharacterLockboxEntries.Add(new CharacterLockboxEntry(10, bank, 1));
                context.SaveChanges();
            }
            var client = Login();
            client.Player.Credits[CurencyType.Credits] = wallet;
            client.Player.LockboxCredits = bank;
            Drain(client);
            return client;
        }

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(-1)]
        [DataRow(127)]
        [DataRow(-127)]
        [DataRow(128)]
        [DataRow(-128)]
        [DataRow(255)]
        [DataRow(-255)]
        [DataRow(256)]
        [DataRow(-256)]
        [DataRow(499)]
        [DataRow(-499)]
        [DataRow(500)]
        [DataRow(-500)]
        [DataRow(1000)]
        [DataRow(-1000)]
        public void LockboxCreditTransfersConserveFundsAndPublishAbsoluteBalances(int amount)
        {
            var client = CreditClient();
            _inventory.TransferCreditToLockbox(client, amount);
            Assert.AreEqual(1000 - amount, client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(1000 + amount, client.Player.LockboxCredits);
            var packets = Drain(client);
            Assert.AreEqual(2, packets.Count); // No invented system message or loot notification.
            Assert.AreEqual(1000 - amount, packets.OfType<UpdateCreditsPacket>().Single().Amount);
            Assert.AreEqual(1000 + amount, packets.OfType<LockboxFundsPacket>().Single().Amount);
            using var reloaded = Context();
            Assert.AreEqual(1000 - amount, reloaded.CharacterEntries.Find(101u).Credit);
            Assert.AreEqual(1000 + amount, reloaded.CharacterLockboxEntries.Find(10u).Credits);
            Assert.AreEqual(0, reloaded.CharacterEntries.Find(102u).Credit);
        }

        [DataTestMethod]
        [DataRow(0, 1000, 1000)]
        [DataRow(1001, 1000, 1000)]
        [DataRow(-1001, 1000, 1000)]
        [DataRow(int.MinValue, 0, int.MaxValue)]
        [DataRow(1, 1, int.MaxValue)]
        [DataRow(-1, int.MaxValue, 1)]
        [DataRow(1, -1, 1000)]
        [DataRow(-1, 1000, -1)]
        public void InvalidCreditTransfersLeaveMemoryStorageAndPacketsUnchanged(int amount, int wallet, int bank)
        {
            var client = CreditClient(wallet, bank);
            _inventory.TransferCreditToLockbox(client, amount);
            Assert.AreEqual(wallet, client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(bank, client.Player.LockboxCredits);
            Assert.AreEqual(0, Drain(client).Count);
            using var reloaded = Context();
            Assert.AreEqual(wallet, reloaded.CharacterEntries.Find(101u).Credit);
            Assert.AreEqual(bank, reloaded.CharacterLockboxEntries.Find(10u).Credits);
        }

        [DataTestMethod]
        [DataRow("wallet")]
        [DataRow("bank")]
        [DataRow("other_character")]
        [DataRow("missing_bank")]
        [DataRow("late_write")]
        public void StaleOrFailedCreditTransferRollsBackBothBalances(string failure)
        {
            var client = CreditClient();
            using (var context = Context())
            {
                if (failure == "wallet") context.Database.ExecuteSqlRaw("UPDATE character SET credit = 999 WHERE id = 101");
                if (failure == "bank") context.Database.ExecuteSqlRaw("UPDATE character_lockbox SET credits = 999 WHERE account_id = 10");
                if (failure == "other_character") client.Player.Id = 102;
                if (failure == "missing_bank") context.Database.ExecuteSqlRaw("DELETE FROM character_lockbox WHERE account_id = 10");
                if (failure == "late_write") context.Database.ExecuteSqlRaw("CREATE TRIGGER fail_bank BEFORE UPDATE ON character_lockbox BEGIN SELECT RAISE(ABORT, 'fixture bank write'); END");
            }
            _inventory.TransferCreditToLockbox(client, -1);
            Assert.AreEqual(1000, client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(1000, client.Player.LockboxCredits);
            Assert.AreEqual(0, Drain(client).Count);
            using var reopened = Context();
            Assert.AreEqual(failure == "wallet" ? 999 : 1000, reopened.CharacterEntries.Find(101u).Credit);
            Assert.AreEqual(0, reopened.CharacterEntries.Find(102u).Credit);
            if (failure != "missing_bank")
                Assert.AreEqual(failure == "bank" ? 999 : 1000, reopened.CharacterLockboxEntries.Find(10u).Credits);
        }

        [TestMethod]
        public void MaximumRepresentableCreditTransferCanMoveAllFundsInBothDirections()
        {
            var client = CreditClient(int.MaxValue, 0);
            _inventory.TransferCreditToLockbox(client, int.MaxValue);
            Assert.AreEqual(0, client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(int.MaxValue, client.Player.LockboxCredits);
            _inventory.TransferCreditToLockbox(client, -int.MaxValue);
            using var reloaded = Context();
            Assert.AreEqual(int.MaxValue, reloaded.CharacterEntries.Find(101u).Credit);
            Assert.AreEqual(0, reloaded.CharacterLockboxEntries.Find(10u).Credits);
            Assert.AreEqual(4, Drain(client).Count);
        }

        [TestMethod]
        public void AccountOwnershipAndSharedBankComparePreventCrossAccountOrStaleWrites()
        {
            CreditClient();
            using var context = Context();
            context.CharacterLockboxEntries.Add(new CharacterLockboxEntry(20, 1000, 1));
            context.SaveChanges();
            var repository = new CharacterLockboxRepository(context);
            Assert.IsFalse(repository.TryTransferCredits(20, 101, 1000, 1000, 1));
            Assert.IsTrue(repository.TryTransferCredits(10, 101, 1000, 1000, 1));
            Assert.IsFalse(repository.TryTransferCredits(10, 102, 0, 1000, -1));
            Assert.IsTrue(repository.TryTransferCredits(10, 102, 0, 1001, -1));
            using var reopened = Context();
            Assert.AreEqual(1000, reopened.CharacterLockboxEntries.Find(20u).Credits);
            Assert.AreEqual(999, reopened.CharacterEntries.Find(101u).Credit);
            Assert.AreEqual(1, reopened.CharacterEntries.Find(102u).Credit);
            Assert.AreEqual(1000, reopened.CharacterLockboxEntries.Find(10u).Credits);
        }
    }

    [TestClass]
    public class LockboxCreditPacketTests
    {
        [TestMethod]
        public void LongEncodedAmountIsAcceptedWithinStorageWidth()
        {
            using var stream = new MemoryStream(new byte[] { 0x81, 0x2F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF });
            using var reader = new PythonReader(new BinaryReader(stream));
            var packet = new TransferCreditToLockboxPacket();
            packet.Read(reader);
            Assert.AreEqual(-1, packet.Ammount);
        }

        [TestMethod]
        public void InvalidLongTagUsesHandledMessageException()
        {
            using var stream = new MemoryStream(new byte[] { 0x81, 0x21 });
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.ThrowsException<InvalidClientMessageException>(() => new TransferCreditToLockboxPacket().Read(reader));
        }

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(-1)]
        [DataRow(127)]
        [DataRow(-128)]
        [DataRow(255)]
        [DataRow(-255)]
        [DataRow(256)]
        [DataRow(-256)]
        [DataRow(499)]
        [DataRow(-499)]
        [DataRow(int.MinValue)]
        [DataRow(int.MaxValue)]
        public void TransferTuplePreservesSignedIntegerAmount(int amount)
        {
            // Literal compact widths exercise the negative-byte decoding that
            // originally prompted the emulator's 500-credit workaround.
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
            {
                writer.Write((byte)0x81);
                if (amount >= sbyte.MinValue && amount <= sbyte.MaxValue) { writer.Write((byte)0x1D); writer.Write((sbyte)amount); }
                else if (amount >= short.MinValue && amount <= short.MaxValue) { writer.Write((byte)0x1E); writer.Write((short)amount); }
                else { writer.Write((byte)0x1F); writer.Write(amount); }
            }
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            var packet = new TransferCreditToLockboxPacket();
            packet.Read(reader);
            Assert.AreEqual(amount, packet.Ammount);
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [DataTestMethod]
        [DataRow("not_tuple")]
        [DataRow("empty")]
        [DataRow("two_fields")]
        [DataRow("wrong_type")]
        [DataRow("wide_positive")]
        [DataRow("wide_negative")]
        public void MalformedCreditTransferUsesHandledMessageException(string malformed)
        {
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, System.Text.Encoding.UTF8, true)))
            {
                if (malformed == "not_tuple") writer.WriteInt(1);
                else
                {
                    writer.WriteTuple(malformed == "empty" ? 0 : malformed == "two_fields" ? 2 : 1);
                    if (malformed == "wrong_type") writer.WriteString("1");
                    else if (malformed == "wide_positive") writer.WriteLong((long)int.MaxValue + 1);
                    else if (malformed == "wide_negative") writer.WriteLong((long)int.MinValue - 1);
                    else writer.WriteInt(1);
                }
            }
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.ThrowsException<InvalidClientMessageException>(() => new TransferCreditToLockboxPacket().Read(reader));
        }
    }
}
