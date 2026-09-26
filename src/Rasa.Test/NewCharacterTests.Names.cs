using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Packets.Game.Client;
using Rasa.Packets.Game.Server;
using Rasa.Repositories.Char.Character;
using Rasa.Repositories.Char.GameAccount;
using Rasa.Structures.Char;

namespace Rasa.Test
{
    public partial class NewCharacterTests
    {
        [DataTestMethod]
        [DataRow("Mcdonald", true)]
        [DataRow("Different", false)]
        public void OriginalNormalizedExistingFamilyKeepsItsPersistedIdentity(string requestedFamily, bool accepted)
        {
            var first = FirstRequest();
            first.FamilyName = "McDonald";
            _manager.RequestCreateCharacterInSlot(_client, first);
            Drain();

            var next = Request(2, "Second");
            next.FamilyName = requestedFamily;
            _manager.RequestCreateCharacterInSlot(_client, next);
            var packets = Drain();
            using var context = Context();
            Assert.AreEqual("McDonald", context.GameAccountEntries.Single().FamilyName);
            Assert.AreEqual("McDonald", _client.AccountEntry.FamilyName);
            Assert.AreEqual(accepted ? 2 : 1, context.CharacterEntries.Count());
            Assert.IsTrue(context.CharacterEntries.All(c => c.AccountId == 10));
            if (accepted)
            {
                Assert.AreEqual("McDonald", packets.OfType<CharacterCreateSuccessPacket>().Single().FamilyName);
                Assert.AreEqual("McDonald", packets.OfType<CharacterInfoPacket>().Single().FamilyName);
            }
            else
                Assert.AreEqual(CreateCharacterResult.InvalidCharacterName,
                    packets.OfType<UserCreationFailedPacket>().Single().Result);
        }

        [DataTestMethod]
        [DataRow(false, "First")]
        [DataRow(true, "First")]
        [DataRow(false, "Élodie")]
        [DataRow(true, "Élodie")]
        public void DuplicateFirstNameWithinFamilyDoesNotCreateOrConsumeCloneCredits(bool clone, string name)
        {
            var first = FirstRequest();
            first.CharacterName = name;
            _manager.RequestCreateCharacterInSlot(_client, first);
            Drain();
            using (var context = Context())
            {
                context.CharacterEntries.Single().CloneCredits = 1;
                context.SaveChanges();
            }
            if (clone)
            {
                var packet = new RequestCloneCharacterToSlotPacket
                    { CloneSlotNum = 1, SlotNum = 2, CharacterName = name, Scale = 1, RaceId = Race.Human };
                foreach (var appearance in FirstRequest().AppearanceData)
                    packet.AppearanceData.Add(appearance.Key, appearance.Value);
                _manager.RequestCloneCharacterToSlot(_client, packet);
            }
            else
                _manager.RequestCreateCharacterInSlot(_client, Request(2, name));

            Assert.AreEqual(CreateCharacterResult.NameInUse, Drain().OfType<UserCreationFailedPacket>().Single().Result);
            using var reloaded = Context();
            Assert.AreEqual(1, reloaded.CharacterEntries.Count());
            Assert.AreEqual(1u, reloaded.CharacterEntries.Single().CloneCredits);
        }

        [TestMethod]
        public void AnotherFamilyCanUseTheSameFirstName()
        {
            using (var context = Context())
            {
                context.GameAccountEntries.Add(new GameAccountEntry
                    { Id = 20, Name = "Other", FamilyName = "Other", Email = "other@example.invalid" });
                context.SaveChanges();
                new CharacterRepository(context).Create(new GameAccountRepository(context).Get(20), 1, "First", 1, 1, 0);
            }
            _manager.RequestCreateCharacterInSlot(_client, FirstRequest());
            Assert.AreEqual(1, Drain().OfType<CharacterCreateSuccessPacket>().Count());
            using var reloaded = Context();
            Assert.AreEqual(2, reloaded.CharacterEntries.Count(c => c.Name == "First"));
            var repository = new CharacterRepository(reloaded);
            var own = reloaded.CharacterEntries.Single(c => c.AccountId == 10);
            Assert.IsFalse(repository.IsCharacterNameTaken("First", own.Id, 10));
            Assert.IsTrue(repository.IsCharacterNameTaken("FIRST", 0, 10));
        }

        [DataTestMethod]
        [DataRow("fixture", "Fixture")]
        [DataRow("Élodie", "Élodie")]
        [DataRow("élodie", "Élodie")]
        public void FamilyNameReservationIsCaseInsensitiveOnSqlite(string existing, string requested)
        {
            using (var context = Context())
            {
                context.GameAccountEntries.Add(new GameAccountEntry
                    { Id = 20, Name = "Other", FamilyName = existing, Email = "other@example.invalid" });
                context.SaveChanges();
            }
            var request = FirstRequest();
            request.FamilyName = requested;
            _manager.RequestCreateCharacterInSlot(_client, request);
            Assert.AreEqual(CreateCharacterResult.FamilyNameReserved, Drain().OfType<UserCreationFailedPacket>().Single().Result);
            using var reloaded = Context();
            Assert.AreEqual(0, reloaded.CharacterEntries.Count());
            Assert.AreEqual("", reloaded.GameAccountEntries.Single(a => a.Id == 10).FamilyName);
        }

        [TestMethod]
        public void StaleAccountCacheCannotReplaceAnExistingFamilyDuringCreation()
        {
            _manager.RequestCreateCharacterInSlot(_client, FirstRequest());
            Drain();
            _client.AccountEntry.FamilyName = "";
            _client.AccountEntry.Characters.Clear();
            var request = Request(2, "Second");
            request.FamilyName = "Changed";
            _manager.RequestCreateCharacterInSlot(_client, request);
            Assert.AreEqual(CreateCharacterResult.InvalidCharacterName, Drain().OfType<UserCreationFailedPacket>().Single().Result);
            using var reloaded = Context();
            Assert.AreEqual("Fixture", reloaded.GameAccountEntries.Single().FamilyName);
            Assert.AreEqual(1, reloaded.CharacterEntries.Count());
        }
    }
}
