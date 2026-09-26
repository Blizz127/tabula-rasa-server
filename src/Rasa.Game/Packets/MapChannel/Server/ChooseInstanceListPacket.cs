using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// clientmethod.Recv_ChooseInstanceList(instances) (client 1.16.5.0, clientmethod.py line 488, method id 685):
    /// "display an instance list the user can choose from to go to a shared map". The waypoint window unpacks each
    /// entry as (ordinal, instanceId, mapTemplateId, startGroup, overloadedStatus) (waypointwindow.ShowInstances
    /// line 470): the name is the template's context name plus "(ordinal)" when the ordinal is not None, instanceId
    /// comes back in SelectInstance, startGroup is echoed back with it, and the status picks the LOW/MEDIUM/HIGH/FULL
    /// label (shared/gameconstants.pyo POPULATION_* 1-4; _SetupTreeView line 920).
    /// </summary>
    public class ChooseInstanceListPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ChooseInstanceList;

        public sealed class Entry
        {
            public uint Ordinal { get; }
            public uint MapInstanceId { get; }
            public uint MapContextId { get; }
            public MapInstanceStatus Status { get; }

            public Entry(uint ordinal, uint mapInstanceId, uint mapContextId, MapInstanceStatus status)
            {
                Ordinal = ordinal;
                MapInstanceId = mapInstanceId;
                MapContextId = mapContextId;
                Status = status;
            }
        }

        public List<Entry> Instances { get; }

        public ChooseInstanceListPacket(List<Entry> instances)
        {
            Instances = instances;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(Instances.Count);

            foreach (var entry in Instances)
            {
                pw.WriteTuple(5);
                pw.WriteUInt(entry.Ordinal);
                pw.WriteUInt(entry.MapInstanceId);
                pw.WriteUInt(ClientMapTemplates.For(entry.MapContextId));
                // No start groups are recovered for any map; the client only echoes this back.
                pw.WriteNoneStruct();
                pw.WriteUInt((uint)entry.Status);
            }
        }
    }
}
