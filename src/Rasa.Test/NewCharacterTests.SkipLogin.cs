using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Packets.Game.Client;
using Rasa.Packets.Game.Server;
using Rasa.Repositories.Char.Character;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Test
{
    public partial class NewCharacterTests
    {
        private void StopAfterCommittedSelection(bool skip)
        {
            // The creation fixture deliberately has no world-session repositories.
            // This simulates failing world initialization after the selection commit,
            // without MapLoaded or the map-exit callback that used to save NumLogins.
            var error = Assert.ThrowsException<NotSupportedException>(() =>
                _manager.RequestSwitchToCharacterInSlot(_client,
                    new RequestSwitchToCharacterInSlotPacket { SlotNum = 1, SkipBootcamp = skip }));
            Assert.AreEqual("get_Clans", error.Message);
        }

        [DataTestMethod]
        [DataRow(false, true, true, 0u, false)]
        [DataRow(true, false, true, 0u, true)]
        [DataRow(true, true, true, 1u, false)]
        [DataRow(true, true, false, 0u, false)]
        public void SkipAdmissionUsesPersistedEntitlementAndCannotRepeatAfterInterruptedLoading(
            bool persistedEntitlement, bool cachedEntitlement, bool requestedSkip, uint priorLogins, bool shouldSkip)
        {
            _manager.RequestCreateCharacterInSlot(_client, Request());
            Drain();
            using (var context = Context())
            {
                context.GameAccountEntries.Single().CanSkipBootcamp = persistedEntitlement;
                context.CharacterEntries.Single().MapContextId = 1985;
                context.CharacterEntries.Single().NumLogins = priorLogins;
                context.SaveChanges();
            }
            _client.AccountEntry.CanSkipBootcamp = cachedEntitlement;
            var destinationCalls = 0;
            _manager.SkipBootcampDestination = (_, _) =>
            {
                destinationCalls++;
                return new ContentLocationEntry { MapContextId = 1220, PosX = 765, PosY = 294, PosZ = 386 };
            };

            StopAfterCommittedSelection(requestedSkip);
            using (var context = Context())
            {
                var saved = new CharacterRepository(context).GetByAccountId(10, 1);
                Assert.AreEqual(priorLogins + 1, saved.NumLogins);
                Assert.IsNotNull(saved.LastLogin);
                Assert.AreEqual(shouldSkip ? 1220u : 1985u, saved.MapContextId);
                Assert.AreEqual(shouldSkip ? 1 : 0, destinationCalls);
                Assert.AreEqual(saved.MapContextId,
                    new CharacterInfoPacket(1, true, "Fixture", saved).GameContextId,
                    "Reconnect must advertise a played context, suppressing the original first-login skip prompt.");
            }

            // Replay after a reconnect/initialization failure cannot regain the first-login choice.
            StopAfterCommittedSelection(true);
            using var reloaded = Context();
            Assert.AreEqual(priorLogins + 2, reloaded.CharacterEntries.Single().NumLogins);
            Assert.AreEqual(shouldSkip ? 1 : 0, destinationCalls);
        }

        [TestMethod]
        public void FailedSkipAdmissionRollsBackPositionSelectionAndLoginCount()
        {
            _manager.RequestCreateCharacterInSlot(_client, Request());
            Drain();
            using (var context = Context())
            {
                context.GameAccountEntries.Single().CanSkipBootcamp = true;
                context.CharacterEntries.Single().MapContextId = 1985;
                context.SaveChanges();
                context.Database.ExecuteSqlRaw("CREATE TRIGGER reject_first_login BEFORE UPDATE ON character WHEN NEW.num_logins != OLD.num_logins BEGIN SELECT RAISE(ABORT, 'login counter failure'); END;");
            }
            var selected = _client.AccountEntry.SelectedSlot;
            _manager.SkipBootcampDestination = (_, _) => new ContentLocationEntry
                { MapContextId = 1220, PosX = 765, PosY = 294, PosZ = 386 };
            Assert.ThrowsException<DbUpdateException>(() => _manager.RequestSwitchToCharacterInSlot(_client,
                new RequestSwitchToCharacterInSlotPacket { SlotNum = 1, SkipBootcamp = true }));
            using var reloaded = Context();
            Assert.AreEqual(1985u, reloaded.CharacterEntries.Single().MapContextId);
            Assert.AreEqual(0u, reloaded.CharacterEntries.Single().NumLogins);
            Assert.AreEqual(selected, reloaded.GameAccountEntries.Single().SelectedSlot);
            Assert.AreEqual(selected, _client.AccountEntry.SelectedSlot);
        }

        [DataTestMethod]
        [DataRow(0u)]
        [DataRow(7u)]
        [DataRow(uint.MaxValue)]
        public void AdmissionAndLaterLogoutSaveCountExactlyOneLogin(uint priorLogins)
        {
            _manager.RequestCreateCharacterInSlot(_client, Request());
            Drain();
            using var context = Context();
            context.CharacterEntries.Single().NumLogins = priorLogins;
            context.SaveChanges();
            var repository = new CharacterRepository(context);
            var detached = repository.GetByAccountId(10, 1);
            repository.UpdateLoginData(detached.Id);
            context.SaveChanges();
            var manifestation = new Manifestation(detached, new Dictionary<EquipmentData, AppearanceData>());
            var expected = priorLogins == uint.MaxValue ? uint.MaxValue : priorLogins + 1;
            Assert.AreEqual(expected, manifestation.NumLogins);
            Assert.AreEqual(expected, context.CharacterEntries.Single().NumLogins);
            // The existing registered-map exit persists, rather than increments, this same count.
            repository.UpdateCharacterLogin(detached.Id, 0, manifestation.NumLogins);
            using var reloaded = Context();
            Assert.AreEqual(expected, reloaded.CharacterEntries.Single().NumLogins);
        }
    }
}
