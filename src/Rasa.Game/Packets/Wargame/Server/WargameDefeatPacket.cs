namespace Rasa.Packets.Wargame.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// client/wargame.py Recv_WargameDefeat(wargameId), opcode 607, on entity 23. The mirror of
    /// WargameVictory: clears bActive, hides the tracker, and for a DUEL shows PM_WARGAME_DEFEAT
    /// ("You have been defeated in a Wargame.") and the big PM_WARGAME_BIGTEXT_DUEL_LOSE
    /// ("Duel Wargame: You Lost!").
    ///
    /// Note what it does not do: nothing here kills the player, and shared/gameconstants.py gives
    /// a duel WARGAME_FLAGS_DUEL = WARGAME_BLOCK_INTERACTTIONS | WARGAME_IS_AGGRESSIVE, without
    /// WARGAME_REZ_SICKNESS or WARGAME_WEAPON_DECAY (which only WARGAME_FLAGS_CLAN carries). A
    /// duel loss costs nothing, so the server never lets the losing blow become a real death.
    /// </summary>
    public class WargameDefeatPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WargameDefeat;

        public uint WargameId { get; set; }

        public WargameDefeatPacket(uint wargameId)
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
