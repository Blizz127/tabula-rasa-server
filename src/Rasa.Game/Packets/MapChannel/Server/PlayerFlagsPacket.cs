using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class PlayerFlagsPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PlayerFlags;

        public List<uint> PlayerFlags { get; }

        // The client stores the value and evaluates HasPlayerFlag as 'id in flags',
        // so the argument must be a sequence, never a plain integer.
        public PlayerFlagsPacket(IEnumerable<uint> playerFlags)
        {
            PlayerFlags = playerFlags == null ? new List<uint>() : new List<uint>(playerFlags);
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(PlayerFlags.Count);
            foreach (var flag in PlayerFlags)
                pw.WriteUInt(flag);
        }
    }
}

