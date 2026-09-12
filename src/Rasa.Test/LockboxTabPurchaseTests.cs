using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Memory;
using Rasa.Packets.Inventory.Client;
using Rasa.Packets.Inventory.Server;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Protocol;
using Rasa.Repositories.Char.CharacterLockbox;
using Rasa.Structures.Char;

namespace Rasa.Test
{
    public partial class InventorySessionTests
    {
        private Client TabClient(int wallet, int tabs)
        {
            var client = CreditClient(wallet, 37);
            client.Player.LockboxTabs = tabs;
            using var context = Context();
            context.CharacterLockboxEntries.Find(10u).PurashedTabs = tabs;
            context.SaveChanges();
            return client;
        }

        [DataTestMethod]
        [DataRow(2, 100000)]
        [DataRow(3, 1000000)]
        [DataRow(4, 10000000)]
        [DataRow(5, 100000000)]
        public void LockboxTabPurchaseDeductsOriginalPriceOnceAndPersistsAccountUnlock(int tab, int price)
        {
            var client = TabClient(price, tab - 1);
            _inventory.PurchaseLockboxTab(client, new PurchaseLockboxTabPacket { TabId = tab });
            Assert.AreEqual(0, client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(tab, client.Player.LockboxTabs);
            Assert.AreEqual(37, client.Player.LockboxCredits);
            var packets = Drain(client);
            Assert.AreEqual(2, packets.Count);
            Assert.AreEqual(0, packets.OfType<UpdateCreditsPacket>().Single().Amount);
            var permission = packets.OfType<LockboxTabPermissionsPacket>().Single();
            using (var stream = new MemoryStream(Serialize(permission)))
            using (var reader = new PythonReader(new BinaryReader(stream)))
            {
                Assert.AreEqual(1, reader.ReadTuple());
                Assert.AreEqual(5, reader.ReadDictionary());
                for (var i = 1; i <= 5; i++)
                {
                    Assert.AreEqual(i, reader.ReadInt());
                    Assert.AreEqual(i <= tab, reader.ReadBool());
                }
                Assert.AreEqual(stream.Length, stream.Position);
            }
            _inventory.PurchaseLockboxTab(client, new PurchaseLockboxTabPacket { TabId = tab });
            Assert.AreEqual(0, Drain(client).Count);
            using var reopened = Context();
            Assert.AreEqual(0, reopened.CharacterEntries.Find(101u).Credit);
            var accountBank = new CharacterLockboxRepository(reopened).Get(10);
            Assert.AreEqual(tab, accountBank.PurashedTabs);
            Assert.AreEqual(37, accountBank.Credits);
            Assert.AreEqual(0, reopened.CharacterEntries.Find(102u).Credit);
        }

        [DataTestMethod]
        [DataRow("insufficient")]
        [DataRow("dead")]
        [DataRow("skip")]
        [DataRow("owned")]
        [DataRow("invalid")]
        [DataRow("negative")]
        [DataRow("stale_wallet")]
        [DataRow("stale_tabs")]
        [DataRow("wrong_account")]
        [DataRow("missing_bank")]
        [DataRow("late_write")]
        public void InvalidOrFailedLockboxTabPurchasePublishesNoPaymentOrUnlock(string failure)
        {
            var wallet = failure == "insufficient" ? 99999 : 200000;
            var client = TabClient(wallet, 1);
            var tab = failure == "skip" ? 3 : failure == "owned" ? 1 : failure == "invalid" ? 6 : failure == "negative" ? -1 : 2;
            if (failure == "dead") client.Player.State = CharacterState.Dead;
            using (var context = Context())
            {
                if (failure == "stale_wallet") context.Database.ExecuteSqlRaw("UPDATE character SET credit = 199999 WHERE id = 101");
                if (failure == "stale_tabs") context.Database.ExecuteSqlRaw("UPDATE character_lockbox SET purashed_tabs = 2 WHERE account_id = 10");
                if (failure == "wrong_account")
                {
                    context.CharacterLockboxEntries.Add(new CharacterLockboxEntry(20, 37, 1));
                    context.SaveChanges();
                    typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(client, new GameAccountEntry { Id = 20 });
                }
                if (failure == "missing_bank") context.Database.ExecuteSqlRaw("DELETE FROM character_lockbox WHERE account_id = 10");
                if (failure == "late_write") context.Database.ExecuteSqlRaw("CREATE TRIGGER fail_tab BEFORE UPDATE ON character_lockbox BEGIN SELECT RAISE(ABORT, 'fixture tab write'); END");
            }
            _inventory.PurchaseLockboxTab(client, new PurchaseLockboxTabPacket { TabId = tab });
            Assert.AreEqual(wallet, client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(1, client.Player.LockboxTabs);
            Assert.AreEqual(0, Drain(client).Count);
            using var reopened = Context();
            Assert.AreEqual(failure == "stale_wallet" ? 199999 : wallet, reopened.CharacterEntries.Find(101u).Credit);
            if (failure != "missing_bank")
            {
                var bank = reopened.CharacterLockboxEntries.Find(10u);
                Assert.AreEqual(failure == "stale_tabs" ? 2 : 1, bank.PurashedTabs);
                Assert.AreEqual(37, bank.Credits);
            }
            if (failure == "wrong_account") Assert.AreEqual(1, reopened.CharacterLockboxEntries.Find(20u).PurashedTabs);
        }

        [TestMethod]
        public void TwoCharactersCannotPayTwiceForTheSameAccountTab()
        {
            TabClient(200000, 1);
            using (var context = Context())
            {
                context.CharacterEntries.Find(102u).Credit = 200000;
                context.SaveChanges();
            }
            using var work = Context();
            var repository = new CharacterLockboxRepository(work);
            Assert.IsTrue(repository.TryPurchaseTab(10, 101, 200000, 1, 2, 100000));
            Assert.IsFalse(repository.TryPurchaseTab(10, 102, 200000, 1, 2, 100000));
            using var reopened = Context();
            Assert.AreEqual(100000, reopened.CharacterEntries.Find(101u).Credit);
            Assert.AreEqual(200000, reopened.CharacterEntries.Find(102u).Credit);
            Assert.AreEqual(2, reopened.CharacterLockboxEntries.Find(10u).PurashedTabs);
        }
    }

    [TestClass]
    public class LockboxTabPacketTests
    {
        [TestMethod]
        public void OriginalSingleIntegerTupleDecodesRequestedTab()
        {
            using var stream = new MemoryStream(new byte[] { 0x81, 0x12 });
            using var reader = new PythonReader(new BinaryReader(stream));
            var packet = new PurchaseLockboxTabPacket();
            packet.Read(reader);
            Assert.AreEqual(2, packet.TabId);
        }

        [DataTestMethod]
        [DataRow(0x10, 0x12)]
        [DataRow(0x80, 0x12)]
        [DataRow(0x82, 0x12)]
        [DataRow(0x81, 0x00)]
        public void MalformedTabPurchaseUsesHandledMessageException(int tuple, int field)
        {
            using var stream = new MemoryStream(new byte[] { (byte)tuple, (byte)field });
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.ThrowsException<InvalidClientMessageException>(() => new PurchaseLockboxTabPacket().Read(reader));
        }
    }
}
