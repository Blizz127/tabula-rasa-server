namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class WeaponReloadRecoveryPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PerformRecovery;
        public uint ArgumentId { get; }
        public uint AmmoCount { get; }

        public WeaponReloadRecoveryPacket(uint argumentId, uint ammoCount)
        {
            ArgumentId = argumentId;
            AmmoCount = ammoCount;
        }

        public override void Write(PythonWriter pw)
        {
            // WeaponReload.DoAction requires newAmmoCount. Zero is not None.
            pw.WriteTuple(3);
            pw.WriteUInt((uint)ActionId.WeaponReload);
            pw.WriteUInt(ArgumentId);
            pw.WriteUInt(AmmoCount);
        }
    }
}
