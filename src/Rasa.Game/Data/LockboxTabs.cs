namespace Rasa.Data
{
    // Original 1.16.5.0 generated/client/lockboxtabdata.pyo.
    // Provenance and consumer rules: docs/lockbox-tab-client-evidence.md.
    public static class LockboxTabs
    {
        public static bool ContainsSlot(int unlockedTabs, uint slot)
            => unlockedTabs >= 1 && unlockedTabs <= 5 && slot < unlockedTabs * 96;

        public static int? PurchasePrice(int tabId) => tabId switch
        {
            2 => 100000,
            3 => 1000000,
            4 => 10000000,
            5 => 100000000,
            _ => null
        };
    }
}
