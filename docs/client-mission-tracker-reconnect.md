# Mission tracker after reconnect

Audit date: 2026-09-22. Original-client emulator run 07 reconnected Alden after
the [run 06 checkpoint](client-gearing-playthrough.md). The top-right tracker was
empty, but **mission 1992 and its progress were intact**. Opening the journal
showed the completed equipment and Practice Dummy steps, with Lightning pending.
Selecting the journal's Track checkbox restored the mission and objective to
the tracker. The initial audit was read-only; the subsequent candidate below
addresses the admission ordering, with serial and original-client validation
recorded below. A separate default-save defect was then found and corrected.

The [evidence record](evidence/client-mission-tracker-reconnect.json) hashes the
immutable screenshots, database snapshot, original bytecode and disassemblies.
Runtime files are under
`/home/blizz/backups/rasa-net/research/20260922-client-playthrough/playthrough-07/`:

| Artifact | Observation |
| --- | --- |
| `02-recruit-boots-tooltip.png` | Tracker empty after reconnect; only backpack/hover input had occurred. |
| `06-mission-journal-reconnect.png` | Gearing Up for Battle remains in the journal with prior steps complete and Lightning pending; Track is unchecked. |
| `07-tracking-selected.png` | Selecting Track immediately restores the mission and Lightning objective to the top-right tracker. |
| `reconnect-mission-state.json` | Before checkbox input, at 20:50:59.888306 UTC: mission 1992 active, objectives 1/2/3/4/5/6/9 completed, objective 8 incomplete. Character option 55 contains the exact string **`"1,992"`**. |

## Grouped option value: original native US formatting

Option 55 is `MissionTrack0`. The server's acceptance code creates mission IDs
with invariant decimal `ToString`, without grouping. `CharacterOptions.Read`
reads Unicode, and the option handler/repository preserve that string unchanged.
No reviewed server path explains adding the comma.

Original `clientmethod.SaveCharacterOptions`, source line 619, bytecode offsets
56–71, reads `GetOptionUnicode`; offsets 87–105 add that unchanged value to the
save list, and source line 632, offsets 141–159, sends it. Conversely,
`Recv_CharacterOptions` strips trailing whitespace at source line 569, offsets
44–53, then calls native `SetOptionUnicode` at line 574, offsets 96–111.
The subsequent native trace resolves the formatter. In the original executable
(preferred image base `0x400000`), GetOptionInt's binding at `0x496ab9` reaches
handler `0x495430`, then parser `0x496fd0`. The parser constructs a string stream,
pushes locale category `0x3f` at `0x497003` and the literal `"usa"` at
`0x497005`, constructs the locale at `0x497019`, and imbues the stream at
`0x497038`. Integer extraction is at `0x497061`, through IAT `0xb2b3d8`:
`std::basic_istream::operator>>(int&)` from MSVCP80.dll.

SetOptionInt handler `0x494f90` calls formatter `0x4040c0` at `0x494fcd`.
That formatter also imbues a `"usa"` locale (`0x404120`–`0x40414a`), inserts the
integer at `0x404175` through IAT `0xb2b33c`, and retrieves the formatted string
at `0x404184` through IAT `0xb2b314`. The locale literal is at `0xbc81e0`.
GetOptionUnicode follows `0x4952e0` → `0x481780` to convert the stored string;
SetOptionUnicode follows `0x494f20` → `0x481670` → `0x494270` to store it.
The decoded imports and retained disassemblies are hashed in the evidence
record under `tracker-reconnect-audit/native-option-*`.

This original US-locale integer input/output path explains canonical `1,992`
as legitimate client formatting. It is not evidence of server data corruption.
The exact runtime parser result has not been separately instrumented, and this
audit makes no claim about its tolerance for malformed grouping.

The server previously rejected every grouped value through invariant
`NumberStyles.Integer`. Its reader now additionally accepts canonical comma
grouping, requiring an exact invariant `N0` formatting round trip after parsing.
Thus `1,992` and `1,234,567` are accepted, while `19,92`, repeated commas, other
punctuation, suffixes and overflow remain rejected. Existing ungrouped parsing
is unchanged. Reading and transport preserve the original option string;
intentional zero/untracking remains zero. This correction prevents a later
mission acceptance from dropping valid grouped IDs when it reads existing
tracked slots. Five valid grouped cases, thirteen malformed/overflow cases,
and a grouped/plain deduplication and zero-preservation case cover the change.

