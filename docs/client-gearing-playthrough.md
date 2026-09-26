# Original-client Gearing Up playthrough

Run 07 subsequently verified both tooltip corrections, retained mission progress,
completed the Lightning objective and logged out normally. Mission 1992 remains
active with the next Hartmann conversation pending. The sections below preserve
run 06's earlier boundary; the final follow-up section records run 07 separately.

On 2026-09-22, Alden Vanguard completed the equipment and Practice Dummy portions of mission 1992
against the isolated emulator using the recovered original client: **Delessio
talk → crate Loot All → first boots equip → remaining equipment → second
Delessio talk → Hartmann → Practice Dummy → Hartmann → Lightning objective**.
Ordinary logout preserved this checkpoint. **Mission 1992 remains incomplete;
Lightning objective 8 is pending.** This demonstrates client/server integration.
It does not certify original live behavior or close the remaining item-stat gaps.

Run `playthrough-06` resumed the character after the opening lesson. The operator
used normal movement and interactions throughout this run, without GM teleporting.
The approach reached Delessio's raised deck through the original map's gate and
ramp. Successful traversal validates accessibility in this emulator; the original
NPC route and pace remain reconstructed.

## Captures and persisted progression

Artifacts are under
`/home/blizz/backups/rasa-net/research/20260922-client-playthrough/playthrough-06/`.
[The evidence record](evidence/client-gearing-playthrough.json) hashes 28 immutable
artifacts: captures 25–35, 37–39, 42, 45–46 and 49; six database snapshots;
the final client/server logs; runtime binary identities; and terminal process
status. The harness stopped after character logout. Every owned process exited;
the client's terminal code -15 records harness termination after logout, rather
than a natural zero exit.

| Capture | Observed result | Persisted corroboration |
| --- | --- | --- |
| 25–26 | First Delessio dialogue gives the crate/backpack instructions; Continue reveals the crate objective. | Original client text 21484 is selected by `(1992,4,2560,1,1)`. |
| 28 | Supply Crate loot window opens; opening alone does not finish the objective. | `crate-open-state.json`: objective 4 completed, objective 1 incomplete, at 20:27:02 UTC. The snapshot names **28-crate-use.png**; capture 27's filename says “open” but shows no loot window. |
| 29–30 | Loot All delivers five items. The equip objective remains active while the starter outfit and pistol are equipped. | `crate-looted-state.json`: objective 1 completed, objective 2 incomplete, at 20:27:25 UTC; issued rifle instance 11 has 20 rounds. |
| 31 | Equipping only the issued boots completes equipping and reveals the second Delessio talk. | `first-boots-equipped-state.json`: objective 2 completed, objective 5 incomplete, at 20:28:19 UTC. Boots template 12209 occupies equipment slot 2; Recruit Boots 122854 returns to backpack slot 1. The other four issued pieces remain in the backpack. |
| 32–33 | Rifle tooltip has 20/20 rounds. Remaining armor and rifle equip successfully; HUD reads 20/1000. | Final equipment snapshot places rifle 13713 in drawer slot 0, armor 12209/15803/26879/12208 in slots 2/3/16/15. Starter clothing and pistol remain owned; reserve cartridges remain 1000. |
| 34–35 | Second Delessio dialogue instructs visiting Hartmann; Continue reveals that objective. | Text 21487 matches `(1992,5,2560,1,1)`. `equipment-lesson-state.json`: objectives 4/1/2/5 completed, objective 6 incomplete, at 20:30:32 UTC. |
| 38–39 | Hartmann's first dialogue presents the shooting instructions; Continue reveals Shoot the Practice Dummy. | Original text 21491 maps to `(1992,6,2563,1,1)`. |
| 42 | Practice completion banner and return-to-Hartmann tracker appear. Red damage reads -106, HUD reads 18/1000, and the restored target is visible. | The subsequent `practice-completed-state.json` has objective 3 completed, objective 9 incomplete and rifle ammunition 17. This snapshot follows the screenshot; they are not simultaneous. |
| 45–46 | Hartmann's return dialogue gives the Lightning instruction. Continue reveals Use your Lightning power on the Target Dummy. | Original text 21665 maps to `(1992,9,2563,1,1)`; final persistence has objective 9 completed, objective 8 incomplete. |
| 49 | Ordinary logout returns to character selection. | `logout-persisted-state.json` retains the active mission, issued equipment, rifle 17 and reserve 1000. No mission 1992 completion reward was earned. |

The three-round magazine decrease, 20→17, persisted without spending reserve
ammunition. The captured damage and visible restored target establish client
feedback and progression. These stills do **not** quantify single-hit destruction
or the reset interval; the nominal 930 ms timer and inferred 1 HP remain subject
to the separate [combat evidence limitations](bootcamp-combat-feedback-audit.md).

Logout saved context 1985, position `(385.91796875, 119.52734375, 163.27734375)`
and yaw `3.1054482460021973`. Credits remained 100 and experience 1250, inherited
from the earlier checkpoint. Successful persistence does not establish reconnect
behavior, since this pass ended at character selection.

The `items` collection in `crate-looted-state.json` is an unfiltered item-table
snapshot and includes starter items belonging to another character. Ownership
claims above use the later character-scoped `inventory` snapshots, rather than
assuming every item-table row belongs to Alden.

## Comparison with surviving original footage

Video `7Lrst9SG3pk` shows five item receipts at 308.067 s, crate completion at
308.400 s and the equip objective at 308.533 s. Its first boots equip at
321.667–322.267 s returns the tracker to Delessio by 322.400 s. The current
client sequence reproduces these observed transitions. First Delessio talk and
crate opening are hidden by the original 304.667→304.733 s cut; second Delessio
and first Hartmann talks are hidden by the 343.400→343.467 s cut. Their text
mapping comes from the original client table, rather than an uninterrupted
recording of the conversations.

