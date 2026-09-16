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
- Rogers at Alia Das (`GAP-ROGERS` closed, 2026-09-14, OD-35): `BootcampFixRogersTurnIn` (SQLite and MySQL, frozen
  rows in `BootcampData/BootcampFixRogersTurnInRows.cs`; 2 manifest rows and 2 `changes` entries) seeds the reserved
  creature 198514 Outpost Commander Rogers (name 2973; level 20 observed in B3-020; class 3846 and 1000 hp analogues
  under OD-11), places him in shared context 1220 at the measured command-tent position (855.84, 294.14, 387.40)
  ±1.5 m from the radar handset icon, rotation 250 deg ±30 inferred, stationary, package 116 original, always present
  (placement 198684, position `npc.outpost_commander_rogers`), and changes the 1995/2005 receiver from emulator
  creature 100 to 198514. Creature 100 (pre-D11 values, outside the reserved keys) and its spawnpool (counts 0/0) are
  untouched. The scenario tests now turn 1995 and 2005 in at the placed Rogers (refused out of range, MissionComplete
  marker, completion persisted). No appearance rows are seeded (`GAP-NPC-BODY`) and the turn-in pays nothing
  (`GAP-S5-MISSION-REWARDS`). The segment-3 arrival draft still names creature 100 and placement 122150 and must be
  rebased on 198514/198684. Full suite 846/846 under the .NET 5 SDK image.

## W1 (Wilderness arrival: Training Day) status

- `WildernessArrivalTrainingDay` (SQLite and MySQL, frozen rows in `WildernessData/WildernessArrivalTrainingDayRows.cs`;
  17 manifest rows, slice W1, recorded in the boot-camp manifest under OD-36) begins segment 3 at the boot-camp exit.
  It seeds:
  - Training Officer Kincaid, reserved creature 198515 (name 10604 original; level 8 inferred from a partly legible
    target-frame glyph, B2-036; class 3846 and 1000 hp analogues under OD-11/OD-37). He stands stationary in shared
    context 1220 in front of the barracks tent at (765.40, 294.12, 386.05) ±2 m, measured from one radar viewpoint
    (check SEG3-03) and 0.6 m from the client "Class Trainer: Alia Das" marker; rotation 90 deg ±45 inferred;
    placement 198685, position `npc.training_officer_kincaid`. He carries package 2588, which the class-trainer code
    (`ClassAdvancement`, `c3707cf`) recognises.
  - Mission 1526 "Training Day": radio giver 0; receiver Kincaid; level 4 inferred; category 10000001
    "Class (Recruit)" and shareable false inferred (OD-39). Objective 1 "Report to a Class Trainer." (ordinal 1 and
    required inferred, revealed observed) replaces the NULL skeleton row and completes through the client conversation
    (1526,1,2588,1,1). Rewards: 120 credits (observed, partly legible) and a choice of the Vextronics Pistol (116929) or
    the Vextronics Pulse Pistol (116930). Both template ids are inferred (OD-38); 121053/121086 are the recorded
    alternative.
  - The forced Headquarters offer: rule 1985011 (entered_map in 1220) with condition 198910, (1995,4) or (2005,4)
    Completed and no 1526 row, dispensing 1526 with `forced = true`. Decline is greyed in B2-023, and the client greys it
    only for forced offers. No prerequisite row is seeded, because the radio rule is the only offer path.
  - Item template rows for 116929/116930 (`itemtemplate`, `itemtemplate_weapon`); their class, skill and level
    requirement rows are original seed rows. Observed from the re-read tooltips B2-026/B2-029: range 20, alt damage
    115/122 physical, AE type 0. Inferred: quality 3, inventory category 1, ammo per shot 1, windup/recovery/refire
    0/250/150 and 0/250/100, attack type 2. Labelled analogues (OD-38): Not Tradeable and Not Sellable from the equipped
    AccuMax Shotgun tooltip (B2-027), no Bound/BoE/Unique, lockbox placeable, and the emulator's uniform weapon
    placeholders for the remaining sent fields. Prices are stored as 0 (`GAP-W1-ITEM-PRICES`). The module ids
    900221/900256 cannot be stored (`GAP-W1-REWARD-MODULES`).
- `ProvenanceRegistry` now lists `itemtemplate` and `itemtemplate_weapon`. Flags, quality, category and every weapon
  column the client is sent are required; `buy_price`, `sell_price` and the unsent `reuse_override` are optional. The
  slice vocabulary gains `W1`. The curated footage events add B2-026/027/029 (tooltips; verification.json has no
  verdict, so a curator re-read of the upscaled frames, 2026-09-14, is recorded in each description) and B2-036/037.
