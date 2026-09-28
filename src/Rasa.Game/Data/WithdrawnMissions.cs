using System.Collections.Generic;

namespace Rasa.Data
{
    /// <summary>
    /// Missions the official live notes withdrew, so a later migration removed their <c>npc_mission</c> row. A
    /// character who accepted one before its withdrawal keeps a <c>character_mission</c> row the server can no
    /// longer define; <see cref="Rasa.Managers.MissionManager.CreateMissionStatusSnapshot"/> cannot send it and
    /// used to log that at <c>Error</c>, once per login, indistinguishable from an actual data defect. What the
    /// original server did with an in-progress instance of withdrawn content is not recorded
    /// (GAP-WITHDRAWN-MISSION-IN-PROGRESS); the server's own choice is to leave it out of the snapshot exactly as
    /// it would for a mission that was never defined, but at a log severity that reflects the documented cause.
    /// <list type="bullet">
    /// <item>767 Mighty Miasma (Dr. Munson, Wilderness): withdrawn per the official Deployment 11 live notes.
    /// <see cref="Rasa.Migrations.WildernessData.WildernessMunsonWithdrawalRows"/>.</item>
    /// </list>
    /// </summary>
    public static class WithdrawnMissions
    {
        private static readonly HashSet<uint> Ids = new() { 767u };

        public static bool IsWithdrawn(uint missionId) => Ids.Contains(missionId);
    }
}
