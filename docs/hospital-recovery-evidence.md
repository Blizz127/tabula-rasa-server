# Hospital locations and recovery evidence — 12 September 2026

The final-live 1:1 target remains incomplete. This pass recovers original
Wilderness **map-marker** locations and hospital state consumers, and clarifies
the distinct hospital identifiers and recovery requests. It does not establish
resurrection spawn positions or a complete playable player recovery path.
The new immutable marker catalog is unconnected data; player lethal damage
still uses the documented placeholder until recovery can be implemented from
evidence. See [the preceding packet audit](player-death-client-evidence.md).

## Sources and retained evidence

The inspected package is the selected 1.16.5.0 client described in
[client-artifacts.md](client-artifacts.md). Its embedded executable version and
checked ZIP members identify this artifact; an authenticated original final
manifest remains missing. No acquired executable or game Python module was
executed or imported.

All raw research for this pass is outside Git at
`/home/blizz/backups/rasa-net/research/20260912-hospital-recovery/`.
The strict Python 2.4 literal decoder from the mission audit was copied there
and extended only with numeric `UNARY_NEGATIVE`, needed for map coordinates.
It rejects imports, calls, attribute access, and control-flow instructions.
Consumers were inspected with xdis 6.1.7; decompilation was used for navigation.
`literal-source-manifest.json` and `consumer-source-manifest.json` record member
paths, hashes, embedded source filenames, and UTC compilation times.

| Original member | SHA-256 / date |
| --- | --- |
| `game.zip:generated/client/uimapmarker.pyo` | `c89fb45e6ad920ebc5fe36e188140f5cfab5dc2711f8fceabfffa82dd0e489b1`; 10 February 2009 03:55:40 UTC |
| `game.zip:generated/client/gamecontext.pyo` | `21171550354785d1cd8d85fa74e2d92a0729a4e04b7ca8ccfd25390a95e3ad36`; 10 February 2009 03:55:34 UTC |
| `game.zip:generated/client/uimapmarkertext.pyo` | `d25d9f27785b67bf0252d1586053bab99627899b438ca75a3349c72c87a07f7d`; 10 February 2009 03:55:40 UTC |
| `data/maps/adv_foreas_concordia_wilderness/adv_foreas_concordia_wilderness.map` | `86a6c190377b66ece30a99fb36a038db6d3ccfcce195081186112488cbe07a5e`; outer ZIP CRC `93104b9f` verified |

The source archives remain under the adjacent
`20260912-client-artifacts/final-client-selected/Tabula Rasa 1.16.5.0/` directory.
The binary `.map` member was retrieved separately from the same preserved
outer ZIP through two checked HTTP byte ranges. The extractor refuses a full
archive response, verifies the local filename, uncompressed size, and CRC,
and retains HTTP metadata in `wilderness-map-acquisition.json`. Only 228282
compressed data bytes were retrieved, plus the local header/name/extra data.
This does not authenticate the original publisher's package.

## Original Wilderness spatial data

`gamecontext.lookup[1220]` identifies Wilderness and points to **map template
1378**. These IDs belong to different namespaces: indexing map-marker data by
1220 returns no row. `uimapmarker.factionedmarkers[1378]` contains records of
the following shape, verified in `mapwindow._ShowMarkerWidgets`, original
source line 1377, including raw `UNPACK_SEQUENCE 7` at offset 72:

```text
(markerEntityId, markerType, nameTextId, tooltipTextId, x, y, z)
```

The table's hospital/safe-zone subset is below. Coordinates are rounded here
for readability; the catalog and `wilderness-hospital-marker-rows.json` retain
the exact original values. The factioned-marker map entry is stored at original
bytecode offset **980**.

| Original marker entity ID | Marker type | Name / tooltip text IDs | Name | Original XYZ |
| --- | --- | --- | --- | --- |
| 133079561961145 | 19, `SAFE_ZONE` | 229 / 229 | Twin Pillars Hospital | -125.887421, 220.749985, -468.618469 |
| 133079561961146 | 3, `HOSPITAL` | 230 / 230 | Alia Das Hospital | 787.008850, 294.332428, 366.599152 |
| 133079561961147 | 3, `HOSPITAL` | 231 / 231 | Ranja Gorge Hospital | -693.869019, 170.132980, -342.360809 |
| 133079561961149 | 3, `HOSPITAL` | 232 / 232 | Daghda's Urn Hospital | -654.554138, 283.645966, 882.818542 |
| 133079561961338 | 3, `HOSPITAL` | 306 / `None` | Hospital: Imperial Valley (Control Point) | -330.000000, 172.771683, -532.000000 |
| 133079561962699 | 3, `HOSPITAL` | 303 / `None` | Hospital: Landing Zone (Control Point) | 156.027802, 163.074783, -86.590790 |

