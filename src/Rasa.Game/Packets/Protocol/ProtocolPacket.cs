using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Rasa.Packets.Protocol
{
    using Data;
    using Memory;

    public class ProtocolPacket : IBasePacket
    {
        public ClientMessageOpcode Type { get; private set; } = ClientMessageOpcode.None;

        public ushort Size { get; private set; }
        public byte Channel { get; private set; }
        public uint SequenceNumber { get; set; }
        public bool Compress { get; private set; }
        public IClientMessage Message { get; set; }

        public ProtocolPacket()
        {
        }

        public ProtocolPacket(IClientMessage message, ClientMessageOpcode type, bool compress, byte channel)
        {
            Message = message;
            Type = type;
            Compress = compress;
            Channel = channel;
        }

        public void Read(BinaryReader br)
        {
            var available = br.BaseStream.Length - br.BaseStream.Position;
            if (available < 4)
                throw new InvalidDataException("Truncated protocol frame header.");

            Size = br.ReadUInt16();
            Channel = br.ReadByte();
            br.ReadByte(); // padding
            if (Size < 4 || Size > available)
                throw new InvalidDataException("Invalid protocol frame size.");

            if (Channel == 0xFF)
            {
                if (Size != 4)
                    throw new InvalidDataException("Invalid internal timeout frame.");
                return;
            }

            if (Channel != 0)
            {
                if (Size < 16)
                    throw new InvalidDataException("Truncated protocol channel header.");
                SequenceNumber = br.ReadUInt32();
                br.ReadInt32(); // 0xDEADBEEF
                br.ReadInt32(); // skip
            }

            var packetBeginPosition = br.BaseStream.Position;
            using (var reader = new ProtocolBufferReader(br, ProtocolBufferFlags.DontFragment))
            {
                reader.ReadProtocolFlags();
                reader.ReadPacketType(out ushort type, out bool compress);
                Type = (ClientMessageOpcode)type;
                Compress = compress;
                reader.ReadXORCheck((int)(br.BaseStream.Position - packetBeginPosition));
            }

            var xorCheckPosition = br.BaseStream.Position;
            var readBr = br;
            MemoryStream decompressed = null;
            try
            {
                if (Compress)
                {
                    var compressionType = br.ReadByte();
                    if (compressionType > 1)
                        throw new InvalidDataException("Invalid compression type.");
                    if (compressionType == 1)
                    {
                        var declaredSize = br.ReadInt32();
                        if (declaredSize < 0)
                            throw new InvalidDataException("Invalid decompressed length.");

                        // A claimed size must not cause an allocation before bytes exist.
                        // The input stream is bounded to one received protocol frame.
                        decompressed = new MemoryStream();
                        var buffer = ArrayPool<byte>.Shared.Rent(8192);
                        try
                        {
                            using var deflate = new DeflateStream(br.BaseStream, CompressionMode.Decompress, true);
                            int count;
                            while ((count = deflate.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                if (decompressed.Length + count > declaredSize)
                                    throw new InvalidDataException("Decompressed data exceeds its declared length.");
                                decompressed.Write(buffer, 0, count);
                            }
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(buffer);
                        }
                        if (decompressed.Length != declaredSize)
                            throw new InvalidDataException("Decompressed length does not match its declaration.");
                        decompressed.Position = 0;
                        readBr = new BinaryReader(decompressed, Encoding.UTF8, true);
                        xorCheckPosition = 0;
                    }
                }

                Message = Type switch
                {
                    ClientMessageOpcode.Login => new LoginMessage(),
                    ClientMessageOpcode.Move => new MoveMessage(),
                    ClientMessageOpcode.CallServerMethod => new CallServerMethodMessage(),
                    ClientMessageOpcode.Ping => new PingMessage(),
                    _ => throw new InvalidDataException($"Unsupported incoming packet type {Type}."),
                };

                using (var reader = new ProtocolBufferReader(readBr, ProtocolBufferFlags.DontFragment))
                {
                    reader.ReadProtocolFlags();
                    reader.ReadDebugByte(41);
                    if ((Message.SubtypeFlags & ClientMessageSubtypeFlag.HasSubtype) != 0)
                    {
                        Message.RawSubtype = reader.ReadByte();
                        if (Message.RawSubtype < Message.MinSubtype || Message.RawSubtype > Message.MaxSubtype)
                            throw new InvalidDataException("Invalid message subtype.");
                    }
                    Message.Read(reader);
                    reader.ReadDebugByte(42);
                    reader.ReadXORCheck((int)(readBr.BaseStream.Position - xorCheckPosition));
                }
                if (decompressed != null && decompressed.Position != decompressed.Length)
                    throw new InvalidDataException("Unexpected data after decompressed message.");
            }
            finally
            {
                if (readBr != br)
                    readBr.Dispose();
                decompressed?.Dispose();
            }
        }

        public void Write(BinaryWriter bw)
        {
            var sizePosition = bw.BaseStream.Position;

            bw.Write((ushort) 0); // Size placeholder

            bw.Write(Channel);
            bw.Write((byte) 0); // padding

            if (Channel != 0)
            {
                bw.Write(SequenceNumber); // sequence num?
                bw.Write(0xDEADBEEF); // const
                bw.Write(0); // padding
            }

            var packetBeginPosition = (int) bw.BaseStream.Position;

            // TODO: find limits and maybe lower this number
            // OR: use NCMS as the target stream
            var packetBuffer = ArrayPool<byte>.Shared.Rent(0x8000);
            int uncompressedSize;

            using (var ms = new MemoryStream(packetBuffer, true))
            {
                using var packetWriter = new BinaryWriter(ms, Encoding.UTF8, true);
                using var writer = new ProtocolBufferWriter(packetWriter, ProtocolBufferFlags.DontFragment);

                writer.WriteProtocolFlags();

                writer.WriteDebugByte(41);

                if ((Message.SubtypeFlags & ClientMessageSubtypeFlag.HasSubtype) == ClientMessageSubtypeFlag.HasSubtype)
                    writer.WriteByte(Message.RawSubtype);

                Message.Write(writer);

                writer.WriteDebugByte(42);

                var currentPos = (int)ms.Position;

                writer.WriteXORCheck(currentPos);

                uncompressedSize = (int)ms.Position;
            }

            var compress = (Message.SubtypeFlags & ClientMessageSubtypeFlag.Compress) == ClientMessageSubtypeFlag.Compress && uncompressedSize > 0;

            using (var writer = new ProtocolBufferWriter(bw, ProtocolBufferFlags.DontFragment))
            {
                writer.WriteProtocolFlags();

                writer.WritePacketType((ushort) Message.Type, compress);

                writer.WriteXORCheck((int) (bw.BaseStream.Position - packetBeginPosition));

            }

            int packetSize = uncompressedSize;

            if (compress) // TODO: test
            {
                bw.Write((byte) 0x01);
                bw.Write(uncompressedSize);

                var compressedBuffer = ArrayPool<byte>.Shared.Rent(uncompressedSize);
                int compressedSize;

                using (var compressStream = new MemoryStream(compressedBuffer, true))
                {
                    using (var compressorStream = new DeflateStream(compressStream, CompressionMode.Compress, true))
                        compressorStream.Write(packetBuffer, 0, uncompressedSize);

                    compressedSize = (int) compressStream.Position;
                }

                ArrayPool<byte>.Shared.Return(packetBuffer);

                packetBuffer = compressedBuffer;
                packetSize = compressedSize;
            }

            bw.Write(packetBuffer, 0, packetSize);

            ArrayPool<byte>.Shared.Return(packetBuffer);

            var currentPosition = bw.BaseStream.Position;

            bw.BaseStream.Position = sizePosition;

            bw.Write((ushort) (currentPosition - sizePosition));

            bw.BaseStream.Position = currentPosition;
        }
    }
}
