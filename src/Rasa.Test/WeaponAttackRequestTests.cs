using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Memory;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.Protocol;

namespace Rasa.Test
{
    [TestClass]
    public class WeaponAttackRequestTests
    {
        [DataTestMethod]
        [DataRow("int", false)]
        [DataRow("int", true)]
        [DataRow("long", false)]
        [DataRow("long", true)]
        [DataRow("none", false)]
        [DataRow("none", true)]
        public void EntityTargetEncodingAndAlternateFlagSurviveDecoding(string encoding, bool alternate)
        {
            var request = Decode(writer =>
            {
                writer.WriteTuple(4);
                writer.WriteInt(174);
                writer.WriteInt(3);
                if (encoding == "int") writer.WriteUInt(0xF0000001U);
                else if (encoding == "long") writer.WriteULong(0xF000000100000002UL);
                else writer.WriteNoneStruct();
                writer.WriteBool(alternate);
            });
            Assert.AreEqual(ActionId.WeaponMelee, request.ActionId);
            Assert.AreEqual(3, request.ActionArgId);
            var expected = encoding == "int" ? (ulong?)0xF0000001U :
                encoding == "long" ? 0xF000000100000002UL : null;
            Assert.AreEqual(expected, request.TargetId);
            Assert.IsNull(request.TargetLocation);
            Assert.AreEqual(alternate, request.IsAltAction);
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void LocationTargetKeepsItsCoordinatesSeparateFromAnEntityId(bool list)
        {
            var request = Decode(writer =>
            {
                writer.WriteTuple(4);
                writer.WriteInt(140);
                writer.WriteInt(1);
                if (list) writer.WriteList(3); else writer.WriteTuple(3);
                writer.WriteInt(-2);
                writer.WriteDouble(1.5);
                writer.WriteDouble(30.25);
                writer.WriteBool(false);
            });
            Assert.IsNull(request.TargetId);
            Assert.AreEqual((-2D, 1.5D, 30.25D), request.TargetLocation.Value);
        }

        [DataTestMethod]
        [DataRow(3)]
        [DataRow(5)]
        public void WrongRequestArityIsRejected(int count)
        {
            Assert.ThrowsException<InvalidClientMessageException>(() => Decode(writer => writer.WriteTuple(count)));
        }

        [TestMethod]
        public void InvalidTargetShapeIsRejected()
        {
            Assert.ThrowsException<InvalidClientMessageException>(() => Decode(writer =>
            {
                writer.WriteTuple(4);
                writer.WriteInt(1);
                writer.WriteInt(1);
                writer.WriteString("invalid target");
                writer.WriteBool(false);
            }));
        }

        [TestMethod]
        public void InvalidLocationArityAndNonFiniteCoordinatesAreRejected()
        {
            foreach (var invalidArity in new[] { true, false })
                Assert.ThrowsException<InvalidClientMessageException>(() => Decode(writer =>
                {
                    writer.WriteTuple(4);
                    writer.WriteInt(140);
                    writer.WriteInt(1);
                    writer.WriteTuple(invalidArity ? 2 : 3);
                    writer.WriteDouble(double.NaN);
                    writer.WriteDouble(0);
                    writer.WriteDouble(0);
                    writer.WriteBool(false);
                }));
        }

        private static RequestWeaponAttackPacket Decode(Action<PythonWriter> write)
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            var request = new RequestWeaponAttackPacket();
            request.Read(reader);
            Assert.AreEqual(stream.Length, stream.Position);
            return request;
        }
    }
}
