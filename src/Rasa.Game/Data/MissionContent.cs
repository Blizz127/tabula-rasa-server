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
        PlacementState = 7
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
        CreatureAi = 2
    }

    public enum ContentConditionKind : byte
    {
        MissionStateIs = 1,
        ObjectiveStateIs = 2,
        MissionAbsent = 3,
        FactEquals = 4,
        HasLogos = 5
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
        PlacementDestroyed = 12
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
        PlayTutorialAudio = 13
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
        TransferDestination = 2
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
