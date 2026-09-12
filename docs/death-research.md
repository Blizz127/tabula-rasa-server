# Player death and hospital recovery audit — 2026-09-12

Preservation target: **the exact final official game state before shutdown**,
without approximated mechanics. The project setup currently requires client
**1.16.5.0**; that requirement alone does not prove which final official content
and rules it represents. Establish the final build/patch chronology and retain
the source versions. This audit changes no gameplay code. Replacing the
lethal-hit health refill requires a working hospital recovery path in the same
change; setting the player to dead alone would leave normal play stuck.

## Current connected path and missing pieces

- `src/Rasa.Game/Managers/MissileManager.cs`, `DoDamageToPlayer`, consumes armor
  and health, queues an `UpdateHealthPacket`, then immediately restores maximum
  health when current health reaches zero. It sends no death notification and
  leaves the actor alive. `UpdateHealthPacket` retains the mutable
  `ActorAttributes` reference, so later serialization can already contain the
  restored value. A future death/revive change must snapshot these values or
  otherwise guarantee the intended zero-health update survives queuing.
- `GameOpcode` already defines `PlayerDead = 595`, `ActorKilled = 776`,
  `ReviveMe = 170`, `Revived = 171`, and `DeadOnArrival = 261`. C# has empty-tuple
  `ActorKilledPacket` and `MadeDeadPacket`, and `RevivedPacket(sourceId)` in the
  misspelled file `Packets/ClientMethod/Server/RevivevPacket.cs`. There is no
  `PlayerDeadPacket`, `ReviveMePacket`, or registered revive handler.
  `ActorManager` lists `ReviveMe` and `BuryMe` as TODOs.
- `CharacterState.Dead` is control state 5; `CharacterState.Normal` is control
  state 11. These are distinct from posture state `Standing`. Existing creature
  death broadcasts `StateChangePacket([Dead])`. Creature AI already stops
  targeting players with zero health or the dead state.
- Weapons, ability requests, action recovery, and received movement do not
  consistently reject dead players. See `ManifestationManager.PlayerTryFireWeapon`,
  `RequestPerformAbility`, `ActorActionManager`, and `Client.HandleProtocolPacket`
  (`ClientMessageOpcode.Move`). Death requires those connected controls; a death
  animation alone does not prevent continued movement or attacks.
- `Manifestation(CharacterEntry, ...)` initializes control state to `Normal`.
  `MapChannelManager.MapLoaded` calls `UpdateStatsValues(client, true)`, restoring
  health. The character model has no persisted life state or current health.
  Reconnecting currently bypasses any future memory-only death state. This is
  an explicit lifecycle gap, not a verified retail recovery rule.

## Hospital data already available

Read-only queries against `rasaworld.db` found **161 type-5 hospital rows across
64 map contexts**. All have nonzero map contexts and nonzero positions.
`WaypointType.Hospital = 5` agrees with their descriptions. The existing
`teleporter` table includes ID, description, position, rotation, and map context.

`DynamicObjectManager.InitTeleporters` loads type 5 into both the per-map and
global teleporter dictionaries, but its `case 5` does no hospital-specific work.
These records can supply actual destination data; a separate arbitrary spawn
coordinate table is unnecessary. Presence in this emulator database does not
prove retail discovery requirements, current safety, ownership, or spawn height.

Wilderness (map 1220) contains:

| ID | Description | Position (X, Y, Z) |
| --- | --- | --- |
| 103 | Hospital: Alia Das | 750.0742, 294.21094, 382.28906 |
| 104 | Hospital: Wilderness LZ | 156.28906, 163.07422, -88.94141 |
| 105 | Hospital: Twin Pillars | -129.46875, 220.7539, -480.16406 |
| 106 | Hospital: Ranja Gorge | -698.21953, 170.13281, -344.25 |
| 108 | Hospital: Daghda's | -657.21875, 283.64844, 880.51953 |
| 218 | Hospital: Imperial Valley (Control Point) | -330.6211, 172.77344, -532.71484 |

Fourteen `map_info` rows have no type-5 hospital: 1115, 1430, 1737, 1823, 1991,
2055, 2110, 2155, 2156, 2163, 2233, 2327, 2361, and 20000009. Some are test or
selection maps; others are adventures or instances. Their parent-map recovery
or actual hospital data must be established before enabling death everywhere.
Text matches are insufficient: ID 595 is named `MIS_INDRACAVERNS_GRAVEYARD` but
has type 1 and map 1823. Do not silently treat every name containing “graveyard”
as a hospital or send every uncovered map to Alia Das.

Reproduce the principal coverage checks without writing to production:

```sql
SELECT COUNT(*), COUNT(DISTINCT map_context_id)
FROM teleporter WHERE type = 5;

SELECT map_context_id, map_name FROM map_info
WHERE map_context_id NOT IN
  (SELECT map_context_id FROM teleporter WHERE type = 5 AND map_context_id != 0);
```

## Pinned C++ comparison and limits

The inspected experimental branch resolves to
`4a9ab5f1fcdf6a18ab6911c384189cc41ddae651`. This later emulator is supporting
evidence only. Final-client data, final patch records, and authenticated retail
captures take priority; disagreements must be resolved before implementation.

