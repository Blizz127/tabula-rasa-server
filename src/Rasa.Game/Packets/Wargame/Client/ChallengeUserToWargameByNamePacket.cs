namespace Rasa.Packets.Wargame.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// client/wargame.py SendWargameChallenge(targetName, timeMins, maxKills) ->
    /// gameclient.SendWorldMsg('ChallengeUserToWargameByName', (targetName, timeMins, maxKills)),
    /// opcode 626 (generated/client/methodid.pyo ChallengeUserToWargameByName = 626).
    ///
    /// Two callers, and they disagree about the last two fields. The radial menu's
    /// "Challenge to Duel" goes through client/communicator.py ChallengeWargameDuel with the
    /// target's name alone, which leaves timeMins and maxKills at the 0 they are initialised to
    /// (communicator.py:701-702), and a non-numeric typed argument falls back to 0 as well. Zero
    /// therefore means "the server decides", and is what every right-click challenge carries.
    /// </summary>
    public class ChallengeUserToWargameByNamePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ChallengeUserToWargameByName;

        public string TargetName { get; set; }
        public int TimeMins { get; set; }
        public int MaxKills { get; set; }

        public override void Read(PythonReader pr)
        {
            var count = pr.ReadTuple();

            TargetName = pr.PeekType() == PythonType.UnicodeString ? pr.ReadUnicodeString() : pr.ReadString();

            // communicator.py builds the tuple out of a split of the typed line, so a client that
            // sends fewer than three elements is leaving its own optional arguments out.
            if (count > 1)
                TimeMins = pr.ReadInt();

            if (count > 2)
                MaxKills = pr.ReadInt();
        }
    }
}
