namespace Rasa.Config
{
    public class GameConfig
    {
        public string PublicAddress { get; set; }
        public int Port { get; set; }
        public int Backlog { get; set; }

        /// <summary>
        /// How often the world loop's metrics go out to GM clients, in milliseconds. Zero turns
        /// it off. Nothing else reads them, so off costs nothing but the readout.
        /// </summary>
        public int PerformanceMetricsInterval { get; set; }
    }
}
