using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    [Table(TableName)]
    public class CharacterMissionEntry
    {
        public const string TableName = "character_mission";

        public CharacterMissionEntry()
        {
        }

        public CharacterMissionEntry(uint characterId, uint mission_id, uint mission_state)
        {
            CharacterId = characterId;
            MissionId = mission_id;
            MissionState = mission_state;
        }

        public CharacterMissionEntry(uint characterId, uint missionId, uint missionState, uint changeTime)
            : this(characterId, missionId, missionState)
        {
            ChangeTime = changeTime;
        }

        [Column("character_id")]
        [Required]
        public uint CharacterId { get; set; }

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        /// <summary>
        /// Original client missionstate value (0 active, 1 success, 2 failed, 4 completed).
        /// </summary>
        [Column("mission_state")]
        [Required]
        public uint MissionState { get; set; }

        /// <summary>
        /// Unix seconds of the last state change, sent to the client as changeTime.
        /// </summary>
        [Column("change_time")]
        [Required]
        public uint ChangeTime { get; set; }
    }
}
