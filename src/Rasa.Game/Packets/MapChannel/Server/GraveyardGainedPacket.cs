namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Manifestation.Recv_GraveyardGained(waypointId): posts PM_GAINED_GRAVEYARD
    /// "You just gained %(graveyard)s." with waypointlanguage[waypointId], owner only.
    /// </summary>
    public sealed class GraveyardGainedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.GraveyardGained;

        public uint WaypointId { get; }

        public GraveyardGainedPacket(uint waypointId)
        {
            WaypointId = waypointId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(WaypointId);
        }
    }
}
