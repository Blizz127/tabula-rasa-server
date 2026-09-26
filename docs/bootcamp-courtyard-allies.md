# Capture the Flag: missing combat companions

Audit date: 2026-09-23. The source is the locally archived final-week original
gameplay recording `7Lrst9SG3pk`, with frames and hashes in
[the evidence manifest](evidence/bootcamp-courtyard-allies-audit.json). The
recovered 1.16.5.0 client remains the emulator compatibility target; its exact
match to the shutdown client is unverified.

At 362.133-384.933 seconds, after Capture the Flag is accepted, the player
targets a level-3 Forean Gunner, Archer and Shaman Initiate in the same group
beside the camp pylon (event A3-070). From 386.5 to
401.5 seconds, several allied soldiers bearing the orange two-person marker
fight level-2 Thrax Infantry Initiates around the Luna Cavern courtyard. The
player survives and receives kill rewards. At 501.5 seconds in Denzil's
Caldera, the target frame identifies a level-3 Forean Archer Initiate; the
verified event record also identifies a Shaman Initiate nearby. Original
mission text 21237 says soldiers are sent along with the recruit. These facts
support allied combat companions for this mission. They do not establish exact
spawn coordinates, movement script, combat values or how much of the player's
survival the allies caused.

The existing position research measures the camp allies around
`(385.2,119.5,152.3)` with ±2.5 m horizontal uncertainty. Immediately after
the edited handoff the player is near `(389,155)`. The prior inferred DeSimone
placement at `(387.2,127.7,40)` lies about 115 m south of this scene. A
[labelled placement correction](evidence/bootcamp-desimone-camp-placement.json)
now puts him near the visible post-handoff player. His conversation remains
hidden inside the original edit, so this is an inference. A copied recovered
client checkpoint opened and completed his dialogue at the revised emulator
coordinate; that verifies usability, not the original live placement.

The current S4 rows seed hostile Initiates and Tizzik Gi but no allied
companions. The isolated original-client approach with an unmodified world
database died in the courtyard at about `(303,121,69)` without fighting. A
separate attack-suppressed diagnostic reached the cave trigger; see
[the walking record](evidence/client-cave-walk-playthrough.json). This contrast
identifies missing support as a material fidelity gap, but does not establish
that current hostile damage is wrong.

`MissionContentManager.WorkEscorts` can path an escort toward a player. Today it
is called only by `EscortInside`, after the player has already entered an area
objective. It would not send companions along the bridge or through the
courtyard. The same `EscortInside` check would also require every surviving
escort to enter the cave trigger before objective 2 completes if these allies
were simply marked as escort placements. The original recording shows the
objective transition inside a cut, so imposing that new requirement has no
evidence. Combat followers need mission-scoped follow and attack behavior
without automatically becoming required objective participants.

The three named level-3 companions have staged rows with per-field provenance.
Verify the Archer and Shaman classes, target categories and attacks in the
recovered client, then compare the level-2 encounter with the filmed route and
pacing. Their unrecovered stats and precise choreography remain labelled as
estimates.

The first staged row, [camp Gunner](evidence/bootcamp-camp-gunner-companion.json),
uses the observed level-3 name, the measured camp group centre as an inferred
individual position, and explicitly labelled analogue combat values. A new
`CombatCompanion` placement behavior makes the existing AI follow the private
map owner and attack opposite-faction creatures; its orange escort marker is
sent to the client. The cave area objective still tests only the player, because
the companions are excluded from `EscortInside`. The exact Gunner location and
original combat parameters remain open. These rows are pending encounter
validation before live deployment.

An isolated recovered-client replay now confirms the new Gunner renders with
the orange companion marker and the level-3 `Forean Gunner Initiate` nameplate.
The marked soldier followed the level-2 recruit from the camp into Hartmann's
sandbags and back across the platform under ordinary player movement. The
client display ended before the courtyard, so attacks, full route navigation
and survival remain open. The [companion record](evidence/bootcamp-camp-gunner-companion.json)
links the replay screenshots, copied-server log and exact limits. The vivid
purple appearance in this replay is provisional. A later runtime correction
keeps the randomly chosen body tint on each creature instance, so two clients
see the same creature colour; the tint itself remains an unverified analogue
and may still differ from the final-live appearance.

A second disposable-client diagnostic placed the level-2 recruit at
`(302,120.7,69)` and moved this Gunner in the copied world to `(297,120.7,69)`
beside the six S4 courtyard Thrax. The player did not attack. The screenshot
shows a fallen purple Forean-shaped body and incoming damage; the server log
confirms the player died at `(302,120.5,69)`, killed by a nearby Thrax. This
does not prove the Gunner's own death or damage, and it is not the original
walk from camp. It demonstrates that one staged Gunner does not make this
forced six-enemy encounter survivable. The diagnostic screenshots, hashes and
limits are in the companion record. Do not deploy this encounter as a
completed reconstruction on that evidence.

