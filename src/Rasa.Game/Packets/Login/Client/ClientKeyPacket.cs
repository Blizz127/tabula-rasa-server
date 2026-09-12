using System;
using System.IO;

namespace Rasa.Packets.Login.Client
{
    using Cryptography;
    using Data;

    public class ClientKeyPacket : IOpcodedPacket<LoginOpcode>
    {
        public LoginOpcode Opcode { get; } = LoginOpcode.ClientKey;
        public BigNum B { get; set; } = new BigNum();

        public void Read(BinaryReader br)
        {
            var bLen = br.ReadInt32();
            if (bLen < 0 || bLen > 64)
                throw new InvalidDataException("Invalid client key length.");

            var keyBytes = br.ReadBytes(bLen);
            if (keyBytes.Length != bLen)
                throw new EndOfStreamException("Truncated client key.");
            B.ReadBigEndian(keyBytes, 0, bLen);
        }

        public void Write(BinaryWriter bw)
        {
            var data = new byte[64];
            B.WriteToBigEndian(data, 0, data.Length);
        }
    }
}
