# McAllister grounding and end-of-walk audit

Audit date: 2026-09-22. Isolated original-client run 06 captures 37, 42 and 45
show an officer apparently suspended above terrain beside the range. The
operator identifies McAllister; the screenshots alone do not establish his
entity identity, exact position or movement state. This is an emulator defect
investigation, not evidence of an original live placement.

The [machine-readable record](evidence/client-mcallister-grounding.json) hashes
the captures, stopped-run log, probe inputs/results, prebuilt probe executable,
navmesh and retained original terrain decode. External audit artifacts are
preserved in
`/home/blizz/backups/rasa-net/research/20260922-dummy-measurement/mcallister-grounding/`.
The initial investigation was read-only. The bounded stop/facing correction
below was subsequently prepared; no deployment or endpoint change was made.

## Static path and height findings

The run mounts the repository navmesh read-only. The existing prebuilt NavProbe
returns a **complete** path from McAllister's start `(387.2,125.57,53.3)` to
location 19853, `(390,120.75,172)`, through these corners:

`(386,126.31,54.8) → (382.8,128.91,62) → (382.4,121.71,100.4) → (390,120.75,172)`.

The destination is inside a walkable polygon with height 120.75. This is not an
off-mesh point merely projected to a distant surface. The retained original
terrain decode gives 120.0 at the same XZ, a difference of 0.75 m. Neighboring
navigation heights reach 120.82 at `(389,172)`. These are derived representations;
the final runtime coordinate and its exact original collision surface were not
captured. Nearby sandbags and platform geometry prevent assuming terrain alone
is always the correct supporting layer.

`NavMeshManager.SnapToGround` uses navigation polygon height for the creature's
feet, with a 3 m vertical tolerance. It can therefore preserve a simplified
surface above visible ground. Lowering only the destination Y would be undone
by snapping. The navigation build uses a 2 m terrain sampling step, 0.2 m voxel
height, and detail sampling parameters 6/1; these explain why a navigation
surface requires comparison with original collision rather than being treated
as exact rendered height.

## Separate movement findings

`BehaviorManager.FollowPath` ends within 0.8 m of its final corner. The one-shot
completion branch calls `SetActionWander`, which clears path state without
broadcasting a zero-speed movement. The last moving packet advertises 2.5 m/s.
McAllister's run speed 0 prevents ordinary wandering afterward, so a later stop
correction is not guaranteed. This is a concrete source omission, but its role
in these screenshots remains unproven. Movement packets also carry direction
without updating the creature's stored rotation.

`NavMeshManager.FindPath` discards the query's completion flag, so a partial
route can generally be treated as finished. The present destination returned a
complete route; partial routing is not demonstrated as the cause here.

## Reproduction strategy

1. Materialize the actual seeded McAllister with this navmesh, accept mission
   1992, and tick at 250 ms until his one-shot path finishes. Record every
   authoritative position and outgoing movement. Check a final zero-speed
   update and position stability over another 60 seconds.
2. Introduce an observer after arrival as a separate case. Compare that view
   with an observer present during movement to distinguish stale velocity from
   a static position mismatch.
3. Read the final native-client entity coordinate and compare it with the
   server coordinate and original collision triangles at that XZ. Preserve the
   selected bridge/deck layer when considering grounding changes.

The existing opening test advances only 20 ticks after acceptance and verifies
that McAllister moved closer; it does not check final height or stopping. A
synthetic disconnected-navmesh test can cover partial routes separately.
Do not move the inferred endpoint to an invented convenient location to conceal
the symptom. See the [run 06 record](client-gearing-playthrough.md) for the
captures and successful mission progression despite this observation.

## Bounded stop and facing correction

The one-shot completion branch now emits a zero-speed movement at the final
authoritative position, retaining the last heading. Intermediate corners and
cyclic routes retain their existing movement flow. Movement updates also store
their transmitted yaw on the creature so later introductions use the same
heading. A coincident actor/destination is handled without dividing by zero.
The endpoint, arrival tolerance and navigation height are unchanged; this
correction does not establish the cause of the suspended officer screenshots.

