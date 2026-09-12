namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public sealed class RaceIdPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RaceId;

        private readonly Race _race;

        public RaceIdPacket(Race race)
        {
            _race = race;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt((int)_race);
        }
    }
}