- `MissionContentLoadingTests` loads the pistols through the real `ItemManager.LoadItemTemplates` from the migrated rows.
  The class-map, requirement and item/weapon-class seed rows are stood in with their deployed values in
  `RewardItemFixtures` (renamed from `TrainingDayRewardItems` when W2 added the class gear). The test finds no content
  gaps, empty `DefinitionGaps` and reward gaps for 1526, the
  offered pistol choice, weapon rows that write a template tooltip, Kincaid live in 1220 and the live entered_map rule.
  `BootcampReinforcementsScenarioTests` enters Alia Das after the 1995 turn-in (and, on the 2005 path, before the
  Rogers turn-in, the observed order). Both paths receive the forced offer, accept it, see no repeat, report to Kincaid,
  are refused without a valid choice, and turn in for 120 credits plus the chosen pistol in the first equipment slot,
  persisted.
- Corroboration (2026-09-15, Wayback pass): Ellatha/DaOpa's mission DB names Training Day's reward as
  **180 credits** plus a choice of **Vextronics Pistol** (62 physical, 20 m, Standard Grade Cartridges 0/20) or
  **Vextronics Pulse Pistol** (72 EMP, Power Cells 0/10), with Soldier Trainer **Bukowski** and Specialist Trainer
  **Hoffman** as the tier-1 trainers at the barracks tent - independent agreement with the two pistols this slice
  seeds (116929/116930). The DB is early-2008 content (`work/wayback-era-findings.md`), so 180 credits is `inferred`;
  the experience figure stays open.
- Open: Training Day experience (`GAP-W1-1526-XP`); the offer delay (`GAP-W1-OFFER-RULE`); no offer for characters
  who skip the boot camp (`GAP-W1-SKIP-TRAINING-DAY`, OD-40); Kincaid's facing, level and appearance
  (`GAP-W1-KINCAID-PRESENTATION`, `GAP-NPC-BODY`); the template choice (`GAP-W1-REWARD-TEMPLATE-ID`); the weapon
  placeholders (`GAP-W1-WEAPON-PLACEHOLDERS`); and the emulator Major Bonham spawn beside the arrival, kept although the
  2009-01-06 player map still lists him (`GAP-W1-BONHAM`, OD-41). Missions 2010/2011 were closed by W2
  (`GAP-W1-GEAR-MISSIONS`). Owner client checks are still to do.
  Full suite 849/849 under the .NET 5 SDK image.

## W2 (class gear: missions 2010/2011 "Getting It In Gear") status

- `WildernessClassGear` (SQLite and MySQL, frozen rows in `WildernessData/WildernessClassGearRows.cs`; 49 manifest rows,
  slice W2, recorded in the boot-camp manifest under OD-43) closes `GAP-W1-GEAR-MISSIONS` and answers the tier-2 class
  choice the previous slice's `class_selected` event was added for. It seeds:
  - Quartermaster Caufield is the world seed's own creature: spawnpool 210 spawns 132 "AFS Quartermaster Caufield"
    (name 2992, level 10, class 29423) in shared Alia Das at the supply tent, 1.6 m from the pre-D11 TaRapedia `/loc`,
    and the client mission texts name him there and bind both completions to package 133. `npc_package(132 -> 133)`
    therefore attaches the original dialogue package to the original creature, and no duplicate is placed. His class
    29423 is a plain Redshirt body (client entityclass augmentation list [1], no NPC augmentation), so the NPC load was
    fixed to build the NPC record from the mission/package data instead of requiring the class augmentation: before the
    fix the first mission naming him aborted startup with a null-reference in `CreatureInit`, and the package row was
    silently dropped (`GAP-W2-CAUFIELD`, `GAP-NPC-BODY`).
  - Missions 2010 "Getting It In Gear: Soldier Class" and 2011 "…: Specialist Class": radio giver 0, receiver 132,
    level 5 inferred, category 10000002/10000003 "Class (Soldier)/(Specialist)" original, shareable and radio-completable
    false inferred. One objective "Report to Quartermaster Caufield" (ordinal/required/revealed inferred) replaces the
    NULL skeleton row and completes through the client conversation (2010/2011,1,133,1,1).
  - The class-gear offer: rules 1985012/1985013 (`class_selected` in 1220) each with a two-term OR condition
    (198911/198912): the chosen class (2 or 3) and no row of the matching mission yet, dispensing the mission with
    `forced = true`. The trigger exists since `65cafcb`; the dispatch itself is inferred from the broadcast opening and
    the client's non-abandonable list (`GAP-W2-GEAR-OFFER`).
  - The D11 class load-out, as fixed-item rewards (type 4, one of each): Soldier = Reflective armor helmet/vest/gloves/
    legs/boots (122859/122860/122862/122863/122864) plus the Rage-O-Matic machine gun (122865); Specialist = Hazmat
    helmet/vest/gloves/legs/boots (122866/122867/122868/122869/122870) plus the Repair-O-Matic repair tool (122871). The
    template ids, their item classes (18504/18596/18458/18550/18412/27059 and 13710/13802/13664/13756/13618/12797) and
    the class-owned skills that carry them (21/22 for Soldier, 30/14 for Specialist, client `skilldata`) are original
    client data; the per-mission split is inferred, and no XP or credit reward is recovered
    (`GAP-W2-GEAR-REWARDS`).
  - The `itemtemplate` rows for all twelve templates, the `itemtemplate_armor` armor values for the ten armor pieces
    (the client itemclass `max_hp`, 126/189/63/158/95 and 95/142/47/118/71) and the `itemtemplate_weapon` rows for the
    two weapons. The D11 block's quality (2, green) and trade/binding flags, the neutral 0 prices and the world seed's
    uniform machine-gun/tool weapon family row are labelled analogues under OD-43
    (`GAP-W2-ITEM-FLAGS`, `GAP-W2-ITEM-PRICES`, `GAP-W2-WEAPON-PLACEHOLDERS`).