Original `Actor.Recv_ActorInfo`, source lines 2383–2384, offsets 0–28, applies
received yaw with `body.SetYaw` then `SyncFacingToYaw`. Original
`PlayerMovementMgr.UpdateGaitInfo`, source lines 279–285, offsets 0–54, reads
`body.GetMoveSpeed` and retains stopped gait unless speed exceeds 0.01. The
evidence JSON hashes both original modules and their disassemblies. The native
network decoder and interpolation algorithm have not been fully traced; these
Python consumers support the stop/facing contract without proving the precise
rendering cause of run 06's symptom.

`BootcampOpeningTests.WalkArrival.cs` adds two cases using the migrated creature,
placement and destination with the retained map-derived navmesh. They record
authoritative arrival coordinates and movement packets, require a terminal
zero-speed update, preserve position for 60 seconds, and check a real later
introduction's ActorInfo yaw against the last walking packet. A repeated walk
to the same position must stay finite and stationary. Root owns the serial
baseline/fixed test runs; targeted candidate verification is recorded below.

The initial baseline reached `(390, 120.75312, 172)` after 195 ticks of 250 ms,
with 195 movement packets. The last packet still advertised 2.5 m/s and yaw
−3.0358331, while stored yaw remained 0. The stop assertion failed as intended.
The second case instead failed during introduction because the direct-creature
fixture omitted its appearance dictionary; that fixture is now corrected and
the yaw baseline must be rerun. Both initial cases failed in 35.0756 seconds;
only the first is valid failing-before regression evidence. The archived log
and its hash are recorded in the evidence JSON.

The corrected baseline fails both intended assertions in 29.9349 seconds:
terminal velocity remains 2.5 instead of 0, and the actual late introduction
sends yaw 0 instead of −3.0358331203460693. Both cases reproduce the same
195-tick arrival and retain its authoritative position for a further 60 seconds.
This establishes separate stop-packet and stored-facing regressions without
relying on the earlier fixture exception. The second archived baseline log and
its hash are recorded separately.

## Endpoint collision comparison

A further read-only probe used original static meshes from the recovered
1.16.5.0 client. Its map hash matches the earlier map used for the retained
placement CSV. For all 11 entities whose transformed render bounds contain
XZ `(390, 172)`, the existing collision-decoder logic finds no nearby supporting
triangle. Its lowest static intersection is the cavern roof at Y 172.395707;
other hits are overhead rock. The terrain sample at this point is Y 120.0,
leaving the actual reproduced server position 0.75312 m above it.

This strengthens navigation-surface height as a separate candidate from stale
velocity. It is not a native collision measurement: the probe mirrors the
emulator decoder, uses rounded retained CSV transforms and filters on render
bounds, which could omit malformed/outlying collision. The exact native actor
position in run 06 remains unknown. No endpoint or grounding change follows
from this result. The external `probe-collision.py`, `collision-endpoint.json`,
extracted meshes and hashes make the comparison reproducible without a build.

The directly reinspected original `7Lrst9SG3pk` frame pair at
304.667–304.733 s cuts from the approach under the stone arch to the supply
crate loot window on the raised deck. McAllister's arrival and final ground
contact are absent. That footage cannot certify the reconstructed endpoint or
resolve the height discrepancy.

## Fixed candidate verification

Root's targeted run passed 174/174 tests in 40.2117 seconds, including both
McAllister cases. The TRX records their execution at
2026-09-22 21:15:18.431–21:15:19.077 UTC. Arrival remains 195 ticks at
`(390, 120.75312, 172)`. There are now 196 movement packets: the added final
packet advertises velocity 0, retaining yaw −3.0358331. Stored rotation is
−3.0358331203460693, and the actual late introduction matches that heading.
Both tests retain position for 60 seconds; a repeated command to the same point
remains finite and stationary. The multi-corner route emits exactly one stop.

The targeted console log and TRX are archived with hashes in the evidence JSON.
The full suite is running separately under root. These results verify the
bounded packet/facing correction, while original-client visual grounding and
the precise cause of run 06's suspended officer remain unproven.


Final combined verification: **1281/1281 tests passed**, none skipped, in
**6.8934 minutes**. All **2061 source entries** match the tested snapshot.
Logs, TRX and hashes are retained in `verification-options-final/` beneath the
original-client playthrough research directory and linked by the evidence
manifest. These tests verify implementation; the original-client observations
and surviving-source comparisons establish their separate fidelity bounds.
