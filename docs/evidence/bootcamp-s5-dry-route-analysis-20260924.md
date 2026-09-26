# S5 corpse-to-bomb dry route, 2026-09-24

## Scope and evidence boundary

This is a route for **one unreset mission 1995 timer** from the current inferred Conrad corpse to the current inferred crashed-dropship bomb. It is a test plan, not a proven completed run. The original map navmesh has a path between the endpoints, and stitched ordinary movement in the 1.16.5.0 client traversed the dry high bank to a viewpoint that successfully used the bomb. Two continuous timed runs did **not** complete: the first lost game focus roughly 16 m from the bomb with `00:54` left; the second hit bridge rubble north of the safe line and then lost focus. The seeded corpse and bomb placements are reconstructions, not recovered final-live coordinates.

**Later test:** A third, focus-guarded, continuous timed repeat also expired; its fixed-key continuation went west/north to `(-232.035,99.344,39.828)`, far from the bomb. The focus guard worked, but route alignment remained unreliable. See [focus guard and timed repeat](bootcamp-s5-focus-guard-and-timed-repeat-20260924.md). The waypoint list below remains a measured path assembled from ordinary movement checkpoints, and requires visual steering at each bridge turn.

Original gameplay video [`8VXeKzGUv0c`](https://www.youtube.com/watch?v=8VXeKzGUv0c) shows the bomb objective at the wreck with `00:02:06` remaining at 80.600 s, and completion with `00:02:00` remaining at 86.800 s. A cut between 80.533 and 80.600 s removes the corpse-to-wreck travel, so it supports timer feasibility at the original player's endpoint but **does not identify the route**. See `footage-events-s5.json` events B1-044 through B1-050. The original video revision relative to final shutdown remains unproven.

## Dry route for the next bounded attempt

Coordinates are client `/where` X,Y,Z, in metres to the displayed precision. These are **measured** emulator-client checkpoints, with about 0.01 m display rounding and additional movement/collision variation. They are not exact final-live placements. Move in short visual-feedback segments, keeping on the bridge's dry upper bank. Use the wreck and bridge rail as landmarks; avoid treating held-key durations as replay commands.

| Stage | Checkpoint (X,Y,Z) | Landmark and action | Evidence |
| --- | --- | --- | --- |
| Corpse | `(-94.730,84.996,65.641)` | Use Conrad's corpse once. Back away from its metal/prop collision before turning toward decreasing X and Z. Timer starts at this use. | Single-session run `01-corpse-use-after.png`; observed use, measured location. |
| Exit trench | `(-100.734,84.770,57.645)` → `(-109.168,83.770,39.281)` | Clear the corpse structure and bend west toward the low corridor. | Repeat `01-corridor.png` through `06-corner-bypass.png`; measured. |
| High-bank fork | `(-124.855,83.520,38.473)` → `(-128.680,83.520,34.492)` → `(-135.742,84.769,30.594)` | From the corridor, favor the **southward side of the upper bridge/rail**. At X about -135, aim for Z about 30, not Z 37. | Repeat `07-bank-start.png`; timed-water run; stitched high-bank run 01; measured. |
| Bridge edge | `(-140.012,84.770,28.238)` → `(-145.039,84.828,22.246)` → `(-149.371,84.820,18.848)` → `(-160.250,84.820,6.352)` | Follow the dry top of the bridge west/southwest. Keep the Y near 84.8; do not descend to the water under the bridge. | Stitched high-bank runs 01, 02, 04; measured. First single-session run also passed `(-159.258,84.820,7.270)`. |
| Rock climb | `(-162.746,85.727,1.848)` → `(-165.086,89.695,-15.418)` → `(-169.727,95.113,-22.375)` | At X -160, direct west motion was blocked. Angle southwest and climb the rock instead. | Stitched high-bank runs 05 and 06; measured. |
| Wreck approach | `(-180.004,92.568,-26.121)` → `(-185.250,93.516,-35.088)` → `(-202.070,95.773,-35.988)` → `(-206.891,98.414,-52.660)` | Cross the elevated rocky ground toward the burning crashed hull. This is the dry line, not the water below. | Stitched high-bank runs 06 and 07; measured; first single-session run passed `(-181.465,91.632,-21.969)`. |
| Pad lip | `(-207.203,100.223,-61.695)` → `(-208.965,100.266,-60.203)` → `(-219.375,100.773,-64.047)` | At X -207,Z -62, direct west and southwest movement was blocked; step toward Z -60, then west along the upper lip. **Turn west here** rather than continuing south on the far side of the wreck. | Stitched high-bank runs 08–10; measured. |
| Bomb viewpoint | `(-222.063,101.051,-69.953)` → step back to `(-220.633,101.051,-69.164)` | At the first point, center aim did not acquire the object. A short back step produced the “Press [Mouse Button 2] to use” prompt. Aim at the glowing hull panel, right-click, and allow about 8 s for the explosion/objective change. | Stitched high-bank run 11 and bomb acquisition run 12; measured viewpoint and observed use. |

The polyline above is about **211 m horizontally**; this is a computed distance through sampled checkpoints, not an original navigation value. A successful normal play route may be shorter. The navmesh path independently connects the inferred endpoints via roughly `(-127.6,83.71,36.0)`, `(-194.8,95.51,-33.2)`, and `(-218.8,101.11,-64.8)`, but navmesh alone does not account for client collision or enemies.

## Two observed route traps

1. **Too far south too early:** from `(-128.680,83.520,34.492)`, a diagonal through `(-131.801,84.891,23.711)` and `(-141.227,79.824,16.043)` dropped to `(-142.582,79.270,11.359)` in water under the bridge. Remain near the dry bank's `(-135.742,84.769,30.594)` checkpoint before following the bridge rail.
2. **Too far north at the fork:** the second timed attempt ran through `(-137.496,84.270,37.824)` to `(-152.008,84.203,37.262)` and met bridge rubble/collision. The demonstrated dry-bank line is roughly 8–22 m farther south in Z over that X range. Once at the rubble, local strafing and turning did not recover the route before expiry.

At the wreck, the first continuous run continued to `(-205.871,102.355,-71.824)` with `00:54` left, still about 16.1 m horizontally from the inferred bomb. Its `.where` diagnostic then left chat in UI Mode; movement keys typed into chat, and the timer expired without a second `RequestUseObject`. The repeat also experienced this diagnostic focus fault at the bridge. For the next test, take any needed coordinate readings **before** the final wreck approach and navigate the last section visually with game focus confirmed. That is a harness control, not a retail gameplay claim.

## Time assessment and next verification

The first failed continuous run contained 26 held-movement commands totaling **47.0 s** and reached the near wreck with `00:54` left. The repeat contained 19 holds totaling **38.5 s** but stopped at the bridge. These totals omit command dispatch, `/where`, screenshots, combat, loading, and stalled/collided time; they do **not** establish a 47-second complete route. They show that diagnostic overhead dominated the prior 600-second attempts. The original video confirms a bomb completion with two minutes left in that edited original session, but its missing travel prevents comparing route details. A dry route inside 600 seconds is plausible and actionable; **single-session corpse-to-bomb completion remains unverified**. Do not alter the timer, collision, or seeded placement on this evidence.

The next bounded verification is one isolated original-client session from the same explicit corpse checkpoint: use corpse, take the high-bank fork, avoid `/where` and chat near the wreck, acquire the use prompt from the measured viewpoint, right-click before timer expiry, and seal screenshots, game log, objective state, command ledger and SHA-256 hashes. No state reset after corpse use.

## Source identity

All named `s5-*` capture directories are under `/home/blizz/backups/rasa-net/research/20260922-client-playthrough/`; local paths are evidence locations, not retail sources. SHA-256:

| Source | SHA-256 |
| --- | --- |
| `docs/evidence/bootcamp-s5-single-session-timer-route-20260924.json` | `fe9e411c4d9337b10c2271a5c891a3f1c0e6b164cbce606f01220e3678a0c72a` |
| `docs/evidence/bootcamp-s5-single-session-timer-route-repeat-20260924.json` | `76ca4d8b57d226a4485ed8c5062511137dbc8d7d88cb27d003eae1367982cbc3` |
| `docs/evidence/bootcamp-s5-higher-bank-wreck-approach-20260924.json` | `4d4822dd498d7599c6c77ac677e1824969b34442b9eae185807583ead52cfa6b` |
| `docs/evidence/bootcamp-s5-bomb-acquisition-from-route-20260924.json` | `b109df460331d6f7cffa79ccf2500806918e8fcffef971e50f849f75659c5ca6` |
| `docs/evidence/bootcamp-s5-timed-wreck-route-20260924.json` | `70d6876223b7b8bd7e85feff35a178cc696feead60a17df985067a140e9f7747` |
| `docs/evidence/bootcamp-s5-wreck-route-candidate-20260924.json` | `e15ab4cee00829e78fe301bf10339e8581a1db124a77f8535c1098d4180b3f64` |
| `/home/blizz/backups/rasa-net/research/20260914-bootcamp-s5/footage-events-s5.json` | `87ee68cf3c1bc83cda2a87685b94a40f86239b6ba9967c0b44eb3ac3879c11f1` |
| Original gameplay capture `8VXeKzGUv0c`, local `Tabula Rasa Advancing 2of8.mp4` | `43187d064abf0282f150513a83f5abd07023a3b9db5baab01a6431f1536f63e0` |
| `navmesh/adv_bootcamp.nav` | `10a6fd9a1fa9ef88d89befe9b757cacdefb6584cc584ce2462393178f85b2f52` |

**Later 2026-09-24 controller evidence:** `/loc` displays one-decimal coordinates above the minimap and an untimed closed-loop client run walked the full dry route to `(-220.3,101.1,-69.1)`. A separate copied prelaunch diagnostic confirmed the bomb use prompt and objective completion from that viewpoint. Subsequent continuous timer runs still did not use the bomb: V2 stopped at `(-217.3,100.0,-61.0)` with objective 1 active, V3 descended under the bridge, and V4 ended on a controller stale-coordinate error with `04:49` left. These runs do not establish timer impossibility. See [the route and control record](bootcamp-s5-loc-waypoint-route-20260924.md) and its hashed manifest.
