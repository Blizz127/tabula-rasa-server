using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    /// <summary>
    /// Server-side objective definition for an npc_mission row. The original
    /// server's mission script format is unrecovered; this table only stores the
    /// fields the recovered client protocol requires (objective id, ordinal,
    /// required flag) plus whether the objective is revealed on acceptance.
    /// </summary>
    [Table(TableName)]
    public class NpcMissionObjectiveEntry
    {
        public const string TableName = "npc_mission_objective";

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        [Column("objective_id")]
        [Required]
        public uint ObjectiveId { get; set; }

        [Column("ordinal")]
        [Required]
        public uint Ordinal { get; set; }

        [Column("is_required")]
        [Required]
        public bool IsRequired { get; set; }

        [Column("revealed_on_accept")]
        [Required]
        public bool RevealedOnAccept { get; set; }

        [Column("comment", TypeName = "varchar(50)")]
        [Required]
        public string Comment { get; set; }
    }
}
