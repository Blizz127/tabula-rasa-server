namespace Rasa.Packets.Wargame.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// client/wargame.py SendWargameChallengeResponse(accepted, pmMsg = None) ->
    /// gameclient.SendWorldMsg('WargameChallengeResponse', (accepted, pmMsg)), opcode 606.
    ///
    /// The two buttons on the challenged player's dialog are the only duel callers:
    /// OnAcceptWargameDuelChallenge sends (1,) and OnRefuseWargameDuelChallenge sends
    /// (0, playermessage.PM_WARGAME_REFUSED). The refusal line the challenger is shown is
    /// therefore named by the refusing client, and pmMsg has a default, so the tuple can hold
    /// one element or two. The server treats the id as a request, not an instruction:
    /// WargameManager only relays ids from the client's own refusal vocabulary.
    /// </summary>
    public class WargameChallengeResponsePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WargameChallengeResponse;

        public bool Accepted { get; set; }

        /// <summary>The refusal message the client asks the server to relay, or null for none.</summary>
        public PlayerMessage? PmMsg { get; set; }

        public override void Read(PythonReader pr)
        {
            var count = pr.ReadTuple();

            // The client sends the integers 1 and 0 rather than True and False; ReadBool takes both.
            Accepted = pr.ReadBool();

            if (count < 2)
                return;

            if (pr.PeekType() == PythonType.Structs)
            {
                pr.ReadNoneStruct();
                return;
            }

            PmMsg = (PlayerMessage)pr.ReadUInt();
        }
    }
}
