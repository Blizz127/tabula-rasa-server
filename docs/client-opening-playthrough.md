# Original-client opening integration playthrough — 2026-09-22

The recovered **1.16.5.0 client** successfully created a Human Recruit, entered
the emulator's rebuilt boot camp, displayed the starter HUD, and progressed
through both Eloh approach objectives, turned in Initiation, and returned to
character selection through normal logout in an isolated test environment. This is
**emulator integration verification**, not footage of the original live service
and not proof that reconstructed rewards, placements or timings are retail exact.
The shutdown client's precise revision remains a separate evidence question.

The client archive came from the Internet Archive preservation item recorded in
`acquisition.json`; its SHA-256 is
`1ee00dc439d92a9717b9ae925ff827e345063fa4200dd202f0ccf2d22bd14f3e`.
Acquisition records report matching archive metadata hashes and checked ZIP CRCs.
Exact capture, executable, server assembly and log hashes are recorded in
[the machine-readable evidence](evidence/client-opening-playthrough.json).

All paths below are relative to
`/home/blizz/backups/rasa-net/research/20260922-client-playthrough/`.
`run-playthrough.py` starts a Bubblewrap `--unshare-all` environment containing
copied client/server files and isolated databases. The repository navmesh is
mounted read-only. The client and test auth/game servers communicate through
loopback inside that environment. Wine runs on Xvfb display `:107`; client audio
is disabled. These tests did not mutate production services or databases.
Synthetic credentials are deliberately omitted from this record.

| Check | Direct evidence | Result and limit |
| --- | --- | --- |
| Create Alden Vanguard, Human Recruit | `playthrough-01/06-created.png`; `playthrough-01/initial-character-state.json` | Selection renders Alden Vanguard and the Recruit outfit; saved race=1, class=1, level=1, slot=1. This verifies the emulator's real creation flow through the original UI. |
| Starter kit and first map | `playthrough-01/09-initiation-walk.png`; initial state | Pistol HUD reads **20/1000**; Lightning and Sprint appear in the ability drawer. Saved pistol122875 has20 loaded rounds; ammo28 stack1000; boots122854, legs122855 and vest122856 occupy equipment slots2,16,15. Five Recruit skills are rank1. |
| Initiation acceptance | `playthrough-01/09-initiation-walk.png` | Chat displays “Mission Accepted: Initiation”; tracker displays “Approach the Eloh Hologram”. The initial database snapshot predates this acceptance and correctly has empty mission tables. |
| First approach trigger | `playthrough-02/07-first-hologram.png`; `playthrough-02/game.log`; read-only isolated character query | Walking toward area198600 displays “An Ancient Eloh” dialogue and highlights Lightning. Saved character1 has Logos23, mission1990 objective1 status2 (completed), and objective2 status1 (incomplete). This verifies current trigger/grant persistence, not the original service's exact grant timing. |
| Re-entry through supported client arguments | `playthrough-03/commands/1790105812357786441.json`; `playthrough-03/02-world-pi-before-input.png`; game log lines97–99 | The original client's `/user`, `/password`, `/server` and `/character` arguments admit the synthetic account and existing character automatically. The server receives character selection and `MapLoaded`; the world and20/1000 HUD render. Argument values containing credentials are not reproduced. |
| Second Eloh approach and visible projection | `playthrough-03/06-second-hologram-approach.png`; later read-only isolated character query | A large Eloh hologram visibly renders with the second Ancient Eloh dialogue. Both1990/1 and1990/2 are now saved as completed. This establishes that hologram presentation works in this isolated session. |
| Initiation turn-in and next offer | `playthrough-03/11-mcallister-dialogue.png`; `12-initiation-completed.png` | Major McAllister's completion window displays100 credits; completing it produces the1250 XP chat line and offers Gearing Up for Battle (1992). This confirms the configured emulator rewards render and apply; their original-live provenance remains in the reconstruction manifest. |
| Normal logout and saved state | `playthrough-03/15-logout.png`; `16-character-selection.png`; `logout-persisted-state.json` | Esc-menu logout displays its countdown and returns to character selection. The saved character retains1250 XP,100 credits, Logos23, mission1990 state4, both objectives completed, and position(387.19921875,125.03125,49.1171875) in context1985. |

Runs02/03 also include deliberate **isolated orientation probes**. Their
`yaw-pi` captures and re-entry position are diagnostic setup, not measurements
of original facing or spawn coordinates. Mission/Logos persistence is recorded
separately from that position setup. Audio, narration and audiovisual timing
were not validated because sound was disabled.

After the initial orientation setup, movement to McAllister used ordinary
client input. A ledge near Z39.9 interrupted forward walking; the recorded
Space jump (`playthrough-03/commands/1790106146427853253.json`) and subsequent
walking reached the NPC at saved Z49.1171875. This segment did not use a GM
teleport. The capture verifies a traversable route with a normal jump, not
that the reconstructed path, ground heights or facing match the original service.

Verified unresolved observations at this capture boundary:

- Each recorded map admission logs three
  `UpdateStatsValues: Player try to equip non_armor item` messages. They coincide
  with the three starter clothing pieces, but the messages do not identify item
  IDs; the cause and intended clothing/stat treatment require investigation.
- `run-client/tabula_rasa.log` records `TypeError: argument list must be a tuple`
  at line7 during startup/map entry, without a Python traceback for that error.
  The same log also records an armed-weapon-drawer `NoneType` error at lines27–29
  and three `_LoadElementalIconForItemTemplate` iteration errors at lines32–47.
  World admission still succeeds. These observations do not establish whether
  the cause is a packet, initialization order, Wine, or another client path.
  Subsequent source fixes for these issues were not run in this captured session.
- The first-trigger screenshot's camera view did not show an identifiable Eloh
  actor, but the later second-approach capture clearly renders the projection.
  A general missing-hologram defect is therefore not established. Exact first
  projection timing and presentation still need comparison with original footage.
- No completed playthrough through the crate, practice targets, subsequent
  boot-camp missions, exit or skip path is established by these captures.

Run03 ended cleanly; `playthrough-03/launcher.log` records harness shutdown.
The evidence now includes final whole-file hashes for the stopped run logs,
retaining earlier prefix hashes as a history of observations. The read-only database
observation embeds its exact SQL and selected results; it is not a hash of a
live database file. Screenshots and the initial state snapshot have whole-file
hashes. Later fixes or captures should be recorded as later observations rather
than erasing this baseline.
