# New character to endgame preservation order

The user's 2026-09-12 instruction establishes this implementation order for the
final live Tabula Rasa preservation target. This is an ordered work record;
none of the segments below is currently certified as complete or 1:1.

| Order | Playable segment | Required behavior |
| --- | --- | --- |
| 1 — current | Creation and first login | Family/character names, allowed appearance/races, initial inventory and equipment, training/point state, starting location, first-login and boot-camp options, persistence |
| 2 | Final live boot camp | Original mission/NPC sequence, tutorial events, combat and equipment use, Logos acquisition, rewards, failure/retry behavior, exit and supported skip path |
| 3 | Wilderness and first class advancement | Early missions and encounters, loot/economy, leveling and training, level-5 class choice, clone workflow, travel and instance progression |
| 4 | Tier-two progression to level 15 | Both branches, original missions/regions/instances, inherited and new abilities, level-15 advancement and cloning |
| 5 | Tier-three progression to level 30 | All four branches and their content, progression dependencies, level-30 advancement, signatures and cloning |
| 6 | Tier-four progression to level 50 | All eight final classes, original late leveling content, gear, crafting, groups and required regional/system progression |
| 7 | Endgame and shutdown live state | Final instances, encounters, equipment and rewards, control points/PvP/social systems, final deployed content and documented final events |

Where original server data is permanently lost, the user's 2026-09-13
decision applies: reconstruct from footage, the original map and client data,
labelling every estimated value with its provenance tier, source and
uncertainty (see `AGENTS.md`). The boot camp is the first segment built this way.

Within each segment, recover original data and observable behavior, implement
the required server flow, and verify creation/interaction/reconnect and failure
paths. Shared systems belong to the earliest segment that needs them. Working
unit tests establish software behavior; original artifacts and gameplay
comparison establish fidelity. Do not skip unresolved early progression by
substituting invented rewards, unrestricted gear or debug advancement.

Use static analysis of the recovered versioned client, archived official
material, contemporary direct gameplay footage/guides, and other emulator
source. Record source versions, exact locations or video timestamps, confidence
and conflicts. Keep acquired client/video artifacts and private character data
outside Git. An older guide or emulator's default is not by itself proof of
the final-live rule.

## Current creation findings

- Dated post-rewrite evidence establishes all five Recruit skills at rank 1
  with zero initial unspent points. Creation now persists these ranks together
  with the character and items in one transaction, with failure rollback.
- The starter pistol now receives durability from its own template.
- The first-family six-field creation message now has a handler, with the
  original `None` family state and persisted admission/replay checks.
- Fixed Recruit outfit colors match the original creation preview. Selection
  now checks the original two-field message, rejects overflowing slot values,
  and preserves session selection when ownership lookup or saving fails.
- The full initial inventory/placement, race and appearance eligibility,
  starting point, and first-login/skip state remain under reconstruction.
- Deployment 11 rebuilt boot camp. The archived Bootcamp page marks its own
  mission list obsolete; recover the later sequence before importing missions.
- The recovered later client identifies missions 1990, 1992, 1994, 1995 and
  the retry 2005, 21 objectives and ten NPC dialogue bindings. The current
  boot-camp map has no spawn-pool rows or mission definitions; start/reward/skip
  scripts remain unrecovered. See [the boot-camp audit](bootcamp-client-evidence.md).
- A verified 2026-09-13 sweep established that the original static map holds
  no gameplay actors at all, so first-login position, NPC placements and
  objects must come from captures or observation, not client data.
