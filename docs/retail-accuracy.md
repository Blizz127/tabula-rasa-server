# Retail accuracy work log

## Local evidence availability — 2026-09-24

The `/home/blizz/backups` tree disappeared during S5 validation on
2026-09-24. Historical paths under it in this log describe where evidence
and rollback copies were recorded at the time; they are currently unavailable
and must not be cited as still-present files. The 1.16.5.0 public client ZIP
and its verified extracted files were reacquired at
`/home/blizz/private-artifacts/rasa-client-1.16.5.0/`; see
[client artifact recovery](client-artifacts.md). The latest five S5 prefix
captures are preserved separately with a [633-file verified mirror](evidence/bootcamp-s5-prefix-durable-mirror-20260924.json).
Other missing research and rollback artifacts remain unlocated.

## Target and baseline — 2026-09-12

The user's clarified target is **1:1 preservation of the final live game before
shutdown**, covering the full game and its final content. `AGENTS.md` records this
as the repository's governing requirement. Client **1.16.5.0** is the version
required by `docs/setup.md`. An acquired client executable now confirms that
embedded version, with client tables available for static inspection; see
[artifact provenance](client-artifacts.md). An independent official manifest
and exact final server configuration remain missing.
The broad goal remains incomplete: working login and a populated item database do
not establish retail gameplay parity.

Earlier changes below are implementation progress, not certified final-retail
equivalence. They must be rechecked against the clarified preservation standard.
No custom rates, balance changes, replacement quest content, or convenience rules
are part of the requested end state. Unknown behavior must stay visible as a gap
until evidence supports a faithful implementation.

[Final retail evidence](final-retail-target.md) now preserves original official
D16.4/D16.5 announcements and the final event/farewell messages, with capture
dates and hashes. D16.5 is positively identified as live on 17 February 2009;
the exact final executable revision and later server-only changes remain open.
Final-patch mech access and unusual end-of-service rewards must be preserved
when evidenced, even where they differ from earlier retail rules.

The initial local HEAD and GitHub default branch both resolved to
`2a3e4bb8f9f153ebf64805cbd420f855f850c78b` (2023-12-27). The existing Compose
network/port overrides and persistent databases predate this work.

## 2026-09-24 — S5 use recovery and regression check (deployed)

Repeated content-use requests could replace the pending bomb action, letting
an earlier recovery finish a later request before its measured windup. The
[use-windup audit](bootcamp-s5-use-windup-audit.md) records the exact-action
recovery and interruption fix; the original duplicate-request presentation
remains unknown. A map without cover geometry also emitted float `1` in the
combat damage packet's cover slot; the source now retains the original
integer-zero shape when no cover calculation ran. The S2 provenance test now
applies DeSimone's later placement correction before comparing current rows.
The .NET 5 test project builds, and its full suite passes 1,335/1,335. The
[live DIT-overlay rollout](evidence/live-preservation-rollout-20260924a.json)
started successfully with both SQLite databases intact, 414 content rows and
zero content gaps. A [recovered-client login smoke](evidence/live-login-preservation-20260924a.json)
reached Character Select through the VPS Tailscale auth port on that image;
world entry and the owner's client remain unverified. Tests, startup and login
do not establish final-live parity.

## 2026-09-23 — Live authentication reconnection

The owner reported another login stall at “authenticating.” The live game
container had started at 12:01 UTC, but the long-running auth container had
no successful registration for that replacement. Client connections ended
without a logged `Login` opcode. Restarting only auth at 19:55 UTC let game
server 234 authenticate at 19:55:21 UTC; the game log independently recorded
`Successfully authenticated with the Auth server!`, and both the local and
public 2116 listener returned the 11-byte protocol greeting. An owner retry
has not yet confirmed a complete login.

The auth server formerly rejected a valid game server when its ID was still
in the registration table, even if that table held a stale socket. The
worktree now permits a replacement after checking the configured ID and
secret, closes the old socket, and removes a registration on disconnect only
when that exact connection still owns it. Late info and redirect responses
from a replaced connection are ignored. The game communicator also closes a
failed socket before its 10-second retry; its former `Connected` check could
see an old TCP connection in CLOSE_WAIT and suppress reconnect indefinitely.
Both projects built. Separate `rasa_net_auth` and `rasa_net_game` images were
deployed from the running versions with these source changes; the production
auth, character and world SQLite files were backed up and passed
`integrity_check`. A game container recreation registered server 234 with auth
without restarting auth (20:12:41 UTC). Restarting auth alone then caused the
game to log a disconnect, reconnect after 10 seconds and authenticate again
at 20:13:24 UTC, without restarting the game. A recovered 1.16.5.0 client
then logged through public port 2116 with the existing test account, sent
`Login` → `ServerListExt` → `AboutToPlay`, and reached Character Select.
[The deployment and login record](evidence/live-auth-reconnect-20260923.json)
contains the filtered log and screenshot hashes. The owner's own client and
launcher have not yet been observed after the fix.

## 2026-09-22 — Calling for Reinforcements client continuation

An isolated recovered-client continuation accepted mission 1995 from Youngblood
after completing 1994. The tracker showed the missing-soldiers objective without
a timer. The seeded wounded soldier's conversation opened and advanced the
tracker to Conrad's corpse; the copied database confirms objective 2 complete
and objective 3 active. See [the client checkpoint](client-reinforcements-opening.md)
and [artifact manifest](evidence/client-reinforcements-opening.json). Hostile
attacks were disabled in a disposable world copy, and GM teleports bypassed the
normal route. A subsequent [corpse placement comparison](evidence/client-conrad-corpse-placement.json)
found the inferred draft location inside ramp geometry with no complete navmesh
route. An inferred correction to reachable floor was verified in the recovered
client: using the object completed objective 3 and began the 600-second bomb
timer. A follow-up bomb checkpoint found the measured radar point obstructed
by the reconstructed wreck. A [placement comparison](evidence/client-reinforcements-bomb-correction.json)
found a usable near-hull point within the original per-axis measurement bounds.
The recovered client armed and detonated the bomb, completed objective 1,
removed the wreck, spawned reinforcements and revealed Van Valkenberg. This is
an inferred placement correction, not a recovered retail coordinate. Exact
retail positions and normal traversal remain open. A further
[handoff check](evidence/client-reinforcements-handoff.json) opened Van
Valkenberg's completion dialogue, transferred to Alia Das from the pad, offered
Training Day, and turned 1995 in to Rogers. Rogers' generic dialogue showed a
missing greeting placeholder. A further
[diagnostic failure/retry check](evidence/client-reinforcements-retry.json)
recorded mission 1995 failed after timer expiry, Youngblood's retry offer,
mission 2005 acceptance with a 600-second objective timer, and successful
retry bomb detonation revealing Van Valkenberg. The timer was advanced and
the character teleported in an isolated copy. Exact final-live timer values,
1995 rewards and normal traversal remain unverified.

## 2026-09-22 — Training Day recovered-client turn-in

An [isolated client check](evidence/client-training-day-turnin.json) started
from a copied character with boot-camp mission 1995 completed and Training Day
active, then moved the character near Kincaid. Kincaid opened the original
objective-completion and mission-completion client text. The physical pistol
choice sent `CompleteNPCMission`; the copied database recorded mission 1526
complete, 120 more credits and pistol template 116929. A relog showed no
Training Day tracker, with the reward and mission state persisted. The client
tooltip lacks the original footage's “Vextronics” prefix and Reduce Resist
module, because the original server module definition and effect data remain unrecovered. Normal
travel from the boot-camp exit and exact final-live reward values remain open.
The [module binding](evidence/training-day-reward-modules.json) now assigns
the original-client module IDs to the offer and saved reward item. Isolated
recovered-client runs showed both Vextronics names and the filmed `[2] Reduce
Resist` tooltip lines for physical and EMP pistols. The selected physical
pistol persisted module 900221. The tooltip packet's unrecovered coefficients
are labelled inferred; proc chance and combat application remain open.

## 2026-09-22 — Capture the Flag cave trigger client check

An isolated copy of the saved level-2 character entered the mission 1994 cave
area in the recovered client by GM teleport. The tracker advanced from the cave
objective to the Tizzik G boss objective; the copied database confirms objective
2 complete and objective 1 incomplete. A hospital respawn preserved this state.
The [capture and database manifest](evidence/client-cave-trigger-playthrough.json)
records the inputs and results. Teleport deaths near the cave and inferred boss
marker do not establish normal-route encounter balance. A later isolated
client check used one-HP Tizzik Gi and disabled hostile attacks in a disposable
world copy. It completed the boss objective, Youngblood objective conversation
and mission 1994 turn-in, with the expected 5000 XP message. The copied
database has mission state 4 and all four objectives complete. See
[the boss and turn-in record](evidence/client-capture-flag-boss-turnin.json).
Normal-route encounter combat, the full camp-to-boss route and later boot-camp missions remain
open. The diagnostic world copy was restored.
An [isolated normal-health boss probe](evidence/bootcamp-normal-boss-client-probe-20260924.json)
subsequently confirmed that ordinary original-client rifle fire damages the
deployed-derived Tizzik Gi: two aimed 106-damage hits reduced his analogue
100 armor to zero and health from 1000 to 888. The earlier apparent miss was
the moving boss leaving the reticle before firing. This verifies a working
emulator combat path. A following uninterrupted mouse-down with camera tracking
landed eleven normal rifle hits, killed the boss and advanced mission 1994 to
Youngblood. The copied character saved 124 XP and 50 credits; the client stayed
alive. A reconnect from that saved state opened Youngblood's native objective
conversation and completed mission 1994; all objectives persisted and the
character gained the recorded 5000 mission XP. The starting boss checkpoint bypassed the camp-to-cave fight, so this
does not establish final-live boss statistics or full encounter balance.
The [Tizzik XP source reconciliation](evidence/bootcamp-tizzik-xp-source-reconciliation-20260924.json)
keeps his kill payout open. The final-week edit contains 143 XP / 50 credits
and another 71 XP / 10 credits, but hides the boss kill. TaRapedia's dated
Experience article describes hidden monster-type XP multipliers; applying
1.15 before rounding to the fitted level-10 base 124.5 would yield 143. No
source identifies that multiplier for Tizzik, so the current 124-XP emulator
reward is not claimed as final-live accurate and no reward was changed.

An [isolated S5 continuation](evidence/bootcamp-s5-normal-chain-gate-20260924.json)
accepted Calling for Reinforcements from Youngblood after that normal-health
boss turn-in. The 1994 turn-in and 1995 offer/acceptance were ordinary original-client
interactions, but the client relogged between the saved states. Mission bindings,
prerequisite evaluation and the post-1994 Youngblood presence condition also
support this handoff; immediate same-session reoffer remains untested. The
original client restored its “Locate the missing AFS soldiers”
tracker on reconnect. Ordinary movement reached the west gate, where a hostile
Thrax attacked; normal rifle fire killed it and saved 71 XP / 10 credits in the
copied character database. Mission 1995 objective 2 stayed active. This confirms
the handoff and first gate combat work in the emulator; the full travel route,
original final-live enemy values and mission timing remain unverified. A separate
ordinary movement run passed the gate and reached about (4.4,106.7,141.8),
still alive with the S5 tracker active, but did not acquire the moving Thrax
target. Manually reusing that position as a diagnostic login checkpoint placed
the client about 5.7 m lower, so later travel needs a continuous route check.
The [continuous west-route probes](evidence/bootcamp-s5-continuous-west-route-20260924.json)
then passed the gate without teleporting. Two attempted turns toward the
reconstructed soldier stopped against a rock or metal structure near
(12.4,106.5,124.5) and (8.2,101.3,129.7). The emulator navmesh reports a
complete path around this bend; these first probes had not yet traversed it.
The [follow-up soldier approach](evidence/bootcamp-s5-soldier-approach-20260924.json)
identified original-map fence endcaps at the first stop and reached the next
bend at (-20.3,98.4,124.6) through ordinary client movement. A diagnostic
continuation from that measured bend reached (-95.6,85.7,71.7) near the
reconstructed soldier's bunker, with the marker and a figure visible. The
client first stopped about 7 m from the inferred NPC beside original Forean
corpse and sandbag props. A right strafe bypassed them. In a checkpointed
continuation, ordinary movement opened the wounded-soldier Objective
Completion dialogue; Continue sent `CompleteNPCObjective`, displayed the
completion banner, and changed the tracker to Conrad's corpse. The copied
character database has objective 2 completed and objective 3 active. The
original package 2584 is confirmed, but the NPC's exact location, name and
appearance remain estimates, as [the source boundary](evidence/bootcamp-s5-wounded-soldier-source-boundary-20260923.md)
records. The Youngblood-to-dialogue route has not yet been completed as one
continuous run. No live content was changed.

A later [placement and approach recheck](evidence/bootcamp-s5-soldier-corpse-placement-recheck-20260924.json)
found no evidence for moving the soldier or corpse seed. Final-week footage
measures the objective marker, not the soldier, and edits out the corpse
interaction. The current inferred points are reachable in separate copied-client
checks; the earlier normal-hostile death near the bunker identifies a seeded
Thrax threat without establishing original combat range or balance.

Two further [accepted-1995 continuous-prefix probes](evidence/bootcamp-s5-accepted-prefix-normal-combat-20260924.json)
started from a copied Youngblood-area checkpoint with normal hostile behavior.
The first killed gate placement 198674 through ordinary rifle fire, gained
71 XP and 10 credits, and traversed the outer fence to (-94.5,85.2,67.5)
near the inferred soldier bunker without a position edit. The recruit died
there to placement 198683 before soldier dialogue. The second died at the
gate after fixed-angle shots missed a moving Thrax. A further
[visual-acquisition probe](evidence/bootcamp-s5-gate-visual-acquisition-20260924.json)
also died at the gate before firing while the Thrax closed to melee. These
runs verify more of the emulator route, but none includes same-session
Youngblood acceptance or soldier interaction. They do not establish
final-live combat balance.

An [immediate-fire gate probe](evidence/bootcamp-s5-gate-immediate-fire-latency-20260924.json)
failed before the rifle fired: the client capture-to-input loop took about
40 seconds with the Thrax already at melee range. The focus guard then
withheld fire on the death screen. This isolates controller latency, not a
measured combat balance defect.

A [fifth accepted-1995 route attempt](evidence/bootcamp-s5-accepted-prefix-fifth-route-divergence-20260924.json)
replayed the successful gate fight and again earned 71 XP and 10 credits.
The same post-gate input sequence then reached a different fence position:
(-8.3,97.9,124.3) instead of the earlier (12.3,106.5,124.6). Applying
the earlier lateral move from there entered an original structure near
(-43.6,98.5,132.1). The recruit remained alive with objective 2 active;
an orange indoor lamp triggered a false positive in the research focus
guard, ending the run before the soldier. Subsequent route control needs
to branch on measured /loc at the fence rather than replay fixed inputs.

After the deleted client test environment was rebuilt, a
[copied-server client smoke](evidence/bootcamp-s5-rebuilt-adaptive-gate-and-target-lock-20260924.json)
reached playable HUD and passed `/loc` and focus checks. The following
accepted-1995 attempt stopped at the gate with the hostile still alive and
objective 2 active; the recruit reached (70.4,109.0,139.5) after the
opening walk, short of the position in the earlier successful gate fight.
Original 1.16.5.0 keymapping and targeting bytecode show that Tab locks a
target already under the reticle; it does not acquire the nearest Thrax.
No combat or mission values were changed from this input result.

A [corrected gate aim probe](evidence/bootcamp-s5-gate-nameplate-crop-20260924.json)
reached (61.7,110.2,140.3) and visibly put “Thrax Infantry Initiate” under
the reticle after a bounded camera correction. The test controller cropped
the target name before its full height, falsely rejected that positive image,
and stopped before Tab or firing. An offline crop adjustment reads the name
on the positive image and stays negative on the preceding image. This
identifies a harness detection error; the gate-to-soldier route and
same-session Youngblood acceptance are still unverified.
Two calibrated follow-ups also stopped before aiming: one displayed `/loc`
coordinates that the thresholded OCR failed to read, and the next displayed
no coordinates after the command at all. Their sealed copied-client captures
and restored databases are listed in the same evidence note. Neither shows a
gameplay-rule failure.
Removing the gate `/loc` wait in a further bounded run let the original client
acquire the moving Thrax immediately. Tab retained its nameplate and two normal
rifle holds killed it, adding 71 XP and 10 credits. Ordinary movement then
reached the fence waypoint, where another `/loc` command produced no visible
coordinates, so the controller stopped before branching toward the soldier.
The same-session accepted-1995 route still ends there; no soldier or corpse
transition is established by this run.
An offline comparison of earlier waypoint captures shows `/loc` toggles a
persistent coordinate overlay in this client. The fence command switched off
the overlay enabled at login. Subsequent route checks should read it without
issuing another toggle; this explains the intermittent harness coordinate
failures.
The next one-toggle run kept the overlay visible at the gate, reading
(59.9,110.3,140.4). The moving Thrax remained left of the fixed aim points,
and the controller stopped before firing. This is a target-tracking limit in
that bounded run, not a demonstrated enemy-stat or mission defect.
A subsequent six-capture, five-turn sweep still left the melee Thrax left of
the reticle while it tracked the player. Serial screenshot and OCR latency
cannot reliably acquire this moving target at that distance. The gate combat
did succeed in the prior direct-input run; the combined gate-to-soldier route
remains unverified.

At a shorter three-second walk, the [W3 range probes](evidence/bootcamp-s5-w3-gate-range-probes-20260924.json)
kept the Thrax visibly farther away. One capture displayed the client target
panel at the top of the screen while the controller's close-range nameplate
crop stayed negative; the repeated run approached on a different heading and
never acquired it. [Original client targeting code](evidence/bootcamp-s5-original-client-autofire-targeting-20260924.md)
permits reticle acquisition while primary fire is already held, providing a
retail-valid faster control path. A subsequent isolated W3 run held ordinary
primary fire during short camera turns. The original client sent `SetTargetId`,
the gate Thrax died, and the copied character gained 71 XP and 10 credits.
The same run reached the fence at (0.8,99.5,128.7) with objective 2 still
active; it stopped just outside the controller's earlier x<=0 route branch.
This demonstrates the current emulator gate combat path, not final-live
enemy balance or completion of the soldier route.
The [original-client mouse-look audit](evidence/bootcamp-s5-original-client-mouselook-boundary-20260924.md)
confirms that captured mouse events reach a native handler; the available
Python code gives no fixed pixel-to-yaw conversion. Identical test mouse
commands landed at different W3 bearings, so route checks use rendered and
`/loc` feedback rather than assuming a fixed turn angle.

A further [continuous gate-to-outpost run](evidence/bootcamp-s5-gate-to-outpost-corridor-20260924.json)
used that fire sweep, passed the original-map fence by a measured short
forward probe, and reached the outpost corridor in the same original-client
session. Its final screenshot visibly reads `/loc` (-61.8,87.9,75.3); OCR
misread the X coordinate, so the fail-closed controller stopped before the
outpost fight or wounded soldier. The copied database was restored with a
WAL-safe reset after a separate residual-WAL checkpoint had modified a
previous baseline copy. The live database was not involved. This extends
emulator route verification, while one-session soldier/corpse completion and
final-live placement fidelity remain open.
An [original-map corridor audit](evidence/bootcamp-s5-outpost-corridor-map-los-20260924.json)
finds a complete 16.6 m navmesh route from that point to the filmed player's
outpost engagement area, but a walkable path does not establish a clear shot
or safe exposure. The current emulator has no outpost cover triangles and its
inspected hostile acquisition and direct ranged attack paths have no hard
geometry line-of-sight check. These are concrete fidelity gaps to investigate;
the source evidence does not yet establish the final-live ballistic rule or
support a guessed combat change.

The [next bounded outpost probe](evidence/bootcamp-s5-outpost-short-probe-20260924.json)
again cleared the gate and reached (-87.8,85.9,75.4) by ordinary client
movement. The recruit was alive while taking hits, with objective 2 still
pending; this run stopped before soldier interaction by design. Its position
captures preserve ambiguous negative-X OCR as untrusted rather than forcing
a false route coordinate. A separate [original-client weapon audit](evidence/bootcamp-s5-outpost-weapon-los-range-audit-20260924.json)
confirms that primary fire permits blind shots and does not apply the
client's hard range check. It does not establish the final-live server hit
rule, so no LOS or range rejection was added.

An [isolated accepted-Move trace](evidence/bootcamp-s5-accepted-route-telemetry-death-20260924.json)
followed one accepted-1995 client run from the gate to the outpost. It showed
that forward input at the corpse/sandbag scenery slid the recruit south instead
of toward the inferred wounded soldier; the outpost Thrax killed him before
conversation. The trace is copied-emulator instrumentation, not a final-live
movement record, and supplies no basis for changing enemy stats.

An [outpost fight comparison](evidence/bootcamp-s5-outpost-engagement-boundary-20260924.json)
also limits combat conclusions from the earlier soldier-area death: the
final-week footage shows a level-3 shotgun user after an edited level-up,
whereas the copied run used a level-2 rifle recruit. The original outpost
Thrax position is measured only during engagement. No weapon, level, enemy
stat or placement change is justified by that comparison.

A source audit found no justified S5 enemy-stat change: final-live footage
shows level-2 and level-1 Thrax with laser fire in this segment, consistent
with the seeded levels, class and weapon family. The seeded 555 HP, armor,
range, cadence and 10–15 damage remain labelled analogues; the footage does
not resolve their numerical values. The recorded deaths followed prolonged
inspection under outpost fire or missed moving targets, so they cannot be
used to tune retail damage. See the original [footage events](evidence/bootcamp-d11-footage-events.json)
and [courtyard pressure audit](evidence/bootcamp-courtyard-combat-pressure-audit.json).

The [soldier and corpse interaction audit](evidence/bootcamp-s5-soldier-corpse-interaction-boundary-20260924.md)
separates the final-week objective marker measured at
`(-104.6,86.1,70.5)` from the inferred soldier position. The surviving
footage cuts across both the soldier conversation and corpse use, so their
final-live positions and exact triggers remain open. Checkpointed client runs
establish a usable north-side approach to the reconstructed soldier and an
east-side approach to the reconstructed corpse; they do not establish an
uninterrupted route from Youngblood's 1995 acceptance.
The [original-client use audit](evidence/bootcamp-s5-soldier-client-use-protocol-20260924.md)
requires a selected NPC with a visible talk prompt inside the client's 5 m
conversation range. A yellow mission indicator alone cannot confirm a usable
soldier target.

A later [continuous accepted-1995 client run](evidence/bootcamp-s5-continuous-soldier-dialogue-boundary-20260924.json)
killed the gate Thrax with ordinary rifle fire, took the measured north-side
route, displayed the Human talk prompt, and opened the wounded soldier's
Objective Completion dialogue by right-click in that same session. The
controller missed the small Continue button, so objective 2 stayed pending;
the subsequent corrected-button attempts stopped at measured route bounds or
lost the talk prompt under outpost fire. This establishes gate-to-soldier
dialogue in the copied emulator, while same-session objective 2 → 3 and
Conrad use remain unverified. The copied Game trace logged accepted Move
positions only; it is not evidence of final-live server coordinates.
One later bounded Continue attempt stopped when a 0.4-second north strafe
advanced only 0.24 m near the sandbags. A prior run advanced from nearly the
same point, so this single result does not establish a fixed map obstruction.
A yaw-telemetry retry passed copied-server startup and killed the gate Thrax,
but the isolated launcher received unexplained SIGTERM at the fence, before
the planned north-side correction. It adds no soldier-route outcome.
A separate [staged-position diagnostic](evidence/bootcamp-s5-sandbag-yaw-staged-diagnostic-20260924.json)
used an inferred northward heading and moved through the sandbag area with
ordinary client input. It exposed a telemetry controller error: an immediate
post-hold sample can still be a delayed row from the prior hold. The earlier
continuous run's zero-displacement row was itself after its D400 input, so its
stall remains a distinct unresolved observation. The staged position and
heading are diagnostic edits, not continuous or final-live route evidence.
Four [post-hold timing retries](evidence/bootcamp-s5-post-hold-timing-retries-20260924.json)
then used the corrected private reader in accepted-1995 client sessions. One
stopped at an earlier outpost corridor point, one crossed the north sandbag
band but remained short of the soldier, and two stopped after short forward
probes from variable corridor starts. Each guard preserved objective 2 pending;
none attempted the Continue click. The recorded post-hold samples support the
reader fix, while the soldier-to-corpse sequence remains unverified.
Five further [soldier-approach retries](evidence/bootcamp-s5-soldier-approach-retries-20260924.json)
used settled accepted-Move checks at every movement checkpoint. One reached
the original client's Human talk prompt at `(-101.02,85.97,70.77)`, but the
outpost Thrax killed the copied recruit 0.188 seconds after the target
screenshot command began and before the delayed right-click. Four other runs
stopped at measured route guards. These runs refine the private test route;
they do not complete the soldier conversation or support enemy-stat changes.
Six [later corridor retries](evidence/bootcamp-s5-corridor-fastprobe-retries-20260924.json)
tested quicker accepted-Move probes from variable north-fence starts. One copied
client died under outpost fire; five stopped at private route guards, including
an alive point at `(-91.38,85.17,72.22)`. No run reached a verified Continue
click. The diagnostic controller now covers the observed early-corridor starts,
but that revision is untested and does not establish final-live placement or
combat values.
Five [further copied-client attempts](evidence/bootcamp-s5-controller-continuation-20260924.json)
exposed larger gate and fence position variation from the same fixed inputs.
Each stopped at a private route or initial OCR guard before soldier Continue.
The next route check needs feedback steering from accepted client positions;
more narrow guard extensions do not establish mission fidelity.
A [feedback-route plan](evidence/bootcamp-s5-feedback-route-plan-20260924.json)
uses the original navmesh and the two observed fence endpoints to define bounded
short client inputs toward the lower corridor. The x=26 branch has not yet been
tested; these are private steering bounds, not final-live waypoints.
The [first feedback-controller trial](evidence/bootcamp-s5-feedback-fence-collision-20260924.json)
cleared the ordinary gate and reached a fence wall at `(11.01,104.05,129.58)`.
Two fresh accepted W800 packets made no further westward progress, and the
client screenshot shows the obstruction.
A [single copied-client wall-bypass trial](evidence/bootcamp-s5-feedback-wall-bypass-20260924.json)
cleared that obstruction with D400, a camera turn and W600. Subsequent accepted
movement reached `(-16.72,97.67,125.46)` in the lower corridor, but the private
55-second descent deadline stopped the controller before soldier dialogue.
Mission 1995 objective 2 remained active. These are diagnostic steering points,
not original route or NPC placement evidence.
A [75-second controller retry](evidence/bootcamp-s5-feedback-75s-fence-variation-20260924.json)
landed at a different fence point, then stopped because its northward
reposition was wrongly judged by westward progress. It made no soldier or
objective transition. This tests private steering only.
A final guarded upper-fence probe raised Z again but met different geometry
at X 12; its controller stopped before the west probe. The same evidence record
contains the second manifest. No same-session soldier transition was observed.
An [original-navmesh geometry audit](evidence/bootcamp-s5-upper-fence-route-geometry-20260924.json)
shows the two upper-fence runs steered toward lower Z and the wall; its distinct
candidate moves toward Z 130–131 while still near X 16–18. Those points guide
only a guarded private client probe, not original gameplay data.
A [subsequent uninterrupted copied-client run](evidence/bootcamp-s5-soldier-converse-request-no-dialogue-20260924.json)
entered the lower fence branch naturally and reached the soldier talk prompt.
Right-click produced a server `RequestNPCConverse` call, but no dialogue or
Continue appeared and objective 2 remained active. The precise handler or
data cause is unresolved. A [comparison with a prior successful soldier
dialogue](evidence/bootcamp-s5-soldier-converse-no-dialogue-20260924.json)
shows that no gameplay binding change is justified until the target entity ID
and outgoing reply are captured.
The [original-client conversation trace](evidence/bootcamp-s5-converse-client-protocol-20260924.json)
shows that right-click uses the current usable entity and that an empty or
unsupported `Converse` reply can leave the client without a dialogue window.
One bounded retarget retry stopped at a different X12 fence entry before the
soldier; its archive is attached to the request record. It adds no new NPC
packet evidence.
A [checkpointed request/reply trace](evidence/bootcamp-s5-soldier-converse-traced-checkpoint-20260924.json)
then used a copied character at the exact previously accepted soldier position.
One normal right-click resolved placement 198675/package 2584, sent the
`1995/2/1` objective topic, and opened the original client’s Objective
Completion window. The copied binary and DB were restored. This verifies the
conversation path at a checkpoint; the older uninterrupted no-dialogue request
still lacks its target and reply trace.
A [second ordinary-client checkpoint run](evidence/bootcamp-s5-soldier-continue-corpse-guard-20260924.json)
clicked Continue and persisted objective 2 complete with objective 3 active in
the same session. Its corpse approach exceeded a private route guard before a
use prompt appeared; no corpse click was made.
A [guarded follow-up from the same soldier checkpoint](evidence/bootcamp-s5-soldier-corpse-same-session-20260924.json)
completed both soldier Continue and Conrad corpse use through the original client
in one isolated session. Copied mission 1995 has objectives 2 and 3 complete,
objective 1 active with a persisted 600-second timer, and the client showed
`00:09:59`. A verified post-use copied character DB was archived for a timed
wreck continuation. The run begins at a diagnostic checkpoint, so the full
new-character route remains open.
A [no-reset timed continuation](evidence/bootcamp-s5-timed-wreck-collision-stop-20260924.json)
resumed from the archived post-use DB with `00:06:25` left and reached an
accepted high-bank position `(-105.45,83.77,43.03)`. Its next W hold made no
progress in the controller's first sample, so it stopped with `00:04:16`
remaining. The archived trace contains a later accepted Move to
`(-107.20,83.77,41.27)` from that hold. The wall screenshot does not establish
a collision; the guard sampled too early. No bomb prompt or use occurred.
The [packet-timing recheck](evidence/bootcamp-s5-trench-delayed-move-audit-20260924.json)
records the delayed accepted position and calls for a bounded fresh-packet wait
before changing the route.
A [timer-anchor-reset private diagnostic](evidence/bootcamp-s5-wreck-anchor-reset-diagnostic-20260924.json)
then used the corrected accepted-packet wait, crossed the dry high bank, and
reached the wreck approach at `(-182.48,95.30,-33.20)`. It stopped at a broad
sandbag barrier with `00:02:24` visible and no bomb prompt. The timer reset
was confined to a copied DB and is not an original-timer completion; the
[barrier audit](evidence/bootcamp-s5-wreck-sandbag-approach-audit-20260924.json)
records its movement trace and a navmesh candidate that first goes north,
then west around the sandbags. Client traversal of that bypass remains
unverified. The earlier [V14 run](evidence/bootcamp-s5-timed-loc-v14-20260924.json)
records an unreset corpse-to-bomb-to-S6 success, but its source captures were
in the missing backup tree, so this new run cannot independently recheck it.
A [north-west copied-client detour](evidence/bootcamp-s5-sandbag-northwest-diagnostic-20260924.json)
accepted moves beyond the first stop to `(-185.65,94.31,-28.24)`, then found
another visible sandbag line. Its next input produced no fresh accepted move;
with `00:02:55` left on the reset diagnostic timer, it ended without a bomb
prompt or use. This does not overturn the earlier V14 completion record.
The [next isolated probe](evidence/bootcamp-s5-second-sandbag-probe-interrupted-20260924.json)
stopped earlier on the dry-bank route: its first fresh Move appeared to slide
away from a waypoint, but a later accepted Move from the same hold reduced the
distance. The route guard was premature, so this probe did not test the
second sandbag opening.
The [guard-corrected retry](evidence/bootcamp-s5-second-sandbag-retry-sigterm-20260924.json)
received SIGTERM while the copied client was still loading, before any route
input. The isolated databases and DLL were restored; the termination cause and
second-sandbag passage remain open. A [runner lifecycle audit](evidence/bootcamp-s5-isolated-sigterm-lifecycle-audit-20260924.json)
found no Game exception or identified signal sender; an outer process-group
interruption is the leading, still unproved explanation.
A [detached-supervisor retry](evidence/bootcamp-s5-detached-second-sandbag-route11-20260924.json)
stayed live for five minutes and passed the earlier packet-timing stop, but
ended at a steep rock slope near `(-168.09,91.41,-16.88)`. Its final accepted
move gained 0.23 m toward the waypoint, below the guard threshold; hard
collision and the later sandbag bypass remain unverified. The supervisor also
terminated the launcher just after its stop result, before normal status
write, a separate documented cleanup race.
A [rock-slope comparison](evidence/bootcamp-s5-rock-slope-normal-client-bypass-candidate-20260924.json)
finds an earlier copied-client accepted climb from a point only 0.46 m west
of this stop. Those recorded positions give the next short waypoint test;
the first adjustment is smaller than the current movement guard threshold.
A [bounded client retry](evidence/bootcamp-s5-rock-slope-v2-route11-divergence-20260924.json)
reached the first local waypoint approach, but aiming directly at that nearby
point sent two accepted moves farther west and away from it. The guard stopped
there, before the second sandbag. This is route-controller divergence, not
evidence that the rocky slope is impassable; the copied harness was restored.
A [far-goal client retry](evidence/bootcamp-s5-rock-slope-v3-northwest-path-20260924.json)
then climbed the slope, passed the earlier northwest stop, and reached
`(-203.17,95.95,-36.54)` with about 4:30 remaining on the reset copied timer.
Its driver stopped because it still expected the old stop coordinate. Sandbags
remained visible ahead; the wreck corridor and bomb were not yet traversed in
that uninterrupted client session.
A [v4 continuation](evidence/bootcamp-s5-v4-northwest-progress-threshold-20260924.json)
again reached the rocky wreck approach. Its first northwest holds moved the
client to `(-182.71,92.45,-25.96)`, but a near-waypoint progress threshold
rejected the 0.222 m target improvement and stopped the driver. This is a
controller threshold, with no bomb interaction in that run.
A [v5 client retry](evidence/bootcamp-s5-v5-northwest-waypoint2-stop-20260924.json)
passed that threshold, then stopped beside the next sandbag line at
`(-187.58,95.16,-30.24)`: one forward hold changed yaw but yielded no
accepted position change. A prior copied-client passage used a slightly
more northerly, lower approach here. The screenshot and single hold do not
establish an impassable collider; the bomb remains untested in this run.
A [staged local backtrack](evidence/bootcamp-s5-v6-local-backtrack-command-race-20260924.json)
then used ordinary client controls from that stop to reach
`(-186.20,94.79,-29.05)`, close to the prior successful approach. It began
the next move, but an isolated harness command-file parse race stopped the
diagnostic before a sandbag passage result. This copied-position test does
not establish an uninterrupted mission-timer route.
A [second staged local probe](evidence/bootcamp-s5-v7-local-northwest-point-reached-20260924.json)
used the earlier successful heading at that backtrack point and reached
`(-189.02,94.70,-29.72)` in two short ordinary moves, within about 0.08 m
of the earlier passage trace. This confirms local reachability under the
staged heading; a continuous fallback from the sandbag stop remains untested.
An [integrated v8 run](evidence/bootcamp-s5-v8-slope-waypoint12-divergence-20260924.json)
ended earlier on the rocky slope: two accepted moves from
`(-170.72,94.46,-21.71)` increased distance to the wreck approach target.
Its northwest fallback and bomb path were never exercised. The starting
position was close to v3's successful slope point, but the client heading
was about 0.88 radians different; a short guarded heading adjustment needs
its own client verification.
A [staged local slope probe](evidence/bootcamp-s5-v9-local-slope-point-reached-20260924.json)
then began from that v8 accepted position and yaw. Three short ordinary
client moves reached `(-173.21,92.75,-20.62)`, about 0.46 m from v3's
first wreck-approach point. This verifies the local adjustment, while the
copied position and timer reset leave continuous route completion open.
The [armed-bomb interaction audit](evidence/bootcamp-s5-armed-bomb-interaction-boundary-20260924.json)
finds a generic 114-to-113 disarm transition in the original client and an
armed-state use prompt in the prior footage transcription. The emulator
currently ignores a second use while armed and restarts the fuse after an
instance rebuild. Original S5 second-use and reconnect behavior remain
unverified, so neither behavior has been changed on this evidence alone.

A [checkpointed corpse approach](evidence/bootcamp-s5-corpse-approach-20260924.json)
then confirmed the new tracker state and moved beside the inferred Conrad
corpse through ordinary client input. Initial direct paths stopped at scenery.
The [east-side follow-up](evidence/bootcamp-s5-corpse-use-20260924.json)
reached the usable prompt by ordinary movement; a diagnostic login at that
measured position used the object, completed objective 3, and started the
visible 9:59 bomb countdown. Its copied database persisted objective 3 complete
and objective 1 active with a 600-second timer. A single uninterrupted
soldier-to-corpse-use run and the original corpse placement remain unverified;
the current inferred placement did not need a usability move.
The [original-client corpse-use audit](evidence/bootcamp-s5-corpse-original-client-use-protocol-20260924.json)
shows that the client checks the selected usable, its state, 3D range to the
model's DAMAGE1 point, and native line of sight before sending
`RequestUseObject`. The current analogue class has a nominal 3 m use range;
that does not establish Conrad's final-live class, range, or placement.
The older checkpointed corpse-use record's 19 primary source paths are
[currently unavailable](evidence/bootcamp-s5-corpse-source-availability-20260924.json)
in this workspace. Its retained summary is derivative evidence until those
artifacts are recovered or the isolated use is repeated and archived.
The [reinforcement pad-hold correction](evidence/bootcamp-s5-reinforcement-pad-hold-20260924.json)
reconciles the scripted walk with the corrected final-week frame ledger. Objective
1995/1 completes at 86.8 s, while the group stands on the pad through 98.333 s.
The forward `BootcampReinforcementPadHold` migration removes the immediate
walk rules for 1995 and inferred retry 2005, retaining their placements and
destroyed-wreck condition. Departure after the footage cut remains unknown;
the cited original frame mosaics are currently unavailable for reinspection.
On 2026-09-24 the isolated migration image was deployed with the DIT overlay
after copied-DB startup verification; the live world DB now has the pad-hold
migration and no walk rules 1985015/1985017. The unrelated 13:00 Wilderness
migration was excluded and remains pending.
The [beam-in timing audit](evidence/bootcamp-s5-reinforcement-beam-timing-20260924.json)
keeps a separate gap open: current conditional presence creates ordinary solid
reinforcements as soon as the wreck is destroyed, while the frame ledger first
shows a Forean nameplate about 4.9 seconds later, then a blue cone and a gradual
ghost-to-solid appearance. The [original-client beam binding audit](evidence/bootcamp-s5-reinforcement-beam-client-binding-20260924.json)
maps two human dropship classes to their state animations, packages and beam
assets, but neither class appears among the static boot-camp map entities.
No source yet binds either class or the original packet order to S5. The
[reconnect-safe delay design](evidence/bootcamp-s5-delayed-presence-architecture-20260924.json)
remains a design only: first visible nameplate does not establish the original
server spawn or targetability time.
The [navmesh route candidate](evidence/bootcamp-s5-wreck-route-candidate-20260924.json)
records a complete corridor from that inferred corpse to the inferred bomb
and damaged pad. It is planning evidence only. A
[checkpointed timed client attempt](evidence/bootcamp-s5-timed-wreck-route-20260924.json)
followed ordinary movement southwest but dropped into water beneath the bridge;
the copied 1995 mission failed when its wall-clock timer expired, before any
wreck interaction. A [higher-bank follow-up](evidence/bootcamp-s5-higher-bank-wreck-approach-20260924.json)
then reached the wreck by ordinary client movement from restored checkpoints,
with the timer reset diagnostically between runs. Two clicks from the far
face produced no use request; a short ordinary move to the
[near face](evidence/bootcamp-s5-bomb-acquisition-from-route-20260924.json)
showed the use prompt, sent `RequestUseObject`, and completed objective 1995/1
after the explosion. The current bomb placement did not require a correction.
From that ordinary reached bomb point, a short walk opened Van Valkenberg's
objective dialogue; Continue from the ordinarily reached pad position
[transferred the character to Alia Das](evidence/bootcamp-s5-postbomb-ordinary-exit-20260924.json),
set the skip flag, and showed Calling for Reinforcements complete with the
Training Day offer. A later [V14 timed run](evidence/bootcamp-s5-timed-loc-v14-20260924.json)
verified corpse-to-bomb use and the Alia Das transfer in one original-client
session. The earlier soldier-to-corpse route and original encounter combat
remain unverified.
A [copied Alia Das route segment](evidence/bootcamp-s6-rogers-ordinary-route-20260924.json)
accepted Training Day and walked from the arrival point to (+853.625,294.016,+378.868),
about 8.8 m from seeded Rogers, without a movement edit during the run. The
next diagnostic logins reached server `MapLoaded` but did not render a playable
HUD before their harness timeouts; Rogers interaction from that earlier route
was unverified. A [longer loading diagnostic](evidence/bootcamp-s6-wilderness-loading-diagnostic-20260924.json)
measured 231.37 seconds from character switch to `MapLoaded`; the client still
showed Wilderness loading at its 240-second HUD check. The Game connection
succeeded and no server error was identified, so this does not establish a
server fault. A later [S6 relog continuation](evidence/bootcamp-s6-after-v14-ordinary-rogers-20260924.json)
from V14's saved Alia Das arrival became playable, accepted Training Day,
walked to Rogers without a coordinate edit, and turned in 1995 in the original
client. The Mission Completed banner and copied database confirm the result;
the separate relog boundary remains explicit.
A [post-Rogers Training Day continuation](evidence/s6-training-day-ordinary-route-kincaid-20260924.json)
used the preserved character state and ordinary controls to walk from the
Alia Das arrival position to Kincaid at `(767.01,294.14,386.97)`. The original
client showed his objective dialogue, persisted objective 1526/1 complete,
then displayed the mission panel with 120 credits and two pistol choices. Its
900-second isolated cap ended before a reward was selected. Earlier
[near-Kincaid checks](evidence/client-training-day-turnin.json) separately
verified reward selection and persistence; those used a diagnostic position.
A [second isolated continuation](evidence/s6-training-day-ordinary-route-reward-relog-20260924.json)
walked again from the saved arrival position, selected the Vextronics Pistol,
completed 1526, then logged out and back in. Its copied database persisted
mission state 4, credits 360→480 and item template 116929 with module 900221;
level 2 and 8695 XP did not change. These are current emulator observations.
The 120-credit value conflicts with a contemporary 180-credit listing; the
[source audit](evidence/training-day-credit-source-audit-20260924.json)
cannot resolve the final-live amount. The template/module IDs remain
reconstructed. Natural class eligibility is
still open pending the missing XP source.

An unrelated [isolated startup race](evidence/map-channel-registry-race-20260924.json)
logged a concurrent dictionary-copy exception in `MapChannelManager.Channels()`.
Registry snapshots and production mutations now share a lock in source;
private-instance, disconnect and logout tests pass 18/18. Two isolated
original-client logins on a candidate build reached a playable private boot-camp
map without a worker error. The source fix was included in the
[2026-09-24 DIT-overlay rollout](evidence/live-preservation-rollout-20260924a.json).
The single exception does
not establish the cause of the failed client attempt in that run.

## 2026-09-22 — Boot camp rifle alternate melee reconstruction (deployed)

The original compatibility client identifies rifle melee as action `(174,5)`:
200 ms windup, 133 ms recovery, 4 m range and 250 ms reuse. Final-week
boot-camp footage shows 82 Physical melee damage in a Shinobi Rifle tooltip;
the filmed server template ID and minimum/base damage remain unknown. The
reconstruction replaces template 13713's unsupported pistol-like alternate fields
with the observed tooltip amount and original rifle action, labelled as an
inferred template binding. Runtime now accepts declared melee alternate
requests, charges no ammunition and checks range at impact. It does not enable
unresearched alternate attack families. See [the rifle evidence and limits](bootcamp-rifle-melee.md).

Under .NET 5, all 33 weapon attack lifecycle tests pass, including new melee
timing/range/no-ammo cases. The data migration changes only the intended rifle
fields and rolls back exactly. An isolated recovered original client displayed
the expected 106 primary/82 Physical melee tooltip. A copied live database
migrated cleanly, then image `rasa-dit-test:20260922b` was deployed and the
live game reached `Server ready!`. A later isolated client pass pressed F against
a GM-spawned Thrax, recorded three `RequestWeaponAttack` calls, killed it and
left rifle ammo at 15. XP and credits increased as shown in
[the targeted-client record](evidence/bootcamp-rifle-melee-targeted-client.json).
The first [client check](evidence/bootcamp-rifle-melee-client-check.json)
remains the tooltip and deployment record.

The recovered client's alternate attack path also bypasses the normal empty
magazine and jam checks for rifle melee (`MeleeAttackMovement` uses no ammo).
The server had still rejected both cases. The guards now apply only to primary
attacks; 33 weapon attack lifecycle tests pass under .NET 5, and image
`rasa-dit-test:20260922c` reached `Server ready!`. See
[the bytecode locations and limits](bootcamp-rifle-melee.md).

## 2026-09-22 — Elder Moawi conversation repair

Mission 1407 objective 1 is active on the local character beside Moawi, and the
world has the required package 113 binding. The world seed gives creature 38
client class 6163 (`Redshirt_Forean_Elder`), whose client augmentation list is
`1,59`: it has no NPC dialogue augmentation 52. This prevents the client from
opening his conversation and showing the objective conversation marker. The
`MoawiDialogueClass` migration assigns original client class 28415, which has
the same elder mesh and class flags plus augmentation 52. This class is an
explicit analogue; Moawi's exact final-live entity class is unverified. Field
provenance is in [the Moawi class manifest](evidence/moawi-dialogue-class.json).
The mounted SQLite world database received the same one-row correction on
2026-09-22, and the game container restarted and reached `Server ready!`.
The mounted character database later recorded Blizz's mission 1407 complete with
both objective 1 (Moawi conversation) and objective 10 complete at
2026-09-22 23:28:30 UTC. That is completion evidence from saved state; the
exact client interaction that produced it was not captured.
An isolated 2026-09-23 replay with objective 1 injected active rendered a
yellow marker above the elder. Scripted right-clicks did not complete the
objective, and the game log contains no `RequestNPCConverse` call from them.
This is an inconclusive input/targeting check, so direct client conversation
validation remains open; the replay
used a diagnostic character and does not reverse the live character's saved
completion. Inputs, screenshot and database hash are in
[the Moawi class manifest](evidence/moawi-dialogue-class.json).
A second isolated client replay on 2026-09-23 again rendered Moawi's yellow
objective marker. The camera did not acquire his talk prompt, and the server
received no `RequestNPCConverse`. A later replay from a navmesh-verified
walkable approach reached Moawi's prompt, opened his original-client objective
dialogue, and advanced objective 1 to complete with objective 10 active after
Continue. The server log records `RequestNPCConverse` and
`CompleteNPCObjective`; the stopped copied character database confirms the
transition and passes SQLite integrity check. This validates the implemented
interaction for the tested compatibility client, while the exact final-live
entity class remains unverified. Captures and hashes are in
[the direct Moawi replay](evidence/client-moawi-nav-verification.json).
The mounted Blizz character separately has mission 1407 state 4
with both objectives complete, so its missing Moawi marker is expected for
that character.
A [fan-site image labeled Council Elder Moawi](https://www.ellatha.com/tr/npcview.asp?id=6&key=Council+Elder+Moawi)
shows the red robe, gold shoulder piece and crystal staff rendered by the
28415 analogue in the isolated client. Its capture date and exact entity class
are unknown; the image supports appearance only.

On 2026-09-23, the mounted Blizz character still had mission 1407 complete
with both objectives complete, and mission 1069 Receptive Reception active at
its first Logos-shrine objective. Moawi therefore has no current objective
marker for this character. A 2007 TaRapedia revision records 1407 as 1069's
requirement; `WildernessHubReceptiveGate` now enforces that prerequisite for
future offers. The same row was applied to the mounted SQLite world database,
and the restarted game server reported `Server ready!` and 0 content gaps.
The source and remaining final-live uncertainty are in
[the gate manifest](evidence/wilderness-receptive-gate.json).

The 1069 shrine recovery path also had a server defect: with more than one
pending object use, it could consume the first object triggered by the player
instead of the Logos entity named by that action. Source now matches the exact
entity, and a migrated-seed .NET 5 test completes objective 1 and reveals
"Return to Solis" while leaving the other shrine's pending use intact. Image
`rasa-dit-test:20260923b` deployed on 2026-09-23, connected to auth and reached
`Server ready!` with 397 content rows and 0 gaps. See
[the shrine recovery record](evidence/wilderness-receptive-shrine-recovery.json).
An isolated original-client replay on the later deployed `20260923e` binaries
has now used the Enhance shrine. The client sent `RequestUseObject`, displayed
"Added Logos Element Enhance to Tabula," and the copied character database
recorded Logos 10, objective 1 complete and "Return to Solis" active. Acceptance
and travel were bypassed in this diagnostic run; the full Caverns route and
Solis/Apirka conversations remain open. The same evidence record holds its
screenshots, log and database hashes.
Three isolated follow-up positions near the seeded Solis failed to acquire a
talk prompt because walls and courtyard props obstructed the camera. No
`RequestNPCConverse` reached the server and objective 2 stayed active; this is
an inconclusive targeting test, not proof that Solis is unreachable. A later
navmesh query found a continuous floor path from `(810,302.12,499)` to the
seeded Solis position, and the original client reached the hut from that
approach. The same replay directly completed Moawi's adjacent objective; a
Solis objective conversation and its saved transition are still unverified. The
[approach record](evidence/client-receptive-solis-approach.json) preserves the
captures and the already known conflict between the world seed and an older
named location report. Final-live location reconciliation is still required.
The follow-up original-client checks found named Solis spawned but hidden by
Moawi's hut stonework from the tested front approaches. TaRapedia's dated
Alia Caverns-side coordinate, a second map pin and the seed's unnamed Forean
shaman position instead converge near `(786.87,287.32,581.47)`. In a disposable
world copy the named Solis rendered there with the active objective marker.
`SolisCavernsPlacement` now moves his named pool 184 to that floor and disables
the overlapping unnamed pool 92. This is an inferred identity and position
binding, not a recovered final-live placement; the exact final-live position
and Solis conversation transitions are still open. See
[the field provenance and client captures](evidence/solis-caverns-placement.json).
The migration compiled in `rasa-dit-test:20260923f`, passed an isolated
Up/Down row check, and deployed on 2026-09-23. The live game reached
`Server ready!` with 0 content gaps, and the mounted world database records
migration `20260923020000_SolisCavernsPlacement`, the intended pool values and
SQLite integrity `ok`.

A dated pre-shutdown TaRapedia revision lists Receptive Reception at level 4;
the earlier reconstruction seeded level 5 without direct support.
`WildernessHubReceptiveLevel` corrects the mission row to 4. The matching data
update was applied to the mounted SQLite world on 2026-09-23 and the restarted
game server reached `Server ready!` with 0 content gaps. The exact shutdown
level remains unverified. See [the level evidence](evidence/wilderness-receptive-level.json).

The next Wilderness chain mission, 479 Forming Alliances, is deployed with a
per-field evidence manifest, but is not yet verified in the original client.
The original client item class for its twelve Thrax Hearts is 10346, but both
templates 2285 and 16540 map to that class. The original client mission log
specifies Thrax Soldiers; a contemporary guide reports random drops without a
probability. The existing server has global
creature loot rows; the server now supports mission-conditional loot rolls tied
to an active objective. The recovered client has a
separate opcode-566 item counter keyed by item class; the server now sends it
after successful loot pickup, saves partial progress and includes it in mission
log snapshots. Collection items are consumed in the same transaction as mission
completion and rewards; that removal is an inference from the original mission
text, with rollback and partial-stack tests. The deployed seed uses an explicitly
estimated 50% drop chance, heart template 2285 and vest counterpart 13738.
The vest is the nearest original class and has the reported armor value, but its
Luminar name and effects remain unverified and unimplemented. The seed also
gates mission 427 on completion of 479. Image `rasa-dit-test:20260923c` reached
`Server ready!` with 0 content gaps after both world migrations; the mounted
SQLite database passed `PRAGMA integrity_check`. The full .NET 5 suite passed
1,319/1,319 tests. The remaining source conflicts are
recorded in [the Forming Alliances research](evidence/wilderness-forming-alliances-research.json).

The next mission, 1390 Conscientious Objector, now requires completed Forming
Alliances. A pre-shutdown TaRapedia revision explicitly names that requirement;
the gate is deployed in image `rasa-dit-test:20260923d`. The original client
identifies 1391 as **Bug Em**, rather than a second Conscientious Objector
branch. Missions 1392 and 1393 are the two Part Two variants. See [the gate
evidence](evidence/wilderness-conscientious-gate.json).

The original client's `PerformNPCChoice` call carries choice indexes 1 and 2.
Its mission text maps 1 to releasing Milpas and 2 to arresting him; the two
Part Two reports contain distinct release/arrest prose. The source now routes
those choices through only their corresponding Quillas, escort and Apirka
objectives, and gates Part Two 1393 on release or 1392 on arrest. The previous
transition rows crossed both routes and skipped the escorts. This correction
and the observed 8,000 XP / 800 credit Part Two rewards are recorded in [the
branch evidence](evidence/wilderness-conscientious-branches.json). Image
`rasa-dit-test:20260923e` is live; the full .NET 5 suite passed 1,322/1,322,
the SQLite world passed `PRAGMA integrity_check`, and the server loaded 404
content rows with 0 gaps. In an isolated original-client run with mission 1390
objective 1 injected active, Quillas opened both choice lines. Selecting the
release line sent `PerformNPCChoice`, displayed "Speak to Quillas again," and
left objective 1 complete with release objective 2 active in the copied
character database. See [the client choice record](evidence/client-conscientious-choice.json).
The arrest choice, normal acceptance and full escort routes remain unchecked
in the original client.

The follow-up mission-speaker audit found twelve mission givers, receivers or
objective speakers without client NPC augmentation: Oliver, Wagner, Salter,
Lt. Saviours, Duncan, Caufield, Scott Corman, Hugh Corman, Victor Corman,
Mohindra, Miras and Erodan. A paired SQLite/MySQL candidate migration assigns
original client NPC classes; the three Brann speakers retain the same mesh,
while the nine humans require a different client mesh and
explicitly labelled analogue outfits. Exact final-live classes and outfits
remain unverified. All twelve rows and their appearance data were applied to
the mounted SQLite world database on 2026-09-22; the restarted game container
reached `Server ready!`. A query of all seeded mission givers, receivers and
objective speakers now finds zero creatures without NPC augmentation. See
[the speaker manifest](evidence/mission-speaker-dialogue-classes.json).
Five of the twelve (Wagner 104, Lt. Saviours 120, Scott Corman 137, Victor
Corman 140, Erodan 199508) have no `npc_package` binding. Their original
package IDs remain unresolved; their mission topics still need original-client
interaction verification.

## 2026-09-22 — Gearing handoff, promotion and item-instance infrastructure (not deployed)

The isolated original-client playthrough completes Hartmann's final dialogue,
turns Gearing Up in to DeSimone for 1250 XP/200 credits, accepts Capture the Flag,
and earns the 500 XP promotion to level 2. The original skills panel displays
2 training points, and the level-up message reports 3 attribute points. Normal
logout preserves the equipment and progression, with the cave objective pending.
See [handoff evidence](client-gearing-handoff.md). This is emulator integration;
reconstructed placements/rewards retain their existing provenance limitations.

The combined candidate passes **1300/1300 tests**, none skipped, in **5.9650
minutes**. Item instances now persist ordered loot-module IDs, nullable levels
and nullable trade/sale overrides across inventory reconstruction and copying;
stack merges respect that metadata. Paired SQLite/MySQL migrations preserve
existing rows through null defaults. The module tooltip response now preserves
all nine effect fields, fractional coefficients and absent values, while
unknown definitions remain explicit gaps. No reward modules or combat effects
are assigned by this infrastructure. See [item storage](item-instance-metadata.md)
and [module semantics](bootcamp-shinobi-module-semantics.md).

Mission kill counters and objective completion now commit together. Two seeded
baseline failures demonstrated split-commit corruption and missing reconnect
recovery; the corrected three-case regression passes, including exactly-once
recovery of an already persisted satisfied counter. See
[Capture the Flag audit](client-capture-the-flag-audit.md).

A fresh original-client run on the 1300-test candidate verifies the item
migration against all 15 prior items: every legacy field is unchanged and all
new metadata fields are null. The level-2 character reloads correctly. Normal
movement crosses the ceremonial bridge, but an unopposed approach to the enemy
group ends in death outside the cave trigger; objective 2 remains pending.
Normal hospital respawn succeeds. Firearms rank-two preview/accept works, and
attribute preview/accept persists one point in each attribute. The latter
exposes a client refresh defect: the UI incorrectly returns to three available
attribute points after saving. The original window refreshes from the cached
point balance when stat updates arrive; the server sent the new balance later.
The allocation handler now publishes the balance before attributes, after its
existing database write. A clean rebuild passes 127 focused cases. A separate
copy of the naturally earned unspent checkpoint verifies the original client
now displays two points after partial spending and zero after full spending.
The final combined suite passes **1302/1302 tests**, none skipped, in **5.7901
minutes**; all 2072 tested source entries match the workspace. Baseline failures,
the clean rebuild and full-suite artifacts are retained in
`verification-attribute-final/`. See
[allocation reply evidence](client-attribute-allocation-reply.md) and
[cave and training playthrough](client-cave-training-playthrough.md).

Another isolated original-client check on 2026-09-23 traced the westward
approach from the old inferred southern DeSimone placement to the hostile courtyard. The
unmodified Initiates killed the level-2 character around `(303,121,69)` before
the cave trigger. In a disposable world copy with ambient Initiate attacks
suppressed, client movement from that junction reached the cave area and
advanced objective 2 to the Tizzik Gi boss counter. The isolated world was
restored. This verifies the emulator's walkable approach and area transition
under diagnostic combat conditions; the unmodified encounter and final-live
balance remain open. See [the walking record](evidence/client-cave-walk-playthrough.json).

The original footage's edited 1992/1994 handoff resumes by the north camp
pylon, with the player near `(389,155)` and the measured Forean Initiate group
near `(385,152)`. The prior DeSimone seed at `(387,127.7,40)` was an unsupported
southern estimate. `BootcampDeSimoneCampPlacement` replaces it with the inferred
walkable point `(390.5,119.55,156)`, with field provenance and rollback in
[the placement record](evidence/bootcamp-desimone-camp-placement.json). A copied
1.16.5.0 client displayed his marker and talk prompt there, opened the original
promotion dialogue, awarded 500 XP after Continue and revealed the cave
objective. The copy's mission rows confirm objective 4 complete and objective 2
active. The source edit still hides DeSimone himself, so exact final-live
placement remains unknown. The north-camp cave route and missing Forean
companions still need continuous client verification.
The 2026-09-23 live deployment uses image `rasa-dit-test:20260923g`.
SQLite comparison against the fresh world backup found only the three
DeSimone placement coordinates and the migration-history row changed; the
game container reported ready and `integrity_check` returned `ok`. Backup
hashes and deployment limits are in the placement record.

A first Forean Gunner companion is now staged in the worktree with a distinct
mission-follow combat behavior and [field-level evidence](evidence/bootcamp-camp-gunner-companion.json).
The generated SQLite migration and full rollback passed on an isolated world
copy, and 28 focused migration/content tests passed under .NET 5. The row and
a discovered `creature.max_hp` fallback fix are not in the live image yet;
client rendering and short-range following now pass an isolated 1.16.5.0 replay
from a copied post-DeSimone checkpoint. The Gunner showed its orange marker,
level-3 nameplate and weapon, and followed through the camp sandbags. Combat,
the full cave route, encounter pacing and the broader HP effect still need
direct verification. A second staged migration now adds the Archer and Shaman
named beside him in original footage A3-070, with original-client bow and staff
attack identifiers and [field-level estimates](evidence/bootcamp-camp-archer-shaman-companions.json).
An isolated native-client check renders three marked Foreans and shows them
following across the camp platform; the Archer's level-3 target frame and the
Shaman's staff appear. The generated SQLite migration applies and rolls back
its content rows on a copied live database, and the focused migration test
passes. A later isolated one-Thrax replay with opt-in creature-hit tracing
confirms server-side damage from all three staged companions and a Gunner
killing hit. The copied recruit did not attack and received no XP for that
ally-only kill. A forced six-Thrax proximity test still killed the player.
Neither diagnostic replaces a continuous native-client run of the filmed route;
exact combat values and ally survival remain evidence gaps. See the
[combat diagnostic](evidence/bootcamp-camp-allies-combat-diagnostic.json).
A separate [copied-client route run](evidence/bootcamp-camp-allies-route-diagnostic.json)
then showed all three following up the camp stairs, across the raised walkway
and down the rocks to `(360.07,121.20,114.64)`. The player used a diagnostic
jump over the walkway rail. The display ended before the courtyard, so this
does not verify the filmed approach, fight or cave transition. The companion
rows remain staged and absent from the live database.
A further [courtyard approach replay](evidence/bootcamp-camp-allies-courtyard-approach.json)
kept all three companions near the player through `(316.13,120.28,85.93)`.
The copied character could target a level-2 Thrax near `(308.73,120.10,79.21)`,
but no creature-hit trace occurred before combat range. A diagnostic shortcut
on the next run dropped the player below the navmesh corridor; the resulting
stuck position is a route-test limit, not evidence of final-live combat or
companion pathing. A subsequent [normal-approach combat run](evidence/bootcamp-courtyard-normal-approach-combat.json)
reached the courtyard edge with all three companions. Alden died at
`(297.2,120.2,73.4)` before the cave trigger; all three allies then died.
The opt-in trace recorded no ally attacks in this run despite confirming ally
damage in the isolated one-Thrax test. The player died about four seconds
before the first logged hit on a companion. Encounter playability and companion
engagement remain open; this single unopposed death does not establish
original hostile damage or a justified balance change.
A [copied forward-formation test](evidence/bootcamp-courtyard-formation-diagnostic.json)
then logged Gunner, Archer and Shaman attacks when the three were staged ahead
near the first filmed courtyard viewpoint. It verifies their autonomous attack
path under that diagnostic setup, not the original escort route. The original
recording cuts over the travel into the courtyard, and the six current Thrax
coordinates were measured at different fight times rather than observed as
simultaneous spawn points. Both timing and formation need reconstruction.
Original radar measurements in the [escort combat rule record](evidence/bootcamp-courtyard-escort-combat-rules.json)
place one orange escort icon about 15 m behind the player at 385.467 seconds,
closing to about 5 m by 389.467 seconds. Allied soldiers visible ahead may be
separate courtyard defenders; their identities are unverified. The emulator's
old fighting AI also leashed the camp companions to their spawn point more
than 100 m from this fight. The staged code instead uses the followed player
as the escort's leash centre and connects mission companions to assisted target
selection, with hostile-faction validation and no selected-player attacks by
mission escorts. Focused .NET 5 regressions keep
an escort fighting 130 m from its spawn and verifies clean retreat after the
followed actor disappears. Original assisted-target semantics
and exact leash range are unverified. A copied one-Thrax replay did not reach
the courtyard because the client stalled at a stone wall, then fell below the
walkable corridor on a detour; no ally
hit is claimed for the new code. The live image has not been changed.
An [isolated checkpoint continuation](evidence/bootcamp-courtyard-escort-combat-rules.json)
then loaded the recruit at the verified `(316.13,120.28,85.93)` point with all
three companions still placed at camp. They reached the player during map load.
After a partial walk and two same-map diagnostic teleports, each companion dealt
11 logged hits to the one remaining courtyard Thrax; no player fire occurred
during that fight, and the target reached zero health. The teleports and
five removed Thrax mean this is a focused companion-combat check, not a normal
route or original encounter-balance verification. The live service is unchanged.

The courtyard footage audit also exposed a presentation inconsistency:
`CreateCreatureOnClient` chose fresh random body tints every time it introduced
the same creature to a client. Body tint is now retained on the creature
instance, so observers see a consistent entity. The underlying tint remains
an unverified estimate, including for the camp Foreans; this change does not
establish their original colours. Original video `7Lrst9SG3pk` sampled at 385-430 s
shows multiple allied figures and repeated Thrax kills, but it cannot identify
all allies or prove the six current Thrax placements were alive simultaneously.
A separate [courtyard Warrior record](evidence/bootcamp-courtyard-forean-warrior.json)
stages the level-2 Forean Warrior visibly targeted at 401.8-402.067 seconds
as an additional defender. His placement and combat values are explicit
estimates. An original navmesh probe rejected the first inferred point on the
lower floor and established an upper walkable point at `(294,120.5,65)`.
The code builds; provider migration parity and a full SQLite migration test
pass. An [isolated original-client probe](evidence/bootcamp-courtyard-warrior-client-probe.json)
rendered a fourth Forean-shaped actor on the upper courtyard floor in the
staged world. The complete six-Thrax fight killed an idle level-2 recruit
before the Warrior's nameplate could be read. A separate diagnostic that
temporarily isolated and centred the Warrior produced a native level-2
**Forean Warrior** target and overhead plate. The original class and position,
natural encounter and exact defender composition remain unverified; the live
service is unchanged.

A [six-Thrax original-client checkpoint replay](evidence/bootcamp-courtyard-six-thrax-checkpoint-replay.json)
now confirms all three staged companions attack with all six hostile placement
rows present. Combat began at the copied upper-path checkpoint before movement;
the Warrior and companions died after dealing 3, 25, 18 and 20 hits respectively,
and the Shaman killed one Thrax. The recruit killed a second for 71 XP and 10
credits, then died with mission 1994 objective 2 still active. This verifies
staged combat function; original balance and a continuous completed encounter
remain unverified. The live service is unchanged.

An [upper-route recovered-client replay](evidence/bootcamp-courtyard-upper-route-client-replay.json)
traversed from a copied rocky-descent checkpoint to the upper gap, then from a
second checkpoint crossed it by normal W+Space input. Another ordinary jump
cleared the courtyard obstruction, and movement reached `(306.54,120.53,67.16)`
with companions following. The Shaman killed one staged Thrax without player
fire. The Warrior and companions died, and a Thrax killed the recruit 27.5 m
from the cave trigger; mission 1994 objective 2 stayed active. This is a
staged route verification through separate checkpoints, not an uninterrupted
camp-to-cave completion or retail balance measurement. No live service change.

Three [timed continuous client route replays](evidence/bootcamp-courtyard-continuous-route-replay.json)
subsequently crossed both upper-path obstacles from the rocky-descent checkpoint
without relogging or teleporting. The closest no-fire sprint reached
`(290,120.5,64)`, about 1.1 m outside the cave trigger boundary, then died.
Two final-path variants also died before the trigger. The copied rifle had its
observed 20-round initial load restored for these runs; no player shots were
fired. Mission 1994 objective 2 stayed active. The staged route is traversable
to the courtyard, while completed combat and normal cave entry remain open.

A [recovered-client cover-fire replay](evidence/bootcamp-courtyard-cover-fire-replay.json)
started from a copied courtyard checkpoint with the rifle's observed 20-round
initial load restored. The recruit made two Thrax killing blows through the
native client, each giving 71 XP and 10 credits; R reloaded the drawn rifle
from 5/1000 to 20/985. The first kill preceded the last companion's death.
An attempted cave run then stopped at `(295.8,120.5,70.6)`, 17.4 m from the
10 m cave trigger, where another Thrax killed the recruit. This checks
emulator combat and reload behavior, not final-live balance or a completed
camp-to-cave progression route. The live service remains unchanged.

The [conditioned-creature respawn correction](evidence/bootcamp-conditioned-creature-respawn-audit.json)
prevents a mission-state refresh from recreating a defeated zero-respawn
courtyard Warrior or camp companion after its corpse disappears. It also keeps
a timed conditioned placement from reappearing before its scheduled respawn.
Two focused .NET 5 tests pass. These are seeded lifetime semantics, not proof
of the original game's allied respawn rules. A further focused test confirms
that a creature's killing blow on Tizzik Gi completes the private-map owner's
1994/1 counter and objective without player kill XP. Original NPC-assisted
boss credit remains an evidence gap.

The same isolated replay exposed a headless startup bug: `Console.KeyAvailable`
threw when stdin was redirected, ending the host while the auth listener could
still accept a connection with disposed services. `RasaHost` now skips console
command polling under redirected stdin and keeps the server loop cancellable.
Both auth and game projects build, and the recovered client logged in through
copied headless servers after the fix. This server lifecycle correction is
staged in the worktree; the deployed image was not changed.
A disposable client fight with the player and Gunner moved near the six S4
courtyard Thrax ended in the level-2 player's death without player attacks.
The fallen purple body appears to be the Gunner, but no entity-id kill record
proves that identification or confirms its weapon damage. This is a diagnostic
stress case; it does not establish the final-live encounter's balance or
normal camp-to-cave route. See the companion record for screenshots and exact
limits.

A fresh client on the continuing (non-diagnostic) checkpoint confirms Firearms
rank 2, zero training points, Body/Mind/Spirit 13 each and zero attribute points.
It retains 15/1000 rifle ammunition and the pending cave objective, then reaches
character selection through ordinary logout. All 137 runtime binary inputs
match the tested build. The harness is stopped and its evidence is frozen.
This closes the saved-training reconnect check, not cave combat or boot-camp
completion. No production services or databases were changed.

A [cave-approach replay](evidence/bootcamp-courtyard-cave-approach-replay.json)
used the recovered client on a disposable server copy. From a previously
client-reached courtyard checkpoint, ordinary movement crossed the sandbags
and entered area 198602 when staged Thrax attacks were suppressed. Mission
1994 advanced from the cave approach to the Tizzik boss objective, confirming
the current trigger and route geometry. In a separate replay with normal staged
attacks from the rocky descent, the recruit reached 3.8 m outside the trigger
radius before dying. This documents the outcome of a no-fire sprint through
the current staging; it does not establish a retail damage or spawn correction. The
diagnostic suppression was confined to the disposable database and restored.
The same normal-attack log exposes the courtyard opening: all six seeded
Thrax struck the single staged Warrior within one tick, and he died 8.075
seconds after MapLoaded. The final-week recording has a targetable Forean
Warrior at 401.8–402.067 seconds, but an edit cuts from camp at 384.467 seconds
to the player already in the courtyard at 385.467 seconds. The recording cannot
establish the travel time, encounter activation or number of earlier defenders.
The replay also included inspection pauses. The current six-against-one opening
needs comparison with more direct evidence before changing timing or combat
values.
A [copied-client camp route](evidence/bootcamp-camp-to-upper-route-replay.json)
now reaches the rocky descent from the staged camp point through ordinary
movement with the three companion markers following. One continuation reaches
the upper approach at `(327.1,121.1,105.8)` before its attempted jump drops
under the bridge. This verifies camp-to-descent navigation in the current
emulator, not an original route or a completed camp-to-courtyard fight.
A [side-jump checkpoint replay](evidence/bootcamp-upper-gap-side-jump-replay.json)
subsequently crossed the upper gap by ordinary D+Space movement, landed on an
original-map upper navmesh polygon at `(320.4,120.9,93.8)`, and reached
`(301.1,119.8,78)` with companions before the no-fire recruit died. The replay
began at a copied upper-gap checkpoint and included long inspection pauses;
it does not verify continuous camp travel or final-live encounter balance.
A [continuous camp-to-courtyard replay](evidence/bootcamp-camp-to-courtyard-continuous-replay.json)
then crossed the gap and reached the sandbags at `(301.2,119.8,77.9)` from
the staged camp in one recovered-client login, without any post-login position
edit. The recruit did not fire and died after inspection pauses. Companion
markers appeared farther back on the courtyard radar, and the log contains
no outgoing companion hit; their passage across the gap is not established.
The cave trigger was still 25.1 m away. This confirms a continuous player
route in the emulator, while normal courtyard combat and cave entry remain
unverified. The original final-week recording cuts over the travel, so this
run is not proof of the original route or encounter timing.
A [position-traced continuous replay](evidence/bootcamp-continuous-companion-position-trace.json)
on a rebuilt disposable game binary then showed all three camp companions
crossing the upper gap and reaching the player at the sandbags. Each landed
nine hits on a Thrax before the no-fire recruit died. The earlier continuous
run used a different binary and logged no companion hits, so the cause of
that difference remains unresolved. The original navmesh reports a complete
camp-to-courtyard path, but this does not prove the final-live route or
activation schedule. Normal encounter completion and cave entry are open.
A [continuous camp-to-courtyard combat trial](evidence/bootcamp-camp-to-courtyard-combat-trial.json)
used the recovered client and the rifle's observed 20-round load. The three
companions each landed eight hits before Alden died, but Tab selected the
friendly Archer and all four player attacks resolved to entity 0. The trial
therefore did not test effective player cover fire or reach the cave. A PDB
comparison shows the old and rebuilt binaries used the same BehaviorManager
source; the content runtime source differs, and the reason for the earlier
zero-hit companion run is still open.
A [current-build targeting checkpoint](evidence/bootcamp-current-build-targeting-checkpoint.json)
used the recovered client at the previously reached upper-path point
`(316.13,120.28,85.93)`. A visible Thrax target received seven player rifle
hits and died, with 71 XP and 10 credits displayed. Four other shots resolved
to entity 0. The isolated harness ended before the next target capture, with
the recruit alive and the cave still unentered. This verifies client combat
on the current build, not continuous camp travel or final-live balance.
A [Warrior sequence audit](evidence/bootcamp-courtyard-warrior-sequence-audit.json)
adds a direct final-week comparison: three courtyard kill reward events at
391.533, 396.133 and 401.533 seconds precede a targetable Forean Warrior at
401.9 seconds.
In four traced emulator runs, the only staged Warrior died roughly 7–8
seconds after MapLoaded, before any Thrax died. This is a sequence conflict
for those runs. The footage edit hides map-load time, actor identity and any
later arrival, so the evidence does not justify an invented activation timer,
extra Warrior or combat-stat change.
A [sustained-fire checkpoint replay](evidence/bootcamp-courtyard-sustained-fire-checkpoint.json)
on the current build confirmed two native-client rifle kills, two 71 XP and
10-credit reward pairs, all three companions attacking, and a successful
reload to 20/984. The recruit then died at `(296.4,120.5,67.6)`, 7.5 m
outside the cave trigger, with four Thrax still alive. Inspection pauses and
the copied starting checkpoint prevent a retail pacing comparison. The
complete fight and normal cave entry remain unverified.
Three additional [normal-attack cave approaches](evidence/bootcamp-courtyard-normal-cave-approach.json)
did not reach area 198602. The closest native-client position was 6.8 m
outside its radius; a further forward input at that point hit the sandbag
barricade. This establishes a routing limitation in those runs, not a cave
trigger failure or a final-live difficulty measurement.
An earlier [attack-suppressed client walk](evidence/client-cave-walk-playthrough.json)
did enter the cave area and advance objective 2. A fourth normal-attack run
started from a position reached in that walk, but the saved facing angle did
not reproduce its camera-relative strafe; the character moved toward the outer
sandbags and died. Normal encounter completion remains unverified.
The [cover mechanics audit](evidence/bootcamp-cover-mechanics-gap.json)
identifies original client cover damage values and UI thresholds. Before this
reconstruction, creature attacks and missile damage did not evaluate cover.
The full system remains a confirmed implementation gap. Original sandbag static positions and collision
triangles are known; a diagnostic ray probe intersects them at one death
position. Neither the final-live cover calculation nor its effect on the
courtyard runs has been measured.
A bounded [cover reconstruction](evidence/bootcamp-cover-reconstruction.json)
now loads the six original sandbag collision meshes and applies the recovered
client lookup to direct ranged hits. Its nine sample points and interpolation
are labelled estimates. An isolated compatible-client run loaded 110 triangles,
logged 183 fully occluded incoming hits scaled to 25% before rounding, and
still ended in death without objective progress. The cover-only derived game
image is deployed and reauthenticated with auth; final-live cover behavior
remains unverified.
The recovered client also consumes each weapon hit's `coverModifier` for
directional-hit and overhead-cover indicators. The worktree now sends the
computed damage factor as a Python float for direct ranged weapon hits, while
preserving the original integer-zero default for other hits. Focused packet
and geometry tests pass 16/16; the [cover feedback record](evidence/bootcamp-cover-feedback.json)
labels the server-side factor assignment inferred. TaRapedia's Weapon page
independently describes reduced damage when a target is partly behind cover,
without specifying the final-live multiplier. The
[TaRapedia revision audit](evidence/tarapedia-bootcamp-source-boundary-20260923.json)
pins that statement to its 2008-09-24 revision and confirms the older
Basic Training 101 and Captain Burba bypass pages predate the rebuilt camp.
A copied-client replay initially
entered before content had loaded; after waiting for content readiness, the
private boot camp instance spawned all six Thrax. Their sandbag shots produced
192 cover traces at nine blocked samples out of nine. The recovered client
showed red damage numbers and the low-cover directional hit arc, matching its
own threshold code for the inferred 0.25 factor. Original client player message
1171 explicitly identifies yellow/orange arcs near the targeting reticule as
reduced incoming damage from cover, and greeting 574 names walls and sandbags.
This verifies the client
feedback path in the staged diagnostic, not the final-live server multiplier.
The feedback path was deployed in the
[2026-09-23 readiness update](evidence/live-auth-server-list-wait-20260923.json);
live owner-account cover feedback remains unobserved.
The same diagnostic exposed a [startup admission race](evidence/game-startup-content-readiness-20260923.json):
a fast client could switch characters before mission content loaded and enter
boot camp without its private instance or placed creatures. The worktree game
server now accepts world clients and registers with Auth after content startup.
An immediate-launch isolated replay logged content readiness before admission
and created the private instance. It also exposed an Auth race: the recovered
client kept a `No servers found` modal after receiving an empty first server
list. Auth now waits up to 20 seconds for game registration before answering
that first request. A second isolated immediate-launch replay reached the
world without the modal. Both fixes are deployed. Live startup loaded 404
content rows with zero gaps before world admission, and a recovered-client
login through `100.104.53.1:2116` reached Character Select. The owner MacBook
has not been retested after deployment. See the
[deployment evidence](evidence/live-auth-server-list-wait-20260923.json).
The next [live game update](evidence/live-creature-health-respawn-20260923.json)
corrects the no-`creature_stat` health fallback: the runtime had used 100 HP
for 769 live creature templates even though each has a `creature.max_hp`
between 500 and 1,500. The boot-camp Initiate's seeded 555 HP is an analogue,
not a measured final-live value. The same narrowly derived image enforces
zero/timed respawn fields during mission-condition refresh. Three focused
tests pass, game startup reports 404 content rows and zero gaps, and a fresh
recovered-client Tailscale login reached Character Select. A subsequent
[isolated original-client replay](evidence/bootcamp-after-health-client-replay.json)
entered the staged private camp, approached a Thrax and fired; Alden later
died during inspection pauses and the hospital UI opened. No internal enemy
HP was measured and the normal encounter remains incomplete.
The [client-ready cave route trial](evidence/bootcamp-courtyard-client-ready-cave-route.json)
then moved immediately after the recovered client's playable frame. From a
copied courtyard checkpoint, a 3.5-second left strafe entered cave area 198602
under the staged six-Thrax attacks and completed mission 1994's cave objective.
The recruit died afterward; this does not validate the encounter's final-live
balance or a complete normal fight.
The [2026-09-24 live companion deployment](evidence/live-bootcamp-camp-companions-20260923.json)
adds the three evidence-labelled Forean camp companions and their mission-scoped
follow and combat behavior. The isolated candidate and live Game each loaded
409 content rows with zero gaps; four focused behavior tests passed. The
separate courtyard Warrior remains withheld because the current one-Warrior
staging conflicts with the sequence in final-week footage. The exact live
companion fight and final-live balance still need an in-world replay. A later
[recovered-client check of the exact deployed binary](evidence/bootcamp-live-companion-binary-client-replay.json)
rendered the three allies at camp, reached the courtyard sandbags in one
ordinary movement run, and attributed 20, 21 and 20 hits from the three allies
to one Thrax in a separate controlled copied-world fight. That diagnostic
removed five Thrax and began at the sandbags; the normal encounter remains open.
A [six-Thrax deployed-binary probe](evidence/bootcamp-six-thrax-deployed-binary-probe.json)
confirmed that the copied recruit must draw the empty Shinobi rifle before R
reloads it; the recovered client then showed 20/980 rounds. Two repeated camp
routes fell to the lower level. After a diagnostic teleport to a previously
reached upper checkpoint, the unopposed recruit died at `(307.6,120.5,60.8)`
while the three companions still followed 2-4 m behind and logged no hits.
This exposes an engagement-order gap for that route; it is not a normal fight
or a final-live damage measurement.
A later [six-Thrax player-fire check](evidence/bootcamp-six-thrax-player-fire-20260924.json)
used the exact deployed-derived binary and a fresh copied world. From the
previously client-reached upper checkpoint, the recovered client drew the rifle,
targeted a level-2 Thrax and fired normally. All three camp companions hit that
enemy; Alden's ninth logged hit killed it, and the client displayed 71 XP and
10 credits. A friendly Shaman became the visible target after the first kill,
and the remaining five enemies were not cleared. The checkpoint bypasses the
camp route, and the analogue enemy health and damage still lack final-live
verification.
A longer [six-Thrax clear attempt](evidence/bootcamp-six-thrax-clear-attempt-20260924.json)
on the same deployed-derived binary killed two Thrax from that copied checkpoint.
The Gunner delivered the first final blow and the recruit received no kill
reward; Alden delivered the second and received 71 XP and 10 credits. Gunner
198688 then died under three Thrax attackers. Later scripted shots resolved
against entity 0 while the target panel showed a friendly Shaman, and repeated
forward inputs left the recruit at the sandbags, 32.7 m from the cave area.
This records the emulator's present killer-credit behavior and the fixed-input
route limit; final-live shared credit and the complete fight remain unverified.
An [original-client sandbag route check](evidence/bootcamp-sandbag-to-cave-native-route-20260924.json)
then used the original boot-camp navmesh to pick a walkable line west of the
barrier. W+D for two seconds, followed by forward movement, took the copied
recruit from the prior upper checkpoint around the sandbags to cave area
198602 at `(284.6,120.5,67.8)` while the six staged Thrax attacked. The native
client displayed the objective completion and the copied database marked
mission 1994 objective 2 complete. This is a checkpoint route and trigger
verification; the continuous camp approach, full fight and final-live route
remain open.
The [post-deployment public login check](evidence/live-auth-after-cover-20260923.json)
used the recovered 1.16.5.0 client and existing test account. It sent Login,
ServerListExt and AboutToPlay through port 2116, was redirected to server 234,
and reached Character Select. The owner's client/account has not been observed
after this deployment.
After the owner reported a new "cannot connect" error while using the VPS
Tailscale IP, a [tailnet login check](evidence/live-auth-tailscale-20260923.json)
reached Character Select through `100.104.53.1:2116`. The Mac server and
Alienware Bazzite peers independently received the auth greeting over
Tailscale. The Alienware launcher was using its public-host default despite
the reported Tailscale preference; its settings are now backed up and set to
`100.104.53.1:2116`. A launcher restart and owner login retry remain unverified.

Later official live notes reconcile the rifle's Laser +12 as **12 resistance
rating**, not 12 percent mitigation: Deployment 14 establishes ratings and
Deployment 14.8 sets rank-two weapon/tool modules to 12. Original effect 413
is the stronger final-client candidate. Rifle melee action `(174,5)` and 4 m
range are recovered, but template binding is inferred and final base damage
remains unresolved. The current alternate attack implementation also rejects
melee; [the audit](bootcamp-rifle-melee.md) records the remaining implementation
and evidence work. The full preservation goal remains active.

## 2026-09-22 — Tracking admission and McAllister arrival (not deployed)

Original-client runs 09 and 10 restore saved mission tracking during admission
for both `1992` and the native US-locale representation `1,992`. Initial owner
creation now defers controller activation until yaw and saved mission state
exist. A later snapshot retains normal mission reconciliation and timer/equipment
processing. Native integer formatting and parsing were traced to the original
executable; canonical grouped values are now understood by the server.
See [tracking evidence](client-mission-tracker-reconnect.md).

The combined candidate passed **1276/1276 tests**, none skipped, in **6.0647
minutes**. Actual migrated McAllister routes now advertise zero velocity at
arrival and retain facing for later observers, without changing endpoints or
navmesh heights. Original visual grounding remains unverified; the decoded
terrain is lower than the current navmesh endpoint. See [movement evidence](client-mcallister-grounding.md).

A further original-client check found that unchecking a mission did **not** survive
logout: the client omits default-valued options, while the server merged saves
and ignored an empty snapshot. Native export code establishes a complete
non-default snapshot. The server now replaces it atomically and publishes cache
changes only after commit. Original-client runs 11/12 verify untracking across a
fresh-client reconnect, preserved journal progress, and re-tracking saved as the
native `1,992`. See [option snapshot evidence](client-character-options-snapshot.md).

The final combined suite passes **1281/1281 tests**, none skipped, in **6.8934
minutes**. All 2061 source snapshot entries match the workspace; archived logs,
TRX and source hashes are in `verification-options-final/` under the retained
client-playthrough research directory. The added seeded handoff regression
covers Lightning → Hartmann, wrong-NPC rejection, interrupted progression,
DeSimone's one-time 1250 XP/200-credit payout, and mission 1994 eligibility.
The actual original-client Hartmann final dialogue/DeSimone turn-in remains the
next playable progression check; the test alone does not establish fidelity.

At this earlier checkpoint the probable Shinobi/Armor Piercing 8/Laser 12
module pair was reconstructed conditionally. Subsequent official live-note
research corrects the Laser value to resistance rating, and the newer checkpoint
above adds generic instance persistence. Base-template identity, fixed-versus-random
loot and gameplay effects remain unresolved.
See [rifle-module research](bootcamp-shinobi-rifle.md). No production services or
databases were changed, and the full preservation goal remains active.

## 2026-09-22 — Selection hardening and original-client equipment lesson (not deployed)

The combined creation/admission/selection changes passed **1246/1246 tests**,
none skipped, in 6.0290 minutes. All 2050 source snapshot entries match the
tested workspace. Deletion now requires the original one-slot tuple, rejects
byte overflow (257 previously became slot 1), and runs only in character
selection. Existing-family creation accepts the original UI's capitalization
normalization while retaining the stored family spelling and ownership. See
[selection evidence](client-selection-deletion.md).

The isolated original-client playthrough now executes both Delessio dialogues,
crate Loot All, first-boots equipment completion, the first Hartmann dialogue
and Practice Dummy shooting. Persisted objectives and screenshots corroborate
the transitions. This is emulator integration, not original-live certification;
see [the equipment playthrough](client-gearing-playthrough.md).

Direct comparison found rifle tooltip discrepancies, including 80 m versus the
original 60 m range and 25 versus 82 melee damage. The original action table
independently corroborates 60 m; paired migrations now correct only template
13713's range. The original client displays the corrected 60 m. Item maker,
modules, restrictions, melee behavior and server-side range enforcement still
need reconciliation; see [rifle range evidence](bootcamp-rifle-range.md).
Hovering unequipped Recruit clothing also exposed a second missing tuple in
its tooltip payload. The packet now sends `((None, None), [])`; all three
clothing tooltips render without the former exceptions. The earlier clothing
fix addressed only the resistance collection. See [clothing evidence](recruit-clothing-audit.md).

The subsequent combined suite passed **1251/1251 tests**, none skipped, in
6.2316 minutes (`/tmp/rasa-retail-20260922-gearing-verified.log`), including an
actual SQLite upgrade/rollback comparison of every weapon row and field. Both
providers' operation/model parity was checked; this is not a claim of executing
MySQL's generated migration. All 2056 source entries in the final snapshot match
the workspace. Lightning training also completed in original-client run 07;
mission 1992 remains active, awaiting the next Hartmann conversation. The mission
journal retained its progress after reconnect, but the tracker did not restore
until its checkbox was selected. Tracking options and initialization order need
further verification; no tracking workaround is claimed.

The startup tuple exception has a strong static candidate in the original
asset callback, but the isolated debugger attempt did not establish its runtime
caller. A separate zero-size graphics-view warning is likewise unresolved.
Neither has been suppressed. See [startup analysis](client-startup-tuple-error.md)
and [projection analysis](client-projection-warning.md). The officer apparently
suspended beside the range remains an unverified movement/grounding observation;
[the focused audit](client-mcallister-grounding.md) separates navmesh height and
missing stop-update candidates without claiming a proven cause.
No production services or databases were changed. The full preservation goal
remains active.

## 2026-09-22 — Original-client opening playthrough and admission fixes (not deployed)

An isolated copy of the compatibility client now authenticates to disposable
servers, creates a Human Recruit, enters Luna Cavern, completes both Eloh
approaches, turns in Initiation to McAllister and returns to character selection.
The normal logout persisted 1250 XP, 100 credits, completed mission 1990 and
Logos 23. The original client displays the loaded starter pistol (20/1000),
Eloh projection and next mission offer. These are emulator integration results,
not independent proof of original-live fidelity; see the
[playthrough record](client-opening-playthrough.md).

- **Saved orientation:** original native controller setup snapshots actor facing.
  Sending ActorInfo before controller assignment fixes the ignored saved yaw;
  the original-client pi probe now faces and moves down the +Z causeway.
  A perpendicular-axis probe establishes compass heading = actor yaw + 180
  degrees. Paired migration `BootcampFirstLoginYaw` converts only new-character
  location 19851 from 6.02139 to 2.879793 radians, preserving the measured
  compass heading 345 degrees and its ±15-degree uncertainty. Exact first
  arrival facing remains an estimate from a later footage frame. See
  [orientation evidence](client-world-admission-orientation.md).
- **Equipment admission:** EquipmentInfo emits an original global weapon-drawer
  event whose handler requires the controlled player. Control setup now precedes
  both owner equipment and nearby-player introductions, retaining corpse and
  transfer handling. See [weapon drawer audit](client-weapon-drawer-admission.md).
- **Recruit clothing:** legitimate non-armor equipables no longer send a null
  resistance collection or produce false non-armor errors. The original UI
  iterates that collection unconditionally. No stats were invented. Four new
  cases reproduced the old failures; see [clothing audit](recruit-clothing-audit.md).
- **Practice Dummy:** five original-footage single-hit disappearances contradict
  the prior 100-HP analogue, including a clear 84-damage hit. A paired migration
  uses explicitly inferred minimal 1 HP to reproduce the observed behavior;
  exact original HP versus scripted destruction remains unknown. Reset timing
  now records all five samples and their wider variation. The Lightning target
  remains separately unverified. See [combat audit](bootcamp-combat-feedback-audit.md)
  and [measurements](evidence/bootcamp-practice-dummy-observations.json).
- **Skip grants:** official live D11.6 supports account-completion eligibility,
  but recovered sources do not establish arrival XP, gear or other skip grants.
  Those remain explicit gaps; see [skip audit](evidence/bootcamp-skip-grant-audit.json).

The orientation and dummy integration passed **1224/1224 tests**, none skipped
(`/tmp/rasa-retail-20260922-admission-final.log`). The subsequent clothing and
equipment-admission integration passed **1232/1232 tests**, none skipped, in
5.0007 minutes (`/tmp/rasa-retail-20260922-opening-final.log`). Original-client
run 04 confirms the weapon-drawer and clothing exceptions are gone, with no
false armor warnings; a separate tuple-argument startup error remains unresolved.
The later heading migration passed **97/97 targeted checks**, including full
SQLite Up/Down and both providers' parity, with none skipped
(`/tmp/rasa-retail-20260922-heading-final.log`). A fresh character created
through the original UI then received 2.879793 automatically and rendered facing
down the causeway; no diagnostic character edit was used for this check.
Production services and databases were not changed. The full preservation goal
and creation/boot-camp verification remain active.

## 2026-09-22 — Crate, first combat and original-client startup (not deployed)

The parallel audit continued creation and the first equipment/combat lesson:

- **Supply crate:** the Use packet now names opened state 201, matching the
  original transition table. The issued rifle has the observed 20 loaded rounds,
  persisted in its initial item insert and validated against class capacity.
  Paired provider migrations add an explicit per-item-set count, defaulting to
  zero elsewhere. Partial-loot reconnect recovery omits single-piece mission
  gear already held; this recovery rule is labelled as inferred. Invalid rows
  cannot silently complete the objective. See [crate audit](bootcamp-crate-state-audit.md).
- **Practice targets:** weapon windup retains the selected object ID, and living
  targets receive damage records in the recovery packet so the original client
  can display damage feedback. Destroyed targets do not report further hits
  until restored. All five new regression cases failed against the preceding
  verified code; with the fixes, all 54 combat/appearance focused cases passed.
  Dummy HP and the cut Lightning sequence remain evidence gaps. See
  [combat audit](bootcamp-combat-feedback-audit.md).
- **Appearance:** original hybrid controls disable Beard/accessory selection;
  admission now enforces that restriction. All five palette textures and native
  sampling were recovered, with 262,144 skin texels cross-checked. Exact RGB
  admission remains deferred because runtime scaling and inherited clone colors
  need verification. See [palette record](evidence/character-creation-palettes.json).
- **Client:** the complete 3.09 GB compatibility archive was acquired and all
  members CRC-checked. Static diagnosis identified Wine's incompatible D3DX
  version predicate. Supplying Microsoft's native helper in the isolated scratch
  copy reaches the original animated login screen without patching the client.
  No server connection or world playthrough has yet passed. See
  [artifact and launch record](client-artifacts.md).

The reconstruction manifest now covers the previously omitted S2 placements,
NPC identities and McAllister movement rows, retaining per-field estimates and
source locations. Full isolated .NET 5 verification passed **1,218/1,218 tests,
zero skipped**, in 4.4251 minutes. The TRX has 1,218 leaf and 130 data-driven
parent outcomes, all passed. Log: `/tmp/rasa-retail-20260922-crate-final.log`;
preserved TRX: `/tmp/rasa-retail-20260922-crate-final.trx`.
The 2,055 source/evidence files matched the workspace before execution. Only
the launch-research JSON subsequently gained the native-runtime result; tested
production code, tests and seeded evidence remained unchanged. The copied world
database and read-only assets kept live state separate. Disposable
MariaDB 10.5.29 checks passed the new magazine column/default, scoped 20-round
update and data/schema rollback; these used equivalent SQL, not EF-generated
SQL. Log: `/tmp/rasa-crate-magazine-mariadb-audit.log`.
Live services and databases remain unchanged. This is implementation progress;
creation/boot camp and the full preservation goal remain incomplete.

## 2026-09-22 — Creation eligibility, first admission and McAllister (not deployed)

The next parallel audit continued the earliest progression segment:

- **Appearance:** creation and cloning now use the original client's 92-choice
  catalog, required Hair/Face, optional Eyewear/Beard, race constraints and opaque
  color channels. They reject clothing/weapon injection, missing or cross-race
  faces and malformed tuple/channel encodings before writes. The catalog does
  not impose a gender filter absent from the original picker. RGB texture-palette
  membership remains unverified. See [appearance evidence](character-creation-appearance-evidence.md).
- **Names:** shared creation/clone/rename validation follows the original
  creation error messages for length, initial capital, letters and repetition.
  First-name uniqueness is family-scoped, labelled as a release-era inference;
  family and first-name reservation use consistent managed Unicode comparisons.
  Persisted family state prevents stale-cache replacement. Eight new format
  cases failed against the previous verified source, demonstrating the defect.
  [Naming evidence](character-name-evidence.md) records the unrecovered original
  profanity/reservation lists and exact Unicode/final-live scope gaps.
- **First admission:** login count now commits with selection/optional skip
  position, before world loading. Crashes or disconnects before map registration
  cannot restore zero-login skip eligibility. Skip entitlement comes from the
  persisted account. Registered logout preserves the same count. The original
  commit boundary is inferred; skip rewards remain unverified. See
  [first-login research](first-login-skip-research.md).
- **McAllister:** his zero walk speed prevented the queued path from moving him.
  He now departs after Gearing Up acceptance, matching the observed sequence.
  The 2.5 m/s rate is explicitly an analogue of his reconstructed class's client
  animation reference rate. Double storage retains fractional values and all
  former uint values; narrow data rollback restores his previous row/rule.
  Exact speed, path, endpoint and reconnect placement remain gaps. See
  [movement audit](bootcamp-opening-movement-audit.md).

The pre-existing DIT creator received only the compatibility changes needed to
provide valid initial appearance and replace its copied appearance slots. Its
operator fixture behavior remains separate from preservation content. Live
services and databases were not changed. These corrections do not certify the
creation or boot-camp segment as complete.

**Verification:** final isolated .NET 5 suite **1,202/1,202 passed, zero skipped**,
4.8519 minutes. The source and evidence copy matched the workspace by SHA-256.
The world database was a copy and client/map/navmesh assets were read-only.
The focused run passed 202/204; its two evidence failures used the manifest
before its final format correction, which was included in the successful full
run. Final log: `/tmp/rasa-creation-final-20260922.log`; TRX:
`/tmp/rasa-retail-20260922-integrated/src/Rasa.Test/TestResults/creation-final.trx`.
The TRX contains 1,202 leaf test outcomes and 127 data-driven parent outcomes;
all passed, with no inconclusive or unexecuted results.
Disposable MariaDB 10.5.29 checks also passed equivalent movement column/data
operations, full former uint preservation, fractional persistence, unrelated-row
preservation and scoped rollback. Those SQL checks were not EF-generated SQL;
see `/tmp/rasa-mcallister-mariadb-audit.log`.

## 2026-09-22 — Creation and opening boot-camp corrections (not deployed)

The parallel creation, equipment and boot-camp audit produced these local changes,
in the progression order required by `AGENTS.md`:

- **Race eligibility:** Human is the default; hybrid selection, creation and
  cloning require persisted account unlocks. Mission completion grants the
  corresponding unlock transactionally. SQLite/MySQL migrations backfill only
  completed qualifying missions, not previously created hybrid characters.
  Existing characters remain playable. The mission chains themselves still need
  reconstruction. See [race evidence and remaining gaps](race-unlock-research.md).
- **Recruit loadout:** new characters receive the evidenced recruit pistol and
  worn outfit, 20 loaded rounds and 1,000 reserve rounds. Gear sell/trade flags
  match observed tooltips; existing inventories are not rewritten. Clone ability
  initialization is repaired. Exact template choices, some visual/default values
  and clone-kit behavior retain their inferred/analogue labels in the
  [loadout manifest](evidence/new-character-loadout.json), with the original HUD
  interpretation recorded in [equipment research](starter-equipment-research.md).
- **Gearing Up:** the starter outfit no longer satisfies the supply-crate equip
  objective. The binding requires crate-set gear, and crate settlement recognizes
  a rifle already moved into the weapon drawer. Footage establishes the boots
  transition; the more general set predicate remains inferred. Rechecked tooltip
  crops identify the crate armor classes, but do not uniquely identify template
  IDs. See [boot-camp audit](bootcamp-equip-audit.md).
- **Creation protocol:** clone requests enforce the original seven-field tuple
  and checked slot/gender conversion. Height validation accepts the original
  float-encoded 0.9 endpoint while rejecting nonfinite and out-of-range values.
  Original client offsets and emulator robustness policy are distinguished in
  [creation validation evidence](character-creation-validation-evidence.md).
- **Seed loading:** bounded insert operations and a compact item/class iterator
  remove defects that obstructed migration tests. All 30,225 ordered Int32 pairs
  retain the same SHA-256. These are representation changes, not additional
retail evidence; see [loading verification](seed-loading-performance.md).

**Verification:** the final isolated .NET 5 run passed **1,130/1,130 tests, zero
skipped**, in 4.9523 minutes, including migration upgrade/rollback, transaction
failure cases, evidence checks and world/navmesh audits. Source and evidence
files in the test copy were checked against the workspace before execution;
the world database was a copy and client/navmesh/map assets were read-only.
The first combined run exposed two rollback failures caused by missing migration
target models; frozen designer models corrected them before this final run.
The log is `/tmp/rasa-retail-20260922-final.log`; TRX is
`/tmp/rasa-retail-20260922-integrated/src/Rasa.Test/TestResults/final.trx`.
The race backfill also passed direct execution against disposable MariaDB
10.5.29 tables (deduplication, filtering, repeat/incremental grants and rollback);
see `/tmp/rasa-race-mariadb-validation-20260922.log`. Tests verify implementation,
not 1:1 fidelity of fields still labelled as estimates.

All new reconstruction claims have machine-readable provenance. The exact final
server state, estimated fields, original-client playthrough and the rest of
new-character-to-endgame progression remain incomplete. These changes have not
been deployed; live databases and services were not modified.

## Content and combat, 2026-09-16

Five missions later in the Wilderness and the Divide were seeded from the client's own conversations, and the
resistance curve finally reached combat.

- **Seeded** (all objectives already conversation-bound in the client, rewards TaRapedia's recorded values), 36
  missions in total by the end of the session: 431, 442, 444, 549, 836, 427, 682 (Wilderness), 332, 347, 382, 796,
  1743 (Divide), nine Palisades missions (199100-199107 givers) and six Valverde ones (199200-199205).
  431 Distress On The River, 442 Quarantine, 444 Unity Among Men, 549 Failure to Launch, 836 Incoming!,
  427 Lurking In The Shadows (Proctor Fulgor, creature 76), 682 Childhood's End (Arioch Xanx, creature 77),
  332 Ammo Express, 347 Cleansing the Toxins: Part II, 382 Retrieval for Recon, 796 Behind Closed Doors,
  1743 Report to Liaison Noonan.
- **NPCs created** (18 in total): 199000-199003 Divide (Lt. Sebastian, Shaman Horea, Field Dr. Dawson, Receptive
  Liaison Brice), 199100-199107 Palisades, 199200-199205 Valverde. Name id
  from the client's `creaturenamelanguage` (original), level/zone//loc from TaRapedia (inferred, dated), appearance
  an analogue under OD-45. The pipeline is the answer to the 660 missions whose giver is not in the world seed.
- **Defect found and fixed**: `npc_mission_reward` carries experience and credits in the same `credits` column, which
  the first batch got wrong (the loader refused all five for "Experience reward amount 0 is not positive").
- **Combat**: the resistance conversion the client itself carries (`shared/damageresistance.pyo`, cross-checked against
  Deployment 14's published table on all eight points) was recovered, tested - and unused. The equipment pass now sums
  each worn item's resist list per damage type onto the player, and a landed hit is scaled by the target's resistance
  before armour absorbs it. The rounding the live server used stays a recorded parameter
  (GAP-D14-RESISTANCE-ROUNDING).
- **Provenance**: 125 manifest rows added across the day's slices plus the `community_db` source kind (TaRapedia, the
  Ellatha mission DB) and the W3 slice; two gaps opened for what the sources do not settle
  (GAP-W3-COUNTER-OBJECTIVES, GAP-W3-NPC-POSITION-COVERAGE) and one for the rounding above.
- **Not verified in-game**: none of the new missions, NPCs or the resistance change has been played yet; the audit
  that did catch something was the world position one, which flagged the two NPCs whose TaRapedia /loc has no navmesh
  under it.

## Sources and confidence

- [Rasa.NET](https://github.com/InfiniteRasa/Rasa.NET): authoritative for this
  implementation, not proof of retail behavior. The README explicitly describes
  an incomplete server. Open issues cover abilities, attributes, XP, missions,
  cloning, AI, control points, squads, and travel.
- [Older C++ server, experimental branch](https://github.com/InfiniteRasa/Game-Server/tree/experimental):
  another implementation to compare. Its `src/manifestation.cpp` validates the
  available budget before allocating attributes and sends updated allocation
  points afterward. It uses **two** attribute points per level, unlike this C#
  implementation and the reference below. Do not copy formulas indiscriminately.
- [TaRapedia: Leveling Up](https://tabularasa.fandom.com/wiki/Leveling_Up): indexed
  text states the general award is three attribute points and two skill points.
  It also describes trainer advancement at levels 5/15/30. Full page retrieval
  was blocked; treat the indexed text as corroboration, not a complete versioned
  specification. No trainer gate has been added based only on that excerpt.
- [TaRapedia: Level](https://tabularasa.fandom.com/wiki/Level): indexed text says
  level 50 awards four additional training points beyond the normal two.
- [TaRapedia: Attributes window](https://tabularasa.fandom.com/wiki/Attributes_window):
  indexed text describes a purchased respec token. Negative allocation requests
  are not an appropriate substitute for a respec system.
- [Upstream first-map crash report](https://github.com/InfiniteRasa/Rasa.NET/issues/45):
  a reproducible historical report, not proof that the current observed session
  encountered that crash.

## Implemented in this pass

- Attribute requests must be nonnegative and fit the remaining earned budget.
  Validate all three values before mutation. Wide arithmetic prevents overflow
  from turning large requests into apparently affordable allocations.
- Invalid legacy negative/overspent allocations expose zero spendable points;
  this does not modify existing characters or attempt an unsupported respec.
- Refresh the client's remaining allocation points after allocation or rejection.
- Level-up messages report the change in available points for that level instead
  of reporting the entire accumulated unspent balance as newly earned points.
  Existing skill-point awards, including milestone bonuses, are preserved.

Regression tests cover earned budgets at levels 1/2/5/15/30/50, exact spending,
repeated requests, negative values, overspending, integer overflow, empty requests,
and invalid legacy allocations. These do not prove rendered client behavior.

## Observed content and next work

Read-only SQLite inventory on 2026-09-12:

| Table | Records |
| --- | ---: |
| map_info | 78 |
| creature | 140 |
| creature_stat | 92 |
| spawnpool | 218 |
| npc_package | 2 |
| npc_mission | 2 |
| npc_mission_reward | 0 |
| itemtemplate | 4,985 |
| itemtemplate_armor | 5 |
| itemtemplate_weapon | 2,440 |
| vendor | 20 |
| vendor_item | 109 |
| logos | 166 |
| teleporter | 581 |

Counts show content coverage gaps, not how many records retail should contain.
`MissionManager` currently loads definitions; a complete quest lifecycle still
needs investigation. Priorities for continued work:

1. Complete skill prerequisites: batch validation and class ancestry are now
   implemented (see below). Final-client evidence now establishes signature caps
   and their exclusion from ordinary purchases; exact grant and rank-level/Logos
   prerequisites still need reconstruction.
2. Audit stat formulas against client data and patch-era references; validate
   allocations and level-up display in a real client, including reconnect.
3. Continue melee investigation: the shared recovery route and false hit lists
   are corrected (see below), but formulas, timing, and on-hit effects remain.
4. Build mission progression and rewards from identified retail quests, including
   NPC relationships, prerequisites, objective state, persistence, and rewards.
   Do not mass-import guessed quests or treat the older SQL as verified retail.
5. Inspect armor templates, equipment requirements, XP thresholds, trainer
   advancement, cloning, loot, AI, and control-point behavior with separate
   evidence and tests for each implemented mechanic.

## Validation commands

Build an isolated candidate before replacing a live image:

```sh
docker build -t rasa_net:retail-candidate .
docker run --rm --network none rasa_net:retail-candidate dotnet test src/Rasa.Test/Rasa.Test.csproj --no-build --no-restore
```

Tests run without production database mounts. Keep live-client validation and
remaining fidelity work explicit; passing unit tests is not retail certification.

2026-09-12 validation: Docker build succeeded with zero errors and five existing
unused-variable/field warnings. All 26 tests passed (zero failures/skips).
The tested candidate image was promoted to `rasa_net:latest` and the game service
was recreated; auth was left running. Candidate image ID:
`sha256:f0a147ae9d143c4c57ba8ff7e1b7690b2a81b4992d16eaf862dadc18b6f4ad1f`.

Pre-deployment SQLite backups passed `PRAGMA integrity_check` and are stored in
`/home/blizz/backups/rasa-net/20260912T170541Z-retail-allocation`.
Previous image retained as `rasa_net:before-retail-allocation-20260912`.
For code rollback, retag that image as `rasa_net:latest` and recreate only the
game service with `docker compose up -d --no-deps --no-build game`. No schema
migration or character-data rewrite was introduced by this patch.

Post-deployment logs confirm authentication to the auth server, listening on
port 8102, loading world data, and `Server ready!` at 17:05:53 UTC. The running
game container's image ID matches the tested candidate. In-client allocation,
level-up display, and reconnect persistence still require gameplay verification.

## Skill training and attack recovery — subsequent 2026-09-12 pass

Source details: [skill research](skill-research.md) and
[mission research](mission-research.md). These distinguish implementation evidence
from client-verified retail facts and retain conflicting/obsolete source notes.

Changes:

- Training validates the complete batch before modifying player skills: known
  IDs, one entry per skill, matching arrays, rank bounds, no learned-rank
  downgrades, and enough points for all intervening ranks. Rejections resynchronize
  current skills and points rather than throwing for invalid requests.
- Firearms enum corrected to ID 1, matching both existing wire tables and the
  older C++ definition. Live `character_skills` was empty at inspection; no ID 2
  data migration was performed.
- Class ancestry checks use the pinned C++ catalog's 73 IDs and 15 classes,
  corroborated by retail class descriptions and patch-era changes. Characters
  retain ancestor skills and cannot buy from unrelated branches. Class membership
  initially had medium retail confidence pending client-data comparison. The
  subsequent artifact pass below confirms the complete catalog and signature
  caps; exact per-rank level requirements and Logos prerequisites remain incomplete.
- The whole training batch is saved with one EF transaction before live ranks
  change. A failed save rolls back the batch, leaves player state intact, logs
  the failure, and resynchronizes skills/points. SQL schema is unchanged.
- `SkillsPacket` now owns a snapshot. Constructing another player's packet, or
  changing a rank before the send queue drains, cannot replace its data.
- Recovery re-resolves the original target at impact. Untargeted shots and
  removed/dead/replaced actors produce empty hit lists, without false damage
  entries or missing-target dictionary exceptions. Existing live-target damage
  is preserved.
- `WeaponMelee` action 174 explicitly uses weapon recovery, as in
  [the pinned C++ recovery implementation](https://github.com/InfiniteRasa/Game-Server/blob/4a9ab5f1fcdf6a18ab6911c384189cc41ddae651/src/missile.cpp#L299).
  That source also labels melee a placeholder; this does not establish correct
  retail melee formulas, animation timing, or effects.

New tests exercise class inheritance/rejection, rank costs, malformed/duplicate
requests, no partial mutations, skill-packet serialization across players,
SQLite batch persistence across a fresh context, injected-save rollback, empty
attack packets, target disappearance/death/reuse, and ordinary target damage.

Mission research found the two current seeds have incorrect NPC relationships,
missing objectives/rewards, and storage/serialization defects. River Recon has a
concrete older-project reference, but importing it directly would use mismatched
script opcodes and unverified rewards. It remains the next mission implementation
candidate; no live quest rows were changed in this pass.

Validation and deployment: final Docker build completed with zero errors and the
same five unused-variable/field warnings. All **60 tests passed**, zero failed or
skipped, in the isolated container (no live database mounts). This includes the
previous 26 tests plus 34 training, persistence, and missile recovery cases.

The deployed game container matches tested image
`sha256:583ae914f43a367c1a11570163f7cee95bacc283506f79baf117264c878e98f4`.
Pre-deployment SQLite backups passed integrity checks; backups and build/test logs
are in `/home/blizz/backups/rasa-net/20260912T171705Z-retail-training`.
Code rollback image: `rasa_net:before-retail-training-20260912` (retag as
`rasa_net:latest`, then recreate only game with `--no-deps --no-build`).
Live-client training, effects, combat animations, and reconnect UI remain
unverified; the database reload test verifies storage, not the real client's UI.
Startup logs confirm auth connection, world loading, and `Server ready!` at
17:17:16 UTC after this deployment.

## Original client evidence and corrective pass — 2026-09-12

The [acquired client](client-artifacts.md) has embedded executable version
1.16.5.0 and recoverable Python 2.4 client code and generated tables. Selected
members were validated against ZIP sizes/CRCs and hashed; static inspection did
not execute game code. Its community-upload provenance remains distinct from
an independently authenticated official final distribution.

Implemented corrections:

- [Skill evidence](final-client-skill-evidence.md) confirms all 73 skill IDs,
  their class ownership and ancestry. Eight signature skills have maximum rank
  one and no ordinary training controls. Ordinary purchases now reject these
  signature grants/increases and invalid higher ranks; their original grant
  mechanism remains missing. Existing point-award arithmetic was not changed.
- Tactical Evasion (skill 54) now advertises ability 10000005 after training,
  matching the generated requirement table and actual client action. This
  repairs its skill-to-ability mapping; server ability effects remain incomplete.
  The live skill table contained zero rows before deployment, so no saved
  ability-ID repair was required on this server.
- [Mission persistence](mission-research.md) now supports multiple missions per
  character and filters reads by account and character slot. Generated SQLite
  and MySQL migrations preserve existing rows and widen mission-category
  representation. The original client contains category 10000044.
- [Mission packets](final-client-mission-evidence.md) preserve change time,
  distinct X/Y/Z markers, nullable timers, and three-value generic counters.
  Signed compact integer encoding/decoding now handles negative values without
  corrupting packet structure, while retaining all bits of unsigned IDs.
- [Normal logout](death-retail-evidence.md) now advertises and enforces ten
  seconds with cancellation and a monotonic deadline. Immediate quit/socket
  disconnect retention is still missing. Death/recovery research now has
  original trauma constants and client protocol evidence, but recovery gameplay
  was not activated from incomplete trigger/health evidence.

The final combined Docker build succeeded with zero errors and the same five
pre-existing unused-variable/field warnings. **All 105 tests passed**, zero
failed or skipped, without network or production database mounts. Separate
isolated MySQL checks covered generated Char migration SQL and category
widening; their scope is recorded in the mission research document.

Tested and deployed image:
`sha256:e9a6eb5c6a13f36b53743c2c69130a1d43391a4f51585d981edccabe29f838ab`,
retained as `rasa_net:retail-mission-candidate`. Only the game service was
recreated. It authenticated to the existing auth service, loaded world data,
and reported `Server ready!` at **17:45:26 UTC**. The running image matches the
tested candidate; the auth container's image is unchanged.

Verified SQLite backups, build/test logs, and before/after metadata are in
`/home/blizz/backups/rasa-net/20260912T174516Z-retail-mission`.
All three backups passed integrity checks. Post-startup Char and World databases
also passed integrity checks, with unchanged non-migration table counts. The
new composite mission key and both SQLite migration records were confirmed;
existing mission categories remain 1 and 2.

Rollback image: `rasa_net:before-retail-mission-20260912`. This pass changes the
schema: an image-only rollback is insufficient once new data uses multiple
missions or wider categories. Stop game writes, preserve any subsequent data,
and use the consistent pre-upgrade Char/World backups when restoring the old
schema and image. Do not truncate categories or discard missions to force a
downgrade; do not restore the independently running auth database unnecessarily.

The full preservation goal remains active. Passing these checks establishes
the corrected implementation, not final-retail equivalence. Quest lifecycle,
death/recovery, complete ability effects, content, final events, and real-client
comparison still require substantial reconstruction and verification.

## Ability requests, NPC dialogue, and disconnect lifecycle — 2026-09-12

The [ability-use audit](ability-use-client-evidence.md) adds original-client
request shapes and action-failure signatures. All 73 catalog rows and the
complete skill/ability/Logos join passed independent raw-bytecode comparison;
the resulting 53 active C# ability requirements matched with zero differences.
The server now checks learned rank, class ancestry, signature caps, and all
required Logos before queueing a skill ability. Lower learned ranks remain
usable and crouching remains allowed. Non-skill actions require their own
authorization paths; item/mech/polymorph support is still incomplete.

Ability requests now preserve optional entity/location/None targets, full
64-bit entity/item identifiers, and optional yaw. Rejected skill requests send
the client's supported `UserActionFailed` tuple with no guessed localized
message. Recognized ability effects, resource costs, cooldowns, targeting,
interruptions, and Lightning/Sprint placeholder formulas still require work.

[Original mission tables](river-recon-client-evidence.md) establish River Recon
429's objective text IDs, dialogue keys and narrative sequence, plus mission
321's Machina objective/counter label. They also prove that local Rogers was
assigned the dying patrol member's conversation package. The fresh seed now
uses package 116. Paired generated data migrations correct only the known
`npc_package` row 100/value 726 combination, retaining other values and NPCs.
No reward, prerequisite, objective trigger, NPC position, or mission assignment
was guessed from client text. The migration's `Down` intentionally does not
restore a known bad package or overwrite a row that was already correct.

The [disconnect lifecycle](death-retail-evidence.md) now retains an actor after
socket closure when a server-processed logout request still has time remaining.
The original deadline continues, world combat remains active, and world removal
and the existing character snapshot run at the end. Repeated callbacks cannot
repeat cleanup; closed connections stop accepting input/output, retained actors
continue occupying their accounts, and character replacement is gated to the
selection state. Loading/normal-logout races have isolated regression coverage.
Loss without a pending logout performs intended emulator cleanup; its exact
retail grace period and reconnect policy remain unknown. Health, death, and
active-effect persistence are still absent from the existing character snapshot.

[Character progression research](character-progression-client-evidence.md)
records the original trainer/class-selection and clone requests and the 5/15/30
tier text. The server-supplied training eligibility and reward totals are not
contained in those UI messages. Signature grants, precise point accounting,
and complete trainer/clone transactions remain open, with no invented numeric
replacement introduced.

Validation: the final Docker build completed with zero errors and five existing
unused-variable/field warnings. All **135 tests passed**, with zero failures or
skips, in an isolated container with no network or production database mounts.
This includes 16 new ability/protocol cases, seven disconnect cases, and seven
NPC package cases. The generated package-correction SQL also passed isolated
MySQL fixture checks; detailed logs and scope are linked in the mission record.

Tested/deployed image:
`sha256:39c7ffc84dd58fc269771d29fa27e4c41a4c8373fc516619590ab57f0a5f3fd3`,
retained as `rasa_net:retail-ability-candidate`. Only game was recreated. Startup
confirmed authentication to auth, world loading, and `Server ready!` at
**18:08:28 UTC**. The running image matches the tested candidate and auth's
image is unchanged.

Fresh SQLite backups, candidate build/test logs, focused records and before/after
deployment metadata are in
`/home/blizz/backups/rasa-net/20260912T180817Z-retail-ability`.
All three backups passed integrity checks. Post-startup Char/World checks also
passed. Non-migration table counts were unchanged; Rogers's package is now 116,
Witherspoon remains 208, and the expected World migration record was added.

Rollback image: `rasa_net:before-retail-ability-20260912`. The package correction
is compatible with that previous application, so code rollback can retain the
corrected package. If reverting data is specifically necessary, use the verified
pre-deployment World backup; the data migration's `Down` is intentionally empty.
Do not restore independently active auth/character data unnecessarily.

Operational observation: the previous game container restarted three times
around **18:00 UTC**, before this candidate was deployed. Its retained stdout
contains **three `Out of memory.` lines**, each preceding a restart's startup
sequence, in
`/home/blizz/backups/rasa-net/20260912T180817Z-retail-ability/rasa-game-before-ability.log`
(also retained at `/tmp/rasa-game-before-ability.log`). No stack trace identifies
which allocation or code path failed. The earlier search omitted that phrase;
the logs must not be described as containing no failure evidence. Bounded
historical Docker-event and accessible kernel-journal queries supplied no
additional cause. The replacement remained at restart count zero in a read-only
follow-up around **18:13 UTC**, with approximately 315 MiB container memory and
zero `oom` / `oom_kill` events in its current cgroup. Those replacement-container
observations do not explain the prior process failures. Keep their allocation
source open for investigation; do not infer a kernel OOM kill or a framing
attack from these logs alone.
Neither unit tests nor successful startup prove sustained availability or
original-client gameplay. Full preservation remains active and incomplete.

## Sprint, Lightning base damage, and malformed frames — 2026-09-12

[Sprint reconstruction](sprint-client-evidence.md) joins the final client's
literal action properties, original effect consumers and official live notes.
The five ranks now use movement multipliers **1.2/1.3/1.4/1.5/1.6**, with
activation costs and two-second drain amounts **30/27/25/20/18 CHI**. The
previous experimental short duration and speed formula are removed. Effect
updates account for every map-loop delta. Sprint attachment carries the
original consumer's required scalar argument, and the original right-click
effect-cancel request now removes the actor's own Sprint. Duplicate and
unaffordable activations do not add another effect or spend resources.

Normal adrenaline capacity is **1000**, inferred from all eight original
signature descriptions specifying 100% adrenaline and their corresponding
1000-CHI action costs, independently corroborated by Sprint's percentage
conversion. It no longer uses an unrelated Power/stat formula. Remaining
adrenaline gain/decay, starting-resource rules, modifiers, precise first-tick
phase and repeated-activation toggle behavior are explicitly unverified.
The client duration fields are retained as long internal caps while the
authored open-ended presentation has no countdown; final server cap behavior
still requires direct evidence.

[Lightning base damage](lightning-client-evidence.md) now follows the selected
rank and experience-level scaling. The fixed 233–311 sample is replaced by
original base bounds 180–240 at rank 1 and 240–300 at ranks 2–5, scaled by
`int(base * 2 ** ((level - 1) / 8.0))`. This completes only the base-range
correction. Arcs, Sonic damage, stun/storm effects, costs, timing, targeting,
and the full damage/modifier pipeline remain required combat work.

[Network runtime evidence](runtime-network-evidence.md) records an isolated
reproduction: an oversized four-byte length header caused the previous socket
callback to terminate its process with `Out of memory.`. Frames outside the
existing receive-buffer bounds now close their connection with resources
returned. Word lengths are unsigned, coalesced/fragmented frames are preserved,
and failed decryption is rejected. Protocol decoding now isolates malformed
messages inside their declared frame and handles them at the client boundary.
Declared decompression/field lengths no longer cause eager unchecked allocation.
Independent review also corrected endpoint access after socket disposal and
ownership transfer before synchronous receive continuation.

These tests establish a crash path and its correction, not the cause of the
three historical restarts. Full malformed-client handling, sustained runtime
observation and original-client session validation remain distinct work.

The [continued mission audit](river-recon-client-evidence.md) retained
conflicting historical River Recon rewards and the original live 1.4 notes
documenting 855 replaced mission rewards. Capture date does not establish the
data's revision. No conflicting quest amounts or items were imported.

The combined .NET 5 candidate built with zero errors and the same five existing
unused-variable/field warnings. **All 191 tests passed**, zero failed/skipped.
An independent source comparison between the candidate image and the reviewed
workspace found no differences (excluding generated `bin`/`obj` directories).

The game service was recreated from tested image
`sha256:f798038e3a0e3a514295bf2afc388cce8b0229f091f2cfb8ece861f8fb4a17c6`
at **18:33:06 UTC** and reported `Server ready!` at **18:33:16 UTC**. Its first
post-deployment check was running with zero restarts and no unhandled/OOM
startup lines. Auth's image and start time are unchanged. No new schema/data
migration is included in this pass.

All three SQLite backups passed integrity checks. Backups, private deployment
configuration, reviewed source, build/test logs, source comparison, retained
old/startup logs and before/after metadata are in
`/home/blizz/backups/rasa-net/20260912T183251Z-retail-combat/`.
Rollback image: `rasa_net:before-retail-combat-20260912`. Retag it as
`rasa_net:latest` and recreate only game with `--no-deps --no-build`; this code
rollback needs no database restoration. The broader preservation goal remains
active, including the explicit fidelity limitations in the linked reports.

## Action lifecycle and death notifications — 2026-09-12

The [original action lifecycle](action-lifecycle-client-evidence.md) establishes
Lightning's **500 ms windup, 700 ms recovery and 1200 ms subsequent reuse**.
The same action ID shares reuse across all ranks: normally the actor remains
busy until 1200 ms from starting and can cast Lightning again at 2400 ms.
The server now implements these stages with monotonic deadlines, charges the
original rank costs **25/50/75/100/150 Power** at successful recovery, and
revalidates the actor, learned rank/Logos, available Power and original target
identity before resolving. Spending at recovery is an explicit ordering
inference; original-server resource transaction boundaries remain unverified.

Rejected predictions receive both current-action cancellation and unresolved
request cleanup, with actual remaining reuse time to correct a late client
prediction. An identical request for an already accepted current pair is
ignored so the original client's first-pending-request removal does not discard
the accepted cast. Matching interrupts cancel unfinished casts without applying
damage or spending Power; interruption after resolution retains reuse.
Movement does not interrupt Lightning, matching its original class flags.

Cancelled legacy object/weapon queue entries no longer perform successful
recovery early. Object cancellation explicitly releases the corresponding
pending user while retaining unrelated objects' users. Main-loop elapsed time
now uses a monotonic clock, preserving the existing cadence while preventing
calendar-clock changes from altering durations. Full legacy reload/action
interactions still require integration with the original interruption flags.

The same audit found a preceding [Sprint gap](sprint-client-evidence.md): effect
attachment alone did not remove the original client's unresolved action.
Successful Sprint now sends its inherited self-target recovery acknowledgement
after attachment. Effect announcement is deferred to that recovery so it is
announced once. Exact historical unused hit-data encoding still needs a capture.

The [Lightning effect audit](lightning-effects-client-evidence.md) preserves
original optional arc/Sonic/stun/storm properties and adds typed immutable
damage/arc/storm packet data. Lightning recovery now serializes each actual
hit's amount, flags and effect lists. It does not yet select arc victims or
apply those extra mechanics. Native body-distance range, line of sight,
damageable objects and wargames remain server validation/behavior gaps.

The [death audit](player-death-client-evidence.md) corrects creature lethal-hit
notifications. The killing recovery now carries `deathBlow`, followed by a
victim `ActorKilled` notification covering observers who could not see the
source. The old state-only notification skipped the client's death announcement
and cleanup. Tests exercise source-only, shared and victim-only visibility and
two pending shots where the first kills the target.

Original player death/recovery codecs are now recorded and implemented as
unconnected foundations. Player lethal damage still has the existing placeholder
recovery behavior; it must be replaced together with a working original recovery
path. Hospital IDs cannot be copied from local teleporter IDs: the audit records
specific mismatches with the original graveyard table. Eligibility, relocation,
restored resources, death persistence and re-login remain necessary work.

The combined .NET 5 candidate built with zero errors and the same five existing
unused-variable/field warnings. **All 250 tests passed**, zero failed/skipped.
The candidate's source matched the reviewed workspace, excluding generated
`bin`/`obj` directories. An independent documentation audit found no material
contradictions between the implemented behavior and the stated evidence gaps.
These checks validate this implementation; they do not certify original-client
behavior or complete final-live fidelity.

The game service was recreated from tested image
`sha256:895a13fbcf52626516d16bb2d62a6d644ea55e7697b7da28c2ad636a5c7d6f2c`
at **19:06:20 UTC** and reported `Server ready!` at **19:06:30 UTC**. Its initial
post-deployment check was running with zero restarts and no unhandled/OOM/fatal
startup lines. Auth's image and start time are unchanged. This pass includes no
schema/data migration.

All three fresh SQLite backups passed integrity checks. Backups, private
deployment configuration, reviewed source, build/test logs, source comparison,
retained old/startup logs and before/after metadata are in
`/home/blizz/backups/rasa-net/20260912T190212Z-retail-lifecycle/`.
Rollback image: `rasa_net:before-retail-lifecycle-20260912`. Retag it as
`rasa_net:latest` and recreate only game with `--no-deps --no-build`; this code
rollback needs no database restoration. The full preservation goal remains
active with the limitations recorded above and in the linked evidence reports.

## Weapon lifecycle, inventory conservation and combat reports — 2026-09-12

The [weapon action reconstruction](weapon-actions-client-evidence.md) now uses
all 135 original draw/stow/reload timing rows. A captured weapon action tracks
windup, recovery, shared action-ID reuse and manual/autofire origin. Reload
revalidates its original weapon and inventory before transferring ammunition,
and a delayed resolution preserves the entire unpredicted recovery interval.
Eligible interruptions cancel unfinished reloads without consuming reserve
stacks or applying a successful recovery. Draw/stow readiness at recovery end
remains an explicit inference pending native animation strike evidence.

Reload conserves ammunition across multiple stacks and preserves loaded rounds.
Magazine, reserve stacks and emptied inventory links commit atomically, with
expected-count and account/character/location checks. Inventory movement now
updates the in-memory owner and slot alongside the persisted destination;
withdrawing an item from home storage no longer writes a personal item under
character ID zero. Character IDs are kept distinct from roster slots.

Autofire continues when the first action draws or reloads, maintains one
sequence per client, retries busy actions at the observed client cadence and
advances the global timer list once per elapsed interval. Its prior per-map
invocation incorrectly changed timing with the number of occupied maps.
The current database's uniform 1500 ms reload values, shot/refire values,
keepalive grace, modifiers, heat/jam and complete attack admission remain
unverified mechanics/data; the original action catalog alone does not prove
those server-supplied values.

The [combat report audit](combat-damage-client-evidence.md) propagates the
equipped weapon's actual damage type and separates absorbed armor from final
damage in resolved reports. The existing universal armor-first damage policy
still lacks the original type-specific rules, piercing, resistance and modifier
pipeline. The recovered resistance conversion agrees with official live D14
examples and is preserved as an unused helper awaiting authoritative inputs
and ordering.

Initial actor attributes now use the original constructor's
`normalMax/currentMax/current` order. Queued attribute, health and armor updates
retain their values. Body/Mind no longer keep an initial zero through stat
calculation, and recalculation preserves remaining armor instead of replacing
it with a regeneration accumulator. These consistency repairs do not establish
the emulator's stat growth or regeneration formulas as final-live rules.

The [hospital investigation](hospital-recovery-evidence.md) recovers six exact
Wilderness hospital/safe-zone map markers, and distinguishes waypoint,
graveyard, marker-entity and marker-text identity. It also recovers supplied
friendly/acquired/PvP-safe state and the different burial/hospital UI requests.
The marker catalog is not activated as respawn destinations: original respawn
coordinates, graveyard joins, eligibility, resource restoration and persistence
remain missing. Full player death and the broader final-live preservation goal
remain incomplete.

The final .NET 5 candidate built with zero errors and the same five existing
unused-variable/field warnings. **All 315 tests passed**, zero failed/skipped.
Its source matched the reviewed workspace excluding generated `bin`/`obj`.
Independent review found and closed the late reload-recovery and character-ID/
inventory-location issues; regression tests cover those paths and reopened
SQLite state. The prior full integration run's two stale damage-report
expectations were corrected against the original absorption contract and the
final suite includes full Lightning report parsing. Original-client sessions
and the unimplemented mechanics above remain separate fidelity verification.

Tested image
`sha256:c0649d7af72072c54b5e3ad9f9dc95d0c081d7e83262d38134d5416165dbba18`
replaced the game service at **19:34:01 UTC**, with `Server ready!` at
**19:34:10 UTC**. Initial verification found it running with zero restarts and
no unhandled/OOM/fatal startup lines. Auth's image and start time are unchanged.
This pass includes no schema migration or bulk world-data rewrite.

All three fresh SQLite backups passed integrity checks. The private deployment
configuration, reviewed source/docs, build/test logs, source comparison, old
and startup logs, and before/after metadata are retained in
`/home/blizz/backups/rasa-net/20260912T193344Z-retail-weapon/`.
Rollback image: `rasa_net:before-retail-weapon-20260912`. Retag it as
`rasa_net:latest` and recreate game with `--no-deps --no-build`; code rollback
requires no database restoration. The full final-live preservation goal
remains active.


## Primary attacks and inventory sessions — 2026-09-12

The [original primary attack catalog](weapon-attack-client-evidence.md) now
supplies all 198 action-1/action-174 rows to a captured execution lifecycle.
Manual requests retain their action pair, entity-or-location target, and
alternate flag. Admission checks the equipped primary pair, readiness, jam,
ammunition, current action and shared reuse. An unsupported specialized or
alternate request cannot execute an ordinary primary shot in its place.

The attack resolves after its original windup, remains busy through recovery,
and observes action-ID reuse across arguments. The 1/66 no-reuse flag and
174/32 literal 4/5/2 ms stages remain intact. Autofire schedules its first and
subsequent repeats from these stages instead of uniform template refire data.
Movement preserves these original primary actions. Interruption before
resolution causes no magazine debit or damage; later interruption preserves
already-spent ammunition and reuse.

Resolved attacks use the requested eligible target rather than the actor's
separate tracking target. Missing/dead/friendly targets become blind shots,
and an entity number reused by another object cannot receive a captured shot.
Ammo commits conditionally against the expected stored count and the captured
weapon's account/character/drawer ownership before memory changes. The original
server's precise ammo debit/impact ordering remains an inference. Native
geometry, LOS, target categories beyond the existing creature path, damage
modifiers and specialized/alternate behavior remain required reconstruction.

The [inventory session fixes](inventory-session-evidence.md) filter private
items by selected character before entity publication, retain the active drawer
through login and swaps, and persist empty weapon selections. Invalid stored
rows no longer prevent valid items from loading. Equipment, appearance and
weapon-information packets snapshot their queued values so later mutations do
not alter earlier updates. Map changes retire the prior inventory entities and
rebuild fixed-capacity lists before republishing items; reusing the character
object no longer grows those lists or skips occupied slots. Initial weapon
appearance is reconciled with the selected item before actor publication, and
an empty selection clears appearance/readiness. Existing second-hue persistence
and original packet-order details remain evidence gaps.

The [world equipment audit](world-equipment-client-audit.md) found exact numeric
matches for all 2,946 weapon classes, 3,377 armor classes and 30,225 template/class
mappings. No bulk rewrite is warranted. The weapon archetype field now reads
the original template ID rather than the class-row ID. All 2,440 weapon template
records still have the same 22 fields, and original server-supplied instance
stats cannot be reconstructed from the class table alone. The audit preserves
4,341 original nulls currently flattened to database zero as a separate gap.

Generic item requirements were incorrectly joined by template ID. The original
client reads those 5,293 requirement rows by item class; this distinction changes
expected requirements for 19,576 loaded templates. Correcting the loader uses
the existing original-matching rows without migrating stored world data.
Skill/race requirement rows and equipment slot mappings also match the original
tables; complete equipment eligibility still needs its own implementation audit.


The final candidate built with zero errors and the same five existing unused
variable/field warnings. **All 380 tests passed**, with zero failures/skips,
inside that candidate image without production database mounts or networking.
The image's source exactly matched the reviewed workspace excluding generated
`bin`/`obj`. Independent review closed first-autofire repeat scheduling and
repeated map-load/initial appearance defects before the final build. The final
suite includes 27 primary lifecycle cases, 18 inventory session cases and two
real-loader requirement cases alongside the prior regressions.
These tests verify implementation behavior; they do not supply missing original
server evidence or certify a complete final-live client session.


Tested game image
`sha256:ba9950bab9556f4932c973822a5730dedcc360bcfaf07517a69b7f15497668e2`
was deployed at **20:05:38 UTC** and reported `Server ready!` at
**20:05:48 UTC**. Initial checks show the expected image running with zero
restarts and no error/unhandled/OOM/fatal startup lines. Auth's image and start
time are unchanged. This pass has no schema migration or bulk world-data rewrite.

All three fresh SQLite backups passed integrity checks. Private deployment
configuration, source/docs, review patch, build/test logs, source comparison,
and before/after metadata are retained in
`/home/blizz/backups/rasa-net/20260912T200524Z-retail-attack/`.
Rollback image: `rasa_net:before-retail-attack-20260912`. Retag it as
`rasa_net:latest` and recreate only game with `--no-deps --no-build`; code
rollback needs no database restoration. The full final-live preservation goal
remains active, including original-client session verification and the gaps
in the linked evidence reports.


## Equipment eligibility, race and item destruction — 2026-09-12

The [equipment runtime](equipment-runtime-evidence.md) now checks original
current-attribute, inclusive level, race, positive skill-minimum and condition
rules. Integer condition preserves the original floor-division quirks and
rejects exactly zero. Both equipment endpoints validate a living avatar and
generated class slot before changing inventory. Personal and direct Home
swaps/unequips now commit both exact locations atomically, preserving character
IDs versus account-home owner zero. Failed writes leave inventory, pending
weapon work and queued success notifications untouched.

The [item-state audit](item-condition-client-evidence.md) found a missing
`RaceId` message. Initial actor data now publishes it before control/equipment,
allowing the original client's race checks to operate. It also found two
opposite trade-flag interpretations: the template loader stored a negative flag
in a positive property, and ItemInfo wrote that property as a negative flag.
Those mistakes canceled for populated ItemInfo rows but inverted tooltips.
The loader, ItemInfo and tooltip now agree; missing-template defaults preserve
the prior wire value and remain explicit placeholders. ItemInfo snapshots all
existing fields when queued.

The [destruction repair](item-consumption-evidence.md) conserves partial and
full item quantities in personal/home inventories. Expected count, registered
instance, account, owner, location and exact item identity are checked before
commit. Full removal updates the count and removes the correct inventory link
atomically; memory and client updates follow success. Excess quantities cannot
wrap, malformed wide values cannot narrow into small deletions, and a decoded
zero quantity is a no-op. Original item wear, repair economics and retention
policies are not inferred from these consistency fixes.

The full final-live goal remains incomplete. Clan equipment routes, storage
permissions/access, binding and uniqueness, remaining inventory operations,
complete mech equipment, original-client sessions and the broader mechanics/
content gaps remain tracked in the linked reports.


Final review also corrected the appearance-save failure path: a provider or EF
save error is logged without aborting stat/equipment refresh after a committed
swap. An isolated trigger-induced failure verifies that both the new armor
maximum and equipment notification still reflect the committed item. Malformed
equipment field types now use the connection's handled message exception.


The final .NET 5 candidate built with zero errors and the same five existing
unused-variable/field warnings. **All 469 tests passed**, with zero failures or
skips, inside the final image without production database mounts or networking.
Its source exactly matched the reviewed workspace excluding generated
`bin`/`obj`. Independent review confirmed the original eligibility predicates,
account/character/Home ownership, transaction rollback and the repaired
appearance-error path. Tests do not certify original-client sessions or supply
missing final-live mechanics and server policy evidence.


Tested image
`sha256:f27ad2110ccb77352b7cb23a714442d51fbb12fd14957e54cb8fb9410b3ca2e6`
replaced the game service at **20:31:30 UTC** and reported `Server ready!` at
**20:31:39 UTC**. Initial verification found the expected image running with
zero restarts and no error/unhandled/OOM/fatal startup lines. Auth's image and
start time are unchanged. No schema migration or bulk world-data rewrite was
introduced.

All three fresh SQLite backups passed integrity checks. Private deployment
configuration, reviewed source/docs, patches, build/test logs, source comparison
and before/after metadata are retained in
`/home/blizz/backups/rasa-net/20260912T203118Z-retail-equipment/`.
Rollback image: `rasa_net:before-retail-equipment-20260912`. Retag it as
`rasa_net:latest` and recreate only game with `--no-deps --no-build`; this code
rollback needs no database restoration. The complete final-live preservation
goal remains active.

## 2026-09-12: original lockbox credit request and conserved transfers

Static inspection of the 1.16.5.0 client proves that the lockbox UI accepts any
positive integer amount, deposits it unchanged and negates it for withdrawal.
The emulator's explicit temporary 500-credit workaround has been removed now
that compact signed decoding is repaired. Wallet and account-bank updates commit
in one transaction with ownership and expected-balance comparisons; failures
leave both persisted balances and session state unchanged. A withdrawal no
longer passes through the loot reward notification helper. No retail bank cap
or original error message is inferred from the existing storage limits.

Provenance, exact original function/offset references, implementation boundaries
and outstanding fidelity gaps: [lockbox credit evidence](lockbox-credit-client-evidence.md).

The final .NET 5 image built with zero errors and the same five existing unused
variable/field warnings. **All 520 tests passed**, with no failures or skips,
inside that image without production database mounts or networking. Source
comparison matched the reviewed workspace excluding generated `bin`/`obj`.
These checks verify the correction; original-service capture comparison and
complete final-live fidelity remain outstanding.

Tested image
`sha256:913a80537fdc87667aa8c33803ef606bb50a5e3fa52cc8c7de8cbc26b393689c`
replaced the game service at **20:43:25 UTC** and reported `Server ready!` at
**20:43:36 UTC**. Startup verification found zero restarts and no error,
unhandled, fatal or OOM log lines. Auth's image and start time are unchanged.
No schema migration or bulk world-data rewrite was introduced.

All three fresh SQLite backups passed integrity checks. Private deployment
configuration, reviewed source/docs, patch, build/test logs, source comparison
and before/after metadata are retained in
`/home/blizz/backups/rasa-net/20260912T204316Z-retail-credit/`.
Rollback image: `rasa_net:before-retail-credit-20260912`; retag it as
`rasa_net:latest` and recreate only game with `--no-deps --no-build`.
Code rollback requires no database restoration. The complete final-live
preservation goal remains active.

## 2026-09-12: original lockbox tab prices and purchase correction

Recovered the original five-row lockbox tab table and its client consumers.
Tabs contain 96 slots each; additional tabs cost 100,000, 1,000,000, 10,000,000
and 100,000,000 wallet credits. The previous purchase handler mistakenly added
the price through a positive signed adjustment. It now deducts the recovered
price and unlocks only the next tab in one account-scoped transaction. The
original living-avatar and affordability predicates are enforced, and stale
or failed purchases publish no payment or unlock. Existing bank credits are
preserved when changing tab ownership.

Evidence and outstanding full-bank fidelity requirements:
[lockbox tab reconstruction](lockbox-tab-client-evidence.md).

The final .NET 5 image built with zero errors and the same five existing unused
variable/field warnings. **All 541 tests passed**, with no failures or skips,
in the final image without production database mounts or networking. Source
comparison matched the reviewed workspace excluding generated `bin`/`obj`.
These checks verify the implementation, not complete original-service fidelity.

Tested image
`sha256:947f2ae3a05a032a4d1355edf1a9ba7181bd085e2f7abc53ae6a1760f2acab2c`
replaced the game service at **20:51:29 UTC**, reporting `Server ready!` at
**20:51:39 UTC**. Verification at 20:52:04 UTC found the expected image running,
zero restarts and no error/unhandled/fatal/OOM log lines. Auth's image and start
time are unchanged. No schema migration or historical balance rewrite occurred.

All three fresh SQLite backups passed integrity checks. Private deployment
configuration, reviewed source/docs, patch, build/test logs, source comparison
and before/after metadata are retained in
`/home/blizz/backups/rasa-net/20260912T205118Z-retail-tabs/`.
Rollback image: `rasa_net:before-retail-tabs-20260912`; retag it as
`rasa_net:latest` and recreate only game with `--no-deps --no-build`.
Code rollback requires no database restoration. Full final-live preservation
remains incomplete and the goal stays active.

## 2026-09-12: original storage quantities and atomic item placement

Original client calls confirm that personal/Home moves carry a selected quantity.
The old handlers ignored it, and the Home withdrawal decoder discarded long
quantities. The four requests now share strict decoding; whole moves and swaps
commit both item locations before publishing changes. Selected partial amounts
split into empty slots with one transaction covering the count decrease, new
item and placement. Persisted instance data survives a split and reload.

The original 96-slot tab ranges, personal categories, living-avatar predicate and
no-lockbox flag govern admission. Home operations compare persisted tab ownership,
including equipment transfers. Original combining rules for occupied stacks
remain unverified and incomplete; this is not a claim of full inventory fidelity.

Provenance, exact original consumers, validation and remaining requirements:
[inventory placement reconstruction](inventory-placement-client-evidence.md).

The final .NET 5 image built with zero errors and the same five existing unused
variable/field warnings. **All 577 tests passed**, with no failures or skips,
in the final image without production database mounts or networking. Source
comparison matched the reviewed workspace excluding generated `bin`/`obj`.
The tests prove the corrected storage invariants and packet decoding; original
service capture comparison and complete inventory fidelity remain outstanding.

Tested image
`sha256:cd22fb83bc58e2bc111f1ff73d9a3048302a2131c605ad0bd124444d4442f9fd`
replaced the game service at **21:06:08 UTC**, reporting `Server ready!` at
**21:06:17 UTC**. Verification at 21:06:48 UTC found the expected image running,
zero restarts and no error/unhandled/fatal/OOM log lines. Auth's image and start
time are unchanged. No schema migration or historical item relocation occurred.

All three fresh SQLite backups passed integrity checks. Private deployment
configuration, reviewed source/docs, patch, build/test logs, source comparison
and before/after metadata are retained in
`/home/blizz/backups/rasa-net/20260912T210552Z-retail-placement/`.
Rollback image: `rasa_net:before-retail-placement-20260912`; retag it as
`rasa_net:latest` and recreate only game with `--no-deps --no-build`.
Code rollback requires no database restoration. The full preservation goal
remains active while original systems and verification are incomplete.

## 2026-09-12: starter equipment character diagnosis

A level-1 Recruit with no saved skills could not satisfy the original Firearms
1 and Motor Assist Armor 1 equipment requirements. The user's requested
character update trained those two skills using the existing point budget;
three points remain. A verifier using the deployed server rules checked the
saved result and all four owned equipment items. No gameplay source, global
requirements, or automatic starter grants changed. See the
[starter equipment investigation](starter-equipment-research.md).

The same tested game image restarted at **21:23:43 UTC** and reported ready at
**21:23:52 UTC**. Verification at **21:24:26 UTC** found zero restarts and no
error/unhandled/fatal/OOM log lines; auth's image and start time are unchanged.
Private integrity-checked backups and operation records are retained in
`/home/blizz/backups/rasa-net/20260912T212342Z-blizz-training/`.

## 2026-09-12: Recruit initialization and atomic creation

The user's progression order is now recorded in `AGENTS.md` and
[the preservation sequence](progression-preservation-plan.md): creation and
final live boot camp first, then successive class tiers through endgame.

A recovered September 2008 wiki revision explicitly establishes all five
Recruit skills at rank 1. Original-client catalog mappings, an archived
official Recruit page and a contemporary level-1 image corroborate the result.
Creation now persists Firearms, Hand to Hand, Motor Assist Armor, Lightning
and Sprint at rank 1, leaving zero unspent points. Lightning retains its Power
Logos requirement. Character, appearance, initial skills/items, family-name
change and first bank tab commit together before creation success is sent.
Later characters preserve existing training and account bank state. Starter
items now receive their own maximum durability; the pistol previously used a
different template. Full starter loadout and tutorial reward fidelity remain open.

The diagnosed historical character received only its three remaining missing
initial ranks after a guarded comparison against its known state. Fresh
snapshot verification with the candidate's actual initializer and requirement
checker passed before and after. Only the skills table changed; no earned
progression, inventory or Logos was rewritten.

Evidence, artifact hashes, dated revisions and remaining first-segment gaps:
[new-character initialization](new-character-client-evidence.md).

The .NET 5 candidate built with zero errors and the same five existing unused
variable/field warnings. **All 584 tests passed**, with no failures or skips,
inside the final image without production database mounts or networking.
Source comparison matched the reviewed workspace excluding generated `bin`/`obj`.
Seven new integration cases exercise creation, persistent state and rollback;
passing tests establish implementation behavior, not full original fidelity.

Tested image
`sha256:b319d6018f9f450743315e5f13b4988d07776d14588c96f2a438178c6d07af09`
replaced game at **22:11:21 UTC**, reporting ready at **22:11:30 UTC**.
Verification at **22:11:50 UTC** found the expected image running, zero restarts
and no error/unhandled/fatal/OOM log lines. Auth's image and start time are
unchanged. No schema migration was required.

All three fresh SQLite backups passed integrity checks. Reviewed source/docs,
private deployment configuration, repair scripts, before/after snapshots,
table hashes, build/test logs and service metadata are retained in
`/home/blizz/backups/rasa-net/20260912T220953Z-retail-creation/`.
Rollback image: `rasa_net:before-retail-creation-20260912`; retag it as
`rasa_net:latest` and recreate only game with `--no-deps --no-build`.
Code rollback does not require restoring the character database. The full
creation/tutorial segment and overall preservation target remain incomplete.

## 2026-09-12: original first-family creation message

Original client code sends `CreateCharacter` (436) with six fields while the
account has no family name, and `RequestCreateCharacterInSlot` (512) with seven
fields for later characters. The first form had no server handler. Both now
reach the same transactional initialization; selection transmits `None` for
an unchosen family so the client can take its original first-family branch.
Persisted account/slot checks reject replay or duplicate initial grants even
when cached account state is stale. Both wire shapes are checked before reading
their fields, and oversized slot integers cannot wrap into an existing pod.

[Creation evidence](new-character-client-evidence.md#first-family-protocol)
records exact client consumers, opcode data, the slotless first-request
compatibility choice and remaining original-session verification. The 95
original starter appearance mappings were audited with no DB differences.

The rebuilt tutorial's four mission IDs, 19 objectives and nine dialogue-package
bindings are now preserved in [the boot-camp catalog](evidence/bootcamp-client-catalog.json)
and explained in [the boot-camp audit](bootcamp-client-evidence.md). The original
map was acquired and CRC/hash checked. This research does not insert speculative
quests or move characters into an unpopulated tutorial: exact spawns, reward
amounts, triggers, start position and skip behavior remain unresolved.

The .NET 5 candidate built with zero errors and the same five existing unused
variable/field warnings. **All 598 tests passed**, with no failures or skips,
inside the final image without production database mounts or networking.
Source comparison matched reviewed code excluding generated `bin`/`obj`.
The 21 focused creation/packet cases cover both messages, persistence and
rollback, slot/replay checks, appearance fields and family-state encoding.

Tested image
`sha256:6d635509c10b47ba63aab47f0d1fc5b2544454920d0d38f64c0205f78c08235e`
replaced game at **22:30:10 UTC**, reporting ready at **22:30:19 UTC**.
Verification at **22:31:25 UTC** found the expected image running, zero restarts
and no error/unhandled/fatal/OOM lines. Auth's image and start time are unchanged.
No schema migration or saved-character rewrite occurred in this deployment.

All three fresh SQLite backups passed integrity checks. Source/docs, private
deployment configuration, build/test logs and before/after service records:
`/home/blizz/backups/rasa-net/20260912T223008Z-retail-entry/`.
Rollback image: `rasa_net:before-retail-entry-20260912`; retag it as
`rasa_net:latest` and recreate only game with `--no-deps --no-build`.
Code rollback requires no database restoration. Complete final-live creation,
boot camp and new-character-to-endgame progression remain incomplete.


## 2026-09-12 — Original Recruit outfit tint and tutorial entry audit

New characters now persist white RGBA for the fixed Recruit boots, vest and
legs, matching the recovered original creation window. The previous packed
value produced gray with partial alpha. Both first-family and later-character
integration cases verify persistence. Chosen appearance fields, inventory-item
colors and existing character records are unaffected. Exact original source
locations and hash are in [creation evidence](new-character-client-evidence.md).

The [boot-camp audit](bootcamp-client-evidence.md) now records the original
skip-prompt conditions and the current first-login context mismatch. Additional
map audio placements and an archived Google Code emulator were checked; neither
supplied the missing original spawns or working quest definitions. Full initial
loadout, boot camp and subsequent progression remain incomplete.

The .NET 5 image built with zero errors and five existing warnings. All **598
tests passed**, zero failed/skipped, without production database mounts or
network access. Reviewed source matched the final image excluding `bin`/`obj`.
Image `sha256:e031437f52a415d8218001ad694c5603c8f67b08e6800456a3bc8bea51424efa`
started game at **23:26:24 UTC**, ready at **23:26:34 UTC**. Verification at
**23:26:49 UTC** found the expected image running with zero restarts and no
error/unhandled/fatal/OOM lines. Auth's image and start time are unchanged.

All three SQLite backups passed integrity checks. Reviewed source/docs,
configuration, test/build logs and service records are preserved at
`/home/blizz/backups/rasa-net/20260912T232612Z-retail-outfit/`.
Rollback image: `rasa_net:before-retail-outfit-20260912`; retag as `rasa_net:latest`
and recreate only game with `--no-deps --no-build`. No schema migration or saved
character rewrite occurred; code rollback needs no database restoration.


## 2026-09-13 UTC — Character selection failure handling and boot-camp audio evidence

Selecting an empty or unowned slot now leaves saved and session state intact.
Previously the session selected-slot field changed before ownership lookup or
saving succeeded. It now updates after the selected-slot/login save completes.
The original two-field selection message is checked explicitly, and oversized
or negative slot integers cannot wrap into a valid pod. This corrects emulator
entry failures; original error-response behavior and the complete first-login
and boot-camp skip path remain unverified.

Ten new cases cover original skip/no-skip packet decoding, malformed messages,
slot overflow, empty/unowned slots and a real SQLite login-save failure. The
23 focused creation/selection cases pass; the final image passes **all 608
tests**, with zero failures or skips and no production DB mounts or networking.
The .NET 5 build has zero errors and the same five existing warnings. Reviewed
source matches the image excluding `bin`/`obj`.

[Boot-camp evidence](bootcamp-client-evidence.md) now records the original
selection sender, the obsolete Elvers/Burba fansite guide and an unresolved
August 2008 description of the rebuilt tutorial's order. A new
[audio catalog](evidence/bootcamp-audio-catalog.json) preserves four ambient sets
and five named voice sets with original table hashes and bytecode offsets.
The voice filenames identify the four later tutorial missions and McAllister's
bark, but do not establish playback triggers, NPC placements or rewards.
Original audio playback was not acquired or heard. Full boot camp and subsequent
progression are still incomplete.

Image `sha256:7c81e87e6b6b8582c6a6d8b1089c72d152ddcca01ffee2cabee26e031d064f5f`
started game at **00:02:35 UTC**, ready at **00:02:44 UTC**. Verification at
**00:03:10 UTC** found the expected image running with zero restarts and no
error/unhandled/fatal/OOM lines. Auth's image and start time are unchanged.
All three fresh SQLite backups passed integrity checks; no schema migration or
saved-character rewrite was performed.

Source/docs, configuration, logs and service records:
`/home/blizz/backups/rasa-net/20260913T000223Z-retail-selection/`.
Rollback: retag `rasa_net:before-retail-selection-20260913` as `rasa_net:latest`
and recreate game alone using `--no-deps --no-build`. Code rollback requires no
database restoration. Original-client playthrough comparison remains pending.


## 2026-09-13 UTC — Mission-log protocol, NPC objective conversations and boot-camp evidence sweep

A verified six-track sweep (dated wiki history, original client tables and
code, contemporary captures, the map file and a server audit, each with an
independent verification pass) established that the original boot-camp map
contains no gameplay actors: first-login position, NPCs, crates and the exit
are server data that no recovered source supplies. Mission 2005 (the
"Calling for Reinforcements" retry), the client's mission-log limits,
objective indicator names, NPC name ids and Eloh speech are now catalogued.
Details and corrected citations: [boot-camp evidence](bootcamp-client-evidence.md#2026-09-13-verified-sweep-what-the-client-and-the-map-do-and-do-not-establish).
No tutorial content, spawn, reward or skip behavior was added.

The server now implements the recovered client contract that every
conversation-driven mission, including the boot camp's, depends on: `NPCInfo`
package ids, per-player conversation status and topics, objective completion
through `CompleteNPCObjective`, turn-in and abandon, persistent objective
progress restored through `MissionStatusInfo`, list-shaped `PlayerFlags`,
optional-integer reward selection and a dictionary-shaped `CanLootItems`.
Mission definitions are offered only when complete; the unvalidated seeds
321/429 are withheld, so their NPC markers no longer advertise them. Item
rewards, radio and shared missions stay explicit gaps; their requests are
decoded and ignored instead of disconnecting the client. Proven client facts
and emulator storage choices are separated in
[mission research](mission-research.md#mission-log-protocol-and-persistence--2026-09-13).

Two adversarial review rounds (four lenses, then a focused re-review of the
fixes, each finding independently verified) confirmed 31 reports, several of
them duplicates and none critical or high. Every code finding was fixed and
tested before deployment: reward display/payout mismatches, completeability
ordering, saved progress after definition changes, undeliverable item
rewards, radio/share requests and an invented chat message. The documentation
findings were corrected in these records, including the D11.4 source
(public-test notes, repeated in the D11.6 live notes), a reversed D13.4
paraphrase and overstated protocol claims.

Schema: `character_mission.change_time` (default 0) and new
`character_mission_objective`; world tables `npc_mission_objective`,
`npc_mission_objective_conversation` and `npc_mission_objective_transition`,
all empty. Migrations were generated with dotnet-ef 5.0.1 for SQLite and a
disposable MySQL 8.4.11 server; the MySQL chain preserved legacy mission rows
including state 4294967295. The SQLite scripts were dry-run against copies of
the live databases before deployment. Records are in
`/home/blizz/backups/rasa-net/research/20260913-bootcamp/mission-log-migrations/`.

The .NET 5 image built from a clean context with zero errors and the same
five existing warnings. **All 634 tests passed** in the image, with no
failures or skips and no production database mounts or networking. Reviewed
source matched the image excluding `bin`/`obj`. Passing tests verify the
implementation; the conversation flow has not yet been exercised with the
original client.

Image `sha256:d7a8e1163c686d78d8b98a6d9fcecbb876a742e3aa4f3f10285a60241598e511`
started game at **04:18:33 UTC**, ready at **04:18:49 UTC**, and authenticated
with auth. Verification at **04:19:13 UTC** found zero restarts, no
error/unhandled/fatal/OOM lines, both migrations applied, and auth's image and
start time unchanged. After startup the live character and world databases
differed from the fresh backups only in migration history and the new empty
tables; auth was unchanged; all passed integrity checks. The auth container
had independently logged a .NET "Out of memory." and restarted at 02:14 UTC,
before this deployment; this deployment did not touch it.

Backups, configuration, reviewed source/docs, logs, scripts and table
comparison: `/home/blizz/backups/rasa-net/20260913T041812Z-retail-missionlog/`.
Rollback: retag `rasa_net:before-retail-missionlog-20260913` as
`rasa_net:latest` and recreate game alone with `--no-deps --no-build`. The
previous image ignores the added column and tables; a database restore is
needed only to remove them.

## 2026-09-13 UTC — Boot-camp reconstruction foundations (S0) and verified footage

This deployment adds the data layer for reconstructing lost boot-camp server content under the
user's evidence-bounded reconstruction decision (`AGENTS.md`). It changes nothing visible: every
new table is empty, and nothing that could use it is implemented yet.

**What was added**
- **World tables** for mission prerequisites, objective bindings, counters, timers and
  indicators, plus content areas, placements, conditions, rules (with filters, including a
  placement-state filter), rule actions, item sets, locations and per-context map settings.
- **Character storage** for objective timers and counters and per-character content facts.
- **Staged character writes**, committed only through the unit of work.
- **A content validator that fails closed:**
  - It withholds any row with a bad reference, an invented column, an unimplemented mechanic,
    a usable kind with no recovered client state machine, a client-posted tutorial id, a rule
    cycle, or an offer of a mission that cannot be offered.
  - A withheld row withholds everything that references it.
  - A mission with a withheld row is not offered.
  - Startup logs every gap. This build implements none of the mechanics, so any seeded row
    would be withheld.
- **Boot-camp entry switch** (`GameDataConfig.Bootcamp`, default `Disabled`). Character creation
  consults it, but until the boot camp can run end to end it always gives the existing Wilderness
  start.
- **Lookup fixes:** use and loot requests that name an object which no longer exists are now
  ignored instead of throwing.

The plan's `map_info.instancing` column became a separate `content_map_setting` table, because
the world seed migration reflects `map_info`'s columns.

The machine-readable evidence contract is in commit `7ac7639`:
- manifest schema;
- boot-camp manifest with sources, reserved key ranges, gap register and open owner decisions;
- empty positions and footage-event files;
- validator, provenance registry and a SQLite/MySQL seed-parity harness, with one rejecting
  fixture per rule.

**Footage**
- Three original recordings supplied by the owner were transcribed frame by frame, with every key
  event independently re-verified (246 checked, 0 refuted), and matched against the client radar
  maps.
- Player chat dates the main session to about 2009-02-26, so it shows the final live boot camp.
- Findings, tags and remaining gaps are in [boot-camp evidence](bootcamp-client-evidence.md#2026-09-13-verified-footage-what-three-original-recordings-establish).
- Nothing from the footage is seeded yet.

**Review**
- An independent review found no startup or gameplay change with empty tables. It confirmed
  defects in staged writes after a delete, several fail-open validator paths, cycle detection
  that could miss members, and tests that could not catch propagation or entry-gate regressions.
- All were fixed. A focused re-review confirmed the fixes, and its three low-severity findings
  (orphan counters after a delete, untested propagation lines, a stale provenance registry)
  were fixed and tested before deployment.

**Schema and migrations**
- Generated with dotnet-ef 5.0.1:
  - SQLite and MySQL `MissionContentLayer` (world, `20260913180618`/`20260913180640`);
  - `MissionContentRuntimeState` (character, `20260913180522`/`20260913180550`).
- On a disposable MySQL 8.4.11 server:
  - legacy mission rows (including state 4294967295) and the seeded world rows survived upgrade,
    rollback and reapply;
  - no content key auto-increments, and an explicit id 0 is kept;
  - rollback drops the new tables and their rows, as designed.
- The SQLite scripts were dry-run on copies of the live databases: integrity ok, and every
  existing table was byte-identical.
- Records: `/home/blizz/backups/rasa-net/research/20260913-bootcamp/content-migrations/`.

**Image and tests**
- The .NET 5 image built from a clean context with zero errors and the same five existing
  warnings.
- **All 753 tests passed** in the image (634 existing, 91 evidence contract, 28 new), with no
  failures or skips and no network.
- The image's `src` and `docs/evidence` matched the reviewed tree.

**Deployment**
- Image `sha256:5ac800497000a21d739c1ab4dd263be5efb653f2d7e63e89ec9778ebabdeec56` started game at
  **18:43:36 UTC**, authenticated with auth at 18:43:46 and was ready at **18:43:48 UTC**.
- The log shows "Loaded 0 content rules (0 content rows, 0 gaps)" and "Boot camp entry:
  Disabled".
- Mission 321/429 gap lines are unchanged.
- Verification at **18:43:54 UTC** found zero restarts, no error/unhandled/fatal/OOM lines, both
  migrations applied, and auth's image and start time unchanged.
- After startup the live character and world databases differed from the fresh backups only in
  migration history and the new empty tables. Auth was unchanged, and all three passed integrity
  checks.

Backups, configuration, reviewed source and docs, logs and the table comparison are in
`/home/blizz/backups/rasa-net/20260913T184328Z-retail-content-s0/`.

**Rollback:** retag `rasa_net:before-retail-content-s0-20260913` as `rasa_net:latest` and recreate
game alone with `--no-deps --no-build`. The previous image ignores the added columns and tables.
A database restore is needed only to remove them.

## 2026-09-13 UTC — Seven-track evidence-source sweep; no code changed

A documentation-only research pass searched for external evidence bearing on final-retail
preservation: other emulator projects, protocol artifacts, packet captures, official
documentation, archived community sites, footage and non-English sources. Results, citations and
provenance tiers are recorded in [source sweep 2026-09-13](source-sweep-2026-09-13.md); raw
evidence and seven per-track reports (~531 MB, 9,033 files at hand-off) are at
`/home/blizz/backups/rasa-net/research/20260913-source-sweep/`. **No source, migration, database or
`docker-compose.yml` was modified and no server or container was run.**

The most consequential result is not a new external source. All **369** `data/game.zip` client
tables are already decoded and verified (0 mismatches) at
`research/20260913-bootcamp/list-tables/decoded/`, with 996 disassembly files and 47 decompiled
`.py` alongside, and **no file in `docs/` referenced any of it** — `client-artifacts.md` still calls
decoding them "the highest-value next step". Likewise `GameOpcode.cs` is a verified 1:1 copy of the
client's `methodid.pyo` (978/978, 0 mismatches), and client→server argument shapes for all **170**
methods the original client can send are recoverable from the disassembly, so no external opcode
list or packet capture is needed. Seven content tables (`armorclass`, `weaponclass`, `itemclass`,
`equipableclass`, `itemtemplate_itemclass` and both requirement tables) are verified **byte-exact**
against the client. Gaps are correspondingly narrower and better quantified: `logos` 166 of 390,
mission objectives 0 of 3,454, objective conversations 0 of 5,821, `map_info` 78 of 784, `vendor`
20 of 118, `creature` 140 of 4,099 names, and **56** `tutorialdata.pyo` tutorial events that
`progression-preservation-plan.md` records as unrecovered.

One confirmed defect: `itemtemplate_armor`'s five rows are hardcoded in
`ItemTemplateArmorPrelaoder.cs:18-22`, three of the five ids are **absent** from both the client's
`armorclass` (3,377 entries) and its name table, the two present ones are 3-tuples unrelated to the
stored scalars, and `ManifestationManager.cs:1094` sums the invented scalar while the verified
`Min`/`MaxDamageAbsorbed` are loaded and never used — so 2,500 of 4,985 templates in armour slots
mitigate nothing. `NewCharacterTests.cs:82-91` expects 47/70/59 where the seeder writes 59/70/0, and
the test does not catch it because it asserts against its own fixture rather than the seeded
database. Recommended: retire the table and derive from `armorclass`. **Do not** import Infinite
Rasa's 13,893 `itemtemplate_armor` rows — a join proves they are our own `armorclass` re-keyed, plus
one derived column matching `round(damageAbsorbed/10)` in only 96.90% of cases.

Several claims that circulated during the sweep were disproved before being recorded, and the
corrections are listed first in the new document: an `itemtemplate_armor` comparison value set that
appears nowhere in the decoded tables; two misattributed video ids (`ePEbTUrfQ0o` is a 2023 art
installation, `C2rGwo6fLw0` is Raisuly episode 17); a claimed EU-vs-US class-roster divergence that
was a truncated sample (38 `afs_class` slugs across de/en/fr are one roster translated — DE
`mikrobiologe` = EN `medic`, DE `xenobiologe` = EN `exobiologist`); the assumption that Operation
Immortality had an in-game component (its official feed is entirely real-world PR); an early CDX
sweep that scored throttling as `cdx=0`; and the official strategy guide's publisher, which is
**BradyGames** (ISBN 978-0-7440-0943-9, 272 pp., 2007-10-23), not Prima, with no public scan
anywhere.

Newly established and citable: the full official host estate (`www.rgtr.com`, `playtr.com`,
`boards.playtr.com`, `eu.rgtr.com`, `eu.playtr.com`, `webdev.ncaustin.com`) read from the German
D16.5 page's own region picker; a **40-deployment timeline with live and public-test-server records
kept separate** and 12 deployments carrying both, which dates when each change reached live players;
**D16.5's Angel/Vulcan under-50 mech fix corroborated verbatim in two official locales** (US 02/17,
DE 02/18), closing that item in `final-retail-target.md`; **trainer gates 5/15/30 attested by 37
official pages in three languages** captured 2008-11-20 → 2009-01-08; the **US final-live roster of
15 classes** with tier, armour, weapons and abilities; official producer and lead-designer statements
on **control points** (Starr Long) and **cloning** (Paul Sage) captured 2009-02-01; the **revamp
boundary, D11 live 2008-08-15**, which separates usable from forbidden boot-camp evidence; an
official **event inventory** from the gallery's own category list; and a complete GLM→CHNK→WAD
decoding path agreed by three independent tools, under which **boot-camp spawn positions become
recoverable from `adv_bootcamp.map` as `CVOGSpawnPoint` records at `original` tier** rather than
estimated from footage. Mission rewards are also structurally corrected: `QuestObjective` carries
`XPIndex`/`CreditsIndex` plus scalers into `tQuestXPLookup`/`tQuestCreditsLookup`, so a model that
stores flat XP or credit values is modelling the wrong thing.

Provenance discipline was tightened rather than relaxed. The whole emulator family is **one lineage**
(J.H.Work 2011 → Google Code `tabula-rasa-server-emulator` / InfiniteRasa → `InfiniteRasa/Rasa.NET`
2016 → this fork), so agreement between its members is not corroboration; only four facts in the
sweep have genuinely independent agreement. The upstream project's own charter states it targets
**client v1.11** and that *"we don't aim to reproduce the original worlds"*, which makes every
inherited default unverified by its authors' own statement — though a 2011-09-29 capture of its test
server demanding **1.16.5.0** shows the blurb was stale, so the client-revision question is recorded
as a four-source conflict rather than resolved either way. Confirmed losses are stated as gaps and
not filled: the official video library (27 FLV names recovered from static `.swf` inspection;
`ftp.playtr.com/movies/` never archived), any Tabula Rasa packet capture, the J.H.Work source
(origin-404 in both 2013 and 2023 crawls), Google Code source and wiki bodies, `infiniterasa.com`'s
371 development posts, the character planner, and — still — the final-live rebuilt boot camp and all
NPC/creature placements, behaviour and stat values. The EU-only "Antagonist"/"Adversaire" armour ids
122112–122128 have **zero** official documentation and remain undetermined as to retail, promo or
test-server origin, so they must not be seeded.

Search engines are captcha-blocked from this machine in both curl and a real desktop browser, and
`archive.org/wayback/available` returns 429 machine-wide, so the sweep adopted a hard rule: a 429,
`code=000` or truncated response is **`INDETERMINATE (throttled)`**, never "not found". Retry queues
are preserved per track. Outstanding work, blocked routes and the items needing a user decision
(contact `dahrkael`, who created a new TR launcher repo in July 2026; the Infinite Rasa Discord;
two small `irsingle` binaries; four client torrents, all 0-seeder) are enumerated in §15 of the new
document.

## 2026-09-13 UTC — Client objective tables seeded at original tier (schema approved by owner)

The owner approved the two schema changes that [mission research](mission-research.md) had
identified as the blockers for seeding the client's objective tables, and both are now deployed
(migrations `20260913234728_MissionObjectiveClientColumns` and `20260913235900_MissionClientObjectiveSkeleton`,
applied to the live SQLite world database and verified against a disposable MariaDB for the MySQL path):

- `npc_mission_objective_conversation` gained `convo_type` as a fifth primary-key column, making the
  table a lossless 1:1 image of the client's `objectiveconversation` (1,727 rows; 202 of the 1,140
  key groups carry more than one convoType, so the previous 4-column key could not represent 34% of
  the data). The runtime `MissionObjectiveConversation` now carries `ConvoType` for the
  completion/reminder/choice distinction the schema preserves.
- `npc_mission_objective`'s `ordinal`, `is_required` and `revealed_on_accept` are now nullable, and
  `comment` was widened to varchar(100) (223 of the 3,454 client objective names exceed 50 chars,
  max 90). The three flags are server-authoritative with no surviving source, so they stay NULL
  instead of receiving guessed defaults: `Mission.DefinitionGaps` now reports
  "objective N has unknown ordinal/required/revealed flag" per objective, which keeps every such
  mission unoffered (fail-closed) rather than asserting gameplay. The two mission-1990 rows seeded
  by `BootcampS1Initiation` keep their footage-tier values and are excluded from the skeleton.

Seeded at **`original`** tier, verbatim from the retail 1.16.5.0 client's `data/game.zip` members
`generated/client/missionobjective.pyo` (3,454 rows) and `generated/client/objectiveconversation.pyo`
(1,727 rows), decoded through `python/client/clientlanguagemanager.py` in `trpython.zip`:
**3,454** `npc_mission_objective` rows (mission_id, objective_id, name as comment) and **1,727**
`npc_mission_objective_conversation` rows (all five key columns). Every value resolves through the
client's own text-id indirection with zero exceptions (see the 2026-09-13 sweep section and
`mission-research.md` for the full table semantics and extraction recipes). Provenance is recorded
in the seed rows class header (`MissionClientObjectiveSkeletonRows.cs`); no manifest is used because
no field is estimated — the seed is a verbatim import, like the Logos and MapInfo seeds.

Deployment consequence: missions 321 and 429 now load their client objectives (310; 4 and 5) and
report precise per-objective unknown-flag gaps instead of "no objectives"; both remain unoffered,
as before. 3,449 objective rows and 1,721 conversation rows reference missions whose
server-authoritative `npc_mission` columns are unrecovered; `LoadMissions` logs this expected state
as one summary line each instead of one error per row. The previously observed withholding of the
mission-1990 offer rule ("objective has no completion binding" at catalog-build time, because
content bindings attach after the catalog computes gaps) is pre-existing fail-closed behavior, not
changed by this work.

Verification: full test suite 777/777 green; both provider migrations applied forward and the
SQLite pair also reverted (Down preserves the 1990 footage-tier rows); container rebuilt and the
game server starts clean with the new schema.

## 2026-09-14 UTC — Merge regressions resolved; player death and hospital recovery

The EllimistArcade merge (`e06035a`) is resolved in `0678c85`: 128 failures under the .NET 5 CI
runtime were regressions where the merge took the other branch over this branch's tested and
evidence-backed code (disconnect contract, bounded protocol-frame parsing, weapon draw/reload/stow
through `WeaponActionManager` — the merged handlers queued reloads that never resolved — skill and
attribute validation, level-up point deltas). The local SDK 8 runtime hid them behind EF Core 5
startup failures; verification now runs in the `mcr.microsoft.com/dotnet/sdk:5.0` image.

Player death is now playable end to end: lethal hits kill, Hospital Selection offers the hospitals
the character knows, and the chosen hospital revives the player at its client map marker with full
health. The boot camp offers Refugee Base Medic (graveyard 20000001) as in the final-week footage
(A4-36), and the respawn position matches the measured respawn to 1.1 m. Wilderness hospitals are
gained within 100 m with the original "You just gained" message. Details, sources and remaining
gaps (trauma, equipment wear, ally revival, death persistence, control points):
[player-death-implementation.md](player-death-implementation.md) and
`docs/evidence/hospital-catalog.json`. Full suite 801/801.

## 2026-09-14 UTC — Creature kill experience, credits and kill streak

The emulator paid `creature level × 100 ± 10%` experience and 1–10 random corpse credits. Kills now
pay from the 1.16.5.0 client's `shared/gameconstants.py` values (`BASE_KILL_XP 62.5`,
`STREAK_BASE_PER_PARTY_MEMBER 3`, `STREAK_LEVEL_BASIS 10`, `STREAK_MAX_VALUE 5`,
`MAX_KILLING_STREAK_PRESTIGE_POINT_BONUS 1`) and a fit to every clean kill line in the final-week
footage: base experience `62.5 + 4.2·L + 0.2·L²` (L = creature level), truncated only after the
streak multiplier as `shared/xpinfo.py ApplyModifier` does, and `5·L` credits paid at the kill
through `GotLoot`. The fit reproduces all nine recorded observations for levels 1–9, including the
four streak-doubled values (133, 143, 189, 233) that would be one lower if the base were rounded
first. The third kill in a streak sends `SetKillStreak(1)`, one prestige point with PM 10000134, and
doubles experience; a streak ends 15 s after the last kill (bounded to 12.5–16.1 s by A4). The
final-week cave-fight timeline (A3-081 to A4-28) is reproduced exactly by `KillRewardTests`.
Level-difference, squad, partial-credit and crit-kill modifiers and original loot tables remain
gaps (`docs/evidence/kill-rewards.json`). Full suite 811/811.

## 2026-09-14 UTC — Private boot-camp instances, S4 mechanisms, fork navmesh work

- **S3:** context 1985 is a per-character instance; see
  [progression-preservation-plan.md](progression-preservation-plan.md#s3-private-instances-status).
- **S4 mechanisms:** kill bindings with objective counters, staged `grant_rewards`, and owner-conditioned
  placement presence in instances are implemented. No mission 1994 content is seeded yet.
- **Fork work merged (code only):** EllimistArcade's commits after `369a663` bring per-map Detour
  navmeshes built from the client's own terrain heightmaps and collision volumes (creatures path on the
  mesh instead of floating through rock), crafting stations placed at the client's `CRAFTING_STATION`
  markers (recipes still decline), and item repair `ItemStatus`. The wander pacing of `c5634b9` (20 m,
  1.6 m/s strolls, 12–40 s idle) is emulator tuning with no retail source. The 328 MB of built `.nav`
  files are derived client assets and stay outside Git (a copy is at
  `/home/blizz/backups/rasa-net/navmesh-8b65ca7/navmesh`; `GameDataConfig.NavMeshPath`, default `navmesh`).
  Without the files, creatures keep straight-line movement. Full suite 825/825.

## 2026-09-14 UTC — Capture the Flag (S4) content seed

- Mission 1994 and its boot-camp content are seeded (`BootcampS4CaptureTheFlag`); details, labels and
  decisions in [progression-preservation-plan.md](progression-preservation-plan.md#s4-capture-the-flag-status)
  and the boot-camp manifest. Values are evidence-bounded reconstructions, not recovered server data: the
  giver, receiver, transitions 4→2 and 1→3, the cave-in trigger radius, the boss position and presence, and
  Tizzik Gi's level are inferred; creature classes, health, speeds and the Thrax attack (emulator
  `creature_action` 33, whose attack pair matches the client's boot-camp Bane pistol) are labelled analogues.
- Not reproduced: the boss fight itself (never recorded), escorts and allies, 1994 credits and item reward,
  Thrax respawn, Youngblood's appearance and walk-in, and the original attack damage and timing.
  Full suite 839/839.

## 2026-09-14 UTC — Calling for Reinforcements (S5) and exit to Alia Das (S6) content seed

- Missions 1995 and 2005 and the boot-camp exit are seeded (`BootcampS5Reinforcements`, `BootcampS6ExitToAliaDas`);
  details, labels, conflicts and decisions in
  [progression-preservation-plan.md](progression-preservation-plan.md#s5-calling-for-reinforcements-seed-status)
  and the boot-camp manifest. Values are evidence-bounded reconstructions, not recovered server data: the objective
  order except 1 → 4, the corpse and wounded-soldier positions, the reinforcement positions, the wreck and bomb
  classes, the 2005 level, the exit radius and the indicator ids are inferred; the bomb windup and fuse, Van
  Valkenberg's position, the indicator positions and the Alia Das arrival are measured; the 1995 timer (600 s), NPC
  classes, levels and health, and the corpse class are labelled analogues. OD-25..OD-34 were decided by the agent for
  the owner and await owner review.
- Not reproduced: the 1995/2005 rewards, the hidden level-3-to-4 experience before Alia Das, a bomb inventory item,
  detonation damage, the reinforcement dropship, beam-in and walk-off, the unnamed reinforcements and other outpost
  creatures and NPC appearance. The D13.4 abandon quirk is kept. Rogers now stands in the Alia Das command tent
  (`BootcampFixRogersTurnIn`: level observed, position measured, rotation inferred, class and health analogues) and
  takes the 1995/2005 turn-in, which still pays nothing. Full suite 846/846.

## 2026-09-14 UTC — Wilderness arrival: Training Day (segment 3, W1) content seed

- Mission 1526 Training Day, Training Officer Kincaid at Alia Das and the forced Headquarters offer on entering
  Alia Das are seeded (`WildernessArrivalTrainingDay`). Details, labels and decisions are in
  [progression-preservation-plan.md](progression-preservation-plan.md#w1-wilderness-arrival-training-day-status) and the
  boot-camp manifest (slice W1). These are evidence-bounded reconstructions, not recovered server data:
  - observed: the offer, its 120 credits (partly legible) and the reward names, and the tooltip range and alt damage;
  - measured: Kincaid's position;
  - inferred: the offer trigger, Kincaid's level (partly legible glyph), rotation and package, the mission level,
    category and shareable flag, and the reward template ids 116929/116930;
  - labelled analogues: Kincaid's class and health, the reward flags and the unevidenced weapon fields.
- The Training Day reward pistols now exist as item templates (`itemtemplate`, `itemtemplate_weapon`). Their offer
  still reads "Pistol"/"Pulse Pistol" without the Vextronics module line, and they carry no price.
- Not reproduced: Training Day experience, the offer delay after the transfer, an offer for characters who skip the
  boot camp, Kincaid's appearance and observed facing, and missions 2010/2011 (held for a class-chosen trigger). The
  emulator's Major Bonham spawn beside the arrival is unchanged. OD-36..OD-42 were decided by the agent for the owner
  and await owner review. Full suite 849/849.

## 2026-09-15 UTC — Class gear: "Getting It In Gear" (segment 3, W2) content seed

- Missions 2010 "Getting It In Gear: Soldier Class" and 2011 "…: Specialist Class" and their class load-out are seeded
  (`WildernessClassGear`), answering the tier-2 class choice that `65cafcb` added the `class_selected` event for. This
  closes `GAP-W1-GEAR-MISSIONS` and supersedes the OD-42 hold (OD-43). Details and labels are in
  [progression-preservation-plan.md](progression-preservation-plan.md#w2-class-gear-missions-20102011-getting-it-in-gear-status)
  and the boot-camp manifest (slice W2). These are evidence-bounded reconstructions, not recovered server data:
  - original (client): the two mission ids, texts and category names 10000002/10000003, the completion package 133, the
    twelve D11 item templates 122859-122871 and their item classes, the class-owned skills that carry them
    (21/22 Soldier, 30/14 Specialist), and the armor values (client itemclass `max_hp`);
  - inferred: the radio giver 0, receiver creature 132, mission level 5, the one objective, the class_selected rules and
    their two-term conditions, the `forced` dispatch, the per-mission reward split and the neutral 0 prices;
  - labelled analogues (OD-43): the templates' quality 2 and trade/binding flags from the D11 new-player block's uniform
    world-seed rows, and the Rage-O-Matic/Repair-O-Matic `itemtemplate_weapon` columns from the world seed's machine-gun
    and tool family rows.
- Quartermaster Caufield is **not** a new placement: the world seed's own spawnpool 210 spawns creature 132 "AFS
  Quartermaster Caufield" (name 2992, level 10, class 29423) in shared Alia Das at the supply tent, 1.6 m from the pre-D11
  TaRapedia `/loc`. The missions attach the original dialogue package 133 to that creature and complete at him. His class
  is a plain Redshirt body (client entityclass augmentation list [1]) and no appearance rows exist, so whether retail used
  this body and the client renders him interactable is unverified (`GAP-W2-CAUFIELD`).
- The NPC load was fixed: `CreatureInit` aborted startup with a null-reference on the first mission that names a creature
  whose class has no NPC augmentation (Caufield), and it bound an `npc_package` row only when the class carried that
  augmentation, which silently dropped Caufield's package. Both bindings now follow the mission and package data;
  `CreatureNpcBindingTests` covers the regression.
- Not reproduced: the missions' XP and credit rewards, a real item price, the per-template weapon statistics, the offer's
  presentation (whether the client showed it as a broadcast and greyed Decline), and any capture of the gear tooltips.
  `GAP-W2-*` records each. OD-43 was decided by the agent for the owner and awaits owner review, as do OD-25..OD-42.
- Deployed 2026-09-15 against the live world database: `WildernessClassGear` is applied (2 missions, package 132 -> 133,
  4 condition rows, 2 rules, 12 item templates) and the game now reports `Loaded 14 content rules (130 content rows,
  0 gaps)` and `Successfully authenticated with the Auth server!`. The first deployment of this slice aborted startup on
  the NPC-load defect above; the rebuilt image fixes it and `CreatureNpcBindingTests` guards it.
- Upstream merge, 2026-09-15 (**PR #91**, commit `e64d6a1`, "Send players the regions they stand in…"): the PR was
  **closed, not merged**, upstream; the commit is the only one in the PR branch that our `development` did not already
  carry, and it is now merged here (`fcb744b`). It adds the `map_region` table, `RegionManager` (per-second per-map check,
  `UpdateRegions` sent on change and once on map entry), `MapRegionEntry`/`MapRegionRepository`, the `NavMeshFlags.
  Underground` flag with `NavMeshQuery.IsUnderground`, GM commands (`.regions`, `.region`, `.setregion`), `docs/regions.md`,
  and a preload of **373 volumes on 58 maps**. Provenance: emulator implementation (**supporting evidence**, not proof of
  final retail behaviour). The volumes themselves are *not* original: the original server decided regions from volumes that
  are lost, and no `.map` carries a `GBB_RegionTrigger` entity; the rows are derived from client artifacts
  (`generated.client.uimapmarker` REGION_LABEL positions for surface circles, `generated.client.gamecontextuiradarinfo`
  minimap rectangles negated in z for boxes), i.e. `measured`/`inferred` with the method documented in `docs/regions.md`.
  Confidence: region ids, names and the client's `UpdateRegions` behaviour are client-backed; the *shapes* are
  approximations — label circles use half the distance to the nearest label (60–200 m) and 51 boxes are marked
  underground-only. 21 volumes sit on the Wilderness context 1220 (Alia Das, Ranja/Pinhole Falls caverns, the outpost
  villages); regions with neither a label nor a minimap (e.g. Alia Caverns) have no volume yet. **Unverified**: the
  underground flag cannot take effect with the `.nav` files in this repository, which were built before
  `NavMeshFlags.Underground` — cavern regions report the surface until the navmeshes are rebuilt (the commit says old files
  load unchanged). Also note `20260915120000_Add_map_region.Designer.cs` carries upstream's snapshot, which lacks our
  content-layer tables; the standalone `*ContextModelSnapshot.cs` files auto-merged correctly and are what scaffolding
  reads, but the Designer is inconsistent with the convention the other migrations follow.
- **Final live deployment identified (2026-09-15, official patch notes)**: the official patch-notes index
  (`playtr.com/news/patch_notes/index.html`, last pre-shutdown capture) lists every deployment: **D11 = 2008-08-15**
  ("brand new Tutorial" — the rebuilt boot camp our S1–S6/W1 slices reconstruct), D12 = 2008-09-18 ("new version of the
  tier selection process" — the feature our W2 slice implements), D13 = crafting, **D14 = 2008-11-11**, D15 = 2008-12-13
  (Empire Sector), D16/D16.4 = 2009-02-09 (mechs, new drop-package items) and **D16.5 = 2009-02-17, the last deployment
  before shutdown**. That matches the client revision this stack targets (1.16.5.0) and fixes the shorthand: `pre_d11`
  means before the rebuilt tutorial, and **values dated 2008-08-15 … 2009-02-17 are the final-era values** (what the bulk
  research's tarapedia tooling flags as `post_d11`).
- **Final-era mission facts from those notes, needed by the "every mission" goal**: D14 **added** the repeatable
  "War Machine" in Raksha Robotics Factory and **removed "Artificial Iniquity"** (so it must not be seeded as live),
  and added the level-50 "A Mystery Unearthed" (Archaeologist Wynne Topper, Twin Pillars); D15 added "Welcome Home
  Soldiers!" and the repeatable "Rapture" (Captain Pauly Seminario / General Frank E. Murphy, Empire Sector, AFS
  Shocktrooper Suit rewards). The client's own mission table is the authority on which missions exist in the final build;
  the notes explain the history. The same notes publish a **resistance rework** (D14: diminishing returns, 10 → 16.67%
  … 250 → 83.33%; resistance skill 30 s → 60 s; Polarity Field -10/pump; resist modules 5 → 10 per rank) that the
  emulator does not implement yet — recorded as a combat-fidelity target. Research record:
  `research/20260915-aliadas-hub/work/official-deployment-notes.md`.
- Client login path, 2026-09-15: the realm's launcher (`banshee-realm-client`) expects the Tabula Rasa auth
  server on **2116** (`TabulaRasaLaunchPlan.DefaultAuthPort`, `docs/MARVEL_HEROES_TABULA_RASA.md`), while Rasa.NET's own
  default is **2106** (`src/Rasa.Auth/appsettings.json`). The compose file now publishes both to the same listener, and
  the owner's client got through: the launch command is `/NoPatch /AuthServer=tabularasa.bansheerealm.com:2116`, which
  resolves to the server's public address. `Rasa.Auth.Client.HandlePacket` now logs every received client opcode (the
  conversation is short and a stalling client looked identical to a silent one); the observed flow is
  `Login` → `ServerListExt` (`LoggedIn`) → `AboutToPlay` (`ServerList`) → the account is redirected to the queue of
  server 234 → the game accepts the client and creates the character's instance. The `SCCheck`/`SCCheckReq` pair stays
  unimplemented, but the real client does not send it on this path.
- Deployment note: the running game container carried an **ad-hoc navmesh mount** that `docker-compose.yml` never
  declared, so recreating the container from the file dropped it and the server fell back to straight-line creature
  movement. The compose file now declares `./navmesh:/app/navmesh` (the repository copy is byte-identical to the one that
  was mounted) and the log confirms `Loaded navmeshes for 76 of 78 maps`.
- Deployment note: recreating the compose network re-assigns container IPs, and both servers parsed
  `CommunicatorConfig.Address` with `IPAddress.Parse`, so the 2026-09-15 network recreation left the game dialling the
  auth container's old address. The configuration now carries the compose service name `auth`, and
  `Rasa.Networking.NetworkAddress` resolves it (literal IPs still parse first, so old configs stay valid);
  `NetworkAddressTests` covers both paths. The untracked `appsettings.env.json` was repointed from 192.168.16.2 to `auth`
  and the previous copy is in `/home/blizz/backups/rasa-net/20260915T005518Z-retail-class-gear-w2`.

## 2026-09-16 UTC — PvP control points: the client's table decoded, the wire format corrected

- The `controlpointdata` row is now a decoding, not a reading: `client/gameuiutil.pyo` `GetControlPointLabel` /
  `GetShortControlPointLabel` / `SortControlPointList` (lines 2228-2264) unpack it as
  `(typeId, nameId, mapTemplateId, level, sortOrder)`. `typeId` is `controlpointownershiptype`, `nameId` a `uielement`
  id, `mapTemplateId` a `maptemplate` id joined to a context through `gamecontext`. Twelve of the 17 rows are the two
  final-live battlegrounds (`adv_wargame_provinggroundsv002` 2361, `adv_wargame_edmundrange2` 2374: Whiskey, Charlie,
  Echo, Blue Base, Red Base, and Edmund Range's East and West Depots, level 50); five are test-map rows. Full record
  with hashes and line numbers: [pvp-control-point-client-evidence.md](pvp-control-point-client-evidence.md).
- **Defect corrected**: `ControlPointStatusPacket` (814) wrote one bare `ControlPointStatus` struct with no argument
  tuple; the client's `Recv_ControlPointStatus(statusList)` takes one list and iterates it. It now writes
  `(statusList,)`, and `ControlPointStatus` carries `ownerId` as the nullable long `shared/controlpointdefs.py`
  declares, with the four `kCPState_*` ids typed. `RequestControlPointStatus` (817), which had no handler, is
  answered with the channel's points - a faithful pair whose only client reader is the dead challenge-board window;
  the live battleground UI takes its points from `ScoreBoardGameScore`'s `cpData` and from CONTROL_POINT map markers
  `(ownerTypeId, ownerId)`, both part of the unbuilt lifecycle. `SetOwnerId` (884) exists as a packet. `UsePacket` writes the extra arguments
  `Usable.Recv_Use(*args)` accepts (declared before, never written). `ControlPointDataTests` (7) pass under net5.
- **Not reproduced, recorded as gaps**: the points' positions (the client maps of both battlegrounds carry no
  control-point entity), capture rules and war timings, the battleground team/scoreboard/win lifecycle (protocol
  recovered in the evidence doc's section 4, no server side), and the mech server side (`mechpad`, `MORPH_MECH` 457,
  pad states 216-219 recorded). `ControlPointManager` answers with every point unheld and `New`, spawns nothing on the
  battleground maps, and refuses an owner change for a point the map has not. The emulator's one Wilderness PvE
  control point (class 3814 at (197.66, 162.27, -54.08), status id 215) is emulator-authored - neither is in the
  client map or table - and is now labelled as such (`GAP-W3-PVE-CONTROL-POINT-PLACEMENT`) rather than removed.
- Deployed 2026-09-16 23:36 UTC. The candidate image ran the whole suite with no network and no database mounts:
  **905 of 907 pass**, the two failures being `EveryCampPlacementStandsOnTheWalkableSurface` and
  `EveryWorldPositionStandsWhereABodyCanWalk`, which need the `navmesh` folder the Dockerfile does not copy and fail
  identically in the pre-change image (both pass on the host tree with `rasaworld.db` present, where the suite is
  1012/1012). The image's `src` and `docs/evidence` hash identically to the reviewed workspace. Backups with
  `PRAGMA integrity_check` = ok are in `/home/blizz/backups/rasa-net/20260916T233502Z-retail-pvp-control-points/`,
  the previous image is kept as `rasa_net:before-retail-pvp-control-points-20260916`, and only `game` was recreated -
  `docker compose up -d --no-deps --no-build game` worked this time without the network workaround the earlier
  deploys needed, and `auth` was not touched (same container since 22:19 UTC, 0 restarts). The game reports
  `Server ready!`, `Loaded navmeshes for 76 of 78 maps`, `Loaded 16 content rules (239 content rows, 0 gaps)`,
  `Connected to the Auth Server!`, 0 restarts and no error or exception lines.
- One manifest correction found by the deploy's own gate: the wire-format fix was first written into the manifest's
  `changes` array, which the schema reserves for content-row changes with a migration and object citations. A packet
  shape is not a content row, so the entry is removed; the corrected gap entry and this log carry the record.
  `RealManifestsAndCompanionFilesPass` and `RealObservedCitationsResolveAgainstTheRealFootageEvents` pass again.
- Also learned: the challenge-board window (clan bidding on control points) is dead code in the final client, and
  `battlegroundrulestype` names a single ruleset, `EDMUND_RANGE`. A survey of the owner's Alienware found the same
  1.16.5.0 client twice, toolkit map renders of both battlegrounds, and no battleground or mech footage.

## 2026-09-16 UTC — Two live findings: the auth handoff after a redeploy, and mission 1992

- **Players hang at "authenticating" after the game container is recreated.** Auth keeps a stale game-server
  registration for the replaced container - it never logs a disconnect for it, and the new game server's
  registration does not take - so authenticated clients have nowhere to be handed to. The game's own log is
  misleading here: `Connected to the Auth Server!` is only the TCP connect. The fix is to restart auth after the
  game redeploy and confirm both sides: auth must log
  `The Game server (Id: ..., Address: ..., Public Address: ...) has authenticated! Requesting info...` and the game
  `Successfully authenticated with the Auth server!`. The game retries every ~10 s, so restarting auth is enough.
  Observed today: game recreated 23:36, auth's last registration 22:19, clients connecting and dropping in under
  70 ms until auth was restarted at 23:42, after which registration succeeded in 5 s.
- **Mission 1992 "Gearing Up for Battle" cannot progress** (`GAP-S2-GEAR-OBJECTIVES`, reported from live play). The
  placements work: the supply crate dispenses its item set and the two dummies stand correctly. But no content rule
  targets the mission's gear objectives, so a player takes the gear, equips it, and the log never moves. Closing it
  needs three `ContentRuleEvent` kinds the engine does not have - a usable-used/looted event, an item-equipped
  event, and a placement-damaged event (`PlacementDestroyed` is not it: the dummy restores after 930 ms instead of
  dying). This is an unimplemented slice, not a regression from the control-point deploy.
  *Superseded 2026-09-17: this diagnosis was wrong - the bindings and their runtime paths existed; the crate's loot
  window was empty. See the next entry.*
- **The endgame-zone wall is structural, now with numbers.** No client map places a single creature spawner or NPC -
  zero entities of any class carrying augmentation 61, 68 or 52 across all fifteen level-banded adventure zones,
  the Wilderness included - and the world seed's 218 spawn pools are all in context 1220. Creature placement was
  entirely server-side and none of it survives, so the zones above the Wilderness need the reconstruction rules
  (OD-45, OD-48) applied at scale rather than any new mechanic.
- **Mechs: the pads were never shipped content.** Only two entity classes carry the MechPad augmentation (83),
  30421 `TEST_KGS_Mechpad` and 30464 `UsableOwnableMechStation`, and the shipped English strings name **both** of
  them "Testing Mechpad"; both have an all-null `usabledata` row, and no client map places either. What did ship
  for players is the Mech PAU line - 26892 `PAU_Vehicle_AFS_MECH` ("Mech PAU"), `Weapon_PAU_AFS_Mech_MiniGun_Physical`
  and `_Laser`, `Ability_PAU_AFS_Mech_Sprint`, and a complete `Shield_Vehicle_Mech_{Light,Medium,Heavy}_{30..50}`
  ladder - alongside the mech NPC/vehicle classes (`Vehicle_AFS_Mech`, `NPC_Vehicle_AFS_Mech`, the Hominis Machina
  family). So the D16 note that mechs were "usable only on Edmund and only from mech pads" is not reflected in the
  client's pad data, and the mech work should start from the PAU vehicle and its shield ladder rather than from the
  pad augmentation (`GAP-W3-MECH-SERVER-SIDE`).

## 2026-09-17 UTC — Mission 1992 re-diagnosed: the supply crate's window was empty, not ruleless

- **The 2026-09-16 diagnosis of `GAP-S2-GEAR-OBJECTIVES` was wrong.** The bindings for 1992/1 (loot_all on crate
  198651), 1992/2 (equip, any item), 1992/3 and 1992/8 (hit on dummies 198652/198653, the second with action 194)
  are in `npc_mission_objective_binding`, were seeded by `BootcampS2GearingUp`, load with 0 gaps, and their runtime
  paths were already tested. The crate is gated by `content_condition` 198800 (ObjectiveStateIs 1992/1 Incomplete),
  so it cannot be looted before the objective is revealed. No new `ContentRuleEvent` kinds were needed.
- **What the character database says.** Character 5 on 2026-09-17: mission 1992 objective 4 status 2, objective 1
  status 1. Its inventory is items 23-27 only - 145 in the ability drawer, 13126/13186/13156 equipped, 28 x65 -
  all created 2026-09-15 17:01:10, i.e. the creation kit, and nothing from item set 19858 (13066, 13096, 13156,
  13186, 13713). The "gear" the player equipped was the starter armour; the crate handed over nothing.
- **The actual defect, and why the crate window showed nothing.** `OpenContentContainer` built the window's rows
  as `new LootItem(templateId, 0, quantity, owner, 0)`: a fresh entity id with no item behind it, and no
  `ItemInfo` sent. The client's `corpselootwindow` resolves every row with `GetEntity(itemId)` before drawing it and
  skips one that comes back `None` (research `client-code/verify/dis/trpython-client-ui-corpselootwindow.pyo.dis`
  lines 316, 373, 596, 663), so the window listed nothing to take. The corpse path had done this right since
  06b3352 (2026-09-13, `RequestCorpseLooting` sends the items before the window); the content path of 6e5f5b0
  (same day, later) did not. And the window's right-click path - `RequestLootItemFromCorpse(entityId, itemId,
  destSlot)`, one row at a time - was routed only to `LootDispenserManager`, never to content containers.
- **The fix.** A content container now holds real items, created once per owner from its item set and introduced
  to the client with `SendItemDataToClient` before `LootInfo`/`CanLootItems`, as the corpse path does.
  `RequestLootItemFromCorpse` on an entity in `ContentUsables` routes to
  `RequestLootItemFromContentContainer`; a row that is gone or was never there is answered with `TakenInfo`, since
  it is still on the asker's screen; a row that does not fit shows `PmInventoryFull`. Loot All takes every row that
  still fits, like the corpse dispenser, instead of the earlier all-or-nothing transaction. The loot_all binding
  completes when the container is empty by either path. Tests: `ContentContainerRowsAreRealItemsIntroducedBeforeTheWindowOpens`
  (every row is a registered item and its `ItemInfo` precedes `LootInfo`),
  `TakingContainerItemsOneAtATimeCompletesTheObjectiveOnTheLastOne`, `TakingAnUnknownOrAlreadyTakenContainerRowChangesNothing`,
  `LootAllTakesWhatIsLeftAfterSingleTakes`; `LootAllIsRefusedWhenTheInventoryCannotTakeEverything` became
  `LootAllTakesNothingWhenNothingFits`. Under .NET 5 (sdk:5.0 container, `--no-incremental`): 909 of 911, the two
  failures the navmesh/`rasaworld.db` audits that need files the scratch tree does not carry.
- **Not established:** whether the original completed the objective when the crate was emptied one row at a time.
  The footage shows only Loot All (A3-017 to A3-024); the one-at-a-time completion is the emulator's choice, and
  the manifest entry says so.

## 2026-09-18 UTC — Deploy: the crate window (commit 31a5a88)

- Candidate `rasa_net:candidate-crate-window-20260918`; the suite ran **918 of 918, nothing skipped**, with the
  world database and the navmesh mounted. Previous image kept as `rasa_net:before-crate-window-20260918`.
- Integrity-checked backups in `/home/blizz/backups/rasa-net/20260918T060857Z-crate-window/`. No database change:
  this deploy is code only.
- Deploy order as before: recreate game (06:09) → restart auth (06:09:37) → restart game. Both handshake lines
  postdate the auth restart at 06:10:00. Nobody was online. After it: `Loaded 16 content rules (249 content rows,
  0 gaps)`, no error lines, only mission 321 unoffered.
- A caveat for the next crate open: the five items the failed 05:43 open created (item ids 61-65) are orphaned in
  `items` with no `character_inventory` row, and the container is per session, so the next open builds five new
  ones. They are harmless, and `PlayerHolds` means the two templates the player already wears from their creation
  loadout will not need taking again.

## 2026-09-17 UTC — Deploy: twelve NPCs given their dialogue (commit 1a83292)

- Candidate `rasa_net:candidate-dialogue-binding-20260917`; the suite ran **918 of 918, nothing skipped**, with
  the world database and the navmesh mounted. Previous image kept as `rasa_net:before-dialogue-binding-20260917`.
- Integrity-checked backups in `/home/blizz/backups/rasa-net/20260917T231510Z-dialogue-binding/`.
- `WildernessDialogueBinding` applied by `dotnet ef database update` against a copy and swapped in with the game
  stopped; afterwards the live world carries all twelve `npc_package` rows and `PRAGMA integrity_check` is ok.
- Deploy order as before: stop game → swap the world → recreate game → restart auth (23:15:44) → restart game.
  Both handshake lines postdate the auth restart at 23:16:07. Nobody was online. After it: `Loaded 16 content
  rules (249 content rows, 0 gaps)`, no error lines, only mission 321 unoffered.
- **Fifteen objectives over nine missions stop being dead ends**: 421/3, 427/1, 431/1, 431/2, 431/3, 442/1,
  444/1, 451/1, 451/2, 549/1, 682/2, 682/4, 682/5, 682/6 and 698/1. The audit's recorded set is down from 34 to
  19.

## 2026-09-17 UTC — Deploy: the respawn the player never saw, and the floor (commit 88be25f)

- Candidate `rasa_net:candidate-respawn-floor-20260917`; the suite ran **918 of 918, nothing skipped**, with the
  world database and the navmesh mounted. Previous image kept as `rasa_net:before-respawn-floor-20260917`.
- Integrity-checked backups in `/home/blizz/backups/rasa-net/20260917T223912Z-respawn-floor/`.
- The world database was migrated by `dotnet ef database update` against a copy (`WorldPlacementFloorSnap`) and
  swapped in with the game stopped; placement 198674 reads 110.23 and 199603 reads 223.48 afterwards, as the
  migration's own rows say. `PRAGMA integrity_check` ok.
- **Mission progress was cleared at the owner's request** ("can you restart the quests?"): all of
  `character_mission`, `character_mission_objective` and `character_mission_objective_counter`, which held six
  mission rows and ten objective rows for the three live characters and three deleted ones. Initiation is a forced
  radio offer on entering the camp, so the chain starts over by itself.
- Deploy order as before: stop game → swap the databases → recreate game (22:40:04) → restart auth (22:40:16) →
  restart game. Both handshake lines postdate the auth restart — auth *"has authenticated! Requesting info..."* and
  the game *"Successfully authenticated with the Auth server!"* at 22:40:38 — with the expected failed attempt at
  22:40:09 while auth was mid-restart. Nobody was online. After it: `Loaded 16 content rules (249 content rows, 0
  gaps)`, 76 of 78 navmeshes, no error lines, and only mission 321 unoffered.

## 2026-09-17 UTC — Deploy: hospital coverage on 41 maps, and mission 429 offered (commit fca47bb)

- Candidate `rasa_net:candidate-hospital-coverage-20260917` ran the whole suite with the world database and the
  navmesh mounted: **918 of 918, nothing skipped** — the first run in which both `MissionLinkAuditTests` checks
  actually execute (they look for `rasaworld.db` beside the `navmesh` folder, which no test container carried, so
  they had been reported *inconclusive* every time). The previous image is kept as
  `rasa_net:before-hospital-coverage-20260917`.
- Integrity-checked backups (`PRAGMA integrity_check` = ok for all three) in
  `/home/blizz/backups/rasa-net/20260917T190310Z-hospital-coverage/`.
- **The world database needed a repair, not just a migration.** `WildernessPinholeNpc` was deployed on its first
  build, before the objective transition row was added to it, so the live database carried the migration as applied
  and mission 429 still logged *"not offered, definition incomplete: required objective 4 is never revealed"*. The
  migration was reverted and re-applied with `dotnet ef database update` against a copy, and the copy's full
  `.dump` differs from the live one by exactly one line — `INSERT INTO npc_mission_objective_transition
  VALUES(429,5,4)` — which is what was swapped in.
- Deploy order as the 2026-09-16 entry requires: stop game → swap the database → recreate game (19:50:19) →
  restart auth (19:50:52) → restart game (19:51:06). Both handshake lines postdate the auth restart — auth *"has
  authenticated! Requesting info..."* 19:51:14.991 and the game *"Successfully authenticated with the Auth
  server!"* 19:51:15.000 — and the game's first attempt at 19:50:45, while auth was mid-restart, failed exactly as
  that entry predicts. Nobody was online.
- After the deploy: `Loaded 16 content rules (249 content rows, 0 gaps)`, `Loaded navmeshes for 76 of 78 maps`,
  373 region volumes, 141 map links, no error lines. **The "Mission 429 is not offered" line is gone**; only 321
  remains, which has no giver in the client tables at all.

## 2026-09-17 UTC — Deploy: the supply crate fix (commit a117311)

- Candidate `rasa_net:candidate-retail-crate-loot-20260917` (sha256 f134d7e3…) ran the whole suite with no network and no
  database mounts: 909 of 911, the two failures the navmesh/`rasaworld.db` audits that need files the Dockerfile does
  not copy; with the database present those two pass (21 s, in the sdk:5.0 iteration container). The image's `src`,
  `docs/evidence` and solution file are byte-identical to the reviewed workspace.
- Integrity-checked backups (`PRAGMA integrity_check` = ok for all three) in `/home/blizz/backups/rasa-net/20260917T010527Z-retail-crate-loot/`; the previous image is kept as
  `rasa_net:before-retail-crate-loot-20260917` (0132e6f6). Compose `--dry-run` named only `game`, so plain
  `docker compose up -d --no-deps --no-build game` recreated it (01:08:00 UTC); nobody was online (last client left
  00:46:34).
- **The auth hand-off needs one more step than the 2026-09-16 entry says.** After the recreate, auth was restarted at
  01:08:44 as prescribed - but the game did *not* re-register: it had logged `Could not connect to the Auth server!
  Trying again in a few seconds...` at 01:08:43 (auth was mid-restart) and then nothing for four minutes, and auth
  showed no new game-server connection. Restarting `game` at 01:13:21 fixed it in 19 s: auth
  `has authenticated! Requesting info...` and the game `Successfully authenticated with the Auth server!` both at
  01:13:40, `Server ready!` 01:13:42, `Loaded navmeshes for 76 of 78 maps`, `Loaded 16 content rules (239 content
  rows, 0 gaps)`, 0 restarts, no further error lines. So the order is: recreate game → restart auth → restart game →
  confirm both log lines. Checking only the game's first `Successfully authenticated` (which predates the auth
  restart) is not enough.

## 2026-09-22 — Recovered-client class and Caufield handoff (W2)

- An isolated recovered 1.16.5.0 client run with a diagnostically levelled Training Day-complete character exercised
  Kincaid's Soldier tier choice. The compatible client showed the permanent-class confirmation, raised the character to
  level 5 with 3 attribute and 4 skill points, and immediately opened the Headquarters class-gear mission 2010 offer.
  Decline was disabled; accepting it put “Report to Quartermaster Caufield” in the tracker.
- At the seeded supply tent, Caufield rendered with the completion indicator and accepted interaction despite his plain
  Redshirt body. His mission topic completed the objective, and a second interaction offered the five Reflective armor
  pieces and Rage-O-Matic. The completion persisted six new item templates 122859/122860/122862/122863/122864/122865
  and mission state 4; XP stayed 43,000 and credits stayed 470. The backpack showed the helmet red until Reflective Armor
  1: Novice is trained.
- Caufield's generic conversation rendered `ERROR: ?: No greeting`. The authentic greeting id/text is not recovered;
  `GAP-W2-CAUFIELD-GREETING` remains open. This run checks emulator behavior through the compatible client, not final-live
  server behavior. A relog kept 2010 complete and its six items, and Caufield offered the separate "Lurking In The
  Shadows" mission rather than class gear. Specialist 2011 and class skill equip remain to be checked. Exact diagnostic
  inputs, screenshots, database snapshots, hashes and limitations: `docs/evidence/client-class-gear-handoff.json`.

## 2026-09-23 — Recovered-client Specialist class gear and W3 entry

- Isolated compatible-client runs 55-57 completed the Specialist path: Kincaid's tier choice opened forced mission 2011,
  Caufield completed the report objective and paid the five Hazmat armor pieces plus Repair-O-Matic. The saved character
  had class 3 at level 5, mission 2011 complete, reward templates 122866-122871, 43,000 XP and 470 credits. The
  diagnostic level/XP and travel position were set directly, so normal earned progression was not verified by this run.
- Training Hazmat Armor 1 and Tools 1 changed the class rewards from red to usable. All five armor pieces and the tool
  equipped and persisted after relog. Repair-O-Matic displayed `0/0` ammo and triggered Out Of Ammo; the emulator's
  client-derived weapon row specifies ammo class 3807 and a 10-round clip, but the original final-live ammo grant is
  unknown (`GAP-W2-REPAIR-TOOL-AMMO`). A [2007 firsthand Specialist account](https://www.engadget.com/2007-11-26-adventures-from-the-back-row-the-specialist-and-her-tools.html)
  reports that repair tools consume Power Cells when repairing damaged armor and require Tools rank I; it does not say
  whether the final-live class mission supplied cells. Caufield still showed the missing-greeting error before mission
  topic selection.
- After relog, Caufield offered "Lurking In The Shadows" instead of class gear; accepting it saved mission 427 active
  with "Speak to Oliver" in the tracker. This begins a W3 client check. Exact screenshots, DB snapshots, hashes and
  limitations: `docs/evidence/client-specialist-class-gear.json`.
- Isolated recovered-client run 58 reached Oliver by diagnostic position edit. His objective marker, talk prompt and
  mission 427 Objective Completion dialogue appeared. Continue advanced the tracker to "Kill Proctor Fulgor" and saved
  objective 1 complete/objective 6 active (`docs/evidence/client-wilderness-oliver-first-objective.json`). The boss,
  return and reward sequence remains to be verified.
- A .NET 5 seeded scenario test drove creature 76 through the actual mission kill handler after objective 1 was complete.
  It persisted objective 6 complete, revealed objective 7 "Return to Caufield", kept mission 427 active and paid no
  early reward. Repeating the kill notification did not repeat the objective packets. This is server transition coverage,
  not recovered-client boss combat or final-live fidelity proof (`src/Rasa.Test/BootcampOpeningTests.WildernessFulgor.cs`).
- Client run 63 placed the diagnostic level-5 Specialist near Fulgor: the boss spawned at the world-seed position and
  killed the character, leaving objective 6 active. A navmesh probe found two local walkable heights; the original
  seed's Y is near the lower layer and a [fan-site `/loc`](https://www.ellatha.com/tr/bossnpcview.asp?key=Proctor+Fulgor)
  is near the upper one. Neither the exact final-live platform nor boss level is proven. Historical mission pages also
  put Forming Alliances before 427 and describe a carried ammo shipment/death failure. The 479 → 427 gate is now deployed,
  but the shipment rule remains unimplemented. These are open fidelity gaps, with source revisions, exact
  coordinates, run artifacts and confidence in `docs/evidence/wilderness-lurking-chain-audit.json`.

## 2026-09-24 — Live admission and deletion cleanup

- The live Game now runs `rasa_net_game:dit-preservation-20260924c` with the required DIT overlay. Startup authenticated
  with Auth, enabled `AllNewCharacters` boot camp admission, and loaded 414 content rows with zero gaps. `/app/dit` is
  mounted, its status is enabled, and both live SQLite databases pass integrity checks. The stopped-server backup,
  exact image ids, logs, source hashes and checks are in `docs/evidence/live-deletion-rollout-20260924c.json`.
- A disposable-client world-entry smoke before admission changed exposed a deletion defect: removing a character left
  ability, inventory, skill, teleporter and item rows. Transactional deletion now removes character-owned state and
  exclusively held items. The .NET 5 image passed all 86 `NewCharacterTests`, including normal cleanup and forced
  rollback. This is emulator consistency verification; original final-live deletion behavior is not proven.
  Details: `docs/character-deletion-cleanup-20260924.md`.
- A fresh recovered-client character entered a playable Luna Cavern / Bootcamp HUD on its first login and received the
  Initiation offer. It was then deleted through Character Select; all 14 character-id child tables and its five item
  instances were empty, while the owner's three characters remained. Exact images, client/server logs, database audit
  and 55 verified hashes are in `docs/evidence/live-bootcamp-smoke-preservation-20260924c.json`. The recovered client
  code accepts forced radio offers when their window closes; this explains the `AssignRadioMission` sent during the
  X/Escape input sequence. The exact input that closed the window is unproved, with client-code evidence in
  `docs/evidence/live-initiation-offer-dismissal-20260924.json`.
- Two bounded S5 client runs used the corpse normally and started the ten-minute bomb timer, but neither reached the
  bomb before expiry. One neared the wreck and lost movement to chat focus; the repeat caught bridge rubble. A sampled
  dry route, hazards, observed footage endpoint, and remaining uncertainty are in
  `docs/evidence/bootcamp-s5-dry-route-analysis-20260924.md`. An uninterrupted S5 completion was unverified after these runs.
- A client-harness guard now blocks held movement while chat or an unknown UI state has focus; two untimed client probes
  confirmed recovery. One subsequent unreset S5 timed run avoided the focus fault but followed the bridge west of the
  wreck and expired without a bomb use. This isolates route steering as the next client verification problem; it does
  not establish that the 600-second mission is impossible. Evidence, 17 screenshot hashes, command ledger and restored
  copied-DB checksum: `docs/evidence/bootcamp-s5-focus-guard-and-timed-repeat-20260924.md`.
- A coordinate-guided `/loc` client route later reached the inferred bomb viewpoint from the corpse checkpoint without
  a timer, and a separate copied diagnostic state produced the use prompt and completed the bomb objective. Three
  unreset timed attempts then exposed harness and steering failures before bomb use; the farthest stopped 8.7 m short.
  The focus, dry-bridge and resume controls now pass five focused tests. The sealed route, diagnostic limits and 52
  verified source hashes are in `docs/evidence/bootcamp-s5-loc-waypoint-route-20260924.json`. At that stage, a
  continuous timed corpse-to-bomb completion was still unverified.
- An audit of the original S5-to-S6 footage confirms a level-3-to-4 jump inside an edit. The XP amount and trigger are
  unobserved, and the visible XP bar cannot distinguish mission reward from intervening combat. No hidden grant is
  seeded; the remaining progression risk is documented in `docs/evidence/bootcamp-s5-hidden-xp-audit-20260924.json`.
  The [tier-gate follow-up](evidence/s5-s6-tier-gate-xp-followup-20260924.json)
  records the full bar and trainer-gate message at Alia Das. The prior rough
  10–11k scenario reaches only emulator level 4, not the tier gate; no reward
  amount or trigger can be assigned from the edited footage. A
  [source-exclusion audit](evidence/s5-s6-xp-source-exclusion-20260924.json)
  shows that the XP predates ordinary 1995 and 1526 turn-ins; it does not
  identify the missing event.
- V5 followed the dry timed route to the wreck hillside with 3:51 remaining, then stopped on a `/loc` OCR failure; a
  bounded OCR fallback now reads that frame. V6 started normally but the outer isolated runner received an unexplained
  SIGTERM shortly after corpse use. Neither run reached bomb use or observed timer expiry, so uninterrupted S5
  completion remains unverified. The [V5](evidence/bootcamp-s5-timed-loc-v5-20260924.json) and
  [V6](evidence/bootcamp-s5-timed-loc-v6-20260924.json) records include 52 and 78 verified source hashes respectively.
- A detached-runner check kept the isolated client responsive. Timed V7 reached the wreck hull with 1:12 left, but
  failed to expose the bomb prompt and the 600 s objective timer expired during local movement probes. An untimed
  recovery from the exact V7 position then reached the prompt at `(-219.08594,101.05078,-69.39844)`. Timed V8
  stopped early while the objective was active after one long diagonal hold fell below the bridge deck. A separate
  untimed probe crossed from V8's exact safe position in five short holds at the deck height. These findings narrow
  the client route without verifying a continuous timed completion or indicating a mission-mechanics defect. See
  [V7](evidence/bootcamp-s5-timed-loc-v7-20260924.json),
  [hull recovery](evidence/bootcamp-s5-hull-untimed-recovery-20260924.json),
  [V8](evidence/bootcamp-s5-timed-loc-v8-20260924.json), and
  [bridge recovery](evidence/bootcamp-s5-bridge-untimed-recovery-20260924.json).
- Timed V9 stayed on the dry bridge deck and stopped with objective 1 active
  when a fixed point tolerance rejected safe forward progress. Its
  [trace](evidence/bootcamp-s5-timed-loc-v9-20260924.json) shows a controller
  false divergence, with no bomb use or timer expiry.
- Timed V10 approached the bridge from a different safe position; a generic
  diagonal step descended below the deck-height guard, so the controller
  stopped with objective 1 active. Its [trace](evidence/bootcamp-s5-timed-loc-v10-20260924.json)
  records no bomb use or timer expiry. An [untimed local probe](evidence/bootcamp-s5-bridge-untimed-recovery-v2-20260924.json)
  crossed from V10's exact safe approach to the far deck in five short holds,
  with every `/loc` reading at Y=84.8. A timed run using this position-dependent
  crossing is still unverified.
- Timed V11 reached the far bridge edge with 5:54 on the client timer, then an
  extra W400 dropped below the deck while objective 1 remained active. A local
  [edge probe](evidence/bootcamp-s5-bridge-postwa-edge-20260924.json) showed
  that position cannot safely continue. From the prior dry point, a separate
  [short-step probe](evidence/bootcamp-s5-bridge-prewa-short-probe-20260924.json)
  crossed with two W+A200 holds at Y=84.8. The [V11 trace](evidence/bootcamp-s5-timed-loc-v11-20260924.json)
  and probes remain route-control evidence, not a timed bomb completion.
- Timed V12 crossed the bridge and reached the wreck viewpoint with 1:30
  remaining. The original client visibly showed the bomb-use prompt, but the
  harness OCR crop missed its left edge and withheld the click. The objective
  remained active; [V12](evidence/bootcamp-s5-timed-loc-v12-20260924.json)
  verifies timed route reachability to the prompt, not bomb use or completion.
- Timed V13 used the corrected prompt detector, but three manual `/loc` reviews
  consumed the remaining margin. The original client showed Objective Failed
  near the wreck, the copied database recorded objective 1 failed, and the
  game log had no bomb-use request. [V13](evidence/bootcamp-s5-timed-loc-v13-20260924.json)
  establishes real timer expiry during a slow harness route; it does not show
  a mission or bomb defect.
- Timed V14 started the 600 s objective by ordinary corpse use, crossed the
  dry bridge, and right-clicked the bomb with 0:22 on the original client
  timer. Objective 1 completed before expiry. The same client session showed
  Van Valkenberg's dialogue, completed all four 1995 objectives and transferred
  to Alia Das with Calling for Reinforcements complete and Training Day offered.
  Two logged use requests and the copied database corroborate the captures.
  [V14](evidence/bootcamp-s5-timed-loc-v14-20260924.json) verifies this
  reconstructed timed route, while original encounter combat, exact fin

## 2026-09-25 — supplied footage ledger

Every timestamp below is from `docs/evidence/gameplay-footage-playlists-20260924.json` or `docs/evidence/footage-fanout-20260925/`. Missing evidence is not 100% retail accuracy. Public-test, beta, and E3 2006 rows are boundaries and are not final-live rules. Upload dates are not recording builds.

- cite:A4udsM0rcLo@30 gap: Three squad status panels and several armed characters fighting near a sandbag line. This observation was not implemented as a server change.
- cite:A4udsM0rcLo@90 gap: Player fires during an outdoor fight; fallen enemies and a loot glow are visible. This observation was not implemented as a server change.
- cite:A4udsM0rcLo@180 gap: Friendly nameplates cluster at a structure entrance while the player fights. This observation was not implemented as a server change.
- cite:A4udsM0rcLo@300 gap: Player fights behind two visible allies inside a passage. This observation was not implemented as a server change.
- cite:A4udsM0rcLo@420 gap: Several friendly characters engage a target labelled Prototype Forean Machina in the Production Chamber. This observation was not implemented as a server change.
- cite:-I1WM5ddg-k@600 gap: Player pauses travel at the barracks interface. This observation was not implemented as a server change.
- cite:Of3Uxw9MNec@60 gap: Player advances toward a control point amid combat. This observation was not implemented as a server change.
- cite:Ik3CErtdVHU@30 gap: Characters gather around a boxing event. This observation was not implemented as a server change.
- cite:EiE2oodlP8A@20 boundary: public-test. not a final-live rule. gap: Rows of recruits perform exercise motions before a large door. The uploader claim says this is the D11 rebuilt boot camp on the Public Test Server, and the observation is unverified as PTS only, so the final live count, layout, and schedule stay unknown.
- cite:kmL7t3rnzpA@9 gap: Several players idle and use emotes together. This observation was not implemented as a server change.
- cite:-I1WM5ddg-k@1488 reconciled: seeded mission 1390 Conscientious Objector completes at Warrior Apirka for 2,000 XP. gap: the two selectable item templates are unidentified, and the recording date is unknown.
- cite:-I1WM5ddg-k@1492 reconciled: the on-screen +2,000 XP matches the seeded 1390 reward. gap: the item templates and the recording date remain unknown.
- cite:-I1WM5ddg-k@1495 reconciled: seeded missions 1392/1393 are Conscientious Objector - Part Two from Apirka toward Rogers. gap: the footage does not show which id is on screen, and the recording date is unknown.
- cite:-I1WM5ddg-k@1510 reconciled: accepting that offer is the seeded Apirka handoff. gap: the clip does not show Rogers or a reward, and the recording date is unknown.
- cite:A4udsM0rcLo@30 gap: Wilderness Targets of Opportunity tracker shows Kill 40 Miasmas, Kill 40 Xanx and Kill 30 Shield Drones. This observation was not implemented as a server change.
- cite:A4udsM0rcLo@300 gap: The location label reads Pravus Interior Halls; Thrax Technicians are visible. This observation was not implemented as a server change.
- cite:A4udsM0rcLo@420 gap: The location label reads Pravus Production Chamber; target label reads Prototype Forean Machina. This observation was not implemented as a server change.
- cite:x_ujwmKj8jA@1 gap: Letterboxed cinematic still of a figure at a glowing circular interface facing a machine in a forest. The same still is held at 82s. No HUD, cursor, or camera motion. This observation was not implemented as a server change.
- cite:x_ujwmKj8jA@82 gap: Middle spectrogram is continuous harmonic music with a steady pulse. Silence appears only as about 1.5s at the end. No speech or combat transients in that window. This observation was not implemented as a server change.
- cite:jhw3kEPHZv8@1 gap: Held letterboxed still of a robed figure with a purple-flamed staff beside sandbags, a second armored figure, pines, and water. A small corner mark is present. No HUD. This observation was not implemented as a server change.
- cite:jhw3kEPHZv8@76 gap: Spectrogram is sustained harmonic music. Description says only 'Valverde Plateau track.' This observation was not implemented as a server change.
- cite:BMUI6Akuv9E@1 gap: Static winged Tabula Rasa logo on a plain gray field, held across the sampled stills. No gameplay image. This observation was not implemented as a server change.
- cite:BMUI6Akuv9E@103 gap: Spectrogram is rhythmic harmonic music. Description calls it one Foreas track variation. Audio continues until a silent tail. This observation was not implemented as a server change.
- cite:iNtYRWv_Fs0@1 gap: Held promotional wallpaper: a rune-lit tree, a machine, and a calendar grid. The top-left text reads tabularasavault.ign.com. No game HUD. This observation was not implemented as a server change.
- cite:iNtYRWv_Fs0@98 gap: After the quiet open, the middle spectrogram is sustained harmonic music. Description calls this the Final variation of the Foreas track. This observation was not implemented as a server change.
- cite:2_knJpz5QbU@1 gap: Held product photograph of a Richard Garriott's Tabula Rasa collector-style box, discs, a field guide, and a letter. The login or main-menu UI is not on screen. This observation was not implemented as a server change.
- cite:2_knJpz5QbU@43 gap: Description says 'Track from the TR login screen.' The middle spectrogram is continuous harmonic music, consistent with a music rip rather than UI clicks. This observation was not implemented as a server change.
- cite:wg4qYJpOJ-M@105 gap: Slideshow of held images, not a moving camera. Sampled cards include an Arieki concept painting (1s), a waterfall settlement (45s and 80s), a first-person still with a weapon and t This observation was not implemented as a server change.
- cite:wg4qYJpOJ-M@120 gap: Spectrogram is continuous harmonic music. Description offers megaupload.com/?d=ZTPRWLEV as a 293MB all-soundtracks pack. This observation was not implemented as a server change.
- cite:aOtqXKj69Yc@1 gap: Held promo render of a goggled woman with a pistol and the winged logo. No HUD or environment. This observation was not implemented as a server change.
- cite:aOtqXKj69Yc@69 gap: Spectrogram is sustained harmonic music. Description is 'Tabula Rasa - AFS Outpost - Soundtrack.' No PTS, beta, or E3 wording. This observation was not implemented as a server change.
- cite:RGWKETi_1bw@1 gap: Held winged logo card. A 2007 NCSoft copyright line is visible in the corner. No base geometry and no HUD. This observation was not implemented as a server change.
- cite:RGWKETi_1bw@66 gap: Spectrogram is harmonic music. Description is 'Tabula Rasa - AFS Base - Soundtrack.' This observation was not implemented as a server change.
- cite:H2vuIiIamU8@1 gap: One held promo render of three armed human figures and the winged logo for the whole file. No Forean character, no HUD. This observation was not implemented as a server change.
- cite:H2vuIiIamU8@106 gap: Spectrogram is continuous harmonic music. Description is 'Tabula Rasa - Forean - Soundtrack.' This observation was not implemented as a server change.
- cite:Jvug1Bn2YYE@1 gap: Held promo render of a tall armored non-human figure with a bladed staff, the winged logo, and www.RGTR.com. Corner copyright reads 2007 NCSoft. No camp and no HUD. This observation was not implemented as a server change.
- cite:Jvug1Bn2YYE@70 gap: Spectrogram is sustained harmonic music. Description is 'Tabula Rasa - Forean Encampment - Soundtrack.' This observation was not implemented as a server change.
- cite:N6NMlvij7SQ@1 gap: Held logo over a split landscape painting, green waterfall on one side and lava on the other. No creatures, HUD, or play. This observation was not implemented as a server change.
- cite:N6NMlvij7SQ@83 gap: Spectrogram is dense sustained harmonic music. Description is 'Tabula Rasa - Cormans - Soundtrack.' This observation was not implemented as a server change.
- cite:Z9fn31_ht_I@24 gap: Short card sequence: a Klintonix logo, a logo card reading Do you miss "Tabula Rasa" soundtrack?, a card telling the viewer to use the description link to download all soundtracks  This observation was not implemented as a server change.
- cite:Z9fn31_ht_I@18 gap: A music bed plays under the cards. The description link is https://www.box.com/s/n939ibijs9jeet9h3vns. That archive was not downloaded. This observation was not implemented as a server change.
- cite:NtogPR3B9rI@0 boundary: e3-2006. not a final-live rule. gap: The clip is a photo of a monitor in a room. A third-person humanoid in red-orange armor is on a rocky forest path, seen from behind and slightly above, with a circular minimap at t
- cite:NtogPR3B9rI@12 boundary: e3-2006. not a final-live rule. gap: The same armored figure holds a long rifle across the body while a bright muzzle-side flash is in front of the weapon. A red bar sits over a small figure farther up the slope. The 
- cite:NtogPR3B9rI@20 boundary: e3-2006. not a final-live rule. gap: Between the 8s, 20s, and 32s samples the rifle figure remains in the lower center while the trail, rocks, and trees scroll, which is a following camera during travel rather than a 
- cite:jNxEdYl6fr4@0 boundary: e3-2006. not a final-live rule. gap: Photographed monitor. Third-person view behind an orange-armored humanoid on open orange-brown ground with dead trees. Bottom unit frames and a round minimap are present.
- cite:jNxEdYl6fr4@12 boundary: e3-2006. not a final-live rule. gap: Two large translucent green domes sit in the midground. Smaller humanoid silhouettes stand between the camera character and the domes. A pale arc is in the sky. The camera characte
- cite:jNxEdYl6fr4@16 boundary: e3-2006. not a final-live rule. gap: The camera character's arms are raised in front of the body while one green dome remains to the right and a small figure is nearer the center. Red bars are over at least one distan
- cite:3reasGu9M9c@0 boundary: e3-2006. not a final-live rule. gap: A Dell monitor bezel is in frame. The game view is a dark interior of repeating hexagonal wall pods, several with green lit panels and a few with orange fire. A red-capped figure i
- cite:3reasGu9M9c@16 boundary: e3-2006. not a final-live rule. gap: The view has moved into a darker corridor. The same red-capped figure remains low in frame, so the camera is still following rather than cutting to a fixed shot.
- cite:3reasGu9M9c@28 boundary: e3-2006. not a final-live rule. gap: The character is in a chamber packed with angular green-glowing shapes. Combat is presented as dense colored light in a tight interior, not as an outdoor shootout.
- cite:2mOneHr2pJA@0 boundary: e3-2006. not a final-live rule. gap: Show-floor monitor with a neighboring screen at the right edge. Third-person view in a dark organic forest. A figure is low in the frame and taller pale figures stand ahead on the 
- cite:2mOneHr2pJA@12 boundary: e3-2006. not a final-live rule. gap: The camera is still behind a central armored figure in a brighter alien forest of curved trees. A red bar is over a creature-like shape to the right, so combat targeting is present
- cite:2mOneHr2pJA@40 boundary: e3-2006. not a final-live rule. gap: The followed character is in the lower center amid red ground effects and dark foliage. Across the minute the camera keeps that behind-the-body framing while the terrain changes, w
- cite:b-WhHUWv8WA@0 boundary: e3-2006. not a final-live rule. gap: Dark interior. A huge red wireframe sphere dominates the upper center. At least two humanoids are low in the frame, one nearer the camera and one in bright green to the right. Bott
- cite:b-WhHUWv8WA@8 boundary: e3-2006. not a final-live rule. gap: The red wire sphere is still large in view and a green cloud or beam is on the left. The group is still clustered under the effect rather than spread across a wide field.
- cite:b-WhHUWv8WA@20 boundary: e3-2006. not a final-live rule. gap: The camera has moved down a ribbed organic corridor with fire ahead. A green-clad figure is in front of the camera character. A line of light text is centered on the screen but is 
- cite:b-WhHUWv8WA@32 boundary: e3-2006. not a final-live rule. gap: The followed character is on a sloped floor in the same dark complex, still third person, with the green figure nearby. Locomotion reads as the camera trailing a walking or running
- cite:v1GogJZ9wxQ@0 boundary: e3-2006. not a final-live rule. gap: Interior room with shelves and a large blue text window on the left. Two humanoids stand close together near the center, one slightly behind the other, in third person. The blue wi
- cite:v1GogJZ9wxQ@12 boundary: e3-2006. not a final-live rule. gap: A character in a yellow-brown suit is centered, seen from behind, walking out through a rectangular doorway onto a platform. Another smaller figure is farther ahead on the platform
- cite:v1GogJZ9wxQ@24 boundary: e3-2006. not a final-live rule. gap: The same behind-the-back framing continues outdoors along a walled path with trees. The character is moving away from the camera between the 12s, 16s, 20s, and 24s samples.
- cite:TaxGO-rjLxg@0 boundary: e3-2006. not a final-live rule. gap: Title card in red text on black: E3 2006 and Tabula Rasa. This is the clip labeling itself as E3, separate from the June 2006 upload date.
- cite:TaxGO-rjLxg@4 boundary: e3-2006. not a final-live rule. gap: Third-person view in a dark rocky space. The camera character is low in frame. Another armored body lies or crouches ahead with a green marker. A round minimap is at the lower righ
- cite:TaxGO-rjLxg@16 boundary: e3-2006. not a final-live rule. gap: Two dark armored figures stand on a path in a rocky cut, camera behind them. A light-colored objective line is across the upper screen. At 360p it appears to be an objective-comple
- cite:TaxGO-rjLxg@32 boundary: e3-2006. not a final-live rule. gap: A full-height character window is open on the left over the third-person scene: a humanoid paper doll, rows of attributes, and a second red-armored figure still visible in the worl
- cite:TaxGO-rjLxg@48 boundary: e3-2006. not a final-live rule. gap: The character window is gone. An orange-suited figure runs away from the camera along a road toward a large dark gate with a bright blue opening. The same behind-the-runner framing
- cite:TaxGO-rjLxg@64 boundary: e3-2006. not a final-live rule. gap: End card uses the MMOG-Welten web address, matching the sister files 95ls4AKYAUA and t7aX-sACBTw.
- cite:n1ekRiQB1iY@0 boundary: e3-2006. not a final-live rule. gap: Photographed monitor. Third person behind a white-armored humanoid on a bright forest path. The figure holds a long weapon upright. A round minimap and bottom unit frames are prese
- cite:n1ekRiQB1iY@16 boundary: e3-2006. not a final-live rule. gap: The view is in blue-purple brush against rock. Small red marks are in the vegetation. The white figure is still the camera anchor at the bottom when visible in neighboring samples.
- cite:n1ekRiQB1iY@24 boundary: e3-2006. not a final-live rule. gap: The white figure is center-low with blue particle streaks around the body and a bright flash ahead, in a grove of curved trees. Combat is shown as colored particles around the foll
- cite:n1ekRiQB1iY@32 boundary: e3-2006. not a final-live rule. gap: The white figure is on a dirt path moving away from the camera, long weapon in hand, with two or more smaller figures farther up the path. Red bars are over some of those figures.
- cite:95ls4AKYAUA@0 boundary: e3-2006. not a final-live rule. gap: Black title card with red text reading E3 2006 and Tabula Rasa.
- cite:95ls4AKYAUA@4 boundary: e3-2006. not a final-live rule. gap: Clean gameplay capture, not a photo of a monitor. An orange-and-teal armored humanoid is low in frame in a running pose while a second figure higher on the rocks is wrapped in a bl
- cite:95ls4AKYAUA@28 boundary: e3-2006. not a final-live rule. gap: Two armored humanoids stand close together in the foreground, one aiming a large rifle toward a shape on the right slope. They are grouped as a pair in front of the camera rather t
- cite:95ls4AKYAUA@44 boundary: e3-2006. not a final-live rule. gap: A large dark multi-legged or winged creature fills the center with a white-blue flash at its base. A smaller figure is in front of it. Red bars sit over parts of the creature. Comb
- cite:95ls4AKYAUA@60 boundary: e3-2006. not a final-live rule. gap: Camera returns behind a blue-purple haired or helmeted figure in the forest, still third person, with other bodies and red bars ahead. Locomotion across the clip is repeated behind
- cite:95ls4AKYAUA@76 boundary: e3-2006. not a final-live rule. gap: End card shows www.MMOG-Welten.de.
- cite:t7aX-sACBTw@0 boundary: e3-2006. not a final-live rule. gap: Black title card with red text reading E3 2006 and Tabula Rasa.
- cite:t7aX-sACBTw@4 boundary: e3-2006. not a final-live rule. gap: Forest floor, third person behind a yellow-helmeted figure. Two nameplated bodies are ahead near a large green translucent dome. Red and blue bars are over those bodies. Bottom uni
- cite:t7aX-sACBTw@20 boundary: e3-2006. not a final-live rule. gap: The yellow-helmeted figure is still the camera anchor, now beside a pale crouching or fallen body and a small mechanical or creature shape, with green triangular markers farther ou
- cite:t7aX-sACBTw@36 boundary: e3-2006. not a final-live rule. gap: Wide combat shot: thick blue and red beams cross the frame between several figures, with a bright impact flash on the right. The camera has pulled back from the tight over-the-shou
- cite:t7aX-sACBTw@40 boundary: e3-2006. not a final-live rule. gap: End card shows www.MMOG-Welten.de and a short German community line.
- cite:R4YJs0GOjB0@0 boundary: public-test. not a final-live rule. gap: Opening card reads Control Point Wargame on Public Test Server, with a song credit. The uploader description calls it a control-point wargame on the public test server, red versus 
- cite:R4YJs0GOjB0@12 boundary: public-test. not a final-live rule. gap: Third-person view behind an armored player inside a sandy structure. Other armed humanoids stand nearby. A target portrait and red bar sit at top center. Bottom-left portrait has r
- cite:R4YJs0GOjB0@24 boundary: public-test. not a final-live rule. gap: The player runs across open ground with a blue glow at the feet and the camera held behind the character. Mission Tracker is in the top right. A circular minimap with a zone label 
- cite:R4YJs0GOjB0@36 boundary: public-test. not a final-live rule. gap: A large translucent red dome covers several figures. The player is in the foreground with a weapon raised. Hostile red bars are visible past the dome.
- cite:R4YJs0GOjB0@60 boundary: public-test. not a final-live rule. gap: The player stands aiming at a stationary turret-like machine in fog. The target frame is up. The minimap shows a tight cluster of white and red dots.
- cite:R4YJs0GOjB0@84 boundary: public-test. not a final-live rule. gap: Several humanoids and smaller creatures are in one fight. White and red beams cross the group. Red floating numbers appear over targets. Friendly blue nameplates and hostile red ba
- cite:R4YJs0GOjB0@132 boundary: public-test. not a final-live rule. gap: On a raised platform the player aims into a red beam or dome while other players and a large machine fight nearby. The combat log is repeating short system lines.
- cite:R4YJs0GOjB0@180 boundary: public-test. not a final-live rule. gap: In a red-lit corridor the player fires while another armored player and a large bipedal machine are ahead. The minimap is crowded with red and white contacts. The combat log is sti
- cite:LUAPx89dxTw@0 boundary: beta. not a final-live rule. gap: Title card reads End of Beta Event 2007-10-26. The description says this is part two of a montage from that event and points at a part one.
- cite:LUAPx89dxTw@12 boundary: beta. not a final-live rule. gap: Large overlay text reads Bane Hospital Camping and Alia Das over a blue-lit indoor crowd. The HUD is already up: chat, Mission Tracker, target frame, minimap, and the same third-pe
- cite:LUAPx89dxTw@42 boundary: beta. not a final-live rule. gap: A wide indoor view is filled with blue friendly nameplates and a few red hostile bars. The camera character is at the bottom edge rather than in the middle of the group. Players ar
- cite:LUAPx89dxTw@70 boundary: beta. not a final-live rule. gap: A bright horizontal beam fight with many nameplates. The combat log shows short failure lines, including wording consistent with not having enough power, and chat about getting som
- cite:LUAPx89dxTw@130 boundary: beta. not a final-live rule. gap: Outdoors, the player looks along a fenced path. A few other players walk ahead on their own heading. Creatures are off to the side. Chat shows join and leave lines plus player chat
- cite:LUAPx89dxTw@144 boundary: beta. not a final-live rule. gap: On a metal bridge or walkway, many players and creatures fight at once. Blue and red nameplates overlap. The player stands in the foreground while the group engages ahead.
- cite:LUAPx89dxTw@216 boundary: beta. not a final-live rule. gap: The player runs toward a vertical blue beam on a paved pad. Several other players stand around the beam at different facings. The minimap is a dense knot of dots.
- cite:LUAPx89dxTw@324 boundary: beta. not a final-live rule. gap: A paved gathering with many players in different armor colors. Chat includes lines that OCR fragments match to admin or server messages, plus ordinary player chat. People stand in 
- cite:LUAPx89dxTw@396 boundary: beta. not a final-live rule. gap: A very large outdoor crowd, with chat that includes an event call for more people. Players and large creatures share the frame. The camera character is in the foreground, not leadi
- cite:LUAPx89dxTw@432 boundary: beta. not a final-live rule. gap: The clip ends on a bright outdoor crowd and a large white-blue effect, still with the full HUD and a packed minimap.
- cite:bCQgVDS-OJQ@0 gap: Black card with a glowing mark and the credit 'by Spartan Fidelity'. The description says the clip is set to that band rather than a Mad World cut, and links the retail site. This observation was not implemented as a server change.
- cite:bCQgVDS-OJQ@12 gap: Sepia third-person shot of an armored figure approaching a large multi-legged creature in grass. No chat, bars, minimap, or target frame are visible. This observation was not implemented as a server change.
- cite:bCQgVDS-OJQ@24 gap: The figure swings a weapon at tall plant-like or creature forms beside a carved circular structure. The grade stays monochrome. This observation was not implemented as a server change.
- cite:bCQgVDS-OJQ@60 gap: The figure stands before a tall bright column in a dark ruined space, seen from behind. This observation was not implemented as a server change.
- cite:bCQgVDS-OJQ@96 gap: Wide sepia view of disc-shaped structures on a terraced rock face. A small humanoid is on the terraces. No HUD. This observation was not implemented as a server change.
- cite:bCQgVDS-OJQ@132 gap: The figure walks away from the camera across a dark plain toward raised platforms and distant lights, weapon in hand. This observation was not implemented as a server change.
- cite:bCQgVDS-OJQ@168 gap: A bright explosion fills the frame, with the top of a helmet at the bottom edge. This observation was not implemented as a server change.
- cite:bCQgVDS-OJQ@221 gap: From a rise, the figure fires into a dusty courtyard where several other figures are gathered. Still no HUD. This observation was not implemented as a server change.
- cite:bCQgVDS-OJQ@240 gap: Two armored figures stand side by side at a railing, seen from behind, looking at a rock formation. They are close but not in a stacked follow pose. This observation was not implemented as a server change.
- cite:Iiq8oaJXxVc@0 boundary: beta. not a final-live rule. gap: Title card reads Beta Test Characters, with a Spartan Fidelity song credit. The description says these are the uploader's characters from the beta test.
- cite:Iiq8oaJXxVc@24 boundary: beta. not a final-live rule. gap: Third-person run toward red crystal structures. A hostile bar is targeted. The combat log is filling with short yellow gain lines. Bottom-left bars, a weapon label, Mission Tracker
- cite:Iiq8oaJXxVc@36 boundary: beta. not a final-live rule. gap: The same red-accented character stands on stone while several tall creatures approach across a red patch of ground. Greenish name text sits on the creatures. The player is alone in
- cite:Iiq8oaJXxVc@60 boundary: beta. not a final-live rule. gap: A different armor set, blue with a glow, runs through trees toward a creature. The nameplate has changed from the red-accented character. Foot glow returns while moving. Ability ic
- cite:Iiq8oaJXxVc@96 boundary: beta. not a final-live rule. gap: Another character, red cloak, level badge reading in the mid-teens, with Mission Tracker text on the right that is too small to read. The view is over the character toward an empty
- cite:Iiq8oaJXxVc@132 boundary: beta. not a final-live rule. gap: A grey-armored character advances with a green spherical effect and green ground light. The combat log continues. Minimap shows green terrain with white and red dots.
- cite:Iiq8oaJXxVc@168 boundary: beta. not a final-live rule. gap: Another nameplate, level badge again in the mid-teens, firing uphill at creatures with red bars. Gain lines continue in the log.
- cite:Iiq8oaJXxVc@185 boundary: beta. not a final-live rule. gap: Camera sits on the weapon, yellow lights along the barrel, still with the HUD and gain lines. The character is shooting rather than locked to another player.
- cite:Iiq8oaJXxVc@204 boundary: beta. not a final-live rule. gap: A further armor and nameplate, badge reading around 20, fights a creature at a rock face with a white muzzle effect and green impact. Solo framing continues.
- cite:Iiq8oaJXxVc@240 boundary: beta. not a final-live rule. gap: A full-screen location card reads Memory Tree Hill and Concordia Wilderness over ongoing combat. The HUD remains visible behind the card, including a different nameplate whose badg
- cite:Iiq8oaJXxVc@264 boundary: beta. not a final-live rule. gap: Closing card thanks developers, designers, and support personnel and says thanks for a great game.
- cite:A4udsM0rcLo@8 gap: At AFS Preparation Camp the player Zorlac Ripslayer runs behind two squad nameplates. Squad chat says The Means of Production is being shared with Varko Ulliuvenn, that Radmon Aolo This observation was not implemented as a server change.
- cite:A4udsM0rcLo@8 gap: The mission tracker reads Wilderness Targets of Opportunity, Complete All 10 Targets of Opportunity, Kill 40 Miasmas with 8 of 40, Kill 40 Xanx with 28 of 40, and Kill 30 Shield Dr This observation was not implemented as a server change.
- cite:A4udsM0rcLo@88 gap: The zone label reads Frontlines. The player is in the foreground of a sandbag fight, reloading a Teleract Rifle, while squad nameplates are in the melee on the targeted Hominis Mac This observation was not implemented as a server change.
- cite:A4udsM0rcLo@180 gap: Still in Frontlines, the player is running toward a fight in which the squad nameplates are already engaged. This observation was not implemented as a server change.
- cite:A4udsM0rcLo@244 gap: The zone label context is the interior. The player moves alone through a dark cluttered passage. Both squad frames are still filled. No squad body is in view. Chat mentions a Pravu This observation was not implemented as a server change.
- cite:A4udsM0rcLo@292 gap: The minimap label reads Pravus Interior Halls. The player runs down a ramp past a body. The squad frames for Ulliuvenn and Aolon are still present. This observation was not implemented as a server change.
- cite:A4udsM0rcLo@408 gap: The minimap label reads Pravus Production Chamber. Squad nameplates are in a fight ahead of the player, who is still back on the floor. This observation was not implemented as a server change.
- cite:A4udsM0rcLo@548 gap: Still in the Pravus Production Chamber, the target frame reads Prototype Forean Machina. The player is shooting, and the same squad nameplates are in the melee. This observation was not implemented as a server change.
- cite:-I1WM5ddg-k@45 gap: Draco Unicornn walks alone toward a hostile Thrax with the reticle on it. Only one unit frame is on screen. This observation was not implemented as a server change.
- cite:-I1WM5ddg-k@120 gap: The player runs alone across open ground with a Logos window open. A creature is on the ridge ahead, not beside the player. This observation was not implemented as a server change.
- cite:-I1WM5ddg-k@225 gap: The player is in melee with a pack of Thrax, shotgun out, damage numbers on the targets. No second unit frame. This observation was not implemented as a server change.
- cite:-I1WM5ddg-k@400 gap: The player runs alone through a dark stretch. The mission tracker is open. No second character is beside them. This observation was not implemented as a server change.
- cite:-I1WM5ddg-k@600 gap: The player stands at a trainer pedestal with a character training window open and a press-to-talk prompt. Another figure stands off to the side of the camp, not on the player's uni This observation was not implemented as a server change.
- cite:-I1WM5ddg-k@750 gap: Indoors, the player stands still in front of an NPC with a press-to-talk prompt while a training window covers the left side. This observation was not implemented as a server change.
- cite:-I1WM5ddg-k@1260 gap: The player runs alone along a path with a weapon out. A hostile name is targeted farther along the trail. This observation was not implemented as a server change.
- cite:-I1WM5ddg-k@1360 gap: The player stands in melee range of a single large Thrax and fires. Corpses are on the ground. Only the player's unit frame is shown. This observation was not implemented as a server change.
- cite:-I1WM5ddg-k@1488 reconciled: seeded mission 1390 Conscientious Objector completes at Warrior Apirka for 2,000 XP. gap: the two selectable item templates are unidentified, and the recording date is unknown.
- cite:-I1WM5ddg-k@1510 reconciled: accepting that offer is the seeded Apirka handoff. gap: the clip does not show Rogers or a reward, and the recording date is unknown.
- cite:-I1WM5ddg-k@1850 gap: The player runs alone toward one standing NPC with a press-to-talk prompt. This observation was not implemented as a server change.
- cite:-I1WM5ddg-k@2200 gap: The player runs alone toward a blue gate marked Twin Pillars. A single distant figure is inside the gate, not at the player's side. This observation was not implemented as a server change.
- cite:-I1WM5ddg-k@2500 gap: In a paved interior, several other characters stand around while say-lines scroll in chat. The player has a weapon out. They are not stacked on the player as a pair of followers. This observation was not implemented as a server change.
- cite:-I1WM5ddg-k@2700 gap: The player runs alone on a forest path with a hostile targeted ahead. This observation was not implemented as a server change.
- cite:-I1WM5ddg-k@2880 gap: The player stands among corpses under a dropship near a landing structure. Another character is off to the side. A talk prompt is up. This observation was not implemented as a server change.
- cite:Of3Uxw9MNec@2 gap: An uploader title card sits over the footage before the fight continues. This observation was not implemented as a server change.
- cite:Of3Uxw9MNec@16 gap: The player is already in melee beside a tree. Another humanoid is nearby in the fight. Only one unit frame is shown. This observation was not implemented as a server change.
- cite:Of3Uxw9MNec@52 gap: The player runs forward through the fight. The minimap carries a control-point label consistent with the uploader's Imperial Valley CP. This observation was not implemented as a server change.
- cite:Of3Uxw9MNec@100 gap: The player runs up a ramp into the control-point structures while fire and bodies fill the approach. Other characters are in the fight. No second unit frame trails the player. This observation was not implemented as a server change.
- cite:Of3Uxw9MNec@148 gap: The player shoots from behind fallen cover at a target in the trees, still with a single unit frame. This observation was not implemented as a server change.
- cite:EiE2oodlP8A@0 boundary: public-test. not a final-live rule. gap: The clip opens on the Tabula Rasa logo, then a black title card that reads Deployment 11 Bootcamp Mini-Game (Kind of).
- cite:EiE2oodlP8A@14 boundary: public-test. not a final-live rule. gap: A group of recruits in gray exercise in front of a large paneled wall. Many have their arms up in a jumping-jack pose. Several are already prone. One figure in a cap is in the fore
- cite:EiE2oodlP8A@44 boundary: public-test. not a final-live rule. gap: Most of the group is standing while several are still on the ground and one is partway through a push-up.
- cite:EiE2oodlP8A@62 boundary: public-test. not a final-live rule. gap: Nearly the whole group is down in a push-up or prone pose together, with one or two slightly out of phase.

## 2026-09-25 — creation and first login compared with labelled evidence

`spawn.first_login` in `docs/evidence/bootcamp-d11-positions.json` is map 1985 at (387.2, 136.75, -79.09), rotation 2.879793, tier measured. The shipped creation path stores that row as content location 19851 when boot-camp entry is on, and `CharacterRepository.Get` returns the same map, position, rotation, and name on a second load. With entry off, creation still uses the existing wilderness default map 1220 at (894.9, 307.9, 347.1), rotation 0. Those defaults were not given a new footage spawn.

Family name, character name, the five appearance slots, rank-1 skills 1/8/19/49/165, the Lightning and rifle tray, and the starter items already follow `docs/evidence/character-creation-appearance.json`, `docs/evidence/new-character-loadout.json`, and `docs/evidence/race-unlocks.json`. No reward, rate, spawn, movement value, or companion parameter was added. Still open, and left unimplemented: the exact later level and class bonus timing, and the tutorial or skip rewards, recorded in `docs/new-character-client-evidence.md`.

## 2026-09-26 — Four ready conversation missions seeded (EarlyReadyMissions)

`20260926100000_EarlyReadyMissions` (SQLite and MySQL, frozen rows in `WildernessData/EarlyReadyMissionsRows.cs`)
seeds 1742 Report to Liaison Brice, 441 In Short Supply, 434 Rendezvous At The LZ and 408 Revealing Treeback
Experimentation from the 2026-09-26 dossiers. Each proposed row was re-checked against the recovered development
tree and the deployed world first; all still apply. Givers and receivers are inferred from the client's own log and
opening texts, TaRapedia and Ellatha, with 434's stale beta-era infobox giver (Jennings) rejected. The objectives are
the client's own rows, and the rewards are TaRapedia's 2007 experience and credits (Ellatha agrees on 434's and 441's
credits). 441 is gated on 549. Package 212 is bound to Randolph (130).

Two findings changed the batch:

- Randolph (130) and Standley (134) were on `Redshirt_Human_Soldier_Light_Male` (aug `1`), which the client cannot
  converse with. They now use the original swapset classes, female 3848 for Randolph (client texts 762-764: "she",
  "a busy woman") and male 3846 for Standley, with outfits copied from world NPCs. Both classes and both outfits
  are analogues under OD-45.
- The client keeps a completion row on each giver's package that only redirects the player: (434,1,208) on
  Witherspoon and (441,1,212) on Randolph. The server completes through any row of the speaker's package, so both
  would have let the giver close the objective on the spot. `MissionManager.LoadMissions` now leaves them out, via
  `Rasa.Data.MissionRedirectConversations`, and the table rows are unchanged. 429's (429,4,116) row on Rogers has the same
  shape and is excluded too: `docs/river-recon-client-evidence.md` names that exact row as one that must not become
  a completion route, and while it stayed loaded Rogers could close River Recon's objective 4 himself. Objective 4
  now completes only at Witherspoon (package 208).

Left out and recorded as gaps:

- the unseeded prerequisites 432 and 406;
- the reward items, whose only lists predate update 1.4 (441's grenade/med-pack pair resolves to 45125/44917, but
  it is not final-state evidence);
- the field report and data pack the player carries, whose client item classes are ambiguous;
- Jorai's position, which is the Viands Village marker, 67.6 m from TaRapedia's /loc;
- the mission levels, which are the zone band (5; 408 at 15).

The gaps are GAP-READY-REDIRECT-COMPLETION, -434-GATE, -408-GATE, -MISSION-ITEM, -REWARD-ITEMS, -SPEAKER-CLASS,
-JORAI-POSITION and GAP-MISSION-LEVEL. Provenance: the manifest rows with migration `EarlyReadyMissions` and
`docs/evidence/early-ready-missions.json`.

## 2026-09-26 — world placement corrections (`WorldDefectsFix`)

Migration `20260926130000_WorldDefectsFix` (rows in `WorldDefectsFixRows.cs`) corrects the rows two read-only audits
of the deployed world found wrong. Probes and dated readings: `docs/evidence/world-defects-20260926.json`. Labels:
the manifest rows for placements 199106/199107 and spawn pools 510137, 510133, 510068, 510118, 510117 and 510085.

- **Treeback Camp.** Field Lt. Bagby (199106) and Lt. Galloway (199107) move from the Palisades overworld (1244) to
  the Treeback Camp instance (1397) at TaRapedia's own x,y,z. The final client's 1788/1789 logs place them at the camp,
  and their pages' infoboxes say `Instance=Treeback Camp`. The readings sit 0.12 and 0.02 m off the instance floor and
  34–40 m under the overworld, so `WorldFloorSweep` had lifted two sourced values; its rows stay as history. Missions
  1788/1789 still resolve: 1397 is a shared context with its own channel, and map links 21/35 are enabled.
  Remaining: `GAP-TREEBACK-GALLOWAY-SCRIPTED` and `GAP-TREEBACK-POSITIONS-PRE-D11`.
- **Warnet Queen.** Spawn pool 520046 is removed (it came from InfiniteRasa's boss preloader and was seeded by a migration
  the deployed world has run). It was a generic hostile level-28 queen on the Divide's "Foreas Base" label, 0.5 m from
  the receiver of seeded 1743, and no source places one there or on the Palisades overworld (`GAP-PALISADES-WARNET-QUEEN`).
- **Marker-stacked receivers.** Valerie Corman: post-D11 TaRapedia plus codex-tr.net, x,z inferred (high). Ranger Jorai:
  pre-D11 TaRapedia only, medium confidence (`GAP-JORAI-PRE-D11-LOC`). Lieutenant Epp: post-D11 TaRapedia, 2.2 m from the
  client's hospital marker. For each, y is the navmesh floor (measured). Kearney and Mela have no source and keep
  upstream's marker positions as analogues (OD-59, `GAP-KEARNEY-POSITION`, `GAP-MELA-POSITION`).
- **Duplicate Whitaker.** Pool 510085 goes to counts 0/0. Placement 199500 at TaRapedia's one reading stays the giver of
  640 (`GAP-WHITAKER-NAME-ID`: the client has names 5428 and 8919 for him). Liu's placement comment now names the Eir
  Crater Field Hospital.
- **Not moved.** Clark's and Norton's "2008-09-25" coordinates are a template reformat of beta readings (2007-08-17),
  so neither moves. Clark stands where the navmesh route does not reach (`GAP-CLARK-POSITION`, `GAP-NORTON-POSITION`).
  Perdu, Obahmi, Franks, Foletto, Nicholson, Orto and Creelig have no post-D11 reading (`GAP-TORDEN-MARKER-POSITIONS`).

## 2026-09-26 — Torden conversation missions: nine seeded, 936 held

`TordenConversationMissions` (20260926110000) seeds nine missions from the Torden dossiers
(`research/20260926-torden-missions`), each completable through the final client's own objectiveconversation rows on
NPCs the world already places. No NPC, placement or package is created or moved.

| Mission | Giver → receiver | Level | Rewards seeded |
| --- | --- | --- | --- |
| 1745 Report to Liaison Repp | Arizpe 199085 → Repp 199505 | 15 (Palisades band) | 10,500 XP / 1,050 cr, pre-1.4 |
| 526 Aid Packages | Epp 510068 → Epp (Maila, Orto deliveries) | 35 (Incline) | 33,000 / 4,350, pre-1.4 |
| 648 Speak to Colonel Franks | Xeniol 199045 → Franks 510084 | 35 (Plains) | 12,500 / 2,500, pre-1.4 |
| 802 Deliver Mire Station Field Report | Obahmi 510067 → Foletto 510066 | 35 (Incline) | 14,500 / 2,700, pre-1.4 |
| 1014 Blue Flu | Parkman 199069 → Norton 510187, Provost first | 20 (Plateau) | none (beta-only XP left out) |
| 1064 Incriminating Delivery | Liu 199510 → Franks 510084, after 1063 | 35 (Plains) | 50,000 / 5,000, pre-1.4 |
| 1070 Go to Incline, Young Soldier | Franks 510084 → Obahmi 510067 | 35 (Plains) | 12,500 / 2,500, pre-1.4 |
| 1326 Spoils of War | Chester 510198 → Snake 199203 | 20 (analogue, OD-60) | 32,500 / 3,600 and one of four Class VII stacks, post-1.4 |
| 1330 Retread Planning Part II | Hermit 199075 → Amee Corman 199204 | 20 (analogue, OD-60) | 30,000 / 3,500 and one of four Class VII stacks, post-1.4 |

- **Evidence.** Objective texts are original (missionobjective). Givers and receivers are inferred from the client's
  texts and dated TaRapedia revisions. The objective flags, 1014's order (CID HQ, then the warehouse, revealed by
  a 2 → 1 transition) and the 887/1063 gates are inferred. Every field is in the manifest with its tier and citation,
  under migration `TordenConversationMissions`, and `TordenConversationMissionsTests` checks the seed against it.
- **Reward era.** Six missions' amounts are recorded only before Update 1.4 (2008-01-29). They are seeded as the
  earlier W3 batches seeded TaRapedia's amounts, labelled `era: pre-1.4` in their rows (GAP-TORDEN-REWARD-ERA).
  Pre-1.4 item lists are never seeded (GAP-TORDEN-REWARD-ITEMS). 1326/1330's post-1.4 consumables resolve one name
  to one template, but the choose-one structure and the ×2/×4 quantities are inferred; the 1.7 requirement change and
  the incomplete D9/D14/January 2009 notes stay open risks (GAP-TORDEN-1326-1330-ITEM-CHOICE).
- **Held.** 936: Science Officer Clark (510191) stands on a surface the Plateau navmesh does not connect to the Wedge
  Rock outpost. Every path from the waypoint, the hospital, the crafting station and Fort Defiance is partial,
  while TaRapedia's 2008-09-25 /loc is reachable (`docs/evidence/torden-conversation-missions-navmesh.json`,
  GAP-TORDEN-936-CLARK-UNREACHABLE). He is left for the world-placement batch. Also held: 555 (radio dispense on the
  unseeded 867), the 831/840 and 842/848 branch pairs, and 1862 (Sergeant Dekay cannot be placed).
- **Gaps opened.** Unseeded or unknown gates for 526 (804), 648, 1326 (1324) and 1330 (563). Items handed over at
  accept are not granted (526, 802, 1064). Also open: 1014's rewards, 1064's scripted ambush, Norton's unconscious
  pose, and the marker-stacked NPC positions these missions use (GAP-TORDEN-NPC-POSITIONS).
- **Pools level.** The Pools has no band. 1326/1330 take 20 from 1068/1541 as an analogue under OD-60, which is
  pending owner review: Smash and Grab records Level=35 and the rewards require 31–35. `ProvenanceRegistry` now
  lists `npc_mission`, with every column required, so a level can carry that label.
- **Not verified in-game.** None of the nine has been played with the original client.

## 2026-09-26 — Reward items for seven seeded missions

`MissionRewardItems` (20260926120000) adds the reward items of seven missions that already paid their credits and
experience. Each list is TaRapedia's post-1.4 `RewardItem` field, offered as one choice (type 5) in the wiki's order:

| Mission | Templates (quantity) | Wiki revision | Identity confidence |
|---|---|---|---|
| 1541 New Orders From The General | 120418, 120419, 120420, 120421 | `?oldid=29408` (2008-03-02) | high |
| 1673 It Lies in Ruins | 120101, 120102, 120103, 120104 | `?oldid=29815` (2008-03-17) | high |
| 1040 Incommunicado | 120382, 120383, 120384, 120385 | `?oldid=29133` (2008-02-23) | high |
| 983 You're the Guy | 120804, 120805, 120806 | `?oldid=30043` (2008-03-29) | medium |
| 970 Lookout Down Below | 45059 x2, 118897 x2, 111037 x4, 111027 x4 | `?oldid=29117` (2008-02-23) | high (items) |
| 1068 South Of The Border | 45059 x2, 118906 x2, 111017 x4, 111027 x4 | `?oldid=29406` (2008-03-02) | high (items) |
| 1863 Missing in Action | 45062 x2, 118898 x2, 111048 x4, 45446 x4 | `?oldid=29490`, restated `30095` | high (items) |

- How a name became a template: the final client's `itemTemplateItemClass` stores mission rewards as runs of
  consecutive ids. Each equipment list matches exactly one strict run whose classes carry the wiki's armor version,
  rarity and level (the client's `reqData` level equals every wiki level). A random 4-entry list of the same shape
  matched a run 0 times in 500; 983's 3-entry, name-only list 3-5% of the time, so 983 is medium confidence. The
  consumable names are each one class with one template. For 1541, block 120109-120112 shares three of its four
  classes, and only "Stealth Legs v4" picks 120418-120421.
- Every template id, quantity and the choose-one structure is `inferred` (manifest rows with migration
  `MissionRewardItems`). No client table links a mission to a template. All 25 templates are in the world seed with
  their class, stack size and category, and load and deliver through the real `ItemManager`.
- The final-state basis is "post-1.4, no documented later change". The 2008-04-28 note that about 70 unnamed mission
  rewards had their armor or tool requirements corrected postdates every list and stays an open risk
  (`GAP-MISSION-REWARD-FINAL-STATE`; the notes for D9, D14 and January 2009 may be incomplete).
- Not seeded: the manufacturer prefixes (Dynamo, Teleract, Shinobi and the rest), which are item modules no client
  table maps (`GAP-MISSION-REWARD-MODULES`); whether a bundle granted one stack or all four (`GAP-MISSION-REWARD-CHOICE`).
  The mission levels are unchanged although the rewards require 26-39 against seeded levels of 20-25; the
  contradiction is recorded in `GAP-MISSION-LEVEL`.
- Held, with reasons in `GAP-MISSION-REWARD-ITEMS`: the 23 Logos missions (the shrine already grants the Logos), 1407
  (its post-1.4 reward is two modification recipes whose templates are server data), 411/412/1390 (partial), every
  pre-1.4-only list and every 2-entry run. 479's vest 13738 is a V04 counterpart where the wiki names v6; recorded,
  not changed.
- Tests: `ContentSchemaMigrationTests` (forward, rollback and re-apply), `MissionRewardItemsProvenanceTests` (seed
  against manifest and the approved list), and `BootcampReinforcementsScenarioTests` (the seven missions load their
  offer from the migrated world, and a turn-in at the seeded receiver delivers the chosen item and stack with the
  completion).

## 2026-09-26 — Munson's missions withdrawn; the doctors' sample missions collect their items

Two migrations, from the collection-mission dossiers (`research/20260926-collection-missions`). Every field's tier and
citation is in the manifest under migrations `WildernessMunsonWithdrawal` and `WildernessXenobiologySamples`, and
`WildernessXenobiologySamplesTests` checks the seed against it.

**Withdrawn at D11 (tier original).** The official Deployment 11 live notes ("Deployment 11.6: 8/15/2008", "Deployment
11 is on Live!", rgtr capture SHA-256 `3dfe1626…`, the bytes `docs/bootcamp-client-evidence.md` cites) say Dr.
Munson's three sample missions, "Boargar, Treelurkers and Miasmas", "are no longer available", and "Mission:
Predatory: This mission has been permanently disabled". So **751 Boargar Acquisition, 780 Treelurker Samples, 767
Mighty Miasma and 769 Predatory are intentionally never offered in the final state**. 767 had been seeded
(`WildernessCollectionDrop`, 2026-09-17). `WildernessMunsonWithdrawal` (20260926140000) removes its definition, kill
binding, counter and rewards, and returns its client objective row to the NULL-flag skeleton. Dr. Munson and the Bane
Miasma stay in the world. The in-progress clause is not modelled: a player with a Munson mission already in the log
could finish it, and an in-log Predatory failed on its own (`GAP-D11-WITHDRAWN-IN-PROGRESS`).

The rest of those notes, checked against this tree:

- Nothing else they withdraw is seeded. Body Count (Caves of Donn) is not seeded. No Predator or Juggernaut spawns
  in the Wilderness, CLRF or Pravus Research contexts.
- Richards already stands at the Pinhole Falls Caverns entrance.
- Mama Miasma's client target is already the D11 value of 3, and the mission is not seeded.
- The trainer cull ("no Tier 4 trainers before Mires, no Tier 2 and 3 after") is superseded by D12's single class
  trainers (`docs/character-progression-client-evidence.md`). It was flagged, not acted on.

**Sample missions** (`WildernessXenobiologySamples`, 20260926141000). The objectives use 479's item mechanism: the
counter is keyed by the client item class, it advances only when the item enters the inventory, and the turn-in
consumes the items.

| Mission | Doctor | Item (class, template) | Source creature | Drop | Rewards | Gate |
|---|---|---|---|---|---|---|
| 758 Fithikally Challenged (new) | Soji 111 | Fithik Spleen 11161, 2533 | Fithik 1 | 100, inferred | 4,000 XP / 600 | 771 |
| 776 Soldier's Blood (new) | Ojy 110 | Thrax Blood Sample 11150, 2524 | Thrax Soldier 3 | 50, analogue (OD-61) | 4,000 XP / 600 | none |
| 771 Droning On (was a kill count) | Ojy 110 | Shield Drone Scraps 11153, 2527 | Bane Shield Drone 85 | 50, analogue (OD-61) | 600 (was 300) | none |
| 787 Xanx For the Help (was a kill count) | Soji 111 | Xanx Pincers 11160, 2532 | Bane Xanx 87 | 100, inferred | unchanged | 758 (new) |

- **What is original.** The objective texts and targets, and the item classes: the MisXeno block of entityclass,
  named in physicalentityclassnamelanguage and matched to each objective by name, as 479 was. Each class maps to two
  identical templates, and the lower one is taken by 479's rule (`GAP-COLLECTION-TEMPLATE-CHOICE`).
- **Inferred.** Givers, receivers, gates, experience and credits come from the client logs and dated TaRapedia and
  Ellatha records. The 787 gate comes from the client's own 787 opening: "you really hooked me up with those Fithik
  spleens". Levels use the Wilderness band of 5 (`GAP-MISSION-LEVEL`).
- **Credit conflicts.** For 776 and 771, Ellatha (early 2008) records 600 and TaRapedia's 2007 pages record 300. The
  later record is taken, as for 479 (`GAP-DOCTOR-CREDITS-CONFLICT`). 771 still pays no experience, because no source
  gives a figure.
- **Drop chance.** No rate survives for any mission item (`GAP-COLLECTION-DROP-CHANCE`).
  - 758 and 787 drop on every kill. TaRapedia's walkthroughs count kills equal to the client target: "Kill ten
    collect item" (rev 15141, 2007-11-16) and "Travel South Kill 4 Xanx" (revs 15143/22864). This is weak pre-D11
    evidence, so the tier is inferred at low confidence. For 787 it plays the same as the kill count it replaces.
  - 776 and 771 have no such line. They repeat 479's 50% as analogues under OD-61, which is now in the manifest,
    pending owner review. This makes 771 slower than its old kill count, which never failed to count.
  - Moving collection objectives from OD-47 kill counts to item drops once the item is identified is OD-66.
- **Geography.** The sources put 758's Fithik in Ranja Cavern. There the seed has only the Hive Monarch boss. The
  nearest ordinary Fithik pool (141, Gellman Meadow) is 392 m from Soji (`GAP-758-FITHIK-GEOGRAPHY`). 776 drops from
  the Thrax Soldier only (`GAP-776-BLOOD-SOURCES`).
- **Left out.** Reward items: every list predates 1.4 (`GAP-MISSION-REWARD-ITEMS`). 771's TaRapedia gate on 795
  Lightbender Glands is held, because the seed has no ordinary Lightbender (`GAP-DOCTOR-CHAIN-GATES`). The manifest's
  771 counter row named item class 24084, which is the Shield Drone creature. It now names 11153 Shield Drone Scraps.
- **Held.** 795, 506, 433, 489, 665, 860 and 1449 are held for the dossier's reasons: missing creatures, and
  server-spawned or destructible objects with no recovered positions.
- **Not verified in-game.** None of this has been played with the original client.

## 2026-09-26 — missing mission givers placed, five missions seeded (`MissingMissionGivers`)

`20260926150000_MissingMissionGivers` (SQLite and MySQL, rows in `WildernessData/MissingMissionGiversRows.cs`) acts on
`research/20260926-missing-npcs`, which found `/loc` readings for four "unplaceable" givers in Ten Ton Hammer's dated
area guides. Probes: `docs/evidence/missing-mission-givers-navmesh.json`. Labels: the manifest rows and changes with
migration `MissingMissionGivers`, checked by `MissingMissionGiversTests`.

| NPC (id) | Where | x, z | y | Level | Class and body |
| --- | --- | --- | --- | --- | --- |
| Cmd. Sgt. Simpson (199950, name 127) | Foreas Base command centre, 1148 | TTH 2007-10-23 and TaRapedia 2008-04-16 agree to 0.6 m, inferred | floor 120.924, measured | 20, analogue (Sebastian, OD-11) | 3846, Witherspoon's set (OD-45) |
| Ranger Tarina (199951, name 135) | Foreas Base, 1148 | TTH 2007-10-23 only, inferred | floor 116.139 | 20, analogue (OD-11) | 7034 Forean Spearman with the rangers' spear (OD-45) |
| Field Sergeant Hanna (199952, name 4846) | Raintree Post, 1761, package 423 | two TTH guides (2008-03), 4.1 m from the waypoint, inferred | floor 276.008 | 28, TaRapedia | 3848, the vendor set (OD-45) |
| Sergeant Dekay (199953, name 8908) | Irendas, 1764 | midpoint of the waypoint and wormhole markers the guide names, measured ±26 m | floor 429.924 | 23, TaRapedia | 3846, Witherspoon's set (OD-45) |

- **Bindings.** Standley (134) takes package 2049 and Arizpe (199085) 2025; each package completes one mission only.
  Receptive Liaison Langerman (133), the 1741 giver, stood on the Redshirt class the client cannot converse with and
  takes 3846 and Standley's outfit, as EarlyReadyMissions did for Standley (OD-45). Noonan and Arizpe were already on
  NPC classes.
- **Mela moved.** Warrior Mela (pool 510117) leaves upstream's marker ring, an OD-59 analogue, for the same guide's
  2007-10-23 reading (222.9, 977.0) inside Thoria Das, 0.09 m from the floor. GAP-MELA-POSITION is closed.
- **Seeded.** 1741 Report to Liaison Standley (Langerman -> Standley, level 5, 4,000 XP; TaRapedia's credits read
  "None", so no credit row), 1744 Report to Liaison Arizpe (Noonan -> Arizpe, 10, 8,000 / 800), 390 Supplies for Thoria
  Das (Tarina -> Mela, 10, 13,000 / 1,950), 818 Report to Field Sergeant Hanna (Foletto -> Hanna, 35, 15,500 / 2,800)
  and 1862 The Infensus Garrison Directive (Dekay -> Michan, 35, 12,500 / 2,500 and one of three Class V stacks). The
  first four amounts are pre-1.4 and labelled so; 1862's are post-1.4 by date. Each mission keeps its one client
  objective, and every completion row sits on the receiver's own package, so no redirect row is involved.
- **Held.** Col. Almos and 551: the only surface under his reading is 3.16 m below it, nothing within its 8 m
  uncertainty comes within 0.3 m of its height, and its y/z repeat the "Viands Village" label (GAP-ALMOS-HEIGHT, OD-67
  open). The research proposal's second level at 176.12 m was a nearest-polygon artefact. Sergeant Conway and 835: no
  position, and omission is preferred over marker anchors (GAP-CONWAY-POSITION). 340 (its split from 1905) and the
  827 arms 833/841 and 842/848 (neither arm can be gated): GAP-827-BRANCH-ARMS.
- **Gaps opened.** GAP-DEKAY-POSITION, -DEKAY-RIFLEMEN (the formation he paced around), -TARINA-SINGLE-SOURCE,
  -MISSING-NPC-LEVELS, -LOGOS-CHAIN-START-POST-D11 (1741's giver rests on 2007 sources), -MISSING-GIVER-PRESENTATION,
  -MISSING-GIVERS-818-GATE (816 unseeded), -MISSING-GIVERS-REWARDS and -MISSING-GIVERS-ACCEPT-ITEMS (the crate and
  the directive).
- **Citation corrected.** `CodexNpcCorrectionsRows.cs` dated Dr. Elise Corman's coordinate "TaRapedia rev 34276,
  2008-09-25". That revision renamed headings; the coordinate has been on the page since rev 983 (2007-06-30), so it
  is a pre-D11 reading. Only the comment and the manifest record change, and her position stands.
- **Leads not acted on.** The same guides read Field Ranger Kearney (-136.9, 205.6, 518.2 in Timora Mines; he serves
  only the held 340), Foletto, Nicholson, Orto and Maila. They are recorded in GAP-KEARNEY-POSITION and
  GAP-TORDEN-MARKER-POSITIONS.
- **Checked.** Replayed against a copy of the deployed world with these rows applied, the world audits pass (26 of
  26, including the walkable-surface, prop-overlap, giver/receiver-spawn and package-carried checks).
- **Not verified in-game.** None of the five missions or four NPCs has been checked with the original client.

## 2026-09-26 — Official live notes checked against the seeded missions

A research pass (`research/20260926-notes-audit`: `findings.json` with verbatim quotes, sources and hashes) read every
official live note from Update 1.4 (2008-01-29) to D16.5 (2009-02-17) against the 114 seeded missions. PTS notes
were read separately and are not used as live evidence. Two migrations act on it:
`MissionSharedKillCredit` (20260926160000, schema) and `OfficialNotesCorrections` (20260926161000, data, both
providers, frozen rows in `OfficialNotesCorrectionsRows`).

| Mission | Note (live) | Was | Now | Tier |
| --- | --- | --- | --- | --- |
| 2016 A Mystery Unearthed | D14, 2008-11-11: "new level 50 mission offered by NPC Archaeologist Wynne Topper at Twin Pillars on Concordia: Wilderness" | level 20 (Plateau band), comment "(Plateau)" | level 50, comment "(Wilderness)" | original |
| 682 Childhood's End, objective 3 | 1.6, 2008-03-26: "it no longer matters who kills the Xanx, just that they are killed"; D8, 2008-05-19: credit "even if they advanced to this objective at the same time as another player" | killer-only credit on the shared Wilderness | `shared_kill_credit` on the binding | original (flag); reach inferred |

- **Shared kill credit, and why it is not general.** Both notes name Childhood's End and nothing else, and other
  notes of the period describe other rules for other missions (D8's Predator Hunt requires helping to kill the Alpha
  Predator). So the rule is a flag on the kill binding, not a server-wide change. When a creature a flagged binding
  names dies, `MissionManager.OnSharedKillCredit` credits every character on that map channel whose objective is
  active and incomplete, after the ordinary path has credited the killer (or a per-character instance's owner),
  so nobody is counted twice. An NPC Ranger's kill now counts too. The kill's experience and credits still go only
  to a player killer. Before this, a player with 682/3 active got nothing when anyone else killed Arioch Xanx. The
  notes give no radius or squad condition, so the channel as the reach is the server's reading
  (`GAP-NOTES-682-SHARED-CREDIT-REACH`). The loader accepts the flag only on kill bindings.
- **976 Restraining Order** ("Killing any Bane Stalker on Mires will now give credit", 1.6) needs no data change.
  Its binding names creature 199812, not a placement, and every Stalker on the Mires is that creature. The world's
  other Stalker (creature 36) stands only in the Wilderness and is not bound. The note does show the Stalkers were
  spread over the Mires, not clustered by Colonel Li Hua as OD-48 placed them. OD-48 now carries that note, and
  `GAP-NOTES-976-STALKER-DISTRIBUTION` records it. No spawns are invented.
- **1125 Security Threat.** The client opening ("Here is the key to Phanin's facility") and the 1.7 note ("will no
  longer drop two Phanin Research Facility Keycards") give one keycard at accept. The item is in the client: class
  24724 is named "Phanin Research Facility Keycard" in `physicalentityclassnamelanguage.pyo`, and template 50310 is
  that class's only template. It is not granted, because the content layer has no accept-time grant
  (`GrantItemSet` is declared but not implemented). This is the same missing mechanism as GAP-TORDEN-ACCEPT-ITEMS
  (`GAP-NOTES-1125-KEYCARD`, which also names "Package from Miras", 24723/50308, as a candidate).
- **Positions recorded, not moved.** No evidence dated after the change each note describes exists:
  - D12 moved the Cumbria Research Facility's NPCs inside New Cumbria. 1745's giver Arizpe (199085) stands at a
    2008-01-05 reading 243 m from "Waypoint: New Cumbria", and 408's giver Jamison (199089) 189 m from it
    (`GAP-NOTES-CRF-RELOCATION`).
  - 1.7 moved Info Specialist Johnson "right next to Lt. Perkins". Both stand at undated Ellatha readings 308 m
    apart, so Johnson's reading is pre-1.7. The note moves Johnson, not Perkins, and Perkins is Ellatha's reading
    rather than the guess the audit assumed. Johnson serves no seeded mission (`GAP-NOTES-321-JOHNSON-PERKINS`).
  - 1.7 moved Elder Quillas "further up the hillside path". Pool 192 agrees with Ellatha (1.1 m) and with Ten Ton
    Hammer's 2007-09-26 guide (14 m, "on a platform"), both pre-1.7 or undated, so it is probably the tree-hut spot
    (`GAP-NOTES-1390-QUILLAS-POSITION`).
- **983/1041.** The 1.7 and D8 notes call them alternative courses ("players that chose an alternate mission
  course"). They do not say what closes the other course, so no exclusion is seeded, although prerequisites could
  express one with state NotAssigned. Both follow the unseeded 982 (`GAP-NOTES-983-1041-ALTERNATIVE-COURSES`).
- **Confirmed by the notes, unchanged:** 1112 has no gate (D9.6); Richards stands at the Pinhole Falls Caverns
  entrance (D11); 366-368/411-413 remain available (D10); 771/787 are not withdrawn (D11 names only Dr. Munson's
  three); Galloway is inside Treeback Camp (1.6); 2016's receiver is Bailey (D15); 1992's text is post-D11.4. No
  live note from 1.4 to D16.5 changes mission experience or credits, so the pre-1.4 amounts stay open gaps.
- **Roster.** The never-seed list (751, 780, 769, 691, Soyuz 1999/2006-2009, Welcome Tour, Artificial Iniquity,
  the PTS-only Epic Gauntlet 2012), the 1998 Time Capsule gap and the missing final-era missions are in
  `docs/progression-preservation-plan.md`, "Final-state mission roster from the official notes". 767 and the other
  collection/missing-NPC findings belong to the parallel batches and are not touched here.
- **Tests:** `ContentSchemaMigrationTests` (forward, rollback of both migrations including the column, re-apply),
  `OfficialNotesCorrectionsProvenanceTests` (each written value against an original-tier changes entry citing the
  notes; Down restores the recorded old values; the open gaps), `MissionContentLoadingTests` (the flag only on kill
  bindings), and `BootcampOpeningTests.SharedKillCredit` (from the migrated world: another player's kill and an NPC's
  kill credit every character with 682/3 active exactly once; a character not yet at objective 3 and a bystander are
  not credited; 427/6 Proctor Fulgor stays killer-only). None of this has been checked in play with the original client.

## 2026-09-26 — one class trainer per hub (`SingleClassTrainers`), and the economy's emulator lineage

Migration `20260926180000_SingleClassTrainers` (rows in `SingleClassTrainersRows.cs`). Evidence decoded from the
1.16.5.0 client: `docs/evidence/class-trainer-evidence.json`, which also re-derives the lost 2026-09-14 class-trainer
specification (rules `CT-*`) that `ClassAdvancement` and the managers cite. Research captures:
`research/20260926-trainers-economy`. Decisions OD-95 to OD-97, all agent-approved and pending owner review.

- **Per-class trainers retired.** `Add_class_trainers` (d717ed1, InfiniteRasa pass) seeded 38 per-class trainers
  (501001–501038), six of them ringed round Kincaid. They had no package, so they could neither talk nor train. They
  are also the pre-D12 model. The official D12.5 live notes (2008-09-18, archived) say "Individual trainers for each
  class have been replaced by one single trainer on maps that had trainers previously". The final client's live maps
  carry one TRAINER marker per hub. The per-class markers ("Soldier/Specialist Trainer: Alia Das") survive only in the
  unused wargame template 2269, and the per-class marker texts 973–979 and 1081–1092 are used by no marker. Their
  pools go to 0/0 and every row stays, so Down only restores the counts.
- **Training Officer Stratton at Daghda's Urn.** TaRapedia lists him at Daghda's Urn in revision 35503 (2008-11-04,
  post-D12), and the client's names carry him as 10606, beside Kincaid's 10604. He stands on marker 987 "Class
  Trainer: Daghda's Urn". The marker's coordinates are original. That he stands on it is inferred: Kincaid is 0.6 m
  from marker 980. The navmesh path from the Daghda's Urn waypoint is complete. Nothing shows him, so his body
  (Kincaid's), level 8, 1000 hp and facing 0 are analogues (`GAP-DAGHDA-TRAINER-PRESENTATION`). His client package is
  unrecovered. The client needs none to train (`npc.CanTrain` reads only `CONVO_TYPE_TRAINING`), so
  `ClassAdvancement.TrainerCreatureIds` recognises him by creature id. `CreatureManager.ApplyPlacementNpc` gives his
  package-less placement an NPC record, and `IsClassTrainer` covers conversation, the Train status and the 20 m check.
- **Not placed: Twin Pillars, Foreas Base, New Cumbria.** No source names their single trainer. TaRapedia's Foreas Base
  and Cumbria pages list only the 2007 per-class casts, and the Twin Pillars page was last edited in 2007. The Ellatha
  NPC database and Ten Ton Hammer's guides name none. The client's unassigned "Training Officer" names (Lebowicz,
  Buckmaster, Delany, Walker, Howell) are candidates and are not used. Buckmaster and Delany share surnames with the
  pre-D12 trainers of Foreas Base and Cumbria (`GAP-HUB-TRAINER-IDENTITY-TWIN-PILLARS`, `-FOREAS-BASE`,
  `-NEW-CUMBRIA`). The later hubs' trainers are in `GAP-LATER-HUB-TRAINERS`.
- **Economy provenance.** The manifest filed `gameserver_dev_Full.sql` as `official_notes`, "the original game
  server's own schema and seed data". It is InfiniteRasa's emulator dump. The source is now kind `emulator_db`, a new
  kind that can support no original value. Under OD-96 the seven loot rows (creature_loot 1–21, 84 fields) are
  re-tiered original → analogue, and every item price from `Regenerate_item_template` is recorded as an analogue.
  That price rule (buy = loot_value, sell = floor(buy/4)+1) was fitted to the same dump, so it is circular
  (`GAP-ITEM-PRICES-EMULATOR-DERIVED`). `GAP-W1/W2-ITEM-PRICES` are reopened. Vendor stock has no source either
  (`GAP-VENDOR-STOCK-UNSOURCED`). No data changed.
- **Test Vendors: reviewed, one kept.** Pools 21–29 ("Test Vendor 1–9") have spawned nothing since the 2023 seed
  (counts 0/0), so no migration is needed for them. Pool 36 ("Test Vendor 5", package 10) is kept. It stands 0.15 m
  from the client's marker 971 "Weapons Vendor: Alia Das", package 10 is a WEAPONS vendor in the client's
  `vendordata`, and it is Alia Das' only weapons vendor. Its missing name, test body and seed stock are
  `GAP-ALIA-DAS-WEAPONS-VENDOR`. Everything it sells except the Laser Chaingun (4018) is also sold at Twin Pillars or
  by the ammo vendor. The hospital "Test Vendor 3" pools 31–35 are left to the hospitals batch
  (`GAP-WILDERNESS-HOSPITAL-TEST-VENDORS`).
- Tests: `ContentSchemaMigrationTests` (forward, exact rollback, re-apply), `SeedMigrationParityTests`,
  `ClassTrainerTests` (a package-less trainer converses, shows Train and trains at Daghda's Urn),
  `ClassTrainerEvidenceTests` (tree, gates, range, dialogue, trainers and marker against the evidence file) and
  `MissionContentLoadingTests` (Stratton live in shared 1220).

## 2026-09-26 — the deployed 2026-09-22..25 tree reconciled; its own narrative restored

The work deployed on banshee-ax41 from an uncommitted checkout (base `095fc45`) reached `development` earlier today as
`af0a9bb`, `234d703`, `9afd210`, `f30d9aa`, `4aa50b3` and `0ed7528`, recovered from the 2026-09-24 Docker image without
its documentation. A reconstruction of that narrative, written from the migrations' doc-comments and the evidence JSON,
stood here briefly. The checkout's own working tree has since been recovered (snapshot 2026-09-26), and a three-way
reconcile against `095fc45` restores the original text instead: the dated 2026-09-22 to 2026-09-25 sections above, the
2026-09-13 source-sweep entry (kept only in that checkout's stash), and the 36 audit documents they cite, among
them `character-name-evidence.md`, `bootcamp-equip-audit.md` and `bootcamp-opening-movement-audit.md`, which replace
their reconstructions. The reconcile also brings in 112 evidence files the image lacked, the live tree's later
corrections (the pad-hold wording in the manifest and `bootcamp-s5-retry-reinforcement-walk.json`, Conrad's corpse
state 44 relabelled `inferred`, creature 73's comment corrected to Armor Supplier Heffernan from the client's name
table, two Walkabout footage cross-references on 1390 and Apirka's Part Two offer), two tests
(`WildernessMissionOffersFollowTheHistoricalReceptiveAndApirkaChain`,
`PadHoldDataOperationsSpecifyTypesForUnmappedContentTables`), the `BootcampCoverExport` tool that generated the committed
cover geometry, and the deployment's compose fixes. No seeded value or tier changed except as listed.

Where the restored originals and today's work disagree, today's stands and is recorded:

- **Too Close For Comfort's level.** The live tree labelled 1407's level 4 `observed`; the manifest keeps it `inferred`,
  because its only source is a community wiki (TaRapedia revision 8563, 2007-09-02, unchanged to 2008-02-28) and the
  validator reserves `observed` for footage. Live's revision-precise citation and `too-close-for-comfort-level.json`
  are adopted.
- **The crate armour band** keeps its `OD-54, pending owner review` marker, which the original fidelity-audit wording
  dropped although the decision is still pending in both manifests.
- **Decision numbers.** The recovered work's owner-review items keep today's free numbers: Forming Alliances' 50%
  heart-drop chance and reward vest 13738 with its V04/V06 naming conflict (OD-61), the practice dummy's 1 hp (OD-58),
  the camp companion Initiates' stats (OD-62), McAllister's 2.5 m/s analogue walk speed (OD-57), Moawi's analogue
  dialogue class (OD-63), Solis's inferred cavern placement (OD-64) and the 20-second account-authentication wait
  (OD-65).
- **Manifest coverage** the reconstruction pass found missing still is: no rows or changes exist for
  `WildernessHubFormingAlliances`, `WildernessHubReceptiveGate`, `WildernessHubReceptiveLevel`,
  `WildernessHubConscientiousGate`, `WildernessHubConscientiousBranches`, `MissionItemDropChance`, `MoawiDialogueClass`,
  `MissionSpeakerDialogueClasses`, `SolisCavernsPlacement`, `BootcampCourtyardForeanWarrior`, `BootcampRifleMelee`,
  `BootcampConradCorpsePlacement` or `BootcampBombHullPlacement`. Recorded, not fixed.

Left out of the repository on purpose: the external DIT companion harness (`src/Rasa.Game/Dit/`, its tests and its
hooks), which is deployed as an overlay; the isolated-client automation scripts (`tools/client_focus_guard.py`,
`tools/client_waypoint_steering.py` and their tests), which several `bootcamp-s5-*` evidence files name as their method
and which stay with the snapshot; the live databases; and `global.json`, which pins SDK 8.0.129 and would stop the
.NET 5 SDK container this repository builds and tests in.

## 2026-09-26 — Client-contract defects: local teleporters, dropships, hospitals, sell and repair prices

The segment-3 systems audit (`research/20260926-segment3-audit`, entries SEG3-LOCAL-TELEPORTERS, -DROPSHIP-WINDOW,
-HOSPITALS and -BUYBACK-REPAIR) found four places where the server does not do what the 1.16.5.0 client expects.
Each fix below was re-read from the client's own bytecode or tables (static decoding only; nothing imported or run).

**Local teleporters.** `DynamicObjectProximityWorker` handled map waypoints, wormholes and dropships and let the
type-1 pads fall to its default case, so none of the 42 could ever be gained. The client treats them as their own
waypoint kind: `constant/waypointtype` has LOCALWAYPOINT 1, `manifestation.Recv_WaypointGained` posts
PM_GAINED_WAYPOINT for it, and `clientmethod.Recv_EnteredWaypoint` opens the travel window with the type. The pads are
now gained within the map-waypoint radius and open the window with this map's gained local pads; selecting one
teleports within the map with the LOCAL_TELEPORTER effect. The radius (2 m) is the server's existing waypoint radius,
inferred (`GAP-LOCAL-TELEPORTER-RADIUS`). Two things are still wrong and are recorded, not guessed: the pad ids 537-574
are emulator ids that the final client's `waypointlanguage` does not contain (its original keys end at 533), so their
names show as the client's missing-translation text (`GAP-LOCAL-TELEPORTER-IDS`, owner decision OD-90); and three
type-1 rows are not local teleporters by the client's map (`GAP-LOCAL-TELEPORTER-TYPES`). Two further type-1 rows,
595 and 597 "MIS_INDRACAVERNS_GRAVEYARD", stand 0.0 m from the client's "AFS Field Medic" HOSPITAL markers on maps 1823
and 1977 and from no LOCAL_TELEPORTER marker; `LocalTeleporterGraveyards` re-types them to the seed's hospital type so
walking through those hospitals gains nothing nameless.

**Dropships.** The dropship window listed every pad on every map from level 1 and drew each map's first pad at
(-225.353, 99.597, -70.5246), which is the world seed's position for Denzil's Caldera Outpost Hospital on the
boot-camp map. `waypointwindow.SetupWaypointLocationRows` places each location at the position sent with it, so each
pad now carries its own row's position. The discovery rule is in the final client's own help text (uielementlanguage
5697, "Waypoints/Dropships"): "Access to a Dropship Transport is gained by walking across the pad ... A special travel
menu will appear with a list of any available Dropship Transports. Remember, you must first travel to another map and
gain access to a Dropship Transport there before you can use this method of travel!" Walking onto a pad now gains it
(stored as type 4), the list holds the gained pads, and `SelectWaypoint` refuses a pad the character has not gained.
Characters on the live world keep the waypoints and hospitals they have; they gain each dropship pad the next time
they step on it. Not modelled: the hovering dropship the help text requires (`GAP-DROPSHIP-HOVER`) and any gain
message, since the client has no dropship waypoint type to announce (`GAP-DROPSHIP-GAIN-MESSAGE`).

**Hospitals.** The Palisades control-point hospital carried the Wilderness Landing Zone hospital's graveyard 136 and
waypoint 216. Gained waypoints are stored by id alone (`character_teleporter` key character_id, waypointId), so gaining
one gained both, and a player who had the Wilderness one got no zone-entry hospital on the Palisades. The Palisades
marker reuses text 303 ("Hospital: Landing Zone (Control Point)", also on templates 1759 and 1839), while every other
marker of that control point says Fort Dew: "Control Point: Fort Dew", "Waypoint: Fort Dew (Control Point)" 33 m away
and "Medical Vendor: Fort Dew (Control Point)" 5 m from the hospital. The row is now graveyard 221 and waypoint 226,
both "Hospital: Fort Dew (Control Point)" (inferred). Waypoint 226 is paired with 225 in the same way as the other
control points' waypoint and hospital ids, the world seed places 226 8.5 m from the marker, and `map_marker` already
bound the marker to 226.

Why Divide and Palisades offered too few hospitals: the 2026-09-17 coverage recipe accepted only exact
`graveyardlanguage` name matches. Three unresolved markers resolve on the next joins the client's data supports
(inferred): Divide's "Hospital: Foreas Base" (graveyard 202 and waypoint 93, both "Foreas Base Hospital", in the same
id blocks as the catalogued Divide hospitals 200/201/203 and 94/96/97), Palisades' Cumbria Research Facility Hospital
(219, the only Cumbria graveyard, with waypoint 112 1 m from the marker) and Devil's Den (41 "Devil's Den Entrance",
the only Devil's Den graveyard, with waypoint 388 "Hospital: Devil's Den"). Each respawns at its marker. The navmesh
check finds ground under each marker, including Cumbria's, where the seed row is 10.8 m lower. Palisades' Hightower
Outpost First Aid Station and Viands Village Hospital stay unresolved, because no graveyard or waypoint text names
either. The catalogue now has 105 hospitals on 42 maps (`docs/evidence/hospital-catalog.json`). Still open:
waypoint 120 "AFS Field Medic" is shared by five instance entrances (`GAP-HOSPITAL-SHARED-WAYPOINT`), and the Wilderness
LZ marker's `map_marker` row names 104 while the catalogue announces 216 (`GAP-WILDERNESS-LZ-HOSPITAL-MARKER`).

**Sell and repair prices.** `ItemInfo.BuyBackPrice` was never set, so the item-info tuple always carried 0. The client
uses that number twice. With a vendor open, `inventorywindow.OnSlotEntered` adds a price line that `tooltipwindow`
fills with stack count times `GetItemBuybackPrice`, and `vendorwindow._GetRepairPrice` bases the repair price on it.
Players therefore saw no sale price and a 1-credit repair on everything. Per unit it is what `RequestVendorSale` pays,
so it is now the sell price, clamped at 0 as the sale is. The repair charge was round((max - cur) × sell / 100); it is
now the client's price: max(int(int(buyback × (100 − condition) × 0.01) × REPAIR_GLOBAL_MODIFIER), 1), with
condition = 100 × cur // max (Python 2 integer division, `item.GetCondition`) and REPAIR_GLOBAL_MODIFIER 1.0
(`gameconstants`). The sell prices themselves keep their existing provenance, InfiniteRasa's loot_value fit
(`Regenerate_item_template`). Their tier is the trainers-economy batch's question, not this batch's. Items still never
lose durability (gameconstants DURABILITYMOD_*), so a repair is rare until wear exists.

Tests: `TravelPadTests` (local pad gain, window type and map filter, dropship gain, list and positions),
`VendorPriceRulesTests` (the client's expression, evaluated by hand for seven cases),
`InventorySessionTests.VendorRepairChargesTheClientsRepairPrice`,
`ItemRequirementLoadingTests.LoadedSellPriceIsTheBuybackPriceTheTooltipCarries`, three `PlayerDeathLifecycleTests`
(Fort Dew no longer shares 216, the only shared waypoint ids left are the recorded ones, and the three new hospitals are
gained and offered), the `LocalTeleporterGraveyards` block in `ContentSchemaMigrationTests` and its provider-parity
check. Manifest: five `changes` entries, the six gaps named above and OD-90.

## 2026-09-26 — Eleven Liaison Logos missions seeded (LiaisonLogosMissions)

`20260926190000_LiaisonLogosMissions` (SQLite and MySQL, frozen rows in `WildernessData/LiaisonLogosMissionsRows.cs`)
seeds the eleven `Logos:` missions the segment-3 audit found unseeded: 1633 Attack, 1634 Target, 1635 Here, 1638
Enhance, 1639 Power, 1640 Area, 1643 Backward, 1644 Defend, 1646 Give, 1647 Increase and 1652 Ground. They follow
`SeedLogosMissions`: one client objective each, required and revealed on acceptance, bound by `LogosRecovered` to
the world's `logos` row for the shrine; the giving Liaison also takes the mission back; no reward item, because the
shrine grants the Logos. None has an objectiveconversation row, so no giver line can close an objective.

Four findings changed the batch:

- **The list had four givers, not one.** The audit filed all eleven under Receptive Liaison Langerman. TaRapedia's
  mission pages (every revision up to the post-D11 ones of 2008-09-16/25) and its NPC pages give Langerman 1633,
  1638, 1639 and 1640 (Alia Das), Standley 1634 and 1635 (Twin Pillars), Noonan 1643, 1644, 1646 and 1647 (Foreas
  Base, Divide) and Arizpe 1652 (Cumbria Research Facility, Palisades). Each shrine stands on its giver's own map,
  and its `logos` row id is the client's own `logosstone` constant for the word (checked read-only against the
  deployed world). 1638's client opening text itself begins "I am Receptive Liaison Langerman". Langerman's other
  four missions (907 Damage, 909 Time, 911 Mind, 921 Projectile) and Standley's 908/912/923/924/960 are not in
  this batch (`GAP-LIAISON-LOGOS-UNSEEDED`).
- **The credit amounts conflict.** Every seeded amount is a TaRapedia reading first written in October–November
  2007. The pages' post-D11 revisions only reformatted them, so each is labelled era pre-1.4
  (`GAP-LIAISON-LOGOS-REWARD-ERA`). Ellatha's early-2008 mission pages give different credits for Attack (800 vs
  100), Target (900 vs 800), Enhance (500 vs 1,500), Power (500 vs 600) and Area (600 vs 1,500). Those credits
  are left out; only Here's 900, where both sources agree, is seeded. The Divide pages' 200 credits were entered
  at page creation by the editor whose creation-time credits on all six Liaison pages that can be checked were
  later corrected or contradicted, so they are left out as weak. Five pages record no experience. What is seeded:
  experience 4,000/2,500/2,500/3,000 for Langerman's four and 4,500 for each of Standley's, credits 900 for Here
  and 1,800 for Ground (single uncorroborated reading). The Noonan missions pay nothing
  (`GAP-LIAISON-LOGOS-REWARDS-MISSING`, which also flags the same 200 credits already seeded on 1649/1650).
- **Langerman cannot be spoken to in this tree alone.** He stands on the Redshirt class 29423. The missing-npcs
  batch's `MissingMissionGivers` (20260926150000) gives him an NPC class for 1741. This migration leaves creature
  133 alone and depends on that one running first (`GAP-LIAISON-LOGOS-SPEAKER`).
- **Levels follow the SeedLogosMissions rule.** Each mission takes its giver's level as an analogue under OD-100:
  Langerman 15, as the final-era footage shows his nameplate (B3-024), although his creature row says 10; Standley
  and Noonan 10, Arizpe 25.

1639 and 1640 are gated on 1069 Receptive Reception, TaRapedia's Requirement since 2008-03-07. Whether 1639 Logos:
Power was still dispensed after D11, when the rebuilt boot camp grants Power on the normal path, is unrecorded
(`GAP-LIAISON-LOGOS-POWER-D11`). A character who skips the boot camp still arrives without Power, and no capture
shows what the skip granted (`GAP-LOGOS-SKIP-POWER`). Nothing is granted in its place.

Provenance: the manifest rows with migration `LiaisonLogosMissions` (43 rows, 11 changes, 6 new gaps, OD-100) and
`docs/evidence/liaison-logos-missions.json`. The research is in `research/20260926-logos-missions`: TaRapedia and
Ellatha fetches, the static client decode, and the world check.

## 2026-09-26 — Kill experience: squad share, danger penalty, crit kills

The segment-3 audit (`research/20260926-segment3-audit`, SEG3-KILL-XP-MODIFIERS) found kills paying the same at any
player level and only to the killer. The 1.16.5.0 client was re-read statically for every use of the kill constants.
`XP_MOD_PER_PARTY_MEMBER` is the only one it reads: `experiencebarwindow.OnXPEntered` (lines 220-223) computes the
squad share for the XP-bar tooltip. The `DANGER_PENALTY_*`, `KC_*_PLACE_MOD` and `MIN_DISTANCE_FOR_KILL_CREDIT`
constants appear nowhere but `gameconstants`, so how the server used them is not in the client. Code only; no
migration. `KillRewardRules`, `KillRewardManager`, `XPInfo`:

| rule | now | evidence | tier |
| --- | --- | --- | --- |
| squad share | each member gets `100 - 8(n-1)`% of the solo experience; baseGained is the even split, groupMod `n x share / 100` (sent at full precision, so the chat line reads +84/+152/+204/+240/+260% for 2-6) | the client's tooltip arithmetic; Recv_ExperienceChanged prints "[base Base XP] (+N% Group Bonus)"; TaRapedia Experience rev 34776 (2008-10-06) agrees except its three-member +154% | original |
| who shares | the killer, and squadmates on the channel within 100 m of the creature; n counts every member in the world, as the tooltip does; squadmates get no streak | TaRapedia "so long as you are in range when it is killed"; `MIN_DISTANCE_FOR_KILL_CREDIT 100` used from its name (OD-106) | inferred |
| danger penalty | full to 5 levels above the creature, then 80/60/40/20% at 6-9 above, nothing from 10; each recipient's own level; a 0 line is not sent | `DANGER_PENALTY_VALUE 0.2`, `_LEVELDIFF_MIN 5`, `_MAX 9` (original); ramp shaped like the same module's `LEVELDIFF_DAMAGEMOD`; TaRapedia's zero at 10+; B3-047 full at one level above (OD-105) | inferred |
| crit kill | a wasCritKill chunk equal to the plain kill, then the plain kill, both counting toward the streak; one credit line | B1-028 (final week) "66 ... by Crit Killing / 66 / 5 credits"; TaRapedia "counts as two kills in a chain" (OD-107) | observed |

Nothing raises a crit kill yet. Overkill, the pre-death effect and `RequestCritDeathFinish` are not built
(`GAP-CRIT-DEATH-FINISH`). The modifiers truncate in the order streak, group, danger. The solo fit fixes only the first
step (`GAP-XP-MODIFIER-ORDER`). Solo kills within five levels pay exactly as before: all nine footage observations still
reproduce, and the five whose player level is on screen also pass through the new path.

**B3-060 re-read.** The audit took "[101 Base XP]" with 40 credits at player level 10 as a level-difference reduction.
Every final-week reward prints its experience line before its credit line (A3-081, B1-018, B1-028), so the 40 credits
belong to the cut-off line above it. The 101 line is a level-7 base halved after a +100% streak
(floor(1017 x 2 x 0.5 / 10) = 101), three levels below the danger minimum. Together with the plain 17/33/35 lines of B1
(a quarter or a half of the level 1/2 base), it fits the client's damage-ranked `KC_1ST/2ND/3RD_PLACE_MOD`
1.0/0.5/0.25. That partial credit stays unbuilt: the ranking is not in the client, and creatures keep no per-attacker
damage (`GAP-XP-PARTIAL`).

Also open: `GAP-XP-DANGER-SHAPE` (no final-era kill five or more levels above), `GAP-XP-SQUAD-RANGE`,
`GAP-XP-SQUAD-STREAK` (`STREAK_BASE_PER_PARTY_MEMBER` suggests a squad streak) and `GAP-KILL-CREDITS-MODIFIERS` (credits
stay the killer's). Provenance: `docs/evidence/kill-rewards.json` (rules `xp.group`, `xp.group.recipients`, `xp.danger`,
`xp.crit`, `xp.modifier_order`, each with client file hash, source line and bytecode offset) and the manifest's changes,
gaps and OD-105-107.

## 2026-09-26 — Clone credits: the token's use, where credits come from, and what a clone keeps

The segment-3 audit (`research/20260926-segment3-audit`, SEG3-CLONE-CREDIT-SOURCES and SEG3-CLONE-COPY-RULES) found
the Clone Credit item inert and the clone's copy list partly contradicted. Evidence: the 1.16.5.0 client decoded
statically, TaRapedia's revision histories and four Wayback captures of the official site, all in
`research/20260926-clone-credits` (hashes in `work/SHA256SUMS`). The rules are in
`docs/evidence/class-trainer-evidence.json` (`CT-CLONE`, `CT-CLONE-GATE-CREDIT`, `CT-CLONE-POSITION`,
`CT-CLONE-TOKEN`, `CT-CLONE-SELECTION`, and `clone_credit_sources`). Migration `20260926220000_CloneCreditNotTradable`.
Decisions OD-115 and OD-116, agent-approved and pending owner review.

- **Using a Clone Credit (opcode 706).** `clonecredit.pyo` gives the item a right-click Use that sends
  `RequestUseCloneCredit(entityId)`. Nothing handled it. The client expects only the new count back:
  `manifestation.Recv_CloneCredits` shows PM 955 "Your cloning credits have increased." and the Clone Credit
  tutorial when the count rises. TaRapedia's Clone Credit page (rev 31080, 2008-06-03, text unchanged at its last
  revision, 2008-10-16) says one token gives one credit: "Right-click on the clone credit token ... to enable a clone
  credit on your character." `ManifestationManager.RequestUseCloneCredit` now checks the item's class carries the
  clonecredit augmentation (70). It spends one token from the character's backpack through the atomic stack
  consumption, adds the credit, saves it and sends `CloneCredits`. The client's footlocker and clan-lockbox windows
  can send the same request. No source shows the live server accepting that, so it is ignored (OD-115,
  `GAP-CLONE-TOKEN-LOCKBOX-USE`). The client has no failure message, so a refused request gets no reply.
- **The token cannot be traded.** The same TaRapedia text: "They are, at the moment, not able to be traded." Template
  111219's `not_tradable_flag` was `Regenerate_item_template`'s uniform 0. It is now 1 (observed). Its other flags and
  prices remain placeholders (`GAP-CLONE-TOKEN-FLAGS`). Name, stack of 10 and class come from the client's tables.
- **The selection screen already shows credits.** The pod reads `CharacterData[8]` and enables Clone only with
  credits above zero and a free pod (`characterselectionwindow.pyo` lines 597, 614). The server already sends
  that field, and after a clone it re-sends both pods. Unknown: whether live used `CloneCreditsChanged` (705) for
  the source pod instead; both set the same field.
- **Where credits come from.** The tier gate (one at 4, 14 and 29) is live. Every other dated source is a mission
  reward. Wilderness Targets of Opportunity 1449 (Lt. Col. Cimoch, Alia Das) pays a Clone Credit with the title
  Master of Wilderness (TaRapedia rev 35297, 2008-10-21, unchanged since 2007-10-26). The same is true of the
  Divide, Palisades and Plains ToOs and of the three hybrid missions 1861, 1851 and 1899. None of these missions is
  seeded. A reward row for a mission the server does not define would be dangling, so none is written (OD-116). Each
  reward is recorded for when its mission is seeded (`GAP-WILDERNESS-TOO-CLONE-CREDIT`,
  `GAP-CLONE-TOKEN-LATER-SOURCES`). No final-era vendor sold clone credits. A 2007 promise of one never shows up
  again, and the late-2008 Prestige vendors sell respec tokens and boosters (`GAP-CLONE-CREDIT-VENDOR`). The
  2008-01-31 grant to every character was a one-off compensation and is not final state.
- **What a clone keeps: the conflict dissolves.** The audit set the "2009-01-25 Beginners Guide" (completed missions
  carry) against IGN ("wipes the quest log"). Every source that separates the two agrees: a clone keeps its source's
  completed missions and loses the open ones in its log. TaRapedia's Cloning page says so (rev 16906, 2007-11-28:
  "no active missions in the mission log"). So does its Beginners Guide (rev 35313, 2008-10-23: "Quests completed"
  persist). This is the page the 2009-01-25 citation meant; its last revision before shutdown is 2008-10-23. The
  official 1.4 notes (2008-01-29) have a clone's Targets of Opportunity auto-complete "non-repeatable objectives"
  its source had completed, which needs the clone to know those missions. The official site's "Your Clone and You"
  lists "Missions" as reset, but also says to finish missions "you do not want to repeat" before cloning. Its text
  is the same from 2007-12-11 to 2009-01-09, which also explains its stale "Attributes" not reset. The
  implementation already copies completed missions and not active ones, so nothing changed.
- **Arrival and the clone's own credit.** The same guide keeps "Location upon cloning" ("if you clone in a hostile area
  that is where your naked clone is created"). The clone already starts at its source's position (observed,
  `CT-CLONE-POSITION`). A clone has no credit of its own. It earns the gate's credit only if it was made before its
  source reached the gate ("clone at level 14.9, not at 14.9999999", TaRapedia Cloning; Beginners Guide 2008-10-23).
  `ApplyLevelUps` already grants the credit only when the experience crosses the threshold, and a test now pins
  that (`CT-CLONE-GATE-CREDIT`).
- Open: `GAP-CLONE-TOO-AUTOCOMPLETE` (the 1.4 ToO rule waits for the ToOs) and `GAP-CLONE-SOCIAL-STATE` (friends and
  ignores are account-wide here; early sources reset them "for now"; clan and titles unsourced).
- Tests: `CloneCreditUseTests` (decode through the registered handler, one token per credit, the last token leaves
  the backpack, footlocker, foreign and non-token items refused, the evidence and manifest checks),
  `ClassTrainerTests.AClonesOwnTierCreditDependsOnWhetherItWasMadeBeforeTheGate`, `ContentSchemaMigrationTests`
  (forward, rollback, re-apply) and `SeedMigrationParityTests`.

## 2026-09-26 — creature loot counted from the footage (`CreatureLootFootage`)

Migration `20260926210000_CreatureLootFootage` (rows in `CreatureLootFootageRows.cs`). Evidence:
`docs/evidence/creature-loot-footage-ledger.json`, the ledger of every creature-loot drop visible in the supplied
footage. Audit item SEG3-CREATURE-LOOT (`research/20260926-segment3-audit`). Decisions OD-110 to OD-112, all
agent-approved and pending owner review.

- **The ledger.** 40 drops in the final-week session (7Lrst9SG3pk, 8VXeKzGUv0c, Ycxm8Pa1-v4), taken from the
  frame-by-frame transcripts of 2026-09-13 and their verification pass. The A4 and C1 copies are byte-identical to the
  committed transcripts. There are also 32 squad-loot entries from the January 2009 Pravus run (A4udsM0rcLo, read from
  its chat at 1/4 fps), and 20 kill windows. A kill counts only if it has a "You received N credits" line. Chat that
  appears across an edit cut is its own window, with its kills and its drops both counted. The final-week drops are
  25 Thrax Skull, 5 standard-grade ammunition stacks, 2 Boargar Ear, 2 schematics, 1534 Mimeomech, Wellcare Motor
  Assist Armor Legs, an experimental AccuMax Shotgun, the player-named "Xray2ia's Laser Cannon", 5 snowballs and a
  Forming Alliances Thrax Heart.
- **Thrax Skull on the Initiates.** 22 skulls in 42 credited Thrax Infantry Initiate kills: 52.38% (measured, Wilson
  95% 37.7–66.6%), always 1. This is a lower bound of the per-corpse chance, because a corpse never opened counts as a
  kill without a skull. Three of the four corpse windows that show their whole contents hold a skull. Template 41666
  (Loot_Junk_Thrax_Skull 20307), not the test class 25553 of the same name.
- **Ammunition does not follow the killer's weapon.** The audit expected weapon-matched ammunition. Of the seven drops
  whose receiver's weapon is known, five are power cells or rockets. They dropped for a Shinobi Rifle, an AccuMax
  Shotgun and a Teleract Rifle, and all three fire Standard Grade Cartridges (their own tooltips). In A5 the rifle's
  reserve did not grow by the 117 cells. The 19 ammunition drops cover all five of the client's standard-grade weapon
  ammunition classes: cartridges 5, power cells 4, rockets 5, canister 2, pharmaceuticals 3. Each Initiate
  therefore gets five rows. The total rate, 4 in 42 kills (9.52%), is measured. Splitting it evenly, 1.9% a type, is
  inferred (OD-111). Stack ranges are measured per type (cartridges 117–249, power cells 80–159, rockets 33–47,
  canister 48–55, pharmaceuticals 105–281). The Pravus stacks come from level 8–12 kills, and the Initiate's own four
  stacks fall inside those ranges.
- **Thrax stand-in and Young Forest Boargar (OD-112).** The Wilderness Thrax on camera are Thrax Infantry Trainees, a
  creature this world does not seed (3 skulls and 1 rockets stack in 4 kills). Creature 3, the world's Wilderness Thrax
  stand-in, takes the Initiate's rows (inferred). The Young Forest Boargar (44) gives Boargar Ear in both of its two
  kills (100% measured, Wilson 34.2–100%, stack 1–2).
- **Removed.** creature_loot 1, 8 and 15 were the emulator's 12% 1–35 cartridges on the same three creatures. No
  observed stack is under 33, and the type varies. The emulator's Motor Assist (0.5% each) and med-pack (5%) rows stay
  as OD-96 analogues. One Motor Assist piece and no med pack in 42 kills neither confirms nor contradicts them.
- **The stand-in drop (OD-110).** Every creature without rows got three cartridges on a coin flip. The drop was
  unlabelled, and the old gap called it "closer to [the original] than dropping nothing". It now lives in
  `CreatureLoot.StandInDrop` and is labelled an analogue. It stays on, because switching it off
  (`StandInDropEnabled = false`) leaves every creature the evidence does not cover dropping nothing but mission items.
  The owner chooses. Creatures with rows never reach it.
- **Not seeded.** Schematics, random gear, crafting resources, seasonal items and the final patch's red gear are seen
  but not counted (`GAP-LOOT-SCHEMATICS`, `-RANDOM-GEAR`, `-RESOURCES`, `-SEASONAL`). Higher ammunition grades never
  appear (`GAP-LOOT-AMMO-GRADE`). Squad loot distribution with need and greed rolls is a mechanic of its own
  (`GAP-LOOT-SQUAD-DISTRIBUTION`). The junk templates' quality 2 against the client's JUNK is open
  (`GAP-LOOT-JUNK-QUALITY`), and so are the sample sizes (`GAP-LOOT-RATE-SAMPLES`). The final-week video files are not
  on this machine, so F07's partial "11[7]" cannot be re-read (`GAP-LOOT-FINAL-WEEK-VIDEO-COPIES`).

Provenance: manifest rows with migration `CreatureLootFootage` (19 rows, 4 changes, 10 new gaps, `GAP-CREATURE-LOOT`
narrowed). `CreatureLootFootageTests` recomputes every seeded rate and stack range from the ledger's own drops and
kill windows.

## 2026-09-26 UTC — Deploy: development dae1a33 on banshee-ax41

The live server moved from the OVH VPS to banshee-ax41 (65.109.31.181) on 2026-09-25. Its checkout
(`~/servers/rasa-net`, at 095fc45 plus 595 uncommitted changes) was reconciled into development first (commits
0123ee2..dae1a33), then replaced by development dae1a33 at 16:43 UTC.

- **Backups** (on ax41, `~/backups/rasa-net/predeploy-20260926T160619Z/`): the three SQLite databases, both compose
  files and appsettings, a tarball of the whole previous working tree, the git bundle deployed, and the DIT overlay.
  Previous images are tagged `rasa_net_game:rollback-20260926` (726f4dd2c684) and `rasa_net_auth:rollback-20260926`
  (92958edb3445). Rollback = restore the three databases, re-pin `docker-compose.dit.yml` to the rollback tag, and
  bring auth and game back up.
- **Images**: `rasa_net_auth` 94b28b5aa862 and the base `rasa_net_game:latest` c9ae30deeebd were built from the tree
  without DIT; `rasa_net_game:dit-20260926` 58fc6f122287 from the same tree with the uncommitted DIT overlay (the
  `Dit/` directory, `DitBotTests.cs` and `dit-overlay.patch`, kept outside the repository), as the AGENTS.md DIT
  section requires. `docker-compose.dit.yml` now pins `dit-20260926`; Game was relaunched with both compose files.
- **Startup**: the world database applied everything through `20260926220000_CloneCreditNotTradable` (150
  migrations; the char database 16, through `ItemInstanceMetadata`); 131 `npc_mission` rows; 16 content rules, 430
  rows, 0 gaps; navmeshes for 76 of 78 maps; Game authenticated with Auth advertising 65.109.31.181; ports 2106, 2116,
  8102 and 8001 listening; `dit/status.json` enabled and fresh, `/app/dit` mounted.
- **Logged at load, not new to this deploy's evidence**: missions 321 (objective flags unknown), 955 and 969 ("required
  objective … is never revealed") are withheld as incomplete. 955/969 are Mires missions seeded before 2026-09-22; why
  their required objective is never revealed is open.
- **Not yet verified**: no client has logged in to the new build.

## 2026-09-26 — supplied footage ledger: final night, Eloh Vale and pre-D11 Foreas

Every timestamp below is from `docs/evidence/gameplay-footage-supplied-20260926.json` or
`docs/evidence/footage-fanout-20260926/`. Missing evidence is not 100% retail accuracy. `launch-era` and `pre-d11`
rows are boundaries and are not final-live rules; they predate the Deployment 11 boot camp/content rebuild and must
not seed it. `final-night`, `final-week` and `cinematic-only` rows are not boundaries: they are primary evidence of
actual final-live behaviour (or, for `cinematic-only`, carry no mechanics to disclaim), not a pre-final-live state.

- cite:fxAtDpxypSw@5 gap: pre-t=5 admin/Neph broadcast text not captured. Concordia Divide mission tracker shows Wilderness Targets of Opportunity (client mission 1449); chat is already reacting to a server-wide ADMIN/Neph message that never appears on screen.
- cite:fxAtDpxypSw@56 GM Vagabond narrates the Neph broadcast in `[1. General]`, confirming a live GM account active during the final night.
- cite:fxAtDpxypSw@80 Player claim that Earth 2's Empire Sector was re-taken shortly before shutdown; not a server message, and not proof of the outcome in every numbered Earth instance.
- cite:fxAtDpxypSw@106 gap: a running player joke about fireworks/an explosion at shutdown. No fireworks, explosion or shockwave is visible in the recording. Do not implement fireworks or an explosion as a shutdown effect.
- cite:fxAtDpxypSw@122 First shutdown broadcast, yellow text, no channel prefix: "ADMIN MESSAGE: Server Shutting Down in 10."
- cite:fxAtDpxypSw@156 Final countdown broadcast "ADMIN MESSAGE: 1"; the 10-to-1 cadence is irregular (about 3-7 s per step, mean 3.8 s +/-1 s), not one number per second.
- cite:fxAtDpxypSw@159.1 Disconnect at t=159.1 (+/-0.1 s): modal dialog "You have been disconnected from the server" / Ok. World stays rendered behind the dialog; no attack, explosion, fireworks or fade before the drop.
- cite:fxAtDpxypSw@176 Server selection list, all four servers OFFLINE (Cassiopeia, Centaurus, Hydra, Orion), refreshed and still OFFLINE. Post-shutdown state only; do not seed it as a live server-list condition.
- cite:fxAtDpxypSw@144.5 measured: cross-check of the admin countdown against a bystander's own closure timer; both converge on zero at t~144-145, +/-1 s.
- cite:_gwh1__XecI@24 GM-directed pre-shutdown gathering at Foreas Base (map 1148, Divide); the GM's own broadcast line is not itself on screen.
- cite:_gwh1__XecI@76 Fighting at Hydro Plant Outpost (CP), target frame "Episch: 1 / Jager" (Hunter); players level 23-50. No capture-rule mechanics are shown.
- cite:_gwh1__XecI@116 ADMIN broadcast, German client: "ADMIN-NACHRICHT: ALERT: PLATEAU IS LOST!" — the German prefix wraps an English payload, proving the payload is server-sent free text, not a client string.
- cite:_gwh1__XecI@98 gap: player chat claims "you MUST hold all FOUR CPS" or get kicked from Plateau. This capture-rule wording is a player claim, not observed mechanics; do not seed it as a confirmed rule.
- cite:_gwh1__XecI@202 Fighting at Dybukkar Forward Camp then Charon's Crossing; target frames include "50 Neph Waven" (level 50, Neph portrait, seven player-style buff icons) — the Neph-led offensive.
- cite:_gwh1__XecI@300 Arrival in Empire Sector (minimap "Madison Square Park"), red Bane-infested ground and spires — the Earth Last Stand phase begins. The specific numbered instance/context (2375) is inferred from the client table, not read on screen.
- cite:_gwh1__XecI@310 gap: "[1. Allgemein] Stauffer: for some reason I was auto promoted to level 50 / well all my alts were anyway" — a player claim of automatic level-50 promotion. Unverified against any official or client evidence. Do not implement auto-promotion to level 50.
- cite:_gwh1__XecI@425 Empire Sector street battle at a sandbag line (minimap "New York City"): Bane Stalker-type walkers, Thrax riflemen, a ballistic mini-turret, many level-50 players.
- cite:_gwh1__XecI@35 gap: a large armoured bipedal figure stands among posing players during the pre-shutdown gathering; chat includes "mechs for everyone" / "where is freemech?". Identity unconfirmed; D16.4 notes confine mechs to Edmund, so this conflicts with that and remains unresolved and low confidence. Do not implement player mechs outside Edmund from this footage alone.
- cite:_gwh1__XecI@502 A.F.S. Outpost E34's red Bane spire and force fields turn white/blue by t=515 — the AFS retakes the outpost, consistent with mission text 22231 "Regain Control of A.F.S. Outpost E34". gap: the tracker-entry link to id 22231 is inferred; no tracker entry for it is shown on screen.
- cite:_gwh1__XecI@521 "[1. Allgemein] Twinsen: 0 hours, 00 minutes, 47 seconds until Tabula Rasa EU server closes" — a player's own clock, not a server message; cross-confirms the countdown seen independently in fxAtDpxypSw.
- cite:_gwh1__XecI@546 "ADMIN-NACHRICHT: Server Shutting Down in 10..." — the same wording as fxAtDpxypSw's English client, from a second independent viewpoint.
- cite:_gwh1__XecI@584.9 Disconnect at t=584.9 (+/-0.1 s): German dialog "Ihre Verbindung zum Server wurde getrennt." / Ok — the same client string as fxAtDpxypSw's English disconnect dialog, a different client language. No explosion, fireworks, cinematic or fade before the drop.
- cite:_gwh1__XecI@584.9 measured: countdown cadence from continuous footage (521.0-588.3 s) agrees within 1 s with fxAtDpxypSw's independently sampled cadence; both show about 3-7 s per step, not 1 s per step.
- cite:j_4B22Y8z28@455 "U-Bahn-Station: 28th Street" (28th Street Station) with capture-point brackets — part of the Earth Last Stand's Empire Sector fighting.
- cite:CUnkvStC93o@134 Stacked kill-XP bonuses ("[659 Base XP] [+84% Group Bonus] [+100% Kill Streak Bonus] [+2000% Booster Bonus]") show the bonuses multiply (1.84 x 2 x 21 = 77.28, matching the observed total/base ratio), they do not add. Recorded 2009-02-20, 8 days before the shutdown night: final-week corroboration, not shutdown-sequence evidence. gap: the +2000% booster is plausibly the D16.4 Hyper-EXP token, unverified.
- cite:P40g1AEuLlY@0 The whole video is the game's opening cinematic; no gameplay and no shutdown content of any kind. Kept only to correct its earlier misattribution as CommanderGrog shutdown footage.
- cite:ZevYcdI1y8I@0 boundary: launch-era. not a final-live rule. gap: a squad runs the obsolete Eloh Bridge/Eloh Shield Gate/Eloh Cave/Eloh Sanctuary region (`maptemplate.pyo[1555]` = `adv_foreas_concordia_divide_elohvale`; `gamecontextlanguage.pyo[1398]` = "Eloh Vale (OLD)", retired from the final client's `gamecontext.pyo`). Must not seed the D11-rebuilt Eloh Vale (context 2084).
- cite:ZevYcdI1y8I@159.5 boundary: launch-era. not a final-live rule. "/rave" in Squad chat produces a byte-identical match (plus trailing space) to the final 1.16.5.0 client's `playermessagelanguage` 976, confirming this emote's text survived unchanged from launch to final.
- cite:ZevYcdI1y8I@202 boundary: launch-era. not a final-live rule. gap: death by whirlpool shows the Hospital Selection window listing raw, untranslated graveyard names "gy_eloh_vale"/"gy_pyramid"; the final client has no such rows, and the rebuilt Eloh Vale (context 2084) instead has graveyards 279 "Hospital: Forean Pyramid" and 280 "Forward Recon Medic". This confirms the obsolete map's graveyard naming was replaced, not what the rebuilt map's own spawn/reward layout should be.
- cite:ikIRxsE9HhU@0.1 boundary: pre-d11. not a final-live rule. Boot camp login runs "Basic Training 101" (+2500 XP) into "Obstruction Destruction", recorded 2008-08-08..12, three days before Deployment 11 (2008-08-15). This is the pre-rebuild boot camp; must not seed D11 missions 1990-1995.
- cite:ikIRxsE9HhU@222 boundary: pre-d11. not a final-live rule. A level-3 kill ("[76 Base XP] [+100% Kill Streak Bonus]", 15 credits) exactly fits the final-era kill-rewards.json formula for creature level 3, corroborating that formula already held before D11.
- cite:ikIRxsE9HhU@48 boundary: pre-d11. not a final-live rule. A level-17 kill shows the first observed +200% kill-streak bonus, with "You received 1 prestige points for reaching max kill streak" printed before the +200% XP line — supports kill-rewards.json's streak clamp and message ordering. gap: an immediate plain 107-XP kill right after is unexplained.
- cite:ikIRxsE9HhU@25 boundary: pre-d11. not a final-live rule. gap: level 17-19 kills in Concordia Divide give XP/credit combinations that do not fit the final-era xp.base formula, which is fitted only for creature levels 1-9 (`GAP-XP-PREDX11-LEVELS`). This neither supports nor refutes the final-era extrapolation above level 9.
- cite:ikIRxsE9HhU@186 boundary: pre-d11. not a final-live rule. Tracker shows "Boargar Acquisition" (751) active. This mission was withdrawn by the Deployment 11 live notes; must not be offered in the final client.
- cite:ikIRxsE9HhU@98 boundary: pre-d11. not a final-live rule. Squad fights "Predator" and "Juggernaut" targets in the Pravus Research Facility. D11 live notes removed Predator spawns; the final client seeds no Predator/Juggernaut in Wilderness, CLRF or Pravus Research. Must never be used to seed those spawns.
- cite:ikIRxsE9HhU@213 boundary: pre-d11. not a final-live rule. gap: a two-member squad kill ("[78 Base XP] (+84% Group Bonus) [+100% Kill Streak Bonus]" = 280 XP) confirms the group-bonus display format and message order, but no combination of the repo's fitted base and documented rounding order reproduces 280 for n=2 at streak x2 (`GAP-XP-MODIFIER-ORDER`, `GAP-XP-SQUAD-BONUS-VALUE`). Pre-D11, so this does not overturn the client-derived rule; left open pending final-era squad footage.

Corrections carried into `docs/source-sweep-2026-09-13.md`: `P40g1AEuLlY` (2010 opening-cinematic upload by EnciclopediaLusa) and `CUnkvStC93o` ("Goodbye Tabula Rasa", 2009-02-20, final-week corroboration) are not CommanderGrog shutdown footage; `_gwh1__XecI` is CommanderGrog's verified 2021 re-upload of `j_4B22Y8z28` in better quality; `xXerhmFdqqE` and `U0fdU3bkNBo` are added as unreviewed leads.

Provenance: `docs/evidence/gameplay-footage-supplied-20260926.json` (7 videos, private-copy SHA-256s, era verdicts,
boundary labels) and `docs/evidence/footage-fanout-20260926/{fxAtDpxypSw,j_4B22Y8z28,ZevYcdI1y8I,ikIRxsE9HhU}.json`
(the second file carries `_gwh1__XecI`, `j_4B22Y8z28`, `CUnkvStC93o` and `P40g1AEuLlY` together, following the
2026-09-25 file's convention of grouping observations by research folder rather than one file per video id).
`FootageLedgerSupplied20260926Tests` recomputes every `cite:` string and boundary flag from these files and asserts
each appears in this section.

## 2026-09-26 — The shutdown broadcast: admin messages, the countdown and the disconnect (segment 7)

Two independent recordings of the EU server's last minute were studied on 2026-09-26: fxAtDpxypSw (English client,
480p) and CommanderGrog's _gwh1__XecI (German client, the 1080p re-render of j_4B22Y8z28). Both show the same
sequence: an admin countdown and then every client's disconnect dialog over the still-rendered world. Research is in
`research/20260926-final-minutes/` and `research/20260926-shutdown-event-grog/`. The client was decoded statically
from the 1.16.5.0 bytecode. Code only; no migration. Provenance: `docs/evidence/shutdown-broadcast.json`. Decisions
OD-120 to OD-124, agent-approved and pending owner review.

- **The admin message is a client path, and only its text is the server's.** `communicator.Recv_AdminMessage(msg,
  filterId)` (method 24, `communicator.pyo` line 1046) filters the text and prints uielementlanguage 4146
  `ID_CHAT_MESSAGE_HEADER_GM` in front of it. That is "ADMIN MESSAGE: " in English and "ADMIN-NACHRICHT: " in
  German. The German footage shows "ADMIN-NACHRICHT: ALERT: PLATEAU IS LOST!", a localized header before an English
  payload (observed). The emulator already had an `AdminMessagePacket` with the right shape, a unicode string and an
  int, but nothing sent it. The filter is the server's argument. SYSTEM_GM (10000046) is chosen (inferred): it is the
  only filter the client colours `Chat_Yellow` by default, and the default General tab subscribes to it
  (`clientmessagesettings` lines 100 and 185). The footage's admin lines are yellow in the General tab, next to the
  white SYSTEM_GENERAL line "You may not issue a command at this time."
- **The final client cannot send one.** `SendAdminChat` resolves `chatIDToMethodName[SYSTEM]`, which is `None`,
  nothing calls it, and `slashcommand.pyo` has no admin command. Live staff used a tool outside the player client.
  In its place (`GAP-SHUTDOWN-ADMIN-TOOL`, OD-120): console `announce <text>` and `announcemap <mapContextId> <text>`,
  and in game `.announce <text>` and `.announcemap <text>` at GameMaster. The chat commands send the text exactly as
  typed, and a Player- or Observer-level account gets the usual refusal. Recipients are the clients in a live map
  channel (OD-124). `AdminBroadcastManager`.
- **The countdown** (console `shutdown start`, `ShutdownCountdown`, OD-121). It sends "Server Shutting Down in 10..."
  and then the bare numbers 9 to 1, as observed. The wording of the first line is the 1080p reading; the 480p copy
  leaves the trailing dots unresolved. The default cadence is fxAtDpxypSw's continuous take, measured ±1 s per line:
  lines at 0, 6, 10, 13, 16, 19, 23, 27, 31 and 34 s, and the disconnect at 37.1 s (fx t = 122 … 156 and 159.1).
  _gwh1__XecI agrees within the two readings' sampling error for every line it kept (10, 9, 8, 7, 6, 4, 2, and its
  drop at 38.9 s). The steps are 3–7 s, not 1 s, and read like typing (`GAP-SHUTDOWN-CADENCE-ORIGIN`), so the
  measured night is the default (OD-123). `shutdown start <from> <seconds>` gives a uniform countdown instead;
  `cancel` and `status` exist. By default the process then stops as `exit` does, once every disconnected player has
  been removed and saved (30 s at most). The footage shows all four servers OFFLINE afterwards. `stay` keeps it
  running (OD-122). Nothing else starts the countdown: no timer, date, player count or chat command.
- **The disconnect dialog is the client's reaction to a close it did not ask for.** When the connection ends, the
  engine calls the input state's `GameOnDisconnect`. From the game state that is `exitgame.GameOnDisconnect` (line
  109). Unless the player logged out (`g_requestedRestart`, set only in `OnLogout`), it runs
  `inputhandlers.OnDisconnect`: a modal "Disconnected" box with uielement 9, "You have been disconnected from the
  server" / "Ihre Verbindung zum Server wurde getrennt.", and Ok back to login. It then calls `ClearChatInfo`, which
  is the blank chat in both videos. Character selection takes the same branch. The countdown's disconnect therefore
  sends nothing and just closes every connection: `Client.Close` shuts the socket down in order.
- **Recorded, not built.** The Neph/"bold statement" broadcast before the EU countdown, whose text is not in the
  footage (`GAP-SHUTDOWN-NEPH-BROADCAST`). The admin action hidden by Grog's edit cut at t≈447, after which players
  say "admin = neph" (`GAP-SHUTDOWN-GROG-CUT-ADMIN-ACTION`). What triggered the Plateau alert: players claimed
  "hold all FOUR CPS", which is chat, not evidence. The text can be sent by hand, and no zone-loss rule exists
  (`GAP-SHUTDOWN-ZONE-LOSS-RULE`). The Earth Last Stand: the client ships context 2375 "Empire Sector: The Last
  Stand", but no artifact read so far has its spawns, scripting, route or rewards (`GAP-SHUTDOWN-LAST-STAND`).
- Tests: `ShutdownBroadcastTests`. They cover the packet's wire shape (2-tuple, unicode text, SYSTEM_GM, no header
  in the payload), broadcast recipients, command gating (`.announce` at GameMaster; no `.shutdown` chat command),
  the measured cadence tick by tick, disconnect of every client only at 37.1 s and once, cancel, the uniform
  variant and argument checks, an orderly end of stream on a real loopback socket, and the evidence file and
  manifest against the implementation.

## 2026-09-27 — Instancing: squad copies of the mission maps, the instance chooser and the way out

Before this, every map but the boot camp was one shared channel (segment-3 audit `SEG3-INSTANCING`): two squads in
Pravus Research met each other and shared its creatures and mission objects. The final client was decoded
statically (xdis; `research/20260926-instancing-mechanics/`, hashes in `work/SHA256SUMS`) and read against dated
notes before anything was built. Migration `MissionContextSquadInstancing`; code in `MapChannelManager`,
`DynamicObjectManager`, `PartyManager`; decisions OD-125 to OD-129, agent-approved and pending owner review.

- **Which maps are instances comes from the client.** `gamecontext.pyo` gives every context a type;
  `gamecontexttype.pyo` names 5 MISSIONCONTEXT and 4 BATTLEFIELDCONTEXT. The loading screen shows its "Instance"
  widget for type 5 and "Persistent" for type 4 (`wonkavatorwindow` Init line 102), and entering a type-5 map other
  than 1985 posts the INSTANCE_ENTERED tip (`wonkavator.OnExitState` lines 111-112). The 53 loaded type-5 contexts
  (all but the boot camp) become per-squad (`content_map_setting` instancing 2): the Divide's Minos Caverns, Timora
  Mines, Torcastra Prison and Purgas Station; the Wilderness's Pravus Research, Crater Lake Research Facility, Caves of
  Donn, Guardian Prominence, The Empire Sector (2327) and Epic Caves of Donn; the Palisades' Warnet Caverns, Devil's
  Den, Treeback Camp, Eloh Temples and Eloh Vale; and the Valverde, Torden and Ligo instances down to Omega Labs, The
  Gauntlet and Dybukkar Garrison. The list, with each row's client offset, is in
  `MissionContextSquadInstancingRows` and the manifest. TaRapedia's Category:Instances names 48 of them. The boot camp
  stays per-character (OD-2). Context 2375 "Empire Sector: The Last Stand" is type 4, "Empire Sector shared map for
  endgame event", so it stays shared.
- **One copy per squad.** TaRapedia "Operation" rev 32773 (2008-09-04): "the server creates an identical copy of the
  zone for each party that enters it". `ChannelForEntry` finds the squad's copy or makes one. A player outside a squad
  gets their own. A copy is populated from everything the context's primary channel was loaded with: its own spawn
  pools (fresh counters), Logos shrines, teleporter pads, lockboxes, map links, navmesh and content placements
  (`PopulateContextCopy`). The primary channel of a per-squad context is only the template; nobody enters it.
- **The live invite quirk is kept (OD-125).** D10 (2008-07-23) and D13 (2008-10-15) list, as a known issue:
  "Inviting a player into your squad while you are in an instance will not initially allow the invited player to
  join the same instance as the squad leader ... after the squad leader exits and re-enters the instance, he will be
  placed in the same instance as the invited character." A copy is bound to whoever created it, the squad or a solo
  character, and that binding reproduces the quirk. No later note says it was fixed
  (`GAP-SQUAD-INVITE-QUIRK-FINAL-STATE`).
- **Leaving the squad leaves the instance.** "/leave: ... This will also remove you from an instance" (TaRapedia
  Beginners Guide rev 35313, 2008-10-23). The client warns first: `SquadMemberList`'s `partyExclusiveMap`, which was
  always false and is now true inside a squad copy, makes `party.OnLeaveParty` ask PM 946 "You will have to leave the
  current map". Disbanding uses PM 945, "All squad members will be kicked out of the current map". Leaving, being
  kicked or the squad disbanding sends the player back with PM 1058, "You have been sent back to your previous map
  because you are no longer in the squad." A disbanded squad's copies can no longer be joined, because squad ids are
  recycled.
- **The way out (OD-129).** `Recv_EnteredWaypoint` says: "if waypoints is None, user is on an adventure, and they can
  only abort the mission". The client then lists PM 315 "Leave current adventure" as waypoint 0. Inside a squad copy
  the map-waypoint list is now that one row, and `SelectWaypoint(mapId, 0)` from the pad returns the player. Local
  teleporters inside instances are unchanged. The return point is the instance's own exit link to the map the player
  came from, else the spot they left that map from, else, after a restart, its first exit link. This follows "you can
  only leave them towards the same zone you entered from". Logging out inside still logs back in there (live
  2007-08-07).
- **An empty copy is not reset at once (OD-126).** The notes say "re-enters it before the instance resets" (live
  2007-11-29) and "leaves the instance (allowing it to reset)" (2008-08-22). A player wrote "I've found that ten
  minutes works". A copy its squad can re-enter is kept 600 s after it empties: measured, an upper bound 0-10 min
  (`GAP-INSTANCE-RESET-TIMER`). A disbanded squad's copy goes at once. The boot camp keeps OD-2's immediate
  destruction.
- **Numbered copies and the chooser (OD-127).** `Recv_ChooseInstanceList(instances)` (method 685): "display an
  instance list the user can choose from to go to a shared map". Each entry is `(ordinal, instanceId, mapTemplateId,
  startGroup, overloadedStatus)` (`waypointwindow.ShowInstances` line 470). The client answers with
  `SelectInstance(mapId, startGroup)` (687) or `SelectInstanceCancel()` (688, "we no longer want to zone out"). Live
  notes 2007-07-24 say "Zoning into shared world maps will give you a choice of instances", and 2007-08-21 added a
  "waypoint window that allows user to change between instances of the same map". Both are built. With more than one
  copy of a shared map, entering waits on the chooser, which lists every copy with its status. Choosing a full one
  gets PM 934 "Please select another map". The waypoint window lists one row per copy, where it used to send one
  identical row per waypoint, and choosing another copy's waypoint moves the player there. The template id is the
  client's (`ClientMapTemplates`, from `gamecontext.pyo` column 3). Copies open only at a configured per-context
  capacity, and none is configured (`GAP-SHARED-COPY-CAPACITY`). The Last Stand's "Earth 1"-"Earth 5" are therefore
  not opened, and its content remains a gap (`GAP-LAST-STAND-COPIES`).
- **What a copy is called (OD-128).** The client prints Wonkavate's `instanceId` after the map name, "Name(n)", on
  the loading screen (`wonkavatorwindow._UpdateLoadingScreen` line 408) and in the map window header
  (`mapwindow._UpdateMapName` line 1772). The same "(n)" follows the ordinal in both instance lists. Players' "Earth 1"
  to "Earth 5" fit small numbers per context. Wonkavate now carries the copy's number: the lowest free one, with 1 for
  a shared primary. The boot camp keeps its approved monotonic id. The id the client echoes back is the context id
  for a primary channel and 0x40000000 plus the instance id for a copy.
- **Not built:** missions that fail or reset on leaving an instance (`GAP-INSTANCE-MISSION-RESET`), expulsion on
  death in hospital-less instances (`GAP-INSTANCE-DEATH-EXIT`), start groups (`GAP-INSTANCE-START-GROUPS`, None is
  sent), and any lockout: no dated source mentions one, so none exists. The boot camp's per-character instances have
  never had a navmesh (only primary channels load one). This is recorded for review and not changed here.
- `SquadInstanceTests` covers creation, squad joining, solo copies, the invite quirk, isolation at identical
  coordinates, population from the whole context, the linger and destruction with creatures, disbanding, the return
  on leaving the squad, "Leave current adventure", the chooser flow, and the byte shapes of ChooseInstanceList,
  SelectInstance, EnteredWaypoint with None waypoints and SquadMemberList's flag. It also checks the manifest rows.

## 2026-09-27 — Instance travel and death: the doors the client draws, the instance hospitals, the retired rows

Batch 2 of the instance inventory (`research/20260926-instances/README.md`, "Batch 2: instance travel and death";
per-context client markers and verdicts in `instances.json`). Migrations `InstanceTravelLinks` and
`InstanceTravelRetiredRows`; code in `HospitalCatalog` and `PlayerDeathManager.OfferedHospitals`; evidence
`docs/evidence/instance-travel-20260927.json` (decoded markers, navmesh probes, findings) and
`docs/evidence/hospital-catalog.json`; decisions OD-130 to OD-134, agent-approved and pending owner review.

- **Four operations could not be entered.** `MapLinkPreloader` built a door only where the client has a marker at both
  ends: the entrance (`uimapmarker.maplinkmarkers` type 7) on the parent map and the exit (type 8) inside, which is
  also where the door arrives. Warnet Caverns 1384, Ustor Yard 1502, Sanctus Grotto 1823 and The Refuge 2156 have the
  entrance and no exit, so they had no link either way. Each now has a door in (trigger: the client's own marker,
  original) and out (map_link 199250-199257). The arrival is the instance's entrance hospital marker (inferred,
  OD-130): Ten Ton Hammer's Warnet guide (2008-01-24) "As you enter Warnet Caverns, you'll first see the Field Medic",
  its Ustor Yard guide (2008-03-21) "Upon entering the instance you'll find a local waypoint generator and a Field
  Medic", and the Sanctus Grotto dev journal (Massively 2008-02-28) ends the map "near the entrance". Warnet has two
  "First Aid Station" markers; the world seed's hospital rows 130 "Warnet Caverns Entrance" and 131 "Research Area"
  tell them apart (131 stands on one marker; 130 is the other's position with x negated, where the navmesh has no
  floor). The Refuge's arrival is low confidence: one hospital marker at a navmesh dead end and no source on its way
  in. The exits stand on the arrival and return onto the entrance marker, as for all 48 doors the preloader built;
  arrival yaws follow the preloader's rule (atan2 about the destination's `gamecontextuimapinfo` centre), which
  reproduces its rows. Players arriving are inside the exit and must step out first (`MapLinkManager`, unchanged).
- **The Last Stand and Edmund Range.** 2375's two exit markers now lead to the CELLAR (199258/199259), arriving on the
  CELLAR's one link marker with no destination, the centre of the zone-pad ring (inferred, OD-134); the way into 2375
  stays unbuilt (`GAP-SHUTDOWN-LAST-STAND`). The CELLAR's north-end marker names the D15 map 2361 "Edmund Range OLD";
  D15.7 (live 2008-12-13) put "an entrance to the Edmund Range wargame map at the north end" and D16/D16.4 (live
  2009-02-09) replaced that map ("the D15 Edmund Range map ... the new version"), so the link goes to 2374 (199260),
  arriving at its "Staging Area" label where the map's trainer and vendors stand. 2374 has no link marker at all, so
  its way back stands on the arrival point, labelled analogue (199261, OD-131, `GAP-EDMUND-EXIT`).
- **Not built, recorded.** Ustor Yard's western exit to the Maligo bridge has no marker or position
  (`GAP-USTOR-WEST-EXIT`). Eloh Vale 2084 has no door: it is entered by dropship through missions 1198/1199/1429 near
  the Palisades pad and left from a terminal that calls a dropship (TaRapedia rev 32028). The content layer can move a
  player between maps (`TransferToLocation`), but the missions, the trigger and the terminal are not seeded
  (`GAP-ELOH-VALE-DROPSHIP-ENTRY`); inside a squad copy "Leave current adventure" is the way out. The 1.4 start
  portable waypoints (38 instances) and the 1.6 portable wormholes (seven named instances): the client has the entity
  class (28474) but marks only three contexts, and the Portable Waypoint consumable (ActionId 487) that uses them is
  not implemented (`GAP-INSTANCE-PORTABLE-WAYPOINTS`).
- **22 instance hospitals.** 63 hospital markers had no graveyardlanguage entry reading their name, so those maps
  revived their dead in place. Joined the way the client-defects batch joined Foreas Base and Cumbria - one graveyard
  naming the place, the waypoint by waypointlanguage name or the world seed's hospital row at the marker (OD-132):
  Warnet's Entrance (48/130) and Research Area (49/131), the three Eloh Temples (81/342, 80/176, 82/175), Live Target
  Pens' Entrance (153/351) and Guard Station (154/591), Cuthah Base's Entrance (135/274) and Rat Hole (134/273),
  Edmund Range's West/East Control Point (273/524, 274/525) and Red/Blue Base (275/522, 276/523), the CELLAR (243/480),
  Torcastra's AFS Medical Officer (43/119), Turpis (232/579), Retread Caves (114/586), Kardash (125/383), Temporal
  Chamber (86/387), Phanin (87/382), Staal Junkyard (130/271) and the Epic Caves of Donn (281/529). 127 hospitals on 55
  maps, each with navmesh ground under it. Torcastra's inner medic, Timora Mines and Bane Fluxite Mines (graveyard 38
  fits one by id order and the other by text), Lamna, Ustor, Caves of Donn, Logos, Crater Lake, Purgas' Control Room,
  Indra's second medic, Comm Tower, Brann, Energy Weapon Center, Rivasa, Incurables Ward and The Refuge (no waypoint)
  stay open, each with its reason; Quasso's "Hospital Vendor" is the vendor's marker. Four hospitals carry a waypoint
  id only the world seed knows (`GAP-HOSPITAL-EMULATOR-WAYPOINT`); Edmund Range's are ungated by team
  (`GAP-EDMUND-TEAM-HOSPITALS`).
- **Eloh Temples offers only the current section's hospital.** D10.5: "Players will now only be able to access the
  Hospital point for the section of Eloh Temples they are currently in." The client draws no section boundaries; each
  temple's hospital stands at its own entrance, 238-253 m from the next, so the section is the one whose hospital is
  nearest the body (inferred).
- **Retired rows.** map_info 1991 (no client gamecontext row), 2233 and 1737 (the client's "Default for map
  [test_...]" test rows) are removed, and so are Edmund Range OLD's six service spawn pools (500320-500325); 2361's
  map_info stays so a character saved there loads (`GAP-EDMUND-OLD-RELOCATION`). The map_marker rows of the unshipped
  wargame copies 2265/2373 are never served (no map_info, and markers go only to a player on that map) and stay.
- Tests: `InstanceTravelLinksTests` (rows against the client markers, the evidence file and the manifest; door pairing;
  the preloader's kinds, radii and yaw rule; `MapLinkManager.Contains`; loaded maps and ground at every arrival),
  `PlayerDeathLifecycleTests` (the new hospitals gained and offered; the Eloh Temples section rule), the migration
  block in `ContentSchemaMigrationTests` (rows, rollbacks) and provider parity. Full suite 1494/1494.

## 2026-09-27 — Crater Lake Research Facility: what the navmesh reaches, and what it holds

The Crater Lake section of the wilderness instance dossiers (`research/20260926-instance-dossiers-wilderness`,
`README.md` and `dossiers.json`), seeded where the evidence and the navmesh allow. Migration
`WildernessCraterLakeResearchFacility` (20260927030000); evidence `docs/evidence/crater-lake-research-facility.json`
(every navmesh probe, the seeded and held sets); decisions OD-140 to OD-144, agent-approved and pending owner review.
D11 reworked parts of the instance (Predators removed, three unnamed bosses, more ore canisters and treasure crates)
and no post-D11 footage exists, so pre-D11 guides are used only for what D11 did not name, at inferred tier.

- **Every position was re-probed from the instance entrance** (the arrival of map_link 15) and the hospital marker
  (teleporter 588) with this tree's `Rasa.NavMesh --path`; a seeded height is the surface minus 0.276 m (measured).
  Two of the dossier's four terminal readings are not reachable. Ten Ton Hammer (2007-10-01) puts the Processing
  Center terminal "into the destroyed building upper floor" (124.3, -27.3): the navmesh has the HQ's second floor
  (103.95, the level of its folding table and cabinet) and its roof (117.88), and neither is joined; every probe ends on
  the joined ground floor (99.64). The Biological Lab terminal (102.3, -89.6) is inside the client's
  `ArchCormanGreenhouseV01`, whose floor and roof are islands too. This is the interior defect the Pravus batch is
  investigating (`GAP-CLRF-INTERIOR-NAVMESH`). The dossier's "no navmesh" for the Observation Center reading
  (-118.4, -76.0) came from probing only y 90-180: there is floor at 71.25 with a complete path, but it is valley floor
  with no building within 30 m, against the guide's own "trail leading up" from the GPS beacon at y 141
  (`GAP-CLRF-DT3-POSITION`).
- **Seeded.** Captain Velns (199061, already placed) carries client package 23, the only completion line of 450/4
  "Speak to Velns." (missiontext 5982). **450 The Dead Live**: Dr. Franja Corman (107) to Velns, one objective,
  900 credits (TaRapedia in all 17 revisions, Ellatha) and no experience row, because no source records one
  (`GAP-CLRF-450-XP`); no prerequisite, because TaRapedia says "None" throughout and Ten Ton Hammer's starter "Hoping for
  the Best" is not a mission of the final client (OD-144). **Lt. Casper**: spawnpool 520012 stops drawing (0/0, Down
  restores 1/1) and his existing creature row stands at Ellatha's /loc as placement 1721100, guarding his spot, one per
  squad copy, not respawned in a copy (OD-142); the D11 test-server notes still name "his encampment".
  **Overseer Tyryd**: creature 1721001 (client name 7002, class 10502 for Ellatha's "Thrax Technician", level 9) at
  TaRapedia's supply-pen-key /loc (placement 1721101); health, attack and speed are analogues (OD-141,
  `GAP-CLRF-TYRYD`). **960 Logos: Movement, Around, Chaos**: Standley (134) gives and takes it back; objectives 4/5/6
  bind by LogosRecovered to the world's logos rows 50, 45 and 4 (the client's logosstone constants, all on 1721 and all
  reached from the entrance); 18,000 experience and 1,500 credits (TaRapedia, Ellatha). Levels are the givers' (10,
  OD-140). Around and Chaos stood behind the supply-pen force field that 1056/4 lowered; neither is modelled, so they are
  ungated (OD-143, `GAP-CLRF-PEN-FORCEFIELD`). Every amount is a pre-1.4 reading, labelled (`GAP-REWARD-ERA-CLRF`); the
  pre-1.4 item lists are not seeded.
- **Held.** 1055 Destroying the Evidence: three of its four speakers are unplaceable (above), so the reachable Bane
  CommLink terminal is held with it; the terminals would also need a body, because the client's
  `UsableNPCHumMonitorV01`/`UsableNPCBaneKeyboard` carry only the NPC augmentation and a usable placement cannot carry a
  package (`GAP-CLRF-TERMINAL-BODY`). 1065 Bending the Rules and its exit area: the loader rejects its prerequisite 1056,
  which is unseeded, and dropping it would offer the escort ungated (`GAP-CLRF-1056-CHAIN`). The radar dish is a
  TwoStateSwitch (augmentation 9), a state machine the content layer does not have, and 1054 is held
  (`GAP-CLRF-RADAR-DISH-TWOSTATE`; the client's creature name 6757 "Thrax ECM Jamming Terminal" is a likely name for
  1054's jammer, `GAP-CLRF-JAMMER`). 489, the ambient population, the three D11 bosses and the D11 crates, as the
  dossier (`GAP-CLRF-CRYSTAL-CONTAINER-CLASS`, `GAP-CLRF-POPULATION`, `GAP-CLRF-D11-BOSSES`,
  `GAP-CLRF-D11-TREASURE-CRATES`). Daniel Corman (199062) stands on reachable floor in the pen already.
- **Squad copies carry the shrines.** The dossier's open question (`GAP-LOGOS-IN-SQUAD-INSTANCE`) is answered by the
  instancing code: a copy clones its context's Logos shrines under the same ids (`DynamicObjectManager.CopyStaticObjects`),
  so 960's bindings fire in every copy.
- Tests: `CraterLakeResearchFacilityTests` (the seeded set against an independent list, store types, every row against
  its manifest row, every position on floor with a complete path from the entrance and the held floors without one,
  each squad copy's own shrines, rollback, the deployed world's rows and free ids), the Crater Lake block of
  `MissionContentLoadingTests` (450 and 960 offerable, the four placements every copy materializes) and of
  `ContentSchemaMigrationTests` (rows, rollback), and provider parity.
