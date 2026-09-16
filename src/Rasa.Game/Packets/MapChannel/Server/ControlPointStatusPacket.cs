using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    /// <summary>
    /// ControlPointStatus (814), sent to the client's control-point manager (SysEntity.ClientControlPointManagerId):
    /// client/controlpointmanager.py Recv_ControlPointStatus(statusList), line 72. The one argument is a list of
    /// ControlPointStatus tuples; the client stores each by its controlPointId and posts
    /// UI_CONTROL_POINT_STATUS_UPDATED once for the whole list.
    ///
    /// The earlier form of this packet wrote one bare struct with no argument tuple, which the client would have
    /// unpacked as four positional arguments to a one-argument handler.
    /// </summary>
    public class ControlPointStatusPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ControlPointStatus;

        public List<ControlPointStatus> StatusList { get; }

        public ControlPointStatusPacket(IEnumerable<ControlPointStatus> statusList)
        {
            StatusList = new List<ControlPointStatus>(statusList);
        }

        public ControlPointStatusPacket(ControlPointStatus status)
            : this(new[] { status })
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(StatusList.Count);
            foreach (var status in StatusList)
                pw.WriteStruct(status);
        }
    }
}