The companion audit also exposed a combat defect: when a creature has no
`creature_stat` row, `CreateFromTemplate` gave it 100 HP regardless of the
`creature.max_hp` value. The new Gunner and the existing boot-camp Thrax have
no override row. The staged runtime fix uses their database `max_hp` in that
case, so this encounter will now exercise the labelled HP estimates instead
of an undocumented 100 HP fallback. This affects other creatures without
`creature_stat` rows too and needs encounter review before deployment.

The later review of A3-070 corrects the first audit's narrower reading:
Archer and Shaman are directly named beside the Gunner at the opening pylon,
not only in the later Caldera fight. A second staged migration adds them using
original-client bow/staff weapon classes and attack pairs. The Shaman's
range, cadence and damage are explicitly analogue values. Their individual
camp coordinates are inferred offsets from the measured group centre, with
navmesh-measured heights. See [their field record](evidence/bootcamp-camp-archer-shaman-companions.json).
An isolated recovered-client replay loads all 409 content rows with zero gaps,
renders three orange-marked camp Foreans and shows all three following the
recruit across the platform. The Archer target frame reads level 3; the
Shaman's purple crystal staff is visible. Screenshots, hashes and the replay's
limits are recorded in the field record. Their attacks and the continuous
camp-to-cave route remain unchecked. The migration applies to a copied live
SQLite database and rolls back all content rows; SQLite's creature-action
autoincrement sequence advances from 45 to 46 during that round trip.

A later [copied-client combat diagnostic](evidence/bootcamp-camp-allies-combat-diagnostic.json)
put the level-2 recruit and all three companions near the courtyard group.
With six Thrax active and no player attacks, the companions fell and the
player died. Those forced positions do not measure the filmed encounter's
survivability. A second copied run kept only one Thrax and enabled the opt-in
`RASA_TRACE_CREATURE_HITS` server trace. It records 11 Gunner, 10 Archer and
10 Shaman hits on placement 198660, including the Gunner's killing hit from
8 to 0 HP. The Thrax hit the Archer 33 times; the Archer still had 253 HP on
the last recorded hit. These traces verify that all three staged attack actions
deal server-side damage, but the normal approach, target switching, ally
survival and original combat values remain unverified. The ally-only kill did
not change the copied player's XP or area objective; the filmed player attacks
as well, so that diagnostic cannot settle original kill-credit rules.

A subsequent [native-client route diagnostic](evidence/bootcamp-camp-allies-route-diagnostic.json)
started from the copied camp checkpoint with all three companions at their staged
placements. Ordinary movement took the level-2 recruit up the camp staircase,
across the raised walkway, over a rail by a player jump, and down the rocky
descent to `(360.07,121.20,114.64)`. Screenshots show the three orange-marked
Foreans following on the stairs, walkway and rocks; all had caught up at
`(371.85,120.80,124.78)`. The copied player's XP and mission objective were
unchanged, and no creature-hit trace was recorded before the display session
closed. The jump is a diagnostic route choice, not established original
choreography. Courtyard combat, ally survival and cave transition remain open.

The [next isolated continuation](evidence/bootcamp-camp-allies-courtyard-approach.json)
reached `(316.13,120.28,85.93)` with three orange-marked Foreans still close
behind. A level-2 Thrax Infantry Initiate was visible and targetable at
`(308.73,120.10,79.21)`, but the player remained outside the cave trigger and
no hit trace occurred before the first client display ended. A second camp-edge
replay followed the same companions through the rocks; the diagnostic movement
line then went below the intended navmesh corridor near `(323.51,118.43,89.38)`
and could not regain it. Neither run reaches the courtyard fight. The low
route is a test-navigation error, not evidence of a companion pathing defect.

A [normal-approach copied-client combat run](evidence/bootcamp-courtyard-normal-approach-combat.json)
continued from the upper-path checkpoint into the courtyard with all three
companions present. Without player attacks, Alden died at `(297.2,120.2,73.4)`
before entering the cave area. All three companions then died under fire from
five of the six staged Thrax; the opt-in hit trace recorded zero companion
hits in this run. The player died about four seconds before the first logged
hit on a companion. This diagnostic does not establish that the original
final-live encounter had these damage values.
The earlier one-Thrax run did record all three companions dealing damage.
Their engagement trigger and the filmed player-attacking route need review
before these rows can be deployed as a faithful, playable encounter.

