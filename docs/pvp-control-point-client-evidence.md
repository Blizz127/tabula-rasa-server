# PvP control points and battlegrounds: what the final client carries

Inspected 2026-09-16 for the final-live preservation target. Source is the archived 1.16.5.0 client's original
Python 2.4 bytecode (`trpython.zip`, `data/game.zip`), read statically with xdis; no game module was imported or
executed. Disassembly output is kept outside Git under
`/home/blizz/backups/rasa-net/research/20260915-aliadas-hub/work/controlpoint-dis/`, and the decoded tables under
`/home/blizz/backups/rasa-net/research/20260913-bootcamp/list-tables/decoded/`.

This closes the gap recorded on 2026-09-16 as `GAP-W3-PVP-CONTROL-POINTS`: the five fields of a `controlpointdata`
row are now named from the code that reads them, and the whole client-side protocol of the battleground control
points is recovered. What the client cannot say - where the points stand, how they are captured, how a battleground
runs - is recorded as gaps at the end.

## 1. The control-point table, decoded

`data/game.zip::generated/client/controlpointdata.pyo` (SHA-256
`05e80d280a79fe0a34d9d90716eda969da3f7ea9e01d3e297d43e724d3e77f5e`, timestamp 1234238129 = 2009-02-10 UTC) holds
`lookup`, 17 rows of 5-tuples. The readers are in `trpython.zip::client/gameuiutil.pyo`
(`0a6543c60a39f8be64040c598216d606b0c211f9411dff96308b1deaff17df7b`): `GetControlPointLabel` (source line 2228),
`GetShortControlPointLabel` (2240) and `SortControlPointList` (2252) each do
`UNPACK_SEQUENCE 5` into **`typeId, nameId, mapTemplateId, level, sortOrder`**. The same unpacking, with the same
names, is in `client/ui/challengeboardwindow.pyo` `_UpdateListWidgets` (line 419).

- `typeId` is compared against `generated/client/controlpointownershiptype.pyo`
  (`6a880630584549cf091beb329d6243b63df70087af64460375f211cd0280b614`): `CLAN_OWNED` 1, `TEAM_OWNED` 3,
  `FACTION_OWNED` 6 (gameuiutil `GetControlPointOwnerColorDef`, line 2211). Every live row is `TEAM_OWNED`.
- `nameId` is a `uielement` id; `GetControlPointLabel` builds `"CP " + uielementlanguage[nameId]`. This is why the
  earlier reading of the field as a physical entity class resolved to nothing.
- `mapTemplateId` is a `generated/client/maptemplate.pyo` key
  (`f8f0f04e533366612e5fac90878593375780e4e8d9066d0dbf8e4f2420725dd8`; id → map name). The challenge board resolves
  it with `client.gamemap.GetContextIdForMapTemplateId`; `generated/client/gamecontext.pyo` row field 4 is the map
  template of a context, which gives the join below. This is why the field is not one of the 78 map contexts.
- `level`: the battleground's level.
- `sortOrder`: the tracker's row order; `SortControlPointList` sorts `(sortOrder, cpId)` tuples, so rows whose
  order is `None` sort first.

