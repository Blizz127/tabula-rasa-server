namespace Rasa.Config
{
    using Data;

    public class GameDataConfig
    {
        public int[] EnabledRaces { get; set; }
        public BootcampConfig Bootcamp { get; set; } = new BootcampConfig();

        /// <summary>
        /// Server flags that are on from startup, by name or by number. The client's own
        /// spelling works: "MINION_COMMANDS" as well as "MinionCommands".
        /// </summary>
        public string[] ServerFlags { get; set; }

        /// <summary>
        /// The JSON file holding the knowledge-base articles SearchKB and RetrieveKBArticle
        /// answer from, relative to the server's working directory. Nothing in the shipped
        /// client asks for one; see KnowledgeBaseManager.
        /// </summary>
        public string KnowledgeBaseFile { get; set; }
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
