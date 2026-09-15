using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Test.Reconstruction
{
    public enum ColumnRole
    {
        Unknown,

        /// <summary>Primary-key column; identity, not an evidence field.</summary>
        Key,

        /// <summary>Storage-only column (scope, comment); listed in <c>storage_fields</c> with a role.</summary>
        Storage,

        /// <summary>
        /// The row cannot do its job without a value: no neutral default exists, or the
        /// default disables the behaviour the row exists for. Columns that are needed only
        /// for some kinds or actions (binding kind, rule action, usable kind) are required.
        /// Only these columns may hold an <c>analogue</c>.
        /// </summary>
        Required,

        /// <summary>
        /// Build plan section 1.3 gives a neutral default (none, any, always, never) and the
        /// content still functions with it. Leave such a value out and record the omission;
        /// it may never be an <c>analogue</c> (AGENTS.md: prefer leaving an optional value out).
        /// </summary>
        Optional
    }

    public sealed class TableProvenance
    {
        public TableProvenance(string table, string[] keys, string[] required, string[] optional, string[] storage)
        {
            Table = table;
            KeyColumns = keys;
            RequiredColumns = required;
            OptionalColumns = optional;
            StorageColumns = storage;
        }

        public string Table { get; }
        public IReadOnlyList<string> KeyColumns { get; }
        public IReadOnlyList<string> RequiredColumns { get; }
        public IReadOnlyList<string> OptionalColumns { get; }
        public IReadOnlyList<string> StorageColumns { get; }

        public IEnumerable<string> AllColumns => KeyColumns.Concat(StorageColumns).Concat(RequiredColumns).Concat(OptionalColumns);

        public ColumnRole RoleOf(string column)
        {
            if (KeyColumns.Contains(column)) return ColumnRole.Key;
            if (StorageColumns.Contains(column)) return ColumnRole.Storage;
            if (RequiredColumns.Contains(column)) return ColumnRole.Required;
            if (OptionalColumns.Contains(column)) return ColumnRole.Optional;
            return ColumnRole.Unknown;
        }
    }

    /// <summary>
    /// Per-table column classification for the reconstruction tables of build plan
    /// section 1.3 (the new content tables, including <c>content_map_setting</c>, which replaced the plan's
    /// <c>map_info.instancing</c> column, and the reserved <c>creature</c> rows). The manifest validator uses it for the analogue
    /// rule: an analogue is allowed only in a registered required column. Later
    /// segments extend this registry together with their migrations.
    /// </summary>
    public sealed class ProvenanceRegistry
    {
        private readonly Dictionary<string, TableProvenance> _tables;

        public ProvenanceRegistry(IEnumerable<TableProvenance> tables)
        {
            _tables = tables.ToDictionary(t => t.Table, StringComparer.Ordinal);
        }

        public IReadOnlyCollection<TableProvenance> Tables => _tables.Values;

        public bool TryGet(string table, out TableProvenance provenance) => _tables.TryGetValue(table ?? string.Empty, out provenance);

        public ColumnRole RoleOf(string table, string column)
            => TryGet(table, out var provenance) ? provenance.RoleOf(column) : ColumnRole.Unknown;

        private static string[] Cols(params string[] columns) => columns;

        public static readonly ProvenanceRegistry Default = new ProvenanceRegistry(new[]
        {
            // Existing table.
            new TableProvenance("map_info",
                keys: Cols("map_context_id"),
                required: Cols("map_name", "map_version", "base_region"),
                optional: Cols(),
                storage: Cols()),

            // Replaces the plan's map_info.instancing: the world seed migration reflects map_info's columns.
            new TableProvenance("content_map_setting",
                keys: Cols("map_context_id"),
                required: Cols("instancing"),
                optional: Cols(),
                storage: Cols("comment")),

            // Existing table; reserved boot-camp ids 198500-198599. action1..8: 0 = no action.
            // action1 is required (owner decision OD-23, 2026-09-14): a creature that exists to fight cannot
            // function without an attack, so its first action may carry a labelled analogue. 0 stays the
            // correct, evidenced value for non-combat NPCs. action2..8 remain optional extra attacks.
            new TableProvenance("creature",
                keys: Cols("id"),
                required: Cols("class_id", "faction", "level", "max_hp", "name_id", "run_speed", "walk_speed", "action1"),
                optional: Cols("action2", "action3", "action4", "action5", "action6", "action7", "action8"),
                storage: Cols("comment")),

            new TableProvenance("npc_mission_prerequisite",
                keys: Cols("mission_id", "or_group", "required_mission_id"),
                required: Cols("required_state"),
                optional: Cols(),
                storage: Cols("comment")),

            // placement_id: 0 = any placement of creature_id or class; counter_id: 255 = none.
            new TableProvenance("npc_mission_objective_binding",
                keys: Cols("mission_id", "objective_id", "binding_id"),
                required: Cols("kind", "area_id", "creature_id", "action_id", "destroying_hit_only", "equip_match",
                    "item_template_id", "item_set_id", "target_state"),
                optional: Cols("placement_id", "counter_id"),
                storage: Cols("comment")),

            new TableProvenance("npc_mission_objective_counter",
                keys: Cols("mission_id", "objective_id", "counter_id"),
                required: Cols("initial_value", "target_value"),
                optional: Cols(),
                storage: Cols()),

            new TableProvenance("npc_mission_objective_timer",
                keys: Cols("mission_id", "objective_id"),
                required: Cols("limit_seconds", "on_expire"),
                optional: Cols(),
                storage: Cols()),

            // Indicators are optional presentation: omitted until measured (build plan S4, S6).
            new TableProvenance("npc_mission_objective_indicator",
                keys: Cols("mission_id", "objective_id", "indicator_index"),
                required: Cols(),
                optional: Cols("indicator_id", "pos_x", "pos_y", "pos_z", "radius", "show_3d"),
                storage: Cols()),

            new TableProvenance("content_area",
                keys: Cols("id"),
                required: Cols("shape", "pos_x", "pos_y", "pos_z", "radius", "half_height"),
                optional: Cols(),
                storage: Cols("map_context_id", "comment")),

            // npc_package_id: 0 = the creature's npc_package row; restore_ms: 0 = never; fuse_ms: 0 = none;
            // respawn_ms: 0 = only on instance rebuild; present/usable_condition_id: 0 = always.
            // hit_points (0 = not damageable) is required: a destroyable placement does nothing without it.
            new TableProvenance("content_placement",
                keys: Cols("id"),
                required: Cols("kind", "creature_id", "entity_class_id", "usable_kind", "pos_x", "pos_y", "pos_z",
                    "rotation", "behavior", "initial_state", "hit_points", "loot_item_set_id"),
                optional: Cols("npc_package_id", "alternate_state", "alternate_state_condition_id", "windup_ms",
                    "name_override_id", "restore_ms", "fuse_ms", "respawn_ms", "present_condition_id", "usable_condition_id"),
                storage: Cols("map_context_id", "comment")),

            new TableProvenance("content_condition",
                keys: Cols("condition_id", "or_group", "term_index"),
                required: Cols("kind", "mission_id", "objective_id", "state", "fact_key", "value", "negate"),
                optional: Cols(),
                storage: Cols()),

            // Filters mission_id, objective_id, area_id, placement_id, state_id: 0 = any; condition_id: 0 = always.
            new TableProvenance("content_rule",
                keys: Cols("id"),
                required: Cols("event"),
                optional: Cols("mission_id", "objective_id", "area_id", "placement_id", "state_id", "condition_id"),
                storage: Cols("map_context_id", "comment")),

            // Typed action columns; the loader enforces required and forbidden columns per action.
            new TableProvenance("content_rule_action",
                keys: Cols("rule_id", "sequence"),
                required: Cols("action", "mission_id", "forced", "greeting_id", "npc_name_id", "tutorial_id", "logos_id",
                    "logos_protocol", "experience", "credits", "item_set_id", "placement_id", "state_id", "fact_key",
                    "fact_value", "location_id", "audio_set_id"),
                optional: Cols("damage"),
                storage: Cols("comment")),

            new TableProvenance("content_item_set",
                keys: Cols("item_set_id", "item_template_id"),
                required: Cols("quantity"),
                optional: Cols(),
                storage: Cols()),

            // map_context_id here is the destination context, not a scope column.
            new TableProvenance("content_location",
                keys: Cols("id"),
                required: Cols("purpose", "map_context_id", "pos_x", "pos_y", "pos_z", "rotation"),
                optional: Cols(),
                storage: Cols("comment")),

            // Existing world-seed table, keyed by the client item template id (WildernessArrivalTrainingDay adds the Training
            // Day reward pistols). quality_id and inventory_category decide the reward tuple and inventory placement
            // (BuildRewardInfo). The seven flags are sent in every ItemInfo and drive trade, vendor, lockbox and binding
            // rules: either value asserts gameplay and none is neutral, so each is required. buy_price/sell_price are read
            // only by vendors (none stocks these templates), buy-back and repair; 0 (no price) is their neutral default,
            // recorded with a gap where it applies (GAP-W1-ITEM-PRICES).
            new TableProvenance("itemtemplate",
                keys: Cols("id"),
                required: Cols("quality_id", "has_sellable_flag", "not_tradable_flag", "has_character_unique_flag", "has_account_unique_flag",
                    "has_boe_flag", "bound_to_character_flag", "not_placable_in_lockbox_flag", "inventory_category"),
                optional: Cols("buy_price", "sell_price"),
                storage: Cols()),

            // Existing world-seed table, keyed by the client item template id. A template without this row has no WeaponInfo
            // and the tooltip and WeaponInfo packets dereference it, so every column those packets send is required.
            // reuse_override is loaded but never sent (WeaponInfoPacket writes None): 0 = no override, optional.
            new TableProvenance("itemtemplate_weapon",
                keys: Cols("id"),
                required: Cols("aim_rate", "reload_time", "alt_action_id", "alt_action_arg_id", "ae_type", "ae_radius", "recoil_amount",
                    "cool_rate", "heat_per_shot", "tool_type", "ammo_per_shot", "windup", "recovery", "refire", "range",
                    "alt_max_damage", "alt_damage_type", "alt_range", "alt_ae_radius", "alt_ae_type", "attack_type"),
                optional: Cols("reuse_override"),
                storage: Cols())
        });
    }
}
