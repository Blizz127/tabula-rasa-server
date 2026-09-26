using System;
using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class ModuleTooltipInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ModuleTooltipInfo;

        public int ModuleId { get; }
        public int? ModuleLevel { get; }
        public IReadOnlyList<ModuleInfo> Effects { get; }

        public ModuleTooltipInfoPacket(ItemModule module)
        {
            if (module == null)
                throw new ArgumentNullException(nameof(module));
            ModuleId = module.ModuleId;
            ModuleLevel = module.ModuleLevel;
            Effects = module.Effects;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt(ModuleId);
            WriteOptionalInt(pw, ModuleLevel);
            // Original _AddModuleEffects iterates this collection, then unpacks each row.
            pw.WriteList(Effects.Count);
            foreach (var effect in Effects)
            {
                pw.WriteTuple(9);
                pw.WriteInt(effect.EffectId);
                pw.WriteInt(effect.SetLevel);
                WriteCoefficient(pw, effect.FlatValue);
                WriteCoefficient(pw, effect.LinearValue);
                WriteCoefficient(pw, effect.ExpValue);
                WriteOptionalInt(pw, effect.Arg1);
                WriteOptionalInt(pw, effect.Arg2);
                WriteOptionalInt(pw, effect.Arg3);
                WriteOptionalInt(pw, effect.Arg4);
            }
        }

        private static void WriteOptionalInt(PythonWriter pw, int? value)
        {
            if (value.HasValue)
                pw.WriteInt(value.Value);
            else
                pw.WriteNoneStruct();
        }

        private static void WriteCoefficient(PythonWriter pw, double value)
        {
            if (value >= int.MinValue && value <= int.MaxValue && value == Math.Truncate(value))
                pw.WriteInt((int)value);
            else
                pw.WriteDouble(value);
        }
    }
}
