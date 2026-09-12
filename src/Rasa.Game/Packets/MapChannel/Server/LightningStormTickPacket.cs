using System;
using System.Collections.Generic;
using System.Linq;
using Rasa.Data;
using Rasa.Memory;
using Rasa.Structures;

namespace Rasa.Packets.MapChannel.Server
{
    // Recv_GameEffectTick(effectId, *args) -> StormEffect.OnTick(target, dotData, arcData).
    public sealed class LightningStormTickPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.GameEffectTick;
        public int EffectId { get; }
        public IReadOnlyList<LightningArcHit> DotData { get; }
        public IReadOnlyList<LightningArcHit> ArcData { get; }

        public LightningStormTickPacket(int effectId, IReadOnlyList<LightningArcHit> dotData,
            IReadOnlyList<LightningArcHit> arcData)
        {
            if (dotData == null)
                throw new ArgumentNullException(nameof(dotData));
            if (arcData == null)
                throw new ArgumentNullException(nameof(arcData));
            EffectId = effectId;
            DotData = Array.AsReadOnly(dotData.ToArray());
            ArcData = Array.AsReadOnly(arcData.ToArray());
        }

        public override void Write(PythonWriter writer)
        {
            writer.WriteTuple(3);
            writer.WriteInt(EffectId);
            LightningArcHit.WriteList(writer, DotData);
            LightningArcHit.WriteList(writer, ArcData);
        }
    }
}
