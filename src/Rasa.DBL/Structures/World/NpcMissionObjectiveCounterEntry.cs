using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    /// <summary>
    /// Generic objective counter shown by the client as (count, initial, target).
    /// </summary>
    [Table(TableName)]
    public class NpcMissionObjectiveCounterEntry
    {
        public const string TableName = "npc_mission_objective_counter";

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        [Column("objective_id")]
        [Required]
        public uint ObjectiveId { get; set; }

        [Column("counter_id")]
        [Required]
        public byte CounterId { get; set; }

        [Column("initial_value")]
        [Required]
        public int InitialValue { get; set; }

        [Column("target_value")]
        [Required]
        public int TargetValue { get; set; }
    }
}
