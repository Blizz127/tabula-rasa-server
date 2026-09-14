using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Structures.Content
{
    using Data;
    using Repositories.World.MissionContent;
    using Structures.World;

    /// <summary>
    /// A reason a content row cannot go live. Content with a gap is withheld, never guessed around.
    /// </summary>
    public sealed class ContentGap
    {
        public string Table { get; }
        public string Key { get; }
        public string Message { get; }

        // The row withheld because of this gap: the rule of an action, the condition of a term,
        // the item set of an item row, the mission of an objective row, otherwise the row itself.
        public uint OwnerId { get; }

        public ContentGap(string table, string key, uint ownerId, string message)
        {
            Table = table;
            Key = key;
            OwnerId = ownerId;
            Message = message;
        }

        public override string ToString() => $"{Table} {Key}: {Message}";
    }

    /// <summary>
    /// Lookups into data owned by other managers, so validation stays independent of their singletons.
    /// </summary>
    public interface IContentReferences
    {
        bool MapContextExists(uint mapContextId);
        bool MissionExists(uint missionId);
        bool ObjectiveExists(uint missionId, uint objectiveId);
        uint MissionGiver(uint missionId);
        // The mission definition itself is complete (its own gaps, not counting content rows).
        bool MissionOfferable(uint missionId);
        bool CreatureExists(uint creatureId);
        bool EntityClassExists(uint entityClassId);
        bool ItemTemplateExists(uint itemTemplateId);
        bool LogosExists(uint logosId);
        bool HasLegacyWorldObjects(uint mapContextId);
    }

    /// <summary>
    /// Immutable, indexed view of the reconstructed-content tables.
    /// </summary>
    public sealed class MissionContentCatalog
    {
        public static readonly MissionContentCatalog Empty = new MissionContentCatalog();

        public IReadOnlyDictionary<uint, ContentMapSettingEntry> MapSettings { get; }
        public IReadOnlyList<NpcMissionPrerequisiteEntry> Prerequisites { get; }
        public IReadOnlyList<NpcMissionObjectiveBindingEntry> Bindings { get; }
        public IReadOnlyList<NpcMissionObjectiveCounterEntry> Counters { get; }
        public IReadOnlyList<NpcMissionObjectiveTimerEntry> Timers { get; }
        public IReadOnlyList<NpcMissionObjectiveIndicatorEntry> Indicators { get; }
        public IReadOnlyDictionary<uint, ContentAreaEntry> Areas { get; }
        public IReadOnlyDictionary<uint, ContentPlacementEntry> Placements { get; }
        public IReadOnlyDictionary<uint, IReadOnlyList<ContentConditionEntry>> Conditions { get; }
        public IReadOnlyDictionary<uint, ContentRuleEntry> Rules { get; }
        public IReadOnlyDictionary<uint, IReadOnlyList<ContentRuleActionEntry>> RuleActions { get; }
        public IReadOnlyDictionary<uint, IReadOnlyList<ContentItemSetEntry>> ItemSets { get; }
        public IReadOnlyDictionary<uint, ContentLocationEntry> Locations { get; }

        private readonly List<(string Table, uint Id, string Message)> _keyErrors = new();
        private const string ReservedId = "id 0 means none and cannot be referenced";

        private MissionContentCatalog()
            : this(new List<ContentMapSettingEntry>(), new List<NpcMissionPrerequisiteEntry>(), new List<NpcMissionObjectiveBindingEntry>(),
                new List<NpcMissionObjectiveCounterEntry>(), new List<NpcMissionObjectiveTimerEntry>(), new List<NpcMissionObjectiveIndicatorEntry>(),
                new List<ContentAreaEntry>(), new List<ContentPlacementEntry>(), new List<ContentConditionEntry>(), new List<ContentRuleEntry>(),
                new List<ContentRuleActionEntry>(), new List<ContentItemSetEntry>(), new List<ContentLocationEntry>())
        {
        }

        public MissionContentCatalog(
            IEnumerable<ContentMapSettingEntry> mapSettings,
            IEnumerable<NpcMissionPrerequisiteEntry> prerequisites,
            IEnumerable<NpcMissionObjectiveBindingEntry> bindings,
            IEnumerable<NpcMissionObjectiveCounterEntry> counters,
            IEnumerable<NpcMissionObjectiveTimerEntry> timers,
            IEnumerable<NpcMissionObjectiveIndicatorEntry> indicators,
            IEnumerable<ContentAreaEntry> areas,
            IEnumerable<ContentPlacementEntry> placements,
            IEnumerable<ContentConditionEntry> conditions,
            IEnumerable<ContentRuleEntry> rules,
            IEnumerable<ContentRuleActionEntry> ruleActions,
            IEnumerable<ContentItemSetEntry> itemSets,
            IEnumerable<ContentLocationEntry> locations)
        {
            MapSettings = Index(mapSettings, setting => setting.MapContextId, ContentMapSettingEntry.TableName);
            Prerequisites = prerequisites.ToList();
            Bindings = bindings.OrderBy(b => b.MissionId).ThenBy(b => b.ObjectiveId).ThenBy(b => b.BindingId).ToList();
            Counters = counters.ToList();
            Timers = timers.ToList();
            Indicators = indicators.OrderBy(i => i.MissionId).ThenBy(i => i.ObjectiveId).ThenBy(i => i.IndicatorIndex).ToList();
            Areas = Index(areas, area => area.Id, ContentAreaEntry.TableName);
            Placements = Index(placements, placement => placement.Id, ContentPlacementEntry.TableName);
            Conditions = Group(conditions, term => term.ConditionId, ContentConditionEntry.TableName,
                terms => terms.OrderBy(t => t.OrGroup).ThenBy(t => t.TermIndex));
            Rules = Index(rules, rule => rule.Id, ContentRuleEntry.TableName);
            RuleActions = ruleActions
                .GroupBy(action => action.RuleId)
                .ToDictionary(group => group.Key, group => (IReadOnlyList<ContentRuleActionEntry>)group.OrderBy(a => a.Sequence).ToList());
            ItemSets = Group(itemSets, item => item.ItemSetId, ContentItemSetEntry.TableName, items => items);
            Locations = Index(locations, location => location.Id, ContentLocationEntry.TableName);
        }

        public static MissionContentCatalog Load(IMissionContentRepository repository)
        {
            return new MissionContentCatalog(
                repository.GetMapSettings(), repository.GetPrerequisites(), repository.GetBindings(), repository.GetCounters(),
                repository.GetTimers(), repository.GetIndicators(), repository.GetAreas(), repository.GetPlacements(),
                repository.GetConditions(), repository.GetRules(), repository.GetRuleActions(), repository.GetItemSets(),
                repository.GetLocations());
        }

        public int RowCount =>
            MapSettings.Count + Prerequisites.Count + Bindings.Count + Counters.Count + Timers.Count + Indicators.Count +
            Areas.Count + Placements.Count + Conditions.Values.Sum(c => c.Count) + Rules.Count +
            RuleActions.Values.Sum(a => a.Count) + ItemSets.Values.Sum(s => s.Count) + Locations.Count + _keyErrors.Count;

        public MapInstancing InstancingFor(uint mapContextId)
        {
            return MapSettings.TryGetValue(mapContextId, out var setting) ? (MapInstancing)setting.Instancing : MapInstancing.Shared;
        }

        // Rows keyed 0 are never indexed: 0 means "none" in every reference column.
        private IReadOnlyDictionary<uint, T> Index<T>(IEnumerable<T> rows, Func<T, uint> key, string table)
        {
            var result = new Dictionary<uint, T>();

            foreach (var row in rows)
                if (key(row) == 0)
                    _keyErrors.Add((table, 0, ReservedId));
                else if (!result.TryAdd(key(row), row))
                    _keyErrors.Add((table, key(row), "duplicate key"));

            return result;
        }

        private IReadOnlyDictionary<uint, IReadOnlyList<T>> Group<T>(IEnumerable<T> rows, Func<T, uint> key, string table, Func<IEnumerable<T>, IEnumerable<T>> order)
        {
            var result = new Dictionary<uint, IReadOnlyList<T>>();

            foreach (var group in rows.GroupBy(key))
                if (group.Key == 0)
                    _keyErrors.Add((table, 0, ReservedId));
                else
                    result[group.Key] = order(group).ToList();

            return result;
        }

        #region Validation

        public ContentValidation Validate(IContentReferences references, ContentCapabilities capabilities = null)
        {
            var validator = new Validator(this, references, capabilities ?? MissionContentRules.Implemented);
            return new ContentValidation(this, validator.Run(), references);
        }

        private sealed class Validator
        {
            private readonly MissionContentCatalog _c;
            private readonly IContentReferences _references;
            private readonly ContentCapabilities _can;
            private readonly List<ContentGap> _gaps = new();

            public Validator(MissionContentCatalog catalog, IContentReferences references, ContentCapabilities capabilities)
            {
                _c = catalog;
                _references = references;
                _can = capabilities;
            }

            public List<ContentGap> Run()
            {
                foreach (var (table, id, message) in _c._keyErrors)
                    _gaps.Add(new ContentGap(table, $"{id}", id, message));

                MapSettings();
                Prerequisites();
                ObjectiveRows();
                Bindings();
                Areas();
                Placements();
                Conditions();
                Rules();
                ItemSets();
                Locations();
                RuleCycles();

                return _gaps;
            }

            private Action<string> Gaps(string table, string key, uint owner) => message => _gaps.Add(new ContentGap(table, key, owner, message));

            private static bool Defined<TEnum>(byte value) where TEnum : Enum => Enum.IsDefined(typeof(TEnum), value);

            private static void CheckUnused(Action<string> gap, HashSet<string> used, params (string Column, bool Set)[] columns)
            {
                foreach (var (column, set) in columns)
                    if (set && !used.Contains(column))
                        gap($"unexpected {column}");
            }

            private void MapSettings()
            {
                foreach (var setting in _c.MapSettings.Values)
                {
                    var gap = Gaps(ContentMapSettingEntry.TableName, $"{setting.MapContextId}", setting.MapContextId);

                    if (!_references.MapContextExists(setting.MapContextId))
                        gap("unknown map context");

                    if (!Defined<MapInstancing>(setting.Instancing))
                    {
                        gap($"unknown instancing {setting.Instancing}");
                        continue;
                    }

                    var instancing = (MapInstancing)setting.Instancing;

                    if (!_can.Instancing.Contains(instancing))
                        gap($"{instancing} instancing is not implemented");

                    if (instancing == MapInstancing.PerCharacter && _references.HasLegacyWorldObjects(setting.MapContextId))
                        gap("spawnpool, footlocker or logos rows exist in a per-character context");
                }
            }

            private void Prerequisites()
            {
                foreach (var prerequisite in _c.Prerequisites)
                {
                    var gap = Gaps(NpcMissionPrerequisiteEntry.TableName, $"{prerequisite.MissionId}/{prerequisite.OrGroup}/{prerequisite.RequiredMissionId}", prerequisite.MissionId);

                    if (!_can.Prerequisites)
                        gap("mission prerequisites are not implemented");

                    if (!_references.MissionExists(prerequisite.MissionId))
                        gap("unknown mission");

                    if (!_references.MissionExists(prerequisite.RequiredMissionId))
                        gap("unknown required mission");

                    var state = (MissionState)prerequisite.RequiredState;
                    if (state != MissionState.Completed && state != MissionState.Failded && state != MissionState.NotAssigned)
                        gap($"required state {prerequisite.RequiredState} is not completed, failed or not assigned");
                }
            }

            private void ObjectiveRows()
            {
                foreach (var counter in _c.Counters)
                {
                    var gap = Gaps(NpcMissionObjectiveCounterEntry.TableName, $"{counter.MissionId}/{counter.ObjectiveId}/{counter.CounterId}", counter.MissionId);

                    if (!_can.Counters)
                        gap("objective counters are not implemented");

                    if (!_references.ObjectiveExists(counter.MissionId, counter.ObjectiveId))
                        gap("unknown objective");

                    if (counter.CounterId == 255)
                        gap("counter id 255 means none");

                    if (counter.TargetValue < 1 || counter.InitialValue < 0 || counter.InitialValue >= counter.TargetValue)
                        gap("counter needs 0 <= initial < target");
                }

                foreach (var timer in _c.Timers)
                {
                    var gap = Gaps(NpcMissionObjectiveTimerEntry.TableName, $"{timer.MissionId}/{timer.ObjectiveId}", timer.MissionId);

                    if (!_can.Timers)
                        gap("objective timers are not implemented");

                    if (!_references.ObjectiveExists(timer.MissionId, timer.ObjectiveId))
                        gap("unknown objective");

                    if (timer.LimitSeconds < 1)
                        gap("limit must be at least one second");

                    if (!Defined<ObjectiveTimerExpiry>(timer.OnExpire))
                        gap($"unknown expiry {timer.OnExpire}");

                    // A failure must not strand the character: some mission has to be offerable
                    // after this one failed (build plan 1.5, the retry readiness rule).
                    if ((ObjectiveTimerExpiry)timer.OnExpire == ObjectiveTimerExpiry.FailObjectiveAndMission &&
                        !_c.Prerequisites.Any(prerequisite => prerequisite.RequiredMissionId == timer.MissionId &&
                                                              (MissionState)prerequisite.RequiredState == MissionState.Failded))
                        gap($"timed objective {timer.MissionId}/{timer.ObjectiveId} can fail the mission but no retry is offered");

                    if (MissionRules.NonAbandonableMissions.Contains(timer.MissionId))
                        gap("a non-abandonable mission cannot have a timer");
                }

                foreach (var indicator in _c.Indicators)
                {
                    var gap = Gaps(NpcMissionObjectiveIndicatorEntry.TableName, $"{indicator.MissionId}/{indicator.ObjectiveId}/{indicator.IndicatorIndex}", indicator.MissionId);

                    if (!_can.Indicators)
                        gap("objective indicators are not implemented");

                    if (!_references.ObjectiveExists(indicator.MissionId, indicator.ObjectiveId))
                        gap("unknown objective");

                    if (indicator.IndicatorId == 0 || indicator.Radius < 0)
                        gap("indicator needs an id and a non-negative radius");
                }
            }

            private void Bindings()
            {
                foreach (var binding in _c.Bindings)
                {
                    var gap = Gaps(NpcMissionObjectiveBindingEntry.TableName, $"{binding.MissionId}/{binding.ObjectiveId}/{binding.BindingId}", binding.MissionId);

                    if (!_references.ObjectiveExists(binding.MissionId, binding.ObjectiveId))
                        gap("unknown objective");

                    if (!Defined<ObjectiveBindingKind>(binding.Kind))
                    {
                        gap($"unknown binding kind {binding.Kind}");
                        continue;
                    }

                    var kind = (ObjectiveBindingKind)binding.Kind;

                    if (!_can.BindingKinds.Contains(kind))
                        gap($"binding kind {kind} is not implemented");

                    var used = new HashSet<string>();
                    _c.Placements.TryGetValue(binding.PlacementId, out var placement);

                    switch (kind)
                    {
                        case ObjectiveBindingKind.AreaEntered:
                            used.Add("area_id");
                            if (!_c.Areas.ContainsKey(binding.AreaId))
                                gap("unknown area");
                            break;

                        case ObjectiveBindingKind.UseCompleted:
                        case ObjectiveBindingKind.LootAll:
                            used.Add("placement_id");
                            if (placement == null || (ContentPlacementKind)placement.Kind != ContentPlacementKind.Usable)
                                gap("needs a usable placement");
                            else if (kind == ObjectiveBindingKind.LootAll && (ContentUsableKind)placement.UsableKind != ContentUsableKind.Container)
                                gap("loot binding needs a container placement");
                            break;

                        case ObjectiveBindingKind.Equip:
                            used.Add("equip_match");
                            switch (binding.EquipMatch)
                            {
                                case 0:
                                    break;
                                case 1:
                                    used.Add("item_template_id");
                                    if (!_references.ItemTemplateExists(binding.ItemTemplateId))
                                        gap("unknown item template");
                                    break;
                                case 2:
                                    used.Add("item_set_id");
                                    if (!_c.ItemSets.ContainsKey(binding.ItemSetId))
                                        gap("unknown item set");
                                    break;
                                default:
                                    gap($"unknown equip match {binding.EquipMatch}");
                                    break;
                            }
                            break;

                        case ObjectiveBindingKind.Hit:
                        case ObjectiveBindingKind.Kill:
                            if (binding.PlacementId != 0)
                            {
                                used.Add("placement_id");
                                if (placement == null)
                                    gap("unknown placement");
                            }
                            else
                            {
                                used.Add("creature_id");
                                if (!_references.CreatureExists(binding.CreatureId))
                                    gap("needs a placement or a known creature");
                            }

                            if (kind == ObjectiveBindingKind.Hit)
                            {
                                used.Add("action_id");
                                used.Add("destroying_hit_only");
                                // Weapon attacks (0) and the Recruit Lightning ability are the only hits the server resolves.
                                if (binding.ActionId != 0 && binding.ActionId != (uint)ActionId.AaRecruitLightning)
                                    gap($"hit by action {binding.ActionId} has no server implementation");
                            }
                            break;

                        case ObjectiveBindingKind.PlacementState:
                            used.Add("placement_id");
                            used.Add("target_state");
                            if (placement == null || (ContentPlacementKind)placement.Kind != ContentPlacementKind.Usable)
                                gap("needs a usable placement");
                            else if (!MissionContentRules.UsableStates((ContentUsableKind)placement.UsableKind).Contains(binding.TargetState))
                                gap($"state {binding.TargetState} is not a state of {(ContentUsableKind)placement.UsableKind}");
                            break;
                    }

                    if (binding.CounterId != 255)
                    {
                        used.Add("counter_id");
                        if (!_c.Counters.Any(c => c.MissionId == binding.MissionId && c.ObjectiveId == binding.ObjectiveId && c.CounterId == binding.CounterId))
                            gap("unknown counter");
                    }

                    CheckUnused(gap, used,
                        ("area_id", binding.AreaId != 0), ("placement_id", binding.PlacementId != 0), ("creature_id", binding.CreatureId != 0),
                        ("action_id", binding.ActionId != 0), ("destroying_hit_only", binding.DestroyingHitOnly), ("equip_match", binding.EquipMatch != 0),
                        ("item_template_id", binding.ItemTemplateId != 0), ("item_set_id", binding.ItemSetId != 0), ("target_state", binding.TargetState != 0));
                }
            }

            private void Areas()
            {
                foreach (var area in _c.Areas.Values)
                {
                    var gap = Gaps(ContentAreaEntry.TableName, $"{area.Id}", area.Id);

                    if (!_references.MapContextExists(area.MapContextId))
                        gap("unknown map context");

                    if (!(area.Radius > 0))
                        gap("radius must be positive");

                    switch ((ContentAreaShape)area.Shape)
                    {
                        case ContentAreaShape.Sphere:
                            if (area.HalfHeight != 0)
                                gap("a sphere has no half height");
                            break;
                        case ContentAreaShape.VerticalCylinder:
                            if (!(area.HalfHeight > 0))
                                gap("a cylinder needs a positive half height");
                            break;
                        default:
                            gap($"unknown shape {area.Shape}");
                            break;
                    }
                }
            }

            private void Placements()
            {
                foreach (var placement in _c.Placements.Values)
                {
                    var gap = Gaps(ContentPlacementEntry.TableName, $"{placement.Id}", placement.Id);

                    if (!_references.MapContextExists(placement.MapContextId))
                        gap("unknown map context");

                    if (!Defined<ContentPlacementKind>(placement.Kind))
                    {
                        gap($"unknown placement kind {placement.Kind}");
                        continue;
                    }

                    var kind = (ContentPlacementKind)placement.Kind;

                    if (!_can.PlacementKinds.Contains(kind))
                        gap($"placement kind {kind} is not implemented");

                    if (!Defined<ContentPlacementBehavior>(placement.Behavior))
                        gap($"unknown behavior {placement.Behavior}");
                    else if (!_can.PlacementBehaviors.Contains((ContentPlacementBehavior)placement.Behavior))
                        gap($"behavior {(ContentPlacementBehavior)placement.Behavior} is not implemented");

                    var used = new HashSet<string> { "behavior", "respawn_ms" };

                    if (placement.RespawnMs != 0 && !_can.PlacementRespawn)
                        gap("placement respawn is not implemented");

                    switch (kind)
                    {
                        case ContentPlacementKind.Creature:
                            used.Add("creature_id");
                            used.Add("npc_package_id");
                            if (!_references.CreatureExists(placement.CreatureId))
                                gap("unknown creature");
                            if (placement.NpcPackageId != 0 && !_can.NpcPackageOverride)
                                gap("npc package override is not implemented");
                            break;

                        case ContentPlacementKind.Usable:
                            used.Add("entity_class_id");
                            used.Add("usable_kind");
                            used.Add("initial_state");
                            used.Add("windup_ms");
                            used.Add("name_override_id");
                            if (!_references.EntityClassExists(placement.EntityClassId))
                                gap("unknown entity class");

                            if ((ContentPlacementBehavior)placement.Behavior != ContentPlacementBehavior.Stationary)
                                gap("usable placements must be stationary");

                            if (placement.UsableKind == 0 || !Defined<ContentUsableKind>(placement.UsableKind))
                            {
                                gap($"unknown usable kind {placement.UsableKind}");
                                break;
                            }

                            var usableKind = (ContentUsableKind)placement.UsableKind;

                            if (!_can.UsableKinds.Contains(usableKind))
                                gap($"usable kind {usableKind} is not implemented");

                            var states = MissionContentRules.UsableStates(usableKind);
                            if (states.Count == 0)
                                gap($"usable kind {usableKind} has no recovered state machine");
                            else if (!states.Contains(placement.InitialState))
                                gap($"initial state {placement.InitialState} is not a state of {usableKind}");

                            if (placement.AlternateStateConditionId != 0)
                            {
                                used.Add("alternate_state");
                                used.Add("alternate_state_condition_id");
                                if (states.Count > 0 && !states.Contains(placement.AlternateState))
                                    gap($"alternate state {placement.AlternateState} is not a state of {usableKind}");
                            }

                            switch (usableKind)
                            {
                                case ContentUsableKind.Destroyable:
                                    used.Add("hit_points");
                                    used.Add("restore_ms");
                                    if (placement.HitPoints == 0)
                                        gap("a destroyable placement needs hit points");
                                    break;
                                case ContentUsableKind.Bomb:
                                    used.Add("fuse_ms");
                                    if (placement.FuseMs == 0)
                                        gap("a bomb placement needs a fuse");
                                    break;
                                case ContentUsableKind.Container:
                                    used.Add("loot_item_set_id");
                                    if (!_c.ItemSets.ContainsKey(placement.LootItemSetId))
                                        gap("a container needs a known loot item set");
                                    break;
                            }

                            if (placement.UsableConditionId != 0)
                                used.Add("usable_condition_id");
                            break;
                    }

                    if (placement.PresentConditionId != 0)
                        used.Add("present_condition_id");

                    var perCharacter = _c.InstancingFor(placement.MapContextId) == MapInstancing.PerCharacter;

                    foreach (var (column, conditionId) in new[]
                             {
                                 ("present_condition_id", placement.PresentConditionId),
                                 ("alternate_state_condition_id", placement.AlternateStateConditionId),
                                 ("usable_condition_id", placement.UsableConditionId)
                             })
                    {
                        if (conditionId == 0 || !used.Contains(column))
                            continue;

                        if (!_c.Conditions.ContainsKey(conditionId))
                            gap($"unknown condition in {column}");

                        // Presence and alternate state are evaluated against one owner; a shared world has none.
                        if (column != "usable_condition_id" && !perCharacter)
                            gap($"{column} requires a per-character context");
                    }

                    CheckUnused(gap, used,
                        ("creature_id", placement.CreatureId != 0), ("npc_package_id", placement.NpcPackageId != 0),
                        ("entity_class_id", placement.EntityClassId != 0), ("usable_kind", placement.UsableKind != 0),
                        ("initial_state", placement.InitialState != 0), ("alternate_state", placement.AlternateState != 0),
                        ("alternate_state_condition_id", placement.AlternateStateConditionId != 0), ("windup_ms", placement.WindupMs != 0),
                        ("name_override_id", placement.NameOverrideId != 0), ("hit_points", placement.HitPoints != 0),
                        ("restore_ms", placement.RestoreMs != 0), ("fuse_ms", placement.FuseMs != 0),
                        ("loot_item_set_id", placement.LootItemSetId != 0), ("present_condition_id", placement.PresentConditionId != 0),
                        ("usable_condition_id", placement.UsableConditionId != 0));
                }
            }

            private void Conditions()
            {
                foreach (var term in _c.Conditions.Values.SelectMany(terms => terms))
                {
                    var gap = Gaps(ContentConditionEntry.TableName, $"{term.ConditionId}/{term.OrGroup}/{term.TermIndex}", term.ConditionId);
                    var used = new HashSet<string> { "negate" };

                    if (!Defined<ContentConditionKind>(term.Kind))
                    {
                        gap($"unknown condition kind {term.Kind}");
                        continue;
                    }

                    var kind = (ContentConditionKind)term.Kind;

                    if (!_can.ConditionKinds.Contains(kind))
                        gap($"condition kind {kind} is not implemented");

                    switch (kind)
                    {
                        case ContentConditionKind.MissionStateIs:
                            used.Add("mission_id");
                            used.Add("state");
                            if (!_references.MissionExists(term.MissionId))
                                gap("unknown mission");
                            if (!Enum.IsDefined(typeof(MissionState), (int)term.State))
                                gap($"unknown mission state {term.State}");
                            break;
                        case ContentConditionKind.ObjectiveStateIs:
                            used.Add("mission_id");
                            used.Add("objective_id");
                            used.Add("state");
                            if (!_references.ObjectiveExists(term.MissionId, term.ObjectiveId))
                                gap("unknown objective");
                            if (!Enum.IsDefined(typeof(MissionObjectiveState), (int)term.State))
                                gap($"unknown objective state {term.State}");
                            break;
                        case ContentConditionKind.MissionAbsent:
                            used.Add("mission_id");
                            if (!_references.MissionExists(term.MissionId))
                                gap("unknown mission");
                            break;
                        case ContentConditionKind.FactEquals:
                            used.Add("fact_key");
                            used.Add("value");
                            if (string.IsNullOrEmpty(term.FactKey))
                                gap("fact condition needs a key");
                            break;
                        case ContentConditionKind.HasLogos:
                            used.Add("value");
                            if (term.Value <= 0 || !_references.LogosExists((uint)term.Value))
                                gap("unknown logos in value");
                            break;
                    }

                    CheckUnused(gap, used,
                        ("mission_id", term.MissionId != 0), ("objective_id", term.ObjectiveId != 0), ("state", term.State != 0),
                        ("fact_key", !string.IsNullOrEmpty(term.FactKey)), ("value", term.Value != 0));
                }
            }

            private void Rules()
            {
                foreach (var rule in _c.Rules.Values)
                {
                    var gap = Gaps(ContentRuleEntry.TableName, $"{rule.Id}", rule.Id);

                    if (!_references.MapContextExists(rule.MapContextId))
                        gap("unknown map context");

                    if (!Defined<ContentRuleEvent>(rule.Event))
                    {
                        gap($"unknown event {rule.Event}");
                        continue;
                    }

                    var ruleEvent = (ContentRuleEvent)rule.Event;

                    if (!_can.Events.Contains(ruleEvent))
                        gap($"event {ruleEvent} is not implemented");

                    // Filters are optional (0 = any), but a filter the event cannot carry never matches.
                    var takesMission = ruleEvent is ContentRuleEvent.MissionAccepted or ContentRuleEvent.MissionCompleteable or ContentRuleEvent.MissionTurnedIn
                        or ContentRuleEvent.MissionFailed or ContentRuleEvent.MissionAbandoned or ContentRuleEvent.ObjectiveCompleted
                        or ContentRuleEvent.ObjectiveRevealed or ContentRuleEvent.ObjectiveFailed;
                    var takesObjective = ruleEvent is ContentRuleEvent.ObjectiveCompleted or ContentRuleEvent.ObjectiveRevealed or ContentRuleEvent.ObjectiveFailed;
                    var takesArea = ruleEvent == ContentRuleEvent.AreaEntered;
                    var takesPlacement = ruleEvent is ContentRuleEvent.PlacementStateEntered or ContentRuleEvent.PlacementDestroyed;
                    var takesState = ruleEvent == ContentRuleEvent.PlacementStateEntered;

                    if ((!takesMission && rule.MissionId != 0) || (!takesObjective && rule.ObjectiveId != 0) ||
                        (!takesArea && rule.AreaId != 0) || (!takesPlacement && rule.PlacementId != 0) || (!takesState && rule.StateId != 0))
                        gap($"filter columns do not match event {ruleEvent}");

                    if (rule.ObjectiveId != 0 && rule.MissionId == 0)
                        gap("objective filter without a mission filter");
                    else if (rule.ObjectiveId != 0 ? !_references.ObjectiveExists(rule.MissionId, rule.ObjectiveId) : rule.MissionId != 0 && !_references.MissionExists(rule.MissionId))
                        gap("unknown mission or objective filter");

                    if (rule.AreaId != 0 && (!_c.Areas.TryGetValue(rule.AreaId, out var area) || area.MapContextId != rule.MapContextId))
                        gap("unknown area filter in this context");

                    if (rule.PlacementId != 0)
                    {
                        if (!_c.Placements.TryGetValue(rule.PlacementId, out var filtered) || filtered.MapContextId != rule.MapContextId)
                            gap("unknown placement filter in this context");
                        else if (rule.StateId != 0 && !MissionContentRules.UsableStates((ContentUsableKind)filtered.UsableKind).Contains(rule.StateId))
                            gap($"state {rule.StateId} is not a state of the filtered placement");
                    }

                    if (rule.ConditionId != 0 && !_c.Conditions.ContainsKey(rule.ConditionId))
                        gap("unknown condition");

                    if (!_c.RuleActions.TryGetValue(rule.Id, out var actions) || actions.Count == 0)
                        gap("rule has no actions");
                }

                foreach (var action in _c.RuleActions.Values.SelectMany(actions => actions))
                    Action(action);
            }

            private void Action(ContentRuleActionEntry action)
            {
                var gap = Gaps(ContentRuleActionEntry.TableName, $"{action.RuleId}/{action.Sequence}", action.RuleId);

                if (!_c.Rules.ContainsKey(action.RuleId))
                    gap("unknown rule");

                if (!Defined<ContentRuleAction>(action.Action))
                {
                    gap($"unknown action {action.Action}");
                    return;
                }

                var kind = (ContentRuleAction)action.Action;

                if (!_can.Actions.Contains(kind))
                    gap($"action {kind} is not implemented");

                var used = new HashSet<string>();

                switch (kind)
                {
                    case ContentRuleAction.DispenseRadioMission:
                        used.Add("mission_id");
                        used.Add("forced");
                        if (!_references.MissionExists(action.MissionId))
                            gap("unknown mission");
                        break;

                    case ContentRuleAction.OfferMissionAtNpc:
                        used.Add("mission_id");
                        used.Add("placement_id");
                        if (!_references.MissionExists(action.MissionId))
                            gap("unknown mission");
                        if (!_c.Placements.TryGetValue(action.PlacementId, out var giver) || (ContentPlacementKind)giver.Kind != ContentPlacementKind.Creature)
                            gap("needs a creature placement");
                        else if (_references.MissionExists(action.MissionId) && _references.MissionGiver(action.MissionId) != giver.CreatureId)
                            gap("placement creature is not the mission giver");
                        break;

                    case ContentRuleAction.ForceConverseGreeting:
                        used.Add("greeting_id");
                        used.Add("npc_name_id");
                        if (action.GreetingId == 0 || action.NpcNameId == 0)
                            gap("forced greeting needs a greeting id and a speaker name id");
                        break;

                    case ContentRuleAction.TutorialNotification:
                        used.Add("tutorial_id");
                        if (action.TutorialId == 0)
                            gap("needs a tutorial id");
                        else if (MissionContentRules.ClientPostedTutorialIds.Contains(action.TutorialId))
                            gap($"tutorial {action.TutorialId} is posted by the client itself");
                        break;

                    case ContentRuleAction.GrantLogos:
                        used.Add("logos_id");
                        used.Add("logos_protocol");
                        if (!_references.LogosExists(action.LogosId))
                            gap("unknown logos");
                        if (!Defined<LogosGrantProtocol>(action.LogosProtocol))
                            gap($"unknown logos protocol {action.LogosProtocol}");
                        break;

                    case ContentRuleAction.GrantRewards:
                        used.Add("experience");
                        used.Add("credits");
                        if (action.Credits < 0)
                            gap("rewards cannot remove credits");
                        if (action.Experience == 0 && action.Credits == 0)
                            gap("reward grant is empty");
                        break;

                    case ContentRuleAction.GrantItemSet:
                        used.Add("item_set_id");
                        if (!_c.ItemSets.ContainsKey(action.ItemSetId))
                            gap("unknown item set");
                        break;

                    case ContentRuleAction.SetFact:
                        used.Add("fact_key");
                        used.Add("fact_value");
                        if (string.IsNullOrEmpty(action.FactKey))
                            gap("needs a fact key");
                        break;

                    case ContentRuleAction.ClearFact:
                        used.Add("fact_key");
                        if (string.IsNullOrEmpty(action.FactKey))
                            gap("needs a fact key");
                        break;

                    case ContentRuleAction.SetPlacementState:
                        used.Add("placement_id");
                        used.Add("state_id");
                        if (!_c.Placements.TryGetValue(action.PlacementId, out var target) || (ContentPlacementKind)target.Kind != ContentPlacementKind.Usable)
                            gap("needs a usable placement");
                        else if (!MissionContentRules.UsableStates((ContentUsableKind)target.UsableKind).Contains(action.StateId))
                            gap($"state {action.StateId} is not a state of {(ContentUsableKind)target.UsableKind}");
                        break;

                    case ContentRuleAction.TransferToLocation:
                        used.Add("location_id");
                        if (!_c.Locations.TryGetValue(action.LocationId, out var location) || (ContentLocationPurpose)location.Purpose != ContentLocationPurpose.TransferDestination)
                            gap("needs a transfer destination location");
                        break;

                    case ContentRuleAction.SetAccountSkipBootcamp:
                        break;

                    case ContentRuleAction.PlayTutorialAudio:
                        used.Add("audio_set_id");
                        if (action.AudioSetId == 0)
                            gap("needs an audio set id");
                        break;
                }

                CheckUnused(gap, used,
                    ("mission_id", action.MissionId != 0), ("forced", action.Forced), ("greeting_id", action.GreetingId != 0),
                    ("npc_name_id", action.NpcNameId != 0), ("tutorial_id", action.TutorialId != 0), ("logos_id", action.LogosId != 0),
                    ("logos_protocol", action.LogosProtocol != 0), ("experience", action.Experience != 0), ("credits", action.Credits != 0),
                    ("item_set_id", action.ItemSetId != 0), ("placement_id", action.PlacementId != 0), ("state_id", action.StateId != 0),
                    ("fact_key", !string.IsNullOrEmpty(action.FactKey)), ("fact_value", action.FactValue != 0),
                    ("location_id", action.LocationId != 0), ("audio_set_id", action.AudioSetId != 0));
            }

            private void ItemSets()
            {
                foreach (var item in _c.ItemSets.Values.SelectMany(items => items))
                {
                    var gap = Gaps(ContentItemSetEntry.TableName, $"{item.ItemSetId}/{item.ItemTemplateId}", item.ItemSetId);

                    if (!_references.ItemTemplateExists(item.ItemTemplateId))
                        gap("unknown item template");

                    if (item.Quantity == 0)
                        gap("quantity must be positive");
                }
            }

            private void Locations()
            {
                foreach (var location in _c.Locations.Values)
                {
                    var gap = Gaps(ContentLocationEntry.TableName, $"{location.Id}", location.Id);

                    if (!_references.MapContextExists(location.MapContextId))
                        gap("unknown map context");

                    if (!Defined<ContentLocationPurpose>(location.Purpose))
                        gap($"unknown purpose {location.Purpose}");
                }
            }

            /// <summary>
            /// A rule whose actions raise events that lead back to itself would loop. Setting a placement
            /// state raises that placement's state events (including rules filtering "any placement" in its
            /// context); a transfer raises entered_map in the destination context. Every rule in a strongly
            /// connected component with more than one rule, or with an edge to itself, is a gap.
            /// </summary>
            private void RuleCycles()
            {
                var edges = new Dictionary<uint, List<uint>>();

                foreach (var rule in _c.Rules.Values)
                {
                    var next = new List<uint>();

                    if (_c.RuleActions.TryGetValue(rule.Id, out var actions))
                        foreach (var action in actions)
                        {
                            switch ((ContentRuleAction)action.Action)
                            {
                                case ContentRuleAction.SetPlacementState when _c.Placements.TryGetValue(action.PlacementId, out var placement):
                                    next.AddRange(_c.Rules.Values
                                        .Where(r => (ContentRuleEvent)r.Event is ContentRuleEvent.PlacementStateEntered or ContentRuleEvent.PlacementDestroyed &&
                                                    (r.PlacementId == placement.Id || (r.PlacementId == 0 && r.MapContextId == placement.MapContextId)) &&
                                                    (r.StateId == 0 || r.StateId == action.StateId))
                                        .Select(r => r.Id));
                                    break;
                                case ContentRuleAction.TransferToLocation when _c.Locations.TryGetValue(action.LocationId, out var location):
                                    next.AddRange(_c.Rules.Values
                                        .Where(r => (ContentRuleEvent)r.Event == ContentRuleEvent.EnteredMap && r.MapContextId == location.MapContextId)
                                        .Select(r => r.Id));
                                    break;
                            }
                        }

                    edges[rule.Id] = next;
                }

                // Tarjan's strongly connected components.
                var index = 0;
                var indices = new Dictionary<uint, int>();
                var lowLinks = new Dictionary<uint, int>();
                var stack = new Stack<uint>();
                var onStack = new HashSet<uint>();
                var inCycle = new SortedSet<uint>();

                void Connect(uint id)
                {
                    indices[id] = lowLinks[id] = index++;
                    stack.Push(id);
                    onStack.Add(id);

                    foreach (var next in edges[id])
                    {
                        if (!indices.ContainsKey(next))
                        {
                            Connect(next);
                            lowLinks[id] = Math.Min(lowLinks[id], lowLinks[next]);
                        }
                        else if (onStack.Contains(next))
                            lowLinks[id] = Math.Min(lowLinks[id], indices[next]);
                    }

                    if (lowLinks[id] != indices[id])
                        return;

                    var component = new List<uint>();
                    uint member;
                    do
                    {
                        member = stack.Pop();
                        onStack.Remove(member);
                        component.Add(member);
                    } while (member != id);

                    if (component.Count > 1 || edges[id].Contains(id))
                        inCycle.UnionWith(component);
                }

                foreach (var id in edges.Keys.OrderBy(id => id))
                    if (!indices.ContainsKey(id))
                        Connect(id);

                foreach (var id in inCycle)
                    _gaps.Add(new ContentGap(ContentRuleEntry.TableName, $"{id}", id, "rule is part of a cycle"));
            }
        }

        #endregion
    }

    /// <summary>
    /// The validated catalog: gaps plus what may go live. A withheld row withholds everything that
    /// references it, so partly valid content is never exposed.
    /// </summary>
    public sealed class ContentValidation
    {
        public MissionContentCatalog Catalog { get; }
        public IReadOnlyList<ContentGap> Gaps { get; }
        public IReadOnlyCollection<uint> WithheldContexts { get; }
        public IReadOnlyCollection<uint> WithheldConditions { get; }
        public IReadOnlyCollection<uint> WithheldItemSets { get; }
        public IReadOnlyCollection<uint> WithheldLocations { get; }
        public IReadOnlyCollection<uint> WithheldAreas { get; }
        public IReadOnlyCollection<uint> WithheldPlacements { get; }
        public IReadOnlyCollection<uint> WithheldRules { get; }

        // Missions with a withheld binding, counter, timer, indicator or prerequisite row, and why.
        public IReadOnlyDictionary<uint, IReadOnlyList<string>> MissionGaps { get; }

        public ContentValidation(MissionContentCatalog catalog, List<ContentGap> gaps, IContentReferences references)
        {
            Catalog = catalog;

            HashSet<uint> Owners(string table) => new(gaps.Where(gap => gap.Table == table).Select(gap => gap.OwnerId));

            var contexts = Owners(ContentMapSettingEntry.TableName);
            var conditions = Owners(ContentConditionEntry.TableName);
            var itemSets = Owners(ContentItemSetEntry.TableName);
            var locations = Owners(ContentLocationEntry.TableName);
            var areas = Owners(ContentAreaEntry.TableName);
            var placements = Owners(ContentPlacementEntry.TableName);
            var rules = Owners(ContentRuleEntry.TableName);
            rules.UnionWith(Owners(ContentRuleActionEntry.TableName));

            // Id 0 means "none" in every reference column; rows keyed 0 are never indexed.
            foreach (var set in new[] { contexts, conditions, itemSets, locations, areas, placements, rules })
                set.Remove(0);

            locations.UnionWith(catalog.Locations.Values.Where(l => contexts.Contains(l.MapContextId)).Select(l => l.Id));
            areas.UnionWith(catalog.Areas.Values.Where(a => contexts.Contains(a.MapContextId)).Select(a => a.Id));

            // Placements depend only on contexts, conditions and item sets, none of which depend on placements.
            foreach (var placement in catalog.Placements.Values)
                if (contexts.Contains(placement.MapContextId) ||
                    itemSets.Contains(placement.LootItemSetId) ||
                    conditions.Contains(placement.PresentConditionId) ||
                    conditions.Contains(placement.AlternateStateConditionId) ||
                    conditions.Contains(placement.UsableConditionId))
                    placements.Add(placement.Id);

            var missionGaps = new Dictionary<uint, List<string>>();

            void MissionGap(uint missionId, string message)
            {
                if (!missionGaps.TryGetValue(missionId, out var list))
                    missionGaps[missionId] = list = new List<string>();

                list.Add(message);
            }

            foreach (var gap in gaps.Where(gap => gap.Table is NpcMissionPrerequisiteEntry.TableName or NpcMissionObjectiveBindingEntry.TableName
                         or NpcMissionObjectiveCounterEntry.TableName or NpcMissionObjectiveTimerEntry.TableName or NpcMissionObjectiveIndicatorEntry.TableName))
                MissionGap(gap.OwnerId, $"{gap.Table} {gap.Key}: {gap.Message}");

            foreach (var binding in catalog.Bindings)
            {
                var key = $"{binding.MissionId}/{binding.ObjectiveId}/{binding.BindingId}";

                if (areas.Contains(binding.AreaId))
                    MissionGap(binding.MissionId, $"{NpcMissionObjectiveBindingEntry.TableName} {key}: area {binding.AreaId} is withheld");

                if (placements.Contains(binding.PlacementId))
                    MissionGap(binding.MissionId, $"{NpcMissionObjectiveBindingEntry.TableName} {key}: placement {binding.PlacementId} is withheld");

                if (itemSets.Contains(binding.ItemSetId))
                    MissionGap(binding.MissionId, $"{NpcMissionObjectiveBindingEntry.TableName} {key}: item set {binding.ItemSetId} is withheld");
            }

            // A rule that offers a mission which cannot be offered would expose an incomplete definition.
            foreach (var action in catalog.RuleActions.Values.SelectMany(actions => actions))
                if ((ContentRuleAction)action.Action is ContentRuleAction.DispenseRadioMission or ContentRuleAction.OfferMissionAtNpc &&
                    references.MissionExists(action.MissionId) &&
                    (missionGaps.ContainsKey(action.MissionId) || !references.MissionOfferable(action.MissionId)))
                    gaps.Add(new ContentGap(ContentRuleActionEntry.TableName, $"{action.RuleId}/{action.Sequence}", action.RuleId,
                        $"offers mission {action.MissionId}, which is not offerable"));

            rules.UnionWith(Owners(ContentRuleActionEntry.TableName));
            rules.Remove(0);

            foreach (var rule in catalog.Rules.Values)
            {
                var withheld = contexts.Contains(rule.MapContextId) ||
                               conditions.Contains(rule.ConditionId) ||
                               areas.Contains(rule.AreaId) ||
                               placements.Contains(rule.PlacementId);

                if (catalog.RuleActions.TryGetValue(rule.Id, out var actions))
                    withheld |= actions.Any(action =>
                        placements.Contains(action.PlacementId) ||
                        itemSets.Contains(action.ItemSetId) ||
                        locations.Contains(action.LocationId));

                if (withheld)
                    rules.Add(rule.Id);
            }

            Gaps = gaps;
            WithheldContexts = contexts;
            WithheldConditions = conditions;
            WithheldItemSets = itemSets;
            WithheldLocations = locations;
            WithheldAreas = areas;
            WithheldPlacements = placements;
            WithheldRules = rules;
            MissionGaps = missionGaps.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<string>)pair.Value);
        }

        public IEnumerable<ContentRuleEntry> LiveRules => Catalog.Rules.Values.Where(rule => !WithheldRules.Contains(rule.Id));

        public IEnumerable<ContentPlacementEntry> LivePlacements => Catalog.Placements.Values.Where(placement => !WithheldPlacements.Contains(placement.Id));

        public IEnumerable<ContentAreaEntry> LiveAreas => Catalog.Areas.Values.Where(area => !WithheldAreas.Contains(area.Id));

        public IEnumerable<NpcMissionObjectiveBindingEntry> LiveBindings => Catalog.Bindings.Where(binding => !MissionGaps.ContainsKey(binding.MissionId));
        public IEnumerable<NpcMissionPrerequisiteEntry> LivePrerequisites => Catalog.Prerequisites.Where(prerequisite => !MissionGaps.ContainsKey(prerequisite.MissionId));
        public IEnumerable<NpcMissionObjectiveTimerEntry> LiveTimers => Catalog.Timers.Where(timer => !MissionGaps.ContainsKey(timer.MissionId));
    }
}