A [formation diagnostic](evidence/bootcamp-courtyard-formation-diagnostic.json)
clarifies the zero-hit result. The original footage cuts from camp to
courtyard. Allied figures stand ahead in the first courtyard fight, but an
orange escort marker on the original radar trails the recruit by roughly 15 m
at 385.467 seconds and closes to roughly 5 m by 389.467 seconds. The figures
ahead may be separate AFS defenders; their identity is unverified. When a
copied character and all three companions were artificially placed ahead,
the server logged 2 Gunner, 8 Archer and 17 Shaman hits
against a Thrax. With no timely player attacks they and then the recruit
still died. A further input attempt fired only after about 44 seconds of
ally combat while the client loaded, so it does not test the filmed opening.
The current six hostile coordinates come from combat positions measured at
different timestamps, not observed simultaneous spawn points. Escort travel
and hostile timing across the footage cut remain explicit gaps.

The [escort combat rule audit](evidence/bootcamp-courtyard-escort-combat-rules.json)
identified a concrete emulator blocker: fighting AI used each companion's
camp spawn as a 60 m combat leash centre even after it followed the player to
the courtyard, over 100 m away. The staged fix centres that leash on the
followed player while the creature remains an escort. It also links mission
companions to the existing assisted-target rule and checks creature faction
before copying a selected target. The actor lookup also now tolerates the
followed player's removal during combat. Mission escorts ignore selected player
targets, preserving their AFS allegiance. The specific assist semantics are inferred;
original footage establishes allied combat but not that target-selection rule.
The projects build, and focused .NET 5 AI checks confirm combat 130 m from
camp followed by a clean retreat when the player disappears, plus hostile
target assistance without attacking a selected friendly player. A copied-client
one-Thrax route with all companions at camp
remains incomplete: client navigation stalled at a stone wall, then a detour
fell below the walkable corridor, so it yielded no ally-hit result for the new code. This change
still needs a continuous client route and encounter verification before a
deployment decision.

A [second copied-client checkpoint](evidence/bootcamp-courtyard-escort-combat-rules.json)
loaded the recruit at a previously verified walkable point `(316.13,120.28,85.93)`
while the three companions retained their staged camp placements. All three
reached the recruit during map load. After a partial walk to `(308.73,120.10,79.18)`,
diagnostic same-map teleports brought the recruit to `(295,121.6,68)` near the
single Thrax left in the disposable world. Without player fire during the fight,
the trace recorded 11 outgoing hits from each companion and the Thrax falling
to zero health. This verifies copied-client escort combat far from camp, but
the teleports and removal of five Thrax prevent a claim about the natural route
or the original encounter's pacing and balance. The 555-health and 100-armor
enemy values remain labelled analogues. A [target-panel footage review](evidence/bootcamp-thrax-health-audit.json)
found no defensible maximum-health measurement, so the 555 estimate remains
explicitly unverified rather than being presented as a retail stat.

A [full six-Thrax checkpoint replay](evidence/bootcamp-courtyard-six-thrax-checkpoint-replay.json)
used the recovered client with all six staged hostiles, three camp companions
and the staged Warrior. Combat started while the recruit was still at the copied
upper-path checkpoint, before movement input. The Warrior landed three hits
and died; the Gunner, Archer and Shaman landed 25, 18 and 20 hits respectively.
The Shaman killed one Thrax after all three companions had damaged it. The
recruit later killed another Thrax and received 71 XP and 10 credits, then died
with mission 1994 objective 2 still active. All four allies died. This verifies
companion attacks with all six placements present in the staged world, but the
checkpoint login bypassed the camp approach and long input pauses prevent a
retail pacing comparison. A complete natural encounter remains unverified.

A later [upper-route original-client replay](evidence/bootcamp-courtyard-upper-route-client-replay.json)
started from a copied rocky-descent checkpoint at `(360.07,121.20,114.64)`.
Ordinary movement reached `(327.59,120.63,96.11)` on the upper path, but
walking across the next narrow gap fell to the lower floor. A fresh diagnostic
login at that reached upper-path point crossed the gap with W+Space, then used
ordinary movement and another jump over the courtyard obstruction to reach
`(306.54,120.53,67.16)`, near the final-week filmed viewpoint. The three camp
companions followed and killed one Thrax without player fire; the Warrior and
companions later died. Thrax 198662 killed the recruit at that viewpoint,
27.5 m from the 10 m cave trigger. Mission 1994 objective 2 remained active.
The two checkpoint runs establish a traversable staged route and the jump
locations, not one uninterrupted camp-to-cave playthrough or original fight
balance. Long pauses between inputs also prevent a timing comparison.

