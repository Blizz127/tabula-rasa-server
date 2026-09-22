namespace Rasa.Packets.Wargame.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// client/wargame.py Recv_RevokeWargameChallenge(wargameId), opcode 630, on entity 23.
    ///
    /// The only handler that calls _DeleteWargameStatus: it posts WARGAME_CHALLENGE_REVOKED,
    /// closes both challenge windows and forgets the id entirely. That makes it the right packet
    /// for every way a challenge ends without a duel - revoked by the challenger, or timed out -
    /// and it is sent to both sides, because the challenger's own status would otherwise be left
    /// behind (OnRevokeWargameDuelChallenge only kills the indicator).
    /// </summary>
    public class RevokeWargameChallengePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RevokeWargameChallenge;

        public uint WargameId { get; set; }

        public RevokeWargameChallengePacket(uint wargameId)
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