- Tests: `ContentSchemaMigrationTests` seeds and rolls W2 back (missions, package, templates, conditions and rules, and
  the restored NULL skeleton row); `MissionContentLoadingTests` finds no content or definition gaps for 2010/2011, checks
  the reward list, the class skill and level-5 requirements and the two weapon rows; `BootcampReinforcementsScenarioTests`
  chooses each class at Kincaid in Alia Das, gets the forced offer for the matching mission, reports to Caufield, turns it
  in and receives the whole six-piece load-out, and sees no repeat. Deployed to the live world
  database on 2026-09-15: 14 content rules, 130 content rows, 0 gaps.
- The NPC load was an emulator defect this slice exposed: `CreatureInit` dereferenced a null `Npc` for any mission giver or
  receiver whose creature class carries no NPC augmentation (a startup abort once 2010/2011 named the Redshirt-bodied
  Caufield), and it bound an `npc_package` row only when the class had that augmentation, silently dropping the row. Both
  bindings now come from the mission and package data; `CreatureNpcBindingTests` covers them.
- For that check the live test environment was seeded on 2026-09-15: character 1 "Blizz" (account 4) was moved to
  level 4 with 43,000 experience in Alia Das so the tier gate is open, and account 4 was raised to GM level 10 (Admin).
  Both are test scaffolding rather than recovered data and can be reverted; `.chg_class` bypasses the class_selected
  event, so only training at Kincaid exercises the gear offer.
- Owner client checks are still to do for W2 as well. What to capture, in order: (1) the tier window and the level-5
  release at Kincaid; (2) whether the forced "Getting It In Gear" offer appears immediately, on the next map entry or not
  at all, its header and whether Decline is greyed; (3) the objective text in the mission tracker; (4) whether Caufield is
  interactable at the supply tent (name plate, interaction cursor, dialogue) - he is the Redshirt body without an NPC
  augmentation, so this is the open question; (5) the six granted pieces: names, icons, quality colour, tooltips, whether
  they equip at level 5 and whether any XP or credits were paid; (6) that the offer does not repeat after the turn-in.
  Full suite 853/853 under the .NET 5 SDK image.

## W3 (Alia Das hub: the first Wilderness missions) status

Reconnaissance only as of 2026-09-15 (nothing seeded yet). This is the next segment after W2: the missions the
character picks up in Alia Das once Training Day and the class choice are behind them. Evidence gathered in
`/home/blizz/backups/rasa-net/research/20260915-wilderness-bulk` (`work/client.json`, `work/tarapedia.json`,
`work/npc_resolution.json`, `work/membership.json`, and the `tools/` extraction scripts).

- **Givers, from the client's own texts** (`work/tarapedia.json` lists → zone Wilderness, area Alia Das, revision
  2008-03-06T13:23:08Z): Outpost Commander Rogers (River Recon, Miner Difficulties, Too Close For Comfort), Council Elder
  Solis (Receptive Reception), Lt. Colonel Cimoch (Wilderness Targets of Opportunity), Dr. Elise Corman (Supplies On The
  Double), Lt. Saviours (A Father's Goodbye), Quartermaster Caufield (Lurking In The Shadows), Warrior Apirka (Forming
  Alliances, Conscientious Objector I/II), Dr. Munson (Boargar Acquisition, Treelurker Samples, Mighty Miasma), Brigadier
  General Beacham (Orders From High Command), Receptive Liaison Langerman (the eight `Logos:` missions and Report to
  Liaison Standley).
