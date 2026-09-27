using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Memory;
using Rasa.Packets.MapChannel.Client;

namespace Rasa.Test
{
    [TestClass]
    public class UserOptionsPacketTests
    {
        // Rebinding a key on live (2026-09-27 21:43 UTC) sent an option whose value is the empty unicode
        // string 0x50; it was read as null, the user_option NOT NULL constraint failed and the client was
        // disconnected. Each option must come through as a non-null string.
        [TestMethod]
        public void EmptyOptionValueIsReadAsAnEmptyStringNotNull()
        {
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, System.Text.Encoding.UTF8, true)))
            {
                writer.WriteTuple(1);
                writer.WriteList(2);
                writer.WriteTuple(2);
                writer.WriteUInt(7);
                writer.WriteUnicodeString("R");
                writer.WriteTuple(2);
                writer.WriteUInt(8);
                writer.WriteUnicodeString(null);
            }

            stream.Position = 0;
            var packet = new SaveUserOptionsPacket();
            using (var reader = new PythonReader(new BinaryReader(stream)))
                packet.Read(reader);

            Assert.AreEqual(2, packet.OptionsList.Count);
            Assert.AreEqual("R", packet.OptionsList[0].Value);
            Assert.AreEqual(string.Empty, packet.OptionsList[1].Value);
        }
    }
}
