using System;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class SetUsablePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetUsable;

        public bool Enabled { get; set; }

        public SetUsablePacket(bool enabled)
        {
            Enabled = enabled;
        }

        // client/augmentations/usable.pyo Recv_SetUsable(self, isEnabled):
        // one bool; toggling it on attaches and off detaches the
        // MISSION_USABLE_INDICATOR effect when missionActivated is set.
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteBool(Enabled);
        }
    }
}
