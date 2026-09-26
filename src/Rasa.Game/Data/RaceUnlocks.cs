using System.Collections.Generic;
using System.Linq;

namespace Rasa.Data
{
    /// <summary>Client finishing-text joins and dated live evidence: docs/evidence/race-unlocks.json.</summary>
    public static class RaceUnlocks
    {
        public static Race? GrantedByMission(uint missionId) => missionId switch
        {
            1861 => Race.Forean, // Traitor on the Run
            1851 => Race.Brann,  // Remedy (client DNA text + contemporary completion walkthrough)
            1899 => Race.Thrax,  // Genome Sweet Genome
            _ => null
        };

        public static IEnumerable<Race> EnabledRaces(IEnumerable<byte> unlocked)
            => new[] { Race.Human }.Concat(unlocked.Where(r => r >= (byte)Race.Forean && r <= (byte)Race.Thrax)
                .Select(r => (Race)r).Distinct().OrderBy(r => r));

        public static bool IsEnabled(Race race, IEnumerable<byte> unlocked)
            => race == Race.Human || (race >= Race.Forean && race <= Race.Thrax && unlocked.Contains((byte)race));
    }
}
