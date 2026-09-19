using System;
using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Repositories.UnitOfWork;
    using Structures;

    /// <summary>
    /// The client's action tables - generated/client/actiondata.pyo actionModules, actionArguments,
    /// actionAttributeCost and abilityData, row for row (1,813 levels, 405 costs, 4,259 properties; checked
    /// against the decoded client on 2026-09-19) - as the server reads them: what an ability is, what it costs,
    /// how long it winds up, recovers and waits, how far it reaches and what it does.
    ///
    /// The tables are Ellimist's (474d1ce on ellimist/development). The damage abilities they make resolvable here
    /// are the client's DamageBase modules: those whose DoAbility reads each hit as (rawInfo, onHitData) and whose
    /// OnAbility ignores onHitData, so a hit is the same damage record a weapon sends.
    /// </summary>
    public class ActionTableManager
    {
        private static ActionTableManager _instance;
        private static readonly object InstanceLock = new object();
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;
        private readonly Dictionary<ActionId, ActionInfo> _actions = new Dictionary<ActionId, ActionInfo>();

        /// <summary>client/actions/abilities modules derived from DamageBase that take a plain damage hit.</summary>
        public static readonly HashSet<string> DirectDamageModules = new HashSet<string>
        {
            "abilities.lightning", "abilities.knockback", "abilities.rushingblow", "abilities.shrapnel",
            "abilities.tectonicstrike", "abilities.stun", "abilities.concussivewave", "abilities.energywave",
            "abilities.vortex", "abilities.deathdamage", "abilities.stalkereggattack"
        };

        public static ActionTableManager Instance
        {
            get
            {
                if (_instance == null)
                    lock (InstanceLock)
                        _instance ??= new ActionTableManager(Server.GameUnitOfWorkFactory);
                return _instance;
            }
        }

        private ActionTableManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
        }

        public int Count => _actions.Count;

        /// <summary>For tests and loading: registers an action.</summary>
        public void Add(ActionInfo action) => _actions[action.ActionId] = action;

        public void Remove(ActionId actionId) => _actions.Remove(actionId);

        public bool TryGetLevel(ActionId actionId, uint level, out ActionInfo action, out ActionLevelInfo info)
        {
            info = null;
            return _actions.TryGetValue(actionId, out action) && action.Levels.TryGetValue(level, out info);
        }

        /// <summary>
        /// A player ability this server resolves as direct damage: a DamageBase module with a damage amount. Lightning
        /// (194) keeps its own lifecycle and is not reported here.
        /// </summary>
        public bool TryGetDirectDamage(ActionId actionId, uint level, out ActionInfo action, out ActionLevelInfo info)
        {
            return TryGetLevel(actionId, level, out action, out info) && actionId != ActionId.AaRecruitLightning &&
                DirectDamageModules.Contains(action.Module) && info.Has(AbilityProperty.DamageAmountMin);
        }

        /// <summary>
        /// Abilities resolved by their own game effect rather than a damage hit: Ruin (abilities.decay, a damage over
        /// time on one enemy) and Rage (abilities.rage, a toggled damage and resistance buff on the soldier and,
        /// at some levels, the squad around them).
        /// </summary>
        public static readonly HashSet<string> EffectModules = new HashSet<string> { "abilities.decay", "abilities.rage", "abilities.bioaugmentation",
            "abilities.scourge", "abilities.shieldextender", "abilities.reconstruction",
            "abilities.tacticalevasion", "abilities.firesupport", "abilities.cure" };

        /// <summary>Modules aimed at a friendly player (TARGET_FRIENDLY in the client module): self when nothing else is named.</summary>
        public static readonly HashSet<string> FriendlyModules = new HashSet<string> { "abilities.bioaugmentation", "abilities.shieldextender" };

        /// <summary>Modules aimed at the performer alone (TARGET_SELF), which take no target.</summary>
        public static readonly HashSet<string> SelfModules = new HashSet<string> { "abilities.rage", "abilities.scourge", "abilities.reconstruction", "abilities.tacticalevasion" };

        /// <summary>Any table-driven ability this server resolves: the damage family and the effect abilities.</summary>
        public bool TryGetResolvable(ActionId actionId, uint level, out ActionInfo action, out ActionLevelInfo info)
        {
            if (TryGetDirectDamage(actionId, level, out action, out info))
                return true;
            return TryGetLevel(actionId, level, out action, out info) && EffectModules.Contains(action.Module);
        }

        public void ActionTableInit()
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();

            foreach (var entry in unitOfWork.Actions.GetActions())
                _actions[(ActionId)entry.Id] = new ActionInfo
                {
                    ActionId = (ActionId)entry.Id, Name = entry.Name, Module = entry.Module, IsCharged = entry.IsCharged != 0
                };

            foreach (var entry in unitOfWork.Actions.GetActionLevels())
                if (_actions.TryGetValue((ActionId)entry.ActionId, out var action))
                    action.Levels[entry.Level] = new ActionLevelInfo
                    {
                        ActionId = action.ActionId, Level = entry.Level,
                        WindupMs = Math.Max(0, entry.WindupMs), RecoveryMs = Math.Max(0, entry.RecoveryMs),
                        MaxRange = entry.MaxRange, ReuseMs = Math.Max(0, entry.ReuseMs),
                        StartReuseOnPerform = entry.StartReuseOnPerform != 0
                    };

            foreach (var entry in unitOfWork.Actions.GetActionCosts())
                if (TryGetLevel((ActionId)entry.ActionId, entry.Level, out _, out var level))
                    level.Costs.Add(new ActionCost { Attribute = (Attributes)entry.AttributeId, Amount = entry.Cost });

            foreach (var entry in unitOfWork.Actions.GetActionProperties())
                if (TryGetLevel((ActionId)entry.ActionId, entry.Level, out _, out var level))
                    level.Properties[(AbilityProperty)entry.PropertyId] = entry.Value;

            Logger.WriteLog(LogType.Initialize, $"Loaded {_actions.Count} client actions");
        }
    }
}
