using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// The Operations Mainframe and the Computer Access Terminal given a body the server can spawn.
    ///
    /// TarapediaMissingNpcs put them on UsableNPCCormanComputerV01 (7123), a console that carries the NPC
    /// augmentation and so can be spoken to - but not the Creature augmentation, which is what CreatureManager
    /// needs to build an actor: "Creature with dbId = 199512, don't have creature Augmentation" (2026-09-20).
    ///
    /// They now use NPC_Hominis_Machina (10642), which carries both and is the world's own talking machine. It is
    /// an analogue under OD-45: a machine that can be addressed, in place of the console the pages describe.
    /// </summary>
    public static class TarapediaMachineClassRows
    {
        public const string Migration = "TarapediaMachineClass";

        public const uint Mainframe = 199511u, Terminal = 199512u;
        public const uint Console = 7123u, Machine = 10642u;

        public static void InsertData(MigrationBuilder migrationBuilder) => Set(migrationBuilder, Machine);

        public static void DeleteData(MigrationBuilder migrationBuilder) => Set(migrationBuilder, Console);

        private static void Set(MigrationBuilder migrationBuilder, uint classId)
        {
            foreach (var id in new[] { Mainframe, Terminal })
                migrationBuilder.UpdateData(table: "creature", keyColumn: "id", keyValue: id, column: "class_id", value: classId);
        }
    }
}
