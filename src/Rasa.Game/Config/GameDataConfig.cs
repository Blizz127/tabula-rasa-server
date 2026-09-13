namespace Rasa.Config
{
    using Data;

    public class GameDataConfig
    {
        public int[] EnabledRaces { get; set; }
        public BootcampConfig Bootcamp { get; set; } = new BootcampConfig();
    }

    /// <summary>
    /// Operational switch for sending new characters into the reconstructed boot camp. It is a
    /// rollback lever, not gameplay: with Disabled every new character starts in the Wilderness.
    /// </summary>
    public class BootcampConfig
    {
        public BootcampEntryMode EntryMode { get; set; } = BootcampEntryMode.Disabled;
        public uint[] AccountIds { get; set; } = new uint[0];
    }
}
