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

- **2026-09-22 boot-camp rifle melee reconstruction:** the original client action
  `(174,5)` and 4 m range are now admitted by the alternate attack runtime;
  the crate rifle's inferred template carries the 82 Physical melee tooltip
  value observed in final-week footage. Thirty-three weapon attack tests and six
  content migration tests pass under .NET 5, including range, no-ammo and exact
  rollback checks. An isolated original client displays the 82 Physical melee
  tooltip, and the live server now runs this migration. An isolated targeted
  F-key attack killed a GM-spawned Thrax without spending ammunition; exact
  template identity, minimum damage and scaling remain gaps. See
  [rifle melee evidence](bootcamp-rifle-melee.md).
  The recovered client additionally bypasses normal empty-magazine and jam
  checks for the no-ammo melee alternate. That server fix is deployed in
  `rasa-dit-test:20260922c` and covered by 33 lifecycle tests.

- **2026-09-22 handoff and promotion:** the original client completes final
  Hartmann → DeSimone turn-in → Capture the Flag acceptance → level-2 promotion,
  then persists the result through normal logout. The combined 1300-test
  candidate adds item-instance metadata, correct module-tooltip framing and
  atomic kill-counter/objective completion with reconnect recovery. Final-live
  Laser module values are resistance ratings; melee action/range are recovered,
  while damage scaling remains open. The new candidate reconnect preserves all
  existing item fields. A normal
  cave approach ends in death before the trigger, followed by a successful
  hospital respawn. Firearms training saves correctly; attribute spending saves
  but exposed an available-point refresh defect. Reply ordering is now fixed
  and independently verified in the original client on a copied checkpoint.
  The final combined suite passes 1302/1302.
  A fresh continuing-checkpoint client also restores Firearms 2, zero skill and
  attribute points, and 13 in each primary attribute, then logs out normally.
  A later client pass verifies F-key rifle melee against a Thrax with no ammo
  loss. An isolated client teleport verifies the cave area trigger and its
  persistence through hospital respawn. A further isolated client check killed
  Tizzik Gi, completed Youngblood's conversation and turned in mission 1994;
  those checks used copied world data with boss HP set to one and hostile
  attacks disabled. A later [normal-health boss checkpoint](evidence/bootcamp-normal-boss-client-probe-20260924.json)
  killed Tizzik Gi through ordinary original-client rifle fire with the
  deployed-derived combat values and continued through Youngblood's dialogue
  and mission 1994 turn-in after reconnect. A further
  [S5 client continuation](evidence/bootcamp-s5-normal-chain-gate-20260924.json)
  accepted mission 1995 from Youngblood, restored its tracker after reconnect,
  and killed the first hostile Thrax at the west gate with ordinary movement,
  aiming and rifle fire. A separate ordinary movement run passed the gate to
  approximately (4.4,106.7,141.8), with objective 2 still active. Next: verify the uninterrupted
  camp-to-cave route, full S5 travel and encounter combat, and following boot-camp missions
  before closing this segment. See
  [continuous S5 route probes](evidence/bootcamp-s5-continuous-west-route-20260924.json),
  [soldier approach and original-map obstruction](evidence/bootcamp-s5-soldier-approach-20260924.json),
  [handoff](client-gearing-handoff.md),
  [item metadata](item-instance-metadata.md), [cave route and recovery](client-capture-the-flag-audit.md)
  and [rifle melee](bootcamp-rifle-melee.md).
  The 2026-09-23 [combat-companion audit](bootcamp-courtyard-allies.md) confirms
  that final-week footage shows named Forean Initiates fighting alongside the
  player from the courtyard into later fighting, while the S4 seed has none.
  Three named camp companions now have live mission-scoped follow and combat
  rows, with inferred placements and labelled stat estimates; the
  [live deployment record](evidence/live-bootcamp-camp-companions-20260923.json)
  reports 409 content rows and zero gaps. A copied-client
  [combat trace](evidence/bootcamp-camp-allies-combat-diagnostic.json) confirms
  all three damage one Thrax. A further copied-client
  [route diagnostic](evidence/bootcamp-camp-allies-route-diagnostic.json)
  shows all three following from the camp staircase through the raised walkway
  and rocky descent to `(360.07,121.20,114.64)`. A subsequent
  [courtyard approach](evidence/bootcamp-camp-allies-courtyard-approach.json)
  shows them still following near `(316.13,120.28,85.93)`, with a Thrax
  targetable ahead. The player then took a diagnostic line below the walkable
  route. A later [normal-approach combat run](evidence/bootcamp-courtyard-normal-approach-combat.json)
  kept all three allies at the courtyard edge but ended in Alden's death before
  the cave trigger, followed by all three ally deaths. The trace recorded no
  companion attacks in that run, unlike the isolated one-Thrax test. The
  engagement trigger, filmed player-attacking route, and cave transition are
  current-segment work. The observed unopposed death cannot by itself justify
  retuning hostile damage.
  A [forward-formation diagnostic](evidence/bootcamp-courtyard-formation-diagnostic.json)
  confirms all three companions acquire and damage a Thrax when staged ahead
  near the first filmed courtyard viewpoint. Original radar measurements in the
  [escort combat rule record](evidence/bootcamp-courtyard-escort-combat-rules.json)
  instead place one marked escort behind the player; the identities of visible
  defenders ahead remain unknown. The fighting AI's spawn-based 60 m leash
  obstructed camp escorts in the courtyard, so a followed-player leash centre
  and inferred assisted-target link are now deployed with the three companions.
  Focused .NET 5 checks confirm
  combat 130 m from camp, clean retreat when the player disappears, and that
  the escort assists a selected enemy without attacking a selected player.
  The isolated route with those
  changes stalled before combat, leaving its outcome unverified. The original
  recording cuts away during escort travel, and its six measured hostile combat
  positions do not
  establish simultaneous spawns. Preserve both as unresolved reconstruction
  questions before adjusting the encounter.
  A later [copied-client checkpoint](evidence/bootcamp-courtyard-escort-combat-rules.json)
  kept the three companions at camp while the recruit loaded at a verified
  point near the courtyard. They followed him to that point and, after two
  diagnostic same-map teleports, each dealt 11 hits to the only Thrax left in
  the disposable world. The single enemy had analogue health/armor values;
  natural-route movement and original six-enemy timing remain unverified.
  The final-week recording also targets a separate level-2 Forean Warrior in
  the courtyard. One [estimated defender](evidence/bootcamp-courtyard-forean-warrior.json)
  is now staged on an original-map walkable point with original-client
  class-name fallback; migration and provider-parity tests pass. His exact
  placement, combat values and the number of other defenders remain open.
  A later [six-Thrax checkpoint replay](evidence/bootcamp-courtyard-six-thrax-checkpoint-replay.json)
  confirms all three camp companions can attack with every staged Thrax
  placement present. Combat began on login at a copied upper-path checkpoint;
  the Shaman killed one Thrax and the recruit killed another before the recruit
  and all four seeded allies died. This does not complete the natural route or
  establish the original encounter's balance.
  The [exact deployed-binary client replay](evidence/bootcamp-live-companion-binary-client-replay.json)
  subsequently rendered the three live companions at camp, reached the
  courtyard sandbags in one ordinary route run, and traced 20/21/20 ally hits
  in a separate one-Thrax copied-world diagnostic. A repeated route landed on
  the lower level, so normal six-Thrax passage and balance remain unverified.
  A [deployed-binary six-Thrax probe](evidence/bootcamp-six-thrax-deployed-binary-probe.json)
  confirmed H then R draws and reloads the copied recruit's empty rifle to
  20/980. Its two repeated camp routes fell below the upper path; after a
  diagnostic teleport to a prior upper checkpoint, one Thrax killed the
  unopposed recruit while all three companions still followed behind and
  had logged no hits. Resolve the filmed defenders' placement and activation,
  and repeat with ordinary player fire before treating encounter balance as
  preserved.
  A [client-ready leftward route trial](evidence/bootcamp-courtyard-client-ready-cave-route.json)
  now reaches cave area 198602 from a copied courtyard checkpoint with all six
  staged Thrax attacking; mission 1994's cave objective completes and the
  original client advances to "Find a way out of the cave." The recruit dies
  afterward. Its camp escorts were about 40 m behind at the trigger because
  this checkpoint skipped natural travel. The cave route is verified for this
  checkpoint, while normal ally formation, survival and encounter fidelity
  remain open.
  A [subsequent upper-route client replay](evidence/bootcamp-courtyard-upper-route-client-replay.json)
  used ordinary movement to reach the upper gap from a rocky-descent checkpoint.
  A separate login at that reached point crossed the gap and courtyard
  obstruction with normal jumps, reaching the filmed courtyard line before
  hostile fire killed the recruit. The cave objective stayed active. A single
  uninterrupted camp-to-cave survival and encounter completion are still open.
  [Timed continuous replays](evidence/bootcamp-courtyard-continuous-route-replay.json)
  now cross both obstacles in one session from the rocky-descent checkpoint;
  the closest no-fire sprint died about 1.1 m outside the cave trigger.
  A [cover-fire checkpoint replay](evidence/bootcamp-courtyard-cover-fire-replay.json)
  then produced two player Thrax kills, 142 XP and 20 credits, and a native-client
  reload from 5/1000 to 20/985. The recruit later died 17.4 m from the cave
  trigger center while trying to cross the courtyard obstacle. These are
  emulator observations with estimated combat data and inspection pauses.
  The [cover mechanics audit](evidence/bootcamp-cover-mechanics-gap.json)
  finds recovered client cover damage values and UI states; before this
  reconstruction, the emulator's distance-based creature attacks had no cover
  calculation. Original
  sandbag static positions and client collision triangles are recovered. A
  diagnostic ray probe crosses those triangles on all three sampled body
  heights at one normal-attack death point. The original server's sampling and
  damage formula remain unknown; earlier runs cannot quantify its effect.
  A bounded [cover reconstruction](evidence/bootcamp-cover-reconstruction.json)
  now uses the original six sandbag collision meshes and client damage lookup
  for direct ranged hits, with nine sample points and interpolation explicitly
  estimated. In an isolated original-client run, 183 incoming hits at one fully
  occluded checkpoint were scaled to a quarter of their pre-cover damage; the
  recruit still died and the cave objective stayed active. The bounded
  reconstruction is deployed in a cover-only derived game image; it does not
  establish final-live balance.
  A follow-up isolated route trial queued input on the server's MapLoaded event,
  while the original client still displayed Entering Battle; its first strafe
  did not move the character, and a later forward move led north to another
  death outside the cave area. The next normal route replay must wait for a
  playable client frame before sending movement.
  The final fight and an uninterrupted camp-to-cave run remain unverified.
  The [courtyard reward timeline](evidence/bootcamp-courtyard-kill-pacing.json)
  records six kills over 36.2 seconds, without establishing simultaneous
  spawns. A [conditional placement fix](evidence/bootcamp-conditioned-creature-respawn-audit.json)
  now prevents zero-respawn allies/defenders from returning after corpse cleanup
  merely because mission state refreshes; two focused state tests pass.
  The filmed post-handoff player and camp allies also contradict the usefulness
  of the old southern DeSimone estimate. The
  [revised placement](evidence/bootcamp-desimone-camp-placement.json) is still
  inferred. A copied original-client checkpoint completed his objective
  conversation there; continuous Hartmann → DeSimone → cave travel remains open.
  A [cave-approach replay](evidence/bootcamp-courtyard-cave-approach-replay.json)
  confirms the current cave trigger advances mission 1994 using ordinary client
  movement when staged attacks are diagnostically suppressed. A no-fire run
  with normal attacks died 3.8 m outside the trigger. Its server trace shows
  all six staged Thrax hitting the single Warrior at map load and killing him
  before the recruit arrived in that replay. The final-week recording shows a
  targetable Warrior during the fight, but cuts directly from camp to courtyard,
  hiding travel and any earlier encounter activity. Enemy activation,
  concurrent count and defender placement need direct evidence before revising
  the encounter.
  A [new copied-client route trace](evidence/bootcamp-camp-to-upper-route-replay.json)
  walked from the staged camp point through the gate to the rocky descent using
  ordinary inputs, with all three companions following. A third pass reached
  `(327.1,121.1,105.8)` on the upper approach, but its jump fell beneath the
  bridge. The original recording cuts over that travel.
  A [separate upper-gap checkpoint replay](evidence/bootcamp-upper-gap-side-jump-replay.json)
  found a native-client D+Space crossing and landed at `(320.4,120.9,93.8)`
  on the original upper navmesh floor. It reached the courtyard at
  `(301.1,119.8,78)` with companions present before the no-fire recruit died
  under normal staged attacks. This resolves the checkpoint geometry route,
  while a single uninterrupted camp-to-courtyard run and normal combat remained
  open at that checkpoint.
  A [continuous camp-to-courtyard replay](evidence/bootcamp-camp-to-courtyard-continuous-replay.json)
  subsequently crossed the same upper gap and reached the courtyard sandbags
  at `(301.2,119.8,77.9)` from the staged camp in one original-client login,
  without movement edits after login. The recruit died without firing after
  inspection pauses. Companion markers appeared behind him on the courtyard
  radar, and none logged outgoing hits in this run; their route across the gap
  remains unverified. The cave trigger and normal fight are still open.
  A subsequent [position-traced continuous replay](evidence/bootcamp-continuous-companion-position-trace.json)
  with a freshly built disposable game binary showed all three companions
  following across the gap and reaching the courtyard sandbags. Each landed
  nine hits before the no-fire recruit died. The earlier copy used a different
  binary and logged no companion hits; the difference is not yet isolated.
  This verifies present follow behavior, while final-live routing, encounter
  pacing and a completed fight still require evidence.
  A [continuous native-client combat trial](evidence/bootcamp-camp-to-courtyard-combat-trial.json)
  started at camp with the issued 20-round rifle load. It reached the sandbags
  with the three companions attacking, but the first Tab selected the friendly
  Archer; four player shots resolved against entity 0 and the recruit died
  before reaching the cave. This is a targeting failure in the trial, not
  evidence for changing Thrax combat values. The old and rebuilt game PDBs
  carry the same BehaviorManager source checksum, while the content runtime
  checksum differs; the earlier zero-hit discrepancy remains unresolved.
  A [current-build combat checkpoint](evidence/bootcamp-current-build-targeting-checkpoint.json)
  then aligned the recovered-client reticle on a Thrax from the previously
  reached `(316.13,120.28,85.93)` point. Seven player rifle hits killed one
  staged Thrax and the client showed 71 XP and 10 credits. The isolated
  harness exited before its next target capture, with Alden still alive;
  encounter clearance and cave entry were not observed.
  A [Warrior sequence audit](evidence/bootcamp-courtyard-warrior-sequence-audit.json)
  compares the final-week footage with four traced emulator runs. Two reward
  events precede a targetable Forean Warrior in the recorded courtyard fight;
  the emulator's only staged Warrior dies before the first Thrax in every
  tested run. The video edit hides map-load timing and whether that Warrior
  was a later arrival, so no unsupported spawn or combat change was made.
  A [sustained-fire checkpoint replay](evidence/bootcamp-courtyard-sustained-fire-checkpoint.json)
  on the current build used the original client to kill two staged Thrax,
  showed two 71 XP/10-credit pairs, and reloaded the crate rifle to 20/984.
  All three companions attacked. A cave push ended in the recruit's death at
  `(296.4,120.5,67.6)`, 7.5 m outside the trigger; four Thrax were still
  alive when he died. This verifies the checkpoint combat path but leaves the
  normal complete fight and cave transition open.
  Three further [normal-attack cave approaches](evidence/bootcamp-courtyard-normal-cave-approach.json)
  ended before area 198602: one before movement began, one at a client-reached
  point 6.8 m outside its radius, and one at that point against a sandbag
  barricade. The trigger itself was not reached or disproven.
  A [courtyard pressure audit](evidence/bootcamp-courtyard-combat-pressure-audit.json)
  compares final-week frames with a traced emulator opening. The original
  recruit's health bar remains visually filled through the six-kill interval,
  with a level-2 Forean Warrior targetable after three rewards. In the copied
  emulator run all six seeded courtyard Thrax hit the only Warrior within four
  milliseconds of one another and kill him 7.1 seconds after map load. This
  narrows the next test to enemy activation and target selection, while the
  original pre-fight edit and unknown defender count still prevent a supported
  spawn-timer or damage adjustment.
  The [2026-09-23 live runtime correction](evidence/live-creature-health-respawn-20260923.json)
  now uses `creature.max_hp` for templates without `creature_stat` instead of
  a hardcoded 100 HP. The live boot-camp Initiate row has 555 HP in the seeded
  data, but that number is an emulator analogue, not a recovered final-live
  value. The same image prevents mission-condition refresh from bypassing a
  placement's zero or timed respawn setting. Focused tests pass 3/3; a live
  recovered-client login reached Character Select, while combat after this
  correction remains untested.
  A [copied-client follow-up](evidence/bootcamp-after-health-client-replay.json)
  reached the playable boot-camp HUD on the deployed-derived binary, moved
  toward the courtyard and fired at a visible level-2 Thrax. Long inspection
  pauses left Alden exposed; a staged Thrax killed him 32.5 m from the cave
  area's center and the client displayed Refugee Base Medic. The replay
  confirms the changed binary admits the instance and encounter; it does not
  measure internal enemy HP or complete the normal fight. The live-derived
  binary lacks some staged companion follow/assist changes, so this replay
  also cannot verify the full ally reconstruction.
  An earlier attack-suppressed [client cave walk](evidence/client-cave-walk-playthrough.json)
  did enter the area and advanced the objective. A fourth normal-attack replay
  from its saved position took a different camera-relative path and died outside
  the area, so the normal encounter remains open.

- **2026-09-22 tracking and movement checkpoint:** deferred owner admission
  restores saved mission tracking; native grouped IDs are accepted; full
  character-option snapshot replacement preserves intentional untracking across
  a fresh original-client reconnect. McAllister's route now stops and retains
  facing without changing its inferred endpoint. The combined suite passes
  1281/1281, including seeded final Hartmann/DeSimone handoff and payout checks.
  Next: perform that handoff in the original client, then continue the current
  boot-camp segment. Rifle modules/melee and officer visual grounding remain
  open. See [tracking](client-mission-tracker-reconnect.md),
  [option saves](client-character-options-snapshot.md), and
  [movement](client-mcallister-grounding.md).

- **2026-09-22 equipment continuation:** the original client now executes
  Delessio's two talks, crate collection, first-boots equip, Hartmann's firing
  instruction, Practice Dummy and Lightning objectives on isolated servers. The
  combined suite passes 1251/1251 after correcting the rifle range to the original
  60 m and repairing unequipped clothing's skill-requirement tuple. Both repairs
  render correctly in the original UI. Rifle melee/modules, mission-tracker
  reconnect and officer grounding remain current-segment work, not grounds to
  advance to Wilderness. See [equipment playthrough](client-gearing-playthrough.md)
  and [selection audit](client-selection-deletion.md).

- **2026-09-22 original-client playthrough:** creation, initial HUD, both Eloh
  approaches, Initiation turn-in and normal logout persistence now execute on
  isolated disposable servers. Saved yaw admission is corrected and verified;
  clothing tooltip and weapon-drawer initialization defects found during this
  run are corrected and verified by a second original-client admission plus the
  1232-test suite. The spawn-heading convention is now calibrated; the measured
  direction and supported skip grants still carry evidence gaps. See [playthrough](client-opening-playthrough.md) and the
  latest [work-log entry](retail-accuracy.md). This does not certify the segment
  complete or establish final-live parity.

- **2026-09-22 next audit, pending deployment/playthrough:** recovered all five
  appearance palettes and the native texel sampler; hybrid Beard/accessory
  admission now follows the original disabled controls. Exact color admission
  still needs runtime scaling and inherited clone-color verification. Crate lid
  state, the observed 20-round issued rifle, partial-loot recovery and
  practice-target damage feedback are corrected. See
  [crate audit](bootcamp-crate-state-audit.md),
  [combat feedback](bootcamp-combat-feedback-audit.md) and
  [palette evidence](evidence/character-creation-palettes.json).
  The complete compatibility client now renders its original login screen in
  isolated Wine after supplying the native DirectX helper. The later isolated playthrough above verifies authentication, creation and
  Initiation; the remaining boot-camp sequence still needs verification. See
  [client acquisition](client-artifacts.md).
- **2026-09-22 follow-up, pending deployment/client verification:** appearance
  admission now follows the original 92-choice slot/race catalog, names follow
  the original format messages with explicitly inferred uniqueness scope, and
  first-admission count/skip entitlement survive interrupted world loading.
  McAllister now actually walks after Gearing Up acceptance; his pace remains a
  labelled analogue. See [appearance](character-creation-appearance-evidence.md),
  [names](character-name-evidence.md), [first login](first-login-skip-research.md)
  and [movement](bootcamp-opening-movement-audit.md). Exact color admission, name
  collation/reservation policy, skip rewards and original-client comparison
  remain incomplete.
- **2026-09-22 correction, pending deployment/client verification:** creation now
  persists the footage-backed Recruit outfit as worn inventory, a loaded pistol
  in drawer slot 1 and 1,000 reserve rounds. Hybrid creation and cloning require
  persistent account unlocks from the identified mission completions. Clone
  packet framing and height validation are corrected. The boot-camp equip lesson
  requires reconstructed crate gear instead of automatically crediting starter
  equipment. See [loadout evidence](evidence/new-character-loadout.json),
  [race eligibility](race-unlock-research.md),
  [request validation](character-creation-validation-evidence.md) and
  [equip-step audit](bootcamp-equip-audit.md). Estimates remain labelled; these
  changes do not certify the creation/boot-camp segment as complete.
- Dated post-rewrite evidence establishes all five Recruit skills at rank 1
  with zero initial unspent points. Creation now persists these ranks together
  with the character and items in one transaction, with failure rollback.
- The starter pistol now receives durability from its own template.
- The first-family six-field creation message now has a handler, with the
  original `None` family state and persisted admission/replay checks.
- Fixed Recruit outfit colors match the original creation preview. Selection
  now checks the original two-field message, rejects overflowing slot values,
  and preserves session selection when ownership lookup or saving fails.
- Initial inventory and race eligibility now have the corrections above; exact
  inferred fields and original-client verification remain incomplete. Appearance,
  naming and first-login/skip audits are recorded below as they are verified.
- Deployment 11 rebuilt boot camp. The archived Bootcamp page marks its own
  mission list obsolete; recover the later sequence before importing missions.
- The recovered later client identifies missions 1990, 1992, 1994, 1995 and
  the retry 2005, 21 objectives and ten NPC dialogue bindings. The original base
  emulator lacked their spawn pools and mission definitions; the reconstructed
  slices below now supply playable content. Original server start/reward/skip
  scripts remain unrecovered. See [the boot-camp audit](bootcamp-client-evidence.md).
- A verified 2026-09-13 sweep established that the original static map holds
  no gameplay actors at all, so first-login position, NPC placements and
  objects must come from captures or observation, not client data.