| cpId | typeId | nameId | uielementlanguage (English) | mapTemplateId | maptemplate name | context | level | sortOrder |
| ---: | --- | ---: | --- | ---: | --- | ---: | ---: | ---: |
| 1 | TEAM_OWNED | 5448 | PVP Control Point | 2238 | test_pvpcontrolpoint01 | - | 1 | None |
| 2 | TEAM_OWNED | 4090 | Title Test | 1843 | test_sean | 1843 | 1 | None |
| 3 | TEAM_OWNED | 6015 | Echo | 2365 | adv_wargame_provinggroundsv002 | 2361 | 50 | 3 |
| 4 | TEAM_OWNED | 6017 | Whiskey | 2365 | adv_wargame_provinggroundsv002 | 2361 | 50 | 1 |
| 5 | TEAM_OWNED | 6016 | Charlie | 2365 | adv_wargame_provinggroundsv002 | 2361 | 50 | 2 |
| 6 | TEAM_OWNED | 6021 | Blue Base | 2365 | adv_wargame_provinggroundsv002 | 2361 | 50 | None |
| 7 | TEAM_OWNED | 6022 | Red Base | 2365 | adv_wargame_provinggroundsv002 | 2361 | 50 | None |
| 8 | TEAM_OWNED | 6021 | Blue Base | 2377 | adv_wargame_edmundrange2 | 2374 | 50 | None |
| 9 | TEAM_OWNED | 6022 | Red Base | 2377 | adv_wargame_edmundrange2 | 2374 | 50 | None |
| 10 | TEAM_OWNED | 6016 | Charlie | 2377 | adv_wargame_edmundrange2 | 2374 | 50 | 2 |
| 11 | TEAM_OWNED | 6015 | Echo | 2377 | adv_wargame_edmundrange2 | 2374 | 50 | 3 |
| 12 | TEAM_OWNED | 6017 | Whiskey | 2377 | adv_wargame_edmundrange2 | 2374 | 50 | 1 |
| 13 | TEAM_OWNED | 6151 | Control Point: East Depot | 2377 | adv_wargame_edmundrange2 | 2374 | 50 | None |
| 14 | TEAM_OWNED | 6150 | Control Point: West Depot | 2377 | adv_wargame_edmundrange2 | 2374 | 50 | None |
| 10000001 | CLAN_OWNED | 4090 | Title Test | 1656 | a | - | 2 | None |
| 10000003 | TEAM_OWNED | 1552 | Let's get started, recruit. | 10000001 | test_shuai_2 | 10000001 | 1 | None |
| 10000004 | TEAM_OWNED | 1552 | Let's get started, recruit. | 10000008 | test_shuai_battleground_3 | 10000008 | 1 | None |

Twelve rows are the two final-live battlegrounds; the other five are on test maps. Both live contexts are game
context type 4 (`BATTLEFIELDCONTEXT`) with level band 45-50 in `gamecontext` fields 5-6, and the emulator's map
table has them as 2361 `adv_wargame_provinggroundsv002` and 2374 `adv_wargame_edmundrange2`. The one battleground
ruleset the final client knows is `generated/shared/battlegroundrulestype.pyo`
(`3e3ab504b9ad3cc8be4b0ad6f387ff9bd85fd35d7131f11e65ead904584c3271`): `EDMUND_RANGE` = 1, referenced only by the
`GBB_BattlegroundRulesController` entity class.

Emulator: `src/Rasa.Game/Data/ControlPointData.cs` carries the rows with these names and the English text;
`ControlPointDataTests` pins them.

## 2. The status struct and its states

`trpython.zip::shared/controlpointdefs.pyo` (`762cffe0d3acf089ff5db9db79774e54b6b9675f3144cc0bd8e800dea50e617c`),
source lines 10-11:

```
Struct('ControlPointStatus', (Field('controlPointId', IntType, None, False),
                              Field('ownerId', LongType, None, True),
                              Field('stateId', IntType, None, False),
                              Field('endTime', IntType, None, False)))
```

The fourth Field argument is nullability: `ownerId` may be `None`. `shared/controlpointstatus.pyo`
(`054c55d7f8c04994393a24760c4fbecafad2828e004b3cbfb9a28ff026c56e21`), lines 10-13: `kCPState_New` 0,
`kCPState_PreWar` 1, `kCPState_War` 2, `kCPState_PostWar` 3.

Emulator: `Structures/ControlPointStatus.cs` now has a nullable `OwnerId` and a typed `StateId`; the previous form
wrote the owner as an unsigned int and could not send `None`.

## 3. The protocol the client handles

From `client-protocol-inventory.json` (every `Recv_*` with its argument list) and the disassembly:

