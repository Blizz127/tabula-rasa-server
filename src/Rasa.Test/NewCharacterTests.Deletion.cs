using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Memory;
using Rasa.Packets.Game.Client;
using Rasa.Packets.Game.Server;
using Rasa.Structures;
using Rasa.Structures.Char;

namespace Rasa.Test
{
    public partial class NewCharacterTests
    {
        [TestMethod]
        public void OversizedDeleteSlotCannotWrapAndDeleteTheFirstCharacter()
        {
            _manager.RequestCreateCharacterInSlot(_client, FirstRequest());
            Drain();
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            writer.WriteTuple(1);
            writer.WriteUInt(257);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            var packet = new RequestDeleteCharacterInSlotPacket();
            Assert.ThrowsException<OverflowException>(() =>
            {
                packet.Read(reader);
                _manager.RequestDeleteCharacterInSlot(_client, packet);
            });
            using var context = Context();
            Assert.AreEqual(1, context.CharacterEntries.Count());
            Assert.AreEqual(1, context.CharacterEntries.Single().Slot);
            Assert.IsTrue(context.CharacterAppearanceEntries.Any());
            Assert.AreEqual(0, Drain().Count);
        }

        [DataTestMethod]
        [DataRow(0)]
        [DataRow(2)]
        public void DeleteRejectsWrongTupleShapeBeforeReadingTheSlot(int tupleSize)
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            writer.WriteTuple(tupleSize);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.ThrowsException<InvalidDataException>(() => new RequestDeleteCharacterInSlotPacket().Read(reader));
        }

        [DataTestMethod]
        [DataRow(ClientState.None)]
        [DataRow(ClientState.Connected)]
        [DataRow(ClientState.LoggedIn)]
        [DataRow(ClientState.Loading)]
        [DataRow(ClientState.Ingame)]
        [DataRow(ClientState.Disconnected)]
        [DataRow(ClientState.Teleporting)]
        public void DeleteOutsideSelectionCannotMutateCharacterOrAppearance(ClientState state)
        {
            _manager.RequestCreateCharacterInSlot(_client, FirstRequest());
            Drain();
            _client.State = state;
            _manager.RequestDeleteCharacterInSlot(_client, new RequestDeleteCharacterInSlotPacket { Slot = 1 });
            using var context = Context();
            Assert.AreEqual(1, context.CharacterEntries.Count());
            Assert.AreEqual(5, context.CharacterAppearanceEntries.Count());
            // A disconnected transport intentionally drops outgoing packets;
            // rejection still must leave every persisted row untouched.
            Assert.AreEqual(state == ClientState.Disconnected ? 0 : 1,
                Drain().OfType<DeleteCharacterFailedPacket>().Count());
        }

