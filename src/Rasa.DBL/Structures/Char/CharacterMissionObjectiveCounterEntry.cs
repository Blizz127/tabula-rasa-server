using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    /// <summary>
    /// Current value of one objective counter for a character.
    /// </summary>
    [Table(TableName)]
    public class CharacterMissionObjectiveCounterEntry
    {
        public const string TableName = "character_mission_objective_counter";

        [Column("character_id")]
        [Required]
        public uint CharacterId { get; set; }

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        [Column("objective_id")]
        [Required]
        public uint ObjectiveId { get; set; }

        [Column("counter_id")]
        [Required]
        public byte CounterId { get; set; }

        [Column("value")]
        [Required]
        public int Value { get; set; }
    }
}