- The server now speaks the recovered mission-log and NPC objective
  conversation protocol with persistent per-character progress, which the
  tutorial's conversation objectives require. No tutorial content is loaded;
  incomplete definitions are withheld. See
  [mission research](mission-research.md#mission-log-protocol-and-persistence--2026-09-13).

- Three original recordings, the main one dated by player chat to the final week before shutdown,
  were transcribed, independently verified and matched against the client radar maps. They give
  measured positions for the arrival point, the Initiation trigger areas and the first NPCs and
  objects, and the observed rewards of the first missions. See
  [verified footage](bootcamp-client-evidence.md#2026-09-13-verified-footage-what-three-original-recordings-establish).
- The server has the data layer for labelled reconstruction (S0 of the boot-camp build plan): content
  rows are validated and withheld unless every mechanic they need exists, and a new-character entry
  switch defaults to the Wilderness start. No boot-camp content is seeded yet; the owner decisions for the
  first slice are recorded below.

## S1 (Initiation) status

- The S1 mechanism is implemented and committed (`3443cd6`): the mission-content runtime (rules,
  conditions, area sweeps, radio offers, forced greetings, tutorials, Logos grants and stationary
  creature placement) plus the packets and hooks it needs. `MissionContentRules.Implemented` now
  declares exactly the S1 mechanics and the entry path reads as implemented, still gated by
  `Bootcamp.EntryMode` (default `Disabled`). No boot-camp rows are seeded, so every hook is a runtime
  no-op; the full suite (756 tests) passes.
- S1 positions are recovered in `docs/evidence/bootcamp-d11-positions.json`: `spawn.first_login`,
  `area.1990.1`, `area.1990.2` and `npc.mcallister`. X,Z are the verified-footage radar measurements;
  Y is read from the decoded `adv_bootcamp` map heightmap (`t000007c1_terrain.glm`, context 1985),
  whose decode reproduces a static prop base to 0.03 m. Each record carries its tier, uncertainty,
  method and frames. `area.1990.2` uses the measured objective-2 marker because the build plan's
  `ArchElohHologramPlatformLarge` anchor sits by the far Eloh hologram; the discrepancy is recorded
  in the record.
- The twenty-one footage events the S1 rows cite are extracted (with verifier corrections applied) into
  `docs/evidence/bootcamp-d11-footage-events.json` from the A1/A2/A3 transcripts and verifications.
- The S1 content is seeded by the `BootcampS1Initiation` data migration (SQLite and MySQL, generated with
  `dotnet-ef 5.0.1`, parity-checked) from `src/Rasa.DBL/Migrations/BootcampData/BootcampS1InitiationRows.cs`:
  the start location, McAllister, mission 1990 with its objectives, transition and rewards, the two
  objective areas and bindings, the four rules and five actions, and the placement. The 23 manifest rows
  record every value's tier and citation. OD-11 resolved McAllister's unrecoverable `class_id` and
  `max_hp` as labelled analogues (entity class 3846 `NPC_Human_Swapset_Male`, the class the world data
  uses for AFS human commanders; 1000 hp at level 10). The full suite (777 tests) passes.
- Remaining S1 verification: deploy the candidate, set `Bootcamp.EntryMode`, and run the owner client
  check against the footage; the planned `ReconstructionSeedTests`, `PositionFileTests`,
  `ReconstructionDefinitionTests` and `SliceReadinessReport` are not written yet.

## S3 (private instances) status

- Context 1985 is a private per-character instance (migration `BootcampS3PerCharacterInstancing`, manifest
  row `content_map_setting` 1985, OD-2). `MapChannelManager.ChannelForEntry` gives a character entering a
  per-character context its own `MapChannel` (instance ids from 2, never 0 or 1), populated from the
  context's content placements; the previous instance of that character is released, and an instance is
  destroyed after the tick in which it empties (creatures, objects and loot leave the entity tables).
- Login, `ChangeMap` and the dropship arrival resolve the channel, and `Wonkavate` carries its instance
  id. Creatures and objects remember their channel, so cell broadcasts, NPC conversation, mission NPC
  resolution, vending, object use, weapon and Lightning targeting and missile hits only act within the
  requester's own channel. The dropship list and waypoint selection refuse per-character contexts.
- `PrivateInstanceTests` cover creation, isolation at identical coordinates, destruction after the owner
  leaves, replacement on re-entry and the instance id sent at login. Full suite 815/815. Owner client
  check (two recruits at once, relog) is still to do.

## S4 (Capture the Flag) status

- Mission 1994 is seeded by `BootcampS4CaptureTheFlag` (SQLite and MySQL, frozen rows in
  `BootcampData/BootcampS4CaptureTheFlagRows.cs`, parity-checked; 41 manifest rows). DeSimone gives it after 1992
  is completed; objective 4 (promotion conversation) is revealed on acceptance and a rule grants its 500 XP;
  4 → 2 → 1 → 3 follows the footage. Objective 2 is an area trigger at the measured cave-in icon (radius 5 m
  inferred); objective 1 is a kill of the Tizzik Gi placement with the "Boss Eliminated 0 / 1" counter; objective 3
  is Youngblood's conversation. Turn-in pays 5000 XP (observed). Indicators 437 (observed id) and 439 (inferred id)
  sit at measured positions.
- Creatures: Captain Youngblood (class 3846 and level 10 analogues, stationary, package 2561), Tizzik Gi (class
  10503, 1000 hp, run 9 analogues per OD-17; level 10 inferred from the 50-credit line, 143 XP conflict recorded) and
  a level-2 Thrax Infantry Initiate template (class 29769, 555 hp, run 9/walk 5 analogues). Fifteen Initiate
  placements stand at the measured engagement positions and guard their spot; the boss is placed at indicator 437
  (inferred) and is present while objective 1 is incomplete; Youngblood is present once objective 1 or the mission is
  completed.
- `creature.action1` is now a required column in the provenance registry (0 stays correct for non-combat NPCs), and
  both hostile templates use emulator `creature_action` 33 as a labelled analogue: its attack pair (1, 1) equals the
  final client's `Weapon_Creature_Bane_Pistol_Bootcamp` (weaponclass 29884); range, cooldown and damage remain
  emulator-authored.
- 21 positions and 54 footage events were added to the evidence files. New gaps: `GAP-S4-CREATURE-ACTIONS` (resolved for
  seeding, damage values open), `GAP-S4-PLACED-AI` (closed by `d0242e5`), `GAP-S4-AMBIENT-PRESENCE`,
  `GAP-S4-UNMEASURED-KILLS`, `GAP-S4-BOSS`, `GAP-S4-ALLY-ESCORT`, `GAP-S4-MISSION-CREDITS`, `GAP-S4-OFFER-RULE`,
  `GAP-S4-INDICATOR-PRESENTATION`, `GAP-S4-RESPAWN` and `GAP-S4-AMBIENT-LEVELS`; `GAP-CAVE-IN`, `GAP-ITEM-REWARDS` and
  `GAP-NPC-BODY` were updated (Youngblood has no appearance rows and renders without equipment). Not seeded: 1994
  credits and item reward, the 1992 turn-in offer rule, allies and escorts, kills without a position, respawn.
- `MissionContentLoadingTests.SeededBootcampContentGoesLiveWithTheImplementedMechanics` migrates an in-memory world
  and checks that the seeded 1990/1992/1994 and context-1985 content has no content or definition gaps under
  `MissionContentRules.Implemented`. Full suite 839/839 under the .NET 5 SDK image. Owner client check (build plan S4
  steps 1–9) is still to do.

## S5 part 1–2 (facts, objective timers, failure and retry) status

- Mechanisms only (the 1995/2005 rows came later, see the S5 seed section below). Content facts and the `fact_equals` / `has_logos` conditions
  persist per character and context.
- Objective timers run in wall-clock mode (OD-5). A timer starts when its objective is revealed (acceptance,
  transition or reconciliation) and is stored as remaining ms plus a Unix-ms anchor. The client receives
  `timeRemaining` in whole seconds, never 0 while the objective is open (original client:
  `missionlog.pyo` adds it to `gameclient.Time()`, `gameuiutil.FormatTextForTime` formats seconds). Completion
  clears the timer; a disarmed timer keeps counting on the client but never fails (B1-044: the countdown ran on
  through the plant until the objective completed).
- Expiry runs in the map tick after use recoveries, and at login before `MissionStatusInfo` (silently, because the
  client has no log yet). It fails the objective (`ObjectiveFailed`) and, with `on_expire` 2, the mission
  (`MissionFailed`, state 2 persisted), in one transaction with the `objective_failed` then `mission_failed` rules.
  Completion after the deadline but before the tick is refused. Failed missions are left out of later
  `MissionStatusInfo` because `Recv_MissionFailed` removed them from the client's log.
- Prerequisite state `NotAssigned` means "no row". A failed mission is offered and accepted again only when a
  satisfied prerequisite group names the mission itself as failed; acceptance replaces the failed row. NPC markers
  of missions whose prerequisites name a changed mission are refreshed too.
- The validator withholds a timer that can fail its mission when no prerequisite names that mission as failed, and a
  timer on a non-abandonable mission. `MissionLogTests.Timers` covers start, whole-second rounding, completion,
  failure order and persistence, once-only expiry, the retry and self-retry, offline expiry, and disarmed timers.
  Full suite 835/835 under the .NET 5 SDK image.

## S5 part 3 (bomb, generic use, placement state) status

- Mechanisms only (the 1995/2005 rows came later, see the S5 seed section below). Planting a bomb (use recovery on a state-113 bomb
  placement) sets state 114 and starts `fuse_ms`. In one transaction it disarms the timers of objectives its
  detonation completes and commits the `placement_state_entered` 114 rules. The client's countdown keeps
  running (B1-044) but can no longer fail the objective.
- The map tick restores destroyed placements (not wired before), detonates burnt-down fuses (state 115), then
  expires timers. Detonation completes the bound `placement_state` objective in the same transaction as the 115
  rules, crediting the instance owner (the arming character in a shared world).
- A usable with `alternate_state_condition_id` waits for its owner. A bomb rebuilt armed (planted-bomb fact)
  gets a fresh fuse, so a logout while the fuse burns still completes the objective once.
- Generic use is the client's StatelessSwitch augmentation 8 (state 44 only returns to itself) and completes
  `use_completed` bindings. Usables refresh their per-client enabled state after every commit and on map entry.
- `BombPlacementTests` and `ABombPlantedBeforeTheInstanceWasRebuiltComesBackArmedWithAFreshFuse` cover this.
  Full suite 844/844. Deployed 2026-09-14 19:27Z together with the S4 seed and navmeshes. Detonation damage to
  the player (B1-049 "-21") is still a gap.

## S5 (Calling for Reinforcements) seed status

- Missions 1995 and 2005 are seeded by `BootcampS5Reinforcements` (SQLite and MySQL, frozen rows in
  `BootcampData/BootcampS5ReinforcementsRows.cs`, parity-checked; 59 manifest rows). Youngblood gives 1995 after 1994
  is completed. Objective 2 (the unnamed wounded soldier, package 2584) is revealed on acceptance, then 2 → 3 (use
  Conrad's corpse) → 1 (the bomb reaching its detonated state 115) → 4 (Van Valkenberg, package 2564); 1 → 4 is
  observed (B1-050/051), the rest inferred. Objective 1 carries the 600 s timer (analogue, OD-6) that fails the
  mission; the receiver is Rogers (creature 100, inferred; `GAP-ROGERS`). No reward is seeded (`GAP-S5-MISSION-REWARDS`).
- 2005 (level 3 inferred) has objectives 1 → 4 with the same bomb binding and a 600 s timer ("You've got ten minutes.",
  inferred); it is offered after a failed 1995 (no 2005 row) and again after its own failure.
- Usables: the bomb (class 7870, inferred) with windup 1420 ms and fuse 4930 ms (both measured ±150 ms), usable once
  the bomb was taken from the corpse while its objective is open, present while `bootcamp.dropship_destroyed` is not
  set, and rebuilt armed while `bootcamp.bomb_planted` is set; Conrad's corpse (analogue 21961 generic use, position
  inferred, windup 0); the wreck (class 24586 as a Structure, closed 31, open 91 once the dropship is destroyed).
  Rules: planting sets `bomb_planted`; detonation sets `dropship_destroyed`, clears `bomb_planted` and opens the wreck;
  a failed bomb objective of 1995 or 2005 clears both facts. Abandoning clears nothing, so the D13.4 stuck state is
  preserved on purpose (`GAP-D13.4`).
- Creatures: Van Valkenberg (name 10574; class 3846, level 10, 1000 hp analogues) at his measured radar position, the
  unnamed wounded soldier (class 3846 analogue) at an inferred bunker position, Infantryman L2 and L3 and the Forean
  Gunner Initiate L3 (analogue classes 21910/21900/6239; levels observed, the Infantryman levels from frame reading
  S5P-01) at inferred pad positions. Van Valkenberg and the reinforcements are present once the dropship is destroyed
  and stand still. One level-1 Thrax Infantry Initiate (the S4 template's analogues, `creature_action` 33) guards the
  measured outpost engagement position. Indicators 435 (1995,2) and 432 (1995,1) use inferred ids at measured positions.
- Conflicts with the build plan recorded in the manifest: C2-16 is a different, static wreck 250 m from the pad; the
  pad-centre radar icon is the (1995,4) indicator, not Van Valkenberg; the wounded soldier's position is inferred,
  not measured; the bomb lies south-west of the player (heading 230 deg), not north; the fuse is 4930 ms state to
  state, not 5300 ms; B2-012 names no reinforcement level.
- 14 positions, 49 footage events (segments B2, B3, C1, C2 added) and sources `official_notes:d11` and
  `client_table:launch-2007-11` (pre-D11, comparison only) were added. New gaps: `GAP-S5-WRECK-MECHANISM` (closed by
  `fbd1539`), `GAP-S5-HIDDEN-XP`, `GAP-S5-WOUNDED-NAME`, `GAP-S5-CORPSE`, `GAP-S5-REINFORCEMENT-MOVE`,
  `GAP-S5-REINFORCEMENT-COUNT`, `GAP-S5-TIMER-START`, `GAP-S5-DETONATION-DAMAGE`, `GAP-S5-MISSION-REWARDS`,
  `GAP-S5-INDICATOR-PRESENTATION`, `GAP-S5-AMBIENT` and `GAP-BEAM-IN`; `GAP-BOMB-ITEM`, `GAP-D13.4`, `GAP-NPC-BODY`,
  `GAP-S4-AMBIENT-LEVELS`, `GAP-ROGERS` and `GAP-NEXT-SEGMENT` were updated. Not seeded: rewards, the hidden
  level-3-to-4 experience, a bomb item, detonation damage, unnamed reinforcements and their walk-off, the other
  outpost creatures, flyovers and the beam-in.

## S6 (Exit to Alia Das) status

- `BootcampS6ExitToAliaDas` (SQLite and MySQL, frozen rows in `BootcampData/BootcampS6ExitToAliaDasRows.cs`; 8 manifest
  rows) seeds the exit pad area (the original damaged-pad map entity, OD-8, radius 12 m inferred), its `area_entered`
  rule armed by (1995,4) or (2005,4) Completed, which transfers to location 19852 and then sets the account's
  skip-bootcamp flag (OD-9), the Alia Das destination (context 1220 at the first observed post-cut position, measured
  ±1.5 m, rotation ±30 deg; `GAP-S6-ARRIVAL`) and indicator 438 for (1995,4) (inferred id, measured position, observed
  3D marker). The trigger itself is unobserved (`GAP-S6-TRIGGER`); the rule fires once per stay, also when the
  check-in ends while the recruit already stands on the pad (`fbd1539`).
- `MissionContentLoadingTests.SeededBootcampContentGoesLiveWithTheImplementedMechanics` now checks 1990–2005 and the
  S5/S6 bindings, timers, prerequisites, indicators, conditions, rules, area and location against the manifest values
  with no content or definition gaps. `BootcampReinforcementsScenarioTests` plays the migrated content: accept 1995,
  talk to the wounded soldier, use the corpse, plant and detonate (wreck open, bomb gone, reinforcements present),
  check in with Van Valkenberg on the pad, transfer and account flag; and the failure path (D13.4 abandon, timer
  failure bringing the ship back, 2005 failing and retaken, then completed). Full suite 846/846 under the .NET 5 SDK
  image. Owner client checks (build plan S5 steps 1–8, S6 steps 1–5) are still to do.
- OD-25..OD-34 were decided by the agent on the owner's behalf because the owner asked the work to continue without
  pausing; they are marked `review_status: approved-by-agent-pending-owner-review` in the manifest.

## Boot-camp owner decisions

The user decided these open points of the boot-camp build plan on 2026-09-13, after the S0
deployment. They are recorded as approved decisions in
`docs/evidence/bootcamp-d11-reconstruction-manifest.json`, and every value that depends on them keeps
its evidence tier.

| Decision | Choice |
| --- | --- |
| OD-4 Power Logos (Lightning) | Silent `LogosStoneTabula` grant of Logos 23 when Initiation objective 1 completes, with greeting 1634 (inferred: no Logos chat line in the footage; the client binds its Lightning tutorial to greeting 1634) |
| OD-1 Entry rollout | All new characters enter the boot camp once Initiation (S1) is ready, even before the later missions and the exit exist |
| OD-3 Radial Menu tip 10000018 | Included on Initiation acceptance (observed id, inferred binding) |
| OD-11 NPC classes | Labelled final-live analogue classes where the class id is unrecoverable; names, levels and positions stay evidence-based |
| OD-2, 5–10, 12, 13, 15–18 (approved 2026-09-14) | The build plan's recommended defaults: per-character boot-camp instances with monotonic ids and separate squad instances; wall-clock objective timers with a labelled 600 s analogue for 1995; Youngblood gives retry 2005; exit pad at the damaged-pad map entity, and the exit sets `can_skip_bootcamp`; crate and second practice dummy by labelled match; the instance owner is credited for any kill of a bound placement; labelled final-live analogues for Tizzik Gi, the wreck, bomb, corpse and wounded soldier; no invented protection for mission NPCs. OD-14 (creation loadout correction) is still open. |
| OD-20 S4 scope and presence (2026-09-14) | Seed the S4 draft rows; Youngblood present while (1994,1) or 1994 is completed (he stays as the 2005 retry giver); ambient Thrax always present (optional default); ambient Initiates level 2 only; omitted items stay omitted |
| OD-21 S4 indicators (2026-09-14) | Indicator 437 for (1994,1) and 439 for (1994,2) at measured positions; radius and 3D effect left at neutral defaults and recorded as omitted |
| OD-22 S4 placement behavior (2026-09-14) | Thrax Initiates and Tizzik Gi use creature AI (guard the spot), labelled inferred; Youngblood stationary |
| OD-23 Creature attacks (2026-09-14) | `creature.action1` is required; the Initiate (OD-11, taken to cover ambient enemies) and Tizzik Gi (OD-17) use emulator creature_action 33 as a labelled analogue |
| OD-24 Tizzik Gi level and health (2026-09-14) | Level 10 inferred from the 50-credit kill rule (143 XP conflict recorded); 1000 hp analogue per OD-17 |
| OD-25 S5 wreck (2026-09-14, agent, pending owner review) | Class 24586 as a Structure (usable kind 5), closed 31, open 91 while the dropship is destroyed; the detonation rule opens it; class tier inferred |
| OD-26 Conrad's corpse (2026-09-14, agent, pending owner review) | Analogue class 21961 as a generic use (state 44), usable while (1995,3) is open; windup 0 as drafted |
| OD-27 Wounded soldier (2026-09-14, agent, pending owner review) | Unnamed NPC (name_id 0) on class 3846 with package 2584 at the inferred bunker position |
| OD-28 S5/S6 indicators (2026-09-14, agent, pending owner review) | Inferred ids 432, 435 and 438 at measured positions |
| OD-29 Reinforcements (2026-09-14, agent, pending owner review) | Both Infantrymen and the Forean Gunner Initiate at inferred pad positions, present once the dropship is destroyed, stationary; Infantryman levels observed from frame reading S5P-01 |
| OD-30 Bomb timings (2026-09-14, agent, pending owner review) | Fuse 4930 ms and windup 1420 ms (measured) |
| OD-31 2005 level (2026-09-14, agent, pending owner review) | Level 3 inferred; the hidden level-up experience is not modelled (`GAP-S5-HIDDEN-XP`) |
| OD-32 Exit trigger (2026-09-14, agent, pending owner review) | Radius 12 m inferred; (1995,4) or (2005,4) Completed; transfer to 19852, then the skip flag |
| OD-33 Level-1 outpost Thrax (2026-09-14, agent, pending owner review) | Seeded, guarding its spot, `creature_action` 33 and class 29769 analogues as the S4 Initiate |
| OD-34 1995/2005 receiver (2026-09-14, agent, pending owner review) | Rogers (creature 100) as drafted; not rejected by the validator or `DefinitionGaps` |

Detailed evidence: [new-character initialization](new-character-client-evidence.md),
[starter equipment](starter-equipment-research.md),
[character progression](character-progression-client-evidence.md), and
[retail accuracy](retail-accuracy.md).