- The server now speaks the recovered mission-log and NPC objective
  conversation protocol with persistent per-character progress, which the
  tutorial's conversation objectives require. Reconstructed tutorial content is
  seeded by the slices below; incomplete definitions are withheld. See
  [mission research](mission-research.md#mission-log-protocol-and-persistence--2026-09-13).

- Three original recordings, the main one dated by player chat to the final week before shutdown,
  were transcribed, independently verified and matched against the client radar maps. They give
  measured positions for the arrival point, the Initiation trigger areas and the first NPCs and
  objects, and the observed rewards of the first missions. See
  [verified footage](bootcamp-client-evidence.md#2026-09-13-verified-footage-what-three-original-recordings-establish).
- The server has the data layer for labelled reconstruction (S0 of the boot-camp build plan): content
  rows are validated and withheld unless every mechanic they need exists. The new-character entry
  switch originally defaulted to the Wilderness start while S1 was being built;
  the subsequent slice and deployment records below describe the reconstructed camp.

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

## Instancing status

Built on 2026-09-27 because every later segment's operations need it. Account: `docs/retail-accuracy.md`,
2026-09-27 "Instancing".

- **Built:** the 53 loaded contexts the final client types MISSIONCONTEXT are per-squad (`MissionContextSquadInstancing`,
  instancing 2). Each squad, or player outside a squad, gets one copy populated from the whole context. The D10/D13
  invite quirk is kept (OD-125). A copy lingers 600 s once empty (OD-126). Leaving, being kicked or the squad
  disbanding sends the player back with PM 1058, and `SquadMemberList` now carries `partyExclusiveMap`. "Leave current
  adventure" is the way out (OD-129). Numbered copies of shared maps, `ChooseInstanceList`/`SelectInstance`/
  `SelectInstanceCancel` and copy switching in the waypoint window exist, but no capacity is configured (OD-127).
  Wonkavate carries the copy's number (OD-128).
- **Open:** the reset timer (`GAP-INSTANCE-RESET-TIMER`), shared-copy capacity and population thresholds
  (`GAP-SHARED-COPY-CAPACITY`), the quirk's final state, the Last Stand copies (`GAP-LAST-STAND-COPIES`),
  fail-on-leave missions, death expulsion and start groups. The boot camp's instances have no navmesh (recorded,
  unchanged). Owner client check (two squads in one operation, leave and disband inside) is still to do.

## Instance travel status

Built on 2026-09-27 as batch 2 of the instance inventory (`research/20260926-instances`). Account:
`docs/retail-accuracy.md`, 2026-09-27 "Instance travel and death".

- **Built:** doors into and out of Warnet Caverns, Ustor Yard, Sanctus Grotto and The Refuge on the client's entrance
  markers, arriving on each instance's entrance hospital (`InstanceTravelLinks`, map_link 199250-199257, OD-130); the
  Last Stand's two exits to the CELLAR (OD-134); the CELLAR's north end to Edmund Range 2374 per D15.7/D16, with an
  analogue way back (OD-131). 22 instance and battlefield hospitals joined (127 on 55 maps, OD-132); Eloh Temples offers
  only the current section's hospital (D10.5). Test maps 1991/2233/1737 and Edmund Range OLD's spawn pools removed
  (`InstanceTravelRetiredRows`, OD-133).
- **Open:** arrival points unobserved (`GAP-INSTANCE-ARRIVAL-POINTS`), Ustor Yard's west exit, Eloh Vale's dropship
  entry, Edmund Range's real exit and the D16.3 relocation, start portable waypoints, 41 hospital markers
  (`GAP-HOSPITAL-UNRESOLVED-GRAVEYARD`), emulator waypoint ids and Edmund's team gating. Owner client check (enter and
  leave each new door, die in Warnet and the Eloh Temples) is still to do.

## Crater Lake status

Seeded on 2026-09-27 from the wilderness instance dossiers (`research/20260926-instance-dossiers-wilderness`). Account:
`docs/retail-accuracy.md`, 2026-09-27 "Crater Lake Research Facility".

- **Built:** Captain Velns' package 23; 450 The Dead Live (107 -> Velns, 900 credits); Lt. Casper as a per-copy
  placement (spawnpool 520012 retired); Overseer Tyryd; 960 Logos: Movement, Around, Chaos on shrines 50/45/4
  (`WildernessCraterLakeResearchFacility`, creature 1721001, placements 1721100-1721101; OD-140 to OD-144). Every
  position is on navmesh floor with a complete path from the instance entrance.
- **Open:** 1055 (the HQ upper floors and the greenhouse are navmesh islands, `GAP-CLRF-INTERIOR-NAVMESH`, with the
  Pravus tooling batch; the Observation Center reading is contradicted; the terminals need a body), 1056/711/1059 and
  so 1065, the radar dish (TwoStateSwitch usables), 489, 1054, the ambient population, the D11 bosses and crates, 450's
  experience and post-1.4 amounts. Owner client check (enter a copy, speak to Velns for 450, draw the three Logos,
  kill Casper and Tyryd) is still to do.

## Pravus Research status

Seeded on 2026-09-27 from the wilderness instance dossiers (`research/20260926-instance-dossiers-wilderness`). Account:
`docs/retail-accuracy.md`, 2026-09-27 "Pravus Research Facility".

- **Navmesh:** the entrance ramp is joined to the interior (terrain cut inside the Bane industrial modules,
  `src/Rasa.NavMesh/data/terrain_cuts.csv`); closes `GAP-PRAVUS-INTERIOR-NAVMESH`. Other maps with terrain inside
  buildings are listed, not rebuilt (`GAP-NAVMESH-TERRAIN-CUTS`).
- **Built:** the population (creatures 1430001-1430010, pools 1430200-1430204), the entrance keypass Trainees, Overseer
  Tarmok, the capsule, the Prototype production, the six infestations, Nylla's gangplank stand, Johnson beside Perkins,
  the packages of Johnson, Nylla and Parsons, and 593, 575 (objectives 1-3), 323 and 924 (`PravusResearchInstance`).
  Decisions: OD-135 creature and object statistics as world analogues; OD-136 region-level pools with the observed
  minimum counts and the world respawn; OD-137 the production as one placement returning 2.5 s after each death; OD-138
  Nylla's gangplank stand present from entry; OD-139 593 offered without its 574 prerequisite. All agent-approved,
  pending owner review.
- **Open:** 574 (Baruhi and the Wilderness Machina), 575/4-5, the capsule stopping the production, the force field,
  the Frontlines allies, Prion, radar registration of the interior groups, squad credit for destroyed infestations and
  the keypass, post-1.6 rewards. The continuations hGS00lxVgQg and RrHLjmYK-YI would settle most of these. Owner client
  check (enter a copy, take 593 and 575, collect the keypass, reach the chamber, destroy the capsule, clear a dish) is
  still to do.

## Divide operations status

Seeded on 2026-09-27 from the Divide instance dossier (`research/20260926-instance-dossiers-divide`): Minos Caverns 1347,
Timora Mines 1348, Torcastra Prison 1349. Account: `docs/retail-accuracy.md`, 2026-09-27 "The Divide operations".

- **Navmesh:** Minos Caverns is one mesh at a 0.2 x 0.1 m grid (`src/Rasa.NavMesh/data/map_build_settings.csv`; its
  cavern tiles' joints were narrower than the default grid resolves), Timora Mines' Fuel Egress block is joined by a
  terrain cut scoped to its one chunnel entrance (`terrain_cuts.csv`, `mesh_prefix@x:z`). Torcastra was connected. Kept
  as original geometry: the Logos Those pit (a drop), Minos' capped Bane control room, the tower console's jamb.
- **Built:** Kearney, Pastre and Hamilton as placements at their sources' positions (pools 510118-510120 at 0/0),
  Hamilton's own package 1526 and his stasis tube; Morrow, Sanchez, the Wardmaster, two Overseers and the Warden,
  Scout Horlo, Lt. Cisco at the entrance, Overseer Torqua and Ranger Ferme (`DivideOperationsInstances`); Tyler walks 392.
  Missions 340, 1905 (objective 1 optional and unrevealed), 792 and 392. Decisions: OD-145 creature analogues and bodies;
  OD-146 1905 without its escort party; OD-147 392 without 383; OD-148 the Minos grid; OD-149 the scoped Timora cut;
  OD-150 the tube's hit points; OD-151 1860/1861 held; OD-152 the 403 drills unplaced. All agent-approved, pending owner
  review.
- **Open:** 403, 404, 1276, 384, 391, 397, 594, 356, 1860, 1861 and 383; the escort party of 1905; the containers of
  1860; the gatekeeper, gate, timer and garrisons of 594; the computer bank of 356; Ferme's guards and cell; the ambient
  populations; the new NPCs' real gear. Owner client check (take 340 from Simpson, report to Kearney, walk 1905 to
  Sanchez, kill the three Timora targets for Pastre, escort Tyler to the medic) is still to do.

## Palisades dossier status

Seeded on 2026-09-27 from the Palisades mission dossier (`research/20260927-palisades-dossiers`), Concordia Palisades
1244. Account: `docs/retail-accuracy.md`, 2026-09-27 "Concordia Palisades: the dossier's finishable missions".

- **Built:** 1812 Logos: True and 1813 Logos: Through (Arizpe, shrines 333 and 322); 1988 A Spiritual Pilgrimage
  (Brocail, Knowledge/Man/Planet, post-1.4 20,000 XP, 3,000 credits and the AccuMax choice 122719-122721); 2014 Crash
  Course (Matlin; Derac's package 136 and Matlin's 1214 bound; Executor Gantic's Datapad; objectives revealed 1 -> 2 -> 6;
  armor choice 130322-130324, no amounts); 1795 Bloody Booty (Mullen; Barbrix's Boot). Package 134 moved to the Palisades
  Corporal Orton and the Valverde Pools duplicate's pool retired; Gantic and Barbrix return 60 s after a kill; teleporter
  624 carries the client's "Waypoint: Viands Village" (`PalisadesDossierMissions`). Decisions: OD-153 to OD-159 are the
  dossier's OD-P1 to OD-P7 with its defaults; OD-160 the named-boss respawn. All agent-approved, pending owner review.
- **Open:** 1799-1801 (Skive Base is empty), 337 (Kaven Corman in the ruins), the 366 <- 1988 prerequisite (left for the
  owner, `GAP-PALISADES-366-PREREQ-1988`), 368's skipped kill and timer, every Palisades kill or creature-drop mission (no
  ambient population), the instance-bound missions (operations batch), and the post-D12 positions of Arizpe, Kaven,
  Briggs, Hutchison and Aldrin. Owner client check (take 1812/1813 from Arizpe, draw True and Through; 1988 from Brocail,
  including the Man shrine in the Horsetail Falls gorge; 2014 from Matlin via Derac and Gantic; 1795 from Mullen via
  Barbrix) is still to do.

## Concordia ambient populations status

Seeded on 2026-09-27 under the owner's OD-161 (footage first, then text, then labelled stand-ins): the overworld Divide
1148 and Concordia Palisades 1244. Account: `docs/retail-accuracy.md`, 2026-09-27 "The Divide and Concordia Palisades:
ambient populations"; evidence `docs/evidence/concordia-ambient-populations-20260927.json`.

- **Built:** 12 creature rows (1148001-1148007, 1244001-1244005) and 22 pools (1148200-1148213, 1244200-1244207):
  Divide Xanx (cave, nests, TaRapedia's 371 spot), Thrax Privates/PFCs with Caretakers (Front Lines by dropship, Bane
  Forward Command, Hydro Plant), Filchers, Warnets at two nests, two Class IV Stalkers; Palisades Boargar at the Eloh
  obelisk, Warnets east of Hightower, Fithik in Uherum Pass and Fithik Trench, Hunters at Skive Base, Thrax Technicians
  in Clearcut Field; area 1244500; Mayes' package 172. Missions 358, 371, 372, 774, 755, 342 and 1808 defined; 368 counts
  its five Warnets (`ConcordiaAmbientPopulations`). Decisions OD-161 (owner) and OD-162 to OD-170 (agent, pending owner
  review).
- **Open:** creature levels, health and attacks (analogues), most pool sizes and the respawn, the Filcher and Hightower
  spots, Divide Hominis Machina and Amoeboids (so 1582 stays held), Predators, 368's timer, 1808's Start/Middle/End, the
  Uherum navmesh island, Palisades species without a place (Tree Lurkers, Howlers, Lightbenders, the Lake Elinor nests,
  the Bane squads of 363 and 355) and every mission whose giver is missing. Downloads that would tighten it are listed
  in `GAP-CONCORDIA-FOOTAGE-DOWNLOADS`. Owner client check (358 at Sebastian, 371 -> 372 via Kerr and Mayes, 774 -> 755
  via Kibner and Sherman, 342 at Yorma Brown, 1808 at Tayros, 368 at Kogari) is still to do.

## S4 (Capture the Flag) status

- Mission 1994 is seeded by `BootcampS4CaptureTheFlag` (SQLite and MySQL, frozen rows in
  `BootcampData/BootcampS4CaptureTheFlagRows.cs`, parity-checked; 41 manifest rows). DeSimone gives it after 1992
  is completed; objective 4 (promotion conversation) is revealed on acceptance and a rule grants its 500 XP;
  4 → 2 → 1 → 3 follows the footage. Objective 2 is an area trigger at the measured cave-in icon (10 m
  reconstructed radius under OD-44; the initial 5 m estimate was superseded); objective 1 is a kill of the Tizzik Gi placement with the "Boss Eliminated 0 / 1" counter; objective 3
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
- No mechanism changes in the 2026-09-22 to 2026-09-24 recovered window; the content-level change to this
  segment's seeded rows (reinforcement walk-off removal) is recorded under "S5 (Calling for Reinforcements)
  seed status" below.

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
- No mechanism changes in the 2026-09-22 to 2026-09-24 recovered window.

## S5 (Calling for Reinforcements) seed status

- A 2026-09-22 isolated client continuation accepted 1995 from Youngblood after
  1994, displayed objective 2 without a countdown, completed the wounded
  soldier conversation and revealed Conrad's corpse objective 3. The copied
  database retains that state. A follow-up isolated client comparison found
  the inferred corpse seed inside ramp geometry with an incomplete navmesh
  route. `BootcampConradCorpsePlacement` moves it to inferred reachable floor
  at (-98,85.39,67.2); a recovered-client right click completed objective 3
  and began the 600-second objective-1 timer. Hostile attacks were disabled
  only in the disposable world, and GM teleports bypassed that early route.
  A later copied-client run reached the corpse through ordinary movement,
  displayed its use prompt, and completed the objective with a right click;
  the copied character database persisted the completion and 600-second timer.
  At that checkpoint, the uninterrupted soldier-to-corpse sequence and full
  timed bomb route were unverified. A later checkpointed high-bank route reached the wreck through
  ordinary movement. Its first far-face clicks found no usable; a short move
  to the near face exposed the prompt, and right-click completed the bomb
  objective and revealed Van Valkenberg. Ordinary movement from the bomb then
  opened his check-in dialogue; Continue on the reached pad transferred to
  Alia Das, set the skip flag and displayed Training Day. Timer resets and
  coordinate restores make this route reachability evidence, not a full timed
  run. The first
  bomb client checkpoint found the measured radar point obstructed by the
  reconstructed wreck; a closer playable position on its near face
  `(-221.95,102.3,-70.5)` is inferred within the original per-axis ±2 m
  measurement bounds. `BootcampBombHullPlacement` applies it. In the recovered
  client the bomb use and detonation completed objective 1, removed the wreck,
  spawned reinforcements and revealed objective 4. A later recovered-client
  check opened Van Valkenberg's completion dialogue, transferred from the pad
  to Alia Das, offered Training Day, and completed 1995 with Rogers. Rogers'
  generic screen displayed `ERROR: ? No greeting`, and exact 1995 rewards
  remain unknown. A diagnostic timer-expiry check recorded 1995 failed in the
  copied database; Youngblood offered retry 2005 with a fresh 600-second timer.
  The recovered client used the corrected bomb in the retry, completed its
  objective 1 and revealed Van Valkenberg. This verifies the emulator path
  under diagnostic timer and position edits, not original final-live values.
  Evidence:
  [mission checkpoint](client-reinforcements-opening.md),
  [corpse placement](evidence/client-conrad-corpse-placement.json),
  [ordinary client corpse use](evidence/bootcamp-s5-corpse-use-20260924.json),
  [navmesh route candidate](evidence/bootcamp-s5-wreck-route-candidate-20260924.json),
  [timed client route attempt](evidence/bootcamp-s5-timed-wreck-route-20260924.json),
  [higher-bank route](evidence/bootcamp-s5-higher-bank-wreck-approach-20260924.json),
  [bomb acquisition from that route](evidence/bootcamp-s5-bomb-acquisition-from-route-20260924.json),
  [ordinary check-in and exit](evidence/bootcamp-s5-postbomb-ordinary-exit-20260924.json),
  [ordinary Alia Das route toward Rogers](evidence/bootcamp-s6-rogers-ordinary-route-20260924.json),
  [bomb placement comparison](evidence/client-reinforcements-bomb-correction.json),
  [Alia Das handoff](evidence/client-reinforcements-handoff.json), and
  [failure/retry](evidence/client-reinforcements-retry.json).
- Three later isolated single-session attempts used Conrad's corpse normally and
  let the 600-second timer run without resets. None reached bomb use: the first
  lost movement to chat focus near the wreck, the second met bridge rubble, and
  the focus-guarded third followed a west/north bridge line away from the wreck.
  The third confirms that focus recovery works but does not establish a timer
  or mission-mechanics defect. The measured dry-bank route and its hazards are
  in [the route analysis](evidence/bootcamp-s5-dry-route-analysis-20260924.md);
  [the guarded timed run](evidence/bootcamp-s5-focus-guard-and-timed-repeat-20260924.md)
  records the latest outcome. A separate
  [detonation aftermath audit](evidence/bootcamp-s5-detonation-aftermath-audit-20260924.json)
  confirms the current 21-damage, wreck, objective and reinforcement order
  against surviving footage, while player recoil and exact beam-in timing remain
  evidence gaps. The [hidden-XP audit](evidence/bootcamp-s5-hidden-xp-audit-20260924.json)
  confirms a level-3-to-4 jump inside the original edit but cannot distinguish
  a mission reward from unseen kills or locate its trigger; no provisional
  grant is seeded. The [tier-gate follow-up](evidence/s5-s6-tier-gate-xp-followup-20260924.json)
  notes that the first Alia Das frame also records a full XP bar and class
  gate message; the earlier rough 10–11k estimate reaches only emulator level
  4, not trainer eligibility. The [source-exclusion audit](evidence/s5-s6-xp-source-exclusion-20260924.json)
  places the gain before ordinary 1995 and 1526 turn-ins but leaves its event
  unknown. An uninterrupted S5 completion was still unverified at this
  stage; V14 later verifies corpse-to-bomb through the Alia Das transfer.
- A later [coordinate-guided client route](evidence/bootcamp-s5-loc-waypoint-route-20260924.md)
  used the original client's `/loc` overlay to reach the inferred bomb-use
  viewpoint by ordinary movement from the corpse checkpoint. A separate copied
  diagnostic state showed the bomb prompt and completed objective 1 there.
  Timed V2 stopped 8.7 m short under a conservative harness cutoff; V3 left
  the dry bridge deck; V4 crossed the bridge but stopped when a resumed
  controller used a stale coordinate. None proves one-session timed completion
  or a mission-timer defect. The focus guard, bridge-height guard and safe
  controller resumption now have five focused harness tests; the next bounded
  check is one unreset client run using those corrections.
- Two further copied-client timer runs remain inconclusive. V5 crossed the dry
  bridge and reached `(-210.1,97.3,-41.6)` with 3:51 showing, but dark-hillside
  `/loc` OCR stopped the harness before bomb use. A bounded OCR fallback now
  reads that frame and keeps unreadable frames paused for review; the seven
  focused harness tests pass. V6 was interrupted by an unexplained SIGTERM to
  the outer isolated runner shortly after corpse use, while objective 1 was
  still active. Neither run shows a mission or timer failure. The exact
  artifacts are [V5](evidence/bootcamp-s5-timed-loc-v5-20260924.json) and
  [V6](evidence/bootcamp-s5-timed-loc-v6-20260924.json); runner-lifecycle
  reliability was the next prerequisite for another uninterrupted check.
- A detached runner held the isolated client responsive during a separate
  lifecycle check. Timed V7 then reached the wreck hull at
  `(-225.0,101.1,-63.7)` with 1:12 remaining, but the bomb prompt was absent;
  local movement probes did not recover it before the real 600 s timer expired.
  An untimed probe from that exact position reached the bomb-use prompt at
  `(-219.08594,101.05078,-69.39844)` using S700, A1000, then W+A400. Timed
  V8 stopped while objective 1 was still active: a 1100 ms diagonal hold from
  `(-142.4,84.8,29.8)` dropped below the dry bridge deck. A further untimed
  probe crossed from that same safe position to `(-145.4,84.8,22.3)` in five
  holds of at most 400 ms, all measured at Y=84.8. These runs establish route
  controls and harness failure modes, not a timed corpse-to-bomb completion.
  See [V7](evidence/bootcamp-s5-timed-loc-v7-20260924.json),
  [hull recovery](evidence/bootcamp-s5-hull-untimed-recovery-20260924.json),
  [V8](evidence/bootcamp-s5-timed-loc-v8-20260924.json), and
  [bridge recovery](evidence/bootcamp-s5-bridge-untimed-recovery-20260924.json).
- Timed V9 used the short bridge sequence and remained on the dry deck at
  `(-144.3,84.8,23.7)` with objective 1 active. The controller stopped because
  this position was 2.34 m ahead of one untimed sample, beyond a fixed 2 m
  sample tolerance; it was about 1.8 m from the next bridge target. V9 did
  not reach the bomb or demonstrate a timer failure. Its
  [sealed trace](evidence/bootcamp-s5-timed-loc-v9-20260924.json) separates
  this controller false divergence from the V8 bridge fall.
- Timed V10 reached a different safe bridge approach, `(-141.8,84.8,28.3)`,
  then a generic 350 ms diagonal hold ended at `(-144.7,84.0,26.3)` below the
  Y≥84.5 deck guard. The controller stopped with objective 1 active and no
  bomb use or timer expiry. This [trace](evidence/bootcamp-s5-timed-loc-v10-20260924.json)
  shows the measured V8-start crossing does not cover every approach position.
  A separate [untimed V10-start probe](evidence/bootcamp-s5-bridge-untimed-recovery-v2-20260924.json)
  crossed from `(-141.8,84.8,28.3)` to `(-147.3,84.8,20.7)` using A300,
  A300, W350, W300, W250; every `/loc` reading remained at Y=84.8. This is
  local route evidence, not a continuous timed completion.
- Timed V11 reached `(-147.9,84.8,23.4)` on the far bridge edge with 5:54
  showing, but an unnecessary W400 moved to `(-151.9,79.4,23.2)` below the
  deck; objective 1 was still active. An untimed copied-client probe from that
  edge fell immediately on W+A200, so its momentary Y=84.8 reading was not a
  safe continuation point. From the exact prior safe position
  `(-143.1,84.8,25.7)`, a separate untimed probe used W+A200 twice to reach
  `(-145.5,84.8,21.4)` on dry deck. These findings are in
  [V11](evidence/bootcamp-s5-timed-loc-v11-20260924.json),
  [edge probe](evidence/bootcamp-s5-bridge-postwa-edge-20260924.json), and
  [short-step probe](evidence/bootcamp-s5-bridge-prewa-short-probe-20260924.json).
  The short-step sequence had not yet been exercised by an unreset timed run.
- Timed V12 crossed the dry bridge and reached the bomb viewpoint at
  `(-219.6,101.1,-69.8)` with 1:30 on the client timer. The original client
  visibly displayed the bomb-use prompt, but the controller's OCR crop missed
  its left edge and withheld the click; objective 1 was still active. This
  [sealed V12 trace](evidence/bootcamp-s5-timed-loc-v12-20260924.json) proves
  timed route reachability to the prompt, not bomb use or mission completion.