## Admission-order hypothesis

The preceding admission sent CharacterOptions and SetControlledActorId from
`InitializePlayerControl` inside `CellManager.AddToWorld`. MissionStatusInfo is
sent later, after AddToWorld and AssignPlayer. Original bytecode establishes:

- `gameui.OnUserControllerAvailable` calls `LoadCharacterData` at source line
  1157, offsets 122–125; that calls `LoadMissionTrackingData` at line 1653,
  offsets 53–56.
- `LoadMissionTrackingData` clears the local list at line 1785, offsets 0–3,
  reads saved IDs through `GetOptionInt` at line 1790, offsets 53–72, and calls
  `FilterMissionTrackingData` at line 1798, offsets 139–145.
- The filter retains only IDs present in the avatar's current mission data:
  lines 1765/1770, offsets 22–31/82–103. It replaces the tracking list at line
  1774, offsets 149–152.
- Later `missionlog.Recv_MissionStatusInfo` stores mission data at line 161,
  offsets 0–12, then calls `UpdateMissionTrackingToAvatarMissions` at line 163,
  offsets 15–39. That helper only removes invalid IDs; it cannot repopulate a
  previously emptied list.

Thus early controller availability could discard a valid saved ID before the
mission list arrives. This was initially a source-supported hypothesis; the before/after original-client
runs below subsequently verified the repair for both plain and grouped values.
The exact live-service server schedule remains unknown. The checkbox result matches `currentmissionswindow.OnTrackingBtnToggled`,
which adds local tracking at source line 328, offsets 81–93.

Run 08 repeated admission with the known ungrouped value `1992`. Before mission
checkbox or movement input, `playthrough-08/01-plain-option-reconnect.png` again
showed an empty tracker. `plain-option-reconnect-state.json` retained option 55
as `1992` and mission 1992 as active. Grouping therefore cannot be the sole cause
of the observed reconnect failure. Its archived `client-final.log` is retained
with the baseline artifacts. The candidate does not normalize option strings;
native formatting evidence and the bounded reader correction are separate from
the admission-order fix.

## Deferred controller candidate

Sending MissionStatusInfo immediately before the old SetControlledActorId is
unsafe: it synchronously posts `UI_UPDATE_CURRENT_MISSIONS`, and
`missionlogwindow.HandleUpdateCurrentMissions` (lines 90–91, offsets 0–24)
dereferences the current avatar without a None guard. `Recv_CharacterOptions`
does not reload tracking, so sending it again after the mission log would not
solve the ordering problem either.

The original client explicitly supports selecting an actor that does not yet
exist. `Recv_SetControlledActorId` sets the native manifestation ID (line 87,
offsets 183–195), registers its entity-added callback (line 90, offsets 215–233),
and creates control immediately only if the entity already exists (lines 93–96,
offsets 238–291). `CreateEntity` registers the newly constructed actor before
applying its initial packet data (line 855, offsets 155–167). Finally,
`PhysicalEntity.Recv_WorldLocationDescriptor` calls `world.AddToWorld` at line
310, offsets 35–50; that posts `ENTITY_ADDED_TO_WORLD` at line 62, offsets
79–100, releasing the deferred controller callback.

The candidate therefore sends CharacterOptions, selects the absent owner ID,
and creates the owner with its existing state, ActorInfo, and a read-only saved
mission snapshot before WorldLocationDescriptor, now the last field. During
mission UI callbacks the selected actor already exists in the registry. During
controller creation its yaw and missions are ready. Owner drawer/equipment and
nearby actor introductions remain after this owner introduction. The corpse
announcement remains in the bundle; normal and dropship transfers use the same
admission path. This retains the earlier orientation and equipment fixes while
moving the actual controller creation to the final location field.

