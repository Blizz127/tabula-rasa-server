using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Packets.Game.Client;
using Rasa.Packets.Game.Server;
using Rasa.Repositories.Char.GameAccount;

namespace Rasa.Test
{
    public partial class NewCharacterTests
    {
        [DataTestMethod]
        [DataRow(Race.Forean)]
        [DataRow(Race.Brann)]
        [DataRow(Race.Thrax)]
        public void HybridCreationRequiresTheAccountsPersistedUnlock(Race race)
        {
            var request = FirstRequest();
            request.RaceId = race;
            SetAppearance(request.AppearanceData, race);
            _manager.RequestCreateCharacterInSlot(_client, request);
            Assert.AreEqual(0, Drain().OfType<CharacterCreateSuccessPacket>().Count());
            using (var context = Context())
            {
                Assert.AreEqual(0, context.CharacterEntries.Count());
                Assert.AreEqual("", context.GameAccountEntries.Single().FamilyName);
                new GameAccountRepository(context).StageRaceUnlock(10, (byte)race);
                context.SaveChanges();
            }

            // The client account object is deliberately stale: the server consults persistence.
            _manager.RequestCreateCharacterInSlot(_client, request);
            Assert.AreEqual(1, Drain().OfType<CharacterCreateSuccessPacket>().Count());
            using var reloaded = Context();
            Assert.AreEqual((byte)race, reloaded.CharacterEntries.Single().Race);
        }

        [TestMethod]
        public void LockedCloneRaceDoesNotSpendCreditsAndUnlockAllowsClone()
        {
            _manager.RequestCreateCharacterInSlot(_client, FirstRequest());
            Drain();
            using (var context = Context())
            {
                context.CharacterEntries.Single().CloneCredits = 1;
                context.SaveChanges();
            }
            var request = new RequestCloneCharacterToSlotPacket
                { CloneSlotNum = 1, SlotNum = 2, CharacterName = "Second", Scale = 1, RaceId = Race.Forean };
            SetAppearance(request.AppearanceData, request.RaceId);
            _manager.RequestCloneCharacterToSlot(_client, request);
            Assert.AreEqual(0, Drain().OfType<CharacterCreateSuccessPacket>().Count());
            using (var context = Context())
            {
                Assert.AreEqual(1, context.CharacterEntries.Count());
                Assert.AreEqual(1u, context.CharacterEntries.Single().CloneCredits);
                new GameAccountRepository(context).StageRaceUnlock(10, (byte)Race.Forean);
                context.SaveChanges();
            }
            _manager.RequestCloneCharacterToSlot(_client, request);
            Assert.AreEqual(1, Drain().OfType<CharacterCreateSuccessPacket>().Count());
            using var reloaded = Context();
            Assert.AreEqual(0u, reloaded.CharacterEntries.Single(c => c.Slot == 1).CloneCredits);
            Assert.AreEqual((byte)Race.Forean, reloaded.CharacterEntries.Single(c => c.Slot == 2).Race);
        }
    }
}
