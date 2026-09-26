# Capture the Flag: next-segment audit

Audit date: 2026-09-22. The initial read-only review compares current isolated-server
world rows and source with retained original-client data and verified footage.
It does not certify a completed native-client playthrough or final-live fidelity.
Exact selected rows, queries and artifact hashes are recorded in
[the evidence JSON](evidence/client-capture-the-flag-audit.json).

## Ordered runtime checklist

1. Finish mission 1992 at DeSimone, placement 198657, package 2562. The
   revised camp estimate is `(390.5,119.55,156)`, near the post-edit player
   and Forean Initiates at the north pylon. DeSimone himself is not visible in
   the edited interval, so his exact position remains unverified. Mission
   1994 becomes available only after 1992 is completed. Its automatic offer
   behavior remains unobserved; reopening conversation is the implemented path.
2. Accept 1994, then complete objective 4 through DeSimone's original
   conversation text 21694. This grants 500 XP and reveals objective 2. The
   original `objectiveconversation` lookup is `(1994,4,2562,1,1) -> [21694]`.
   `7Lrst9SG3pk` A3-066 at 362.133 s shows 1992 completion, 1994 acceptance,
   500 XP and level 2 in chat, but the preceding conversations are cut.
3. Follow the cave route west toward `(279.05, 120.5, 66.07)`. Actual area
   198602 is a cylinder with radius 10 m and half-height 25 m, after later
   migrations. Earlier status prose describing a 5 m sphere is stale. Entering
   the area completes objective 2 and reveals objective 1. A4-20/21 at
   430.533 s and A4-24 at 432.667 s show that transition and the boss counter
   `0/1`; the exact cave-in crossing/interaction occurs inside a cut.
4. Continue through the passage toward Denzil's Caldera and the AFS base.
   Retained measured player points include `(264.62,64.88)` after the cave
   objective, `(246.17,90.54)` in the tunnel, `(233.8,96.95)` at its mouth,
   and `(209.78,93.86)` in the trench, expressed as XZ. The recording cuts
   before `(159.87,173.64)` at the base perimeter. These are observations of
   the player's route, not a continuous validated path or new waypoint seed.
5. Kill the objective-bound boss placement 198659 at
   `(95.1,109.25,150.8)`. It exists while objective 1 is incomplete. The
   position uses the observed Base Center marker (A5-25/26, 541.4/547.6 s),
   not an observed boss spawn. The counter's slot 0 and target 1 are loaded;
   kill matching uses placement identity. Its completion reveals objective 3
   and enables Youngblood placement 198658 at `(93.2,109,137.5)`.
6. Complete Youngblood's objective conversation, then turn in 1994 for
   5000 XP. Original lookup `(1994,3,2561,1,1) -> [21617]` identifies the
   conversation. A5-27 at 551.467 s shows the Youngblood objective after a
   cut; `8VXeKzGUv0c` B1-008 at 4.267 s shows 5000 XP and mission completion.
   Other payout components are not recovered and are omitted.

## Findings and limits

No new normal-path progression blocker was demonstrated by this source review.
Existing loading tests verify definitions, bindings, counter, indicators and
presence conditions; that is narrower than playing the route and combat.
The retained navmesh probe finds the courtyard/tunnel segments complete but
the trench-to-perimeter segment incomplete. Native player movement uses original
collision, so this alone does not prove the player's route is blocked.

Boss level 10, HP 1000 and attack balance remain inferred/analogue values;
the original boss fight is not visible. The 143 XP/50-credit chat at A5-28
cannot establish exact boss stats. Fifteen level-2 Initiates use measured
engagement positions with analogue combat values. Friendly combat support,
arrival effects and Youngblood's complete appearance remain gaps. The cave-in
completion's area-only mechanism is also an inference, not recovered script.

