using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// The final client's mission contexts become per-squad instances (content_map_setting instancing 2,
    /// MapInstancing.PerSquad): every map_info context whose final-client gamecontext row has type 5
    /// (MISSIONCONTEXT), except the boot camp 1985, which stays per-character (BootcampS3PerCharacterInstancing,
    /// OD-2).
    ///
    /// Why: every map but the boot camp was one shared channel, so squads met strangers inside the "private"
    /// operations and shared their mission objects (segment-3 audit SEG3-INSTANCING).
    ///
    /// Evidence (tier original unless noted; research/20260926-instancing-mechanics):
    ///   client 1.16.5.0 game/generated/client/gamecontexttype.pyo: MISSIONCONTEXT = 5, BATTLEFIELDCONTEXT = 4;
    ///   gamecontext.pyo lookup[ctx][1] is the type (offsets per row below); gamecontexttypelanguage 5 'Mission';
    ///   trpython/client/ui/wonkavatorwindow.pyo Init (line 102): the loading screen shows its 'Instance' widget for
    ///   MISSIONCONTEXT (and WARGAMECONTEXT) and 'Persistent' for BATTLEFIELDCONTEXT;
    ///   trpython/client/inputstate/wonkavator.pyo OnExitState (lines 111-112): entering a MISSIONCONTEXT other than
    ///   1985 posts tutorial INSTANCE_ENTERED.
    ///   Dated corroboration: TaRapedia 'Operation' rev 32773 (2008-09-04) "for an instanced zone the server creates
    ///   an identical copy of the zone for each party that enters it" (observed); TaRapedia Category:Instances lists
    ///   48 of these 53 contexts by name (column below).
    ///
    /// Not changed: type-5 client contexts without a map_info row (1498, 1774, 1881, 2200 runivalley, 2359 are test
    /// or unloaded maps), and 2375 'Empire Sector: The Last Stand', which the client types 4 (a shared battlefield map
    /// "for endgame event", run in numbered copies: MapChannelManager shared copies, OD-127).
    /// Both provider migrations call this class. Never edit after release.
    /// </summary>
    public static class MissionContextSquadInstancingRows
    {
        public const string Migration = "MissionContextSquadInstancing";
        public const byte PerSquad = 2;

        public sealed class Row
        {
            public readonly uint MapContextId;
            public readonly string Comment;

            public Row(uint mapContextId, string comment)
            {
                MapContextId = mapContextId;
                Comment = comment;
            }
        }

        public static readonly Row[] Rows =
        {
            new Row(1347u, "Minos Caverns: per-squad instance (type 5)"), // 'Minos Caverns' gamecontextlanguage @88; gamecontext @778 type 5, levels 11-13; TaRapedia Category:Instances: Minos Caverns
            new Row(1348u, "Timora Mines: per-squad instance (type 5)"), // 'Timora Mines' gamecontextlanguage @97; gamecontext @787 type 5, levels 13-16; TaRapedia Category:Instances: Timora Mines
            new Row(1349u, "Torcastra Prison: per-squad instance (type 5)"), // 'Torcastra Prison' gamecontextlanguage @106; gamecontext @796 type 5, levels 15-17; TaRapedia Category:Instances: Torcastra Prison
            new Row(1384u, "Warnet Caverns: per-squad instance (type 5)"), // 'Warnet Caverns' gamecontextlanguage @115; gamecontext @994 type 5, levels 17-19; TaRapedia Category:Instances: Warnet Caverns
            new Row(1394u, "Devil's Den: per-squad instance (type 5)"), // 'Devil's Den' gamecontextlanguage @124; gamecontext @1048 type 5, levels 19-20; TaRapedia Category:Instances: Devil's Den
            new Row(1397u, "Treeback Camp: per-squad instance (type 5)"), // 'Treeback Camp' gamecontextlanguage @133; gamecontext @1066 type 5, levels 20-21; TaRapedia Category:Instances: Treeback Camp
            new Row(1416u, "Guardian Prominence: per-squad instance (type 5)"), // 'Guardian Prominence' gamecontextlanguage @151; gamecontext @1210 type 5, levels 36-38; TaRapedia Category:Instances: Guardian Prominence
            new Row(1429u, "Turpis Refinery: per-squad instance (type 5)"), // 'Turpis Refinery' gamecontextlanguage @160; gamecontext @1273 type 5, levels 33-37; TaRapedia Category:Instances: Turpis Refinery
            new Row(1430u, "Pravus Research: per-squad instance (type 5)"), // 'Pravus Research' gamecontextlanguage @169; gamecontext @1282 type 5, levels 8-10; TaRapedia Category:Instances: Pravus Research Facility
            new Row(1451u, "Bane Supply Depot: per-squad instance (type 5)"), // 'Bane Supply Depot' gamecontextlanguage @178; gamecontext @1399 type 5, levels 37-38; TaRapedia Category:Instances: Bane Supply Depot
            new Row(1465u, "Lamna Armory: per-squad instance (type 5)"), // 'Lamna Armory' gamecontextlanguage @196; gamecontext @1471 type 5, levels 34-36; TaRapedia Category:Instances: Lamna Armory
            new Row(1502u, "Ustor Yard: per-squad instance (type 5)"), // 'Ustor Yard' gamecontextlanguage @223; gamecontext @1696 type 5, levels 30-31; TaRapedia Category:Instances: Ustor Yard
            new Row(1506u, "Caves of Donn: per-squad instance (type 5)"), // 'Caves of Donn' gamecontextlanguage @232; gamecontext @1723 type 5, levels 9-11; TaRapedia Category:Instances: Caves of Donn
            new Row(1694u, "Retread Caves: per-squad instance (type 5)"), // 'Retread Caves' gamecontextlanguage @241; gamecontext @1957 type 5, levels 32-35; TaRapedia Category:Instances: Retread Caves
            new Row(1700u, "Logos Research Facility: per-squad (type 5)"), // 'Logos Research Facility' gamecontextlanguage @250; gamecontext @2002 type 5, levels 36-38; TaRapedia Category:Instances: Logos Research Facility
            new Row(1721u, "Crater Lake Research Facility: per-squad (type 5)"), // 'Crater Lake Research Facility' gamecontextlanguage @268; gamecontext @2155 type 5, levels 7-10; TaRapedia Category:Instances: Crater Lake Research Facility
            new Row(1743u, "P'reo Das: per-squad instance (type 5)"), // 'P'reo Das' gamecontextlanguage @295; gamecontext @2299 type 5, levels 35-37; TaRapedia Category:Instances: P'reo Das
            new Row(1763u, "Live Target Pens: per-squad instance (type 5)"), // 'Live Target Pens' gamecontextlanguage @331; gamecontext @2407 type 5, levels 48-50; TaRapedia Category:Instances: Live Target Pens
            new Row(1773u, "Kardash Atta Colony: per-squad instance (type 5)"), // 'Kardash Atta Colony' gamecontextlanguage @358; gamecontext @2443 type 5, levels 21-22; TaRapedia Category:Instances: Kardash Atta Colony
            new Row(1803u, "Eloh Temples: per-squad instance (type 5)"), // 'Eloh Temples' gamecontextlanguage @367; gamecontext @2695 type 5, levels 18-22; TaRapedia Category:Instances: Eloh Temples
            new Row(1806u, "Purgas Station: per-squad instance (type 5)"), // 'Purgas Station' gamecontextlanguage @376; gamecontext @2722 type 5, levels 39-41; TaRapedia Category:Instances: Purgas Station
            new Row(1823u, "Sanctus Grotto: per-squad instance (type 5)"), // 'Sanctus Grotto' gamecontextlanguage @385; gamecontext @2857 type 5, levels 50-50; TaRapedia Category:Instances: Sanctus Grotto
            new Row(1830u, "Maligo Base: per-squad instance (type 5)"), // 'Maligo Base' gamecontextlanguage @394; gamecontext @2911 type 5, levels 32-34; TaRapedia Category:Instances: Maligo Base
            new Row(1865u, "Ojasa Atta Hive: per-squad instance (type 5)"), // 'Ojasa Atta Hive' gamecontextlanguage @403; gamecontext @3118 type 5, levels 25-27; TaRapedia Category:Instances: Ojasa Atta Hive
            new Row(1977u, "Magma Caverns: per-squad instance (type 5)"), // 'Magma Caverns' gamecontextlanguage @439; gamecontext @3829 type 5, levels 45-46; TaRapedia Category:Instances: Magma Caverns
            new Row(1988u, "Bane Conscription Facility: per-squad (type 5)"), // 'Bane Conscription Facility' gamecontextlanguage @457; gamecontext @3865 type 5, levels 42-43; TaRapedia Category:Instances: Bane Conscription Facility
            new Row(2029u, "Temporal Chamber: per-squad instance (type 5)"), // 'Temporal Chamber' gamecontextlanguage @493; gamecontext @4099 type 5, levels 36-40; TaRapedia Category:Instances: Temporal Chamber
            new Row(2034u, "Phanin Research Facility: per-squad (type 5)"), // 'Phanin Research Facility' gamecontextlanguage @502; gamecontext @4135 type 5, levels 23-25; TaRapedia Category:Instances: Phanin Research Facility
            new Row(2055u, "Indra Caverns: per-squad instance (type 5)"), // 'Indra Caverns' gamecontextlanguage @529; gamecontext @4270 type 5, levels 42-44; TaRapedia Category:Instances: Indra Caverns
            new Row(2084u, "Eloh Vale: per-squad instance (type 5)"), // 'Eloh Vale' gamecontextlanguage @538; gamecontext @4477 type 5, levels 20-22; TaRapedia Category:Instances: Eloh Vale
            new Row(2085u, "Comm Tower: per-squad instance (type 5)"), // 'Comm Tower' gamecontextlanguage @547; gamecontext @4486 type 5, levels 25-28; TaRapedia Category:Instances: Comm Tower
            new Row(2093u, "Brann Water Refinery: per-squad instance (type 5)"), // 'Brann Water Refinery' gamecontextlanguage @565; gamecontext @4540 type 5, levels 20-25; TaRapedia Category:Instances: Brann Water Refinery
            new Row(2103u, "Fault Lever: per-squad instance (type 5)"), // 'Fault Lever' gamecontextlanguage @583; gamecontext @4630 type 5, levels 50-50; TaRapedia Category:Instances: not listed
            new Row(2105u, "Quasso Station: per-squad instance (type 5)"), // 'Quasso Station' gamecontextlanguage @592; gamecontext @4648 type 5, levels 44-46; TaRapedia Category:Instances: Quasso Station
            new Row(2107u, "Energy Weapon Center: per-squad instance (type 5)"), // 'Energy Weapon Center' gamecontextlanguage @601; gamecontext @4666 type 5, levels 29-31; TaRapedia Category:Instances: Energy Weapon Center
            new Row(2110u, "Avernus Outpost: per-squad instance (type 5)"), // 'Avernus Outpost' gamecontextlanguage @610; gamecontext @4693 type 5, levels 41-44; TaRapedia Category:Instances: Avernus Outpost
            new Row(2111u, "Raksha Robotic Facility: per-squad (type 5)"), // 'Raksha Robotic Facility' gamecontextlanguage @619; gamecontext @4702 type 5, levels 25-27; TaRapedia Category:Instances: Raksha Robotic Facility
            new Row(2112u, "Rivasa Atta Colony: per-squad instance (type 5)"), // 'Rivasa Atta Colony' gamecontextlanguage @628; gamecontext @4711 type 5, levels 41-44; TaRapedia Category:Instances: Rivasa Atta Colony
            new Row(2115u, "Bane Fluxite Mines: per-squad instance (type 5)"), // 'Bane Fluxite Mines' gamecontextlanguage @637; gamecontext @4738 type 5, levels 27-28; TaRapedia Category:Instances: Bane Fluxite Mines
            new Row(2125u, "Tahrendra Base: per-squad instance (type 5)"), // 'Tahrendra Base' gamecontextlanguage @646; gamecontext @4819 type 5, levels 29-30; TaRapedia Category:Instances: Tahrendra Base
            new Row(2136u, "Velon Hollow: per-squad instance (type 5)"), // 'Velon Hollow' gamecontextlanguage @664; gamecontext @4900 type 5, levels 30-32; TaRapedia Category:Instances: Velon Hollow
            new Row(2138u, "Staal Junkyard: per-squad instance (type 5)"), // 'Staal Junkyard' gamecontextlanguage @673; gamecontext @4918 type 5, levels 39-41; TaRapedia Category:Instances: Staal Junkyard
            new Row(2141u, "Incurables Ward: per-squad instance (type 5)"), // 'Incurables Ward' gamecontextlanguage @682; gamecontext @4945 type 5, levels 38-40; TaRapedia Category:Instances: Incurables Ward
            new Row(2146u, "Chaukas Robotics Facility: per-squad (type 5)"), // 'Chaukas Robotics Facility' gamecontextlanguage @691; gamecontext @4981 type 5, levels 39-41; TaRapedia Category:Instances: Chaukas Robotics Facility
            new Row(2155u, "Ruins of Tampeii: per-squad instance (type 5)"), // 'Ruins of Tampeii' gamecontextlanguage @709; gamecontext @5062 type 5, levels 44-48; TaRapedia Category:Instances: Ruins of Tampeii
            new Row(2156u, "The Refuge: per-squad instance (type 5)"), // 'The Refuge' gamecontextlanguage @718; gamecontext @5071 type 5, levels 47-48; TaRapedia Category:Instances: The Refuge
            new Row(2162u, "Cuthah Base: per-squad instance (type 5)"), // 'Cuthah Base' gamecontextlanguage @727; gamecontext @5125 type 5, levels 48-50; TaRapedia Category:Instances: Cuthah Base
            new Row(2163u, "Outpost Inferno: per-squad instance (type 5)"), // 'Outpost Inferno' gamecontextlanguage @736; gamecontext @5134 type 5, levels 46-48; TaRapedia Category:Instances: Outpost Inferno
            new Row(2190u, "Dybukkar Garrison: per-squad instance (type 5)"), // 'Dybukkar Garrison' gamecontextlanguage @754; gamecontext @5359 type 5, levels 50-50; TaRapedia Category:Instances: not listed
            new Row(2203u, "Section 5: Omega Labs: per-squad instance (type 5)"), // 'Section 5: Omega Labs' gamecontextlanguage @772; gamecontext @5467 type 5, levels 50-50; TaRapedia Category:Instances: Section 5: Omega Labs
            new Row(2278u, "The Gauntlet: per-squad instance (type 5)"), // 'The Gauntlet' gamecontextlanguage @799; gamecontext @6070 type 5, levels 50-50; TaRapedia Category:Instances: not listed
            new Row(2327u, "The Empire Sector: per-squad instance (type 5)"), // 'The Empire Sector' gamecontextlanguage @808; gamecontext @6511 type 5, levels 50-50; TaRapedia Category:Instances: not listed
            new Row(2368u, "Epic Caves of Donn: per-squad instance (type 5)"), // 'Epic Caves of Donn' gamecontextlanguage @835; gamecontext @6844 type 5, levels 50-50; TaRapedia Category:Instances: not listed
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var row in Rows)
                migrationBuilder.InsertData(
                    table: "content_map_setting",
                    columns: new[] { "map_context_id", "instancing", "comment" },
                    values: new object[] { row.MapContextId, PerSquad, row.Comment });
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var row in Rows)
                migrationBuilder.DeleteData(
                    table: "content_map_setting",
                    keyColumn: "map_context_id",
                    keyValue: row.MapContextId);
        }
    }
}
