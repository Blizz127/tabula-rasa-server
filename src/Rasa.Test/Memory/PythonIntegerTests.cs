using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Memory;

namespace Rasa.Test.Memory
{
    [TestClass]
    public class PythonIntegerTests
    {
        // Compact signed integer forms from the existing protocol. Literal wire
        // vectors catch bugs that a writer/reader round trip alone could hide.
        [DataTestMethod]
        [DataRow(0, new byte[] { 0x10 })]
        [DataRow(12, new byte[] { 0x1C })]
        [DataRow(13, new byte[] { 0x1D, 0x0D })]
        [DataRow(127, new byte[] { 0x1D, 0x7F })]
        [DataRow(128, new byte[] { 0x1E, 0x80, 0x00 })]
        [DataRow(-1, new byte[] { 0x1D, 0xFF })]
        [DataRow(-128, new byte[] { 0x1D, 0x80 })]
        [DataRow(-129, new byte[] { 0x1E, 0x7F, 0xFF })]
        [DataRow(-32768, new byte[] { 0x1E, 0x00, 0x80 })]
        [DataRow(32767, new byte[] { 0x1E, 0xFF, 0x7F })]
        [DataRow(32768, new byte[] { 0x1F, 0x00, 0x80, 0x00, 0x00 })]
        [DataRow(-32769, new byte[] { 0x1F, 0xFF, 0x7F, 0xFF, 0xFF })]
        [DataRow(int.MinValue, new byte[] { 0x1F, 0x00, 0x00, 0x00, 0x80 })]
        [DataRow(int.MaxValue, new byte[] { 0x1F, 0xFF, 0xFF, 0xFF, 0x7F })]
        public void SignedIntegerUsesExpectedWireForm(int value, byte[] expected)
        {
            using var output = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(output));
            writer.WriteInt(value);
            CollectionAssert.AreEqual(expected, output.ToArray());

            using var input = new MemoryStream(expected);
            using var reader = new PythonReader(new BinaryReader(input));
            Assert.AreEqual(value, reader.ReadInt());
            Assert.AreEqual(input.Length, input.Position);
        }

        [TestMethod]
        public void NegativeIntegerDoesNotCorruptFollowingTupleElements()
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            writer.WriteTuple(3);
            writer.WriteInt(-1);
            writer.WriteInt(49);
            writer.WriteBool(true);
            CollectionAssert.AreEqual(new byte[] { 0x83, 0x1D, 0xFF, 0x1D, 0x31, 0x01 }, stream.ToArray());
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(-1, reader.ReadInt());
            Assert.AreEqual(49, reader.ReadInt());
            Assert.IsTrue(reader.ReadBool());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [DataTestMethod]
        [DataRow(0x80000000U, new byte[] { 0x1F, 0x00, 0x00, 0x00, 0x80 })]
        [DataRow(0xFFFF8000U, new byte[] { 0x1F, 0x00, 0x80, 0xFF, 0xFF })]
        [DataRow(0xFFFFFF80U, new byte[] { 0x1F, 0x80, 0xFF, 0xFF, 0xFF })]
        [DataRow(uint.MaxValue, new byte[] { 0x1F, 0xFF, 0xFF, 0xFF, 0xFF })]
        public void UnsignedIdentifiersRetainAllBits(uint value, byte[] expected)
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            writer.WriteUInt(value);
            CollectionAssert.AreEqual(expected, stream.ToArray());
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(value, reader.ReadUInt());
            Assert.AreEqual(stream.Length, stream.Position);
        }
    }
}
