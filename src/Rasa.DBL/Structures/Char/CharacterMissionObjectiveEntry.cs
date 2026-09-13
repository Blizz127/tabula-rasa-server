using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    /// <summary>
    /// Persisted state of one revealed objective of a character's mission.
    /// Status values follow the original client's missionobjectivestate table.
    /// </summary>
    [Table(TableName)]
    public class CharacterMissionObjectiveEntry
    {
        public const string TableName = "character_mission_objective";

        public CharacterMissionObjectiveEntry()
        {
        }

        public CharacterMissionObjectiveEntry(uint characterId, uint missionId, uint objectiveId, uint status)
        {
            CharacterId = characterId;
            MissionId = missionId;
            ObjectiveId = objectiveId;
            Status = status;
        }

        [Column("character_id")]
        [Required]
        public uint CharacterId { get; set; }

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        [Column("objective_id")]
        [Required]
        public uint ObjectiveId { get; set; }

        [Column("status")]
        [Required]
        public uint Status { get; set; }

        /// <summary>
        /// Milliseconds left on a running objective timer when it was last folded; null when untimed.
        /// </summary>
        [Column("timer_remaining_ms")]
        public long? TimerRemainingMs { get; set; }

        /// <summary>
        /// Unix milliseconds when the running timer was anchored; null while paused or untimed.
        /// </summary>
        [Column("timer_anchor_ms")]
        public long? TimerAnchorMs { get; set; }

        /// <summary>
        /// Set when the timer can no longer expire (for example a bomb has been planted).
        /// </summary>
        [Column("timer_disarmed")]
        [Required]
        public bool TimerDisarmed { get; set; }
    }
}
