using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// client/augmentations/actor.py Actor.Recv_WargameData(wargameData), opcode 696. This one is
    /// not on the wargame manager (entity 23) but on the actor's own entity, because it is a
    /// property of a body in the world rather than of a match.
    ///
    /// wargameData is a dict {wargameId: sideToken}. The handler stores it and posts
    /// UI_UPDATE_ACTOR_WARGAME_DATA; Actor.IsWargaming() is just bool(dict), and
    /// Actor.GetWargameParticipantStatus(actorId) intersects two actors' key sets, pops a shared
    /// id and returns (isParticipating, isAllied = myToken == hisToken).
    ///
    /// The tokens come from shared/teamdefs.py, which declares
    /// <c>TeamDef('WargameTeamIds', (Field('teamShirt'), Field('teamSkin')))</c>; shared/stuple.py
    /// Struct numbers a struct's fields with enumerate, so teamShirt is 0 and teamSkin is 1. That
    /// is why actor.py calls the value amIShirt. The client only ever compares two tokens for
    /// equality, so a duel gives the challenger 0 and the challenged 1.
    ///
    /// Without this packet none of the client's own PvP rules engage at all: IsWargaming() stays
    /// False, client/actions/targetedaction.py never applies its wargame targeting rule, and
    /// client/ui/overheadwindow.py never raises the wargame overhead icon. An empty dict is how a
    /// duel is taken off an actor again.
    /// </summary>
    public class WargameDataPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WargameData;

        /// <summary>wargameId -> side token (0 = teamShirt, 1 = teamSkin).</summary>
        public IReadOnlyDictionary<uint, int> WargameData { get; set; }

        public WargameDataPacket(IReadOnlyDictionary<uint, int> wargameData)
        {
            WargameData = wargameData ?? new Dictionary<uint, int>();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(WargameData.Count);

            foreach (var entry in WargameData)
            {
                pw.WriteUInt(entry.Key);
                pw.WriteInt(entry.Value);
            }
        }
    }
}
