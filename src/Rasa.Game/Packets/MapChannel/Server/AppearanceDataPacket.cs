using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class AppearanceDataPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AppearanceData;

        private readonly List<AppearanceData> _appearanceData = new();

        public AppearanceDataPacket(Dictionary<EquipmentData, AppearanceData> appearanceData)
        {
            foreach (var appearance in appearanceData.Values)
                _appearanceData.Add(new AppearanceData
                {
                    SlotId = appearance.SlotId,
                    Class = appearance.Class,
                    Color = appearance.Color == null ? null : new Color(appearance.Color.Hue),
                    Hue2 = appearance.Hue2 == null ? null : new Color(appearance.Hue2.Hue)
                });
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(_appearanceData.Count);
            foreach (var appearance in _appearanceData)
            {
                pw.WriteInt((int)appearance.SlotId);
                pw.WriteTuple(3);
                pw.WriteUInt(appearance.Class);
                pw.WriteStruct(appearance.Color);
                pw.WriteStruct(appearance.Hue2);
            }
        }
    }
}