Three [timed continuous rocky-descent replays](evidence/bootcamp-courtyard-continuous-route-replay.json)
then joined the upper gap and courtyard jumps in a single recovered-client
session. The disposable character's crate rifle was restored from the spent
three-round checkpoint to its observed 20-round issued load before login;
the player did not fire. Ordinary bounded movement crossed from
`(360.07,121.20,114.64)` into the six-Thrax courtyard. The closest run reached
`(290,120.5,64)`, about 11.1 m from the cave trigger center with a 10 m radius,
then died three seconds later. Two variant final movements died at 17.8 m and
18.8 m from the center. Objective 2 remained active, and no timed replay
completed the encounter. These sprint tests establish route continuity from
the descent checkpoint; their no-fire tactics and reconstructed hostile stats
do not establish original survivability or justify a balance change.

The same final-week recording identifies a separate level-2 `Forean Warrior`
in the courtyard at 401.8-402.067 seconds. He is distinct from the named
Gunner, Archer and Shaman Initiates at camp. A [staged defender row](evidence/bootcamp-courtyard-forean-warrior.json)
uses an original-client Forean Warrior class-name fallback and the closest
existing Forean Spearman attack set. His exact class, allegiance, position,
health and attacks are unresolved; those choices are labelled in the record.
The first inferred point `(294,120.5,67)` was off the upper walkable navmesh;
the original-map probe moved the staged point to `(294,120.5,65)` on the upper
polygon. The new row is scoped to the 1994 cave fight and is not a claim that
one Warrior accounts for all soldiers visible ahead. Migration parity, a
full SQLite migration test and field-by-field evidence parity test pass. The
recovered client's `GetCreatureName` bytecode confirms the class-name fallback.

The recording cuts from the player with three Initiates at camp in frame
384.467 seconds to the player already in the courtyard in frame 385.467 seconds.
It cannot establish how those Initiates traveled, when the fight activated, or
how long the Warrior had been fighting before his target plate appeared.
An [isolated original-client probe](evidence/bootcamp-courtyard-warrior-client-probe.json)
loaded the staged row in a disposable world copy. With all six Thrax present,
the idle level-2 recruit died before a readable Warrior target plate could be
captured. In a second diagnostic copy with only those six Thrax placement rows
removed, a fourth Forean-shaped actor rendered beyond the three nearby
companions on the upper courtyard floor. This supports client rendering and
floor placement. A further diagnostic removed the companions as well and
moved only the Warrior into the original client's reticle. The client then
showed both an overhead plate and target panel reading **level 2 Forean
Warrior**. The moved diagnostic position and stationary behavior were restored
in the disposable database afterward. His exact original class, original
position and full natural encounter remain unverified before deployment.

A [conditioned-creature respawn audit](evidence/bootcamp-conditioned-creature-respawn-audit.json)
found that a killed mission-scoped Warrior or camp companion could be recreated
after corpse cleanup by an unrelated mission state refresh, despite its seeded
zero respawn delay. The map channel now records defeated zero-respawn placements
until instance rebuild and leaves timed placements waiting for their due tick.
This enforces the current seed's lifetime rule; the original allied respawn
behavior remains unknown. Two focused .NET 5 state tests pass.
The seeded boss kill path has a focused emulator check: a creature finishing
Tizzik Gi completes the private-map owner's 1994/1 counter and objective
without giving player kill XP. Original NPC-assisted kill credit is still
unverified.

The [original reward timeline](evidence/bootcamp-courtyard-kill-pacing.json)
contains six Thrax XP-and-credit events from 391.533 to 427.733 seconds,
spanning 36.2 seconds. These are kill/reward times, not proof of six simultaneous
spawns or a particular wave timer. The seeded enemy count and timing still need
comparison against a normal client fight before adjustment.

An [isolated cover-fire replay](evidence/bootcamp-courtyard-cover-fire-replay.json)
began at a copied courtyard checkpoint with the observed 20-round rifle load
restored. Native-client fire killed Thrax 198661 while Shaman 198690 was still
fighting; the client awarded 71 XP and 10 credits. After the Shaman died, R
reloaded the drawn rifle from 5/1000 to 20/985, and the recruit killed Thrax
198664 for a second 71 XP and 10 credits. The disposable database ended at
3713 XP and 330 credits. An attempted jump toward the cave stopped at
`(295.8,120.5,70.6)` against the courtyard obstacle; Thrax 198663 killed the
recruit there, 17.4 m from the cave objective center. The objective stayed
active. This verifies that the staged player combat and reload loop works
through the recovered client. Its pause-heavy checkpoint tactics and estimated
enemy values do not establish final-live balance or normal encounter pacing.
