namespace Rasa.Packets.Wargame.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// client/wargame.py OnRevokeWargameDuelChallenge -> SendWorldMsg('WargameChallengeRevoked', ()),
    /// opcode 638: the Revoke button on the challenger's own dialog. It carries nothing, because a
    /// challenger holds one outstanding challenge at a time (PM_WARGAME_FAIL_WAIT_FOR_RESPONSE,
    /// "You have already challenged someone to a Wargame.").
    ///
    /// The client closes its own dialog before sending, but does not delete its WargameStatus -
    /// only Recv_RevokeWargameChallenge does that - so the server sends RevokeWargameChallenge
    /// back to the challenger as well as to the target.
    /// </summary>
    public class WargameChallengeRevokedPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WargameChallengeRevoked;

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
        }
    }
}