| Method id | Name | Handler | Arguments | Notes |
| ---: | --- | --- | --- | --- |
| 817 | RequestControlPointStatus | client → server | `()` | `controlpointmanager.RequestControlPointStatus`, line 63: `SendCallActorMethod('RequestControlPointStatus', ())` |
| 814 | ControlPointStatus | `client/controlpointmanager.pyo` `Recv_ControlPointStatus(statusList)`, line 72 | one list of ControlPointStatus tuples | stored by `controlPointId`; posts `UI_CONTROL_POINT_STATUS_UPDATED`. Sent to `ClientControlPointManagerId` (28) |
| 884 | SetOwnerId | `client/augmentations/ownablecontrolpoint.pyo` `Recv_SetOwnerId(ownerId)`, line 60 | one int | swaps the point's state FX package for the owner |
| 783 | PvPEnabled | `clancontrolpoint.pyo` `Recv_PvPEnabled(isPvPEnabled)` | bool | clan variant only |
| 810 | AllControlPointStatus | none | - | no handler in the final client |
| 812, 813 | ControlPointBidStatus, ...Failed | none | - | no handler in the final client |
| 816 | RequestControlPointBidStatus | client → server | - | nothing sends it in the final client |

`Recv_ControlPointStatus` iterates `statusList` and unwraps each entry with
`stuple.Wrap.FromNetworkFormat('ControlPointStatus', statusData)`. **The emulator's `ControlPointStatusPacket`
wrote one bare struct with no argument tuple**, which the client would have unpacked as four positional arguments to a
one-argument handler. It now writes `(statusList,)`. `RequestControlPointStatus` had no handler; it is answered with
the channel's list.

Who reads what, in the final client - this matters for what a server must send:

- `g_controlPointStatus` (filled by 814) is read by `GetControlPointStatusData`, whose only caller is the dead
  challenge-board window (section 5); the only module that sends 817 is that same window. So 814/817 are a live
  handler pair with no live consumer: the manager entity exists, the handler works, and nothing in the final client
  opens the window that would show the result. The emulator answers 817 correctly and sends 814 on a change so the
  pair is faithful, but the battleground UI does not depend on it.
- The **wargame tracker** (`client/ui/wargametracker.pyo` `BattlegroundScoreTrackerWidget.UpdateGameData`, line 211)
  takes its points from `_team.GetScoreKeeper().GetCPData()` - the `cpData` dict of **ScoreBoardGameScore(remainingTime,
  cpData)** (880), cpId → teamId - labels each with `GetControlPointLabel(cpId)` in `SortControlPointList` order, and
  colours it with `GetTeamColorDef(teamId)`. That is the live feed of the battleground's control points.
- The **world map and radar** (`client/ui/mapwindow.pyo`, `radarwindow.pyo`, via `client/mapstate.pyo`) show a point
  as a map marker of type `uimapmarker.CONTROL_POINT` (= 1) whose state is `(ownerTypeId, ownerId)`:
  `Recv_MapMarkerInfo(mapMarkers)` and `Recv_UpdateMapMarker(entityId, stateData)` (mapstate.py lines 157, 169);
  `GetMarkerStateText` (line 87) unpacks the pair and names the owner (ID_FACTION_HUMAN / ID_FACTION_BANE for
  FACTION_OWNED with a bool owner, `GetTeamName(ownerId)` for TEAM_OWNED, a placeholder string for CLAN_OWNED), and
  `GetControlPointOwnerColorDef(ownerTypeId, ownerId)` picks the colour. The emulator has the MapMarkerInfo /
  UpdateMapMarker opcodes and no packets for them.
- The **point's own entity** shows ownership through `SetOwnerId` (884) on its OwnableControlPoint augmentation.

