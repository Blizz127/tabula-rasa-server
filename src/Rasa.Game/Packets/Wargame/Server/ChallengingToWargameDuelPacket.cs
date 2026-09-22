namespace Rasa.Packets.Wargame.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// client/wargame.py Recv_ChallengingToWargameDuel(wargameId, targetName), opcode 635, on
    /// entity 23. The mirror of ChallengedToWargameDuel: what the challenger gets.
    ///
    /// It posts clientevent.WARGAME_DUEL_CHALLENGE_MADE, which statusupdaterwindow's
    /// ShowWargameDuelChallengeWindow turns into
    /// wargame.ActivateWargameDuelChallengeDialog(targetName, wargameId): a message box carrying
    /// PM_CHALLENGING_TO_WARGAME_DUEL ("You are challenging %(player)s to a Wargame Duel.") with
    /// Revoke (enter, which sends WargameChallengeRevoked) and Close (esc, which does nothing at
    /// all - OnCloseWargameDuelChallenge has an empty body, so closing the window leaves the
    /// challenge standing).
    ///
    /// It registers the challenger's own WargameStatus under this id, so it has the same ordering
    /// duty as ChallengedToWargameDuel.
    /// </summary>
    public class ChallengingToWargameDuelPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ChallengingToWargameDuel;

        public uint WargameId { get; set; }
        public string TargetName { get; set; }

        public ChallengingToWargameDuelPacket(uint wargameId, string targetName)
        {
            WargameId = wargameId;
            TargetName = targetName;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(WargameId);
            pw.WriteUnicodeString(TargetName);
        }
    }
}
