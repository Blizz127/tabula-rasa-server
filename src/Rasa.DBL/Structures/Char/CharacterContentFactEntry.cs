using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    /// <summary>
    /// Per-character world state kept by reconstructed content (for example a destroyed dropship).
    /// Abandoning a mission never deletes facts.
    /// </summary>
    [Table(TableName)]
    public class CharacterContentFactEntry
    {
        public const string TableName = "character_content_fact";

        [Column("character_id")]
        [Required]
        public uint CharacterId { get; set; }

        [Column("map_context_id")]
        [Required]
        public uint MapContextId { get; set; }

        [Column("fact_key", TypeName = "varchar(64)")]
        [Required]
        public string FactKey { get; set; } = string.Empty;

        [Column("value")]
        [Required]
        public int Value { get; set; }

        [Column("change_time")]
        [Required]
        public uint ChangeTime { get; set; }
    }
}
