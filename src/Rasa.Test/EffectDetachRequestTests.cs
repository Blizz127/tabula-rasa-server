using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Memory;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.Protocol;

namespace Rasa.Test
{
    [TestClass]
    public class EffectDetachRequestTests
    {
        [TestMethod]
        public void OriginalCancelCallCarriesOneEffectId()
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            writer.WriteTuple(1);
            writer.WriteInt(12345);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            var packet = new RequestDetachGameEffectPacket();
            packet.Read(reader);
            Assert.AreEqual(12345, packet.EffectId);
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [DataTestMethod]
        [DataRow(0)]
        [DataRow(2)]
        public void InvalidCancelArityIsRejected(int count)
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            writer.WriteTuple(count);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.ThrowsException<InvalidClientMessageException>(() => new RequestDetachGameEffectPacket().Read(reader));
        }

        [TestMethod]
        public void CancelDoesNotCoerceUnrelatedArgumentTypes()
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            writer.WriteTuple(1);
            writer.WriteString("12345");
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.ThrowsException<InvalidClientMessageException>(() => new RequestDetachGameEffectPacket().Read(reader));
        }
    }
}
