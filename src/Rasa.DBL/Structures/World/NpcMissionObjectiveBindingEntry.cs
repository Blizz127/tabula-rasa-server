using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    /// <summary>
    /// A non-conversation completion binding for an objective. Bindings of one objective combine as OR;
    /// kind selects which of the typed columns apply (validated when content is loaded).
    /// </summary>
    [Table(TableName)]
    public class NpcMissionObjectiveBindingEntry
    {
        public const string TableName = "npc_mission_objective_binding";

        [Column("mission_id")]
        [Required]
        public uint MissionId { get; set; }

        [Column("objective_id")]
        [Required]
        public uint ObjectiveId { get; set; }

        [Column("binding_id")]
        [Required]
        public byte BindingId { get; set; }

        [Column("kind")]
        [Required]
        public byte Kind { get; set; }

        [Column("area_id")]
        [Required]
        public uint AreaId { get; set; }

        [Column("placement_id")]
        [Required]
        public uint PlacementId { get; set; }

        [Column("creature_id")]
        [Required]
        public uint CreatureId { get; set; }

        [Column("action_id")]
        [Required]
        public uint ActionId { get; set; }

        [Column("destroying_hit_only")]
        [Required]
        public bool DestroyingHitOnly { get; set; }

        [Column("equip_match")]
        [Required]
        public byte EquipMatch { get; set; }

        [Column("item_template_id")]
        [Required]
        public uint ItemTemplateId { get; set; }

        [Column("item_set_id")]
        [Required]
        public uint ItemSetId { get; set; }

        [Column("target_state")]
        [Required]
        public uint TargetState { get; set; }

        [Column("counter_id")]
        [Required]
        public byte CounterId { get; set; }

        [Column("comment", TypeName = "varchar(50)")]
        [Required]
        public string Comment { get; set; } = string.Empty;
    }
}