A separate recovery defect is suggested by the actual code: `OnKillBinding`
persists a counter, then calls `CompleteBoundObjective`, which commits objective
progress separately. A failed second commit can leave the killed boss's counter
at 1/1 with its objective incomplete. `ReconcilePlayerMissions` repairs missing
reveals, but does not complete satisfied counters. Rebuilding the instance can
recreate the still-required boss; it does not itself repair the satisfied
objective. Failure-injection regressions and a bounded correction now follow.

## Counter transaction and reconnect correction

The final kill now stages its counter in the existing objective completion
transaction, together with the completed status, next objective, timestamps and
content reactions. A failed save leaves the counter and objective unchanged in
storage and memory and sends no success packets. Successful commits retain the
existing counter → objective completed → objective revealed packet order.
Nonterminal counter increments retain their existing path.

Admission also repairs saved states left by the old split commit: an incomplete
kill-bound objective with all explicitly saved counters at their targets is
completed through the existing objective path. It does not count a new kill.
The completed objective makes subsequent reconnects idempotent. For 1994 this
reveals Youngblood's objective and satisfies his presence condition; his
conversation and mission payout remain pending.

Three seeded-data cases cover successful packet order, failure at the objective
save, and a legacy 1/1 incomplete objective loaded through the admission
repository and reconciled twice. Root owns old/new serial test execution;
the old-production and corrected full-suite results are recorded below.
This is an emulator persistence correction. It establishes
no new original gameplay rule or combat value. A wholly failed save leaves no
committed kill progress; rebuilding the instance may still be needed to fight
the required boss again after storage becomes available.

The old-production baseline ran all three cases in 38.1185 seconds. The normal
kill control passed. Both defect cases failed their intended assertions: the
injected objective-save failure left persisted counter 1 instead of 0, and
reconnect retained an incomplete objective despite its saved 1/1 counter.
Neither failure was a fixture exception. The baseline log and TRX are archived
with hashes in the evidence JSON.

The combined corrected full suite passed **1300/1300 tests** in 5.9650 minutes.
Its TRX contains all three seeded boss cases as passing leaf results: normal
packet order, atomic rollback under injected save failure, and idempotent
legacy-counter recovery. The console log and TRX are archived with hashes.
This verifies the persistence correction; the original-client mission 1994
route and encounter remain separately subject to playthrough verification.

## Walking from the observed DeSimone position

A read-only query with the existing prebuilt NavProbe finds a complete route
from the **previous inferred** DeSimone placement `(387.2,127.7,40)` to the
cave marker. This southern placement was not an observed player position.
Its XZ corners are `(384,50.4)`, `(372,63.2)`, `(365.6,63.6)`,
`(297.6,64.4)`, then `(279.05,66.07)`. Head north and slightly west to the
opening junction, then west through the Eloh stairs around `(369.5,65)` and
ceremonial bridge at `(337,65)`. Continue through the courtyard, passing south
of the sandbags near `(297.37,66.31)`, toward the cave marker. The original
measured player position `(310.79,66.12)` falls along this courtyard approach.
Continuing north from the junction instead returns toward the gear deck.

The stair, bridge and sandbag landmarks are original map entries 781, 564 and
1053. Probe heights descend from approximately 125.3 to 120.7 m; these are
simplified navigation heights, not exact native foot heights. The existing
probe does not simulate player collision or certify this route has been walked
in the current original-client run. Inputs/results and their hashes are kept
with the audit artifacts. The cave objective should complete upon entering
its 10 m horizontal trigger while objective 2 is active.

The later [camp placement correction](evidence/bootcamp-desimone-camp-placement.json)
uses the filmed post-handoff player and measured ally group near `(389,155)`.
The probe from that camp to the cave is also complete, with corners around
`(386,133)`, `(359,115)`, `(343,109)`, `(302,81)` and `(288,70)`. This is a
different, more direct approach than the southern bridge route tested above.
The DeSimone coordinate is still inferred. An isolated recovered-client check
at that coordinate showed his marker and talk prompt, opened the original
Objective Completion text, and advanced objective 4 to the cave objective for
500 XP. It used a copied character with objective 4 restored to incomplete;
the full Hartmann → DeSimone → cave route remains open.