The original rifle tooltip was directly rechecked in
`20260913-bootcamp/footage/analysis/A3/work/tt_rifle_loot.png`, event A3-018 at
304.733 s. Capture 32 exposes concrete differences:

| Rifle field | Original footage | Emulator capture 32 |
| --- | --- | --- |
| Name | Shinobi Rifle | Rifle |
| Primary physical damage | 106 | 106 |
| Optimal range | 60 m | 80 m |
| Alternate physical melee damage | 82 | 25 |
| Magazine | 20/20 | 20/20 |
| Restrictions | Not Tradeable; Not Sellable | Neither line displayed |
| Modules | Armor Piercing +8%; Resist: Laser +12 | Neither line displayed |

Matching primary damage and magazine count do not establish matching weapon
behavior. Range, melee damage, restrictions and module presentation need further
source tracing. The original maker/module/template identity remains unresolved;
template 13713 is still an inferred counterpart. Original HUD 20/994 at
335.467 s reflects ammunition use in that recording, so this run's untouched
1000-round reserve is not forced to 994.

This run predates the separately prepared [rifle range correction](bootcamp-rifle-range.md)
and [absent-skill tooltip pair correction](recruit-clothing-audit.md). Later source
changes or test results must not be attributed to the binaries captured here.

## Final log findings

`client-final.log` has 81 lines. Its 55 `ERROR:`-prefixed lines represent one
startup TypeError, 27 projection warnings and three nine-line item-tooltip
tracebacks, rather than 55 independent exceptions:

- Line 7: `TypeError: argument list must be a tuple`; see the unresolved
  [startup tuple investigation](client-startup-tuple-error.md).
- Lines 26–52: zero-size projection messages; see the
  [native graphics-view investigation](client-projection-warning.md). The affected
  runtime view and triggering lifecycle remain unidentified.
- Lines 53–79: three `TypeError: unpack non-sequence` tracebacks from
  `tooltipwindow._AddSkillSlot`, original source line 1564. The required empty
  skill pair is addressed separately in the [clothing audit](recruit-clothing-audit.md).
  The earlier elemental-icon exception is absent in this run.
- Line 80: one `BaseWeaponAttack` action `1/134` was detected as locked and
  canceled. Its cause remains unverified; successful practice completion does
  not close this action-lifecycle observation.

The 1,806-line `game.log` contains no `[Error]` or `[Warning]` entries. It records
ordinary `CharacterLogout` at 15:38:11.161 America/Chicago and instance destruction
at 15:38:11.854. The immutable client log SHA-256 is
`2b5aaeefd53a4eaffe4044867c10a5fd21982d207cc6211894b016cb5965e516`;
server log SHA-256 is
`254f880ac943092c62cf18b2e15093d6de658a302c0d218c75d96e7c464434a3`.
The evidence record includes full binary and remaining artifact hashes.

## Outstanding observations

Capture 37 shows an officer apparently suspended above the terrain beside the
range. The operator identifies McAllister, who was sent toward camp on mission
acceptance. The image alone does not establish identity, exact height, motion
state or cause. This is an unresolved movement/grounding observation; no fix is
claimed here.

Exact mission 1992 tutorial notification triggers are still unidentified. A
pending tutorial icon does not reveal its event. Individual looting, partial-loot
reconnect and all-held recovery were not exercised in this pass. Lightning,
the final Hartmann conversation and mission turn-in remain unverified. See also
[crate state and recovery](bootcamp-crate-state-audit.md),
[equipment binding](bootcamp-equip-audit.md) and the [reconstruction manifest](evidence/bootcamp-d11-reconstruction-manifest.json).

## Follow-up run 07: tooltips, reconnect and Lightning

The next isolated run used the source snapshot whose combined suite passed
1251/1251 tests, none skipped, in 6.2316 minutes. The original UI rendered all
three unequipped Recruit clothing tooltips (captures 02–04), and the equipped
rifle comparison displayed 60 m (capture 05). The complete stopped client log
contains no skill-unpacking or resistance-iteration errors. The startup tuple
and zero-size projection warnings remain; no broader clean-log claim is made.
See [clothing evidence](recruit-clothing-audit.md) and
[rifle evidence](bootcamp-rifle-range.md).

The journal retained mission 1992 and its completed objectives on reconnect
(capture 06), although the tracker was empty. Selecting the journal's tracking
checkbox restored it (capture 07). The saved grouped option value and controller
initialization order are being investigated separately in
[the tracker audit](client-mission-tracker-reconnect.md); mission persistence
itself succeeded.

Ordinary movement, aiming and ability key 1 completed the Lightning objective
(capture 09). The screenshot shows damage 184 and the completed-objective banner;
the target nameplate reads Practice Dummy. This verifies the current emulator
flow and feedback, not original target health, identity, placement or damage.
Saved objective 8 is completed and objective 7 is pending (Speak to Corporal
Hartmann). The mission has not been turned in.

Normal logout returned to character selection (capture 12), retaining position
(380.69140625, 119.59375, 172.84375), yaw 3.042555093765259, 1250 XP, 100 credits,
rifle magazine 17 and cartridge reserve 1000. The owned client, servers and Xvfb
then exited, and their final logs were frozen. The evidence manifest's
`followup_run_07` records 18 artifact hashes, binary input hashes, final snapshots
and stopped-process status. Test logs, TRX and the source snapshot are also
preserved under the research directory's `verification-gearing/` folder.