- Timed V13 used the corrected prompt detector but incurred three manual
  `/loc` OCR reviews. At +589.6 s it was still approaching the upper wreck at
  `(-214.8,99.8,-56.4)`; the next review frame showed the original client's
  Objective Failed banner, and the copied database recorded objective 1 failed.
  The game log contains a corpse use request but no bomb use request. This
  [V13 trace](evidence/bootcamp-s5-timed-loc-v13-20260924.json) is a real
  expiry during a slow client-harness route, not evidence that the mission
  timer or bomb interaction is broken.
- Timed V14 used the original 1.16.5.0 client in one unreset session from
  Conrad's corpse through the wreck. With 0:22 on the client timer, the bomb
  prompt was visible and an ordinary right-click completed objective 1 before
  expiry. The client showed the detonation aftermath and Van Valkenberg's
  objective-completion dialogue; Continue completed all four 1995 objectives
  and transferred the same session to Alia Das, where Calling for
  Reinforcements appeared complete and Training Day was offered. The copied
  database and two use requests (corpse and bomb) corroborate the client
  captures. See the [V14 evidence](evidence/bootcamp-s5-timed-loc-v14-20260924.json).
  This verifies the reconstructed timed S5 interaction path; original encounter
  combat, exact final-live route and rewards, and Rogers' turn-in still require
  independent fidelity checks.
