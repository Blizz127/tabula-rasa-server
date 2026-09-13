using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    /// <summary>
    /// Reaction to a content event, filtered by the non-zero filter columns and an optional condition.
    /// </summary>
    [Table(TableName)]
    public class ContentRuleEntry
    {
        public const string TableName = "content_rule";

        [Key]
        // Reserved explicit keys (evidence manifest scopes); never generated.
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("map_context_id")]
        [Required]
        public uint MapContextId { get; set; }

        [Column("event")]
        [Required]
        public byte Event { get; set; }

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        [Column("objective_id")]
        [Required]
        public uint ObjectiveId { get; set; }

        [Column("area_id")]
        [Required]
        public uint AreaId { get; set; }

        [Column("placement_id")]
        [Required]
        public uint PlacementId { get; set; }

        // placement_state_entered only: the state entered (0 = any).
        [Column("state_id")]
        [Required]
        public uint StateId { get; set; }

        [Column("condition_id")]
        [Required]
        public uint ConditionId { get; set; }

        [Column("comment", TypeName = "varchar(50)")]
        [Required]
        public string Comment { get; set; } = string.Empty;
    }
}
