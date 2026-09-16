using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    using Interfaces;

    /// <summary>
    /// What a creature type drops when it dies, in the shape the original server used.
    ///
    /// The original <c>creature_type_loot</c> table is (id, creatureTypeId, itemTemplateId, chance,
    /// stacksizeMin, stacksizeMax), and its <c>creature_type</c> table is exactly what this world's
    /// <c>creature</c> table already holds (name, name_id, class_id, faction, speeds, hit points, actions) - so a
    /// creature row *is* a creature type and the loot hangs off its id. <c>chance</c> is the percentage its float
    /// column held, and the stack size is drawn between the two bounds.
    ///
    /// Only one creature type's loot survived (the raiding trainee Thrax footsoldier), which is why the table is
    /// small: every other row was in the lost server data. Creatures with no rows here still use the emulator's
    /// stand-in drop rather than paying out nothing (GAP-CREATURE-LOOT).
    /// </summary>
    [Table(TableName)]
    public class CreatureLootEntry : IHasId
    {
        public const string TableName = "creature_loot";

        [Key]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        /// <summary>The world's creature row - the original server's creature type.</summary>
        [Column("creature_id")]
        [Required]
        public uint CreatureId { get; set; }

        [Column("item_template_id")]
        [Required]
        public uint ItemTemplateId { get; set; }

        /// <summary>The percentage the original's float column held: 12 means a 12% chance per kill.</summary>
        [Column("chance")]
        [Required]
        public double Chance { get; set; }

        [Column("stacksize_min")]
        [Required]
        public uint StacksizeMin { get; set; }

        [Column("stacksize_max")]
        [Required]
        public uint StacksizeMax { get; set; }

        [Column("comment", TypeName = "varchar(96)")]
        [Required]
        public string Comment { get; set; }
    }
}