- Missions 1995 and 2005 are seeded by `BootcampS5Reinforcements` (SQLite and MySQL, frozen rows in
  `BootcampData/BootcampS5ReinforcementsRows.cs`, parity-checked; 59 manifest rows). Youngblood gives 1995 after 1994
  is completed. Objective 2 (the unnamed wounded soldier, package 2584) is revealed on acceptance, then 2 → 3 (use
  Conrad's corpse) → 1 (the bomb reaching its detonated state 115) → 4 (Van Valkenberg, package 2564); 1 → 4 is
  observed (B1-050/051), the rest inferred. Objective 1 carries the 600 s timer (analogue, OD-6) that fails the
  mission; the receiver was initially Rogers (creature 100, inferred; superseded by the reserved Rogers below). No reward is seeded (`GAP-S5-MISSION-REWARDS`): the [bounded source audit](evidence/bootcamp-s5-reward-source-audit-20260924.json) found no final-live payout in the inspected original client, footage or contemporary records.
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
  S5P-01) at inferred pad positions. Van Valkenberg and the reinforcements are present once the dropship is destroyed.
  The corrected final-week frame ledger observes the group standing on the pad through 98.333 s, at least 11.533 s
  after objective 1995/1 completes. The earlier immediate walk rules for 1995 and inferred retry 2005 contradicted
  that observation and are removed by `BootcampReinforcementPadHold`; departure after the hard cut remains unknown
  ([pad-hold correction](evidence/bootcamp-s5-reinforcement-pad-hold-20260924.json)).
  The isolated correction was deployed with the DIT overlay on 2026-09-24;
  the live world DB confirms both immediate walk rules absent and the three
  reinforcement placements retained.
  Their [beam-in timing](evidence/bootcamp-s5-reinforcement-beam-timing-20260924.json)
  remains a separate gap: the emulator makes solid NPCs present immediately,
  while the frame ledger first shows a Forean nameplate about 4.9 seconds after
  objective completion and then a gradual visual arrival. The
  [original-client beam binding audit](evidence/bootcamp-s5-reinforcement-beam-client-binding-20260924.json)
  identifies two class state machines and their beam assets, but neither has
  a static boot-camp placement or a verified S5 server binding.
  A [reconnect-safe delay architecture](evidence/bootcamp-s5-delayed-presence-architecture-20260924.json)
  is recorded without a gameplay change because nameplate timing does not
  establish original spawn or targetability timing.
  One level-1 Thrax Infantry Initiate (the S4 template's analogues, `creature_action` 33) guards the
  measured outpost engagement position. Indicators 435 (1995,2) and 432 (1995,1) use inferred ids at measured positions.
- Conflicts with the build plan recorded in the manifest: C2-16 is a different, static wreck 250 m from the pad; the
  pad-centre radar icon is the (1995,4) indicator, not Van Valkenberg; the wounded soldier's position is inferred,
  not measured; the bomb lies south-west of the player (heading 230 deg), not north; the fuse is 4930 ms state to
  state, not 5300 ms; B2-012 names no reinforcement level.
- 14 positions, 49 footage events (segments B2, B3, C1, C2 added) and sources `official_notes:d11` and
  `client_table:launch-2007-11` (pre-D11, comparison only) were added. Gaps identified at seed time included
  `GAP-S5-WRECK-MECHANISM` (closed by `fbd1539`), `GAP-S5-HIDDEN-XP`, `GAP-S5-WOUNDED-NAME`, `GAP-S5-CORPSE`,
  `GAP-S5-REINFORCEMENT-MOVE`,
  `GAP-S5-REINFORCEMENT-COUNT`, `GAP-S5-TIMER-START`, `GAP-S5-DETONATION-DAMAGE`, `GAP-S5-MISSION-REWARDS`,
  `GAP-S5-INDICATOR-PRESENTATION`, `GAP-S5-AMBIENT` and `GAP-BEAM-IN`; `GAP-BOMB-ITEM`, `GAP-D13.4`, `GAP-NPC-BODY`,
  `GAP-S4-AMBIENT-LEVELS`, `GAP-ROGERS` and `GAP-NEXT-SEGMENT` were updated. Since that seed, the observed 21-point
  detonation self-damage was added by `BootcampDetonationDamage`. A north-east reinforcement walk was then added for
  1995 by `BootcampScriptedMoves` and inferred for retry 2005 by `BootcampRetryReinforcementWalk`. The corrected
  frame ledger shows the group still standing on the pad through the last pre-cut frame, so
  `BootcampReinforcementPadHold` removes both immediate walk rules
  ([aftermath audit](evidence/bootcamp-s5-detonation-aftermath-audit-20260924.json),
  [pad-hold correction](evidence/bootcamp-s5-reinforcement-pad-hold-20260924.json)). Their later departure and beam-in
  presentation remain unverified, as does the player's blast knockback. Still unseeded: mission rewards, the hidden level-3-to-4
  experience, a bomb inventory item, unnamed reinforcements, the other outpost creatures and flyovers.

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
  image. A [checkpointed original-client S5 probe](evidence/bootcamp-s5-soldier-approach-20260924.json)
  reached the wounded soldier through ordinary movement, opened the objective-completion dialogue, and persisted
  objective 2 complete and objective 3 active after Continue. A later [checkpointed original-client corpse use](evidence/bootcamp-s5-corpse-use-20260924.json)
  completed objective 3 and started the 600-second timer after ordinary movement reached the prompt. The complete
  Youngblood-to-soldier-to-corpse route in one run, bomb, check-in and exit still require client verification. A checkpointed ordinary-movement attempt toward the wreck dropped into water near the bridge; 1995 expired before wreck interaction. This does not establish a wreck defect. The remaining owner client checks
  (build plan S5 steps 1–8, S6 steps 1–5) are not all complete.
- [Accepted-1995 continuous-prefix probes](evidence/bootcamp-s5-accepted-prefix-normal-combat-20260924.json)
  used normal hostile behavior and ordinary client movement from a copied Youngblood-area checkpoint. One run killed
  gate placement 198674, continued along the outer fence to (-94.5,85.2,67.5) near the inferred soldier bunker,
  then died to outpost placement 198683 before dialogue. A second run died at the gate after missing the moving
  hostile. A [third visual-acquisition attempt](evidence/bootcamp-s5-gate-visual-acquisition-20260924.json) died
  at the gate before firing while the hostile closed to melee. All isolated character databases were restored.
  These are emulator route/combat checks, not final-live balance evidence. A single unbroken Youngblood acceptance
  through wounded soldier and Conrad's corpse remains unverified.
- A [fifth prefix attempt](evidence/bootcamp-s5-accepted-prefix-fifth-route-divergence-20260924.json) reproduced
  the gate kill, but identical post-gate movement controls reached (-8.3,97.9,124.3) at the fence instead of the
  prior (12.3,106.5,124.6). Replaying the lateral input from this different starting point entered an original
  structure, and a bright lamp produced a false UI-mode reading in the research harness. The character stayed alive
  with objective 2 active. The [focus guard](../tools/client_focus_guard.py) now requires the white UI MODE label,
  verified on two surviving gameplay and two actual UI captures. The next route needs a /loc-driven fence branch;
  no enemy-stat or content correction follows from this navigation result.
- After the local research tree disappeared, the 1.16.5.0 compatibility ZIP and isolated harness were restored.
  A [rebuilt-harness smoke and S5 gate record](evidence/bootcamp-s5-rebuilt-adaptive-gate-and-target-lock-20260924.json)
  confirms copied Auth/Game login, playable HUD and `/loc`. The next bounded accepted-1995 attempt stopped at the gate:
  after an ordinary 5-second walk and two rifle holds the moving hostile remained alive, with the recruit at
  (70.4,109.0,139.5), 8695 XP and objective 2 active. No fence/soldier/corpse step was reached. Original client
  bytecode maps Tab to locking the current reticle target, which can stabilize a target already acquired but does
  not select a nearest hostile. Further continuous verification needs visual acquisition plus /loc-based routing;
  these client-input results do not justify changing enemy statistics or the mission data.
- [Subsequent bounded gate and route captures](evidence/bootcamp-s5-gate-nameplate-crop-20260924.json)
  identified two research-controller errors: its initial OCR crop cut off a visible target nameplate, and repeated
  `/loc` commands toggled the client's persistent coordinate overlay off. With direct gate input, the original
  client acquired and Tab-locked the moving Thrax, killed it with ordinary rifle fire (+71 XP, +10 credits), and
  reached the visual fence waypoint. The controller then toggled off `/loc` and stopped before the route branch.
  A corrected one-toggle run kept coordinates visible but fixed serial aim corrections failed to acquire the
  moving melee target. The uninterrupted accepted-1995 gate-to-soldier-to-corpse prefix is still unverified;
  no enemy or mission data change follows from these input limits.
- [Original-client W3 ranged probes](evidence/bootcamp-s5-w3-gate-range-probes-20260924.json)
  found the target panel at range and confirmed the recovered client's
  [auto-fire acquisition path](evidence/bootcamp-s5-original-client-autofire-targeting-20260924.md).
  Holding ordinary fire while sweeping short camera turns killed the gate
  Thrax from a three-second approach (+71 XP, +10 credits) and reached the
  original fence at (0.8,99.5,128.7) in one session. The route stopped at a
  strict test-controller branch guard before the soldier. A separate
  [outpost comparison](evidence/bootcamp-s5-outpost-engagement-boundary-20260924.json)
  shows the original filmed recruit was level 3 with a shotgun after an edit,
  so the level-2 rifle route death cannot calibrate final-live combat values.
- A [continuous gate-to-outpost run](evidence/bootcamp-s5-gate-to-outpost-corridor-20260924.json)
  subsequently killed the gate Thrax, passed the original-map fence by a
  measured 6.85 m forward probe, and reached the outpost corridor. The
  original client visibly displayed (-61.8,87.9,75.3), but an OCR error
  stopped the controller before the next fight. The accepted-1995 route to
  the soldier and corpse, and same-session Youngblood acceptance, remain
  unverified. The isolated SQLite reset now removes stale WAL sidecars before
  restoring its baseline; no live DB was changed.
- A further [outpost short probe](evidence/bootcamp-s5-outpost-short-probe-20260924.json)
  reached (-87.8,85.9,75.4) in the same accepted-1995 client session after a
  normal gate kill. The recruit remained alive under outpost fire and objective
  2 stayed pending; the bounded probe stopped before the wounded soldier.
  Ambiguous negative-X OCR is retained as untrusted. The next check must pass
  the original sandbags and complete objective 2 without a checkpoint reset.
- A [continuous accepted-1995 soldier run](evidence/bootcamp-s5-continuous-soldier-dialogue-boundary-20260924.json)
  later killed the gate Thrax, crossed the sandbags by ordinary movement,
  displayed the Human talk prompt and opened the Conrad/EMP Objective
  Completion dialogue in one client session. Its Continue button was missed
  by the controller, leaving objective 2 pending. Follow-up attempts exposed
  variable north-branch starting positions and camera target offsets, not a
  confirmed mission defect. The next bounded check needs accepted-Move-guided
  short strafes, the visible talk prompt, a verified Continue click and a
  copied-DB objective 2 → 3 check before attempting same-session corpse use.
  One such attempt stopped at a short north strafe that advanced only 0.24 m;
  the earlier successful run moved through almost the same waypoint, so its
  cause remains open pending a bounded repeat probe.
  A [staged yaw diagnostic](evidence/bootcamp-s5-sandbag-yaw-staged-diagnostic-20260924.json)
  later crossed that area with an inferred northward heading, but a separate
  inferred-heading retry received SIGTERM before input. The diagnostic also
  exposed a delayed-Move sampling error in the private controller. Its staged
  character position and rotation do not verify a continuous route, and the
  earlier post-input no-movement row still needs explanation.
  Four [post-hold timing retries](evidence/bootcamp-s5-post-hold-timing-retries-20260924.json)
  validated the corrected private accepted-Move reader on a real north-band
  client trace, but stopped at measured route guards before the soldier. The
  variable corridor start requires feedback-guided short holds; objective 2
  to 3 and same-session corpse use are still unverified.
  Five [later accepted-1995 retries](evidence/bootcamp-s5-soldier-approach-retries-20260924.json)
  reached one visible Human talk prompt before an outpost death and otherwise
  stopped at route guards. The private diagnostic now uses separate north and
  west fence checkpoints, post-hold telemetry at every movement checkpoint,
  and shorter north strafes from already northward starts. Its latest version
  still needs a complete client run through Continue and corpse use.
  Six [corridor fast-probe retries](evidence/bootcamp-s5-corridor-fastprobe-retries-20260924.json)
  subsequently reached new measured route points but stopped at private guards
  or copied-client death before soldier Continue. The copied DB was restored
  after each. There is still no same-session objective 2 → 3 and corpse-use
  confirmation, and these trials justify no gameplay data change.
  Five [later route variations](evidence/bootcamp-s5-controller-continuation-20260924.json)
  ranged from an unreadable initial `/loc` to a far-east fence waypoint after
  ordinary gate combat. They stopped at private controller bounds, with no new
  mission transition. A position-feedback route is needed before another full
  accepted-1995 attempt can distinguish steering failure from gameplay failure.
  A [bounded feedback-route plan](evidence/bootcamp-s5-feedback-route-plan-20260924.json)
  now specifies short accepted-Move probes from the observed x=3 and x=26 fence
  branches, with explicit stop bounds. The x=26 normalization remains untested.
  Its [first copied-client trial](evidence/bootcamp-s5-feedback-fence-collision-20260924.json)
  normalized the gate position but stopped against a visible fence wall at
  X 11.01 on fresh accepted Move packets. A guarded one-time north-side bypass
  was prepared; the soldier and same-session corpse transitions remain unverified.
  [One guarded wall-bypass trial](evidence/bootcamp-s5-feedback-wall-bypass-20260924.json)
  then cleared the obstruction and reached accepted `(-16.72,97.67,125.46)`;
  its private 55-second deadline expired before the soldier. Objective 2 stayed
  active, so soldier Continue and same-session corpse use still need verification.
  A [75-second retry](evidence/bootcamp-s5-feedback-75s-fence-variation-20260924.json)
  exposed a different fence branch and stopped on a private steering guard;
  objective 2 remained active.
  A guarded follow-up on that upper X12 branch stopped at Z 127.48 before its
  west probe; the same evidence record contains its archive and confirms no
  objective transition.
  The [upper-fence geometry audit](evidence/bootcamp-s5-upper-fence-route-geometry-20260924.json)
  identifies a separate original-navmesh route toward higher Z before the
  X12 wall; the exact collider and continuous client traversal remain open.
  An [uninterrupted lower-branch run](evidence/bootcamp-s5-soldier-converse-request-no-dialogue-20260924.json)
  reached the soldier talk prompt and sent `RequestNPCConverse` to Game, but
  received no visible dialogue; objective 2 stayed active. The conversation
  [comparison with prior success](evidence/bootcamp-s5-soldier-converse-no-dialogue-20260924.json)
  leaves the requested entity and outgoing reply unobserved, so no gameplay
  binding change is justified before a bounded retarget check.
  The [client conversation path](evidence/bootcamp-s5-converse-client-protocol-20260924.json)
  confirms current-usable targeting and a no-dialogue fallback for empty
  replies; the failed run lacks the entity ID and reply needed to choose a cause.
  That retarget retry stopped at another X12 fence entry before the soldier;
  its archive is attached to the request record and the conversation remains
  untested in this run.
  A [checkpointed traced client request](evidence/bootcamp-s5-soldier-converse-traced-checkpoint-20260924.json)
  resolved the wounded soldier, emitted objective topic `1995/2/1`, and opened
  Objective Completion. It did not use Continue and does not explain the
  earlier uninterrupted no-dialogue click.
  A [second checkpoint run](evidence/bootcamp-s5-soldier-continue-corpse-guard-20260924.json)
  clicked Continue and confirmed objective 2 complete and objective 3 active
  in one session. The corpse approach stopped on a route guard without a use.
  A [guarded follow-up](evidence/bootcamp-s5-soldier-corpse-same-session-20260924.json)
  then completed both interactions in one original-client session from the
  diagnostic soldier checkpoint. Objective 1 and its 600-second timer became
  active; the post-use state is archived for a timed wreck continuation.
  A [no-reset timer continuation](evidence/bootcamp-s5-timed-wreck-collision-stop-20260924.json)
  advanced along the high bank and stopped on an early low-progress sample near
  `(-105.45,83.77,43.03)` with `00:04:16` left. A later accepted Move to
  `(-107.20,83.77,41.27)` disproves the claimed stall; no bomb interaction occurred.
  The [timing recheck](evidence/bootcamp-s5-trench-delayed-move-audit-20260924.json)
  calls for a fresh accepted-packet wait before treating a short zero-progress
  sample as a collision.
  A [copied timer-anchor-reset route diagnostic](evidence/bootcamp-s5-wreck-anchor-reset-diagnostic-20260924.json)
  crossed the dry bank and reached the wreck approach, then stopped at a
  [sandbag barrier](evidence/bootcamp-s5-wreck-sandbag-approach-audit-20260924.json)
  near `(-182.48,95.30,-33.20)`. It had no bomb prompt and is not an
  original-timer completion. The original navmesh suggests a north-then-west
  bypass; ordinary-client passage on that route is still unverified. An older
  [V14 record](evidence/bootcamp-s5-timed-loc-v14-20260924.json) reports
  unreset bomb use and S6 arrival, with source captures now missing.
  A [north-west copied-client detour](evidence/bootcamp-s5-sandbag-northwest-diagnostic-20260924.json)
  passed the earlier stop but met a second visible sandbag line at
  `(-185.65,94.31,-28.24)` and ended without a bomb prompt or use. It used a
  copied timer reset and does not revise the V14 outcome.
  A [subsequent copied probe](evidence/bootcamp-s5-second-sandbag-probe-interrupted-20260924.json)
  stopped on an early dry-bank route guard; a later accepted Move from that
  same input advanced toward the waypoint, so the intended second-sandbag
  clearance test did not occur.
  The [guard-corrected retry](evidence/bootcamp-s5-second-sandbag-retry-sigterm-20260924.json)
  ended with SIGTERM during copied-client loading, before route input; its
  restored archive adds no evidence about the second sandbag. The
  [runner lifecycle audit](evidence/bootcamp-s5-isolated-sigterm-lifecycle-audit-20260924.json)
  supports a bounded detached supervisor for the next attempt but cannot
  identify the signal sender.
  A [detached retry](evidence/bootcamp-s5-detached-second-sandbag-route11-20260924.json)
  ran five minutes and crossed the earlier waypoint-4 guard, then stopped by
  a steep rock slope at `(-168.09,91.41,-16.88)` after only 0.23 m target
  progress. It did not reach the later sandbag or bomb.
  An [earlier accepted client climb](evidence/bootcamp-s5-rock-slope-normal-client-bypass-candidate-20260924.json)
  passes a point 0.46 m west of that stop, supplying a short guarded local
  detour for the next route test.
  A [bounded retry](evidence/bootcamp-s5-rock-slope-v2-route11-divergence-20260924.json)
  reached the approach but diverged west when aiming at the nearby first
  climb point. It stopped before the second sandbag. A prepared unrun controller
  revision instead aims at the farther goal used in the earlier accepted
  climb, with local divergence and timer guards.
  The [v3 copied-client run](evidence/bootcamp-s5-rock-slope-v3-northwest-path-20260924.json)
  reached `(-203.17,95.95,-36.54)` beyond the previous northwest stop with
  about 4:30 on its reset timer, then stopped on an obsolete driver
  postcondition. The visible sandbags ahead and bomb interaction remain open.
  A [v4 continuation](evidence/bootcamp-s5-v4-northwest-progress-threshold-20260924.json)
  stopped earlier because its northwest driver's 0.25 m target-progress
  threshold rejected a 0.222 m improvement after accepted movement. A bounded
  lower-threshold revision is prepared but unrun.
  The [v5 retry](evidence/bootcamp-s5-v5-northwest-waypoint2-stop-20260924.json)
  passed the earlier threshold, then a forward hold beside the next sandbag
  line changed yaw without changing position at `(-187.58,95.16,-30.24)`.
  A prior accepted route ran slightly north and lower there. Further route
  work needs a guarded lateral move based on that trace, not the same heading.
  A [staged local backtrack](evidence/bootcamp-s5-v6-local-backtrack-command-race-20260924.json)
  reached the earlier successful approach with ordinary movement; a harness
  command-file parse race interrupted its next short probe. The copied
  position edit makes this geometry evidence only.
  A [second staged local probe](evidence/bootcamp-s5-v7-local-northwest-point-reached-20260924.json)
  set the earlier accepted heading at the backtrack point and reached the
  next prior passage point by ordinary client movement. Its staged yaw and
  timer still leave a continuous route from the v5 stop unverified.
  An [integrated v8 attempt](evidence/bootcamp-s5-v8-slope-waypoint12-divergence-20260924.json)
  stopped before reaching the northwest route after its accepted slope
  movement diverged from the next wreck target. The fallback and bomb use
  did not run; the next route adjustment needs a short verified heading
  correction near the earlier v3 slope trace.
  A [staged local slope probe](evidence/bootcamp-s5-v9-local-slope-point-reached-20260924.json)
  used v8's accepted position and yaw and reached within 0.46 m of v3's
  first wreck-approach point through three ordinary client moves. A
  [continuous v10 attempt](evidence/bootcamp-s5-v10-continuous-slope-waypoint13-stop-20260924.json)
  then crossed that slope without a copied-position edit, reaching the same
  approach area with 5:50 left. Its next guarded 180 ms forward hold changed
  yaw but sent no accepted movement; bomb use and S6 remain unverified in that run.
  A [staged local v11 heading probe](evidence/bootcamp-s5-v11-local-waypoint13-heading-20260924.json)
  started from the archived v10 stop. Two guarded holds at headings 0.94 and
  1.08 radians moved through the rocky point to within 0.064 m horizontally
  of v3's next accepted waypoint. This local correction still needs a
  continuous timed route check. The [continuous v12 check](evidence/bootcamp-s5-v12-continuous-waypoint13-endpoint-20260924.json)
  confirmed both heading holds produce accepted movement from a different
  waypoint12 start, but the fixed two-hold controller stopped 0.907 m short
  of its 0.70 m endpoint guard with 6:07 remaining. It did not reach the bomb;
  the next controller should repeat short guarded holds until the target is
  reached or a genuine movement stall is observed. A
  [continuous v13 attempt](evidence/bootcamp-s5-v13-waypoint9-threshold-v14-prepared-20260924.json)
  stopped earlier at waypoint9 despite a fresh accepted packet moving 0.262 m
  and improving target distance by 0.225 m: the older 0.35 m progress guard
  rejected that valid small step. An offline v14 controller now uses a 0.10 m
  positive-progress guard there while retaining elevation, timer and bounded
  movement checks. The [v14 continuous check](evidence/bootcamp-s5-v14-waypoint5-southern-stop-20260924.json)
  stopped earlier at waypoint5 after a southern checkpoint4 trace approached
  nearby cargo geometry. A fresh packet changed yaw without position change;
  collision is unproven. Its next route check is a short guarded rejoin toward
  the earlier northern track before another full timed attempt. The
  [staged v15 local probe](evidence/bootcamp-s5-v15-local-waypoint5-north-rejoin-20260924.json)
  found that rejoin: one 250 ms guarded hold at yaw 0.212 moved 1.625 m to
  within 0.143 m of v13's accepted waypoint5 track. A continuous route with
  that conditional correction remains unverified. The
  [reusable packet guards](evidence/bootcamp-s5-packet-guard-v15-rejoin-20260924.json)
  now encode the observed small-step threshold and narrow northward rejoin
  region for the next isolated controller run; neither changes gameplay.
  The [armed-bomb interaction audit](evidence/bootcamp-s5-armed-bomb-interaction-boundary-20260924.json)
  identifies the original client's generic disarm transition and a prior
  armed-state use prompt. The current server ignores a second use and resets
  the fuse after an instance rebuild; original S5 outcomes for those cases
  remain unverified.
  The [original-client corpse-use audit](evidence/bootcamp-s5-corpse-original-client-use-protocol-20260924.json)
  requires a selected usable with valid state, range to its model DAMAGE1
  point, and native line of sight before `RequestUseObject` is sent. A visible
  use prompt and accepted request are the next interaction checkpoints; the
  current analogue's nominal 3 m use range does not establish Conrad's
  final-live class or position.
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
- A [checkpointed ordinary Alia Das route](evidence/bootcamp-s6-rogers-ordinary-route-20260924.json) accepted Training
  Day and walked to 8.8 m from seeded Rogers; later copied-client logins did not reach a playable HUD. In the
  [longer loading diagnostic](evidence/bootcamp-s6-wilderness-loading-diagnostic-20260924.json), Game accepted the
  connection and received `MapLoaded` 231.37 s after character switch, while the client still showed Wilderness
  loading at the 240 s check. This did not establish a Game server fault or verify the final walk and 1995 turn-in.
  A later [S6 continuation from V14's saved arrival](evidence/bootcamp-s6-after-v14-ordinary-rogers-20260924.json)
  became playable after relog, accepted Training Day, walked by ordinary movement to Rogers at
  `(854.3281,294.2227,388.6525)`, opened his 1995 completion panel and turned in Calling for Reinforcements.
  The original client showed Mission Completed; the copied database persisted 1995 complete and 1526 active.
  The relog boundary is explicit, so this verifies ordinary S6 arrival-to-Rogers completion from the saved
  post-S5 state, not one uninterrupted corpse-to-Rogers client session. The original exit trigger and exact arrival remain
  `GAP-S6-TRIGGER` and `GAP-S6-ARRIVAL` because the final-live footage cuts over the transition.

## W1 (Wilderness arrival: Training Day) status

- A [preserved post-Rogers client continuation](evidence/s6-training-day-ordinary-route-kincaid-20260924.json)
  walked to Kincaid without coordinate or level edits, completed objective
  1526/1, and opened his mission reward panel with 120 credits and two pistol
  choices. Its 900-second bound ended before reward selection; the saved
  objective completion persisted, but timed shutdown left the character at
  the original arrival position. A [second bounded continuation from that saved
  state](evidence/s6-training-day-ordinary-route-reward-relog-20260924.json)
  walked again from the arrival position, selected the Vextronics Pistol,
  completed Training Day, and logged out and back in. Its copied database
  persisted mission 1526 complete, credits 360→480, item template 116929 with
  module 900221, and unchanged level 2 / 8695 XP. These are emulator results;
  the 120-credit amount conflicts with a contemporary 180-credit listing; the
  [source audit](evidence/training-day-credit-source-audit-20260924.json)
  cannot resolve the final-live amount. The template and module IDs remain
  reconstructed. The natural Recruit tier gate remains open
  pending the [XP source](evidence/s5-s6-tier-gate-xp-followup-20260924.json).

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
    900221/900256 are identified from the original client. They are now
    included in the Training Day offer and persisted on the selected item;
    tooltip definitions now reproduce the filmed −15 for 15 sec lines, while
    combat effects remain open
    (`GAP-W1-REWARD-MODULES`).
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
  (`GAP-W1-GEAR-MISSIONS`). An [isolated recovered-client check](evidence/client-training-day-turnin.json)
  opened Kincaid's objective-completion conversation, completed the objective,
  selected the physical pistol and turned in 1526. The copied database gained
  120 credits and template 116929; both persisted after relog, while the
  mission tracker disappeared. The client rendered the reward as generic
  “Pistol” without the original footage's “Vextronics” prefix or Reduce Resist
  module line. The subsequent
  [reward module binding](evidence/training-day-reward-modules.json) sends the
  original-client Vextronics module IDs in the offer and persists the chosen
  ID on the item. A second isolated recovered-client run displayed both
  Vextronics names in the offer and turn-in and persisted module 900221 on
  the selected pistol. Original-client effect IDs 9/115 and the filmed
  amount/duration now feed the module tooltip response. A further client run
  displayed the original `[2] Reduce Resist` lines for both choices. Proc
  chance and combat application remain a fidelity gap; an archived 2008
  support reply says the debuff can trigger on each shot, affects subsequent
  shots, refreshes duration and does not stack, without specifying the chance.
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
- Recovered-client check on 2026-09-22 (isolated run 50, compatible client 1.16.5.0): a copied Training Day-complete
  character was diagnostically set to Recruit level 4 and 43,000 XP at Kincaid. His View Classes topic opened Tier
  Advancement; selecting Soldier enabled Train and brought up the permanent-class/clone confirmation. Confirming raised
  the character to level 5 (3 attribute and 4 skill points), then immediately opened the Headquarters "Getting It In
  Gear: Soldier Class" offer. Its Decline control was grey, and accepting it put "Report to Quartermaster Caufield" in
  the mission tracker. The copied character database records class 2, level 5 and mission 2010 active. This verifies the
  emulator/client handoff and presentation, not final-live timing independently. Evidence: `docs/evidence/client-class-gear-handoff.json`.
- Recovered-client check on 2026-09-22 (isolated run 53): Caufield is visible and talkable at his seeded supply-tent
  spawn. His mission topic completes the objective; the subsequent completion screen lists all six Soldier pieces, and
  Complete Mission grants them. The saved database records templates 122859/122860/122862/122863/122864/122865,
  mission 2010 completed, and no XP or credit change. The helmet is red in the backpack until Reflective Armor 1:
  Novice is trained. His generic conversation shows `ERROR: ?: No greeting`, an unresolved presentation defect
  (`GAP-W2-CAUFIELD-GREETING`); an authentic greeting id/text has not been recovered.
- Run 54 relogged the completed character: class gear 2010 stayed complete and its six items persisted. Caufield offered
  the separate "Lurking In The Shadows" mission instead of repeating class gear.
- Recovered-client checks on 2026-09-23 (isolated runs 55-57) also exercised Specialist: Kincaid's class choice released
  level 5 and immediately offered mission 2011 with disabled Decline. Caufield completed it and granted Hazmat helmet,
  vest, gloves, legs, boots and Repair-O-Matic (templates 122866-122871). One point in Hazmat Armor made the five armor
  items usable; one in Tools made Repair-O-Matic usable. All six equipped and persisted with both skills after relog,
  while mission 2011 stayed complete and Caufield instead offered "Lurking In The Shadows". The tool showed `0/0` ammo
  and an Out Of Ammo tutorial. The emulator's client-derived weapon row points to ammo class 3807 and clip size 10, but
  an original final-live ammo grant is not verified (`GAP-W2-REPAIR-TOOL-AMMO`). A [2007 firsthand Specialist account](https://www.engadget.com/2007-11-26-adventures-from-the-back-row-the-specialist-and-her-tools.html)
  describes repair tools consuming Power Cells on damaged armor and Tools rank I permitting their use; its date and scope
  do not establish the final-live mission reward's ammunition. Evidence, diagnostic inputs and hashes:
  `docs/evidence/client-specialist-class-gear.json`. A later
  [read-only vendor audit](evidence/w2-specialist-power-cell-vendor-audit-20260924.json)
  found template 56, class 3807, stocked for 3 credits at the current Alia Das
  ammo vendor. An [independent original-client audit](evidence/w2-specialist-power-cell-original-client-source-20260924.json)
  confirms Repair-O-Matic template 122871 uses class 3807, and identifies template 56 as Standard Grade Power Cells.
  The vendor stock and price trace only to the emulator seed; final-live stock and price remain
  unverified. The prior isolated Specialist checkpoint was lost with `/home/blizz/backups`. A
  [2026-09-24 isolated continuation](evidence/w2-specialist-vendor-isolated-client-diagnostic-20260924.json)
  diagnostically staged the surviving S6 Recruit at the tier gate, trained Specialist through Kincaid and accepted 2011.
  It reached the barracks tent wall before Caufield, so it did not acquire the tool or reach the vendor. Its archived
  copied database is a Specialist level-5 checkpoint with 2011 active; the working harness was restored to its staged
  Recruit baseline after the run. [Further isolated client diagnostics](evidence/w2-specialist-power-cell-isolated-purchase-reload-20260924.json)
  staged copied positions near Caufield and the emulator vendor. Caufield's turn-in granted Repair-O-Matic; the client
  shop displayed Standard Grade Power Cells at 3 credits, and a 120-cell purchase reduced copied credits from 480 to
  120. After Tools rank I and equipping the tool, a reload moved 10 cells into its clip and the settled HUD read
  `10/110`. The shop title said Armor Supplier Heffernan while the nearby minimap said Alia Das. A
  [direct client name-table check](evidence/w2-heffernan-name-id-correction-20260924.json)
  resolves creature name ID 10790 to that shop title, and the seed comment now identifies Heffernan at Alia Das.
  This confirms the copied emulator/client flow, not final-live vendor placement, stock or price; use on damaged armor
  remains untested.
- Remaining W2 fidelity checks: final-live reward presentation, weapon/tool stats, prices, the generic Caufield greeting,
  and whether either class-gear mission supplied ammo. The compatible-client paths for both classes now work through
  skill training, equipment and relog.

## W3 (Alia Das hub: the first Wilderness missions) status

The 2026-09-15 reconnaissance below preceded the seeded batches later in this section. W3 follows W2: the missions the
character picks up in Alia Das once Training Day and the class choice are behind them. Evidence gathered in
`/home/blizz/backups/rasa-net/research/20260915-wilderness-bulk` (`work/client.json`, `work/tarapedia.json`,
`work/npc_resolution.json`, `work/membership.json`, and the `tools/` extraction scripts).

- Recovered-client entry check on 2026-09-23: after Specialist class gear 2011 stayed complete across relog, Caufield
  presented "Lurking In The Shadows" with "Speak to Oliver" as the first tracker objective. The isolated run accepted
  it and saved mission 427 active, with no class-gear reoffer (`docs/evidence/client-specialist-class-gear.json`). This
  checks the current handoff only; the mission's full objective chain and final-live gating remain to be verified.
- Isolated recovered-client run 58 moved that character to original world-seed Oliver spawnpool 180. Oliver showed the
  objective marker and talk prompt; his Objective Completion dialogue directed the player toward Proctor Fulgor.
  Continue advanced the visible tracker to "Kill Proctor Fulgor" and persisted objective 1 complete/objective 6 active
  (`docs/evidence/client-wilderness-oliver-first-objective.json`). Normal travel and the remaining kill/return/reward
  sequence remain to be checked.
- A seeded .NET 5 scenario test on 2026-09-23 drove creature 76 through `MissionManager.OnCreatureKilled` from that
  persisted objective state: objective 6 completed, objective 7 "Return to Caufield" became active, the mission stayed
  active, and no reward paid early. A repeated kill notification emitted no second completion
  (`src/Rasa.Test/BootcampOpeningTests.WildernessFulgor.cs`). This verifies server transition logic against the migrated
  world seed; the recovered-client combat, Caufield return and payout remain open.
- Run 63 diagnostically placed the same level-5 Specialist near the world-seed Fulgor spawn. The boss spawned at
  (231,261.7,591.2) and killed the character at (237,266.6,591); objective 6 stayed active. The original navmesh has
  walkable layers at about Y261.7 and Y266.2 there, while a [named fan-site boss record](https://www.ellatha.com/tr/bossnpcview.asp?key=Proctor+Fulgor)
  places him at `/loc` (231,265,591), so his final-live platform and level remain unverified.
- The historical progression gate is now seeded: the 2007 official Wilderness walkthrough orders Too Close For Comfort
  → Receptive Reception → Forming Alliances, and a pre-shutdown TaRapedia revision lists Forming Alliances as mission
  427's requirement. The earlier recovered-client run that offered 427 immediately after class gear predates the
  `WildernessHubFormingAlliances` gate migration. A migrated-seed mission-manager test now confirms that 1069 requires
  completed 1407, 479 requires completed 1069, and 427 remains unavailable until 479 is completed
  (`src/Rasa.Test/BootcampOpeningTests.WildernessChainGate.cs`). The 479 Thrax Heart drop and vest counterpart remain
  labelled estimates; this chain still needs a normal client playthrough. The original client identifies item class
  10346, but two template rows map to it and neither is tied to the final-live server's drop table. The 2008
  mission walkthrough also describes carrying the stolen ammo shipment and failing on death; the recovered client has
  only the three visible talk/kill/return objectives and does not identify the item template or server failure rule.
  See `docs/evidence/wilderness-lurking-chain-audit.json`; final-live gate/item applicability remains open.

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
  Forming Alliances **479** (Apirka; one objective, "Collect twelve Thrax hearts") → Conscientious Objector **1390**
  (Apirka; 8 objectives, packages 112/1646; 2,000 XP / 400 credits) → Conscientious Objector – Part Two **1392/1393**
  (turned in to Rogers via package 116; 8,000 XP / 800 credits). Parallel from Forming Alliances: Lurking In The Shadows
  **427** (Caufield, 4,000 XP / 400 credits) and River Recon **429** (Rogers, 10,000 XP / 1,000 credits, follow-up
  Distress On The River). Separate line: Supplies On The Double **428** (Corman) → A Father's Goodbye **421** (Saviours
  gives it, Information Spec. Saviours pays it). The `Logos:` line (1638 and siblings) is the Logos training path.
  Client mission **1391** is Bug Em at Quasso Station, not a Conscientious Objector branch. The 479 → 1390
  gate is deployed. The original-client choice indexes, objective texts and
  report variants now route 1390's release branch to 1393 and arrest branch
  to 1392; image `rasa-dit-test:20260923e` is deployed, while recovered-client
  path verification remains open. See `docs/evidence/wilderness-conscientious-gate.json`
  and `docs/evidence/wilderness-conscientious-branches.json`.
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
  Alia Caverns shrine volume and its usable, the timer for 428, and final-live verification of the 1390 branches. The owner's
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
  - **1390** contains the two branches of the Ethical Parable, spelled out: *"Tell him that Milpas is free to leave
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
  - **GAP-W3-1069-GATE closed (2026-09-23)**: 1407 now has a definition, and `WildernessHubReceptiveGate` adds the
    historically recorded completion requirement to 1069. The 2007 TaRapedia revision supports the chain; final-live
    server gating remains unverified. Blizz already completed 1407 and accepted 1069, so his absent Moawi marker is
    expected at that saved state. See `docs/evidence/wilderness-receptive-gate.json`.
  - **1407 displayed level correction (2026-09-24, source only)**: contemporary TaRapedia revision 8563 lists
    Too Close For Comfort at level 4; the current level-5 seed was inferred. A SQLite/MySQL migration and
    migrated-seed test now set 4, with provenance in
    [the level record](evidence/too-close-for-comfort-level.json). The recovered
    client has no numeric level for this mission, and final-live level remains
    unverified. This field is mission information, not an offer gate; this
    correction has not been deployed to the live Game container. The cited
    local TaRapedia/client extraction paths became unavailable when the
    `/home/blizz/backups` tree disappeared later on 2026-09-24; their SHA-256
    values and exact field locations remain in the evidence record, but the
    source files cannot currently be re-read from those paths.
  - **1069 level and shrine recovery (2026-09-23)**: the 2008 TaRapedia revision lists level 4, correcting the
    unsupported level-5 reconstruction. The world row and migration history are updated, and the server restarted.
    A source-entity match in Logos recovery prevents another pending shrine from consuming the Enhance action;
    migrated-seed tests verify objective 1 completing and objective 2 appearing. The actual recovered-client use
    sequence in Alia Caverns remains to be checked. See `docs/evidence/wilderness-receptive-level.json` and
    `docs/evidence/wilderness-receptive-shrine-recovery.json`.
  - **Next playable chain gap**: 479 Forming Alliances is deployed around the original client objective skeleton. The client
    identifies Thrax Heart class 10346 (12-stack) but maps it to templates 2285 and 16540, with no recovered server
    selection. The original client mission log names Thrax Soldiers, and a 2007 guide says hearts drop randomly;
    drop probability is unknown. The seed labels its chosen 50% chance, template 2285 and vest counterpart
    13738; the original Luminar name and effects remain a gap. The 479 → 427 gate is deployed.
    The recovered client uses a distinct item counter keyed by
    item class; its opcode-566 update packet, pickup handler, saved mission item counter and active-objective
    drop mechanism and atomic consumption of carried items at turn-in are deployed, but have not been tested with the live client yet. The consumption rule is inferred from the mission text. Per-field sources and estimates are in
    `docs/evidence/wilderness-forming-alliances-reconstruction.json`. See
    `docs/evidence/wilderness-forming-alliances-research.json`.
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
    bindings, so 1390's escort steps (objectives 4, 8, 12) initially had none and were flagged optional. The
    2026-09-23 branch rule now requires objective 4 on arrest or 8 on release; objective 12 remains unresolved.
    The content loader requires a completion binding for every fixed required objective. The later escort
    implementation, historical reward records, and progression gates supersede this batch's original gaps;
    exact final-live item rewards and recovered-client branch verification remain open.
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
  - **W3 batch 8 (2026-09-16)**: a fourth zone, the **Marshes** - Lieutenant Morrison and three Retreads, with their
    four missions. That makes 22 NPCs and 24 missions created from this pipeline in one session, and the count of
    missions in the world 46 (8 boot camp, 14 hub and Wilderness, 24 from this pipeline).
  - **W3 batch 9 (2026-09-16)**: Torden - the **Mires** (Sgt. Jeansonne, Chakel, Corporal Cooper, Lt. Foushee,
    Corporal Hairston on map 1759) and one plateau liaison (Receptive Liaison Ridout), with seven missions. Twenty-
    eight NPCs and 31 missions from this pipeline now; 53 missions in the world.
  - **W3 batch 10-11 (2026-09-16)**: Torden completed - the **Plains** (Colonel Whitaker, Xenori, Captain Reyko,
    Engineer Tralos and Receptive Liaison Sage, 11 missions) and the **Incline** (two NPCs, two missions). Two Plains
    missions (648, 1064) were **skipped because Colonel Franks has no /loc recorded anywhere**, and the generator now
    refuses a giver it cannot place instead of inventing a position. Receptive Liaison Sage's Y reading sat 3.2 m
    above the surface the client's navmesh has under her, so her placement takes the map's height and keeps the
    reading's X and Z (TordenNpcGroundSnap) - the audit measures every position against that same navmesh.
  - **W3 batch 12 (2026-09-16)**: the hub's kill missions (427 Proctor Fulgor, 682 the Xanx).
  - **W3 Torden batch (2026-09-26)**: nine more Torden conversation missions on NPCs already standing -
    1745 Report to Liaison Repp, 526 Aid Packages, 648 Speak to Colonel Franks and 1064 Incriminating Delivery (the
    two the 2026-09-16 batch skipped: Franks exists as spawnpool 510084 with package 521), 802, 1014 Blue Flu, 1070,
    1326 Spoils of War and 1330 Retread Planning Part II (`TordenConversationMissions`). **936 The Results Are In is
    held**: Science Officer Clark stands on a surface the Plateau navmesh does not join to the Wedge Rock outpost
    (GAP-TORDEN-936-CLARK-UNREACHABLE). Six more (555, 831/840, 842/848, 1862) stay held on a radio trigger, a branch
    pair and two NPCs that do not exist. Open: four gates on unseeded or unknown missions, accept-time items, 1014's
    rewards, 1064's ambush, the pre-1.4 reward era and the Pools level (OD-60, pending owner review).
  - **W3 missing givers (2026-09-26, `MissingMissionGivers`)**: Ten Ton Hammer's dated area guides, which the earlier
    dossiers had not searched, place four of the "unplaceable" givers (`research/20260926-missing-npcs`). **Cmd. Sgt.
    Simpson** and **Ranger Tarina** (Foreas Base), **Field Sergeant Hanna** (Raintree Post, carrying package 423) and
    **Sergeant Dekay** (Irendas, the wormhole-waypoint midpoint, ±26 m) are created as 199950-199953 on the navmesh floor
    under the guides' readings. Standley and Arizpe take packages 2049/2025, Langerman leaves the unconversable Redshirt
    class (OD-45), and Warrior Mela moves from her OD-59 marker analogue to the same guide's Thoria Das reading. Seeded:
    **1741** (Langerman -> Standley), **1744** (Noonan -> Arizpe), **390** (Tarina -> Mela), **818** (Foletto -> Hanna)
    and **1862** (Dekay -> Michan). **Held**: Col. Almos and 551, because his reading fits no floor within its
    uncertainty and repeats the "Viands Village" label's y/z (GAP-ALMOS-HEIGHT, OD-67 open); Sergeant Conway and 835
    (GAP-CONWAY-POSITION); 340 and the 827 arms 833/841/842/848 (GAP-827-BRANCH-ARMS).
  - **Systems pass, owner goal 2026-09-16 (five items, in order)**: (1) **creature loot** — done, the original
    table's shape with the seven surviving rows (commit `ddcc921`); (2) **escort** — done, `Escort` behavior +
    `escort_mission_id` + the arrival rule, first escort seeded for 1390 (commit `0c288b6`); (3) **mission sharing and
    radio missions** — *not started*, and here is exactly what it needs; (4) **auction and crafting depth** — not
    started; (5) **mechs/PvP/endgame instances** — not started.

    ### (3) Radio completion and mission sharing — where to start

    The two features are the same five opcodes (`UnsupportedMissionRequestPackets.cs`, whose argument shapes were read
    from `client/missionlog.pyo`): radio is `CompleteRadioMission` / `RewardRadioMission`, both
    `(missionId, selectionIdx, rating)`; sharing is `ShareMission(missionId)`, `AssignSharedMission(playerId, missionId)`
    and `DeclineSharedMission(playerId, missionId)`. The mission definition flags that gate them are already loaded and
    sent to the client (`npc_mission.radio_completeable`, `npc_mission.shareable`; `MissionConstantData`), and
    `Mission.DefinitionGaps()` still adds "radio completion is not implemented" and "mission sharing is not
    implemented" for those flags — which is what keeps every such mission unoffered.

    The work: **radio** needs the payout path of `MissionManager.CompleteNpcMission` (lines ~789-900) split out of its
    NPC-specific checks (receiver id, conversation range) into a shared `PayOut(client, definition, selectionIdx)`, then
    a `CompleteByRadio(client, missionId, selectionIdx)` that requires `RadioCompletable` and complete required
    objectives instead; `RewardRadioMission` is the same call (the two packets carry identical tuples, so which one the
    client sends for "complete" and which for "claim the reward" is not established — record it as an open question
    rather than guessing). **Sharing** needs a pending-share map (sharer → target → mission), `ShareMission` to record
    one and offer the mission into the target's log, and `AssignSharedMission`/`DeclineSharedMission` to accept or drop
    it; the client-side presentation of a share offer is unverified, so it should be built to the smallest form that
    does not invent UI, with the gap recorded.

    ### (4) Auction and crafting depth

    `AuctionHouseManager` is 129 lines and `KraftwerksManager` 345; both have their client packets (8 crafting, and the
    auction requests ride the vendor packets). Nothing here is known to be wrong — it is simply thin, and the original
    auctioneer/vendor item tables and the Kraftwerks recipe data would be needed to deepen it. The original dump has
    `vendor_items` and an `items` table, so the auction's own data should be checked there first.

    ### (5) Mechs, PvP and endgame instances

    D15/D16 content (Empire Sector, mechs, Edmund Range). Nothing else depends on it, and it needs the zone data that
    only the reconstruction rules can supply.
  - **(5) Mechs, PvP control points and endgame — control points decoded and wired (2026-09-16)**: the research
    state recorded earlier that day is superseded. The `grep -l controlpointdata` step it named found the readers -
    `client/gameuiutil.pyo` `GetControlPointLabel` / `GetShortControlPointLabel` / `SortControlPointList` (source
    lines 2228-2264), which unpack a row as **`(typeId, nameId, mapTemplateId, level, sortOrder)`**. The full write-up
    is [pvp-control-point-client-evidence.md](pvp-control-point-client-evidence.md); the reusable discovery stands: the
    original client's 996 `.pyo` modules are on disk (`client-code/verify/pyo/`) with every `Recv_*` handler and its
    argument list in `client-protocol-inventory.json`, and a static disassembler drives xdis over them.

    **Control points, decoded**: `typeId` is `controlpointownershiptype` (1 `CLAN_OWNED`, 3 `TEAM_OWNED`, 6
    `FACTION_OWNED`), not `controlpointtype`; `nameId` is a `uielement` id (Whiskey, Charlie, Echo, Blue Base, Red
    Base, Control Point: East/West Depot), which is why it was not an entity class; `mapTemplateId` is a `maptemplate`
    id resolved to a context through `gamecontext` field 4 (2365 = `adv_wargame_provinggroundsv002` = context 2361,
    2377 = `adv_wargame_edmundrange2` = context 2374), which is why it was not a context; `level` is 50; `sortOrder`
    orders the tracker with `None` first. Twelve rows are the two final-live battlegrounds and five are test-map rows;
    `battlegroundrulestype` names one ruleset, `EDMUND_RANGE`. The status struct is `shared/controlpointdefs.py`
    `(controlPointId int, ownerId long nullable, stateId int, endTime int)`, states `kCPState_New/PreWar/War/PostWar`
    0-3, and `SetOwnerId`'s owner ids are -1 none / 0 neutral / 1 `RED_TEAM` (Bane package) / 2 `BLUE_TEAM` (AFS
    package). The challenge-board window that also reads the table is dead code (it names a constant no module
    defines, and the bid opcodes have no handler): clan bidding on control points was cut before shutdown.

    **Emulator**: `Data/ControlPointData.cs` carries the 17 rows with named fields and English text, the three enums,
    the owner ids and the ten `scorekeeperconstants` indices; `ControlPointStatus` has a nullable long owner and a
    typed state; `ControlPointStatusPacket` (814) now writes `(statusList,)` - it wrote one bare struct where
    `Recv_ControlPointStatus(statusList)` takes a list; `SetOwnerIdPacket` (884) is new; `RequestControlPointStatus`
    (817) is handled and answered by `ControlPointManager` with the channel's points, unheld and `New`, and
    `SetOwner` exists for when a capture rule is evidenced. The 814/817 pair's only client reader is the dead
    challenge board; the live tracker reads `ScoreBoardGameScore`'s `cpData` and the map/radar read CONTROL_POINT
    markers `(ownerTypeId, ownerId)` - both in the lifecycle gap. `UsePacket` writes its extra arguments (the mech pad's
    `Use(actorId, curStateId, windupTimeMs, boardingTimeMs, effectTypeId)` needs them). `ControlPointDataTests`
    pins the rows, the tracker order and the wire shapes (7 tests, net5 container).

    **Still gaps, now narrower** (`GAP-W3-PVP-CP-PLACEMENT`, `GAP-W3-PVP-CP-CAPTURE`, `GAP-W3-BATTLEGROUND-LIFECYCLE`,
    `GAP-W3-PVE-CONTROL-POINT-PLACEMENT`, `GAP-W3-MECH-SERVER-SIDE`): the client maps of both battlegrounds hold no
    control-point entity, so the points' positions are server data; capture rules, war timings and the token items are
    not in the client; the team/scoreboard/win protocol is recovered (section 4 of the evidence doc) with no server
    lifecycle to drive it; the emulator's one Wilderness PvE control point (class 3814, status 215) is
    emulator-authored and now says so. **Mechs (D16)**: `mechpad.py OnBeforeUse(actorId, boardingTimeMs,
    effectTypeId)`, `MORPH_MECH` 457, the two abilities, pad use states 216-219 and class 30464
    `UsableOwnableMechStation` (augmentation 83) are recorded; the server side is a system to build. The 2026-09-16
    Alienware survey found the same 1.16.5.0 client twice, toolkit renders of both battleground maps and no
    battleground or mech footage; its unmounted Windows partition is the one unsearched place there.

    **Endgame zones**: unchanged - the twelve level-banded adventure zones are decoded, and nothing places creatures or
    content above the Wilderness; the reconstruction rules (OD-45, OD-48) are what fill it, no new mechanics.
  - **Boot camp scenario tests repaired (2026-09-17)**: the owner asked for the boot camp's scenario quests to be fixed.
    Reading them showed the *camp* is fine and the *tests* had gone stale underneath it, which is worth stating plainly
    because the failures looked like quest bugs. Three things had drifted:

    1. `ArriveAtAliaDas` pinned the Wilderness's whole placement inventory, so every later batch broke it. By the time it
       was read, the zone also held the Alia Das hub's Witherspoon (198686) and Moawi (198687), mission 430's four mortars
       (199700-199703), and Dawson (199002) and Brice (199003) had been **moved to the Divide** - the later,
       evidence-backed correction, since their `/loc` readings are Divide coordinates. The scenario now asserts the three
       placements it actually needs (Rogers, Kincaid, Milpas) with their behaviours; zone inventory belongs to the batches
       that created it, and their own migration tests plus the world position audit already cover it.
    2. The per-batch mission counts were written as *giver* counts while every batch seeded `giver = receiver`.
       MissionAreaLinks then gave the cross-zone hand-offs the givers TaRapedia names, which moves a mission out of one
       batch's giver set and into another's receiver set. They are counted on either side now (`MissionsInvolving`), which
       is what the original numbers meant, and the numbers hold: plains 12 (one of them the incline's 1747, which Sage
       hands out from the plains), incline 1 giver / 3 involved, mires 7, marshes 4, plateau 6, palisades 9.
    3. `TurnInAtRogers` asserted Rogers had nothing left to say at all. He keeps the Alia Das hub's own missions at the
       same arrival, so the assertion is now that *this mission* is no longer among his conversations.

    Repairing those surfaced a real defect rather than hiding one, and the strictest test in the set is what found it:
    `EveryCorrectedMissionIsGivenAndReceivedByASpawnedCreatureOnTheRightMap` fails when a corrected mission's objective
    completes through a package no creature carries. Two do - **422 "Miner Difficulties"** (package 213) and **429 "River
    Recon"** (package 726) - and the client's own text identifies both missing NPCs: 213's dialog ("You're a sight for
    sore eyes, soldier! We've been waiting on these supplies...") is **Mining Coordinator Richards at the entrance to the
    Pinhole Falls Caverns**, whom 422's own text names, and 726's is **the dying Forean Ranger** whose last words send the
    player back to Witherspoon. Both are reconstructable through OD-45; until they exist those objectives have no
    conversation to complete through. Recorded as **GAP-W3-UNBOUND-CONVERSATION-PACKAGE**, and the test now names the two
    missions explicitly so a third can never appear quietly.
  - **The two NPCs 422 and 429 were waiting for (2026-09-17)**: the broken-quest gap found while repairing the boot
    camp tests is closed. The client's `objectiveconversation` table says which conversation package completes each
    objective and the server only offers a conversation from a creature carrying that package; two packages - 213 and
    726 - belonged to nobody in the world.

    **Mining Coord. Richards** (client name **3077**) now stands at the entrance to the **Pinhole Falls Caverns**
    (341.16, 228.70, 477.85). Two independent sources name him: the dialogue that completes 422 objective 1
    ("You're a sight for sore eyes, soldier! We've been waiting on these supplies for too damn long!") and 422's own
    text ("deliver two crates of mining equipment and medical supplies to Mining Coordinator Richards at the entrance
    to the Pinhole Falls Caverns"), while TaRapedia records him as 422's **RewardGiver** at Pinhole Falls Cavern - and
    as the giver of its follow-up "Mama Miasma". 422 is now turned in at him, which is what that page says and what
    the completion package requires.

    **The wounded Forean Ranger** (client name 3013) stands at the **top of Pinhole Falls** (309.51, 271.38, 436.97),
    where 429 sends the player to look for the lost patrol. His dialogue is the one 429 objective 5 completes through:
    "Bane ambush... they killed... all of us... Please... must tell human commander... they are moving... towards Alia
    Das... Tell Witherspoon...". 429's other completion packages (Rogers' 116, Witherspoon's 208) were already in the
    world, so only this one was missing.

    Both positions are the **client's own map markers** (`uimapmarker` text ids 40 "Pinhole Falls Caverns" and 39
    "Pinhole Falls"), and the navmesh floor under each agrees with the marker's own Y within 0.3 m; the falls has two
    floors (226.32 at the river, 271.38 at the top) and the mission says "near the top", so the top one is used. The
    appearance is the OD-45 analogue: the package 213 greeting is the Forean welcome ("our kind were not born of this
    world..."), so Richards is a Forean civilian (`NPC_Forean_Unarmed`, as Council Elder Moawi is a
    `Redshirt_Forean_Elder`) and the ranger a Forean archer (`NPC_Forean_Archer`).

    The batch also **offers 429 at last**. The loader had refused it since the client skeleton exports no objective
    flags: its objectives carried no ordinal, required or revealed flag at all, so it could not be dispensed. Their
    order is the one the mission text states - "search for signs of a lost patrol of Forean Rangers near the top of
    Pinhole Falls, *then* report your findings to Field Sgt. Witherspoon" - so the recon (objective 5) is first and
    revealed on acceptance, the report (objective 4) second. The live loader's "not offered" line for 429 is the
    acceptance check.

    The manifest carries a row per field for the two creatures, their packages and their placements, and a `change`
    per correction (422's receiver, 429's objective flags). Raising the scopes for the new ids also surfaced two
    client maps that earlier hand-off rows cited without being declared; they are declared now, with the two that are
    not decoded to entities saying so in their locator rather than implying decoded positions exist.
  - **Every quest the world cannot finish, named (2026-09-17)**: `MissionLinkAuditTests` reads the deployed
    `rasaworld.db`, but it looks for it beside the `navmesh` folder - and the folder was not in the test container,
    so both of its checks had been reported as *inconclusive* on every run since they were written. With the navmesh
    mounted they run, and they name **34 objectives over 22 missions whose completion package no spawned creature
    carries**: 321, 332, 382, 421, 427, 431, 442, 444, 451, 549, 670, 682, 698, 836, 969, 977, 1040, 1119, 1125,
    1183, 1186 and 1310. Each is a mission a player can accept and then not finish, and the two closed on the same
    day (422 and 429) were the first two of that list, not the whole of it. Every triple is now written into the
    test's `UnboundPackages` set, which fails both ways: a thirty-fifth cannot appear quietly, and one that gains
    its NPC must be taken out. Recorded as **GAP-W3-UNBOUND-CONVERSATION-PACKAGE** with the row list in the
    manifest; the closing recipe per row is the OD-45 pipeline. The same run found the camp's own content test
    two rows behind the world - the placements of Field Sgt. Witherspoon and Council Elder Moawi were withheld in
    its fixture because they name world-seed creatures, and mission 429 is a world-seed row no migration inserts -
    and four missions failing the giver check that have no giver on purpose (1990, 1526, 2010, 2011 are dispensed
    by a content rule). All three are fixed, and the camp's content now loads its seven Alia Das placements with
    zero gaps.
  - **Death anywhere but the two starting maps left the body where it fell (2026-09-17, live report)**: the death
    work of 2026-09-14 shipped a hand-built catalogue of **seven hospitals on two maps**. `OfferedHospitals` looks
    up the map the player died on, finds nothing anywhere else, and `ReviveMe` takes the documented
    revive-in-place fallback - which is exactly what "when i die ... it respawns in place" describes. The
    catalogue is now built by running the same recipe over **every map the server loads**: each client
    `uimapmarker` HOSPITAL (3) or SAFE_ZONE (19) marker, joined to `graveyardlanguage` and `waypointlanguage` by
    its name, with the world seed's type-5 teleporter row naming the marker whose own text has no waypoint entry.
    It reproduces all seven hand-built rows **exactly** - ids, marker entities, positions, the safe-zone flag and
    the 136-over-183 ambiguity choice - and resolves **102 hospitals on 41 maps**. A new standing audit measures
    every one against the navmesh: all 102 have walkable ground, 97 within 2 m, the four highest on raised floors
    the mesh models at their base (up to 4.45 m, so the player lands), and one 1.06 m under a floor, listed with
    its measurement. The 66 markers the recipe cannot resolve - no `graveyardlanguage` entry reads their name, so
    the id Hospital Selection would show is unrecovered - are **GAP-HOSPITAL-UNRESOLVED-GRAVEYARD**; they are
    mostly instances and the wargame maps, and those maps keep the revive-in-place fallback.
  - **"Thrax infantry is in the ground" (2026-09-17, live report)**: measuring every position in the world at
    once showed the audit's own reference was off. Our 107 content placements sit a median **0.415 m below** the
    walkable surface the navmesh gives - but so does everything else: the original server's own 217 creature
    spawns sit 0.276 m below it, its 357 teleporters 0.150 m below it, and the three characters' **own
    client-reported standing positions** 0.08, 0.31, 0.41 and 0.47 m below it. A player who is demonstrably on
    the floor reads under the navmesh, so the navmesh reads high on terrain - Recast's walkable surface is the
    top of a voxel column, not the terrain under it - and a row that matched the navmesh exactly was floating.
    The floor is now taken to be `surface - 0.276 m`, the offset the original spawns sit at, which is the one
    reference that is both original and about the same thing: a creature standing somewhere
    (**GAP-NAVMESH-FLOOR-OFFSET**). The bias is a terrain effect and not a constant - the grating platform's top,
    taken from the client's own map file at 122.11, matches the navmesh there to 0.01 m - so nothing was
    re-snapped wholesale. `WorldPlacementFloorSnap` moves only the **14 rows more than 0.5 m off that floor**:
    seven buried, the worst by 1.66 m, including the boot camp's **Thrax Initiate at the base gate, 0.73 m
    under** - the report's own sighting, and the shape of the invisible attacker of the same day, whose shots
    rendered while its body did not - and seven floating, the worst by 0.72 m. Only X and Z carry evidence for
    those rows; Y was never recovered for any of them. `WorldPositionAuditTests` now measures bodies against the
    floor at 0.5 m and everything else (trigger spheres, walk destinations, map markers) against the raw surface
    at 2.0 m, with the bomb on the wreck hull named as the one placement not stood on the floor.
  - **The respawn teleport reached everyone but the player (2026-09-17, live report)**: the hospital fix of the
    same day was not the whole of it. The log showed the server doing exactly the right thing -
    `Blizz died on map 1985 ... offering 1 hospital(s)` then `Blizz respawns at hospital 20000001 (357.9, 120.3,
    156.5)` - while the recruit stayed at the Thrax that killed them. The client's own `Actor.Recv_Teleport`
    carries no body: it blocks movement, runs the post-teleport fade and schedules `_TelportMovementCompleted`
    after the delay. The position arrives on the **movement channel**, and `MoveToHospital` sent that with
    `ignoreSelf: true` - so every observer's picture of the player moved to the hospital and the player's own did
    not. The waypoint teleport in `DynamicObjectManager` has always passed `false`. The death test could not see
    it, because its `Drain` helper kept only `CallMethodMessage` and threw the movement channel away; it now
    drains both and asserts the owner is told their own new position.
  - **The server's name is the client's, not ours (2026-09-17, owner request)**: nothing on the wire carries a
    server name - `ServerInfo` sends id, address, ports, age limit, PK flag, player counts and status - so the
    client turns the id into a name itself, and our id 234 reads `QA: Programming` out of its own
    `serverselectionlanguage` table. The original client already has the hook for this:
    `clientlanguagemanager.GetServerNameAndDesc` consults `client.development.devserverlanguage` **first** and
    only falls back to that table, and `client/development/` is not in the shipped 1.16.5.0 `trpython.zip`. So
    the rename is one added module and **no original file changed**. Built and verified in
    `research/20260917-server-name`: a Python 2.4 marshal writer that round-trips shipped modules
    byte-identically, bytecode copied from the pattern `serverselectionlanguage.pyo` itself uses for this table,
    all 21 shipped rows carried through (the override is all-or-nothing - `success` is set unconditionally, so a
    missing id would render as `None`) with only 234 replaced, and the result read back with xdis 6.1.7 as
    Python (2, 4) evaluating to `234: (u'Banshee Realm', u'Preservation', 8001)`. Delivered as loose modules and
    as a `trpython.zip` whose 970 original entries are asserted byte-identical. Untested: whether the shipped
    loader picks up an added zip entry, and whether a launcher integrity check rejects the repack.
  - **Fifteen of the 34 dead-end objectives closed, by looking before building (2026-09-17)**: the first read of
    the mission-link audit was that those NPCs did not exist and each would need the OD-45 pipeline that built
    Mining Coord. Richards - a name id, a TaRapedia position, an analogue appearance, an invented level. That was
    wrong. Checking the world seed for the names the client's mission text gives found **twelve of them already
    standing in the world**: Council Luminary Doyan, Council Advisor Todae, Dr. Eleanor Corman, Ranger Anjuhi,
    Ranger Tirna, Arms Supplier Oliver, Engineer Salter, Information Spec. Saviours, Lt. Wood, Medical Assistant
    Duncan, Surveyor Hugh Corman and Tribal Leader Oingin - each with its client name, class, level and health,
    each drawn by a spawnpool slot in the right place, and each with **no `npc_package` row**, so the server had
    no conversation to offer from it. `WildernessDialogueBinding` is twelve rows, and it invents no position, no
    appearance and no level.

    The bindings are the client's own `objectiveconversation` rows read back to the NPC its own text names: 427
    "Locate Arms Supplier Oliver", 431 "a Corman surveyor named Hugh", 441 and 700 "Medical Assistant Duncan at
    the Twin Pillars infirmary", 444 "take the test results to Dr. Eleanor at Ranja", 549 and 623 "Engineer
    Salter at Alia Das". Two are settled by the dialogue itself - Lt. Wood is named by the line before his, and
    Ranger Tirna says "Yes, I was once called Pundi". Tribal Leader Oingin is the interpolation between two
    confirmed neighbours: packages 251, 252, 253 map to client names 3093, 3094, 3095, the outer two confirmed by
    text, and the middle line is a Forean's. Ranger Anjuhi is elimination: 682 sends the player to the Rangers at
    Stone Anvil, the seed has exactly two of them there, and the other is Tirna.

    **Nineteen are left**, and the seed is now the first place to look for each. Two of them are known not to be
    NPCs at all: 451/3's package 569 is the villager who directs the player into the Urn, whose client name is
    not established, and 442/2's package 1486 is the **blood analyzer** at Twin Pillars ("ANALYZING...
    ANALYZATION COMPLETE"), a usable the content layer would have to dispense rather than a creature.
    **GAP-W3-TIRNA-NAME** records the one cosmetic mismatch left: the seed's Ranger carries a name the client
    renders "Ranger Tarina" while every mission text says "Ranger Tirna", and the seed's value is kept rather
    than corrected on a guess.
  - **The crate window never opened, and the crate's class cannot have one (2026-09-18, live report, fourth
    diagnosis)**: "I got my gear from the nearby crate and equipped it but the quest didn't advance", then "the
    crate looked a little transluscent", then "I am using the mouse button 2 on the crate, nothing is happening
    now". The log and the character database said the server was doing everything: the crate opened and logged
    its five rows, the five items of set 19858 were created at that millisecond and sent - and all five sat in
    the item table **owned by nobody**. The gear the player had equipped was their creation loadout, which shares
    two of the crate's five templates.

    The crate is entity class **26714 UsableTreasureDispHumCrateV04**, chosen under **OD-12** as *"the first
    TreasureDispenser candidate"* - by name, never against the loot protocol. Its only augmentation is **64
    TreasureDispenser**, "drops loot into inventory (or world) when used", and the client's treasuredispenser
    augmentation carries **no `Recv_` handlers at all** (`__init__`, `IsClosed`, `IsCipherable`). Every window
    method the server sends - `LootInfo`, `CanLootItems`, `TakenInfo`, and the `LootCorpse` added earlier the
    same day - belongs to augmentation **50 LootDispenser**, so the client dropped all of them. Exactly **two**
    classes in the whole client carry 50 - `Sys_LootDispenser` (3331) and `CorpseLootDispenser` (10000035) - and
    neither is a crate, so no class swap can give the crate a window.

    The original had one: footage **A3-017** shows a window headed **"Supply Crate"** with a **Loot All** button,
    A3-022 its five rows, A3-023 the "You received 1 ..." lines and A3-024 the objective completing. So a loot
    dispenser is **attached** to the crate - what `LootDispenser.AttachedTo` is for, and what every corpse does -
    and the window takes its heading from the attached entity. From there the corpse machinery serves it: the
    client addresses the dispenser, `FindLootable` finds it in `MapChannel.LootDispensers`, and taking, Loot All
    and the settle are paths already proven in play. The content layer now only opens the window and completes
    the objective once the container is empty, and the three loot handlers no longer need a content-container
    special case at all. The dispenser's own class is **GAP-S2-CRATE-DISPENSER-CLASS**: 10000035 is used because
    it is the one proven to drive the window, and the heading comes from `AttachInfo` either way.

    Three earlier diagnoses of this same objective were wrong or incomplete - missing rule kinds (wrong); rows
    built from bare template ids with no item behind them (real, fixed, but not why the window was empty); the
    missing `LootCorpse` (real, necessary, but sent to an entity that cannot receive it). Each was reasoned from
    the server's own code. The one that reached the cause started from the client's entity class table.
  - **The crate works (2026-09-18 22:23 UTC, confirmed in play)**: `RequestUseObject` -> the container opens
    through its attached dispenser -> **three `RequestTooltipForItemTemplateId` calls**, which is the proof the
    window is up, because the client only asks for a tooltip for a row it is drawing -> `RequestLootAllFromCorpse`
    -> `Content container 198651 settled for Blizz: 5 taken, 0 left, 0 still missing` -> `CancelCorpseLooting` ->
    `RequestEquipArmor`. The database agrees: the crate's five items created at 22:23:39 and held, four equipped,
    **1992 objective 1 Completed**, the equip objective Completed with it, and objective 5 revealed. Five attempts,
    and the one that finished was the one where the log could show what the *client* did rather than only what the
    server sent. **GAP-CRATE-OBJECTIVE-DEAD-END closed.**

    One thing is left unexplained and is recorded as such: the same build had already failed at 19:40, the open
    logging and the client again answering with nothing. Two restarts and a reconnect separate that from the
    working run, so the likeliest reading is a stale client-side crate entity from a build that re-sent
    `CreatePhysicalEntity` for it - the translucent crate - which would leave the attached entity the window
    measures its use range against unresolvable. Nothing was captured from the failing session to confirm it.
  - **Kraftwerks fabrication (2026-09-16)**: every crafting request was declined with "not available on this server
    yet" - the manager's own doc named the recipes as the next step. They are the client's: `shared/crafting.pyo`'s
    `recipeItemTemplateTable` holds **160 schematics**, each with its inputs (an item template and a quantity), its
    result (template and amount), an energy cost, `KraftwerksTimeSeconds` and the level required. Schematic **641** is
    the whole chain in one row: 500 of item 1765 into 500 of item 28 (standard-grade cartridges), five seconds, level 1 -
    which the tests assert, since it cross-checks the client table, the decoder and the data class together. Fabrication
    now resolves the schematic (from the item the player carries in the 1.16.5 window's form, from the recipe id in the
    older one), requires its level, **plans every input before consuming anything** so an unaffordable recipe leaves the
    materials alone, consumes with the same tested stack path item use takes, and starts a job timed by the recipe.
    Collection checks the station's clock and creates the item only at that point, so an uncollected job holds no item
    row. Salvaging, extraction, integration and upgrading still decline: their Mimeogel costs are recovered from the
    client's `moduleClassTable`, but the emulator has no item-module support to act on (**GAP-W3-CRAFTING-MODULES**).
    **OD-53**.
  - **Auction house (2026-09-16)**: the handlers were ToDo stubs - every request logged and ignored, and creation
    sent one of each failure message in a loop. It is implemented now from two pieces of the client itself.
    **The protocol**: the client's own handlers (recovered in the client protocol inventory from
    `client/auctionhouse.pyo`) give every reply's argument list - CreationSuccess(itemId), CreationFailed(itemId,
    message), QuerySuccess(itemList), QueryFailed(message), StatusSuccess(itemList), StatusFailed(message),
    BuyoutSuccess(itemId), BuyoutFailed(itemId, message), CancelSuccess(itemId), CancelFailed(itemId, message),
    AuctionSold(itemId, price), AuctionExpired(itemId). Four existed; the other eight are new server packets, replacing
    a ToDo list of names with shapes. **The parameters**: the client's own `durationdata` (four durations whose deposits
    are 5/10/20/25% of the price) and its 83-row `categorydata`, matched to an item through its class name using the
    table's parent/leaf pairs (Weapon contains Weapon_Pistol, Armor contains Armor_MotorAssist). Listing validates that
    the item is really the player's (the same test the vendor path makes), that it is sellable and not bound or
    character-unique, takes the deposit, and hands the item to the listing; buyout checks the client's stated price
    against the listing's and hands the item over before charging anyone; cancel returns it; expiry runs on the map
    channel tick. The auctioneer open request now verifies it is an auctioneer in range - it used to open the window for
    whatever entity id the client named. **OD-52**; the in-memory listings, the missing commission and the bypassed
    pickup window are **GAP-W3-AUCTION-PERSISTENCE**.
  - **Radio turn-in and mission sharing (2026-09-16)**: the last two mission-layer holes, and the ones that were
    actively withholding content - `Mission.DefinitionGaps()` added "radio completion is not implemented" and "mission
    sharing is not implemented" for any definition carrying those flags, which kept every such mission out of the game.
    Both are implemented from the only evidence available: the client's own requests in `missionlog.pyo`
    (`CompleteRadioMission`/`RewardRadioMission` = `(missionId, selectionIdx, rating)`, `ShareMission` = `(missionId)`,
    `AssignSharedMission`/`DeclineSharedMission` = `(playerId, missionId)`) plus the `npc_mission` flags, which decide
    what the client *offers* rather than what the server allows.

    **Radio.** The NPC turn-in's payout (reward choice, balances with their overflow guard, item slots, mission state,
    content reaction) was split out of its NPC checks into `MissionManager.PayOut`; `CompleteRadioMission` reaches it
    while requiring `radio_completeable`, so the receiver id and the conversation range - the whole point of a radio
    turn-in - are not required. `RewardRadioMission` carries an identical tuple and goes through the same path, which
    cannot double-pay because a completed mission is no longer completeable. Which of the two the client sends for the
    completion and which for the claim is **not established** (**GAP-W3-RADIO-REWARD-STEP**).

    **Sharing.** `ShareMission` gives the mission to each party member in the channel who does not have it and records
    the sharer as a pending share; `AssignSharedMission` clears the pending entry and `DeclineSharedMission` drops the
    mission back out. The offer *packet* is not recoverable (**GAP-W3-SHARING-OFFER**), so the mission is logged at
    share time - the reading that makes both accept and decline mean something.

    Together with the loot and escort work this closes the mission layer: the only definitions still withheld are the
    ones whose objective flags the seed does not carry. Tests: four radio (pays out with no receiver, non-flagged
    refused, required objectives still enforced, no double pay) and six sharing (party member gets the share, accept,
    decline, a share can only be answered by its sharer, unshareable refused, needs a party and an active mission).
    **OD-51**.
  - **Escort mechanic (2026-09-16)**: the piece 40-odd objectives were waiting on. `content_placement` gained
    `escort_mission_id` and a third behavior (`Escort = 3`): the creature walks with the player whose mission it is
    (re-pathing at most every two seconds once it falls more than six metres behind), and an **area objective on that
    mission only completes once the escort is inside the area** - so "Take Milpas to Apirka" cannot be finished by
    walking to Apirka alone. The rule lives in `ContentEscort` (tested: no escorts means unaffected, an escort behind
    means no completion, a sphere bounds vertically by its radius and a cylinder by its half height).
    First seeded escort: **1390 Conscientious Objector**, whose objective 4 ("Take Milpas to Apirka", area at Warrior
    Apirka's recorded position) and objective 8 ("Escort Milpas to Divide entrance", area at the world's own
    Wilderness -> Divide map link 11) were the two branch ends. **Ranger Milpas** is created with the client's own name
    id (9519) plus the OD-45 class/level treatment, standing beside the Alia Das arrival. **OD-50**.
  - **Creature loot (2026-09-16)**: the loot mechanic now has the original server's shape. Its
    `creature_type_loot` table is (itemTemplateId, chance as a float, stacksizeMin, stacksizeMax) keyed on the creature
    *type* - and this world's `creature` table already **is** the type table (name, name_id, class_id, faction, speeds,
    hit points, actions), so a creature row is a type and loot hangs off its id. New `creature_loot` table, preloader,
    repository, model snapshots and the roll (`CreatureLoot.Roll`, tested: chance is a percentage, the stack size is
    drawn between the bounds, every row rolls on its own). Only **seven** rows survived, all for original type 20
    "(Raiding) Trainee Thrax Footsoldier" - 12% for a 1-35 stack of standard cartridges, 5% for Class I Basic Med Packs,
    and 0.5% each for the five Motor Assist armour pieces - and they now sit on this world's Thrax soldiers (creature 3
    and the boot camp's initiates). Recorded as **OD-49**; every other creature keeps the stand-in drop
    (**GAP-CREATURE-LOOT**), because the client carries no loot data at all (369 decoded tables checked).
    *Note (2026-09-26, loot-evidence)*: the rows are emulator data (OD-96). The cartridge row is replaced by counts
    from the footage ledger (`CreatureLootFootage`, below), and the stand-in drop is a labelled analogue (OD-110).
  - **Reconstructed species and placement respawn (2026-09-16)**: with the owner's go-ahead for labelled best-guess
    reconstruction, the Mires got its first reconstructed species. The client's entity class table says what each class
    *is* (augmentation 1 = living creature, 6 = item, 41 = usable object, 52 = NPC), which separates a species from a
    mission item - and the item classes carry the creature they come from in their own name ("MisXenoFlareGasherTeeth").
    Lasher 7477, Magmonix 6338 and Stalker 3781 are now real creatures in the world, clustered around the area each
    mission's own sources place them in, under **OD-48**: class, name and the mission's numbers are original, positions
    and health are analogues. Three givers came with them through the OD-45 pipeline (Professor Long, Dr. Robertson,
    Colonel Li Hua), and missions 955, 956 and 976 are live.
    *Note (2026-09-26, notes audit)*: the 1.6 live note "Killing any Bane Stalker on Mires will now give credit for
    'Restraining Order'" shows 976's Stalkers lived across the Mires, not by Li Hua, and the same notes increased the
    Lasher population; the clusters stand for map-wide populations they do not reproduce
    (`GAP-NOTES-976-STALKER-DISTRIBUTION`). No spawns are invented; 976/1 binds the creature, so every Mires Stalker counts.
    Placement respawn became a real mechanic for this: `respawn_ms` was a field the validator withheld as
    unimplemented, so a dead content creature whose placement asks for one is now queued on the channel and
    materialized again by the tick (**GAP-PLACEMENT-RESPAWN** covers what a live check should confirm).
  - **Giver naming fix (2026-09-16)**: the mission sources name givers without the world seed's rank prefix - "Lt.
    Saviours" for creature 120 "AFS Officer Lt. Saviours", "Council Elder Nula" for 93, "Elder Gadfly" for 113. The
    giver match now accepts a suffix match, which unblocked four conversation missions that every earlier batch had
    skipped for a *naming* reason rather than a missing one: 421, 422, 451 and 698.
  - **Collection objectives (2026-09-16)**: three more Wilderness missions went in by a new rule - 767 Mighty
    Miasma (6 Miasma Goo Samples), 771 Droning On (6 Shield Drone Parts), 787 Xanx For the Help (4 Xanx Pincers).
    The client carries each item as its own **physical entity class** ("Miasma Goo" 11162, "Xanx Pincers" 11160 -
    which is why the item name table has no match for mission items) and the objective's own words name the creature
    family the world seed carries (Bane Miasma 88, Bane Shield Drone 85, Bane Xanx 87). No source ties a mission item
    to an item template or loot table, so the objective is counted as that creature's deaths - a reconstruction,
    recorded as **OD-47** with the item class each counter stands for. Also fixed here: the generated rows classes
    did not remove their kill bindings on rollback, which the migration rollback test caught.
    *Superseded 2026-09-26:* 767 was withdrawn at D11 and is no longer offered (`WildernessMunsonWithdrawal`). 771 and
    787 now collect their item classes (Shield Drone Scraps 11153, not creature 24084, and Xanx Pincers 11160)
    through the 479 item mechanism (OD-66, `WildernessXenobiologySamples`).
  - **All 77 client maps are now decoded (2026-09-16)**: the archive.org client's `data/maps/*.map` (12.8 MB, 73 of
    77 decode with the existing tool) give every static entity with class and position for every zone. They do **not**
    carry NPCs (those are server-spawned) but they do carry the props and places missions name - which is how mission
    430's launchers were placed, and where "Walk of Giants", "Fort Defiance", "Fluxite Mines", "Minos Caverns"
    destinations come from. One map (`sanctusgrotto`) needs an older entity loader the decoder does not implement.
  - **Destroyable objectives (2026-09-16)**  - **Destroyable objectives (2026-09-16)**: the client's `.map` files carry every static entity with its class and
    position, and the objects the object-missions name are in them - `adv_foreas_concordia_wilderness.map` has six
    intact `ArchBaneGenObjMortarlauncherV01` (class 7478) and one destroyed. Mission **430 Mortar By Numbers** is the
    first mission rebuilt from that data: four destroyable placements at the map's own positions, each bound with the
    destroying-hit kind the camp's practice dummy uses. Only the hit points are a stand-in (OD-46). The map data does
    **not** carry NPCs - those are server-spawned - so TaRapedia's /loc stays the source for people.
  - **What still blocks the rest (2026-09-16)**: of 434 ready missions, 369 are blocked by an objective with no client
    conversation. Classifying the 1,004 such objectives by their own words: 377 are internal counter steps ("Inc 1 to
    2"), 251 collect items, 247 kill something, 42 place/use an object, 40 escort, 36 reach a place, 11 talk. The
    client marks the kill ones (592 objectives carry a counter label like "Thrax Soldiers Killed") - but the target
    creatures of most of them are in zones the world seed does not carry (it holds 155 creatures, all Wilderness), and
    the original server dump's `creature_type` has only 21 rows, so there is no surviving source for them. Collections
    need the item each counter counts, and the original loot table (`creature_type_loot`, 7 rows) does not name them
    either. These stay explicit evidence gaps rather than guesses.
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
  - Remaining mechanism gaps after the damage correction include the unmeasured S5 player blast knockback,
    **GAP-S5-CORPSE** (a corpse object with a windup and loot), **GAP-S4-ALLY-ESCORT** (allies following the player,
    which the new walk action can be extended into), and **GAP-ITEM-REWARDS** (the item grant planner is wired into
    turn-in already; the reward items need the prefix-family resolution recorded in
    `research/20260915-aliadas-hub/work/item-name-structure.md`).
  - Ellatha's NPC list was harvested (`tools/ellatha_npc_index.py`, `work/ellatha-npc-index.json`): only **71 NPCs**
    (58 Wilderness), so it is a cross-check for the hub, not the roster expansion the 660 no-giver missions need.
- **Ready missions (2026-09-26, `EarlyReadyMissions`)**: four conversation missions whose giver and receiver already
  stand in the world, from the dossiers in `research/20260926-ready-missions/` (each field's tier and citations), each
  proposed row re-checked against this tree and the deployed world first. **1742 Report to Liaison Brice** (Standley
  134 -> Brice 199003, 6,000 XP / 600 credits), **441 In Short Supply** (Randolph 130 -> Duncan 125, 3,500 / 700, gated
  on 549), **434 Rendezvous At The LZ** (Witherspoon 101 -> Randolph 130, 3,000 / 600) and **408 Revealing Treeback
  Experimentation** (Jamison 199089 -> Jorai 510133, 10,000 / 2,000). Package 212 is bound to Randolph (inferred: only
  434 and 441 use it and both name her). The re-check found what the dossier had not: Randolph and Standley stood on
  the Redshirt class without the client's NPC augmentation, so the client could not talk to either; they take NPC
  swapset classes and outfits as analogues (OD-45), Randolph the female class because the client's own texts call
  her "she". The client also stores a completion row on each giver's package that only sends the player on (434 on
  Witherspoon's 208, 441 on Randolph's 212); loaded as-is, the giver would close the objective at once, so the
  loader keeps those two rows out of completion (`MissionRedirectConversations`) - mission 429's identical row on
  Rogers was left loaded and still is, pending an owner decision. Left out and recorded: the unseeded gates 432 and
  406, the pre-1.4 reward items, the carried field report / data pack, Jorai's marker position, the zone-band levels
  (GAP-READY-434-GATE, -408-GATE, -REWARD-ITEMS, -MISSION-ITEM, -JORAI-POSITION, -SPEAKER-CLASS,
  -REDIRECT-COMPLETION, GAP-MISSION-LEVEL). Details: `docs/evidence/early-ready-missions.json`.
- **Liaison Logos missions (2026-09-26, `LiaisonLogosMissions`)**: the eleven unseeded `Logos:` missions of the
  segment-3 audit, seeded the `SeedLogosMissions` way: one shrine objective bound by `LogosRecovered` to the world's
  `logos` row, which is the client's `logosstone` id for the word, the giver also the receiver, no reward item.
  TaRapedia and the shrines' maps give four givers where the audit listed one: Langerman 1633/1638/1639/1640,
  Standley 1634/1635, Noonan 1643/1644/1646/1647, Arizpe 1652. 1639/1640 are gated on 1069. Rewards keep only the
  TaRapedia amounts no other source contradicts. All are pre-1.4 readings carried into post-D11 formatting edits;
  Ellatha contradicts five credit amounts. Levels are the giver's (OD-100; Langerman's observed 15). Langerman
  needs `MissingMissionGivers` for a conversable class. Open: GAP-LIAISON-LOGOS-REWARD-ERA, -REWARDS-MISSING,
  -SPEAKER, -POWER-D11, -UNSEEDED (907/908/909/911/912/921 are seedable the same way) and GAP-LOGOS-SKIP-POWER.
  Details: `docs/evidence/liaison-logos-missions.json`.
