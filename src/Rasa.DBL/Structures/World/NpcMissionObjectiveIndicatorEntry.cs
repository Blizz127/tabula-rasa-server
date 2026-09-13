using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    /// <summary>
    /// Objective marker sent in the client indicator list (position, radius, indicatorId, bShow3DEffect).
    /// </summary>
    [Table(TableName)]
    public class NpcMissionObjectiveIndicatorEntry
    {
        public const string TableName = "npc_mission_objective_indicator";

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        [Column("objective_id")]
        [Required]
        public uint ObjectiveId { get; set; }

        [Column("indicator_index")]
        [Required]
        public byte IndicatorIndex { get; set; }

        [Column("indicator_id")]
        [Required]
        public uint IndicatorId { get; set; }

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

        [Column("show_3d")]
        [Required]
        public bool Show3d { get; set; }
    }
}
