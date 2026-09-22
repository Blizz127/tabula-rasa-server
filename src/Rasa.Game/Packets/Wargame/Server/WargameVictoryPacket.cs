namespace Rasa.Packets.Wargame.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// client/wargame.py Recv_WargameVictory(wargameId), opcode 608, on entity 23.
    ///
    /// Clears bActive, sets didYourSideWin, hides the tracker, and for a DUEL shows
    /// PM_WARGAME_VICTORY ("You have won the Wargame.") plus the big on-screen
    /// PM_WARGAME_BIGTEXT_DUEL_WIN ("Duel Wargame: You Win!"), which the client builds itself from
    /// the status - the server sends no text for it. Then audiosetdata.UI_ALERT_CLAN_WARGAME_END.
    ///
    /// It returns silently if the id is unknown, unlike the handlers that go through
    /// _GetWargameStatusEnsured.
    /// </summary>
    public class WargameVictoryPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WargameVictory;

        public uint WargameId { get; set; }

        public WargameVictoryPacket(uint wargameId)
        {
            WargameId = wargameId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(WargameId);
        }
    }
}
