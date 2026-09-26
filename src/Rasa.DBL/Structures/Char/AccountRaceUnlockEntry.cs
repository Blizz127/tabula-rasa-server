using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    [Table("account_race_unlock")]
    public class AccountRaceUnlockEntry
    {
        [Column("account_id")]
        public uint AccountId { get; set; }

        [Column("race_id")]
        public byte RaceId { get; set; }
    }
}