- **Resolved client ids and the chain**, with TaRapedia's per-mission fields (giver, requirement, follow-up, XP, credits,
  reward items — each page carries `last_ts`/`revid` so the revision can be cited): Receptive Reception **1069** (Solis;
  objectives: locate the Logos shrine in Alia Caverns, return to Solis via package 168, speak to Apirka via package 112) →
  Forming Alliances **479** (Apirka; one objective, "Collect twelve Thrax hearts") → Conscientious Objector **1390/1391**
  (Apirka; 8 objectives, packages 112/1646; 2,000 XP / 400 credits) → Conscientious Objector – Part Two **1392/1393**
  (turned in to Rogers via package 116; 8,000 XP / 800 credits). Parallel from Forming Alliances: Lurking In The Shadows
  **427** (Caufield, 4,000 XP / 400 credits) and River Recon **429** (Rogers, 10,000 XP / 1,000 credits, follow-up
  Distress On The River). Separate line: Supplies On The Double **428** (Corman) → A Father's Goodbye **421** (Saviours
  gives it, Information Spec. Saviours pays it). The `Logos:` line (1638 and siblings) is the Logos training path.
- **What the seed already has**: objective skeletons and objective-conversation rows for all of these (`MissionClientObjectiveSkeleton`),
  but **no `npc_mission` rows** (only 429 has one, and the loader reports it "not offered, definition incomplete"), no
  rewards, no placements and no indicators. Package 116 (Rogers) is the only one of these NPCs already bound to a
  creature; 112 (Apirka) and 168 (Solis) have none.
- **The NPCs are already in the original world seed** (checked 2026-09-15 against `rasaworld.db`), so no position has to be
  inferred for the chain: Council Elder Solis **creature 42** (name 3005, level 10, spawnpool 184, 809.3/302.1/503.8),
  Warrior Apirka **43** (2969, pool 219 at 826.9/301.4/502.3 in Alia Das, plus pools 63/72/78 elsewhere),
  Dr. Munson **109** (6888, pool 187, 763.7/304.0/514.3), Lt. Colonel Cimoch **118** (9639, pool 196, 815.4/294.1/390.6),
  Lt. Saviours **120** (3073, pool 198, 784.2/294.5/367.1), Information Spec. Saviours **116** (3074, Twin Pillars,
  -757.6/175.0/-277.9), Receptive Liaison Langerman **133** (10010, pool 211, 871.0/294.2/385.4), Receptive Liaison
  Standley **134** (10011, Twin Pillars, -110.0/220.3/-494.3) and Outpost Commander Rogers **100** (2973, pool 100,
  870.0/294.2/385.5, already carrying package 116). Only **Dr. Elise Corman (2989) and Brigadier General Beacham (6733)**
  have no creature row yet. This supersedes the earlier note in this section that treated these positions as missing.
  Cross-check against Ten Ton Hammer's 2007 "Alia Das Missions" guide (`research/20260915-aliadas-hub`, era
  `pre_d11`, so structure only): the guide's `/loc` values are 0.9 m (Langerman), 1.2 m (Standley), 1.8 m (Rogers in the
  tent), 3.6 m (Caufield), 4.1 m (Lt. Saviours), 8.2 m (Munson) and 13.9 m (Apirka) from the seed spawns, so the map was
  not moved wholesale after 2007 and the page is a usable lead. **Solis is the outlier at 79.9 m** (guide 783/579.3 and
  TaRapedia's `/loc` 784.7/287.2/581.1 agree with each other, not with creature 42) - open question, not to be guessed.
  **The "two Rogers" question is answered**: the guide names a Rogers "in a tent at 869, 384" (the hand-off NPC, 1.8 m from
  creature 100) *and* the Conscientious Objector turn-in "at 855.6, 294.1, 389.0" (1.6 m from the footage's measured
  Rogers, 14.8 m from creature 100), so the boot-camp S6 slice's creature 198514 at the 855.6 hub is the mission-hub NPC
  and creature 100 is the tent NPC beside Langerman - both are the original data, and the arrival rebase should treat them
  as two NPCs rather than reconciling one away.
- **Objective mechanics settled by the guide** (structure, `inferred`): Forming Alliances' twelve Thrax hearts are a
  counter fed by **random drops off ordinary Thrax**; Receptive Reception is an **interaction** with the Logos shrine in
  Alia Caverns plus the two conversation completions the client already carries (packages 168, 112); Conscientious
  Objector is an **Ethical Parable with two branches** (arrest and lead back, or release and escort to the Divide
  entrance) which is why the client holds the 1392/1393 pair; Supplies On The Double **is on a timer**; Boargar
  Acquisition, Treelurker Samples and Mighty Miasma are counters of 8/5/6. Logos: Enhance is expected to be doable
  alongside Receptive Reception, and the other Logos missions unlock after it.
- **Evidence gaps left before seeding**: the reward items' templates (the guide's credit and item values are pre-D11 and
  cannot be seeded as analogues), the Thrax / Boargar / Treelurker / miasma drop wiring and their item templates, the
  Alia Caverns shrine volume and its usable, the timer for 428, and the branch conditions for 1390/1391. The owner's
  tracker video (Taildrop pending, inbox empty as of this writing) is expected to settle the drop/timer/branch mechanics,
  which have no client-visible signature.
