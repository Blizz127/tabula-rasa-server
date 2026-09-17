# Mission-area audit — 2026-09-17

Every seeded mission, from the boot camp to the Incline, checked for whether it is handed out and turned in
where the original put it: does its giver stand in the world at all, on the map the mission belongs to, and does
the turn-in NPC stand where the mission text sends the player. The data read is the live `rasaworld.db` of
2026-09-17 (77 rows in `npc_mission`), the world seed's `spawnpool` table, the reconstructed `content_placement`
rows, the client's `map_info`/`map_link`, and for the reference the TaRapedia snapshot in
`/home/blizz/backups/rasa-net/research/20260912-new-character/wiki-latest.json` (each page dated) plus the
client's own navmeshes. Nothing here is guessed from memory of the game; where a source is community
transcription it is called that.

## How a mission is tied to a place

`npc_mission.giver_id` / `reciver_id` name creature rows. A creature is in the world only if a `spawnpool` slot
names it with a max count of at least 1 (the world seed, emulator-authored, 218 pools all on the Wilderness) or a
`content_placement` row of kind 1 instantiates it on a `map_context_id` (the reconstruction's own rows). Missions
are offered only by the creature whose `DbId` equals the giver (`MissionManager.AssignNpcMission`), and objective
conversations complete against the NPC's dialogue package (`npc_package.package_id`, or the placement's
`npc_package_id` when set - `CreatureManager` prefers the latter).

Zone → map: Wilderness 1220, Divide 1148, Palisades 1244, Pools 1304, Plateau 1497, Marshes 1454, Mires 1759,
Plains 1764, Incline 1761, boot camp 1985. The zone-to-zone `map_link` rows form two connected chains
(Wilderness ↔ Divide ↔ Palisades; Pools ↔ Marshes ↔ Plateau; Mires ↔ Incline ↔ Plains; the Ligo maps), joined by
the wormhole teleporters and the AFS arena links. Every mission map in the table is reachable.

## The table

| mission | label | level | giver → map | receiver → map | verdict |
| --- | --- | --- | --- | --- | --- |
| 1990 Initiation | boot camp | 1 | radio | McAllister 198500 → 1985 | ok |
| 1992 Gearing Up for Battle | boot camp | 1 | McAllister → 1985 | DeSimone 198504 → 1985 | ok |
| 1994 Capture the Flag | boot camp | 1 | DeSimone → 1985 | Youngblood 198505 → 1985 | ok |
| 1995 / 2005 Calling for Reinforcements | boot camp → Wilderness | 2 / 3 | Youngblood → 1985 | Rogers 198514 → 1220 | ok (the hand-off into Alia Das, as the footage shows) |
| 1526 Training Day | Wilderness | 4 | radio | Kincaid 198515 → 1220 | ok |
| 2010 / 2011 Getting It In Gear | Wilderness | 5 | radio | Caufield 132 → 1220 (pool 210) | ok |
| 321 Assemble With Lieutenant Perkins | Wilderness | 5 | unknown (0) | unknown (0) | recorded gap since 2026-09-13; not offered |
| 429 River Recon | Wilderness | 3 | Rogers **100 → never spawns** | Witherspoon **101 → never spawns** | **fixed** |
| 422 Miner Difficulties, 1392, 1393 Conscientious Objector Part Two | Wilderness | 5 | Rogers **100 → never spawns** | same | **fixed** |
| 431 Distress On The River | Wilderness | 5 | Witherspoon **101 → never spawns** | same | **fixed** |
| 1407 Too Close For Comfort | Wilderness | 5 | Moawi **38 → never spawns** | Solis 42 → 1220 (pool 184) | **fixed** |
| 421, 427, 430, 442, 444, 451, 549, 682, 698, 767, 771, 787, 836, 1069, 1390 | Wilderness | 5 | world-seed NPCs, each in a pool with count ≥ 1 on 1220 | same | ok |
| 332 Ammo Express, 382 Retrieval for Recon | Divide | 10 | Sebastian 199000 → 1148 | same | ok |
| 347 Cleansing the Toxins II | Divide | 10 | Horea 199001 → 1148 | same | ok |
| 796 Behind Closed Doors | Divide | 10 | Dawson 199002 → **1220** | Dawson (wiki: Agent Franz, Foreas Base prison) | **fixed** |
| 1743 Report to Liaison Noonan | Divide | 10 | Brice 199003 → **1220** | Brice (wiki: Noonan, Foreas Base) | **fixed** |
| 366, 367, 368, 411, 412, 413, 670, 1788, 1789 | Palisades | 15 | 199100-199107 → 1244 | same | ok |
| 887, 970, 1040, 2016 | Plateau | 20 | 199200-199202, 199205 → 1497 | same | ok |
| 1068 South Of The Border | Plateau | 20 | Washington 199203 → 1304 (wiki: Colonel Bosley, AFS Camp Resistance, Plateau) | Washington → 1304 (the Snakepit, Pools) | **fixed** |
| 1541 New Orders From The General | Plateau | 20 | Corman 199204 → 1304 (wiki: Bosley) | Corman → 1304 (Retread Camp, Pools) | **fixed** |
| 1673, 1863, 1868, 1904 | Marshes | 25 | 199300-199303 → 1454 | same | ok |
| 940, 955, 956, 969, 976, 977, 983, 1041, 1141 | Mires | 30 | 199400-199404, 199800-199802 → 1759 | same | ok |
| 1748 Report to Liaison Ridout | Mires → Plateau | 30 | Ridout 199405 → 1497 (wiki: Maddox, Fort Haroun, Mires) | Ridout → 1497 (Fort Defiance) | **fixed** |
| 640, 1063, 1112, 1118, 1119, 1122, 1125, 1183, 1186, 1310 | Plains | 35 | 199500-199503 → 1764 | same | ok |
| 1746 Report to Liaison Sage | Plains → Incline | 35 | Sage 199504 → 1761 (wiki: Repp, Irendas Penal Colony, Plains) | Sage → 1761 (Plains Post) | **fixed** |
| 1747 Report to Liaison Maddox | Incline → Mires | 35 | Maddox 199600 → 1759 (wiki: Sage, Plains Post) | Maddox → 1759 | **fixed** |
| 1826 Helping Parsons | Incline | 35 | Otto 199601 → 1761 (wiki: Private Parsons, Plains Post) | Otto → 1761 (Ortho Post) | **fixed**; objectives 3 and 4 also needed Parsons (package 2327) and Lt. Gerry (2328), neither of whom existed |

