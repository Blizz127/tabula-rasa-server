namespace Rasa.Structures
{
    /// <summary>
    /// Original tooltipwindow._AddModuleEffects unpacks these nine values per row.
    /// Coefficients may be fractional; null arguments remain Python None rather than zero.
    /// This record supplies no module values or combat behavior by itself.
    /// </summary>
    public sealed class ModuleInfo
    {
        public int EffectId { get; }
        public int SetLevel { get; }
        public double FlatValue { get; }
        public double LinearValue { get; }
        public double ExpValue { get; }
        public int? Arg1 { get; }
        public int? Arg2 { get; }
        public int? Arg3 { get; }
        public int? Arg4 { get; }

        public ModuleInfo(int effectId, int setLevel, double flatValue, double linearValue,
            double expValue, int? arg1, int? arg2, int? arg3, int? arg4)
        {
            EffectId = effectId;
            SetLevel = setLevel;
            FlatValue = flatValue;
            LinearValue = linearValue;
            ExpValue = expValue;
            Arg1 = arg1;
            Arg2 = arg2;
            Arg3 = arg3;
            Arg4 = arg4;
        }
    }
}