`ownablecontrolpoint.pyo` (`d8bc7bc634978952f6814ad77cf7404ba5251725cba2edbdb09bb887325e5828`), lines 20-32, maps the
owner id to FX packages: `-1` → none, `0` → `USE_CCP_OTHER_CLAN_OWNS`, `1` → `USE_CCP_BANE_OWNS`, `2` →
`USE_CCP_AFS_OWNS` (and the interruptible-use packages likewise). A new point starts with `_ownerId = 1`. For a
`TEAM_OWNED` row the owner id is a team id: `generated/client/constant/teamconstants.pyo`
(`b3320829156d39b27d8cdd5b0552ebcaa20813bec45de7d28e575a7e87f03f11`) has `RED_TEAM` 1 and `BLUE_TEAM` 2, so red is
dressed as Bane and blue as AFS, which matches the "Red Base" / "Blue Base" rows. For a `CLAN_OWNED` point the
clan variant compares the owner against `shared/gameconstants.py` `VIRTUAL_CLAN_AFS` -1 and `VIRTUAL_CLAN_BANE` -2
(lines 311-312); for `FACTION_OWNED`, `GetControlPointOwnerColorDef` treats the owner as a bool (True = friendly).

## 4. The battleground team and scoreboard protocol (recovered, not implemented)

`client/team.pyo` (`1208f4935a006f0ef7a7f00f9477a5c7a2e6c4f3fdbf1fd79c0a8db8d1d53b14`) on `ClientTeamManagerId` (30):

| Method id | Handler | Arguments |
| ---: | --- | --- |
| 10000122 | `Recv_JoinedTeam` | `(teamId, members)` |
| 10000123 | `Recv_LeftTeam` | `()` |
| 10000117 | `Recv_AddTeamMember` | `(characterId)` |
| 10000118 | `Recv_RemoveTeamMember` | `(characterId)` |
| 890 | `Recv_SetNumberOfTeams` | `(numTeams)` |
| 877 | `Recv_ScoreBoardActive` | `(bActive)` |
| 880 | `Recv_ScoreBoardGameScore` | `(remainingTime, cpData)` - `cpData` is a dict cpId → teamId; the tracker colours each point by `GetTeamColorDef(teamId)` |
| 875 | `Recv_ScoreBoardIndividualUpdate` | `(entityId, individualUpdate)` - a tuple indexed by `shared/scorekeeperconstants.py`: USERNAME 0, CLASS 1, TEAMID 2, ACTIVE 3, PVPCPKILLS 4, PVPCPDEATHS 5, PVPCPDAMAGE 6, PVPCPHEALING 7, PVPCPCAPTURES 8, PVPCPPRESTIGE 9 |
| 876 | `Recv_ScoreBoardTrackerUpdate` | `(entityId, (kills, deaths))` |
| 874 | ScoreBoardFullUpdate | no handler in the final client |

`client/augmentations/manifestation.pyo`: `Recv_WonBattleground()` (872) and `Recv_LostBattleground()` (871) show
`PM_BATTLEGROUND_YOU_WON` / `PM_BATTLEGROUND_YOU_LOST`. `client/wargame.pyo`
(`ebb07ca15a7e7da002fdc852c784dafe770ab7e8e0468e571fea9a475730dd23`) is the duel/squad/clan wargame layer
(`Recv_WargameStarted(wargameId, enemyUserIds)`, `Recv_WargameScoreboard(wargameId, yourKills, theirKills, victimId,
killerId)`, `Recv_DisplayWargameTimer(wargameId, timeMs)`, `Recv_SetWargameMaxKills(wargameId, maxKills)`,
victory/defeat/tied/cancelled), separate from the battleground scoreboard. The tracker
(`client/ui/wargametracker.pyo`) reads `client.gamemap.GetContextId()` for the battleground's title and
`_team.GetScoreKeeper().GetCPData()` for the points, then labels each with `GetControlPointLabel(cpId)`.

`ScoreKeeperField` in `ControlPointData.cs` carries the ten indices. The team and scoreboard calls themselves are not
sent by the emulator: there is no battleground lifecycle to drive them (gap below).

## 5. The challenge board is dead code

