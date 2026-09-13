namespace Rasa.Config
{
    public class GameDataConfig
    {
        public int[] EnabledRaces { get; set; }

        /// <summary>
        /// Server flags that are on from startup, by name or by number. The client's own
        /// spelling works: "MINION_COMMANDS" as well as "MinionCommands".
        /// </summary>
        public string[] ServerFlags { get; set; }
    }
}
