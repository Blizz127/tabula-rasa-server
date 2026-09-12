namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class UpdateArmorPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UpdateArmor;

        public ActorAttributes Armor { get; }
        public ulong WhoId { get; set; }

        public UpdateArmorPacket(ActorAttributes armor, ulong whoId)
        {
            // The queued update must survive later damage, death or regeneration.
            Armor = new ActorAttributes(armor.AttributeId, armor.NormalMax, armor.CurrentMax,
                armor.Current, armor.RefreshAmount, armor.RefreshPeriod);
            WhoId = whoId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(4);
            pw.WriteInt(Armor.Current);
            pw.WriteInt(Armor.CurrentMax);
            pw.WriteInt(Armor.RefreshAmount);
            pw.WriteULong(WhoId);
        }
    }
}
