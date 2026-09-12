using System.Collections.Generic;

namespace Rasa.Repositories.Char.Petition
{
    using Structures.Char;

    public interface IPetitionRepository
    {
        /// <summary>
        /// Writes the petition and returns the generated id, or 0 when the row could not be
        /// written. The id is what the client is acknowledged with.
        /// </summary>
        uint AddPetition(PetitionEntry entry);

        /// <summary>The petition with this id, or null when there is none.</summary>
        PetitionEntry GetPetition(uint id);

        /// <summary>
        /// Newest first, capped. A null status means every status - the console defaults to
        /// open ones, because a server that has been up a while has far more closed than open.
        /// </summary>
        List<PetitionEntry> ListPetitions(byte? status, int limit);

        /// <summary>
        /// Moves a petition to a new status, and reports whether the row was there to move.
        /// Resolution is stored alongside so the reason travels with the state.
        /// </summary>
        bool SetPetitionStatus(uint id, byte status, string resolution);
    }
}
