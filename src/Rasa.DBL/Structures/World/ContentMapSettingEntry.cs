using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    /// <summary>
    /// Per-context runtime setting for reconstructed content. Instancing lives here rather than
    /// on map_info because the original data migration seeds map_info by reflecting over
    /// MapInfoEntry's columns, so a new map_info column would break creating a fresh database.
    /// </summary>
    [Table(TableName)]
    public class ContentMapSettingEntry
    {
        public const string TableName = "content_map_setting";

        [Key]
        // Reserved explicit keys (evidence manifest scopes); never generated.
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("map_context_id")]
        [Required]
        public uint MapContextId { get; set; }

        [Column("instancing")]
        [Required]
        public byte Instancing { get; set; }

        [Column("comment", TypeName = "varchar(50)")]
        [Required]
        public string Comment { get; set; } = string.Empty;
    }
}
