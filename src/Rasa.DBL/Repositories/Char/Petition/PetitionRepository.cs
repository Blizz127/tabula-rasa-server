using System;

namespace Rasa.Repositories.Char.Petition
{
    using Context.Char;
    using Structures.Char;

    public class PetitionRepository : IPetitionRepository
    {
        private readonly CharContext _charContext;

        public PetitionRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        /// <summary>
        /// Reports the generated id rather than throwing. This runs inside a packet handler, so
        /// an exception here would cost the player their connection over a failed insert - and
        /// the client already has a "petition failed" message for exactly this case.
        /// </summary>
        public uint AddPetition(PetitionEntry entry)
        {
            try
            {
                _charContext.PetitionEntries.Add(entry);
                _charContext.SaveChanges();

                return entry.Id;
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, "Error adding Petition:");
                Logger.WriteLog(LogType.Error, e);
                return 0;
            }
        }
    }
}
