using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// The 51 NPCs TaRapedia documents that this world did not have.
    ///
    /// The 2026-09-21 sweep read the wiki's complete pre-shutdown history and found 165 NPC pages carrying a
    /// location table. Sixty of those NPCs had no creature here at all - they are not mission givers this branch
    /// reconstructed and left unplaced, they were simply never in the data. Fifty-two of them resolve to a map
    /// this world carries and to walkable ground on it; the other eight are listed at the end of this comment
    /// with the reason, and stay out.
    ///
    /// What is original, and what is not:
    ///
    ///   * <b>Name</b> is the client's own creature name table, matched on the wiki's page title. Every one of
    ///     the 52 resolves, and none of their name ids is already on a creature in this world.
    ///   * <b>X and Z</b> are the wiki's location table, each row carrying the revision id and date it was last
    ///     set on before the shutdown.
    ///   * <b>Y</b> is the navmesh floor under that x,z - the wiki's own Y is whole metres and is used only to
    ///     choose between levels, never to stand a body on.
    ///   * <b>Level, species, gender and faction</b> are the wiki's infobox.
    ///   * <b>Entity class</b> is an analogue under OD-45: the class this world already uses for that species and
    ///     gender - NPC_Human_Swapset_Male/Female, NPC_Corman_Swapset_Male, NPC_Forean_Unarmed,
    ///     NPC_Brann_Swapset_Male/Female, Bane_Thrax_Soldier_Rifle_Boss, Bane_Lightbender_Alternate,
    ///     Bane_Caretaker_Boss, NPC_Vehicle_AFS_Mech. No source records the class of any of them.
    ///   * <b>Appearance</b> is an analogue under OD-11, the same practice as ContentNpcAppearance: a swapset body
    ///     with no creature_appearance rows renders naked and headless, so each takes the set a shipped NPC of
    ///     that same class wears. The female Brann take the male Brann set, which is the only Brann set this
    ///     world holds.
    ///   * <b>Dialogue</b>: none. The wiki calls 30 of them mission givers, but those missions are not seeded
    ///     here, and a package no mission completes through would say nothing.
    ///
    /// Left out, with the reason:
    ///   * <b>Commander Figgins, Hermit, Jumna, Ranger Helka, Research Chemist Horsh, Retread Karl, Shaman Siva
    ///     and Warrior Veska</b>, whose pages name no zone that resolves to a map here.
    ///   * <b>Commander Elvers</b>, whose page puts him at Denzil's Caldera on the boot camp map. The camp is the
    ///     reconstructed D11 segment and what stands in it is the manifest's business, not a wiki page's;
    ///     Ellatha's pass left him out too, its entry reading "Spawns in 3 different spots" instead of a
    ///     coordinate. He needs an owner decision, not a migration.
    /// </summary>
    public static class TarapediaMissingNpcBatchRows
    {
        public const string Migration = "TarapediaMissingNpcBatch";

        private const byte Stationary = 1;

        /// <summary>The set a shipped NPC of each swapset class wears, copied as the analogue body (OD-11).</summary>
        private static readonly System.Collections.Generic.Dictionary<uint, uint[][]> Bodies = new()
        {
            // Field Sgt. Witherspoon, creature 101
            { 3846u, new[] { new[] { 3u, 4021u, 22120u }, new[] { 13u, 27120u, 1u }, new[] { 14u, 3672u, 1u },
                new[] { 15u, 4022u, 13933202u }, new[] { 16u, 4023u, 13933202u }, new[] { 17u, 20824u, 4286886614u } } },
            // General Supply Vendor Twin Pillars, creature 63
            { 3848u, new[] { new[] { 2u, 4021u, 266553u }, new[] { 14u, 25255u, 266553u }, new[] { 15u, 19296u, 266553u },
                new[] { 16u, 19250u, 266553u }, new[] { 17u, 24019u, 4286886614u } } },
            // AFS Officer Lt Wood, creature 121
            { 6339u, new[] { new[] { 1u, 26677u, 4286886614u }, new[] { 2u, 4021u, 4294934528u }, new[] { 3u, 26673u, 22120u },
                new[] { 15u, 4023u, 4294934528u }, new[] { 16u, 4022u, 4294934528u }, new[] { 17u, 24008u, 4286690539u } } },
            // Mechanic Raji Lasat, creature 510014
            { 7776u, new[] { new[] { 2u, 7259u, 4286611584u }, new[] { 3u, 7258u, 4286611584u }, new[] { 15u, 7256u, 4286611584u },
                new[] { 16u, 7257u, 4286611584u }, new[] { 17u, 7690u, 1u } } }
        };

        /// <summary>The male Brann set, for the two female Brann: it is the only Brann body this world holds.</summary>
        private static uint[][] BodyFor(uint entityClass)
            => Bodies.TryGetValue(entityClass == 7775u ? 7776u : entityClass, out var body) ? body : null;

        // id, comment, name id, entity class, faction, level, map, x, floor, z, provenance
        private static readonly object[][] Npcs =
        {
            new object[] { 199022u, "Researcher Endala Berish (Indra Caverns)", 9354u, 7775u, 1u, 43u, 1734u, 312.000, 324.944, 293.000, "Indra Pass/Indra Caverns, TaRapedia rev 35696 2008-11-17; Indra Pass is on the Ashen Desert map, whose floor matches the wiki y to 0.9 m; the Indra Caverns instance is 103 m below it" },
            new object[] { 199023u, "Shinichiro Ito (Ashoka Settlement)", 9272u, 3846u, 1u, 43u, 1734u, 556.000, 248.617, -320.000, "Ashen Desert/Ashoka Settlement, TaRapedia rev 32921 2008-09-10" },
            new object[] { 199024u, "Penumbra Administrator Alexander (Penumbra Headquarters)", 9231u, 3846u, 1u, 45u, 2028u, -601.400, 536.236, -256.500, "Abyss/Penumbra Headquarters, TaRapedia rev 36196 2009-02-27" },
            new object[] { 199025u, "Penumbra Operative Spence (Thrax Machina)", 9239u, 3846u, 1u, 45u, 2028u, -672.500, 544.236, -149.600, "Penumbra Headquarters/Thrax Machina, TaRapedia rev 36208 2009-02-27" },
            new object[] { 199026u, "Coordinator Vash (Nyxroq Post)", 4416u, 7776u, 1u, 30u, 1761u, 180.000, 240.350, -245.000, "Nyxroq Post/Nyxroq Post, TaRapedia rev 28434 2008-02-06" },
            new object[] { 199027u, "Munitions Expert Sal (Plains Post)", 9659u, 3846u, 1u, 25u, 1761u, 167.600, 231.758, -279.500, "Incline/Plains Post, TaRapedia rev 23597 2007-12-23" },
            new object[] { 199028u, "Salvager Thark (Ojasa Highlands)", 4410u, 7776u, 1u, 30u, 1761u, -806.000, 283.370, 353.000, "Incline/Ojasa Highlands, TaRapedia rev 24516 2007-12-29" },
            new object[] { 199029u, "Explosives Specialist John (Ojasa Atta Hive)", 9660u, 3846u, 1u, 26u, 1865u, 319.500, 82.340, 133.400, "Incline/Ojasa Atta Hive, TaRapedia rev 23628 2007-12-23" },
            new object[] { 199030u, "Commander Tumlinson (Mires)", 9950u, 3846u, 1u, 26u, 1759u, 542.000, 229.484, 797.000, "Torden/Mires, TaRapedia rev 17057 2007-11-29" },
            new object[] { 199031u, "Dedarrlik (Mires)", 8842u, 10321u, 0u, 33u, 1759u, -199.000, 203.043, 221.000, "Torden/Mires, TaRapedia rev 31115 2008-06-06" },
            new object[] { 199032u, "Defiler Jemmert (Mires)", 10626u, 10504u, 0u, 31u, 1759u, 348.000, 229.684, 638.000, "Torden/Mires, TaRapedia rev 35547 2008-11-08" },
            new object[] { 199033u, "Ista Kezever (Mires)", 9898u, 10504u, 0u, 30u, 1759u, -480.000, 231.853, 350.000, "Torden/Mires, TaRapedia rev 35556 2008-11-10" },
            new object[] { 199034u, "Phost'Benon (Mires)", 8840u, 10857u, 0u, 33u, 1759u, -282.000, 227.284, 71.000, "Torden/Mires, TaRapedia rev 31102 2008-06-05" },
            new object[] { 199035u, "Rohtik (Waypoint)", 8701u, 7776u, 1u, 26u, 1759u, -650.700, 219.284, -518.300, "Baylor Base/Waypoint, TaRapedia rev 25783 2008-01-05" },
            new object[] { 199036u, "Vilescorn (Mires)", 9894u, 10504u, 0u, 30u, 1759u, -820.000, 243.819, 70.000, "Torden/Mires, TaRapedia rev 36056 2009-02-15; the Mires floor, 13.8 m above the wiki y" },
            new object[] { 199037u, "Specialist Tran (Bane Fluxite Mines)", 9479u, 3848u, 1u, 28u, 2115u, -11.000, 144.524, -7.000, "Mires/Bane Fluxite Mines, TaRapedia rev 24483 2007-12-29" },
            new object[] { 199038u, "Lieutenant Jaru (Viro Relay Tower)", 5446u, 3846u, 1u, 24u, 1764u, 897.000, 427.924, -129.000, "Plains/Viro Relay Tower, TaRapedia rev 16378 2007-11-26" },
            new object[] { 199039u, "Lookout Scout Cohen (Irendas Penal Colony)", 10309u, 3846u, 1u, 30u, 1764u, -44.000, 437.724, -102.000, "Plains/Irendas Penal Colony, TaRapedia rev 28173 2008-01-31" },
            new object[] { 199040u, "Lookout Scout Thomas (Irendas Penal Colony)", 10331u, 3846u, 1u, 30u, 1764u, 247.000, 450.724, 55.000, "Plains/Irendas Penal Colony, TaRapedia rev 28180 2008-01-31" },
            new object[] { 199041u, "Overseer P'toryc (Geyser Chimney Basin)", 8769u, 10857u, 0u, 19u, 1764u, -40.000, 432.658, 188.000, "Plains/Geyser Chimney Basin, TaRapedia rev 16689 2007-11-27" },
            new object[] { 199042u, "Rijii (Waypoint)", 8715u, 7775u, 1u, 28u, 1764u, 14.600, 385.347, -629.000, "Brann LZ/Waypoint, TaRapedia rev 26031 2008-01-05; GUESS at the map: the zone reads Brann LZ, which no map here carries; the Plains floor is the only walkable ground at this x and z" },
            new object[] { 199043u, "Scout Leader Miolov (The Abyssal Zone)", 9316u, 7776u, 1u, 25u, 1764u, -704.000, 421.684, -434.000, "Plains/The Abyssal Zone, TaRapedia rev 15897 2007-11-22" },
            new object[] { 199044u, "Sergeant Major Ngyen (Irendas Penal Colony)", 6875u, 10442u, 1u, 21u, 1764u, 353.000, 432.935, -89.000, "Plains/Irendas Penal Colony, TaRapedia rev 16700 2007-11-27" },
            new object[] { 199045u, "Xeniol (Mount Hellas Outpost)", 5648u, 7776u, 1u, 20u, 1764u, -537.000, 441.724, 421.000, "Plains/Mount Hellas Outpost, TaRapedia rev 16709 2007-11-27" },
            new object[] { 199046u, "Researcher Kranix (Research Outpost Alpha)", 9012u, 7776u, 1u, 24u, 1773u, 6.000, 272.350, -215.000, "Kardash Atta Colony/Research Outpost Alpha, TaRapedia rev 17042 2007-11-28" },
            new object[] { 199047u, "Master Phanin (Phanin Research Facility)", 9032u, 7776u, 1u, 25u, 2034u, -107.000, -39.576, 80.000, "Plains/Phanin Research Facility, TaRapedia rev 24352 2007-12-28; the Phanin Research Facility instance, whose floor matches the wiki y to 0.4 m" },
            new object[] { 199049u, "Commander Gagarin (Foreas Base)", 10590u, 3846u, 1u, 50u, 1148u, -32.000, 115.918, 481.000, "Divide/Foreas Base, TaRapedia rev 33042 2008-09-12" },
            new object[] { 199050u, "Conscript Thull (Foreas Base)", 6940u, 10504u, 0u, 10u, 1148u, 116.300, 62.724, 443.000, "Divide/Foreas Base, TaRapedia rev 15161 2007-11-16; the wiki reads \"??\" for the level, so it takes the median level of the map, 10" },
            new object[] { 199051u, "Field Officer Hogan (Foreas Base)", 153u, 3846u, 1u, 15u, 1148u, -4.300, 116.724, 536.600, "Divide/Foreas Base, TaRapedia rev 15154 2007-11-16" },
            new object[] { 199052u, "Medical Officer Mayes (Foreas Base)", 3010u, 3848u, 1u, 13u, 1148u, -11.700, 116.724, 544.700, "Divide/Foreas Base, TaRapedia rev 15155 2007-11-16" },
            new object[] { 199053u, "Base Cmdr. Matlin (Cumbria Research Facility)", 9029u, 3848u, 1u, 30u, 1244u, -809.000, 138.026, 601.000, "Palisades/Cumbria Research Facility, TaRapedia rev 36080 2009-02-17; the wiki y of 1318 is not a height on this map; x and z land on the Palisades floor" },
            new object[] { 199054u, "Executor Gantic (Cumbria Weald)", 10602u, 10504u, 0u, 18u, 1244u, -60.000, 118.518, 597.000, "Palisades/Cumbria Weald, TaRapedia rev 35365 2008-10-25" },
            new object[] { 199055u, "Information Spec. Nye (Lake Elinor)", 194u, 3846u, 1u, 50u, 1244u, 331.000, 124.577, 522.600, "Palisades/Lake Elinor, TaRapedia rev 29366 2008-02-29; the wiki row reads 331, 522.6, 37; 522.6 is a z on this map, not a height, and 331/522.6 is 80 m from Watchman Hillenmeyer at Lake Elinor on ground of the same height" },
            new object[] { 199056u, "Soanji (Concordia Palisades)", 3014u, 26833u, 1u, 20u, 1244u, 42.000, 112.977, -25.000, "Palisades/Concordia Palisades, TaRapedia rev 30285 2008-04-14" },
            new object[] { 199057u, "Surveyor Savinelli (Cumbria Research Facility)", 9899u, 6339u, 1u, 10u, 1244u, -716.000, 138.116, 685.900, "Palisades/Cumbria Research Facility, TaRapedia rev 35829 2009-01-05; the wiki reads \"??\" for the level, so it takes the median level of the map, 10" },
            new object[] { 199058u, "Warden Ebra (Temple of the Proud Patriarch)", 2951u, 26833u, 1u, 20u, 1244u, -699.000, 194.446, -90.000, "Palisades/Temple of the Proud Patriarch, TaRapedia rev 16095 2007-11-24" },
            new object[] { 199059u, "Warden Tyne (Temple of the Bowed Patriarch)", 2950u, 26833u, 1u, 20u, 1244u, -725.100, 189.295, 49.100, "Palisades/Temple of the Bowed Patriarch, TaRapedia rev 29372 2008-03-01" },
            new object[] { 199060u, "Logos Mentor Ensine (Alia Caverns)", 8589u, 26833u, 1u, 19u, 1220u, 754.500, 295.464, 557.000, "Alia Das/Alia Caverns, TaRapedia rev 6089 2007-08-20" },
            new object[] { 199061u, "Captain Velns (Crater Lake Research Facility)", 8871u, 3848u, 1u, 10u, 1721u, 30.000, 139.163, 200.000, "Crater Lake Research Facility/Crater Lake Research Facility, TaRapedia rev 14545 2007-11-12" },
            new object[] { 199062u, "Daniel Corman (Crater Lake Research Facility)", 3103u, 6339u, 1u, 10u, 1721u, 20.000, 72.038, -10.000, "Crater Lake Research Facility, TaRapedia rev 14498 2007-11-12; the page's location row links Logos pages rather than a zone, and the coordinates are on the CLRF map" },
            new object[] { 199063u, "Elder Orivos (Gangus Outpost)", 10210u, 26833u, 1u, 50u, 2051u, -638.000, 199.724, -985.300, "Howling Maw/Gangus Outpost, TaRapedia rev 27292 2008-01-17" },
            new object[] { 199064u, "Agent Donovan (Temple of Paludos)", 10137u, 3846u, 1u, 36u, 1454u, -206.000, 227.812, 114.000, "Marshes/Temple of Paludos, TaRapedia rev 23697 2007-12-24" },
            new object[] { 199065u, "Elder Bargas (Temple of Paludos)", 8536u, 26833u, 1u, 36u, 1454u, -205.000, 230.002, 40.000, "Marshes/Temple of Paludos, TaRapedia rev 30082 2008-04-01" },
            new object[] { 199066u, "Caretaker Tendahl (Mount Reverance)", 8622u, 26833u, 1u, 34u, 1497u, -781.000, 442.483, 431.700, "Plateau/Mount Reverance, TaRapedia rev 31185 2008-06-11" },
            new object[] { 199067u, "Corporal \"Relay\" O'Brien (AFS Camp Resistance)", 8615u, 3846u, 1u, 31u, 1497u, -621.000, 441.802, -234.000, "Plateau/AFS Camp Resistance, TaRapedia rev 6094 2007-08-20" },
            new object[] { 199068u, "Corporal Wainwright (New Velon Village)", 6765u, 3846u, 1u, 32u, 1497u, 194.000, 354.108, 519.000, "Plateau/New Velon Village, TaRapedia rev 31749 2008-08-12" },
            new object[] { 199069u, "MP Lieutenant Parkman (Fort Defiance)", 8768u, 3846u, 1u, 31u, 1497u, -72.000, 404.002, 951.000, "Plateau/Fort Defiance, TaRapedia rev 6362 2007-08-22" },
            new object[] { 199070u, "Downed Prisoner (Maligo Base)", 8590u, 3848u, 1u, 34u, 1830u, 378.000, 1.040, -179.000, "Valverde/Maligo Base, TaRapedia rev 6130 2007-08-20; GUESS at the map: the zone reads Maligo Base and the instance floor is 23 m above the wiki y" },
            new object[] { 199071u, "Maligo Base Prisoner (Ustor Yard)", 9168u, 3848u, 1u, 30u, 1502u, 19.000, 177.874, 121.000, "Valverde/Ustor Yard, TaRapedia rev 23744 2007-12-24" },
            new object[] { 199072u, "Scout Ryan (The Snakepit)", 9848u, 3846u, 1u, 33u, 1304u, -972.000, 918.924, 362.000, "Pools/The Snakepit, TaRapedia rev 23712 2007-12-24" },
            new object[] { 199073u, "Supply Chief Riggins (The Snakepit)", 9385u, 3846u, 1u, 40u, 1304u, -967.000, 915.573, 400.000, "Pools/The Snakepit, TaRapedia rev 18558 2007-12-06" },
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var npc in Npcs)
            {
                var id = (uint)npc[0];

                migrationBuilder.InsertData(
                    table: "creature",
                    columns: new[] { "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed",
                        "walk_speed", "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8" },
                    values: new object[] { id, npc[1], npc[3], npc[4], npc[5], 555u, npc[2], 9u, 5u,
                        0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u });

                migrationBuilder.InsertData(
                    table: "content_placement",
                    columns: new[] { "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id",
                        "usable_kind", "pos_x", "pos_y", "pos_z", "rotation", "behavior", "initial_state", "alternate_state",
                        "alternate_state_condition_id", "windup_ms", "name_override_id", "hit_points", "restore_ms",
                        "fuse_ms", "loot_item_set_id", "respawn_ms", "present_condition_id", "usable_condition_id", "comment" },
                    values: new object[] { id, npc[6], (byte)1, id, 0u, 0u, (byte)0, npc[7], npc[8], npc[9], 0.0,
                        Stationary, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, $"{npc[1]} ({npc[10]})" });

                var body = BodyFor((uint)npc[3]);

                if (body == null)
                    continue;

                foreach (var piece in body)
                    migrationBuilder.InsertData(table: "creature_appearance", columns: new[] { "id", "slot_id", "Class_id", "color" },
                        values: new object[] { id, piece[0], piece[1], piece[2] });
            }
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var npc in Npcs)
            {
                var id = (uint)npc[0];
                var body = BodyFor((uint)npc[3]);

                if (body != null)
                    foreach (var piece in body)
                        migrationBuilder.DeleteData(table: "creature_appearance", keyColumns: new[] { "id", "slot_id" },
                            keyValues: new object[] { id, piece[0] });

                migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { id });
                migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { id });
            }
        }
    }
}