- **Mission detail source found on disk**: the 20260913 source sweep had already cached the **DaOpa/Ellatha
  mission DB** (`research/20260913-source-sweep/guides/raw/ellatha-missions/m1..m98.html`), which the sweep rated
  "content retail, DB build date unverified". `research/20260915-aliadas-hub/tools/extract_ellatha_hub.py` pulls the
  26 Alia Das hub missions out of it into `work/ellatha-hub-missions.json` and `work/ellatha-hub-digest.md`
  (briefing, ordered objectives, tips, legend, reward rows with stats and requirements). Tier `inferred`; the sweep
  cross-correlated the same names and ordering with the **official** RGTR Wilderness walkthrough (2007-12-13) and
  TaRapedia carries giver/requirement/XP. It gives, for the first time, the per-mission detail:
  - **479** objective **"Collect twelve Thrax hearts — Thrax Heart 0/12"**: the counter and its item name; reward
    Luminar Motor Assist Armor Vest (Body Armor 154, Regen 8%/s, Condition 100%, [2] Resist: Laser +4%) + 400 credits.
  - **1069** is the shrine: the briefing says *"Near the waterfall is the entrance to Alia Caverns… activating the
    shrine should transmit the information directly into your mind"*; reward choice Titan or Prodigy Motor Assist
    Armor Boots (Body Armor 77, [3] Body/Mind +3, [2] Resist: Physical/Electric +4%, Min Level 3).
  - **1390/1391** are the two branches of the Ethical Parable, spelled out: *"Tell him that Milpas is free to leave
    Alia Das"* vs *"…you must do your duty and arrest Milpas"*, then *"Take Milpas to Apirka"* (escort) and
    *"Speak to Apirka"*; 1392/1393 is the report. 400 credits.
  - The rest of the hub's objective lists and reward items are in the digest (8/5/6 counters for Boargar/Treelurker/
    miasma, the Logos missions' six objectives each, River Recon's turn-in to Field Sgt. Weatherspoon in Lower Eloh,
    Supplies On The Double's timer, and so on).
  - Next concrete step: resolve those reward **item names** to client templates (`itemtemplatelanguage` /
    `itemclass`) and record which are recovered originals versus missing, then seed 1069 and 479.
- **Wayback pass (2026-09-15, after the archive came back)**: the **official RGTR Wilderness walkthrough is identical**
  in its 2007-12-13, 2008-09-14 and 2008-11-15 captures (title line aside), so its mechanics were still being published
  unchanged three months before shutdown; and the **Ellatha/DaOpa mission pages are byte-identical** in their Feb-2008
  snapshots and the live-2026 copies (chrome aside), which dates that DB to **early-2008 content, not a later build**
  (ids 9, 10, 20 and 21 have no 2008 snapshot). Recorded in `research/20260915-aliadas-hub/work/wayback-era-findings.md`.
  So the two mission sources are good for *structure* and not for *final values*; final values need the client's own
  tables, the D11 patch notes or footage. Still to sweep: the official patch notes near shutdown and TaRapedia mission
  revisions dated after 2008-08.