The label column is the zone the mission's comment carries, which follows TaRapedia's mission infobox - the zone
the mission is *given* in. For the four "Report to Liaison" hand-offs that is the previous zone, and the liaison
who receives the report stands in the next one; the earlier batches placed the receivers correctly and only the
givers were missing.

## Findings

1. **Three givers never spawn.** `SpawnPoolManager.CreateListOfCreatures` draws `Random.Next(CountMin, CountMax + 1)`
   creatures per slot; the world seed's pools 64 (Council Elder Moawi, creature 38), 100 (Outpost Commander Rogers,
   100) and 101 (Field Sgt. Witherspoon, 101) have 0/0 and so spawn nobody. The Rogers case was known
   (`BootcampFixRogersTurnIn`, 2026-09-14, re-pointed 1995/2005 to the reserved Rogers 198514 and left creature 100
   alone) but 422, 429, 1392 and 1393 still named creature 100; 431 and 429's turn-in named 101; 1407 named 38. Six
   Wilderness missions could not be taken or finished. Pools 100 and 101 also sit at (870, 294, 386-388), which is
   Receptive Liaison Langerman's spot per TaRapedia, not either NPC's.
2. **Dawson and Brice stood on the wrong map.** Their TaRapedia pages say Zone=Wilderness in the infobox, and the Divide
   batch (2026-09-16) took the map from that field. The same pages' Location tables say Divide - "Foreas Base, in the
   back of the Base Hospital" (19.0, 116.9, 524.9) and "Nidu Dav, up the hill near the teleporter pad" (-773.7,
   179.3, 615.9) - and the client's navmeshes settle it: on `adv_foreas_concordia_divide` the ground under those
   points is 117.00 and 179.42, within 0.12 m of the readings; on the Wilderness there is no walkable surface within
   reach of either, which is what `WorldPositionAuditTests` had been excusing as "navmesh coverage" since
   2026-09-16. Nidu Dav is 200 m from the Divide's western portal to the Wilderness (map_link 1 at -965.8, 634.9), so
   the infobox zone is an understandable slip. Brice also carried package 2051, which is Noonan's (1743/3 "Report to
   Receptive Liaison Noonan" completes through it); his own is 2050 (1742/2 "Report to Liaison Brice").
