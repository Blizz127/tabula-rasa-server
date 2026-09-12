# Player death and recovery client contract — 12 September 2026

The preservation target is the final live service immediately before shutdown.
This reconstructs the inspected **1.16.5.0 client contract**, corrects creature
death notification, and supplies unused player death/recovery codecs. It does
not enable player death or claim a complete hospital recovery system. The
lethal-hit player health refill remains an explicit placeholder until recovery
can be implemented without fabricated destinations or revival rules.

## Provenance and confidence

The archives are the selected original members described in
[client-artifacts.md](client-artifacts.md), under
`/home/blizz/backups/rasa-net/research/20260912-client-artifacts/final-client-selected/Tabula Rasa 1.16.5.0/`.
The executable version establishes the inspected package's compatibility
revision; an official final manifest or authenticated installation is still
needed to prove the shutdown package's complete identity.

Static research for this pass is retained outside Git at
`/home/blizz/backups/rasa-net/research/20260912-player-death/`:

- `extract-client-contract.py` extracts selected ZIP members and records xdis
  instructions without importing or executing downloaded game modules.
- `source-manifest.json` records member paths, byte lengths, SHA-256, embedded
  source filenames, and compilation timestamps in UTC.
- `death-contract-bytecode.json` and `death-contract.raw-dis.txt` record function
  arguments, original source lines, and bytecode offsets. The `?.` prefix is
  xdis's root code-object name, not part of the original class name.
- `graveyardlanguage-literals.json` records all 192 literal hospital text
  assignments, with original bytecode offsets. It was decoded by the strict
  literal decoder preserved in the adjacent `river-recon-selected/` research.

All inspected modules are Python 2.4 bytecode. Actor compilation is
**10 February 2009 03:55:14 UTC**; manifestation is one second later. Some
decompiler output displays 9 February in the host's Chicago time zone.
Instruction analysis, rather than decompiled control-flow formatting, governs
the findings below. The original modules and derived disassemblies remain
outside this repository.

| Original member | SHA-256 |
| --- | --- |
| `trpython.zip:client/augmentations/actor.pyo` | `2ecb129d5071ac588e076415857ea4e5b3ebae08a964e2828b885089ac316c4f` |
| `trpython.zip:client/augmentations/manifestation.pyo` | `724f68eb70e5a47338db82977c55abfa09f1b187f86a0b9a560c012a8588886d` |
| `trpython.zip:shared/graveyardinfo.pyo` | `8e6794d15dc4ae18f078f3b66cb2453e9613a969db00f7c6b24b020210331cd0` |
| `trpython.zip:shared/damageinfo.pyo` | `27a36588a0009ef88ee488a936be7b80ea92f9fd349318fa10f72fde75cd8c4e` |
| `game.zip:generated/client/language/english/graveyardlanguage.pyo` | `01fc4eefb67abb699bf4cc202abb7c7fe91723cec35385616049bd99eb39b919` |

Confidence is high for these receivers' argument shapes and executed branches.
An original network capture is still required to establish exact live packet
scheduling, native callback timing, and all server-side death policies.

## Death state and notification order

`Actor.Recv_StateChange`, original source line 1402, changes character states.
`CharacterStateMgr._Transition` (421) invokes state exit/entry handlers;
`shared/characterstate/control/dead.pyo:OnEnterState` (14) blocks movement,
input, and control. These paths do **not** call `Actor.AnnounceDeath`.
Health zero also does not independently put the actor into the dead state:
`CharacterStateMgr.IsDead` (699) checks the current control state.

`Actor.Recv_ActorKilled`, source line 3031, has only the `self` argument:
the wire payload is an empty tuple. Its raw instruction offsets 0–9 call
`IsDead` and jump over `AnnounceDeath` when already dead; offsets 13–19 call
`AnnounceDeath()` otherwise. Its original method documentation identifies this
as the fallback for observers that did not receive the killing damage because
the source was outside their visibility. `Recv_MadeDead` (3023) has a similar
guard, but identifies the distinct server-forced death path.

`Actor.AnnounceDamage` (2851) invokes `AnnounceDeath` when the received damage
has `deathBlow` set. `shared/damageinfo.pyo:DamageInfo.Init` (94) establishes
that flag at **zero-based slot 7 of the twelve-field damage tuple**. Unlike
`Recv_ActorKilled`, this invocation does not first test `IsDead`.

`Actor.AnnounceDeath` (2972) transitions to dead when necessary, then cancels
the current client action, posts actor/owner death events, stops tracking,
hides weapons, applies death presentation, and stops persistent effects.
Those remaining operations are not all inside its state-transition guard.

Consequently, the supported delivery ordering is:

1. Apply authoritative damage and retain stable resource values for queued
   updates. Mark the server actor dead exactly once and stop actions requiring
   a living actor.
