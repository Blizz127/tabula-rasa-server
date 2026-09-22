namespace Rasa.Packets.Wargame.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// client/wargame.py Recv_ChallengedToWargameDuel(wargameId, agressorName), opcode 603, on
    /// sysentity.ClientWargameManagerId (23). What the challenged player gets.
    ///
    /// It posts clientevent.WARGAME_DUEL_REQUEST_PENDING, which statusupdaterwindow's
    /// ShowWargameDuelRequestWindow turns into wargame.ActivateWargameDuelDialog(agressorName):
    /// a message box carrying PM_CHALLENGED_TO_WARGAME_DUEL ("%(player)s has challenged you to a
    /// Wargame Duel.") with Accept (enter) and Decline (esc). It also raises the DUEL tutorial and
    /// registers a WargameStatus under this id with wargameType DUEL and bInvited True.
    ///
    /// Ordering matters: _AddWargameStatus is the only thing that creates the status, and
    /// Recv_WargameStarted / Recv_SetWargameMaxKills / Recv_DisplayWargameTimer all go through
    /// _GetWargameStatusEnsured, which logs and returns None for an id it has never seen. So this
    /// packet must reach the client before any of those, and the id must never be reused inside a
    /// session - _AddWargameStatus logs "Duplicate wargameId used" and keeps the old entry, and
    /// only Recv_RevokeWargameChallenge ever deletes one.
    /// </summary>
    public class ChallengedToWargameDuelPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ChallengedToWargameDuel;

        public uint WargameId { get; set; }
        public string AgressorName { get; set; }

        public ChallengedToWargameDuelPacket(uint wargameId, string agressorName)
        {
            WargameId = wargameId;
            AgressorName = agressorName;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(WargameId);
            pw.WriteUnicodeString(AgressorName);
        }
    }
}
