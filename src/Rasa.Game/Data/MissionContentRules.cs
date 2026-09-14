using System.Collections.Generic;
using System.Linq;

namespace Rasa.Data
{
    /// <summary>
    /// Server capabilities and recovered client constraints that decide whether reconstructed
    /// content can go live. A kind that is not implemented keeps its content withheld (fail closed);
    /// each slice that adds a handler adds its kind here together with the handler's tests.
    /// </summary>
    public static class MissionContentRules
    {
        /// <summary>
        /// What this server build implements. S1 (boot-camp Initiation) adds the mechanics its mission 1990
        /// rows need: area-entered objective bindings, the entered_map / mission_accepted / objective_completed
        /// / mission_turned_in events, the radio offer, forced greeting, tutorial and Logos actions, the
        /// mission/objective state conditions, and stationary creature placements. Every other kind stays
        /// withheld (fail closed).
        /// </summary>
        public static readonly ContentCapabilities Implemented = new ContentCapabilities
        {
            BindingKinds = new HashSet<ObjectiveBindingKind>
            {
                ObjectiveBindingKind.AreaEntered, ObjectiveBindingKind.Equip, ObjectiveBindingKind.LootAll, ObjectiveBindingKind.Hit, ObjectiveBindingKind.Kill,
                // S5: Conrad's corpse and the bomb detonating.
                ObjectiveBindingKind.UseCompleted, ObjectiveBindingKind.PlacementState
            },
            Events = new HashSet<ContentRuleEvent>
            {
                ContentRuleEvent.EnteredMap,
                ContentRuleEvent.MissionAccepted,
                ContentRuleEvent.ObjectiveCompleted,
                ContentRuleEvent.MissionTurnedIn,
                // S6: the exit pad.
                ContentRuleEvent.AreaEntered,
                // S5: the bomb armed and detonated; the timed objective and its mission failing.
                ContentRuleEvent.PlacementStateEntered,
                ContentRuleEvent.ObjectiveFailed,
                ContentRuleEvent.MissionFailed
            },
            Actions = new HashSet<ContentRuleAction>
            {
                ContentRuleAction.DispenseRadioMission,
                ContentRuleAction.OfferMissionAtNpc,
                ContentRuleAction.ForceConverseGreeting,
                ContentRuleAction.TutorialNotification,
                ContentRuleAction.GrantLogos,
                ContentRuleAction.GrantRewards,
                ContentRuleAction.TransferToLocation,
                ContentRuleAction.SetAccountSkipBootcamp,
                ContentRuleAction.SetFact,
                ContentRuleAction.ClearFact,
                // S5: the wreck bursts open when the bomb detonates.
                ContentRuleAction.SetPlacementState
            },
            ConditionKinds = new HashSet<ContentConditionKind>
            {
                ContentConditionKind.MissionAbsent,
                ContentConditionKind.MissionStateIs,
                ContentConditionKind.ObjectiveStateIs,
                // S5: the dropship wreck and bomb state.
                ContentConditionKind.FactEquals,
                ContentConditionKind.HasLogos
            },
            PlacementKinds = new HashSet<ContentPlacementKind> { ContentPlacementKind.Creature, ContentPlacementKind.Usable },
            // S4: creature-AI placements guard their spot (CreatureManager.ApplyPlacementBehavior).
            PlacementBehaviors = new HashSet<ContentPlacementBehavior> { ContentPlacementBehavior.Stationary, ContentPlacementBehavior.CreatureAi },
            UsableKinds = new HashSet<ContentUsableKind> { ContentUsableKind.Container, ContentUsableKind.Destroyable, ContentUsableKind.Bomb, ContentUsableKind.GenericUse, ContentUsableKind.Structure },
            NpcPackageOverride = true,
            // Prerequisites gate each player's offer at dispense time
            // (MissionManager.PrerequisitesSatisfied), enforced per character.
            Prerequisites = true,
            // S4: kill bindings advance objective counters (MissionContentRuntime.OnKillBinding).
            Counters = true,
            // S5: wall-clock objective timers fail their objective (and mission) on expiry
            // (MissionManager.ExpireObjectiveTimers).
            Timers = true,
            // S4: objective counters and map indicators travel in the mission info (PlayerMission.ToMissionInfo).
            Indicators = true,
            // S3: a per-character context gives each character its own MapChannel
            // (MapChannelManager.ChannelForEntry), populated from the context's placements.
            Instancing = new HashSet<MapInstancing> { MapInstancing.Shared, MapInstancing.PerCharacter }
        };

        // The entry path (S1) exists; new characters may enter the boot camp once a live start location is
        // seeded and the operational switch is moved off Disabled.
        public static readonly bool BootcampEntryImplemented = true;

        // Server-side cascades (a rule action raising another rule's event) stop at this depth.
        public const int MaxRuleCascadeDepth = 8;

        /// <summary>
        /// Tutorial ids the 1.16.5.0 client posts by itself (UI_DISPLAY_PLAYER_TUTORIAL with a fixed
        /// tutorialdata constant: 23 distinct ids from the client posting sites, e.g.
        /// mapwindow.Hide → MAP_WINDOW_CLOSED, wonkavator.OnExitState → INSTANCE_ENTERED).
        /// A server rule sending one would duplicate the client's own tip.
        /// </summary>
        public static readonly IReadOnlyCollection<uint> ClientPostedTutorialIds = new HashSet<uint>
        {
            1, 2, 3, 7, 8,
            10000029, 10000030, 10000031, 10000032, 10000033, 10000034, 10000035, 10000036,
            10000037, 10000038, 10000039, 10000040, 10000043, 10000044, 10000045, 10000046,
            10000047, 10000048
        };