`client/ui/challengeboardwindow.pyo` (`0b094068d464672e964410a01ad8e6968adc754255b23455c3240ddd1f61e6eb`) reads
`controlpointdata` too, but expects a status with `.bids`, `.ownership` and `.clanId`, keys its dict by
`(cpId, ordinal)`, and compares against `_controlpointstatus.kCPOwnership_Clan` - a name defined in no module of the
final client (the only `kCPOwnership` string in the 996 modules is this reference). Nothing imports the window, and
the bid opcodes (810, 812, 813) have no handler. The clan bidding on control points it was written for was cut before
shutdown; the live system is the team battleground one above.

## 6. Mechs (D16): the client side, recorded

- `client/augmentations/mechpad.pyo` (`3d629a11f332b90b4306a31d114b191d62db6fdf4bb2ba7d705395c4b9b5871d`):
  `MechPad(Usable)` with one override, `OnBeforeUse(self, actorId, boardingTimeMs, effectTypeId)`, which schedules
  `AnnounceGameEffectAttach(effectTypeId)` on the actor after `boardingTimeMs`. `Usable.Recv_Use` (usable.py line
  902) is `(actorId, curStateId, windupTimeMs, *args)` and hands the extras to `OnBeforeUse`, so a pad's Use call is
  `Use(actorId, curStateId, windupTimeMs, boardingTimeMs, effectTypeId)`. The emulator's `UsePacket` declared an
  `Args` list but never wrote it; it does now.
- `client/gameeffects/mechmorph.pyo`: `MechMorphEffect(BaseMorphEffect)`, `typeId = gameeffectdata.MORPH_MECH`
  (= 457 in `generated/client/gameeffectdata.pyo`), `allowDetach = 1`.
- Use states in the emulator's `UseObjectState`: `MechpadOff` 216, `MechpadOnReady` 217, `MechpadBoarding` 218,
  `MechpadOnEmpty` 219. Entity class 30464 `UsableOwnableMechStation` carries augmentation 83 (`MechPad`).
- Abilities: `client/actions/abilities/ai/mechgroundpoundability.pyo` and `mechmissilesability.pyo`; game effects
  `CF_HUMAN_MECH_PAU` 462, `MECH_ARMOR_SKILL` 10000005, `MODULE_MECH_ARMOR_SKILL` 10000006.
- The Alienware client's `data/catalogs.glm` carries the full mech art set (`creature_human_mech_*`, mech armour
  textures v01-v06) and `physicalentityclassdesclanguage` names the Penumbra and Tannhauser mech suits and the
  "Mech PAU".

What is missing is the whole server side: where pads stand, what boarding does to the actor's stats and abilities,
and how the two abilities behave. None of that is in the client, so it stays a gap rather than a guess.

## 7. What the client cannot say (gaps)

- **Placement.** The client maps `adv_wargame_provinggroundsv002` and `adv_wargame_edmundrange2` contain no
  control-point entities (no class with augmentation 57, 77 or 82 among their 1,224 and 1,511 entities); the points
  were server-placed. The emulator's one control point (class 3814 at (197.66, 162.27, -54.08) in the Wilderness,
  status id 215) is emulator-authored: neither the position nor the id is in the client map or the table.
- **Capture.** How a team takes a point, the war/pre-war/post-war timings behind the four state ids, what
  `endTime` counts down to, and what the `Control Point Assault Token` / `Defense Token` items do.
- **Battleground lifecycle.** Queueing, team assignment (`JoinedTeam`, `SetNumberOfTeams`), scoring
  (`ScoreBoardGameScore`, which is also the tracker's control-point feed), the CONTROL_POINT map markers
  (`MapMarkerInfo` / `UpdateMapMarker` with `(ownerTypeId, ownerId)`) and the win condition (`WonBattleground`);
  `EDMUND_RANGE` is the only ruleset name.

The emulator now answers `RequestControlPointStatus` with every point of the channel's map, unheld (`ownerId`
None) and `kCPState_New`, and has `ControlPointManager.SetOwner` for the moment a placement and a capture rule are
evidenced. A footage capture of a battleground, or the server's own tables, would close the gaps; the Alienware
survey of 2026-09-16 found no battleground or mech footage, and its unmounted Windows partition is the one place on
that machine not yet searched.