        [TestMethod]
        public void ValidSelectionDeleteRemovesOwnedStateAndItemsButKeepsOtherCharacter()
        {
            _manager.RequestCreateCharacterInSlot(_client, FirstRequest());
            Drain();
            _manager.RequestCreateCharacterInSlot(_client, Request(2, "Second"));
            Drain();
            uint deletedId;
            uint survivorId;
            uint[] deletedItemIds;
            using (var context = Context())
            {
                deletedId = context.CharacterEntries.Single(c => c.Slot == 1).Id;
                survivorId = context.CharacterEntries.Single(c => c.Slot == 2).Id;
                deletedItemIds = context.CharacterInventoryEntries.Where(e => e.CharacterId == deletedId)
                    .Select(e => e.ItemId).ToArray();
                context.CharacterLogosEntries.Add(new CharacterLogosEntry(deletedId, 23));
                context.CharacterMissionEntries.Add(new CharacterMissionEntry(deletedId, 1995, 1));
                context.CharacterMissionObjectiveEntries.Add(new CharacterMissionObjectiveEntry(deletedId, 1995, 1, 1));
                context.CharacterMissionObjectiveCounterEntries.Add(new CharacterMissionObjectiveCounterEntry
                    { CharacterId = deletedId, MissionId = 1995, ObjectiveId = 1, CounterId = 0, Value = 2 });
                context.CharacterContentFactEntries.Add(new CharacterContentFactEntry
                    { CharacterId = deletedId, MapContextId = 1220, FactKey = "delete-fixture", Value = 1 });
                context.CharacterOptionEntries.Add(new CharacterOptionEntry(deletedId, 2, "test"));
                context.CharacterTitleEntries.Add(new CharacterTitleEntry(deletedId, 3));
                context.SaveChanges();
                _client.Player = new Manifestation { Id = deletedId };
            }

            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            writer.WriteTuple(1);
            writer.WriteUInt(1);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            var packet = new RequestDeleteCharacterInSlotPacket();
            packet.Read(reader);
            _manager.RequestDeleteCharacterInSlot(_client, packet);

            using var reloaded = Context();
            Assert.AreEqual(1, reloaded.CharacterEntries.Count());
            Assert.AreEqual(survivorId, reloaded.CharacterEntries.Single().Id);
            Assert.AreEqual(0, reloaded.CharacterAppearanceEntries.Count(e => e.CharacterId == deletedId));
            Assert.AreEqual(0, reloaded.CharacterAbilityDrawerEntries.Count(e => e.CharacterId == deletedId));
            Assert.AreEqual(0, reloaded.CharacterInventoryEntries.Count(e => e.CharacterId == deletedId));
            Assert.AreEqual(0, reloaded.CharacterSkillsEntries.Count(e => e.CharacterId == deletedId));
            Assert.AreEqual(0, reloaded.CharacterTeleporterEntries.Count(e => e.CharacterId == deletedId));
            Assert.AreEqual(0, reloaded.CharacterLogosEntries.Count(e => e.CharacterId == deletedId));
            Assert.AreEqual(0, reloaded.CharacterMissionEntries.Count(e => e.CharacterId == deletedId));
            Assert.AreEqual(0, reloaded.CharacterMissionObjectiveEntries.Count(e => e.CharacterId == deletedId));
            Assert.AreEqual(0, reloaded.CharacterMissionObjectiveCounterEntries.Count(e => e.CharacterId == deletedId));
            Assert.AreEqual(0, reloaded.CharacterContentFactEntries.Count(e => e.CharacterId == deletedId));
            Assert.AreEqual(0, reloaded.CharacterOptionEntries.Count(e => e.CharacterId == deletedId));
            Assert.AreEqual(0, reloaded.CharacterTitleEntries.Count(e => e.CharacterId == deletedId));
            Assert.AreEqual(0, reloaded.ItemEntries.Count(e => deletedItemIds.Contains(e.ItemId)));
            Assert.AreEqual(5, reloaded.CharacterInventoryEntries.Count(e => e.CharacterId == survivorId));
            Assert.AreEqual(5, reloaded.ItemEntries.Count());
            Assert.AreEqual(1, reloaded.CharacterLockboxEntries.Count());
            Assert.AreEqual(0u, _client.Player.Id);
            var packets = Drain();
            Assert.IsTrue(packets.OfType<CharacterDeleteSuccessPacket>().Single().HasCharacters);
            var pod = packets.OfType<CharacterInfoPacket>().Single();
            Assert.AreEqual(1u, pod.SlotId);
            Assert.IsNull(pod.CharacterData);
        }

        [TestMethod]
        public void FailedParentDeleteRollsBackChildAndItemDeletes()
        {
            _manager.RequestCreateCharacterInSlot(_client, FirstRequest());
            Drain();
            uint characterId;
            using (var context = Context())
            {
                characterId = context.CharacterEntries.Single().Id;
                context.Database.ExecuteSqlRaw("CREATE TRIGGER reject_character_delete BEFORE DELETE ON character "
                    + "BEGIN SELECT RAISE(ABORT, 'delete fixture failure'); END;");
            }

            _manager.RequestDeleteCharacterInSlot(_client, new RequestDeleteCharacterInSlotPacket { Slot = 1 });

            using var reloaded = Context();
            Assert.AreEqual(1, reloaded.CharacterEntries.Count(e => e.Id == characterId));
            Assert.AreEqual(5, reloaded.CharacterAppearanceEntries.Count(e => e.CharacterId == characterId));
            Assert.AreEqual(2, reloaded.CharacterAbilityDrawerEntries.Count(e => e.CharacterId == characterId));
            Assert.AreEqual(5, reloaded.CharacterInventoryEntries.Count(e => e.CharacterId == characterId));
            Assert.AreEqual(5, reloaded.CharacterSkillsEntries.Count(e => e.CharacterId == characterId));
            Assert.AreEqual(5, reloaded.ItemEntries.Count());
            Assert.AreEqual(1, Drain().OfType<DeleteCharacterFailedPacket>().Count());
        }
    }
}