- [MapChannel.cpp, revive dispatch](https://github.com/InfiniteRasa/Game-Server/blob/4a9ab5f1fcdf6a18ab6911c384189cc41ddae651/src/MapChannel.cpp#L649)
  routes `ReviveMe` to `manifestation_recv_Revive` and describes it as the dead
  player's hospital request. This supplies a concrete request/handler connection.
- [manifestation.cpp, hospital recovery](https://github.com/InfiniteRasa/Game-Server/blob/4a9ab5f1fcdf6a18ab6911c384189cc41ddae651/src/manifestation.cpp#L1418)
  parses a tuple containing a graveyard ID, then ignores that ID. It moves the
  player to hardcoded `(786.92, 294.83, 362.38)`, restores maximum health and
  alive state, and broadcasts `Revived(0)` plus health. Its cell removal/re-add
  calls are commented out as faulty. This is a protocol lead, not a complete
  hospital implementation or verified recovery-health formula.
- [missile.cpp, lethal player damage](https://github.com/InfiniteRasa/Game-Server/blob/4a9ab5f1fcdf6a18ab6911c384189cc41ddae651/src/missile.cpp#L168)
  sets dead state and sends state change, actor killed, and player dead messages.
  Its documented `PlayerDead` arguments are source ID, graveyard collection,
  then `canRevive`. The collection contains dictionaries with `Id`, `pos`,
  `isSafe`, and `name`. However, it sends `PlayerDead` twice with contradictory
  argument order and hardcoded hospital information. Copying the whole block
  would preserve that defect. The collection type, optional fields, integer
  widths, and `canRevive` semantics need target-client confirmation.

An [archived firsthand gameplay account](https://www.shamusyoung.com/twentysidedtale/?p=1914)
describes choosing hospital transport or waiting for another player to revive
the fallen character. It corroborates the recovery choice but is not a versioned
1.16.5.0 protocol specification. Reported penalties from that account have not
been adopted as implementation values.

## Implementation sequence

1. Build a hospital lookup from typed existing world records. Keep destination
   ID, map, and position under server control. Establish the retail eligibility
   rule for discovered hospitals and contested control points, plus recovery
   destinations for playable maps without a hospital. Do not assume that all
   hospitals are permanently safe because the current loader initializes
   `WaypointInfo.Contested` to false.
2. Add the `ReviveMe` decoder and registered handler, and a single documented
   `PlayerDeadPacket`. Verify packet layout against the established final client
   and a retail capture where available before enabling lethal player state.
   Advertise only usable
   hospital choices. Keep ally resurrection requests distinct from hospital
   transport; the existing `RequestRevive`, `RefuseRevive`, and
   `ReviveRequestInfo` opcodes are unimplemented leads.
3. Implement hospital recovery first. Require a dead player and validate the
   selected ID against the choices offered for that death. Reject unknown,
   out-of-map, unoffered, or stale choices without consuming the player's chance
   to recover. Repeated requests must not replay a teleport or restore health
   repeatedly. Decide the return health/armor/chi values from evidence, not the
   C++ hardcoded full-health shortcut.
4. Use a shared authoritative relocation operation: update server position and
   rotation, send movement/teleport messages, reconcile cell membership and
   visibility, and persist the destination. Existing `SelectWaypoint` builds
   client movement messages but does not assign `client.Player.Position`;
   copying it as-is would rely on a subsequent client movement packet. Existing
   `TeleportAcknowledge` only sends `TeleportArrival`. Validate same-map recovery
   and any required parent-map transfer separately.
5. Connect lethal damage to a single death transition: clamp health to zero,
   maintain dead state, stop autofire and unfinished actions that require being
   alive, suppress movement/ordinary action requests while dead, send stable
   health/state notifications and the recovery UI. Preserve a distinction
   between unfinished casts and projectiles already in flight; whether to
   cancel the latter is not established by this audit. The hospital handler
   must restore normal control and clear the death UI before returning to play.
6. Handle disconnect/reconnect and map loading explicitly. Persist the minimum
   state needed for a valid recovery continuation, or establish the actual
   retail login recovery rule. Do not quietly rely on `MapLoaded` full reset
   and call the death lifecycle complete.

Do not deploy only step 5. Complete and validate the connected recovery path
first so removing the refill cannot strand players. This is an implementation
dependency, not a request for user approval or a reason to invent destinations.

## Required verification

- Nonlethal and lethal hits; repeated damage to a dead player; death and revive
  packets queued before serialization without one overwriting another.
- Hospital selection, rejection, and retry; stale/duplicate requests; destination
  coordinates taken from the server; unavailable hospital and missing-map data.
- Old and new observers see the player in the correct cells after recovery;
  the owner can move and fight again, and destination persistence survives login.
- Autofire, queued ability actions, and movement cannot keep a dead player active.
- Disconnect during death, recovery selection, and map transfer cannot leave a
  character permanently dead, duplicate the actor, or bypass the intended state.
- A real-client lethal hit opens the expected UI; hospital selection closes it,
  relocates correctly, restores control, and works a second time. Unit tests alone
  cannot prove the UI/packet handshake.

No durability loss, XP/currency penalty, trauma debuff, healing lockout duration,
revival timer, or fixed hospital healing percentage is specified by this audit.
Those remain separate versioned-evidence work after the playable lifecycle.
