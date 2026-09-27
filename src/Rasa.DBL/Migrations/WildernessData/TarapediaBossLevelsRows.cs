using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// Twelve named bosses whose creature level TaRapedia records and whose world row, from InfiniteRasa's boss preloader
    /// (Add_boss_spawns, d717ed1), says something else. Upstream gave every boss of a zone one level with no source - 42
    /// for each Incline boss, 40 for Plains, 38 for Marshes, 35 for Pools - and the DIT reference
    /// (research/20260927-dit-tr-videos) flagged the Incline ones against the zone's range. The client carries no
    /// creature level anywhere (entityclass, creaturenamelanguage and gamecontext have none; the server levelled creatures
    /// at runtime); its gamecontext.pyo gives each map only the map window's suggested player range (fields 4/5, read
    /// by mapwindow as suggestedMinLevel/suggestedMaxLevel), e.g. Incline 24-27, Plains 21-25, Marshes 36-38, Pools 33-36.
    ///
    /// Each level is the one the boss's own TaRapedia page states, in every revision that states one, through the page's
    /// latest (research/20260927-dit-discrepancies/work/tarapedia/*.json, full MediaWiki histories fetched 2026-09-27):
    ///   * Incline, The Four Horsemen: 520024 Overseer Turquar 28, 520025 Taskmaster Puuk 30, 520026 Thrall Bruunk 26,
    ///     520027 Thrall Yquog 26 (each from 20779-20782, 2007-12-14; latest 24523-24566 and 31501, 2008-07-22).
    ///   * Plains: 520048 Overseer Quarm 28 (16805, 2007-11-27); 520049 Plains Devil Lord 25 ({{Boss|Level=25}}, 34918,
    ///     2008-10-10, latest 34922).
    ///   * Marshes: 520031 Daddy Long-Legs 42 (17260, 2007-11-30, through 33763, 2008-09-20); 520036 Seymour 42 (35629,
    ///     2008-11-11).
    ///   * Pools: 520056 Goriam 37, 520059 Krammitron 37, 520061 Orax 37, 520058 Irix 37 (each from 23347-23358,
    ///     2007-12-22; latest 24207 2007-12-27, 31522 2008-07-23, 35569 and 35566 2008-11-11).
    /// Tier inferred: a player-written level, not a capture, and seven of the twelve were last stated before D11
    /// (2008-08-15). No source for these zones records a later level change; the pages were re-edited after D11 without
    /// changing them (Daddy Long-Legs, Orax, Irix), and Plains Devil Lord's and Seymour's levels were first
    /// written after it. Bosses TaRapedia gives no level for keep upstream's
    /// value (GAP-UPSTREAM-BOSS-LEVELS). Pools' Cavalon (35) and the Mires bosses already carry their page's level.
    ///
    /// Only <c>creature.level</c> changes. <c>max_hp</c> stays upstream's 100 x old level + 500 (OD-172,
    /// GAP-UPSTREAM-BOSS-HP): no source records a boss's health. Palisades and Divide bosses are left to their own
    /// batches. Data only; DeleteData restores upstream's levels.
    /// </summary>
    public static class TarapediaBossLevelsRows
    {
        public const string Migration = "TarapediaBossLevels";

        public static readonly (uint Id, string Name, uint Was, uint Level, uint Map, string Page, uint FirstRevision, string FirstDate, uint LatestRevision, string LatestDate)[] Levels =
        {
            (520024u, "Overseer Turquar", 42u, 28u, 1761u, "Overseer Turquar", 20780u, "2007-12-14", 24523u, "2007-12-29"),
            (520025u, "Taskmaster Puuk", 42u, 30u, 1761u, "Taskmaster Puuk", 20781u, "2007-12-14", 24564u, "2007-12-30"),
            (520026u, "Thrall Bruunk", 42u, 26u, 1761u, "Thrall Bruunk", 20779u, "2007-12-14", 24565u, "2007-12-30"),
            (520027u, "Thrall Yquog", 42u, 26u, 1761u, "Thrall Yquog", 20782u, "2007-12-14", 31501u, "2008-07-22"),
            (520048u, "Overseer Quarm", 40u, 28u, 1764u, "Overseer Quarm", 16805u, "2007-11-27", 16805u, "2007-11-27"),
            (520049u, "Plains Devil Lord", 40u, 25u, 1764u, "Plains Devil Lord", 34918u, "2008-10-10", 34922u, "2008-10-10"),
            (520031u, "Daddy Long-Legs", 38u, 42u, 1454u, "Daddy Long-Legs", 17260u, "2007-11-30", 33763u, "2008-09-20"),
            (520036u, "Seymour", 38u, 42u, 1454u, "Seymour", 35629u, "2008-11-11", 35630u, "2008-11-11"),
            (520056u, "Goriam", 35u, 37u, 1304u, "Goriam", 23352u, "2007-12-22", 24207u, "2007-12-27"),
            (520058u, "Irix", 35u, 37u, 1304u, "Irix", 23354u, "2007-12-22", 35566u, "2008-11-11"),
            (520059u, "Krammitron", 35u, 37u, 1304u, "Krammitron", 23349u, "2007-12-22", 31522u, "2008-07-23"),
            (520061u, "Orax", 35u, 37u, 1304u, "Orax", 23346u, "2007-12-22", 35569u, "2008-11-11"),
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var boss in Levels)
                SetLevel(migrationBuilder, boss.Id, boss.Level);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var boss in Levels.Reverse())
                SetLevel(migrationBuilder, boss.Id, boss.Was);
        }

        private static void SetLevel(MigrationBuilder builder, uint id, uint level)
        {
            builder.UpdateData(table: "creature", keyColumn: "id", keyValue: id, column: "level", value: level);
            var update = (UpdateDataOperation)builder.Operations.Last();
            update.KeyColumnTypes = new[] { "INTEGER" };
            update.ColumnTypes = new[] { "INTEGER" };
        }
    }
}