- **Clone credits (2026-09-26, `CloneCreditNotTradable` and server rules)**: the Clone Credit item works. Its
  right-click `RequestUseCloneCredit` (706) spends one token from the backpack for one clone credit, answered with
  `CloneCredits` (client PM 955), per the client's `clonecredit.pyo` and TaRapedia's Clone Credit page. Tokens in the
  footlocker or clan lockbox are ignored (OD-115). The token is not tradable (observed). The selection screen already
  showed each pod's credits. The copy list was reconciled, not changed: completed missions carry, open ones do not,
  the clone arrives where its source stood, and a clone made past the gate earns no tier credit (now tested). The only
  sources besides the tier gate are unseeded missions: Wilderness Targets of Opportunity 1449, the Divide, Palisades
  and Plains ToOs and the hybrid missions. Their rewards go in with them (OD-116). Open:
  `GAP-WILDERNESS-TOO-CLONE-CREDIT`, `GAP-CLONE-TOKEN-LATER-SOURCES`, `-LOCKBOX-USE`, `-FLAGS`,
  `GAP-CLONE-TOO-AUTOCOMPLETE`, `GAP-CLONE-SOCIAL-STATE`, `GAP-CLONE-CREDIT-VENDOR`. Details: `docs/retail-accuracy.md`,
  "2026-09-26 — Clone credits".
