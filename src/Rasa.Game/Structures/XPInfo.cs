namespace Rasa.Structures
{
    using Memory;
	
    public class XPInfo : IPythonDataStruct
    {
        public uint Total = 0;
        public uint Gained = 0;
        public uint BaseGained = 0;
        /// <summary>A float in the client: Recv_ExperienceChanged prints int(groupMod * 100) - 100 as the group bonus.</summary>
        public double GroupMod = 1.0;
        public int StreakMod = 1;
        public int BoosterMod = 1;
        public bool WasCritKill = false;
        public bool WasTeamCritKill = false;

        public XPInfo()
        {
        }

        public XPInfo(uint total, uint gained, uint baseGained)
        {
            Total = total;
            Gained = gained;
            BaseGained = baseGained;
        }

        public void Read(PythonReader pr)
        {
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(8);
            pw.WriteUInt(Total);
            pw.WriteUInt(Gained);
            pw.WriteUInt(BaseGained);
            // Full precision: the client truncates groupMod * 100, and 2.52 or 3.04 narrowed to
            // single precision would print one percent less.
            pw.WriteExactDouble(GroupMod);
            pw.WriteInt(StreakMod);
            pw.WriteInt(BoosterMod);
            pw.WriteBool(WasCritKill);
            pw.WriteBool(WasTeamCritKill);
        }
    }
}
