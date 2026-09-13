using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    /// <summary>
    /// Items granted together (container loot or rule grants).
    /// </summary>
    [Table(TableName)]
    public class ContentItemSetEntry
    {
        public const string TableName = "content_item_set";

        [Column("item_set_id")]
        [Required]
        public uint ItemSetId { get; set; }

        [Column("item_template_id")]
        [Required]
        public uint ItemTemplateId { get; set; }

        [Column("quantity")]
        [Required]
        public uint Quantity { get; set; }
    }
}