- **Manifest coverage of the 2026-09-22..24 W3 migrations (audit 2026-09-26)**: the manifest has no rows or
  `changes` entries for `WildernessHubFormingAlliances`, `WildernessHubReceptiveGate`, `WildernessHubReceptiveLevel`,
  `WildernessHubConscientiousGate`, `WildernessHubConscientiousBranches`, `MissionItemDropChance`, `MoawiDialogueClass`,
  `MissionSpeakerDialogueClasses` or `SolisCavernsPlacement`; their evidence lives in their Rows files and
  `docs/evidence/` JSON. Recorded as a due-diligence gap, not fixed. Their labelled estimates await owner review as
  OD-61 (Forming Alliances' rewards, including the vest 13738 V04-vs-wiki naming conflict), OD-63 (Moawi's dialogue
  class) and OD-64 (Solis's cavern placement).
- **Order**: implement the chain in play order (1069 → 479 → 1390 → 1392/1393, then the parallel missions), each with
  the W1/W2 discipline: frozen rows + paired migrations, a manifest slice `W3`, and content-loading/scenario tests.
- **World-data defects (2026-09-26, `WorldDefectsFix`)**: two audits of the deployed world corrected. Field Lt. Bagby and
  Lt. Galloway, receivers of the seeded 1788 and 1789, stood 1.3–1.6 km from the Treeback Camp entrance on the Palisades
  overworld; their TaRapedia readings are Treeback Camp coordinates (infobox `Instance=Treeback Camp`, 0.12/0.02 m off the
  instance floor), and they now stand in context 1397, which is shared and linked to the Palisades by map links 21/35.
  Upstream's hostile level-28 "Warnet Queen - Palisades" pool, which stood on the Divide's Foreas Base label beside the
  receiver of seeded 1743, is removed and left out (`GAP-PALISADES-WARNET-QUEEN`). Valerie Corman, Ranger Jorai and
  Lieutenant Epp move from upstream's map markers to their dated readings with the navmesh floor for y; the duplicate
  Colonel Whitaker pool stops spawning. Kearney and Mela keep marker positions as labelled analogues (OD-59); Clark,
  Norton and seven more Torden NPCs stay on markers until a post-D11 reading exists (`GAP-CLARK-POSITION`,
  `GAP-NORTON-POSITION`, `GAP-TORDEN-MARKER-POSITIONS`). Clark is probably unreachable where he stands, which blocks
  seeding 936 until the owner decides. Details: `docs/fidelity-audit.md`, "World-data defects (2026-09-26)".

- **Client-contract defects (2026-09-26, `LocalTeleporterGraveyards` and server rules)**: four segment-3 travel and
  economy systems now follow the 1.16.5.0 client. The 42 local teleporter pads can be gained and used (LOCALWAYPOINT 1,
  its own gain line and window); 595/597, which are hospital points, are re-typed. The dropship window lists only
  gained pads (help text 5697: a pad is gained by walking across it), each drawn at its own position instead of a
  boot-camp literal. The Palisades control-point hospital is Fort Dew's (221/226), so it no longer shares the
  Wilderness LZ's waypoint 216. Divide's Foreas Base, Palisades' Cumbria and Devil's Den hospitals are offered on
  inferred joins, which gives 105 hospitals on 42 maps. Items carry their sell price as the client's buyback price, and
  repairs charge the client's `_GetRepairPrice`. Open: the local pads' emulator ids have no client names
  (`GAP-LOCAL-TELEPORTER-IDS`, OD-90), plus `GAP-LOCAL-TELEPORTER-RADIUS`, `-TYPES`, `GAP-DROPSHIP-HOVER`,
  `-GAIN-MESSAGE`, `GAP-HOSPITAL-SHARED-WAYPOINT` and `GAP-WILDERNESS-LZ-HOSPITAL-MARKER`, and Hightower and Viands
  Village hospitals remain unresolved. Details: `docs/retail-accuracy.md`, "2026-09-26 — Client-contract defects".

- **Mission reward items (2026-09-26, `MissionRewardItems`)**: seven seeded missions now offer their reward items:
  1541 and 1040 on the Plateau, 1673 in the Marshes, 983 in the Mires, and the consumable bundles of 970, 1068 and
  1863. Each is one choice at `inferred` tier from TaRapedia's post-1.4 list. The templates come from the client's
  consecutive reward runs or from single-template classes (research/20260926-reward-items, independently reviewed).
  983 is medium confidence; the rest are high for item identity. Open: `GAP-MISSION-REWARD-TEMPLATE-ID`, `-CHOICE`,
  `-MODULES` (manufacturer prefixes not seeded) and `-FINAL-STATE` (the 2008-04-28 requirements note).
  `GAP-MISSION-LEVEL` now records that these missions' seeded levels (20-25) sit below their reward levels (26-39);
  the levels were not changed. Held: the 23 Logos missions, 1407 (recipes), 411/412/1390, pre-1.4-only lists and
  2-entry runs (`GAP-MISSION-REWARD-ITEMS`). 479's vest is recorded as contradicting the wiki's v6 and left for its
  own change.
- **Official live notes checked (2026-09-26, `MissionSharedKillCredit`, `OfficialNotesCorrections`)**: every live note
  from 1.4 (2008-01-29) to D16.5 (2009-02-17) was read against the 114 seeded missions
  (`research/20260926-notes-audit`). Two seeded values contradicted a note and are corrected at `original` tier:
  **2016 A Mystery Unearthed** takes the D14 notes' level 50 (it had the Plateau band, 20) and is labelled as given
  at Twin Pillars in the Wilderness; **682 Childhood's End** credits the Xanx kill (objective 3) to every character on
  the channel with the objective active, whoever kills it, as the 1.6 and D8 notes say. That is a new per-binding
  flag, `npc_mission_objective_binding.shared_kill_credit`, set only on 682/3 because both notes name that mission
  alone; every other kill binding keeps killer-only credit on shared maps. **976 Restraining Order** needed no data
  change: its binding names the Stalker creature, so every Stalker on the Mires counts, as the 1.6 note requires,
  but the note shows the Stalkers lived across the map, which the OD-48 cluster does not reproduce
  (`GAP-NOTES-976-STALKER-DISTRIBUTION`). Recorded without a change, because no evidence postdates the change the
  note describes: the D12 move of the Cumbria Research Facility NPCs into New Cumbria (1745's Arizpe, 243 m from
  the New Cumbria waypoint, and 408's Jamison, 189 m; `GAP-NOTES-CRF-RELOCATION`), the 1.7 move of Info Specialist
  Johnson beside Lt. Perkins (both at undated Ellatha readings 308 m apart; the note moves Johnson, not Perkins;
  `GAP-NOTES-321-JOHNSON-PERKINS`), Elder Quillas' 1.7 move up the hillside (pool 192 matches a 2007-09-26 reading;
  `GAP-NOTES-1390-QUILLAS-POSITION`), 1125's single keycard at accept (class 24724 / template 50310 is original, but no
  accept-time grant exists; `GAP-NOTES-1125-KEYCARD`), the 983/1041 alternative courses (the notes do not say what
  closes the other course; `GAP-NOTES-983-1041-ALTERNATIVE-COURSES`) and the shared credit's reach
  (`GAP-NOTES-682-SHARED-CREDIT-REACH`). The never-seed list and the missing final-era missions are under
  "Final-state mission roster from the official notes" below. Details: `docs/retail-accuracy.md`, "2026-09-26 —
  Official live notes checked against the seeded missions".

