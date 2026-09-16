using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Data
{
    /// <summary>
    /// The three ways a control point can be owned (generated/client/controlpointownershiptype.pyo). This is the first
    /// field of a <c>controlpointdata</c> row and the <c>ownerTypeId</c> the client's colour rule
    /// (client/gameuiutil.py GetControlPointOwnerColorDef, line 2211) switches on.
    /// </summary>
    public enum ControlPointOwnershipType : uint
    {
        /// <summary>Owned by a clan: the owner id is a clan id (client/augmentations/clancontrolpoint.pyo).</summary>
        ClanOwned = 1,
        /// <summary>Owned by a battleground team: the owner id is a team id (constant/teamconstants: RED_TEAM 1, BLUE_TEAM 2).</summary>
        TeamOwned = 3,
        /// <summary>Owned by a faction: the owner id is a bool, True for the viewer's side (GetControlPointOwnerColorDef).</summary>
        FactionOwned = 6
    }

    /// <summary>
    /// generated/shared/controlpointtype.pyo. The challenge-board window compares a row's type against these to show a
    /// PvE or PvP icon; the window is dead code in the final client (see ControlPointData remarks), so the table is
    /// carried for completeness, and the live rows use <see cref="ControlPointOwnershipType"/> instead.
    /// </summary>
    public enum ControlPointType : uint
    {
        NoOwnership = 1,
        PveOwnership = 2,
        PvpOwnership = 3
    }

    /// <summary>
    /// The state ids a ControlPointStatus carries (shared/controlpointstatus.py lines 10-13: kCPState_New, _PreWar,
    /// _War, _PostWar).
    /// </summary>
    public enum ControlPointState : uint
    {
        New = 0,
        PreWar = 1,
        War = 2,
        PostWar = 3
    }

    /// <summary>
    /// The owner ids the client's OwnableControlPoint augmentation renders (client/augmentations/ownablecontrolpoint.py
    /// lines 20-32: OwnerIdToInterruptiblePkgId / OwnerIdToStatePkgId, keyed -1, 0, 1, 2). For a team-owned point the id
    /// is the team's: RED_TEAM is 1 and BLUE_TEAM is 2 (generated/client/constant/teamconstants.pyo), and the client
    /// dresses 1 in the Bane package and 2 in the AFS package. 0 is the "other clan owns" package, which is what a point
    /// nobody on either team holds shows; -1 selects no package at all. A new OwnableControlPoint starts at 1.
    /// </summary>
    public static class ControlPointOwner
    {
        public const int NoPackage = -1;
        public const int Neutral = 0;
        public const int RedTeam = 1;
        public const int BlueTeam = 2;

        /// <summary>shared/gameconstants.py lines 311-312: the virtual clan ids the clan variant compares a clan owner against.</summary>
        public const int VirtualClanAfs = -1;
        public const int VirtualClanBane = -2;
    }

    /// <summary>
    /// The per-player scoreboard tuple's indices (shared/scorekeeperconstants.py lines 2-11). ScoreBoardIndividualUpdate
    /// carries a tuple indexed by these; ScoreBoardTrackerUpdate carries only (kills, deaths).
    /// </summary>
    public enum ScoreKeeperField
    {
        UserName = 0,
        Class = 1,
        TeamId = 2,
        Active = 3,
        PvpCpKills = 4,
        PvpCpDeaths = 5,
        PvpCpDamage = 6,
        PvpCpHealing = 7,
        PvpCpCaptures = 8,
        PvpCpPrestige = 9
    }

    /// <summary>
    /// The client's own control-point table (generated/client/controlpointdata.pyo, 17 rows, decoded to
    /// generated_client_controlpointdata.pyo.json), with its five fields named from the code that reads it:
    /// client/gameuiutil.py GetControlPointLabel / GetShortControlPointLabel / SortControlPointList (lines 2228-2264)
    /// unpack a row as <c>(typeId, nameId, mapTemplateId, level, sortOrder)</c>.
    ///
    /// <list type="bullet">
    ///   <item><c>typeId</c> is a <see cref="ControlPointOwnershipType"/>: every live row is TEAM_OWNED (3).</item>
    ///   <item><c>nameId</c> is a <c>uielement</c> id whose text is in <c>uielementlanguage</c>: Whiskey, Charlie, Echo,
    ///   Blue Base, Red Base, and the two Edmund Range depots. The tracker shows "CP" + that text.</item>
    ///   <item><c>mapTemplateId</c> is a <c>maptemplate</c> id (a map name), not a game context; the client resolves it
    ///   with gamemap.GetContextIdForMapTemplateId, and here it is resolved by map name against the map table.</item>
    ///   <item><c>level</c> is the battleground's level, 50 for both live maps.</item>
    ///   <item><c>sortOrder</c> orders the tracker rows; the bases and depots have none and sort first.</item>
    /// </list>
    ///
    /// Twelve rows belong to the two final-live battlegrounds, <c>adv_wargame_provinggroundsv002</c> (context 2361) and
    /// <c>adv_wargame_edmundrange2</c> (context 2374); five are on test maps that were never a player's. The one
    /// battleground ruleset the final client knows is EDMUND_RANGE (generated/shared/battlegroundrulestype.pyo).
    ///
    /// The challenge-board window (client/ui/challengeboardwindow.pyo) also reads this table, but it expects a status
    /// with bids, an ownership field and a clanId that the final ControlPointStatus struct (shared/controlpointdefs.py)
    /// does not carry, references a kCPOwnership_Clan constant that no module defines, and nothing opens it: the clan
    /// bidding on control points it was written for was cut before shutdown, and the ControlPointBidStatus opcodes
    /// have no client handler.
    /// </summary>
    public static class ControlPointData
    {
        /// <summary>One control-point row, as the client carries it, plus the resolved name and map for readers.</summary>
        public readonly struct ControlPoint
        {
            public ControlPoint(uint id, ControlPointOwnershipType ownershipType, uint nameId, string name, uint mapTemplateId, string mapName, uint level, uint? sortOrder)
            {
                Id = id;
                OwnershipType = ownershipType;
                NameId = nameId;
                Name = name;
                MapTemplateId = mapTemplateId;
                MapName = mapName;
                Level = level;
                SortOrder = sortOrder;
            }

            public uint Id { get; }
            public ControlPointOwnershipType OwnershipType { get; }
            /// <summary>uielement id; the English text is <see cref="Name"/>.</summary>
            public uint NameId { get; }
            /// <summary>uielementlanguage English text for <see cref="NameId"/>, for readers and logs.</summary>
            public string Name { get; }
            /// <summary>maptemplate id; <see cref="MapName"/> is its maptemplate.lookup value.</summary>
            public uint MapTemplateId { get; }
            public string MapName { get; }
            public uint Level { get; }
            /// <summary>Tracker order; null for the bases and depots, which the client sorts ahead of the numbered points.</summary>
            public uint? SortOrder { get; }

            /// <summary>Whether the point is on a map a player could reach, rather than one of the five test-map rows.</summary>
            public bool IsLive => MapName.StartsWith("adv_", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>The 17 rows, in key order, exactly as controlpointdata.lookup holds them.</summary>
        public static readonly ControlPoint[] Rows =
        {
            //               id         typeId                              nameId  name (uielementlanguage)      mapTemplateId  maptemplate.lookup                  level  sortOrder
            new ControlPoint(1u,        ControlPointOwnershipType.TeamOwned, 5448u, "PVP Control Point",             2238u, "test_pvpcontrolpoint01",            1u,  null),
            new ControlPoint(2u,        ControlPointOwnershipType.TeamOwned, 4090u, "Title Test",                    1843u, "test_sean",                         1u,  null),
            new ControlPoint(3u,        ControlPointOwnershipType.TeamOwned, 6015u, "Echo",                          2365u, "adv_wargame_provinggroundsv002",   50u,  3u),
            new ControlPoint(4u,        ControlPointOwnershipType.TeamOwned, 6017u, "Whiskey",                       2365u, "adv_wargame_provinggroundsv002",   50u,  1u),
            new ControlPoint(5u,        ControlPointOwnershipType.TeamOwned, 6016u, "Charlie",                       2365u, "adv_wargame_provinggroundsv002",   50u,  2u),
            new ControlPoint(6u,        ControlPointOwnershipType.TeamOwned, 6021u, "Blue Base",                     2365u, "adv_wargame_provinggroundsv002",   50u,  null),
            new ControlPoint(7u,        ControlPointOwnershipType.TeamOwned, 6022u, "Red Base",                      2365u, "adv_wargame_provinggroundsv002",   50u,  null),
            new ControlPoint(8u,        ControlPointOwnershipType.TeamOwned, 6021u, "Blue Base",                     2377u, "adv_wargame_edmundrange2",         50u,  null),
            new ControlPoint(9u,        ControlPointOwnershipType.TeamOwned, 6022u, "Red Base",                      2377u, "adv_wargame_edmundrange2",         50u,  null),
            new ControlPoint(10u,       ControlPointOwnershipType.TeamOwned, 6016u, "Charlie",                       2377u, "adv_wargame_edmundrange2",         50u,  2u),
            new ControlPoint(11u,       ControlPointOwnershipType.TeamOwned, 6015u, "Echo",                          2377u, "adv_wargame_edmundrange2",         50u,  3u),
            new ControlPoint(12u,       ControlPointOwnershipType.TeamOwned, 6017u, "Whiskey",                       2377u, "adv_wargame_edmundrange2",         50u,  1u),
            new ControlPoint(13u,       ControlPointOwnershipType.TeamOwned, 6151u, "Control Point: East Depot",     2377u, "adv_wargame_edmundrange2",         50u,  null),
            new ControlPoint(14u,       ControlPointOwnershipType.TeamOwned, 6150u, "Control Point: West Depot",     2377u, "adv_wargame_edmundrange2",         50u,  null),
            new ControlPoint(10000001u, ControlPointOwnershipType.ClanOwned, 4090u, "Title Test",                    1656u, "a",                                 2u,  null),
            new ControlPoint(10000003u, ControlPointOwnershipType.TeamOwned, 1552u, "Let's get started, recruit.",   10000001u, "test_shuai_2",                  1u,  null),
            new ControlPoint(10000004u, ControlPointOwnershipType.TeamOwned, 1552u, "Let's get started, recruit.",   10000008u, "test_shuai_battleground_3",     1u,  null),
        };

        /// <summary>The one ruleset in generated/shared/battlegroundrulestype.pyo.</summary>
        public const uint BattlegroundRulesEdmundRange = 1;

        private static readonly Dictionary<uint, ControlPoint> ById = Rows.ToDictionary(row => row.Id);

        public static bool TryGet(uint controlPointId, out ControlPoint controlPoint) => ById.TryGetValue(controlPointId, out controlPoint);

        /// <summary>
        /// The rows for a map, by the map's name (the join the client makes through maptemplate.lookup and
        /// GetContextIdForMapTemplateId). Case-insensitive, since the map table's spelling of a name varies.
        /// </summary>
        public static IReadOnlyList<ControlPoint> ForMap(string mapName)
        {
            if (string.IsNullOrEmpty(mapName))
                return Array.Empty<ControlPoint>();

            return Rows.Where(row => string.Equals(row.MapName, mapName, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        /// <summary>The names of the maps that have control points a player could reach.</summary>
        public static IReadOnlyList<string> LiveMapNames => Rows.Where(row => row.IsLive).Select(row => row.MapName).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        /// <summary>
        /// client/gameuiutil.py SortControlPointList (line 2252): the tracker lists points by (sortOrder, cpId), where a
        /// missing sortOrder is Python's None and sorts before every number.
        /// </summary>
        public static IReadOnlyList<uint> SortForTracker(IEnumerable<uint> controlPointIds)
        {
            return controlPointIds
                .Select(id => (order: TryGet(id, out var row) ? row.SortOrder : null, id))
                .OrderBy(entry => entry.order.HasValue ? 1 : 0)
                .ThenBy(entry => entry.order ?? 0)
                .ThenBy(entry => entry.id)
                .Select(entry => entry.id)
                .ToArray();
        }
    }
}