3. **Eight cross-zone missions had giver = receiver.** With no giver NPC seeded, the zone batches set the giver to the
   receiver, so the Plateau's "South Of The Border" was handed out in the Pools by the man it delivers to, and
   "Report to Liaison Ridout" by Ridout. TaRapedia's mission pages (dated 2007-11 to 2008-09) name every giver:
   Brice → Noonan (1743), Repp → Sage (1746), Sage → Maddox (1747), Maddox → Ridout (1748), Bosley → Washington
   (1068), Bosley → Corman (1541), Dawson → Franz (796), Parsons → Otto (1826). Two of those givers already exist;
   six did not.
4. **Everything else lines up.** 61 of the 77 missions have a giver and receiver that spawn, on the map their zone
   names, and (where TaRapedia gives a location) at that location; the systematic comparison of every
   TaRapedia-placed NPC's Location-table zone against its placed map found only Dawson and Brice.

## What `MissionAreaLinks` changes (W3 batch 16)

- 422, 429, 1392, 1393: giver (and receiver where it was 100) → Rogers 198514, who carries the same package 116 the
  client binds those conversations to.
- Witherspoon: placement 198686 on 1220 at TaRapedia's /loc (501.5, 238.3, 214.0), Lower Eloh Creek by the teleporter
  (page dated 2008-09-16; Wilderness navmesh ground 238.60). Moawi: placement 198687 at the world seed's own pool-64
  position (812.0, 302.09, 505.20), beside Solis' pool 184 and matching TaRapedia's "a hut along the back of Alia
  Das" (ground 302.12). The seed pools are left as they are.
- Dawson 199002 and Brice 199003: `map_context_id` 1220 → 1148. Brice's package 2051 → 2050 on both `npc_package` and
  the placement.
- New NPCs, each from the client's `creaturenamelanguage` id, TaRapedia's level and /loc, and the OD-45 analogue
  class 3846 / 555 hp the earlier batches used: Receptive Liaison Noonan 199004 (Divide, package 2051; no /loc
  exists, so he stands at the client map's Foreas Base region label (-42, 479) with the navmesh's 116.5 - recorded as
  inferred, GAP-W3-NOONAN-POSITION; level is the Divide band, the page has no infobox), Agent Franz 199005 (Divide,
  Foreas Base prison (107.7, 59.0, 442.1), level 15, package 732), Colonel Bosley 199206 (Plateau, AFS Camp
  Resistance (-621, 442, -212), level 34, package 1001), Receptive Liaison Repp 199505 (Plains, Irendas Penal Colony
  by the wormhole (336, 430, -187), level 30, package 2026), Private Parsons 199602 (Incline, Plains Post tent (164,
  232, -278), level 25, package 2327), Lt. Gerry 199603 (Incline, Mires Post by the crates (-336, 222, -400), level
  25, package 2328). Every position was probed against its map's navmesh before seeding: ground within 0.6 m of the
  reading except Bosley (+1.05) and Gerry (+1.76), both inside the audit's 2 m surface tolerance, so the readings are
  kept as sourced like the other batches' /loc rows.
- Givers/receivers: 1743 receiver → Noonan; 1746 giver → Repp; 1747 giver → Sage; 1748 giver → Maddox; 1068 and 1541
  giver → Bosley; 796 receiver → Franz; 1826 giver → Parsons.

## Not established

- Dawson's own dialogue package. Nothing in the client's conversation rows names it (his only seeded mission
  completes through Franz's 732), so he keeps 732: talking to Dawson will also satisfy "Talk to Agent Franz"
  (GAP-W3-DAWSON-PACKAGE).
- Noonan's exact standing point (above).
- Whether the original offered "Report to Liaison X" from the liaison alone or also by radio on zone entry; the
  wiki pages record only the giver.
- 321 "Assemble With Lieutenant Perkins" still has no giver or receiver (unchanged, recorded 2026-09-13).
- The Incline missions carry level 35 (the batch's "zone band"); TaRapedia's NPC levels there are 25-32 and Plains
  Post is described as levels 20-25. Left as seeded; a level correction would be its own change with its own
  evidence.
