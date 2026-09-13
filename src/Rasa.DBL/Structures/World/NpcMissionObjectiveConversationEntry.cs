using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    /// <summary>
    /// An objective that is completed by talking to an NPC. The key mirrors the
    /// original client's objectiveconversation table
    /// (missionId, objectiveId, npcPackageId, playerFlagId) for completion
    /// conversations, so the server completes exactly the bindings the client
    /// can render.
    /// </summary>
    [Table(TableName)]
    public class NpcMissionObjectiveConversationEntry
    {
        public const string TableName = "npc_mission_objective_conversation";

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        [Column("objective_id")]
        [Required]
        public uint ObjectiveId { get; set; }

        [Column("npc_package_id")]
        [Required]
        public uint NpcPackageId { get; set; }

        [Column("player_flag_id")]
        [Required]
        public uint PlayerFlagId { get; set; }
    }
}
