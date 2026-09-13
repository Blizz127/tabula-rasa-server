using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    public interface INpcMissionObjectiveRepository
    {
        List<NpcMissionObjectiveEntry> Get();
        List<NpcMissionObjectiveConversationEntry> GetConversations();
        List<NpcMissionObjectiveTransitionEntry> GetTransitions();
    }

    public class NpcMissionObjectiveRepository : INpcMissionObjectiveRepository
    {
        private readonly WorldContext _worldContext;

        public NpcMissionObjectiveRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<NpcMissionObjectiveEntry> Get()
        {
            return _worldContext.CreateNoTrackingQuery(_worldContext.NpcMissionObjectiveEntries).ToList();
        }

        public List<NpcMissionObjectiveConversationEntry> GetConversations()
        {
            return _worldContext.CreateNoTrackingQuery(_worldContext.NpcMissionObjectiveConversationEntries).ToList();
        }

        public List<NpcMissionObjectiveTransitionEntry> GetTransitions()
        {
            return _worldContext.CreateNoTrackingQuery(_worldContext.NpcMissionObjectiveTransitionEntries).ToList();
        }
    }
}
