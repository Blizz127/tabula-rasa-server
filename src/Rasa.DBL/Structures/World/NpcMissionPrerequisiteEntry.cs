using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    /// <summary>
    /// A mission may be offered when every row of any one or_group is satisfied. required_state
    /// uses the client missionstate values (4 completed, 2 failed, 3 not assigned = no row).
    /// </summary>
    [Table(TableName)]
    public class NpcMissionPrerequisiteEntry
    {
        public const string TableName = "npc_mission_prerequisite";

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        [Column("or_group")]
        [Required]
        public byte OrGroup { get; set; }

        [Column("required_mission_id")]
        [Required]
        public uint RequiredMissionId { get; set; }

        [Column("required_state")]
        [Required]
        public byte RequiredState { get; set; }

        [Column("comment", TypeName = "varchar(50)")]
        [Required]
        public string Comment { get; set; } = string.Empty;
    }
}
