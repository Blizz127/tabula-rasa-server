namespace Rasa.Data
{
    // Closed vocabularies of the reconstructed-content tables. Stored values are part of the
    // world database format; never renumber. Each kind is only live once a server handler
    // exists for it (see MissionContentRules).

    public enum MapInstancing : byte
    {
        Shared = 0,
        PerCharacter = 1
    }

    public enum ObjectiveBindingKind : byte
    {
        AreaEntered = 1,
        UseCompleted = 2,
        LootAll = 3,
        Equip = 4,
        Hit = 5,
        Kill = 6,
        PlacementState = 7,
        /// <summary>
        /// The source is a Logos shrine. Shrines are Logos dynamic objects built from the world's <c>logos</c>
        /// table (LogosManager.LogosInit) rather than content placements, so this binding's placement_id holds
        /// the logos row id and DynamicObjectManager.LogosRecovery fires it with the id it resolved.
        /// </summary>
        LogosRecovered = 8
    }

    public enum ContentAreaShape : byte
    {
        Sphere = 1,
        VerticalCylinder = 2
    }

    public enum ContentPlacementKind : byte
    {
        Creature = 1,
        Usable = 2
    }

    public enum ContentUsableKind : byte
    {
        None = 0,
        Container = 1,
        Destroyable = 2,
        Bomb = 3,
        GenericUse = 4,
        Structure = 5
    }

    public enum ContentPlacementBehavior : byte
    {
        Stationary = 1,
        CreatureAi = 2,

        /// <summary>
        /// Walks with the player on the placement's escort mission instead of standing or wandering: the escort
        /// objectives ("Take Milpas to Apirka", "Escort Milpas to Divide entrance") depend on it, and an area
        /// objective on that mission only completes once this creature is inside the area too.
        /// </summary>
        Escort = 3
    }

    public enum ContentConditionKind : byte
    {
        MissionStateIs = 1,
        ObjectiveStateIs = 2,
        MissionAbsent = 3,
        FactEquals = 4,
        HasLogos = 5,
        // value = character class id (1 Recruit .. 15 Exobiologist)
        CharacterClassIs = 6
    }

    public enum ContentRuleEvent : byte
    {
        EnteredMap = 1,
        MissionAccepted = 2,
        ObjectiveCompleted = 3,
        ObjectiveRevealed = 4,
        MissionCompleteable = 5,
        MissionTurnedIn = 6,
        ObjectiveFailed = 7,
        MissionFailed = 8,
        MissionAbandoned = 9,
        AreaEntered = 10,
        PlacementStateEntered = 11,
        PlacementDestroyed = 12,
        // A character trained into a new class at a class trainer (SelectNewCharacterClass).
        ClassSelected = 13
    }

    public enum ContentRuleAction : byte
    {
        DispenseRadioMission = 1,
        OfferMissionAtNpc = 2,
        ForceConverseGreeting = 3,
        TutorialNotification = 4,
        GrantLogos = 5,
        GrantRewards = 6,
        GrantItemSet = 7,
        SetFact = 8,
        ClearFact = 9,
        SetPlacementState = 10,
        TransferToLocation = 11,
        SetAccountSkipBootcamp = 12,
        PlayTutorialAudio = 13,

        /// <summary>
        /// A scripted creature walk: placement_id names the placement whose creature moves and location_id the
        /// destination (`content_location`). Used for the boot camp's escorts and for the S5 reinforcements
        /// leaving the pad, where the original moves the NPC rather than standing him still (GAP-ESCORT,
        /// GAP-S5-REINFORCEMENT-MOVE).
        /// </summary>
        MoveCreatureToLocation = 14,

        /// <summary>
        /// Hurts the triggering player by `damage` (armour first, then health, as every other hit does). The
        /// boot camp's bomb blast is the recorded case: the recruit who arms it sees "-21" take off their bar
        /// when it detonates (footage B1-049), which is why the 2005 text warns "Not yourself!" (21566).
        /// </summary>
        DamagePlayer = 15,

        /// <summary>
        /// A creature says one of the client's 859 bark lines: placement_id names the placement whose creature
        /// speaks and bark_id the row of the client's own table (Bark = 411, one argument; the client resolves
        /// the text, the voice clip and how long the bubble stays up). Nothing is persisted, and nothing is sent
        /// to a player outside the client's 20 m bark range (BarkManager).
        /// </summary>
        PlayBark = 16
    }

    public enum LogosGrantProtocol : byte
    {
        // LogosStoneAdded (475): the client prints the Logos message and shows its tutorial tip.
        StoneAdded = 1,
        // LogosStoneTabula (477): silent.
        StoneTabula = 2
    }

    public enum ContentLocationPurpose : byte
    {
        NewCharacterStart = 1,
        TransferDestination = 2,
        // Where a scripted creature walk ends (move_creature_to_location).
        ScriptedMoveDestination = 3
    }

    public enum ObjectiveTimerExpiry : byte
    {
        FailObjective = 1,
        FailObjectiveAndMission = 2
    }

    public enum BootcampEntryMode
    {
        Disabled = 0,
        AllowListedAccounts = 1,
        AllNewCharacters = 2
    }
}
