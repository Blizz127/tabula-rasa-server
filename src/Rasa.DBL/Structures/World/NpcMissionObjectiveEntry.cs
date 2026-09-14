using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    /// <summary>
    /// Server-side objective definition for an npc_mission row. The original
    /// server's mission script format is unrecovered; this table only stores the
    /// fields the recovered client protocol requires (objective id, ordinal,
    /// required flag) plus whether the objective is revealed on acceptance.
    /// Ordinal, IsRequired and RevealedOnAccept are server-authoritative with no
    /// surviving source, so they are nullable: an objective seeded from the
    /// client's missionobjective table (original tier) carries NULL there, and a
    /// definition with unknown values stays unoffered (Mission.DefinitionGaps)
    /// instead of asserting guessed gameplay.
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
        public uint? Ordinal { get; set; }

        [Column("is_required")]
        public bool? IsRequired { get; set; }

        [Column("revealed_on_accept")]
        public bool? RevealedOnAccept { get; set; }

        [Column("comment", TypeName = "varchar(100)")]
        [Required]
        public string Comment { get; set; }
    }
}
