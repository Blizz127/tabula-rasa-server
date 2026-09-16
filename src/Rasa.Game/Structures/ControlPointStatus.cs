namespace Rasa.Structures
{
    using Data;
    using Memory;

    /// <summary>
    /// shared/controlpointdefs.py (lines 10-11): Struct('ControlPointStatus', (controlPointId IntType, ownerId LongType
    /// nullable, stateId IntType, endTime IntType)). The client's control-point manager unwraps each entry of the
    /// ControlPointStatus list with stuple.Wrap.FromNetworkFormat('ControlPointStatus', ...) and keys it by
    /// controlPointId (client/controlpointmanager.py Recv_ControlPointStatus, line 72).
    /// </summary>
    public class ControlPointStatus : IPythonDataStruct
    {
        /// <summary>A controlpointdata key (ControlPointData.Rows).</summary>
        public uint ControlPointId { get; set; }

        /// <summary>
        /// Who holds the point, in the ownership type's own id space (ControlPointOwner): a team id for the live rows.
        /// Nullable on the wire (Field 'ownerId', types.LongType, None, True), and None is what an unheld point sends.
        /// </summary>
        public long? OwnerId { get; set; }

        public ControlPointState StateId { get; set; }

        /// <summary>The time the current state ends, in the client's clock units (compared with gameclient.Time()).</summary>
        public uint EndTime { get; set; }

        public ControlPointStatus()
        {
        }

        public ControlPointStatus(uint controlPointId, long? ownerId, ControlPointState stateId, uint endTime)
        {
            ControlPointId = controlPointId;
            OwnerId = ownerId;
            StateId = stateId;
            EndTime = endTime;
        }

        public void Read(PythonReader pr)
        {
            pr.ReadTuple();
            ControlPointId = pr.ReadUInt();

            // ownerId is None or a long; look at the tag before choosing the reader.
            var stream = pr.Reader.BaseStream;
            var position = stream.Position;
            if (pr.Reader.ReadByte() == 0x00)
                OwnerId = null;
            else
            {
                stream.Position = position;
                OwnerId = pr.ReadLong();
            }

            StateId = (ControlPointState)pr.ReadUInt();
            EndTime = pr.ReadUInt();
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(4);
            pw.WriteUInt(ControlPointId);
            if (OwnerId.HasValue)
                pw.WriteLong(OwnerId.Value);
            else
                pw.WriteNoneStruct();
            pw.WriteUInt((uint)StateId);
            pw.WriteUInt(EndTime);
        }
    }
}
