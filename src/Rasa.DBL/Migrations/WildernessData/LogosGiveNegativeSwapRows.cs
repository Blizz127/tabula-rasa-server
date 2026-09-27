using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// The Divide's Give (logos 15) and Negative (logos 33) shrines stood on each other's pedestals. The rows carried the
    /// right stone class for their name (15 UsableItemDispElohLogosGiveV01 7296, 33 UsableItemDispElohLogosNegativeV01 12691),
    /// so the glyph matched the Logos a player received; only the place was wrong. The world seed put Give on the hilltop
    /// north of Foxtrot Outpost and Negative in the Xanx cave of Benefactor Valley. The DIT bot project's reference
    /// (research/20260927-dit-tr-videos, ref/logos.json) reported it; checked here against primary evidence.
    ///
    /// <b>Evidence.</b>
    ///   * Client map (original): adv_foreas_concordia_divide.map (1.16.5.0, sha256 f73142fc...) has an
    ///     ArchElohLogosDispenserBase pedestal under each row: (184.09, 120.22, 137.75), outdoors among Forean torches, wicker
    ///     and firebugs, and (745.24, 97.85, -5.21), inside a cavern - TerraForeasCavern stalagmites, roots, crystals and
    ///     ceiling dust within 11-25 m and a WaterForeasPond32mDirty pool 10 m away. The map names no stone for either.
    ///   * TaRapedia (inferred): "Give (logos)" puts Give at (746, 98, -4), "within the upper part of the Xanx cave",
    ///     Benefactor Valley, in every revision from 9153 (2007-09-04) to 34470 (2008-09-29); the mission page's walkthrough
    ///     has "one path (to the right) leads down into a pool" and the harder left path "up to get this Logos". "Negative
    ///     (logos)" puts Negative at (184, 121, 138), "North of Foxtrot Outpost", from 11493 (2007-10-20, as 183.6, 120.5,
    ///     139.5) to 34480 (2008-09-29). Both pages carry Verified = 2007-12-31 (1.3.2.3).
    ///   * Giddy Gamer logos_alpha.pdf rev 2007/11/26 (inferred, independent transcription): "Give Con Div 700 85 65 take an
    ///     army if &lt;15 ... S++", "Negative Con Div 185 120 150 hilltop east of trenches ... S".
    ///   * Against them, Ellatha's Divide logos map (undated; fetched 2026-09-27) lists Give at (182, 137) and Negative at
    ///     (742, 8.8), the world seed's assignment, which the 2023 seed probably copied. TaRapedia explains it: from rev
    ///     21379 (2007-12-16) the Negative page carried {{bug|The shrine displays symbol for Logos element Give}}, re-checked
    ///     in rev 24877 (2008-01-01, "ver, bug still present") and removed in rev 32061 (2008-08-24, "wrong displayed logos
    ///     symbol bug was fixed"). For eight months the hilltop shrine showed Give's glyph while granting Negative, so a
    ///     list that names shrines by their glyph puts Give there. The fixed state is the one the shutdown had.
    /// Two independent 2007-08 sources, the bug history and the map's own geometry (a cave pool at one pedestal, a hilltop
    /// at the other) agree; nothing records either shrine moving before the shutdown. The swap is therefore corrected:
    /// each row takes the other's position exactly, the pedestal-top height the world seed already carried, and keeps
    /// its own stone class, so each glyph is the post-fix one. Coordinates original (client pedestals), assignment
    /// inferred. The residual - no final-era capture shows either shrine - is GAP-DIVIDE-GIVE-NEGATIVE-FINAL.
    ///
    /// Missions bind shrines by logos id (1646 Logos: Give, LiaisonLogosMissions), so no mission row changes.
    /// Data only; DeleteData puts both rows back where the world seed had them.
    /// </summary>
    public static class LogosGiveNegativeSwapRows
    {
        public const string Migration = "LogosGiveNegativeSwap";

        public const uint Give = 15, Negative = 33, Divide = 1148;

        /// <summary>The world seed's positions (LogosPreloader), each on the other Logos' pedestal.</summary>
        public static readonly (double X, double Y, double Z) HilltopPedestal = (184.17969, 122.953125, 137.64453);
        public static readonly (double X, double Y, double Z) CavePedestal = (745.16797, 100.58594, -5.2539062);

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            Move(migrationBuilder, Give, CavePedestal);
            Move(migrationBuilder, Negative, HilltopPedestal);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            Move(migrationBuilder, Negative, CavePedestal);
            Move(migrationBuilder, Give, HilltopPedestal);
        }

        private static void Move(MigrationBuilder builder, uint id, (double X, double Y, double Z) to)
        {
            builder.UpdateData(table: "logos", keyColumns: new[] { "id" }, keyValues: new object[] { id },
                columns: new[] { "pos_x", "pos_y", "pos_z" }, values: new object[] { to.X, to.Y, to.Z });
            var update = (UpdateDataOperation)builder.Operations.Last();
            update.KeyColumnTypes = new[] { "INTEGER" };
            update.ColumnTypes = new[] { "REAL", "REAL", "REAL" };
        }
    }
}