An archive string census found five current-mission UI subscribers: journal,
current missions, tracker, map and radar. The first two may dereference the
avatar, while tracker/map/radar guard absence; all can resolve it at the new
snapshot point. The manifestation-ID event's chat, social and clan-lockbox
subscribers use clan caches keyed by ID, without requiring the actor object.
The original mission snapshot receiver only replaces state, filters tracking,
updates UI, and reconciles indicators; it does not replay mission acceptance.

Normal admission retains its later reconciliation snapshot: definition repair,
offline timer expiry, and equipment/content reactions run at their existing
post-controller location, once. The earlier snapshot performs none of these
operations. Two status snapshots are intentional: one supplies saved state for
the client's one-time tracking filter; the other reflects reconciled state.
No active mission is automatically tracked, and stored zero/untracked and
grouped values remain unchanged. Native grouped-option behavior is not inferred
from the server parser.

Seven existing yaw, corpse, transfer and nearby-equipment cases now assert the
deferred sequence. Three new cases preserve `1992`, `0`, and `1,992` options
with saved mission progress in place before world entry. A mission regression
keeps definition reconciliation and timer expiry out of the early snapshot;
the existing equip-reconnect case now proves repeated snapshots cannot complete
or replay equipment objectives. Combined tests and original-client validation
subsequently passed as recorded below.

## Original-client candidate verification

Run 09 restored **Gearing Up for Battle / Speak to Corporal Hartmann** immediately
on admission with option 55 equal to `1992`, before any UI input. Run 10 repeated
the result with the exact native grouped string `1,992`. The corresponding
`02-*-loaded.png` screenshots and read-only mission/options snapshots corroborate
the visible result. Run 08, with the same ungrouped value on preceding binaries,
showed no tracker. Together these establish the admission fix in the recovered
original client, and directly establish native grouped parsing for mission 1992.
They do not certify the unavailable final-live server packet order.

The targeted suite passed **174/174** in **40.2117 seconds**, including deferred
admission, yaw, death, transfers, nearby equipment, pure mission snapshots,
canonical grouped parsing, and the two McAllister movement regressions. Full-suite
and intentional-untracking runtime checks are recorded separately when complete.
Neither run replayed acceptance or changed saved objective progress. The existing
startup tuple error remains present; no new mission/admission traceback was seen.

## Default-valued saves: additional original-client failure

The admission/movement candidate passed **1276/1276 tests**, none skipped, in
**6.0647 minutes** (archived in `verification-tracking-admission/`). This did not
finish tracker persistence verification. In run 10, screenshot
`06-checkbox-untracked.png` visibly shows the mission checkbox unchecked and
tracker empty. Normal logout returned to selection, but
`verified-untracking-logout-state.json` retained option 55 as `1,992`.
`08-untracking-reentry-baseline.png` shows the mission incorrectly tracked again
on re-entry. An earlier automated click was not visually verified; its retained
value is not used as proof of checkbox handling.

Original `SaveCharacterOptions`, line 620 offsets 74–83, excludes values equal to
their defaults; line 630 compares the resulting sorted list with the cached list,
and line 632 sends the new list even when empty. Mission tracking saves unused
slots as integer zero (`gameui.SaveMissionTrackingData`, lines 1816–1820).
The server ignored an empty list and merged other saves, retaining omitted rows.
A subsequent correction must respect this original default-omission behavior,
with packet-scope verification and transactional persistence tests before use.


The [complete option-snapshot correction](client-character-options-snapshot.md)
subsequently passed 179/179 focused tests. Original-client run 11 now clears the
tracking option after a visually verified uncheck and normal logout. Run 12,
a fresh client process, keeps the mission untracked with all journal progress
intact. The earlier run 10 failure remains recorded as the pre-correction
baseline rather than an unresolved admission defect.


Final combined verification: **1281/1281 tests passed**, none skipped, in
**6.8934 minutes**. All **2061 source entries** match the tested snapshot.
Logs, TRX and hashes are retained in `verification-options-final/` beneath the
original-client playthrough research directory and linked by the evidence
manifest. These tests verify implementation; the original-client observations
and surviving-source comparisons establish their separate fidelity bounds.