        /// <summary>
        /// Usable state machines from usabledata.pyo usableaugmentationstatetransition, per server
        /// usable kind: (from, to) pairs the client accepts. Content may only move between these.
        /// </summary>
        public static readonly IReadOnlyDictionary<ContentUsableKind, IReadOnlyCollection<(uint From, uint To)>> UsableStateTransitions =
            new Dictionary<ContentUsableKind, IReadOnlyCollection<(uint From, uint To)>>
            {
                // augmentation 41 InertDestroyable
                { ContentUsableKind.Destroyable, new HashSet<(uint, uint)> { (2, 110), (110, 185), (185, 110), (185, 186), (186, 2), (186, 185) } },
                // augmentation 8 StatelessSwitch: USE_SS_STATE_0 (44) only ever returns to itself.
                { ContentUsableKind.GenericUse, new HashSet<(uint, uint)> { (44, 44) } },
                // augmentation 10 Door: CLOSED 31 <-> OPEN 91. The final client reworked the crashed-dropship wreck
                // (class 24586) into a Door whose closed state burns and whose opening plays the dropship explosion.
                { ContentUsableKind.Structure, new HashSet<(uint, uint)> { (31, 91), (91, 31) } },
                // augmentation 42 Bomb
                { ContentUsableKind.Bomb, new HashSet<(uint, uint)> { (113, 114), (114, 113), (114, 115), (115, 113) } },
                // augmentation 64 TreasureDispenser (usabledata.pyo usableaugmentationstatetransition["64"]):
                // CLOSED 200 → OPENED 201 (use) / REMOVED 202 (cipher-removal), OPENED 201 → CLOSED/REMOVED.
                { ContentUsableKind.Container, new HashSet<(uint, uint)> { (200, 201), (200, 202), (201, 200), (201, 202) } }
            };

        public static IReadOnlyCollection<uint> UsableStates(ContentUsableKind kind)
        {
            var states = new HashSet<uint>();

            if (UsableStateTransitions.TryGetValue(kind, out var transitions))
                foreach (var (from, to) in transitions)
                {
                    states.Add(from);
                    states.Add(to);
                }

            return states;
        }
    }

    /// <summary>
    /// A set of implemented content mechanics. The server uses <see cref="MissionContentRules.Implemented"/>;
    /// tests pass other sets to exercise validation independently of what is implemented.
    /// </summary>
    public sealed class ContentCapabilities
    {
        public IReadOnlyCollection<ObjectiveBindingKind> BindingKinds { get; init; } = new HashSet<ObjectiveBindingKind>();
        public IReadOnlyCollection<ContentRuleEvent> Events { get; init; } = new HashSet<ContentRuleEvent>();
        public IReadOnlyCollection<ContentRuleAction> Actions { get; init; } = new HashSet<ContentRuleAction>();
        public IReadOnlyCollection<ContentPlacementKind> PlacementKinds { get; init; } = new HashSet<ContentPlacementKind>();
        public IReadOnlyCollection<ContentPlacementBehavior> PlacementBehaviors { get; init; } = new HashSet<ContentPlacementBehavior>();
        public IReadOnlyCollection<ContentUsableKind> UsableKinds { get; init; } = new HashSet<ContentUsableKind>();
        public IReadOnlyCollection<ContentConditionKind> ConditionKinds { get; init; } = new HashSet<ContentConditionKind>();
        public IReadOnlyCollection<MapInstancing> Instancing { get; init; } = new HashSet<MapInstancing> { MapInstancing.Shared };
        public bool Prerequisites { get; init; }
        public bool Counters { get; init; }
        public bool Timers { get; init; }
        public bool Indicators { get; init; }
        public bool PlacementRespawn { get; init; }
        public bool NpcPackageOverride { get; init; }

        public static ContentCapabilities All => new ContentCapabilities
        {
            BindingKinds = new HashSet<ObjectiveBindingKind>((ObjectiveBindingKind[])System.Enum.GetValues(typeof(ObjectiveBindingKind))),
            Events = new HashSet<ContentRuleEvent>((ContentRuleEvent[])System.Enum.GetValues(typeof(ContentRuleEvent))),
            Actions = new HashSet<ContentRuleAction>((ContentRuleAction[])System.Enum.GetValues(typeof(ContentRuleAction))),
            PlacementKinds = new HashSet<ContentPlacementKind>((ContentPlacementKind[])System.Enum.GetValues(typeof(ContentPlacementKind))),
            PlacementBehaviors = new HashSet<ContentPlacementBehavior>((ContentPlacementBehavior[])System.Enum.GetValues(typeof(ContentPlacementBehavior))),
            UsableKinds = new HashSet<ContentUsableKind>(((ContentUsableKind[])System.Enum.GetValues(typeof(ContentUsableKind))).Where(kind => kind != ContentUsableKind.None)),
            ConditionKinds = new HashSet<ContentConditionKind>((ContentConditionKind[])System.Enum.GetValues(typeof(ContentConditionKind))),
            Instancing = new HashSet<MapInstancing>((MapInstancing[])System.Enum.GetValues(typeof(MapInstancing))),
            Prerequisites = true,
            Counters = true,
            Timers = true,
            Indicators = true,
            PlacementRespawn = true,
            NpcPackageOverride = true
        };
    }
}
