using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    /// <summary>
    /// A trigger volume in world coordinates (Y up). shape: 1 sphere, 2 vertical cylinder.
    /// </summary>
    [Table(TableName)]
    public class ContentAreaEntry
    {
        public const string TableName = "content_area";

        [Key]
        // Reserved explicit keys (evidence manifest scopes); never generated.
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("map_context_id")]
        [Required]
        public uint MapContextId { get; set; }

        [Column("shape")]
        [Required]
        public byte Shape { get; set; }

        [Column("pos_x")]
        [Required]
        public double PosX { get; set; }

        [Column("pos_y")]
        [Required]
        public double PosY { get; set; }

        [Column("pos_z")]
        [Required]
        public double PosZ { get; set; }

        [Column("radius")]
        [Required]
        public double Radius { get; set; }

        [Column("half_height")]
        [Required]
        public double HalfHeight { get; set; }

        [Column("comment", TypeName = "varchar(50)")]
        [Required]
        public string Comment { get; set; } = string.Empty;
    }
}