2. Send the killing action's `PerformRecovery` with `deathBlow = 1` to observers
   of its source.
3. Send `ActorKilled()` on the victim to observers of the victim. A client that
   already processed the killing damage skips this fallback; a client that
   could not see the source still announces the death.
4. For a player with a working recovery path, send owner-only
   `PlayerDead(sourceId, hospitalList, canRevive)` to supply its recovery UI.

This order is a deduction from the original guards and their documented
purpose, not a claimed captured final-live packet trace. Sending
`StateChange([Dead])` first prevents the fallback's cleanup and presentation.
Sending `ActorKilled` before a killing recovery can announce death twice.
The player notification can arrive early because the receiver buffers it,
but state change alone cannot flush that buffer through `AnnounceDeath`.

## Player recovery and dead snapshots

| Original receiver / sender and source line | Established contract |
| --- | --- |
| `Actor.Recv_PlayerDead` (2926) | `(sourceId, graveyardList, canRevive=0)`. Buffers until dead; for the owner, iterates every entry, decodes it, then schedules the UI callback with delay zero. The list must be iterable: use `[]`, not `None`, for no destinations. |
| `GraveyardInfo_FromDict` (37); `GraveyardInfo_ToDict` (24) | Every dictionary requires `Id`, `pos`, `isSafe`, and `name`. Positions are X/Y/Z triples in waypoint consumers. The object's name may be `None`. |
| `Actor.OnShowReviveAbort` (2406) | Empty hospital list opens the death dialog only. A nonempty list localizes each hospital using `graveyardlanguage[Id]`, then opens the hospital selection window. |
| `Actor.OnRequestRevive` (2425) | Sends `ReviveMe(graveyardId)` as a one-element tuple; the default argument is Python `None`. |
| `Actor.OnRequestBurial` (2435) | Sends the separate empty-tuple request `BuryMe()`. Its server behavior cannot be substituted for hospital transport. |
| `Actor.Recv_Revived` (3044); `Manifestation._DoRevived` (363) | Receives `(sourceId,)`; clears death source, hides the owner’s revive/death/hospital windows, announces revival, and restarts persistent effects. |
| `Actor.AnnounceRevive` (3066) | While dead, transitions to normal/standing/stopped, restores targeting/picking, posts the not-dead event, enables attribute refresh, restores weapons, and stops death effects. It does not restore health, armor, power, or adrenaline. |
| `Actor.Recv_DeadOnArrival` (3148) | Receives `(canRevive,)`; while not already dead, calls `AnnounceDeath(doDeathFX=0)`. Supports introducing a corpse without fresh death FX; does not establish server persistence/reconnect policy. |
| `Actor.Recv_ActorInfo` (2382) | Initial state IDs are applied through state transitions. A dead initial state by itself does not reproduce all death announcement work. |

The decompiler misrepresented the empty-list UI branch as falling through to
the hospital dialog. Raw `OnShowReviveAbort` offset **130 jumps directly to
236**, bypassing the hospital event at offsets 207–228. This distinction is
why a fabricated empty hospital list is not a complete recovery implementation.

`Recv_PlayerDead` resets `canRevive` to zero after scheduling `OnShowReviveAbort`.
The callback subsequently reads that field for the empty-list death dialog.
Native zero-delay callback timing has not been established; preserve this
observed behavior as a client quirk, and do not infer a hospital eligibility
rule from the flag or silently modify it.

`Recv_StateChange` skips normal control while already dead. Recovery therefore
requires the revival announcement path rather than a normal-state packet
alone. Conversely, introducing `ActorInfo` with a dead state before
`DeadOnArrival` makes the latter skip its guarded announcement. A future dead
snapshot sender must verify its complete introduction order against a client;
the present codecs do not enable or invent that order.

`Actor.UpdateAttribute` (942) decides whether to announce a resource change by
testing whether `whoId` resolves to an existing client entity. The sender ID
must not be guessed as a universal zero/source/victim value from its name.
`ActorAttribute._PredictRefresh` (177) skips both regeneration and its next
prediction update while dead. These observations do not establish the server's
resource restoration amounts or rates after recovery.

## Hospital identity and remaining server dependencies

The current teleporter schema has one local primary key, class/type, description,
position/rotation, and map context. It has no separate original hospital ID.
`DynamicObjectManager.InitTeleporters` puts that local key into `WaypointInfo`;
its type-5 switch branch creates no hospital recovery service. Ordinary
`SelectWaypoint` handles a different request and cannot be assumed to implement
`ReviveMe` semantics.

The earlier read-only world audit in [death-research.md](death-research.md)
identified the following Wilderness records. The original text table proves
that their local row IDs cannot be used as hospital protocol IDs:

| Local type-5 row / description | Original hospital text ID / evidence |
| --- | --- |
| 103 — Alia Das | 3 — `Alia Das Hospital`, literal assignment offset 29 |
| 105 — Twin Pillars | 6 — `Twin Pillars Hospital`, offset 47 |
| 106 — Ranja Gorge | 5 — `Ranja Gorge Hospital`, offset 38 |
| 108 — Daghda's Urn | 20 — `Daghda's Urn Hospital`, offset 56 |
| 104 — Wilderness LZ | Original 104 means `Awol Camp Hospital`, offset 452. The Wilderness LZ mapping remains unresolved. |
| 218 — Imperial Valley | Original 218 means `Tahrendra Base Field Medic`, offset 1262. Both 110 and 122 name Imperial Valley control-point hospitals, so the applicable original record remains unresolved. |

The matching names in the first four rows establish localization identities,
not validated spawn coordinates, discovery status, or destination availability.
There is no live database correction or generated hospital list in this pass.
Supplying a server-written name cannot mask an incorrect ID: the client
replaces the supplied name before displaying the list.

`waypointwindow.ShowGraveyards` (341) selects different icons for `isSafe`, but
false does not disable a destination. `Hide` (504) sends `ReviveMe(None)` when
closing a pending choice. `_RespawnPlayer` (1362) keeps the selected destination
at timeout or chooses the nearest advertised one by squared X/Y/Z distance.
The client uses the recovered 300-second maximum choice time; its selection
algorithm does not establish the server's fallback for `None`.

[death-retail-evidence.md](death-retail-evidence.md) retains the official
[D10.6 live notes](https://web.archive.org/web/20080725184546id_/http://eu.playtr.com:80/en/news_article/deployment_106_patchnotes_known_issues_23rd_july_2008_live)
and the original [control-point guide](https://web.archive.org/web/20090201021856id_/http://eu.playtr.com:80/en/field_training/guide/control_points).
Those sources require Eloh Temples section restrictions and hospital
unavailability outside AFS control. They also establish the later 2/4/6-minute,
20/40/60-percent trauma rules, superseding older trauma durations. The inspected
client constants corroborate those numbers. Exact application triggers,
stack refresh, rounding, exemptions, restored resources, and durability loss
application still need server evidence or authentic captures.

A complete player transition still needs validated hospital identity/coordinates,
availability filtering, `None`/burial behavior, stale-selection handling,
relocation and cell updates, resource restoration, and revival notifications.
Server-side death gating must cover movement, autofire, unfinished actions, and
effects; already-launched projectile policy is a separate evidence gap.
`CharacterRepository.SaveCharacter` currently saves position/map/rotation and
running/crouching, not health, death, or active effects. Login resets therefore
cannot be treated as a reconstructed corpse/recovery persistence policy.

## Implemented scope and validation

`PlayerDeadPacket`, immutable `GraveyardInfo`, `DeadOnArrivalPacket`,
`ReviveMePacket`, and `BuryMePacket` now encode/decode the supported shapes.
Hospital IDs remain signed integers for later authoritative lookup; the decoder
does not turn a negative request into an unsigned destination. `None` is
accepted only as the optional hospital request value. Hospital dictionaries
always include the nullable name field, which the original client localizes.
The queued server list is copied, and destination objects are immutable.
These codecs have no request handlers or gameplay activation in this change.

`CreatureManager.HandleCreatureKill` retains authoritative state and kill
bookkeeping but removes its premature dead-state broadcast. The paired
`MissileManager` integration sets the actual killing hit's `deathBlow` and emits
victim `ActorKilled` after source recovery. Existing experience, loot, and
creature death persistence behavior are not validated by this protocol repair.

`PlayerDeathPacketTests` checks dictionary fields/coordinates, source IDs above
32 bits, empty choices, queued list snapshots, optional hospital IDs, invalid
request shapes, dead-on-arrival flags, and existing kill/revival packet shapes.
`CreatureDeathTests` exercises the actual kill path for weapon attack, melee,
and Lightning with source-only, shared, and victim-only observers. Two queued
shots verify one death, correct recovery/fallback ordering, and no false hit
or duplicate death on the second impact.

All **16 focused tests passed** (13 packet cases and three creature integration
cases) in an isolated .NET 5 Docker container, with source copied from a read-only
mount, networking disabled, and no production database mounts. Results are
retained in the external research directory as `focused-validation-04.log` and
`test-results/player-creature-death.trx`. `git diff --check` also passed.

Earlier compile attempts are retained separately: the first encountered a
simultaneously unfinished shared Lightning type, and the next two exposed
missing test namespace imports corrected before the passing run. Unit tests
verify packet formats and emulator notification behavior; original-client
interaction and a complete playable player death/recovery lifecycle remain
unverified.