The client uses X/Z to place these widgets and joins names through
`uimapmarkertextlanguage`. The marker entity IDs are the keys for dynamic map
state. These records provide no revival yaw, spawn radius/offset, graveyard
ID, or resource restoration fields. Twin Pillars must retain its actual
safe-zone marker type; it must not be rewritten as type 3 simply because its
name includes “Hospital.”

The original binary Wilderness `.map` is now preserved for further structural
analysis. The bounded inspection found no readable hospital/graveyard labels
and no literal little-endian 64-bit occurrences of the three hospital marker
IDs checked. Its binary record format was not fully reconstructed, so this
negative search is **not proof that it contains no related spatial data**.
No binary offset was treated as an unverified spawn record.

## Four hospital identifier namespaces

The local `teleporter` IDs are not arbitrary substitutes for every original
identifier. Several exactly match the original **waypoint language** IDs.
`Manifestation.Recv_GraveyardGained`, original source line 1704, takes
`waypointId` and resolves `waypointlanguage`, then posts the acquired-hospital
message. In contrast, `Actor.OnShowReviveAbort` resolves the `PlayerDead`
choice's `Id` through **graveyardlanguage**. Map markers use their separate
64-bit entity ID and `uimapmarkertextlanguage` name key.

| Hospital | Original waypoint label ID matching local row | Original graveyard label ID | Original marker name ID |
| --- | ---: | --- | ---: |
| Alia Das | 103 | 3 | 230 |
| Ranja Gorge | 106 | 5 | 231 |
| Twin Pillars | 105 | 6 | 229 |
| Daghda's Urn | 108 | 20 | 232 |
| Wilderness LZ | 104 names Wilderness LZ; 216 has the control-point hospital label | Both 136 and 183 have the Landing Zone control-point hospital label; applicable record unresolved | 303 |
| Imperial Valley | 218 | Both 110 and 122 name Imperial Valley control-point hospitals; applicable record unresolved | 306 |

The first four graveyard associations are supported by their names, not a
recovered authoritative foreign-key relationship. Duplicate Landing Zone and
Imperial Valley labels prevent deriving a unique destination identity by text.
Do not use waypoint IDs, marker names, or marker entity IDs directly as
`PlayerDead` graveyard IDs. `waypointlanguage-hospital-selected.json` preserves
the original key/store offsets, including 103 at 506 and 218 at 1496.

Read-only SQLite connections used `mode=ro` and `PRAGMA query_only=ON`.
`local-world-hospital-readonly.json` records the current schema and all 161
type-5 rows; `local-waypoint-identity-readonly.json` records the focused join.
Local row 216 exists but has type 0, context 0, and zero position. Row 104 is
populated at Wilderness LZ; these observations do not establish which record
the final original server advertised.

The six populated local Wilderness rows differ from their original map
markers by approximately **0.95–40.13 units**. Alia Das row 103 is 40.13 units
from the hospital marker and instead nearly coincides with an original outgoing
map-link marker at `(751.5, 294.2102966308594, 382.5)`. This is a material
reason to withhold spawn verification, not evidence to overwrite it with the
hospital icon's location. The earlier audit's statement that local records
could supply actual destinations must not be read as final-live coordinate
authentication. Both spawn location and facing remain unresolved.

## Ownership, discovery, and PvP safety

`client/mapstate.pyo:UnwrapHospitalInfo` (64) returns its supplied value.
`GetMarkerStateText` (87) and `mapwindow._ShowMarkerMapState` (1518) consume:

```text
(isFriendly, isKnown, isSafe)
```

`Recv_MapMarkerInfo` (157) receives the complete current-map dictionary;
`Recv_UpdateMapMarker` (169) replaces one marker entry. The client does not
derive those flags from marker coordinates. The hospital status widget is
colored by friendliness. Tooltip text separately reports acquired/not acquired
and, when safe, **PvP safety**, using original UI text 5194. The safe flag
therefore cannot stand in for friendly control or discovery.

