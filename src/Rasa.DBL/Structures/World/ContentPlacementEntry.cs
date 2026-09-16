using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.World
{
    /// <summary>
    /// A fixed creature or usable object placed by reconstructed content. kind: 1 creature, 2 usable.
    /// usable_kind: 1 container, 2 destroyable, 3 bomb, 4 generic use, 5 structure. behavior: 1 stationary,
    /// 2 creature AI. Zero in an id column means "none" unless the column comment says otherwise.
    /// </summary>
    [Table(TableName)]
    public class ContentPlacementEntry
    {
        public const string TableName = "content_placement";

        [Key]
        // Reserved explicit keys (evidence manifest scopes); never generated.
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("id")]
        [Required]
        public uint Id { get; set; }

        [Column("map_context_id")]
        [Required]
        public uint MapContextId { get; set; }

        [Column("kind")]
        [Required]
        public byte Kind { get; set; }

        [Column("creature_id")]
        [Required]
        public uint CreatureId { get; set; }

        [Column("npc_package_id")]
        [Required]
        public uint NpcPackageId { get; set; }

        [Column("entity_class_id")]
        [Required]
        public uint EntityClassId { get; set; }

        [Column("usable_kind")]
        [Required]
        public byte UsableKind { get; set; }

        [Column("pos_x")]
        [Required]
        public double PosX { get; set; }

        [Column("pos_y")]
        [Required]
        public double PosY { get; set; }

        [Column("pos_z")]
        [Required]
        public double PosZ { get; set; }

        [Column("rotation")]
        [Required]
        public double Rotation { get; set; }

        [Column("behavior")]
        [Required]
        public byte Behavior { get; set; }

        /// <summary>
        /// For a placement whose behavior is Escort: the mission the creature walks with the player on. 0 means the
        /// placement is not an escort.
        /// </summary>
        [Column("escort_mission_id")]
        public uint EscortMissionId { get; set; }

        [Column("initial_state")]
        [Required]
        public uint InitialState { get; set; }

        [Column("alternate_state")]
        [Required]
        public uint AlternateState { get; set; }

        [Column("alternate_state_condition_id")]
        [Required]
        public uint AlternateStateConditionId { get; set; }

        [Column("windup_ms")]
        [Required]
        public uint WindupMs { get; set; }

        [Column("name_override_id")]
        [Required]
        public uint NameOverrideId { get; set; }

        [Column("hit_points")]
        [Required]
        public uint HitPoints { get; set; }

        [Column("restore_ms")]
        [Required]
        public uint RestoreMs { get; set; }

        [Column("fuse_ms")]
        [Required]
        public uint FuseMs { get; set; }

        [Column("loot_item_set_id")]
        [Required]
        public uint LootItemSetId { get; set; }

        [Column("respawn_ms")]
        [Required]
        public uint RespawnMs { get; set; }

        [Column("present_condition_id")]
        [Required]
        public uint PresentConditionId { get; set; }

        [Column("usable_condition_id")]
        [Required]
        public uint UsableConditionId { get; set; }

        [Column("comment", TypeName = "varchar(50)")]
        [Required]
        public string Comment { get; set; } = string.Empty;
    }
}
