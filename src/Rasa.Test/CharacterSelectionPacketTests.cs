using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game.Handlers;
using Rasa.Memory;
using Rasa.Packets;
using Rasa.Packets.Game.Client;

namespace Rasa.Test
{
    [TestClass]
    public class CharacterSelectionPacketTests
    {
        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void OriginalSelectionMessagePreservesSlotAndSkipChoice(bool skip)
        {
            var router = new PacketRouter<ClientPacketHandler, GameOpcode>();
            var packet = (RequestSwitchToCharacterInSlotPacket)Activator.CreateInstance(
                router.GetPacketType(GameOpcode.RequestSwitchToCharacterInSlot));
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            writer.WriteTuple(2);
            writer.WriteInt(16);
            writer.WriteBool(skip);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            packet.Read(reader);
            Assert.AreEqual(16, packet.SlotNum);
            Assert.AreEqual(skip, packet.SkipBootcamp);
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [DataTestMethod]
        [DataRow(0u, 0u)]
        [DataRow(3u, 1985u)]
        public void AnUnplayedCharacterReportsNoGameContextSoTheSkipPromptCanAppear(uint logins, uint expected)
        {
            // characterselectionwindow.OnPlayBtn: the prompt needs GetNumLogins() == 0 and GetLastGameContextId() is None.
            var entry = new Structures.Char.CharacterEntry
            {
                Id = 101, Slot = 1, Name = "First", MapContextId = 1985, NumLogins = logins,
                CharacterAppearance = new System.Collections.Generic.List<Structures.Char.CharacterAppearanceEntry>()
            };
            Assert.AreEqual(expected, new Packets.Game.Server.CharacterInfoPacket(1, true, "Family", entry).GameContextId);
        }

        [TestMethod]
        public void SkippingNeedsTheAccountFlagAndAnUnplayedCharacter()
        {
            var manager = new Managers.CharacterManager(null);
            var client = new Game.Client(null, new ClientPacketHandler());
            typeof(Game.Client).GetProperty(nameof(Game.Client.AccountEntry)).SetValue(client, new Structures.Char.GameAccountEntry { Id = 10, CanSkipBootcamp = false });
            var unplayed = new Structures.Char.CharacterEntry { Id = 101, MapContextId = 1985, NumLogins = 0 };
            Assert.IsNull(manager.SkipBootcampDestination(client, unplayed));

            client.AccountEntry.CanSkipBootcamp = true;
            Assert.IsNull(manager.SkipBootcampDestination(client, new Structures.Char.CharacterEntry { Id = 102, MapContextId = 1985, NumLogins = 1 }));
        }

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(3)]
        public void IncorrectSelectionTupleIsRejected(int count)
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            writer.WriteTuple(count);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.ThrowsException<InvalidDataException>(() => new RequestSwitchToCharacterInSlotPacket().Read(reader));
        }

        [DataTestMethod]
        [DataRow(-255)]
        [DataRow(257)]
        public void InvalidSlotIntegerCannotWrapIntoSlotOne(int slot)
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            writer.WriteTuple(2);
            writer.WriteInt(slot);
            writer.WriteBool(false);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.ThrowsException<OverflowException>(() => new RequestSwitchToCharacterInSlotPacket().Read(reader));
        }
    }
}
