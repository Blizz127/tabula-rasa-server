using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class UsePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Use;

        public ulong PlayerEntityId { get; set; }
        public UseObjectState CurState { get; set; }
        public int WindupTimeMs { get; set; }
        public List<int> Args = new List<int>();

        public UsePacket(ulong playerEntityId, UseObjectState curState, int windupTimeMs)
        {
            PlayerEntityId = playerEntityId;
            CurState = curState;
            WindupTimeMs = windupTimeMs;
        }

        /// <summary>
        /// client/augmentations/usable.py Recv_Use(self, actorId, curStateId, windupTimeMs, *args), line 902: anything
        /// after the three fixed arguments is handed to the augmentation's OnBeforeUse / _Transition / OnAfterUse. The
        /// mech pad is one such augmentation (mechpad.py OnBeforeUse(actorId, boardingTimeMs, effectTypeId)), so the
        /// extra arguments go on the wire when there are any.
        /// </summary>
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3 + Args.Count);
            pw.WriteULong(PlayerEntityId);
            pw.WriteUInt((uint)CurState);
            pw.WriteInt(WindupTimeMs);
            foreach (var arg in Args)
                pw.WriteInt(arg);
        }
    }
}