- **Doctors' sample missions and the D11 withdrawals (2026-09-26, `WildernessMunsonWithdrawal`,
  `WildernessXenobiologySamples`)**: the official D11 live notes (2008-08-15) withdraw Dr. Munson's three sample
  missions and disable Predatory. **751, 780, 767 and 769 are intentionally never offered in the final state**. 767,
  seeded 2026-09-16 under OD-47, is removed, and its client objective row goes back to the skeleton. The in-progress
  clause is `GAP-D11-WITHDRAWN-IN-PROGRESS`. The collection objectives now use 479's item mechanism (OD-66):
  - 758 Fithikally Challenged (Soji, after 771) and 776 Soldier's Blood (Ojy) are seeded.
  - 771 Droning On and 787 Xanx For the Help collect their client items instead of counting kills.
  - 787 now requires 758, as its own client opening presupposes.
  - 771 pays Ellatha's 600 credits, the later of two records.
  - Drops: 758 and 787 drop on every kill (inferred from walkthrough kill counts equal to the targets). 776 and 771
    use 479's 50% as analogues under OD-61, now in the manifest (`GAP-COLLECTION-DROP-CHANCE`).
  - Still open: the Ranja Cavern Fithik the seed lacks (`GAP-758-FITHIK-GEOGRAPHY`), `GAP-776-BLOOD-SOURCES`,
    `GAP-DOCTOR-CREDITS-CONFLICT`, `GAP-DOCTOR-CHAIN-GATES` (771's gate on the held 795) and
    `GAP-COLLECTION-TEMPLATE-CHOICE`.
  - Held: 795, 506, 433, 489, 665, 860 and 1449.
  - Details: `docs/retail-accuracy.md`, "Munson's missions withdrawn; the doctors' sample missions collect their items".

- **Class trainers (2026-09-26, `SingleClassTrainers`)**: the class choice at 5 and 15 now has the D12 shape. The 38
  per-class trainers Add_class_trainers had seeded (six round Kincaid, none able to talk or train) are retired. Training
  Officer Stratton (client name 10606, TaRapedia 2008-11-04) stands on the client's "Class Trainer: Daghda's Urn" marker
  and trains exactly as Kincaid does. Twin Pillars, Foreas Base and New Cumbria have client trainer markers but no named
  trainer in any source (`GAP-HUB-TRAINER-IDENTITY-*`), so a character at a gate trains at Alia Das or Daghda's Urn. The
  lost 2026-09-14 trainer specification is re-derived from the client as `docs/evidence/class-trainer-evidence.json`
  (OD-95).
- **Economy provenance (2026-09-26)**: the loot rows and item prices that came from InfiniteRasa's emulator dump are
  now labelled analogues, not original (OD-96). The source is kind `emulator_db`, and `GAP-W1/W2-ITEM-PRICES` are
  reopened. Alia Das' unnamed weapons vendor (pool 36) stays, because it stands on the client's "Weapons Vendor: Alia
  Das" marker (OD-97, `GAP-ALIA-DAS-WEAPONS-VENDOR`). The other "Test Vendor" pools there have never spawned.
- **Kill experience modifiers (2026-09-26, code only)**: the climb to 15 is mostly kill experience, and kills paid
  the same whatever the player's level and only to the killer. Now:
  - Squadmates on the channel within 100 m of the creature share every kill. Each gets `100 - 8(n-1)`% of the solo
    experience, shown as "[split Base XP] (+84% Group Bonus)" for two. The share is the client's own XP-bar
    arithmetic (`experiencebarwindow`, `XP_MOD_PER_PARTY_MEMBER 0.08`); the 100 m range
    (`MIN_DISTANCE_FOR_KILL_CREDIT`) is inferred from its name (OD-106).
  - A player more than 5 levels above the creature loses 20% per level: 80% at 6 above, 20% at 9, nothing from 10
    (TaRapedia 2008-10-06). The constants are original; the ramp between them is inferred (OD-105,
    `GAP-XP-DANGER-SHAPE`). Every recorded final-week kill is at most one level above, so the fit is untouched.
  - A crit kill pays the observed "by Crit Killing" chunk before the plain kill (B1-028), but nothing raises one
    until the crit-death finisher exists (OD-107, `GAP-CRIT-DEATH-FINISH`).
  - B3-060 is no longer read as level-difference evidence: its 40 credits belong to the previous kill. It and the
    half/quarter lines of B1 fit the client's damage-ranked `KC_*_PLACE_MOD` (1.0/0.5/0.25), which stays unbuilt
    (`GAP-XP-PARTIAL`).
  - Also open: `GAP-XP-SQUAD-RANGE`, `-SQUAD-STREAK`, `-MODIFIER-ORDER`, `GAP-KILL-CREDITS-MODIFIERS`.
  - Details: `docs/retail-accuracy.md`, "Kill experience: squad share, danger penalty, crit kills";
    `docs/evidence/kill-rewards.json`.

- **Creature loot from the footage (2026-09-26, `CreatureLootFootage`)**: `docs/evidence/creature-loot-footage-ledger.json`
  counts every loot drop in the supplied footage against the credited kills around it (40 final-week drops, 32 Pravus
  squad-loot entries). The boot-camp Thrax Infantry Initiates and the Wilderness Thrax stand-in (creature 3) drop Thrax
  Skull at 52.38% (22 in 42 kills) and one of the five standard-grade ammunition types at 1.9% each (4 in 42 kills, with
  the stack range observed for each type). The ammunition does not follow the killer's weapon: 5 of 7 attributable drops
  are a type that weapon cannot fire (OD-111). The Young Forest Boargar drops Boargar Ear (2 in 2, OD-112). The
  emulator's contradicted 12% 1-35 cartridge row goes. Every other creature keeps the three-cartridge stand-in, now an
  analogue, pending the owner's choice between keeping it and dropping nothing (OD-110).

## Segment 7 (shutdown live state) status

Segment 7 is not yet the current segment. This note records one shared piece, built on 2026-09-26 while its evidence
was fresh: the shutdown broadcast. Two recordings of the EU server's last minute, fxAtDpxypSw and CommanderGrog's
_gwh1__XecI, show the sequence. The final client's own paths reproduce it (`docs/evidence/shutdown-broadcast.json`;
account in `docs/retail-accuracy.md`, 2026-09-26 "The shutdown broadcast").

- **Built:** admin messages through the client's `Recv_AdminMessage`. The client supplies the "ADMIN MESSAGE: "
  header; the server sends the text with filter SYSTEM_GM. Console `announce` / `announcemap`, chat `.announce` /
  `.announcemap` at GameMaster (OD-120, OD-124). The operator's countdown, console `shutdown start`: "Server Shutting
  Down in 10...", then 9 to 1 at the measured final-night cadence (OD-123), or a uniform one; then every connection
  closed, which the client shows as its "You have been disconnected from the server" dialog; then the process
  stops unless `stay` is given (OD-121, OD-122). It never starts by itself.
- **Open:** the Neph broadcast before the countdown (`GAP-SHUTDOWN-NEPH-BROADCAST`), the admin action inside Grog's
  edit cut (`GAP-SHUTDOWN-GROG-CUT-ADMIN-ACTION`), the zone-loss alert's trigger (only "ALERT: PLATEAU IS LOST!" is
  observed; no rule is built, `GAP-SHUTDOWN-ZONE-LOSS-RULE`), how live issued the countdown
  (`GAP-SHUTDOWN-CADENCE-ORIGIN`) and with what tool (`GAP-SHUTDOWN-ADMIN-TOOL`), and the Earth Last Stand instance
  content (context 2375 exists in the client; spawns, scripting, route and rewards do not, `GAP-SHUTDOWN-LAST-STAND`).
  The final event itself is not built: footage from both viewpoints shows the event state differed by zone and
  server, so no "all bases lost" final state is seeded.

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

### Final-state mission roster from the official notes

The notes audit of 2026-09-26 (`research/20260926-notes-audit/findings.json`, every live note from 1.4 to D16.5, PTS
notes kept apart) sorts the client's missions by what the live notes say about them at shutdown. **The final client
(1.16.5.0) still carries withdrawn and PTS-only missions, so presence in the client, or a "ready" verdict in the
mission catalog, is not evidence that a mission was offered at shutdown.** Filter every catalog verdict through this
list before seeding.

**Never seed** (withdrawn by a live note, or never live):

| Missions | Why | Note |
| --- | --- | --- |
| 751, 780 (767, the third, is seeded; its withdrawal is the collection-missions batch's) | Dr. Munson's sample missions "are no longer available" | D11 live, 2008-08-15 |
| 769 Predatory | "permanently disabled"; the Predators were removed from the map | D11 live |
| 691 Body Count | "permanently disabled"; its objectives moved into 692 What Evil Lurks | D11 live |
| Soyuz Salvage 1999, 2006, 2007, 2008, 2009 | added D11; "the missions to retrieve those pieces have been removed" | D12 live, 2008-09-18 |
| Welcome Tour | removed; "The Reformists" and "Crash Course" added at New Cumbria instead (absent from the final client) | D12 live |
| Artificial Iniquity | "this mission and the associated encounter inside Raksha Robotics Factory have been removed" (absent from the final client) | D14 live, 2008-11-11 |
| 2012 The Epic Gauntlet 1 (Rogers, Alia Das) | "PTS ONLY" section; no live note | D12.5 notes |
| 1985 "Public Test Level Gate" | the D8.3 "NPC level gates were added to the Public Test server" | D8 notes |

Also PTS only: the D15 1-credit Mimeomech vendor in Alia Das. 714 Clean Slate is superseded by 1983 (reworked as a
spawned escort, D9.6), subject to client verification.

**Evidence gap, not seeded:** 1998 Time Capsule (level 12, Foreas Base, added D11). D12 removed only the Soyuz
missions and no later note says whether Time Capsule outlived the event (`GAP-NOTES-1998-TIME-CAPSULE`).

**Missing final-era content** (live at shutdown per the notes, not yet seeded):

- Palisades: 337 The Reformists and 2014 Crash Course at New Cumbria (D12; the catalog wrongly files 337 under Pools);
  1988 A Spiritual Pilgrimage from Warden Brocail, the post-D10 Eloh Temples gate; flashpoint 1952 Inspection at
  Skive Base (D8); the post-D12 Palisades ToO (1630/1809: New Cumbria waypoint, an extra boss, no Stalker objective).
- Wilderness: 323 Pirate Radio (ungated since 1.7); 701 Orders From High Command (fixed D12.4); 1449 Wilderness ToO
  (D13.6); the Caves of Donn D11 rework (693 from the new Elder Nekala inside the instance, the new 2004 Bugged to
  Death); 506 Mama Miasma with 3 bosses (D11); the pre-order Companion Delivery radio mission at Alia Das (1.4, an
  entitlement gap; which of 1475/1478/1479/1769 is not stated); the Empire Sector missions 2027/2024 after 2016 (D15).
- Plains: 2015 Base Invaders (D13); D13 also rebuilt parts of Torden Plains, so the seeded Plains NPC readings need
  a pre-D13 check. Instance: 1939 Bug Hugger in Kardash Atta Colony (1.6).
- Pools instance: 1914 and 1778 in Live Target Pens (1.6; the audit files them with the Plains findings, but the Live
  Target Pens map, 1763, is a Valverde Pools instance).
- Plateau: 1825 All Along The Watchtowers (1.4); the Velon Hollow set 1981 and 1975-1979 from New Velon Village and
  Wedge Rock Outpost (D9.6/D10).
- Incline: 2028 War Machine, level 27 (D14); flashpoint 1911 Predator Hunt (1.6, reworked D8).
- Mires: the ToO (1585) kills Defiler Jemmert instead of Deddarlink (D13.8). Marshes: 1.4 added 16 missions, only
  some named.
- Final seasonal event: the D15 holiday gear at every zone hub (Twin Pillars Outpost, Foreas Base, New Cumbria,
  Irendas Colony, Plains Post, Baylor Base, Fort Defiance, Snake Pit, Paludos); its end date is not in the notes.

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
| OD-59 Mission NPCs with no positional source (2026-09-26, agent, pending owner review) | Upstream's map-marker spawn position stays, on the navmesh floor, labelled analogue with the marker as counterpart and a named gap; applied to Field Ranger Kearney (510118) and Warrior Mela (510117). A dated reading replaces it |

| OD-61 Forming Alliances reward estimates (2026-09-24, agent, pending owner review) | Mission 479's reward is seeded as a labelled estimate, not recovered final-live data: 50% drop chance, heart template 2285, reward level 5, and vest counterpart 13738 — recorded in `docs/evidence/wilderness-forming-alliances-research.json`'s `implementation_gaps`/`reward_vest_gap`. The vest's own naming conflicts with the separate 2026-09-26 reward-items research (`research/20260926-reward-items/README.md`), which reads the wiki as v6 while the seeded template is V04; the two research passes were not reconciled before this seed shipped. Extended 2026-09-26 by the batch lead: the 50% estimate is also the analogue drop chance of 776 Soldier's Blood and 771 Droning On, which have no rate evidence (`WildernessXenobiologySamples`); OD-61 is now in the manifest's `owner_decisions` so those analogues can cite it |
| OD-58 (manifest: Practice Dummy single-hit behavior) Practice dummy hit points (2026-09-24, agent, pending owner review) | `BootcampPracticeDummyHealthRows` sets `content_placement` 198652's `hit_points` to 1, inferred from five single-hit destructions timed in 7Lrst9SG3pk (347.133-361.200 s) against one observed 84-damage hit; the comment states plainly that "low health and a server script forcing destruction remain indistinguishable" and that this is not recovered original HP. The unfilmed Lightning target keeps its prior (pre-existing) estimate, untouched by this change |
| OD-62 Boot-camp companion (Initiate) stats (2026-09-24, agent, pending owner review) | The camp's three named companion Initiates (Forean Gunner 198516, Archer 198517, Shaman 198518) have estimated position, health and attack: `BootcampCampGunnerCompanionRows` calls its own position/health/attack estimates out in-comment ("Individual position, health and attack are estimates"), and `BootcampCampArcherShamanCompanionsRows` borrows the Shaman staff's range/cooldown/damage from "the closest same-rank Forean companion action, not retail data." Equipment classes (Bow 10529, Staff 10533, GooGun 6238) are analogue assignments to the closest surviving original-client NPC weapon rows. Full field-by-field detail in `docs/evidence/bootcamp-camp-gunner-companion.json` and `docs/evidence/bootcamp-camp-archer-shaman-companions.json` |
| OD-57 (manifest: McAllister walking pace) McAllister's walk speed (2026-09-22, agent, pending owner review) | `BootcampMcAllisterWalkRows` sets creature 198500's `walk_speed` to 2.5 m/s as an explicit analogue — the animation reference walk rate of his reconstructed human class, not a speed measured from the lost original spawn; no footage frame-times his walk over a known distance. Detail and the related retrigger-condition change (1990 turn-in to 1992 accepted) in `docs/bootcamp-opening-movement-audit.md` |
| OD-63 Moawi's dialogue class (2026-09-24, agent, pending owner review) | `MoawiDialogueClassRows` reassigns creature 38 (Council Elder Moawi) from world-seed class Redshirt_Forean_Elder (6163, augmentation 1,59: creature/harvestable, unable to converse) to original client class 28415, chosen only because it shares the Forean elder mesh, class flags and NPC augmentation 52 needed to hold dialogue package 113. The migration's own comment calls this "an explicit class analogue; Moawi's final-live entity class has not been recovered" |
| OD-64 Solis identity and placement (2026-09-24, agent, pending owner review) | `SolisCavernsPlacementRows` moves named spawnpool 184 (Council Elder Solis) onto disabled pool 92's original X/Z/rotation and a probed navmesh floor (786.8711, 287.32, 581.46875, rotation 3.0), because pool 184's dated position is obstructed by Moawi's hut geometry in the compatibility client and pool 92 already places an unnamed Forean shaman 2.2 m from the dated report at the same elevation. The migration's own comment calls this binding "inferred... not a recovered final-live server placement." Full provenance and remaining uncertainty in `docs/evidence/solis-caverns-placement.json` |
| OD-65 Account-authentication 20-second wait (2026-09-22, agent, pending owner review) | `Auth.Client` defers only its first empty `ServerListExt` response for up to 20 seconds (`InitialServerListWaitMs`) so the existing game-registration broadcast can satisfy it, instead of showing a persistent "No servers found" dialog; later refreshes are immediate. `docs/evidence/live-auth-server-list-wait-20260923.json` states plainly this is "an emulator availability fix, not a proven final-live auth timeout," not a recovered original duration |
| OD-66 Collection objectives once the client item is identified (2026-09-26, batch lead, pending owner review) | Where the client mission-item class and template are identified (the MisXeno block, matched to the objective by name) and the server's item-drop mechanism can carry it, a collection objective is bound as an item collection with a creature drop, as 479 is, instead of OD-47's kill count. The drop chance is 100 (inferred) where a dated walkthrough counts kills equal to the client target, otherwise OD-61's 50% analogue. Applied to 758, 776, 771 and 787 (`WildernessXenobiologySamples`); OD-47 stays for objectives whose item is not identified |

| OD-67 Col. Almos and 551 (2026-09-26, open) | Almos's only reading (Ten Ton Hammer, 2007-12-02: -290.0, 178.0, -543.6) is 3.16 m above the only floor under it and never within 0.3 m of a floor inside its 8 m uncertainty, while its y/z equal the client's "Viands Village" label. Default: hold Almos and 551 (GAP-ALMOS-HEIGHT). Alternatives for the owner: stand him on the floor under the reading's x,z (174.567), or at the label point whose floor matches the reading's height; either would be an estimate, not a reading |

| OD-90 Local teleporter pads the final client cannot name (2026-09-26, agent, open, pending owner review) | The 37 seed local pads (ids 537-574) are gained and used under their emulator ids, and the client shows its missing-translation text for their names. The alternative is to leave them unusable until original local-waypoint ids are found. The client's `waypointlanguage` holds no local-teleporter ids apart from magma caverns' 488-490, which the seed does not place (`GAP-LOCAL-TELEPORTER-IDS`) |

| OD-95 Class trainers after D12 (2026-09-26, agent, pending owner review) | The 38 pre-D12 per-class trainer pools (501001-501038) stop drawing. A hub's single trainer is placed only where a post-D12 source names him, on the client's own TRAINER marker: Training Officer Stratton at Daghda's Urn (TaRapedia rev 35503, client name 10606). His body (Kincaid's), level 8, 1000 hp and facing 0 are analogues with Kincaid as counterpart, and he is recognised by creature id because his package is unrecovered. Twin Pillars, Foreas Base and New Cumbria stay empty with named gaps; the client's unassigned "Training Officer" names are not used |
| OD-96 Economy data of emulator lineage (2026-09-26, agent, pending owner review) | `gameserver_dev_Full.sql` is InfiniteRasa's emulator dump (source kind `emulator_db`). The seven loot rows (creature_loot 1-21), every `Regenerate_item_template` price and the vendors' stock stay, because loot and vendors need values and no original survives, but they are analogues, not original. Open for the owner: whether the emulator-authored loot rows, which are optional content, should be removed instead |
| OD-97 The Alia Das "Test Vendor" NPCs (2026-09-26, agent, pending owner review) | Pools 21-29 have drawn nothing since the 2023 seed and stay as they are. Pool 36 ("Test Vendor 5", weapons package 10) stands 0.15 m from the client's "Weapons Vendor: Alia Das" marker and is kept as Alia Das' weapons vendor; its name, body and stock are a gap. The hospital pools 31-35 are left to the hospitals batch |
| OD-105 Kill-experience danger penalty (2026-09-26, agent, pending owner review) | Applied, labelled inferred: full to 5 levels above the creature, 20% less per level to 20% at 9 above, nothing from 10 above. The constants (`DANGER_PENALTY_VALUE 0.2`, `_LEVELDIFF_MIN 5`, `_MAX 9`) are original and TaRapedia (2008-10-06) states the zero; only the linear ramp between them is reconstructed (`GAP-XP-DANGER-SHAPE`). The alternative was full experience at any level, which the live game did not give |
| OD-106 Who shares a squad kill (2026-09-26, agent, pending owner review) | The killer's squadmates on the channel within 100 m (`MIN_DISTANCE_FOR_KILL_CREDIT`) of the creature, at the client's XP-bar share; the split counts every member in the world, as the client's tooltip does. Squadmates are paid without a streak (`GAP-XP-SQUAD-RANGE`, `GAP-XP-SQUAD-STREAK`) |
| OD-107 Crit kills and partial credit (2026-09-26, agent, pending owner review) | The crit-kill reward is modelled from B1-028 but has no trigger until the crit-death finisher is built (`GAP-CRIT-DEATH-FINISH`). Damage-ranked partial credit is left out although the client's `KC_*_PLACE_MOD` fit four footage lines, because the ranking is not evidenced (`GAP-XP-PARTIAL`) |

| OD-115 Where a Clone Credit token can be used (2026-09-26, agent, pending owner review) | Only from the using character's own backpack, where TaRapedia locates it. The client's footlocker and clan-lockbox windows can send the same request; it is ignored without a reply until a source shows the live server honoured it (`GAP-CLONE-TOKEN-LOCKBOX-USE`). Alternative for the owner: accept the account footlocker as well |
| OD-116 Clone Credit rewards of unseeded missions (2026-09-26, agent, pending owner review) | No reward row ahead of its mission: the Targets of Opportunity and hybrid missions that paid a Clone Credit are unseeded, and a reward row for an undefined mission is dangling content. Each reward's evidence is kept in `docs/evidence/class-trainer-evidence.json` (`clone_credit_sources`) for when its mission is seeded |

