using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Memory;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.Protocol;

namespace Rasa.Test.Memory
{
    [TestClass]
    public class AbilityPacketTests
    {
        [TestMethod]
        public void EntityAndItemIdsRetainAllSixtyFourBits()
        {
            var packet = Decode(pw =>
            {
                Header(pw, 4);
                pw.WriteULong(0x100000002UL);
                pw.WriteULong(0x200000003UL);
            });
            Assert.AreEqual(0x100000002UL, packet.Target.Value);
            Assert.AreEqual(0x200000003UL, packet.ItemId.Value);
            Assert.IsNull(packet.TargetLocation);
            Assert.IsNull(packet.ClientYaw);
        }

        [TestMethod]
        public void NoTargetAndNoItemAreValidNoneFields()
        {
            var packet = Decode(pw => { Header(pw, 4); pw.WriteNoneStruct(); pw.WriteNoneStruct(); });
            Assert.IsNull(packet.Target);
            Assert.IsNull(packet.TargetLocation);
            Assert.IsNull(packet.ItemId);
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void LocationSequenceAndOptionalYawAreConsumed(bool list)
        {
            var packet = Decode(pw =>
            {
                Header(pw, 5);
                if (list) pw.WriteList(3); else pw.WriteTuple(3);
                pw.WriteDouble(101.25); pw.WriteInt(-42); pw.WriteDouble(303.75);
                pw.WriteInt(23);
                pw.WriteDouble(-1.25);
            });
            Assert.AreEqual((101.25, -42.0, 303.75), packet.TargetLocation.Value);
            Assert.IsNull(packet.Target);
            Assert.AreEqual(23UL, packet.ItemId.Value);
            Assert.AreEqual(-1.25, packet.ClientYaw.Value);
        }

        [TestMethod]
        public void MalformedTupleAndLocationAreRejected()
        {
            Assert.ThrowsException<InvalidClientMessageException>(() => Decode(pw => Header(pw, 3)));
            Assert.ThrowsException<InvalidClientMessageException>(() => Decode(pw =>
            {
                Header(pw, 4); pw.WriteTuple(2); pw.WriteDouble(1); pw.WriteDouble(2); pw.WriteNoneStruct();
            }));
            Assert.ThrowsException<InvalidClientMessageException>(() => Decode(pw =>
            {
                Header(pw, 5); pw.WriteNoneStruct(); pw.WriteNoneStruct(); pw.WriteDouble(double.NaN);
            }));
        }

        private static void Header(PythonWriter pw, int size)
        {
            pw.WriteTuple(size); pw.WriteInt(194); pw.WriteInt(3);
        }

        private static RequestPerformAbilityPacket Decode(Action<PythonWriter> write)
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            var packet = new RequestPerformAbilityPacket();
            packet.Read(reader);
            Assert.AreEqual(stream.Length, stream.Position);
            return packet;
        }
    }
}
