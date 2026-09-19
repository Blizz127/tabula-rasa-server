namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Recv_PlayerEnteredCombat(self) takes no arguments; it only posts UI_UPDATE_AVATAR_COMBAT_STATUS, which toggles the combat
    /// indicator on the player's own status window. Sent to the player's own entity only.
    /// </summary>
    public class PlayerEnteredCombatPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PlayerEnteredCombat;

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(0);
        }
    }
}
