using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    /// <summary>
    /// Time limit of an objective, started when the objective is revealed. on_expire: 1 fail the
    /// objective, 2 fail the objective and the mission.
    /// </summary>
    [Table(TableName)]
    public class NpcMissionObjectiveTimerEntry
    {
        public const string TableName = "npc_mission_objective_timer";

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        [Column("objective_id")]
        [Required]
        public uint ObjectiveId { get; set; }

        [Column("limit_seconds")]
        [Required]
        public uint LimitSeconds { get; set; }

        [Column("on_expire")]
        [Required]
        public byte OnExpire { get; set; }
    }
}
