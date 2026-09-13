using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    /// <summary>
    /// Objective revealed when another objective of the same mission completes.
    /// </summary>
    [Table(TableName)]
    public class NpcMissionObjectiveTransitionEntry
    {
        public const string TableName = "npc_mission_objective_transition";

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        [Column("completed_objective_id")]
        [Required]
        public uint CompletedObjectiveId { get; set; }

        [Column("revealed_objective_id")]
        [Required]
        public uint RevealedObjectiveId { get; set; }
    }
}