The [official control-point guide](https://web.archive.org/web/20090201021856id_/http://eu.playtr.com:80/en/field_training/guide/control_points)
already retained in [death-retail-evidence.md](death-retail-evidence.md) says
hospital service is unavailable outside AFS control. A
[11 October 2007 publication of Starr Long's description](https://worthplaying.com/article/2007/10/11/news/45771-tabula-rasa-control-points-screens/)
provides a dated primary-author corroboration, including his Wilderness LZ
example. It predates release and cannot by itself establish the final ownership
state or acquisition radius.

The [Giddy Gamer zone guide](https://www.giddygamer.com/tabula/content/basics/zones.pdf),
revision **18 November 2007**, describes hospital selection among known
destinations. Its own legend says the observations came from late beta and
could change. This supports the purpose of the client's acquired flag but
does not prove the final server's default discoveries, discovery trigger,
radius, instance inheritance, or filtering exceptions. The official D10.6
Eloh Temples section restriction remains applicable evidence, as recorded in
the preceding audit. Sending every map hospital is not an established rule.

## Hospital requests, burial, and restored resources

The active death window's `Init` (34), `OnRevive` (136), `OnRespawn` (148),
and `_RespawnPlayer` (184) establish two different UI paths:

| Active UI action | Original call | Established / missing |
| --- | --- | --- |
| “Go To Hospital”, UI element 1474 | `OnRequestRevive()` → `ReviveMe(None)` | This is a normal hospital request, including the empty-choice death window's timeout. The selected-hospital window sends an integer instead. Server fallback selection remains unobserved. |
| “Revive”, UI element 1473 | `OnRequestBurial()` → `BuryMe()` | Despite the method's name, this is wired to the revival button. The server controls whether that button is enabled through `canRevive`; the qualifying rules and transaction are missing. |
| Accept an offered ally revival | `RequestRevive(reviverId)` | Separate `revivewindow.OnRevive` (121) path; does not reuse either hospital request above. |
| Decline an offered ally revival | `RefuseRevive(reviverId)` | Separate `revivewindow.OnCancel` (130) path. |

The death prompt uses UI element 1126 and presents waiting for healing or
hospital transport. Player-message text 234 additionally describes revival at
the current location and respawn at the nearest hospital, but this active
window does not use that message as its prompt. Its wording is supporting
evidence, not a recovered implementation of `BuryMe` or `None` fallback.
`PlayerDead` buffering and the client's `canRevive` reset/callback timing
remain as documented in the preceding raw-bytecode audit.

`clientmethod.Recv_ReviveRequestInfo` (333) unpacks a separate offered-revival
record and opens the ally-revival window. `ReviveInfo.Unpack` (26) consumes only
reviver ID/name, time-to-start, time-to-end, and location. Although its
constructor has `startingPercents`, sickness, decay, and reduction fields,
neither packing direction transmits them. A full non-language byte-string
scan found the starting-percent/sickness/reduction field names only in
`shared/reviveinfo.pyo`; it recovered no hospital resource formula.

Some original resurrection ability descriptions explicitly vary restored
health/armor and death penalties. They describe those abilities, not ordinary
hospital recovery. Neither their percentages nor the experimental C++ full
health shortcut establishes hospital health, armor, Power, or adrenaline.

The [firsthand Shamus Young account](https://www.shamusyoung.com/twentysidedtale/?p=1914)
is dated **2 October 2008**, after the official 23 July D10.6 trauma change,
yet reports the obsolete five-minute penalty. It also reports an incorrect
second class-branch level. Its hospital/waiting choice remains supporting
evidence; its publication date does not validate its numeric rules. The
official 2/4/6-minute notes and February 2009 client constants are stronger
evidence. This explicit conflict must not be presented as merely a
chronologically older account superseded after its publication.

## Implemented catalog and remaining work

`WildernessHospitalMapMarkers` preserves the six original marker records,
including the full entity IDs, exact float coordinates, safe-zone type, and
nullable control-point tooltips. The catalog exposes no graveyard destination
ID, assumed facing, default ownership/discovery, or recovery method. No manager,
schema, or live database was changed by this pass.

`verify-wilderness-catalog.py` independently decodes the original literal tables
and compares all seven fields of all six rows, as well as the context/template
join, against the C# catalog. **Zero differences** were found; the result and
source hashes are in `catalog-original-comparison.json`. Two focused tests
passed in a copied-source .NET 5 container without network or production database
mounts, checking immutable collection/position behavior and distinct marker
identities/types/nullable fields. Logs are `focused-hospital-catalog-tests.log`
and `test-results/hospital-map-markers.trx`.

The preserved source set is sufficient for these map-marker records and UI
contracts, but does not yet unblock full player death. Next evidence must
establish original graveyard-to-waypoint/marker relationships, spawn positions
and facing, authoritative eligible choices and `None`/burial policy, restored
resources, trauma application details, relocation sequencing, and death/revival
persistence. A real-client comparison remains necessary after those pieces
are connected.
