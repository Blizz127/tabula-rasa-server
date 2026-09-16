using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// Puts the recorded experience into the five missions WildernessHubConversationChain seeded with zero.
    ///
    /// That migration wrote each reward's amount into the `credits` column only for credit rewards, leaving the
    /// experience rows at 0 - and npc_mission_reward carries *both* amounts in that one column (an experience reward
    /// of 2,500 is the row (2500, type 3, credits 2500)), as the Training Day rows show. The content loader refused
    /// the five missions for it: "Experience reward amount 0 is not positive". The rows class is corrected as well,
    /// so a fresh database never sees the bad values; this migration repairs the world the first one already ran on.
    ///
    /// The amounts are TaRapedia's recorded values for each mission.
    /// </summary>
    public static class WildernessHubConversationChainRewardRows
    {
        public const string Migration = "WildernessHubConversationChainRewards";

        private const uint ExperienceReward = 3u;

        /// <summary>mission id -> the experience its reward row must carry.</summary>
        public static readonly (uint MissionId, int Experience)[] Experiences =
        {
            (431u, 2500),
            (442u, 3500),
            (549u, 3000),
            (836u, 11000)
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var (missionId, experience) in Experiences)
                migrationBuilder.UpdateData(
                    table: "npc_mission_reward",
                    keyColumns: new[] { "id", "type", "item_template_id" },
                    keyValues: new object[] { missionId, ExperienceReward, 0u },
                    column: "credits",
                    value: experience);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var (missionId, _) in Experiences)
                migrationBuilder.UpdateData(
                    table: "npc_mission_reward",
                    keyColumns: new[] { "id", "type", "item_template_id" },
                    keyValues: new object[] { missionId, ExperienceReward, 0u },
                    column: "credits",
                    value: 0);
        }
    }
}
