namespace Rasa.Packets.Mission.Server
{
    using Data;
    using Memory;

    public class UpdateObjectiveCounterPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UpdateObjectiveCounter;

        public uint MissionId { get; set; }
        public uint ObjectiveId { get; set; }
        public uint CounterId { get; set; }
        public int CounterValue { get; set; }
        public int InitialValue { get; set; }
        public int TargetValue { get; set; }

        public UpdateObjectiveCounterPacket(uint missionId, uint objectiveId, uint counterId, int counterValue, int initialValue, int targetValue)
        {
            MissionId = missionId;
            ObjectiveId = objectiveId;
            CounterId = counterId;
            CounterValue = counterValue;
            InitialValue = initialValue;
            TargetValue = targetValue;
        }

        // client/missionlog.pyo Recv_UpdateObjectiveCounter(missionId, objectiveId, counterId,
        // counterVal, initialVal, targetVal)
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(6);
            pw.WriteUInt(MissionId);
            pw.WriteUInt(ObjectiveId);
            pw.WriteUInt(CounterId);
            pw.WriteInt(CounterValue);
            pw.WriteInt(InitialValue);
            pw.WriteInt(TargetValue);
        }
    }
}
