using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    /// <summary>
    /// One term of a condition. A condition holds when every term of any one or_group holds. kind:
    /// 1 mission_state_is, 2 objective_state_is, 3 mission_absent, 4 fact_equals, 5 has_logos.
    /// </summary>
    [Table(TableName)]
    public class ContentConditionEntry
    {
        public const string TableName = "content_condition";

        [Column("condition_id")]
        [Required]
        public uint ConditionId { get; set; }

        [Column("or_group")]
        [Required]
        public byte OrGroup { get; set; }

        [Column("term_index")]
        [Required]
        public byte TermIndex { get; set; }

        [Column("kind")]
        [Required]
        public byte Kind { get; set; }

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        [Column("objective_id")]
        [Required]
        public uint ObjectiveId { get; set; }

        [Column("state")]
        [Required]
        public uint State { get; set; }

        [Column("fact_key", TypeName = "varchar(64)")]
        [Required]
        public string FactKey { get; set; } = string.Empty;

        [Column("value")]
        [Required]
        public int Value { get; set; }

        [Column("negate")]
        [Required]
        public bool Negate { get; set; }
    }
}
