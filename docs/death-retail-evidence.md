# Death, recovery, and logout: original evidence — 2026-09-12

The target remains the final live game immediately before shutdown, preserved
1:1. This updates the evidence gaps in [death-research.md](death-research.md).
It also records the normal-logout correction and the connected immediate-quit
lifecycle correction implemented from the sources below. Player death, hospital recovery, and trauma gameplay are not
implemented by this change.

## Original official live patch and guide evidence

| Evidence | Publication / capture | Exact location and supported rule | Confidence / limits |
| --- | --- | --- | --- |
| [Official D10.6 live notes](https://web.archive.org/web/20080725184546id_/http://eu.playtr.com:80/en/news_article/deployment_106_patchnotes_known_issues_23rd_july_2008_live) | Published 23 July 2008 11:47; captured 25 July 2008 18:45:46 UTC | **PVP, Clans & Control Points → Death Penalty Changes:** trauma lasts 2/4/6 minutes, with 20/40/60% stat penalties. Trauma-kit reuse is 60 seconds. **Logout Timer:** world exit requires ten seconds, including remaining world presence after immediate game quit. **Bug Fixes:** Eloh Temples hospitals are restricted to the current section. | High for the identified live deployment. The page also has a separate **PTS Only** section; those entries were not adopted as live behavior. |
| [Official D11 live notes](https://web.archive.org/web/20081120081005id_/http://eu.playtr.com/en/news_article/deployment_11_patchnotes_known_issues_14_august_2008_live) | Published 14 August 2008; captured 20 November 2008 08:10:05 UTC | **Classes, Combat & Creatures → Abilities and Consumables while crouching:** casting and consumable use are allowed while crouched. | High for the explicitly announced live correction. Does not define every other action prerequisite. |
| [Control Points, by producer Starr Long](https://web.archive.org/web/20090201021856id_/http://eu.playtr.com:80/en/field_training/guide/control_points) | Published 1 February 2008 11:35; captured 1 February 2009 02:18:56 UTC | Paragraph beginning **There are other reasons to take or hold a control point**: a base outside AFS control does not provide hospital or vendor service. | High for the documented ownership rule; section is an older guide retained into 2009. Discovery radii, ownership-to-hospital mappings, and recapture timing are unspecified. |

The five-minute first-stack trauma described in older gameplay accounts is
superseded by D10.6. Do not implement its older 5/10/15-minute progression.
D10.6 also distinguishes corpse abilities in squad/clan PvP from duels: enemy
Reanimation/Reanimation Wave force hospital recovery; Cadaver Immolation has
revival interactions. These are separate future behavior paths, not ordinary
hospital selection rules.

The bounded follow-up checked preserved official D11, D12 (18 September), D13
(15 October), D14 (November), D14.5, and the previously preserved D16.4/D16.5
material. No later trauma-duration replacement was found there. This is not
proof that every server hotfix was archived. The D15 live announcement was
recovered, but its link led to a patch-list index; full D15 notes were not
recovered in this bounded pass.

## Recovered 1.16.5.0 client evidence

[Client acquisition provenance](client-artifacts.md) identifies the
[community-hosted package](https://archive.org/details/TabulaRasa1.16.5.0),
the executable's embedded 1.16.5.0 version, selected-member CRC checks, and
hashes. This establishes the inspected artifact's contents. Independent
official checksums or an authenticated original installation are still needed
to establish byte-for-byte official package identity.

Selected `trpython.zip` members were extracted as data and statically decompiled
with uncompyle6 3.9.3; relevant instructions were checked with xdis disassembly.
No downloaded executable or game Python module was executed or imported. The
bytecode is Python 2.4, magic `6df20d0a`; module headers identify compilation on
10 February 2009 UTC (the decompiler displayed 9 February in the local Chicago
time zone). Original embedded source paths and function names below are
stable locators. Decompiled line numbers are analysis-output positions, not
original source line numbers.

### Numeric constants

`trpython.zip:shared/gameconstants.pyo`, embedded
`python/shared\\gameconstants.py`, contains the following assignments. Xdis
confirms the `LOAD_CONST` / `STORE_NAME` pairs, including original source lines
348–359 and 478.

| Constant | Value | Established / still missing |
| --- | ---: | --- |
| `DEATH_PENALTY_MIN_LEVEL` | 5 | Actual constant; server threshold application and exceptional death types still need the original server path or captures. |
| `REZ_SICKNESS_MAX_STACK` | 3 | Matches the three-stage official live rule. |
| `REZ_SICKNESS_PENALTY` | -20.0 | Matches the official per-stage percentage. Rounding and attribute baseline remain unresolved. |
| `REZ_SICKNESS_DURATION` | 120 | Matches the official first-stage duration in seconds. |
| `REZ_SICKNESS_MAX_PENALTY` | -60.0 | Matches the official maximum percentage. |
| `REZ_SICKNESS_MAX_DURATION` | 360 | Matches the official maximum duration in seconds. |
| `REZ_SICKNESS_NO_HEAL_DURATION` | 30 | Strong versioned corroboration of the separate no-healing condition; exact attachment trigger remains unresolved. |
| `REVIVE_REQUEST_DURATION` | 120 | Stored constant; do not confuse with hospital-choice timeout. |
| `EQUIPMENT_DAMAGE_PER_DEATH` | -10.0 | Stored constant; exact affected slots, durability baseline, rounding, and exemptions need implementation evidence. |
| `MAX_REVIVE_ACCEPT_TIME` | 300 | Consumed by the death and hospital windows for their five-minute choice timer. |

The constants settle their numeric definitions in this artifact. They do not
establish when the server applies them. In particular, they do not authorize
applying trauma to every resurrection type or assuming that every death
increases a stack regardless of level or game mode.

### Death notification and hospital choice

`client/augmentations/actor.pyo`, embedded
`python/client\\augmentations\\actor.py`, establishes:

- `Recv_PlayerDead(self, sourceId, graveyardList, canRevive=0)` fixes the argument
  order. It buffers the notification until the actor is dead, converts hospital
  dictionaries, and invokes the recovery UI for the owning character.
- `OnRequestRevive(self, graveyardId=None)` sends the one-element tuple
  `ReviveMe(graveyardId)`. **Python `None` is a legitimate request value.** An
  integer-only decoder would reject normal UI paths.
- `OnRequestBurial()` sends `BuryMe()` separately. `canRevive` must not simply be
  equated with “hospital transport available.”
- `AnnounceDeath()` cancels the current client action, stops tracking and
  persistent effects, hides weapons, and transitions to dead control.
- `Recv_Revived(sourceId)` reaches `AnnounceRevive`, which restores normal,
  standing, and stopped states. The manifestation override hides the revival,
  death, and hospital windows. These methods do not set restored health.

`shared/graveyardinfo.pyo` defines each hospital dictionary with exactly the
keys `Id`, `pos`, `isSafe`, and `name`. It requires these keys during decoding;
`isSafe` defaults false only in the object's constructor.

`client/ui/waypointwindow.pyo`, functions `ShowGraveyards`, `Hide`,
`TeleportToGraveyard`, and `_RespawnPlayer`, establishes:

- The server-provided entries are displayed for the current map. A false
  `isSafe` value selects the ordinary hospital icon and **does not disable the
  destination**. It is not a substitute for ownership/eligibility filtering.
- Selection sends the hospital's integer ID. Closing the still-pending window
  sends `ReviveMe(None)`.
- The choice timer uses `MAX_REVIVE_ACCEPT_TIME`. At timeout it keeps an
  existing selection, otherwise selecting the nearest advertised hospital by
  squared X/Y/Z distance. This is client selection behavior; it does not prove
  the server's fallback when receiving `None`.

`client/ui/deathwindow.pyo` likewise uses the 300-second timeout and sends
`ReviveMe(None)` for its respawn path. Its other button invokes `BuryMe()`.
No guessed interpretation of button labels is needed to preserve those calls.

### Dead-state gating and healing

`shared/characterstate/control/dead.pyo`, `OnEnterState` / `OnExitState`, directly
blocks and unblocks movement, input, and character control. This provides a
concrete client comparison for server-side dead-player action gating. It does
not establish that projectiles already launched must be deleted.

`client/gameeffects/rezsickness.pyo` defines separate `RezSickness` and
`RezSicknessNoHeal` classes. Its attachment handler triggers the tutorial; it
does not compute the attribute penalty or its timing.

`shared/reviveinfo.pyo` contains fields for starting resource percentages,
sickness, decay, and reduction, but its transmitted `Pack`/`Unpack` tuple carries
only reviver ID/name, time-to-start, time-to-end, and location. The unpopulated
fields do not prove full-health recovery or any other restore percentage.
Exact hospital/ally-revival health, armor, power, and adrenaline remain gaps.

### Logout client contract and implemented correction

The original `client/clientmethod.pyo:Recv_LogoutTimeRemaining` explicitly
forwards **milliseconds**. `client/ui/logoutwindow.pyo` disables normal logout
until that value reaches zero and sends `CancelLogoutRequest()` on cancel.
`client/inputstate/exitgame.pyo` sends `RequestLogout()` to begin and
`CharacterLogout()` for the completed character-select path. Its immediate quit
path calls `OnExit()` / `PostQuitRequest()` from both quit buttons without a
`CharacterLogout()` message. Both game quit and character selection first send
`RequestLogout()` through `_RequestLogout`. Together with D10.6's explicit
remaining world presence, this establishes that socket closure after that
request must preserve its original deadline. The client's displayed integer
rounding is retained; do not
shave a millisecond from the server's ten-second requirement to alter the UI.

The server previously advertised 5000 milliseconds and accepted completion
whenever a boolean had been set; cancellation was a TODO. The corrected normal
path uses `LogoutCountdown` with a monotonic deadline:

- A first request advertises 10000 milliseconds; repeated starts advertise the
  remaining time and retain the original deadline.
- Completion before the deadline or without a pending request does not mark
  the character for map removal.
- Cancellation clears the pending request. A new request starts a fresh delay.
- Completion consumes the request. A new `Manifestation` has no old countdown.

`LogoutTests` covers the wire value, early/missing completion, the exact
deadline, cancellation/restart, repeated starts with queued timer messages,
and character replacement. Five tests passed in an isolated Docker container
using the source copied into the existing SDK image, with no network and no
production database mounts. The combined candidate subsequently passed all 105
tests and was deployed successfully; see [validation log](retail-accuracy.md).
Original-client logout interaction still needs verification.

The follow-up now connects `Client.Close`, `DisconnectedClientQueue`, the server
loop, map cleanup, and account occupancy:

- Socket closure enqueues cleanup and stops packet processing/output. It does
  not save or remove a character before an outstanding logout deadline.
- The disconnected character stays in the map, cells, and actor registry until
  the remaining delay elapses. Map updates run with no connected sockets, so
  existing combat can still affect it. The deadline is checked by the server
  loop; the existing loop scheduling resolution still applies.
- At removal, the existing character snapshot is saved once; the existing
  separate login-metadata update also runs once for an admitted character.
  Entity, cell, inventory, pending action, auto-fire, and map queue references
  are cleaned up. Duplicate closure and accepted normal logout followed by
  socket loss converge on the same completed cleanup.
- The existing `AlreadyLoggedIn` check counts retained world presence. This
  prevents a second actor for that account during retention; it does not claim
  to reproduce retail reconnect/reattachment semantics. Character selection
  packets are accepted only in the existing character-selection state.
- A socket lost during map loading cleans up queued/admitted references even
  when its cell matrix has not yet been created. A default character with ID
  zero is never passed to persistence.

**Remaining logout gaps:** no original evidence recovered here establishes the
server grace period or timeout start for network loss without `RequestLogout`.
That path now performs the previously intended immediate cleanup instead of
leaving a registered ghost; this is an emulator lifecycle repair, not a verified
final-retail timing claim. Retrying a connection gets the existing login error
while the actor is retained; authentic retail retry/reattachment behavior still
needs a capture. The repository's character snapshot currently covers position,
map, rotation, running, and crouching; it does not persist health, death, or
active effects. These unimplemented persistence rules remain gaps. Original
client smoke checks and exact server timeout behavior remain outstanding.

An additional transport-ordering gap remains: `Close()` marks the connection
disconnected before the server drains already-buffered incoming packets. A
`RequestLogout` immediately followed by socket closure can therefore remain
unparsed and take the no-pending-request cleanup path. The original quit UI
normally waits for the server timer response before enabling immediate quit,
but that does not resolve this server ordering defect. No code change for this
gap is included in the tested lifecycle candidate.

`DisconnectTests` adds seven focused cases covering remaining-deadline world
presence and real missile damage, deferred SQLite position persistence and
idempotence, accepted normal logout followed by loss, canceled/no-request
cleanup, disconnected input/output and terminal connection state, replacement
requests during removal, loss during map loading, and the unsaved default
character. The seven cases and five `LogoutTests` passed together (12/12) in
an isolated `rasa_net:latest` SDK container on 12 September 2026, with source
copied from a read-only mount, `--network none`, and no production database
mounts. The combined candidate subsequently passed all 135 tests and was
deployed successfully; see [the work log](retail-accuracy.md). These automated
checks validate implementation; original client interaction remains unverified.

The older C++ implementation at commit
`4a9ab5f1fcdf6a18ab6911c384189cc41ddae651`,
[`src/MapChannel.cpp`](https://github.com/InfiniteRasa/Game-Server/blob/4a9ab5f1fcdf6a18ab6911c384189cc41ddae651/src/MapChannel.cpp),
`mapChannel_readData`, marks immediate removal/disconnection on receive failure.
This corroborates intended emulator cleanup only: the same source advertises a
zero-duration logout, so it cannot establish final retail timing. Its raw copy
is retained as `upstream-MapChannel.cpp`, SHA-256
`69127f6d37c6a13b2a82ea42ec89dfcf697fb989b03f2f4c2b65e824e7aadbde`.
The bounded official-note, original-client constant, and contemporary-description
search did not produce a no-request timeout or reconnect policy.

The D11 crouching rule was audited against `RequestPerformAbility`,
`ActorActionManager`, and current consumable dispatch. Existing casting has no
crouching rejection; a complete consumable-use path is absent. No posture
restriction or invented consumable implementation was added.

## Next implementation paths

1. Use the proven death packet order and optional hospital ID in protocol work.
   Preserve all four hospital keys and native client localization. Validate
   hospital selection against server-advertised eligible choices; research the
   authoritative `None` fallback before implementing it.
2. Reconcile hospital ownership/discovery, Eloh section restrictions, playable
   maps missing hospital records, and relocation/cell lifecycle before enabling
   lethal death globally. A list of every map hospital is not proven eligibility.
3. Build the connected death/recovery transition with current-action cleanup
   and authoritative movement gating. Compare queued notifications and a real
   client against the inspected methods; preserve launched-projectile behavior
   as a separate question.
4. Attach trauma and healing lockout only after the original trigger, stacking
   refresh semantics, attribute baseline, exception rules, and persistence are
   established. The recovered constants now remove numeric guesswork.
5. Verify the connected logout paths with the original client and recover the
   remaining abrupt-loss timing, reconnect, and complete character-persistence
   rules before claiming full logout fidelity.
6. Signature rank limits and other skill tables are handled by the separate
   [final client skill audit](final-client-skill-evidence.md). The older official
   abilities overview retains stale names and is insufficient to set final caps.

## Preserved source files

Raw official HTML and derived plain text are outside the repository under
`/home/blizz/backups/rasa-net/research/20260912-death-retail/`.

| File | SHA-256 of raw HTML |
| --- | --- |
| `20080725184546-deployment_106_patchnotes_known_issues_23rd_july_2008_live.html` | `6a5590eadee8a267a28ee3db70fd8eef25c7724243f19ce79173534030bbb5f9` |
| `20081120081005-deployment_11_patchnotes_known_issues_14_august_2008_live.html` | `3fed7243a69cdcb99d97aa655cc2ceedf452d475cf2d2a3848b2a8cfdb3c4f19` |
| `20090201021856-control_points.html` | `10122e164284f4b1d48649b514e0036a17eb847d8cef41f5d09439d2e1d34dc4` |

The directory also retains the CDX discovery results and examined later notes.
Some archived responses incorrectly advertise gzip; retrieval retained the
received HTML bytes after checking the payload's actual gzip signature. The
normal web-page tool could not open these archive URLs; direct raw `id_`
retrieval succeeded.

The `client-bytecode/` subdirectory holds extracted original members,
uncompyle6 `_dis` analyses, selected xdis disassemblies, and
`source-manifest.json` with original ZIP member paths, magic, timestamps, and
SHA-256 hashes. Raw copyrighted code remains outside the repository.

An [official November 2008 field-guide page](https://web.archive.org/web/20081115093501id_/http://www.rgtr.com/game_intel/official_guides/the_afs_field_guide.html)
was also preserved; it links `ftp://ftp.playtr.com/TR_manual.pdf`. The bounded
CDX lookup found no capture for that PDF path. No manual-only death rule is
claimed as verified from that unavailable file.
