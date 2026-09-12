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
    }
}
