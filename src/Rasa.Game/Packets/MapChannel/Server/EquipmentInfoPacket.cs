using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class EquipmentInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.EquipmentInfo;

        private readonly List<(uint Slot, ulong EntityId)> _equipment = new();

        public EquipmentInfoPacket(List<ulong> equipmentInfo)
        {
            for (var slot = 0; slot < equipmentInfo.Count; slot++)
                if (equipmentInfo[slot] != 0)
                    _equipment.Add(((uint)slot, equipmentInfo[slot]));
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(_equipment.Count);
            foreach (var entry in _equipment)
            {
                pw.WriteTuple(2);
                pw.WriteUInt(entry.Slot);
                pw.WriteULong(entry.EntityId);
            }
        }
    }
}
