using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Memory;

namespace Rasa.Test.Memory
{
    [TestClass]
    public class PythonStringTests
    {
        // PythonWriter only ever emits the null form and the 0xD/0xE/0xF length-prefix
        // forms, so a writer/reader round trip cannot reach the inline-length tags. These
        // literal wire vectors are the only coverage for them. The client is free to send
        // the compact forms, and before this the reader threw NotImplementedException on
        // 0x41/0x42/0x52, which tore down the connection.
        [DataTestMethod]
        [DataRow(null, new byte[] { 0x40 })]
        [DataRow("A", new byte[] { 0x41, 0x41 })]
        [DataRow("Hi", new byte[] { 0x42, 0x48, 0x69 })]
        [DataRow("abc", new byte[] { 0x43, 0x61, 0x62, 0x63 })]
        [DataRow("Hi", new byte[] { 0x4D, 0x02, 0x48, 0x69 })]
        [DataRow("Hi", new byte[] { 0x4E, 0x02, 0x00, 0x48, 0x69 })]
        [DataRow("Hi", new byte[] { 0x4F, 0x02, 0x00, 0x00, 0x00, 0x48, 0x69 })]
        public void StringReadsExpectedWireForm(string expected, byte[] wire)
        {
            using var input = new MemoryStream(wire);
            using var reader = new PythonReader(new BinaryReader(input));

            Assert.AreEqual(expected, reader.ReadString());
            Assert.AreEqual(input.Length, input.Position, "reader must consume the whole tag");
        }

        [DataTestMethod]
        [DataRow(null, new byte[] { 0x50 })]
        [DataRow("A", new byte[] { 0x51, 0x41 })]
        [DataRow("Hi", new byte[] { 0x52, 0x48, 0x69 })]
        [DataRow("abc", new byte[] { 0x53, 0x61, 0x62, 0x63 })]
        [DataRow("Hi", new byte[] { 0x5D, 0x02, 0x48, 0x69 })]
        [DataRow("Hi", new byte[] { 0x5E, 0x02, 0x00, 0x48, 0x69 })]
        [DataRow("Hi", new byte[] { 0x5F, 0x02, 0x00, 0x00, 0x00, 0x48, 0x69 })]
        public void UnicodeStringReadsExpectedWireForm(string expected, byte[] wire)
        {
            using var input = new MemoryStream(wire);
            using var reader = new PythonReader(new BinaryReader(input));

            Assert.AreEqual(expected, reader.ReadUnicodeString());
            Assert.AreEqual(input.Length, input.Position, "reader must consume the whole tag");
        }

        // Low nibbles 0x1-0xC are inline lengths for every tagged type in this protocol;
        // ReadInt, ReadUInt, ReadDictionary and ReadList already implemented theirs.
        [TestMethod]
        public void EveryInlineLengthIsSupportedForBothStringTypes()
        {
            var text = "abcdefghijkl"; // 12 chars, the largest inline length

            for (var length = 1; length <= 12; ++length)
            {
                var expected = text.Substring(0, length);
                var body = Encoding.UTF8.GetBytes(expected);

                var ascii = new byte[1 + body.Length];
                ascii[0] = (byte)(0x40 | length);
                body.CopyTo(ascii, 1);

                using (var input = new MemoryStream(ascii))
                using (var reader = new PythonReader(new BinaryReader(input)))
                {
                    Assert.AreEqual(expected, reader.ReadString(), $"inline ASCII length {length}");
                    Assert.AreEqual(input.Length, input.Position);
                }

                var unicode = new byte[1 + body.Length];
                unicode[0] = (byte)(0x50 | length);
                body.CopyTo(unicode, 1);

                using (var input = new MemoryStream(unicode))
                using (var reader = new PythonReader(new BinaryReader(input)))
                {
                    Assert.AreEqual(expected, reader.ReadUnicodeString(), $"inline unicode length {length}");
                    Assert.AreEqual(input.Length, input.Position);
                }
            }
        }

        // An inline-length string must not desynchronise the values that follow it, which
        // is how a mis-decoded tag would actually surface in a live packet.
        [TestMethod]
        public void InlineStringDoesNotCorruptFollowingValues()
        {
            var wire = new byte[]
            {
                0x83,                         // tuple of 3
                0x42, 0x48, 0x69,             // inline string "Hi"
                0x1D, 0xFF,                   // int -1
                0x51, 0x41                    // inline unicode "A"
            };

            using var input = new MemoryStream(wire);
            using var reader = new PythonReader(new BinaryReader(input));

            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual("Hi", reader.ReadString());
            Assert.AreEqual(-1, reader.ReadInt());
            Assert.AreEqual("A", reader.ReadUnicodeString());
            Assert.AreEqual(input.Length, input.Position);
        }

        // Records the current writer behaviour so the asymmetry stays a known, deliberate
        // fact rather than something silently "fixed" in either direction: the writer uses
        // the 1-byte prefix form even for strings short enough to be inline.
        [DataTestMethod]
        [DataRow("", new byte[] { 0x4D, 0x00 })]
        [DataRow("A", new byte[] { 0x4D, 0x01, 0x41 })]
        [DataRow("Hi", new byte[] { 0x4D, 0x02, 0x48, 0x69 })]
        public void WriterEmitsPrefixFormAndReaderAcceptsIt(string value, byte[] expected)
        {
            using var output = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(output)))
                writer.WriteString(value);

            CollectionAssert.AreEqual(expected, output.ToArray());

            output.Position = 0;
            using var reader = new PythonReader(new BinaryReader(output));
            Assert.AreEqual(value, reader.ReadString());
        }

        [TestMethod]
        public void WriterEmitsNullTagForNullString()
        {
            using var output = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(output)))
                writer.WriteString(null);

            CollectionAssert.AreEqual(new byte[] { 0x40 }, output.ToArray());

            output.Position = 0;
            using var reader = new PythonReader(new BinaryReader(output));
            Assert.IsNull(reader.ReadString());
        }
    }
}
