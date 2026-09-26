using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// The Clone Credit token (item template 111219, class 26491 "Clone Credit", augmentation clonecredit) cannot be
    /// traded, as the final-era wiki records it. What the token does is code (ManifestationManager.RequestUseCloneCredit);
    /// this is the one table value its evidence changes.
    ///
    /// <b>Evidence.</b> TaRapedia "Clone Credit", revision 31080 (2008-06-03), section Clone Credit Token: "They are, at
    /// the moment, not able to be traded. This is an option slated to be added to the game, but no time-frame has been
    /// announced." The text stands unchanged at the page's final revision 35135 (Zarevak, 2008-10-16, "image format
    /// change"), and no later official note (the archived live notes of D9 to D15.7, 2008-06 to 2008-12) makes the token
    /// tradable. Tier: observed (research/20260926-clone-credits/work/tarapedia/pages/Clone_Credit.json).
    ///
    /// <b>What it replaces.</b> Regenerate_item_template gave all 30225 templates the same flags - sellable, the other six
    /// clear - because neither of its sources varied them (ItemTemplateRegeneratedPreloader). The 0 was that placeholder,
    /// not a reading. Only not_tradable_flag changes: the page says nothing of selling, binding or the footlocker, so
    /// has_sellable_flag and not_placable_in_lockbox_flag keep their placeholders (GAP-CLONE-TOKEN-FLAGS).
    /// </summary>
    public static class CloneCreditNotTradableRows
    {
        public const string Migration = "CloneCreditNotTradable";

        public const uint CloneCreditTemplate = 111219u;
        public const byte NotTradable = 1, NotTradableWas = 0;

        // {"table": "itemtemplate", "id": 111219} observed: not_tradable_flag (TaRapedia Clone Credit rev 31080)
        public static void InsertData(MigrationBuilder migrationBuilder) => Flag(migrationBuilder, NotTradable);

        public static void DeleteData(MigrationBuilder migrationBuilder) => Flag(migrationBuilder, NotTradableWas);

        private static void Flag(MigrationBuilder migrationBuilder, byte value)
            => migrationBuilder.UpdateData(table: "itemtemplate", keyColumn: "id", keyValue: CloneCreditTemplate,
                column: "not_tradable_flag", value: value);
    }
}