- **Batch 1 progress (2026-09-15)**:
  - `b82f235` added the Logos binding kind, so an objective can wait on a shrine activation.
  - `93b6b1b` fixed the S1 objective triggers that a live playthrough exposed: both areas stand on the ceremonial
    bridge but their Y came from the terrain under it (12.6/18.6 m below the deck), so the 4 m spheres could never be
    entered. They are vertical cylinders now. The same commit rebuilt `Add_map_region`'s Designer, which upstream
    PR #91 had shipped without the content tables — EF generates a migration's Down operations against the previous
    migration's model, so that omission broke any rollback through it.
  - `0e251a5` seeded **1069 Receptive Reception** (Solis 42 → Apirka 43, objectives 1..3, the Enhance-shrine binding,
    XP 4,000 and 600 credits). Deployed; the loader reports 131 content rows with 0 gaps.
  - **GAP-W3-1069-GATE**: TaRapedia gates 1069 on 1407 "Too Close For Comfort", but 1407 has no definition and the
    loader rejects a prerequisite naming an unknown mission, so Solis offers 1069 directly until 1407's slice lands.
  - **Gap pass (owner chose "emulator mechanisms", 2026-09-15)**: `1234cd3` closed **GAP-GM** by verification (new
    accounts default to level 0; only the test account was raised). `30653f2` added the missing **scripted creature
    movement** - `ContentRuleAction.MoveCreatureToLocation` (placement -> `content_location` with purpose 3), a
    `BehaviorManager.WalkTo` that hands the creature to the existing path-following action over the navmesh, and the
    catalog validation - then seeded two walks: McAllister to the S2 gear area at the 1990 turn-in (A2-044 "Follow me
    over to the ...") and the three placed reinforcements 15 m north-east off the pad when 1995 objective 1 completes
    (S5P-05). Destinations inferred +/-5 m. **GAP-S5-REINFORCEMENT-MOVE closed, GAP-ESCORT partly closed** (Youngblood's
    walk-in and a follow-the-player mechanism remain).
  - **W3 batch 3 (2026-09-16)**: five more Wilderness missions seeded mechanically, with the rule made explicit -
    431 Distress On The River, 442 Quarantine, 444 Unity Among Men, 549 Failure to Launch, 836 Incoming! Their
    givers resolved to single world-seed creatures, **every** objective already carried a client conversation row
    (so nothing is invented to complete them), and the experience and credits are TaRapedia's recorded values
    (2,500/500, 3,500/700, -/1,200, 3,000/600, 11,000/1,650). One trap found on the way: npc_mission_reward keeps
    *both* amounts in its `credits` column, so the first pass wrote experience 0 and the loader refused all five
    ("Experience reward amount 0 is not positive"); corrected by WildernessHubConversationChainRewards.
    751 Boargar Acquisition was deliberately held back - its objective collects eight samples, and a conversation
    alone would let the collection be skipped (**GAP-W3-COUNTER-OBJECTIVES**).
  - **W3 batch 2 (2026-09-16)**: the hub's conversation chain is live - **1390 Conscientious Objector** (the Ethical
    Parable: question Elder Quillas, either answer, report to Warrior Apirka), **1392/1393 Conscientious Objector -
    Part Two** (the report to Rogers) and **1407 Too Close For Comfort** (Moawi sends you to check on Solis). Their
    client objectives and conversation bindings were already in the seed; what was seeded is the definition: giver,
    receiver, the objective ordering and flags, the branch transitions, Elder Quillas' and Moawi's dialogue packages
    (114->1646, 38->113) and 1390's 400 credits. Two findings worth keeping: the client only exports *conversation*
    bindings, so 1390's escort steps (objectives 4, 8, 12) have none and are optional
    (**GAP-W3-1390-ESCORT**), and the content loader now only requires a completion binding for a *required*
    objective, since an optional one cannot strand a character. Rewards and the chain's gates are recorded as
    **GAP-W3-HUB-REWARDS** and **GAP-W3-HUB-PREREQUISITES** rather than guessed.
  - **W3 batch 3-5 (2026-09-16)**: the Wilderness hub's conversation missions went in by rule (431, 442, 444, 549,
    836), then its first two real fights through the boot camp's existing kill binding (427 Proctor Fulgor, 682 the
    Xanx), then the **Divide**, which needed its givers created first: Lt. Sebastian, Shaman Horea, Field Dr. Dawson
    and Receptive Liaison Brice, each with the client's own name id, TaRapedia's level/zone//loc and an appearance
    analogue under **OD-45** (see "NPCs created from evidence" below). Missions 332, 347, 382, 796 and 1743 came with
    them. 751 Boargar Acquisition was held back - its objective collects eight samples and a conversation alone would
    let the collection be skipped (**GAP-W3-COUNTER-OBJECTIVES**).
  - **W3 batch 6-7 (2026-09-16)**: the pipeline above then ran over the next two zones - **Palisades** (eight NPCs,
    nine missions) and **Valverde** (six NPCs: six missions on the plateau and in the pools). Eighteen NPCs and
    twenty missions came out of it in one session. The world position audit earned its keep: six of the eighteen
    stand where their map's navmesh has no polygon within reach of TaRapedia's /loc, all listed with that reason in
    **GAP-W3-NPC-POSITION-COVERAGE**.
  - **NPCs created from evidence (OD-45)**: name id = original (client `creaturenamelanguage`), level/zone//loc =
    inferred (TaRapedia, dated), appearance = analogue of a world-seed NPC of the same faction. This is the pipeline
    that unblocks the 660 missions with no giver in the world seed, and it is mechanical: resolve the package the
    client's conversation rows bind, take its identity, then create the NPC that owns it.
  - **Combat fidelity (2026-09-16)**: the resistance curve the client itself carries
    (`shared/damageresistance.pyo`, with Deployment 14's published table matching it on all eight points) was
    recovered, tested - and then never used. Nothing accumulated the equipped resist lists and no damage path read
    them, so resistance gear changed nothing in a fight. The equipment pass now sums each worn item's resist list
    per damage type onto the player, and the player damage path scales a landed hit by that resistance before
    armour absorbs it, through the same curve. Nine published points, the scaling arithmetic and the per-type sum
    are pinned by tests; the rounding the live server used is recorded as **GAP-D14-RESISTANCE-ROUNDING** rather
    than assumed.
  - **Whole-world position audit (2026-09-15, owner request)**: every position in the world is now measured against
    the walkable surface the navmesh gives at its XZ - 434 rows over content placements, locations, trigger areas,
    objective markers, Logos shrines and creature spawns. Our content must sit within 2 m of the surface; original
    data (Logos shrines, creature spawns) only must not be buried. Result: **one real defect of ours** - the inferred
    McAllister escort destination was 6.75 m under the ground (it had inherited the crate's platform-origin Y), now
    lifted to the measured 120.75 - and four explained rows listed with reasons in the test. Two new gaps came out of
    it: **GAP-LOGOS-POSITION-UNSET** (the original logos table has a shrine at exactly (0,0,0) on Palisades) and the
    2 m-under cavern shrine in Minos Caverns. All 35 content placements, all locations, all areas and all markers pass.
  - **Placement heights (2026-09-15, live report "I do not see Captain Delessio")**: the camp's placements are now
    measured against the walkable surface the navmesh gives at each XZ (original-tier: the navmesh is built from the
    client's map data), and `PlacementHeightAuditTests` repeats that measurement on every run, failing on anything
    more than 1.5 m off. 30 of 33 placements were already on the surface; Delessio and the 1992 supply crate were
    8.1 m **under** the grating platform (their Y came from the platform static's *origin*, which is its base, not
    its top at 122.11) and DeSimone was 2.1 m under. Fixed by `BootcampPlacementGroundSnap` +
    `BootcampPlatformTopCorrection`. The audit also exposed **GAP-S2-PLACEMENT-PROVENANCE**: the S2 slice's six
    placements have no manifest rows at all.
  - `e183d80` added the **detonation self-damage**: a `damage` column on `content_rule_action`, the
    `ContentRuleAction.DamagePlayer` action (armour first then health, death announced if the bar empties) and a
    damage-21 action on the bomb rule - the value read straight off the footage's "-21" health line, so `observed`.
    **GAP-S5-DETONATION-DAMAGE closed** (the knock-back stays open). Deployed: 16 rules, 142 rows, 0 gaps.
  - Remaining mechanism gaps, in the order the owner's choice implies: **GAP-S5-DETONATION-DAMAGE** (the footage shows
    "-21" self-damage at the blast; needs a damage action and therefore a value column on `content_rule_action`),
    **GAP-S5-CORPSE** (a corpse object with a windup and loot), **GAP-S4-ALLY-ESCORT** (allies following the player,
    which the new walk action can be extended into), and **GAP-ITEM-REWARDS** (the item grant planner is wired into
    turn-in already; the reward items need the prefix-family resolution recorded in
    `research/20260915-aliadas-hub/work/item-name-structure.md`).
  - Ellatha's NPC list was harvested (`tools/ellatha_npc_index.py`, `work/ellatha-npc-index.json`): only **71 NPCs**
    (58 Wilderness), so it is a cross-check for the hub, not the roster expansion the 660 no-giver missions need.
- **Order**: implement the chain in play order (1069 → 479 → 1390/1391 → 1392/1393, then the parallel missions), each with
  the W1/W2 discipline: frozen rows + paired migrations, a manifest slice `W3`, and content-loading/scenario tests.

## All-missions program (owner goal, 2026-09-15)

Goal: implement every mission of the final build (client 1.16.5.0 / D16.5), evidence-bounded, instead of
curating one slice at a time. Scaffolding lives in `research/20260915-aliadas-hub/tools/`.

**The scope, measured** (`work/mission-catalog.json`, `work/mission-catalog-summary.md`):

- The client carries **1,168 missions, 3,454 objectives and 1,727 objective-conversation rows**; our seed already
  carries **all 3,454 objective skeletons** (1,109 distinct missions) but only **10 fully defined missions**
  (`npc_mission` rows) — the boot camp, Training Day and the class-gear pair.
- `tools/build_mission_catalog.py` merges the client tables, TaRapedia revisions (with the `post_d11` final-era flag),
  the Ellatha/DaOpa detail, the package→NPC resolution and the world seed, and gives each mission a readiness verdict.
  Current verdicts: **10 implemented, 490 ready to seed, 668 blocked**.
- Per zone (top): Palisades 82, Wilderness 80, Mires 74, Incline 67, Plains 62, Plateau 61, Ashen Desert 58,
  Marshes 57, Divide 52, Crucible 49, Howling Maw 49, Pools 37, Abyss 35, Thunderhead 34, Descent 21, plus 289 with
  no zone and 59 with no objectives in the client tables.

**What unblocked 420 missions at once**: the world seed's `creature` table holds only **98 creatures (92 distinct
names)**, so resolving a mission's giver to an existing creature almost always failed. TaRapedia's **430 NPC pages**
carry, per NPC, the **level, zone, faction, a `/loc` coordinate triple and the list of missions that NPC gives**, so
they supply a roster and positions for the whole game. Missions anchored this way are labelled `inferred` and their
positions are pre-shutdown wiki `/loc` values: they must be cross-checked against the world seed where it has the NPC
(that check already caught Solis ~80 m out and confirmed Langerman/Caufield/Rogers/Cimoch to within metres) and against
the deployment notes for zones that changed late (Twin Pillars was excavated in D14, Enigma Cave disturbed in D15).

**Remaining blockers, by size**: 660 missions have no giver at all — of which many are the "Targets of Opportunity" and
device-given families whose giver is a *thing* ("Headquarters", "Radio", "Bane CommLink Terminal", "Holographic
Projection Device"), which the content layer can already dispense by radio rule; 289 have no zone; 59 have no
objectives in the client tables.

**Batch order** (progression first): Wilderness (the Alia Das hub, 71 ready) → Divide (19) → Palisades (48) → the
level-banded Torden/Valverde zones → instances → the D15/D16 endgame zones (Empire Sector, Edmund Range). Each batch
is seeded with frozen rows + paired migrations + a manifest slice + content-loading and scenario tests, and deployed
with a DB backup.

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
| OD-34 1995/2005 receiver (2026-09-14, agent, pending owner review) | Rogers (creature 100) as drafted; not rejected by the validator or `DefinitionGaps`; the receiver id is superseded by OD-35 |
| OD-35 Rogers at Alia Das (2026-09-14, agent, pending owner review) | A reserved creature 198514 placed in context 1220 and recorded in the boot-camp manifest, with the 1995/2005 receiver changed from 100 by `BootcampFixRogersTurnIn`, instead of updating emulator row 100 from a new Wilderness manifest; no appearance rows |
| OD-36 W1 record (2026-09-14, agent, pending owner review) | Training Day recorded in the boot-camp manifest as slice W1, continuing the reserved keys (creature 198515, placement 198685, condition 198910, rule 1985011); item template ids 116929/116930 get explicit non-reserved scope entries |
| OD-37 Kincaid class and health (2026-09-14, agent, pending owner review) | Class 3846 and 1000 hp as OD-11 analogues, as for the boot-camp officers and Rogers |
| OD-38 Training Day reward items (2026-09-14, agent, pending owner review) | Templates 116929/116930 inferred (alternative 121053/121086); item fields as observed/inferred/labelled analogues from the field matrix; prices 0 with a gap; module ids unstorable (gap) |
| OD-39 Training Day flags (2026-09-14, agent, pending owner review) | shareable false and category 10000001 "Class (Recruit)", inferred |
| OD-40 Skip path (2026-09-14, agent, pending owner review) | Characters who skip the boot camp are not offered Training Day (draft condition), recorded as a gap |
| OD-41 Major Bonham (2026-09-14, agent, pending owner review) | The emulator spawn stays untouched; the 2009-01-06 player map listing him is recorded as a gap |
| OD-44 Trigger radii (2026-09-15, owner) | An objective or area trigger takes a larger radius than a bare reading of the measurement suggests - enough that a player crossing the intended place cannot miss it - but not so large that it fires from outside that place. The radius is a reconstruction parameter and is labelled as one. Applied: S1's two bridge objectives 4 m -> 10 m (the bridge is 16.8 m wide and a recruit crossed it without touching the 4 m sphere; GAP-S1-TRIGGER-RADIUS) and 1994's cave-in trigger 5 m -> 10 m |
| OD-42 Missions 2010/2011 (2026-09-14, agent, pending owner review) | Held (not seeded) until a class-chosen trigger exists. Superseded for the seeded parts by OD-43 |
| OD-43 Class-gear missions 2010/2011 (2026-09-15, agent, pending owner review) | Seed 2010/2011 now that the `class_selected` trigger exists and the reward identities are evidenced from the final client (the D11 new-player item block 122859-122871 and its class-owned skills 21/22 and 30/14); keep the world seed's creature 132 as Quartermaster Caufield with dialogue package 133. Use the D11 block's uniform world-seed quality (2) and trade/binding flags, the neutral 0 prices and the world seed's machine-gun/tool `itemtemplate_weapon` family row as labelled analogues; the reward XP/credits, the item prices, the weapon fields, the offer presentation and Caufield's final position stay open (`GAP-W2-*`) |

Detailed evidence: [new-character initialization](new-character-client-evidence.md),
[starter equipment](starter-equipment-research.md),
[character progression](character-progression-client-evidence.md), and
[retail accuracy](retail-accuracy.md).
