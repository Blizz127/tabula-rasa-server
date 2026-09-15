using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    /// <summary>
    /// One ordered action of a rule; action selects which typed columns are required (validated on load).
    /// </summary>
    [Table(TableName)]
    public class ContentRuleActionEntry
    {
        public const string TableName = "content_rule_action";

        [Column("rule_id")]
        [Required]
        public uint RuleId { get; set; }

        [Column("sequence")]
        [Required]
        public byte Sequence { get; set; }

        [Column("action")]
        [Required]
        public byte Action { get; set; }

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        [Column("forced")]
        [Required]
        public bool Forced { get; set; }

        [Column("greeting_id")]
        [Required]
        public uint GreetingId { get; set; }

        [Column("npc_name_id")]
        [Required]
        public uint NpcNameId { get; set; }

        [Column("tutorial_id")]
        [Required]
        public uint TutorialId { get; set; }

        [Column("logos_id")]
        [Required]
        public uint LogosId { get; set; }

        [Column("logos_protocol")]
        [Required]
        public byte LogosProtocol { get; set; }

        [Column("experience")]
        [Required]
        public uint Experience { get; set; }

        [Column("credits")]
        [Required]
        public int Credits { get; set; }

        [Column("item_set_id")]
        [Required]
        public uint ItemSetId { get; set; }

        [Column("placement_id")]
        [Required]
        public uint PlacementId { get; set; }

        [Column("state_id")]
        [Required]
        public uint StateId { get; set; }

        [Column("fact_key", TypeName = "varchar(64)")]
        [Required]
        public string FactKey { get; set; } = string.Empty;

        [Column("fact_value")]
        [Required]
        public int FactValue { get; set; }

        [Column("location_id")]
        [Required]
        public uint LocationId { get; set; }

        [Column("audio_set_id")]
        [Required]
        public uint AudioSetId { get; set; }

        /// <summary>
        /// The amount a damage_player action takes (the boot camp's detonation is 21, read from the footage's
        /// "-21" health line). Zero for every other action.
        /// </summary>
        [Column("damage")]
        [Required]
        public int Damage { get; set; }

        [Column("comment", TypeName = "varchar(50)")]
        [Required]
        public string Comment { get; set; } = string.Empty;
    }
}
