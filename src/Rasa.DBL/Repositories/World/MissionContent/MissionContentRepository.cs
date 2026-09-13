using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World.MissionContent
{
    using Context.World;
    using Structures.World;

    /// <summary>
    /// Read-only access to the reconstructed-content tables. All rows are loaded once at startup.
    /// </summary>
    public interface IMissionContentRepository
    {
        List<ContentMapSettingEntry> GetMapSettings();
        List<NpcMissionPrerequisiteEntry> GetPrerequisites();
        List<NpcMissionObjectiveBindingEntry> GetBindings();
        List<NpcMissionObjectiveCounterEntry> GetCounters();
        List<NpcMissionObjectiveTimerEntry> GetTimers();
        List<NpcMissionObjectiveIndicatorEntry> GetIndicators();
        List<ContentAreaEntry> GetAreas();
        List<ContentPlacementEntry> GetPlacements();
        List<ContentConditionEntry> GetConditions();
        List<ContentRuleEntry> GetRules();
        List<ContentRuleActionEntry> GetRuleActions();
        List<ContentItemSetEntry> GetItemSets();
        List<ContentLocationEntry> GetLocations();
    }

    public class MissionContentRepository : IMissionContentRepository
    {
        private readonly WorldContext _worldContext;

        public MissionContentRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<ContentMapSettingEntry> GetMapSettings() => _worldContext.CreateNoTrackingQuery(_worldContext.ContentMapSettingEntries).ToList();
        public List<NpcMissionPrerequisiteEntry> GetPrerequisites() => _worldContext.CreateNoTrackingQuery(_worldContext.NpcMissionPrerequisiteEntries).ToList();
        public List<NpcMissionObjectiveBindingEntry> GetBindings() => _worldContext.CreateNoTrackingQuery(_worldContext.NpcMissionObjectiveBindingEntries).ToList();
        public List<NpcMissionObjectiveCounterEntry> GetCounters() => _worldContext.CreateNoTrackingQuery(_worldContext.NpcMissionObjectiveCounterEntries).ToList();
        public List<NpcMissionObjectiveTimerEntry> GetTimers() => _worldContext.CreateNoTrackingQuery(_worldContext.NpcMissionObjectiveTimerEntries).ToList();
        public List<NpcMissionObjectiveIndicatorEntry> GetIndicators() => _worldContext.CreateNoTrackingQuery(_worldContext.NpcMissionObjectiveIndicatorEntries).ToList();
        public List<ContentAreaEntry> GetAreas() => _worldContext.CreateNoTrackingQuery(_worldContext.ContentAreaEntries).ToList();
        public List<ContentPlacementEntry> GetPlacements() => _worldContext.CreateNoTrackingQuery(_worldContext.ContentPlacementEntries).ToList();
        public List<ContentConditionEntry> GetConditions() => _worldContext.CreateNoTrackingQuery(_worldContext.ContentConditionEntries).ToList();
        public List<ContentRuleEntry> GetRules() => _worldContext.CreateNoTrackingQuery(_worldContext.ContentRuleEntries).ToList();
        public List<ContentRuleActionEntry> GetRuleActions() => _worldContext.CreateNoTrackingQuery(_worldContext.ContentRuleActionEntries).ToList();
        public List<ContentItemSetEntry> GetItemSets() => _worldContext.CreateNoTrackingQuery(_worldContext.ContentItemSetEntries).ToList();
        public List<ContentLocationEntry> GetLocations() => _worldContext.CreateNoTrackingQuery(_worldContext.ContentLocationEntries).ToList();
    }
}
