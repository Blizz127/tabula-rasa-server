# Calling for Reinforcements: first client checkpoint

Audit date: 2026-09-22. A copied level-2 Alden checkpoint had mission 1994
completed. An isolated recovered 1.16.5.0 client and server loaded that save.
Only the isolated world copy disabled attacks for nearby creatures 198507 and
198513. The live world was not changed, and the test world was restored after
the run. [Captures, databases, setup and hashes](evidence/client-reinforcements-opening.json)
identify the exact inputs and outputs.

Youngblood offered mission 1995 after the 1994 turn-in. His Mission Available
dialogue displayed the original client briefing and objective 2, “Locate the
missing AFS soldiers.” Accept Mission added it to the tracker without a
countdown. This matches the absence of a timer during objective 2 in the
[retained original footage](bootcamp-client-evidence.md).

A diagnostic GM teleport placed Alden near the seeded wounded soldier at
`(-102.4,86.09,70)`. Its marker appeared low beside the ramp in the client.
The right-click talk prompt and Objective Completion dialogue worked. The
client displayed the speaker as “Human” and text 21558 about Conrad's bomb.
Continue completed objective 2 and revealed objective 3, “Remove the bomb from
Conrad's corpse.” The copied database records mission 1995 active, objective
2 complete and objective 3 incomplete, with XP and credits unchanged.

The soldier's placement and class are inferred/analogue in the
[reconstruction manifest](evidence/bootcamp-d11-reconstruction-manifest.json).
The diagnostic approach proves the conversation path but does not prove
physical access from the normal route or the exact final-live position.
The next [isolated client comparison](evidence/client-conrad-corpse-placement.json)
found that the inferred Conrad placement `(-102.4,85.69,66.8)` sits inside
the ramp support in the client, has no navmesh floor at that XZ, and has no
complete route from the wounded soldier's area. A right click at those
coordinates did not complete the objective. The corrected inferred placement
`(-98,85.39,67.2)` has a navmesh floor and a complete route. A right click
there sent `RequestUseObject`, completed objective 3, revealed objective 1,
and started a 600-second timer displayed as `00:09:59`. The saved character DB
confirms both objective states and the timer duration. The exact final-live
corpse position remains unknown; this is a labelled playable reconstruction,
not a recovered retail coordinate. The seeded S5 row remains as the historical
draft, with the correction in `BootcampConradCorpsePlacement`.

The candidate appears beside ramp sandbags, so its exact visual placement
and the normal walking approach need verification. A further
[bomb checkpoint](evidence/client-reinforcements-bomb-attempt.json) loaded the
seeded bomb entity at the burning wreck, but right clicks from three diagnostic
positions did not send `RequestUseObject`; the countdown persisted across a
relogin. Original footage shows use of an upright orange-lit panel. The
subsequent [placement comparison](evidence/client-reinforcements-bomb-correction.json)
isolated the obstruction: the same bomb worked on open ground, and a
corrected near-hull position `(-221.95,102.3,-70.5)` displayed the client use
prompt. Its X and Z are inferred within the original radar estimate's
per-axis ±2 m bounds. Right click sent `RequestUseObject`; detonation
completed objective 1, removed the wreck, spawned reinforcements, and
revealed objective 4, “Check in with Corporal Van Valkenberg.” The copied
database confirms objective 1 complete, objective 4 active and the
`bootcamp.dropship_destroyed` fact. The exact retail bomb point and
presentation are still unverified.

A further [client handoff](evidence/client-reinforcements-handoff.json) opened
Van Valkenberg's original objective-completion text. Continue completed
objective 4 and, because Alden was already on the damaged pad, transferred him
to Alia Das. The saved account has `can_skip_bootcamp=1`; mission 1995 remained
active with all objectives complete. Headquarters offered Training Day on
arrival. At Alia Das, Rogers offered the Calling for Reinforcements turn-in,
and Complete Mission changed mission 1995 to state 4. The tracker then showed
Training Day. Rogers' generic conversation screen displayed `ERROR: ? No
greeting`; its original greeting is an evidence gap. The normal route to
Rogers, exact rewards and original presentation remain to be verified.

A [failure and retry client check](evidence/client-reinforcements-retry.json)
advanced the copied objective-1 timer diagnostically. The compatible client
showed the countdown near zero; the saved database then recorded mission 1995
failed and objective 1 failed. Youngblood offered the original retry briefing,
and accepting created mission 2005 with a fresh 600-second objective timer.
Using the corrected near-hull bomb in that retry sent `RequestUseObject`,
removed the wreck, completed objective 1 and revealed Van Valkenberg as
objective 4. The test used a timer edit, GM teleports and the isolated bomb
placement correction; it verifies this emulator path through the recovered
client, not the unknown final-live server values or normal traversal.

The repository built under the .NET 5 SDK after the migration. The boot-camp
migration parity suite passed 15/15 after the earlier `BootcampRifleMelee`
migration was converted from equivalent raw SQL to shared `UpdateData` calls.
