namespace Rasa.Packets.Party.Server
{
    using System.Collections.Generic;
    using Data;
    using Memory;

    /// <summary>
    /// client/party.py Recv_SquadMemberList(squadMembers, partyExclusiveMap): the (userId, entityId)
    /// pairs of members who are in the world, and whether the recipient's map is squad-exclusive (a squad
    /// instance). The client keeps the flag (party.IsPartyExclusiveMap) to warn before leaving the squad:
    /// party.OnLeaveParty line 811 swaps PM 280 for PM 946 "Are you sure you want to leave your squad? You will
    /// have to leave the current map.", and OnDisbandParty uses PM 945.
    /// </summary>
    public class SquadMemberListPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SquadMemberList;

        internal List<(uint UserId, ulong EntityId)> Members { get; }
        internal bool PartyExclusiveMap { get; }

        internal SquadMemberListPacket(List<(uint UserId, ulong EntityId)> members, bool partyExclusiveMap = false)
        {
            Members = members;
            PartyExclusiveMap = partyExclusiveMap;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteList(Members.Count);

            foreach (var (userId, entityId) in Members)
            {
                pw.WriteTuple(2);
                pw.WriteUInt(userId);
                pw.WriteULong(entityId);
            }

            pw.WriteBool(PartyExclusiveMap);
        }
    }
}