| OD-110 The stand-in drop (2026-09-26, agent, pending owner review) | A creature without loot rows keeps InfiniteRasa's stand-in, three Standard Grade Cartridges on a coin flip, labelled an analogue (`CreatureLoot.StandInDrop`). The alternative, `StandInDropEnabled = false`, means those creatures drop nothing but mission items: no corpse income and no ammunition off a corpse outside the Thrax infantry and the Young Forest Boargar. Owner to choose |
| OD-111 The creature ammunition drop (2026-09-26, agent, pending owner review) | Weapon-matched ammunition is refuted by the footage ledger (5 of 7 attributable drops mismatch). One row per standard-grade ammunition type at an even share (1.9%) of the measured 4-in-42 Initiate rate, with each type's observed stack range; the emulator's 12% 1-35 cartridge row is removed |
| OD-112 Loot on thinly evidenced creatures (2026-09-26, agent, pending owner review) | Creature 3, the world's Wilderness Thrax stand-in, takes the Initiate's rows, because the Thrax Infantry Trainee on camera is not seeded. The Young Forest Boargar takes Boargar Ear at the measured 2 in 2 (stack 1-2) despite the sample size |
| OD-120 Who may send an admin message (2026-09-26, agent, pending owner review) | The operator from the console (`announce`, `announcemap`) and GameMaster accounts in game (`.announce`, `.announcemap`): a message hands nothing out and the client names its header for GMs. The final client has no sender of its own (`GAP-SHUTDOWN-ADMIN-TOOL`). Alternative for the owner: Admin only, or console only |
| OD-121 Where the shutdown countdown starts (2026-09-26, agent, pending owner review) | Console only (`shutdown start`); no chat command, timer, date or player-count trigger |
| OD-122 After the countdown (2026-09-26, agent, pending owner review) | The process stops as `exit` does, once every disconnected player has been removed and saved (30 s at most), matching the OFFLINE server list after the EU shutdown (fxAtDpxypSw t=176-185); `stay` keeps it running |
| OD-123 The countdown's default cadence (2026-09-26, agent, pending owner review) | The measured final night (lines at 0, 6, 10, 13, 16, 19, 23, 27, 31, 34 s; disconnect at 37.1 s), though it may have been typed by hand; `shutdown start <from> <seconds>` gives a uniform one |
| OD-124 Who hears an admin message (2026-09-26, agent, pending owner review) | Clients in a live map channel; a client at character selection or loading gets no line, and the countdown still disconnects it |
| OD-125 Who a squad instance belongs to (2026-09-27, agent, pending owner review) | The squad that created it, or the character when created outside a squad; later arrivals of that squad join it; a solo creator who forms a squad keeps the solo copy (the invitee gets the squad's copy and the leader joins it on re-entry), as the D10/D13 known issue describes. The documented behaviour, not a fix (GAP-SQUAD-INVITE-QUIRK-FINAL-STATE). |
| OD-126 How long an empty squad instance is kept (2026-09-27, agent, pending owner review) | 600 000 ms (measured upper bound from a player's report); a disbanded squad's copy goes at once; the boot camp keeps OD-2's immediate destruction. Official notes show re-entry before reset; the value is labelled measured (GAP-INSTANCE-RESET-TIMER). |
| OD-127 Numbered copies of shared maps (2026-09-27, agent, pending owner review) | Copies open only when every copy of a context is at a configured per-context capacity; none is configured, so every shared map stays one channel; with more than one copy, entering sends ChooseInstanceList (all copies with status) and waits for SelectInstance/SelectInstanceCancel; a full choice gets PM 934; empty copies are destroyed at once. The client contract is original, the capacity is not (GAP-SHARED-COPY-CAPACITY). |
| OD-128 The number a copy shows (2026-09-27, agent, pending owner review) | The lowest number no live copy of the context holds (a shared primary is 1), sent as Wonkavate's instanceId (shown 'Name(n)' on the loading screen and map window) and as ChooseInstanceList/waypoint ordinals; the boot camp keeps OD-2's monotonic id. Players named the shutdown copies 'Earth 1'-'Earth 5', which fits small per-context numbers; the reuse rule is inferred. |
| OD-129 Where leaving a squad instance goes (2026-09-27, agent, pending owner review) | The instance's own exit link to the map the player came from, else the spot they left it from, else (after a restart) its first exit link; 'Leave current adventure' is the only map-waypoint entry inside a squad instance (local teleporters unchanged); leaving, being kicked or the squad disbanding sends the player out with PM 1058. PM 1058/945/946 and Recv_EnteredWaypoint's abort row are original; the exact arrival point is inferred. |
| OD-130 Where the four instance doors without a client exit marker arrive, and where their exits stand (2026-09-27, agent, pending owner review) | Arrive on the instance's entrance hospital marker (Warnet Caverns' Entrance First Aid Station, Ustor Yard Field Medic, Sanctus Grotto's AFS Field Medic, The Refuge's Eloh Sanctuary); the exit trigger stands on the arrival point and returns to the parent's entrance marker, as for every door the preloader built. The trigger on the parent map is the client's own marker (original); the arrival is inferred from the guides and the world seed's hospital names, and The Refuge's is low confidence (no source describes its way in). Alternative for the owner: leave The Refuge closed until a source places its entrance. |
| OD-131 The way back from Edmund Range (2026-09-27, agent, pending owner review) | A 4 m link on the Staging Area label (the arrival point) back to the CELLAR's north-end marker, labelled analogue: 2374 has no client link marker, and without it a player who takes the CELLAR's north end cannot leave. Alternative for the owner: leave the CELLAR's north end unlinked until the Edmund Range PvP batch recovers the map's real exit (the D15.7 winners' victory area and losers' staging area). |
| OD-132 How the remaining instance hospital markers are joined to Hospital Selection ids (2026-09-27, agent, pending owner review) | Join a marker to a graveyard only where one graveyardlanguage entry names its place (inferred); take the waypoint from waypointlanguage by name, else the world seed's hospital row at the marker even when its id is one the client cannot name (as Tahrendra's 606); leave a marker open where two readings conflict (graveyard 38 between Timora Mines and Bane Fluxite Mines) or generic names cannot be told apart. 22 markers resolved, 21 left open with a reason each (docs/evidence/hospital-catalog.json). |
| OD-133 Retired and unshipped server rows (2026-09-27, agent, pending owner review) | Remove map_info 1991, 2233 and 1737 (test maps; no link in) and Edmund Range OLD 2361's six spawn pools; keep 2361's map_info so a character saved there still loads; keep the 2265/2373 map_marker rows, which are never served. Alternative for the owner: also remove 2361's map_info once the D16.3 relocation of characters saved there is implemented. |
| OD-134 Where the Last Stand's two exits arrive (2026-09-27, agent, pending owner review) | On the CELLAR's one link marker with no destination (134419591463417, the centre of the zone-pad ring); the way into 2375 stays unbuilt. The exits are the client's own markers; the arrival is inferred. Alternative for the owner: leave them out until the shutdown-event batch recovers the Last Stand's access route. |
| OD-135 Health, attacks and movement of the Pravus creatures and objects (2026-09-27, agent, pending owner review) | Each Pravus creature takes the hit points, first attack and run/walk rates of the world seed's counterpart of its kind (Machina 555 / action 18, Thrax soldiers 555 / 33 or 2, Technician 555 / 9, Shield Drone 750 / 42, Tarmok 1000 / 41 as Overseer Graal), and the Production Fueling Capsule 1000 and each Living Infestation 100 hit points; every value is recorded as an analogue (GAP-PRAVUS-CREATURE-STATS, GAP-PRAVUS-CAPSULE-HP, GAP-PRAVUS-INFESTATION-HP). Alternative for the owner: hold the population until a capture records them. |
| OD-136 How the Pravus groups are drawn and come back (2026-09-27, agent, pending owner review) | Spawn pools on the instance map at the region the minimap names, the fewest individuals the footage shows at once, the world seed's respawn 20. Five pools (Frontlines by Bane dropship, hill route, Interior Halls, Control Rooms, chamber escort), counts = min = max = the observed minimum, respown_time 20 as every pool the seed puts on an instance map. Each squad copy clones them with fresh counters. Alternative: never respawn inside a copy (a large respown_time). |
| OD-137 The Prototype Forean Machina production (2026-09-27, agent, pending owner review) | One creature placement at the machine that comes back after each death, at the kill cadence the footage shows. Placement 1430121 respawns 2500 ms after each death (the chamber kills arrive every 2-3 s from t 456 to 510 and production is still running when the video ends). It does not stop when the capsule is destroyed (GAP-PRAVUS-PRODUCTION-STOP). Alternatives for the owner: several simultaneous placements, a slower cadence, or holding the production until hGS00lxVgQg is read. |
| OD-138 Nylla's second stand on the Control Rooms gangplank (2026-09-27, agent, pending owner review) | Always present in the copy. A squad copy has no owner whose mission state a presence condition could test, so the dossier's condition (575/2 completed) would never let her appear; she stands there from entry. Alternative: hold the placement until squad-scoped presence exists (575 can still be turned in at her camp). |
| OD-139 593 The Escapist while 574 Machinations is held (2026-09-27, agent, pending owner review) | Offer 593 without its prerequisite. TTH 2007-10-04 (pre-1.4) says 593 requires 574, but 574 cannot be seeded (no Baruhi, no Wilderness Forean Machina), so the prerequisite row is left out and recorded (GAP-PRAVUS-593-PREREQUISITE); 575 keeps its prerequisite on 593. Alternative for the owner: enforce 574 and leave 593/575 unreachable until 574 exists. |
| OD-140 Mission level for The Dead Live (450) and Logos: Movement, Around, Chaos (960) (2026-09-27, agent, pending owner review) | The giver's level, the rule OD-100 applied to the Liaison Logos missions: 450 Dr. Franja Corman's 10, 960 Standley's 10. An analogue under GAP-MISSION-LEVEL; nothing on the server gates on the value. Alternative: the Crater Lake band minimum 7. |
| OD-141 Overseer Tyryd's health, attack, movement and facing (2026-09-27, agent, pending owner review) | 1000 hp (the world seed's level 6-10 Thrax Overseer bosses), attack 29 (the world seed's Bane_Thrax_Technician_Boss row 48), run 9 / walk 0 (the Thrax boss rows 82-84 and Tizzik Gi), facing 0. Every value is an analogue; name, class, level and faction are sourced. Alternative: hold Tyryd until 1056 is seeded, since his only recorded role is the supply pen key. |
| OD-142 Lt. Casper: from the shared spawnpool to a per-copy placement (2026-09-27, agent, pending owner review) | Retire spawnpool 520012 (counts 0/0, restored on rollback) and place creature 520012 at Ellatha's /loc as content placement 1721100 with creature AI, no respawn inside a copy, facing 0 (the pool's). The batch lead's instruction: objective-bound bosses are placements in their instance. The emulator's 500-unit pool respawn is not evidence and is not carried over. |
| OD-143 960's Around and Chaos shrines without the supply pen force field (2026-09-27, agent, pending owner review) | Seed 960 with all three shrines reachable; the pen force field (1056/4) is not modelled. (GAP-CLRF-PEN-FORCEFIELD). Alternative: hold 960 until 1056 and the force field are seeded. |
| OD-144 The Dead Live's prerequisite (2026-09-27, agent, pending owner review) | No prerequisite: TaRapedia records Requirement=None in all 17 revisions, and the starter Ten Ton Hammer names ('Hoping for the Best', 2007-10-01) is not a mission of the 1.16.5.0 client. |
| OD-145 Bodies, levels, health, attacks and movement of the Divide operations' new creatures (2026-09-27, agent, pending owner review) | Labelled world analogues (the OD-135/OD-141 convention). Morrow, Sanchez and Cisco wear Kearney's row (class 3846, 1000 hp, standing), Ferme Captain Velns' female body 3848, Horlo Elder Q'uoa's Forean class 7035; the Timora Wardmaster, Overseers and Warden the world seed's Thrax Overseer bosses (class 10504, 1000 hp, action 41, run 9 / walk 0) at the band maximum 16; Torqua the world seed's Bane Hunter Boss (2000 hp, action 35, run 9 / walk 0) at TaRapedia's level 19. NPC levels are the band minimum. Morrow, Sanchez and Cisco wear Kearney's shipped clothing set and Ferme Velns' (GAP-DIVIDE-NPC-APPEARANCE). Alternative: hold the bosses and 792 until a capture records them. |
| OD-146 1905 Central Bound Patrol while its escort party is unrecorded (2026-09-27, agent, pending owner review) | Offer 1905 with objective 1 optional and unrevealed. 1905/2 (report to Kearney) is required and revealed, 1905/1 (escort the soldiers) optional and never revealed, so the mission runs Kearney -> Sanchez along the escort's route without the soldiers (GAP-TIMORA-ESCORT-SOLDIERS). No reward rows (GAP-340-1905-REWARDS), so nothing is paid for the shortened mission. Alternative: hold 1905 until the soldiers are identified. |
| OD-147 392 Cave Extraction while 383 Data Thieves is not seeded (2026-09-27, agent, pending owner review) | Offer 392 without its prerequisite. The OD-139 rule: TTH makes 392 follow 383, but 383 (Karik/Zola's escort and the monitoring equipment) is not in this batch, so the prerequisite row is left out and recorded (GAP-DIVIDE-392-PREREQUISITE). Alternative: hold 392 until 383 is seeded. |
| OD-148 Minos Caverns navmesh voxel grid (2026-09-27, agent, pending owner review) | 0.2 x 0.1 m cells for this map (data/map_build_settings.csv), the default 0.4 x 0.2 m elsewhere. The finer grid samples the same client geometry and joins the cave where the client lets players walk; agent radius, height, climb and slope are unchanged, so no connection is added that the geometry does not have. Alternative: keep the default grid and hold the escort and every Minos creature AI. |
| OD-149 Timora Mines terrain cut (2026-09-27, agent, pending owner review) | Cut the terrain inside the one chunnel entrance at 242, 202, -181 only (mesh_prefix@x:z). The arch_bane_industrial_ family cut Pravus uses cuts this map's other chunnel entrance too and split Kearney's hall from the mine; scoped to the Fuel Egress placement it cuts 22 triangles and changes no other island. Alternative: leave the Fuel Egress block an island (the Warden could not be pathed to). |
| OD-150 Hamilton's stasis tube while 356 is held (2026-09-27, agent, pending owner review) | Place the tube as a destroyable with the Pravus capsule's analogue 1000 hit points. TTH puts Hamilton in 'a tube that you can destroy' in the Research Ward; the tube stands with him so the ward shows its original state, and a destroyable cannot exist without hit points (GAP-TORCASTRA-HAMILTON-RELEASE). Destroying it does nothing until 356 is seeded. Alternative: leave the tube out and Hamilton standing in the open. |
| OD-151 1860 Stolen History and 1861 Traitor on the Run (2026-09-27, agent, pending owner review) | Hold both. The batch rule: neither container (the Minos map chest, the Timora cipher crate) has an evidenced class, so 1860's objectives 1 and 2 are held, which leaves 1860 unfinishable; 1861 follows it and its objectives 2-5 are unrecorded. Their post-1.4 reward items stay unseeded. Alternative: an analogue container class (e.g. a Bane crate) for both, which would be an estimate standing in for original objects. |
| OD-152 The four 403 drills TTH locates (2026-09-27, agent, pending owner review) | Do not place them while 403 is held. A drill is only a bomb target for 403 (held), the detonator item and the fuse are unrecorded, and a plantable bomb with nothing to complete is an invented use; their positions stay in the dossier (GAP-DIVIDE-403-DRILLS). Alternative: place them as inert scenery once a non-usable static shape exists. |
| OD-153 The old research facility NPCs standing in the D12 ruins (Arizpe, Kaven Corman, Briggs, Hutchison, Aldrin), who have no post-D12 position source (2026-09-27, agent, pending owner review, dossier OD-P1) | Keep them where they are, labelled (no move to an analogue point); keep Arizpe; hold 337; stop Aldrin only if the evidence says he left. Arizpe, Kaven, Briggs and Hutchison stay on their pre-D12 readings with GAP-NEW-CUMBRIA-*; 1812 and 1813 are given by Arizpe there; 337 is held. Aldrin keeps spawning: the D12.5 note removed his Soyuz missions and relocated the facility's NPCs, and no source says he left (GAP-NEW-CUMBRIA-ALDRIN). Alternative: move each to a labelled analogue point inside New Cumbria. |
| OD-154 Skive Base object missions 1799, 1800 and 1801 while the base holds no Bane (2026-09-27, agent, pending owner review, dossier OD-P2) | Hold them. Held until Skive Base has defenders (GAP-SKIVE-BASE-POPULATION); the dossier's proposed rows (1244100-1244109, 1244800-1244803, 1244900) are not seeded. |
| OD-155 Mission level of 1812, 1813, 1988, 2014 and 1795 (2026-09-27, agent, pending owner review, dossier OD-P3) | The giver's level, the OD-100/OD-140 rule. An analogue under GAP-MISSION-LEVEL: Arizpe 25, Brocail 15, Matlin 30 and Mullen 20, each from the giver's TaRapedia page (Mullen's world row still carries the upstream 10, GAP-MISSION-LEVEL/GAP-MULLEN-LEVEL). 2014's rewards say 'min level 19'; the alternative is 19. Nothing on the server gates on the value. |
| OD-156 Combat stats for Executor Gantic (199054) and Barbrix (199095) (2026-09-27, agent, pending owner review, dossier OD-P4) | Left as they are (action1 0, 555 hp): the kill completes and the item drops without the boss fighting back, so no mission needs an attack; GAP-PALISADES-CREATURE-STATS records the unrecorded stats. Alternative: the OD-135 world-seed analogues (Thrax Overseer bosses, 1000 hp, action 41). |
| OD-157 Seeded 368 Searching for Acceptance, which skips its Warnet kill and time limit (2026-09-27, agent, pending owner review, dossier OD-P5) | Do not change it in this batch; record the defect. 368 stays as seeded; GAP-PALISADES-368-KILL-TIMER records the defect. Alternative: withdraw it from offer until Palisades Warnets exist. |
| OD-158 Package 134: move it to the Palisades Corporal Orton 199086 and retire the Valverde Pools duplicate 510196 (2026-09-27, agent, pending owner review, dossier OD-P6) | Verified and applied: the client binds 134 to 331/1 and 337/3 ("Speak with Corporal Orton") and its 331 log says "Corporal Orton from Cumbria Research"; 510196 stands on map 1304 at a y/z-swapped point and 199086 had no package. npc_package 199086 -> 134, the 510196 package row removed and its pool 0/0; Down restores both. |
| OD-159 Pre-1.4 experience and credit amounts of 1812, 1813 and 1795 (2026-09-27, agent, pending owner review, dossier OD-P7) | Seed them labelled with their era. Seeded at tier inferred, era pre-1.4, confidence low (GAP-REWARD-ERA); 1988's amounts are post-1.4 and 2014 has none (TaRapedia 0/0, GAP-2014-REWARD-ZERO). |
| OD-160 Respawn of the two named bosses whose drop completes 2014 and 1795 on the shared Palisades map (2026-09-27, agent, pending owner review) | Batch-agent choice (not one of the dossier's decisions): without a respawn the first kill leaves each boss defeated until a restart and every later player could accept but not finish. 60 s, recorded as an analogue on the change entries (GAP-PALISADES-NAMED-BOSS-RESPAWN). Alternative: hold 2014 and 1795 until a capture records the respawn. |
| OD-161 Ambient creature populations beyond the Wilderness (2026-09-27, owner) | The owner's decision in the session of 2026-09-27: put ambient creature/enemy populations into the zones beyond the Wilderness, retail-accurate; build each from original footage or screenshots (observed/measured) where it exists, then text (inferred), and only then labelled analogue stand-ins so the content functions. AGENTS.md applies in full. First batch: the Divide and Concordia Palisades (ConcordiaAmbientPopulations). |
| OD-162 Levels of the Divide and Palisades ambient creatures (2026-09-27, agent, pending owner review) | The floor of the zone's recorded mob band, one level per species: Divide 12 (TaRapedia 'Mob Levels=12-18'), Palisades 15 (the level of seeded 368). No target frame's level is legible (GAP-CONCORDIA-AMBIENT-LEVELS). Alternative: hold the populations until a legible frame. |
| OD-163 Health, attacks and movement of the new creatures (2026-09-27, agent, pending owner review) | The world seed's counterpart of the same class (the OD-135 convention), first attack only; the Class IV Stalker takes 199812's 600 hp and attack 2 (GAP-CONCORDIA-CREATURE-STATS). |
| OD-164 Pool sizes, places and respawn where footage and text give none (2026-09-27, agent, pending owner review) | Labelled analogues: 3 per single-species pool (4 for the Boargar herd, 2 Technicians), the Filchers at the trench waypoints and the Crossroads label, 368's Warnets on the first valley floor east of Hightower, respown_time 20 (OD-136); the second Stalker borrows the first's 15 minutes (GAP-CONCORDIA-POOL-COUNTS, -PLACES, -RESPAWN). Alternative: hold 358 and 368. |
| OD-165 371 Dissections: Part II while 370 is held (2026-09-27, agent, pending owner review) | Offer 371 without its prerequisite (the OD-139 rule): 370's giver Medic Markis is not in the world (GAP-DIVIDE-371-PREREQUISITE). Alternative: hold 371 and 372. |
| OD-166 774 The Tallest and its Class IV Stalkers, so that 755 Careless keeps its post-1.4 prerequisite (2026-09-27, agent, pending owner review) | Seed 774 with TaRapedia's two Stalkers (beneath the Foxtrot bridge every 15 minutes; near the Bane Forward Base); scraps drop on every kill (inferred, the 758/787 rule). Alternative: hold both. |
| OD-167 Mission levels of 371, 372, 774, 755, 342 and 1808 (2026-09-27, agent, pending owner review) | The zones' seeded missions' levels, Divide 10 and Palisades 15, instead of OD-155's giver level (Yorma Brown's 50 is itself an out-of-band analogue); 358 takes TaRapedia's 'Requirement=Level 12' (inferred). Alternative: the giver levels. |
| OD-168 368 Searching for Acceptance's kill (2026-09-27, agent, pending owner review) | Count five Palisades Warnet kills and withhold Kogari's objective-1 row (text 2741) in MissionRedirectConversations; the five-minute timer stays held, a timed failure needing a retry path (GAP-PALISADES-368-KILL-TIMER). Supersedes OD-157 for the kill. |
| OD-169 1808 Clear Your Uherum's traverse (2026-09-27, agent, pending owner review) | 'Traverse Uherum Pass' completes on reaching the east mouth (area 1244500 at the original entrance piece); the client's bodiless Start/Middle/End are optional and unrevealed (GAP-1808-TRAVERSE). Alternative: hold 1808. |
| OD-170 342 Noise Pollution's pre-D12 Boargar reading east of the fortification line (2026-09-27, agent, pending owner review) | Use it: TaRapedia rev 11642 (2007-10-23) puts the Boargar beside the Eloh obelisk the final map still carries, north of the New Cumbria perimeter and outside the destroyed facility; no D12 note names the area (GAP-PALISADES-342-OBELISK-ERA). Alternative: hold 342. |

Detailed evidence: [new-character initialization](new-character-client-evidence.md),
[starter equipment](starter-equipment-research.md),
[character progression](character-progression-client-evidence.md), and
[retail accuracy](retail-accuracy.md).
