using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// Twelve active mission givers, receivers or objective speakers still use Redshirt classes
    /// without the original client's NPC augmentation (52). The Brann replacement shares its original mesh. The human
    /// replacement is the original NPC_Human_Swapset_Male class; its required outfit is an analogue
    /// copied from the preloaded AFS quartermaster appearance.
    /// </summary>
    public static class MissionSpeakerDialogueClassesRows
    {
        private static readonly uint[] Humans = { 103u, 104u, 115u, 120u, 125u, 132u, 137u, 138u, 140u };
        private static readonly uint[] Brann = { 199407u, 199506u, 199508u };

        private static readonly (uint Slot, uint Class, ulong Color)[] HumanAppearance =
        {
            (2u, 4021u, 4294934528uL), (13u, 6271u, 1uL),
            (14u, 9781u, 4278655809uL), (15u, 4023u, 4294934528uL),
            (16u, 4022u, 4294934528uL), (17u, 24008u, 4286690539uL)
        };

        private static readonly (uint Slot, uint Class, ulong Color)[] BrannAppearance =
        {
            (2u, 7259u, 4286611584uL), (3u, 7258u, 4286611584uL),
            (15u, 7256u, 4286611584uL), (16u, 7257u, 4286611584uL),
            (17u, 7690u, 1uL)
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var id in Humans)
                SetSpeaker(migrationBuilder, id, 29423u, 3846u, HumanAppearance);
            foreach (var id in Brann)
                SetSpeaker(migrationBuilder, id, 7253u, 7776u, BrannAppearance);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var id in Humans)
                RestoreSpeaker(migrationBuilder, id, 29423u, HumanAppearance);
            foreach (var id in Brann)
                RestoreSpeaker(migrationBuilder, id, 7253u, BrannAppearance);
        }

        private static void SetSpeaker(MigrationBuilder migrationBuilder, uint id, uint oldClass, uint npcClass,
            (uint Slot, uint Class, ulong Color)[] appearance)
        {
            migrationBuilder.Sql($"UPDATE creature SET class_id = {npcClass} WHERE id = {id} AND class_id = {oldClass}");
            foreach (var (slot, itemClass, color) in appearance)
                migrationBuilder.Sql($"INSERT INTO creature_appearance (id, slot_id, class_id, color) " +
                    $"SELECT id, {slot}, {itemClass}, {color} FROM creature WHERE id = {id} " +
                    $"AND NOT EXISTS (SELECT 1 FROM creature_appearance WHERE id = {id} AND slot_id = {slot})");
        }

        private static void RestoreSpeaker(MigrationBuilder migrationBuilder, uint id, uint oldClass,
            (uint Slot, uint Class, ulong Color)[] appearance)
        {
            foreach (var (slot, itemClass, color) in appearance)
                migrationBuilder.Sql($"DELETE FROM creature_appearance WHERE id = {id} AND slot_id = {slot} " +
                    $"AND class_id = {itemClass} AND color = {color}");
            migrationBuilder.Sql($"UPDATE creature SET class_id = {oldClass} WHERE id = {id}");
        }
    }
}
