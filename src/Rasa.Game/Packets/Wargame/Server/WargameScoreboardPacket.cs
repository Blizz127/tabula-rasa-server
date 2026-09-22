namespace Rasa.Packets.Wargame.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// client/wargame.py Recv_WargameScoreboard(wargameId, yourKills, theirKills, victimId, killerId),
    /// opcode 632, on entity 23.
    ///
    /// yourKills and theirKills are oriented to the recipient - the handler assigns them straight
    /// to killsFor and killsAgainst - so the two duellists get mirrored copies of the same packet.
    /// victimId and killerId are not oriented: WargameStatus.UpdateScores adds a death to one and
    /// a kill to the other in userScores, which is keyed by the same userId the squad roster uses
    /// (the account id). Either may be None, and the handler guards for it.
    ///
    /// It only records the score while status.bActive; otherwise it logs
    /// "Recv_WargameScoreboard received without an active wargame" and still refreshes the tracker.
    /// So the scoreboard has to arrive before the victory or defeat that clears bActive.
    /// </summary>
    public class WargameScoreboardPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WargameScoreboard;

        public uint WargameId { get; set; }
        public int YourKills { get; set; }
        public int TheirKills { get; set; }
        public uint? VictimId { get; set; }
        public uint? KillerId { get; set; }

        public WargameScoreboardPacket(uint wargameId, int yourKills, int theirKills, uint? victimId, uint? killerId)
        {
            WargameId = wargameId;
            YourKills = yourKills;
            TheirKills = theirKills;
            VictimId = victimId;
            KillerId = killerId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(5);
            pw.WriteUInt(WargameId);
            pw.WriteInt(YourKills);
            pw.WriteInt(TheirKills);

            if (VictimId.HasValue)
                pw.WriteUInt(VictimId.Value);
            else
                pw.WriteNoneStruct();

            if (KillerId.HasValue)
                pw.WriteUInt(KillerId.Value);
            else
                pw.WriteNoneStruct();
        }
    }
}