## 2026-09-23 original-client walking checks

An isolated level-2 checkpoint with objective 2 active walked from DeSimone's
position to the opening junction in the recovered 1.16.5.0 client. From the
junction at approximately `(373,125,63)`, ordinary left strafe crossed the
bridge and reached the hostile courtyard near `(303,121,69)`. The unmodified
level-2 Initiates killed the character there before the cave area. This
reproduces the previously observed unopposed death with a more precise route;
it does not establish that the encounter is harder than final live. An early
attempt at `(364,124,67)` ran into a rock and then fell from the narrow path;
that was a steering error, not a demonstrated collision defect.

A second disposable run restored the same checkpoint and disabled only the
ambient Initiate creature's `action1` attack in its isolated world copy
(`198507: 33 -> 0`). This is a diagnostic setting, not a content change.
Starting at the junction after a diagnostic GM teleport, ordinary client
movement crossed the courtyard and entered area 198602. The tracker changed
from “Find a way out of the cave” to “Eliminate the Tizzik G” with `Boss
Eliminated: 0/1`; the saved objective 2 changed to complete while objective 1
became active. The last pre-trigger position was about `(285.95,119.80,54.06)`;
the crossing occurred while moving toward the marker `(279.05,120.5,66.07)`.
The isolated world database was restored to `action1=33` afterward. Inputs,
screenshots, copied databases, exact hashes and diagnostic limits are in
[the walking record](evidence/client-cave-walk-playthrough.json).

This verifies the emulator's client-driven area transition along a walkable
approach. It does not verify the unmodified fight, the original final-live
trigger geometry, or the complete route from DeSimone in one uninterrupted run.

## Isolated client check at the cave trigger

A copied level-2 Alden checkpoint entered area 198602 through a GM teleport in
the recovered 1.16.5.0 client. After map load, the tracker changed from “Find a
way out of the cave” to “Eliminate the Tizzik G” with `Boss Eliminated: 0/1`.
The copied character database records mission 1994 objective 2 complete and
objective 1 incomplete; objective 4 remains complete. A hospital respawn
preserved the boss tracker. Captures, database, process status and hashes are in
[the playthrough record](evidence/client-cave-trigger-playthrough.json).

Deaths after teleporting into the cave and near the inferred base marker are
diagnostic. They do not reproduce the normal walking route or establish original
encounter balance. The boss and Youngblood transitions were checked separately
below.

## Isolated client check of boss and Youngblood transitions

A second copied checkpoint loaded objective 1 in the recovered 1.16.5.0 client.
The disposable world copy set Tizzik Gi to one HP and disabled its and the
ambient Initiates' attacks solely to test the mission sequence. After a GM
teleport to the base, the client targeted and killed Tizzik Gi. Objective 1
completed, its counter saved at 1/1, and objective 3 appeared. The copied
database recorded 124 XP and 50 credits for this diagnostic kill. The actual
final-live boss reward is unverified.

On reconnect with the completed boss checkpoint, Youngblood appeared with his
objective marker. Right-click opened the Objective Completion dialogue. After
Continue, a second conversation opened Mission Completion; clicking Complete
Mission displayed 5000 XP and removed Capture the Flag from the tracker. The
saved mission is state 4, all four objectives have status 2, and the copied
character has 8124 XP and 350 credits. The world copy was restored after both
checks. Captures, databases, diagnostic row changes and hashes are in
[the boss and turn-in record](evidence/client-capture-flag-boss-turnin.json).

This verifies the visible client/server mission transitions under diagnostic
conditions. It does not verify the normal walking route, actual encounter
difficulty, friendly support, or original spawn heights. The boot-camp segment
remains open until those and the following missions are checked.
