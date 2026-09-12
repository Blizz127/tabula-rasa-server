using System.Collections.Generic;
using System.Linq;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class AttributeInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AttributeInfo;
        
        public IReadOnlyDictionary<Attributes, ActorAttributes> ActorAttributes { get; }
        
        public AttributeInfoPacket(Dictionary<Attributes, ActorAttributes> actorAttributes)
        {
            ActorAttributes = actorAttributes.ToDictionary(entry => entry.Key, entry =>
                new ActorAttributes(entry.Value.AttributeId, entry.Value.NormalMax, entry.Value.CurrentMax,
                    entry.Value.Current, entry.Value.RefreshAmount, entry.Value.RefreshPeriod));
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDictionary(ActorAttributes.Count);
            foreach (var entry in ActorAttributes)
            {
                var attribute = entry.Value;
                pw.WriteInt((int)attribute.AttributeId);
                pw.WriteTuple(5);
                // Recv_AttributeInfo passes this tuple directly to ActorAttribute;
                // its constructor order differs from that receiver's old docstring.
                pw.WriteInt(attribute.NormalMax);
                pw.WriteInt(attribute.CurrentMax);
                pw.WriteInt(attribute.Current);
                pw.WriteInt(attribute.RefreshAmount);
                pw.WriteInt(attribute.RefreshPeriod);
            }
        }
    }
}
