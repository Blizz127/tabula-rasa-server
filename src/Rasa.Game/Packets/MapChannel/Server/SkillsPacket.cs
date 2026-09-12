using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class SkillsPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Skills;

        private readonly Dictionary<SkillId, SkillsData> _skillsData = new Dictionary<SkillId, SkillsData>();
        
        public SkillsPacket(Dictionary<SkillId, SkillsData> skillsData)
        {
            foreach (var skill in skillsData)
                _skillsData.Add(skill.Key, new SkillsData(skill.Value.SkillId, skill.Value.AbilityId, skill.Value.SkillLevel));
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(_skillsData.Count);
            foreach(var entry in _skillsData)
            {
                pw.WriteTuple(2);
                pw.WriteInt((int)entry.Value.SkillId);
                pw.WriteInt(entry.Value.SkillLevel);
            }
        }
    }
}
