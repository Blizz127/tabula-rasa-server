namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// The radio and shared mission requests from client/missionlog.pyo, in the shapes the client sends them. They
    /// were decoded but ignored while the server had no radio or sharing support; both are implemented now
    /// (MissionManager.CompleteRadioMission, ShareMission/AssignSharedMission/DeclineSharedMission).
    ///
    /// selectionIdx is None or the 0-based reward choice and rating is None unless the (disabled) mission-rating UI is
    /// on, exactly as in CompleteNPCMissionPacket - neither is a boolean.
    /// </summary>
    public abstract class RadioAndSharedMissionRequestPacket : ClientPythonPacket
    {
        public uint MissionId { get; set; }
        public int? SelectionIdx { get; set; }
        public int? Rating { get; set; }

        /// <summary>Reads (missionId, selectionIdx, rating).</summary>
        protected void ReadMissionOutcome(PythonReader pr)
        {
            pr.ReadTuple();
            MissionId = pr.ReadUInt();
            SelectionIdx = CompleteNPCMissionPacket.ReadOptionalInt(pr);
            Rating = CompleteNPCMissionPacket.ReadOptionalInt(pr);
        }

        /// <summary>Reads (playerId, missionId).</summary>
        protected void ReadShareResponse(PythonReader pr)
        {
            pr.ReadTuple();
            PlayerId = pr.ReadULong();
            MissionId = pr.ReadUInt();
        }

        public ulong PlayerId { get; set; }
    }

    public class CompleteRadioMissionPacket : RadioAndSharedMissionRequestPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CompleteRadioMission;

        public override void Read(PythonReader pr) => ReadMissionOutcome(pr);
    }

    public class RewardRadioMissionPacket : RadioAndSharedMissionRequestPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RewardRadioMission;

        public override void Read(PythonReader pr) => ReadMissionOutcome(pr);
    }

    public class AssignSharedMissionPacket : RadioAndSharedMissionRequestPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AssignSharedMission;

        public override void Read(PythonReader pr) => ReadShareResponse(pr);
    }

    public class DeclineSharedMissionPacket : RadioAndSharedMissionRequestPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.DeclineSharedMission;

        public override void Read(PythonReader pr) => ReadShareResponse(pr);
    }

    public class ShareMissionPacket : RadioAndSharedMissionRequestPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ShareMission;

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            MissionId = pr.ReadUInt();
        }
    }
}
