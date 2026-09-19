using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Managers;

namespace Rasa.Test
{
    /// <summary>
    /// Critical hits are chance (DamageModifiers.RollCrit). Tests that assert exact damage would fail one run in
    /// twenty, so crits are off for the whole assembly; the crit tests turn them on for themselves.
    /// </summary>
    [TestClass]
    public static class TestAssemblySetup
    {
        [AssemblyInitialize]
        public static void Initialize(TestContext context) => DamageModifiers.CritRollOverride = _ => false;
    }
}
