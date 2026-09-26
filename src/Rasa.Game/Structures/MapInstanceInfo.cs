namespace Rasa.Structures
{
    using Data;
    using Memory;

    /// <summary>
    /// One map instance row of the waypoint window: clientmethod.Recv_EnteredWaypoint and waypointwindow.ShowWaypoints
    /// (line 265) unpack it as (ordinal, mapId, overloadedStatus). The ordinal is shown after the map name ("Name(2)")
    /// unless it is None; mapId comes back in SelectWaypoint(mapInstanceId, waypointId) and is compared with the
    /// packet's currentMapId to mark the "(Current)" row; the status picks the population label.
    /// </summary>
    public class MapInstanceInfo : IPythonDataStruct
    {
        internal uint Ordinal { get; set; }
        internal uint MapId { get; set; }
        internal MapInstanceStatus MapInstanceStatus { get; set; }

        public MapInstanceInfo(uint ordinal, uint mapId, MapInstanceStatus mapInstanceStatus)
        {
            Ordinal = ordinal;
            MapId = mapId;
            MapInstanceStatus = mapInstanceStatus;
        }

        public void Read(PythonReader pr)
        {

        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteUInt(Ordinal);
            pw.WriteUInt(MapId);
            pw.WriteUInt((uint)MapInstanceStatus);
        }
    }
}
