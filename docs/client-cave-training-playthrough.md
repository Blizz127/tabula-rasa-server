# Original-client cave approach and training playthrough

On 2026-09-22, isolated run `playthrough-14` verified reconnecting at level 2,
walking toward the cave, death and hospital respawn, and accepting skill and
attribute allocations through the recovered original client. It also exposed an
attribute-points display defect. **The cave objective remained incomplete, and
normal logout was not verified.** These are emulator integration observations,
not evidence that the reconstructed encounter matches final live balance.

The run used candidate binaries from the 1,300-test verification recorded in
[item-instance metadata](item-instance-metadata.md). Its runtime record reports
all 137 binary inputs matching that tested build: DLLs, PDBs, native libraries,
dependency manifests and runtime configuration. Isolated server settings are
outside that binary comparison. The harness used a synthetic account and isolated
databases; this pass did not mutate production state.

Artifacts are under
`/home/blizz/backups/rasa-net/research/20260922-client-playthrough/playthrough-14/`.
[The evidence manifest](evidence/client-cave-training-playthrough.json) records
exact paths and SHA-256 hashes for the captures, state snapshots, frozen logs,
terminal process status and adjacent binary-input and character-database copies.
Hashes were taken after the operator confirmed the harness had stopped.

| Captures | Observed behavior | State corroboration and limits |
| --- | --- | --- |
| 01–02 | Loading Operation: Bootcamp(2), then level 2 Alden Vanguard with Capture the Flag tracked and “Find a way out of the cave” pending. | Reconnect snapshot: XP 3000, credits 300; missions 1990 and 1992 completed; mission 1994 active, objective 4 complete and objective 2 incomplete. Rifle magazine 17, reserve 1000. Capture 01's “selection” filename is misleading: it shows loading. |
| 03–10 | Route toward the cave, including the ceremonial bridge and a hostile group. Capture 10 shows a level 2 Thrax Infantry Initiate and incoming damage. | The operator records ordinary westward movement without fighting during this approach. These stills do not establish original encounter difficulty, attack cadence or a combat balance defect. |
| 11–12 | Death opens Hospital Selection with Refugee Base Medic available; subsequent respawn is inside the medical tent. | Capture 11's “cave-objective” filename does not indicate completion. `cave-trigger-state.json` retains objective 2 incomplete. Server log lines 704–706 record `ReviveMe`, hospital 20000001 respawn and teleport acknowledgement. |
| 13–15 | Firearms starts at rank 1 with two training points; rank 2 preview shows zero remaining; Accept commits rank 2. | Preview snapshot still has skill 1 at rank 1. Accepted snapshot has rank 2; the other four skills are unchanged. Server log line 778 records `LevelSkills`. |
| 16–18 | Body/Mind/Spirit display 12 each with three points, then preview 13 each with zero points. After Accept, values remain 13 each but the UI incorrectly displays three points remaining. | Preview database allocations remain 0/0/0; accepted allocations become 1/1/1. Server log line 856 records `AllocateAttributePoints`. The displayed remaining-point defect is present in this candidate; no later fix is validated by this run. |
| 19 | Character remains in-world at the medical tent, with rifle magazine 15. | The attempted logout sequence failed and clicks fired two rounds after training. Despite its “trained-character-selection” filename, this is not character selection. The harness subsequently terminated the client. |

At death, the HUD reports `(289.3, 120.5, 62.7)`. The server's area probe records
a horizontal distance of 10.8 m from area 198602, outside its configured 10 m
cylinder, with vertical distance 0 m of 25 m and `entered False`. This explains
why this approach did not trigger objective 2 in the current implementation; it
does not validate the reconstructed trigger's original size or placement. The
hospital HUD position is `(357.9, 120.4, 156.5)`; the server's respawn record rounds
Y to 120.3, and persisted Y is 120.32544708251953. These are distinct observations,
not interchangeable precision claims.

The item migration preserved all 15 legacy item-table rows and their prior
fields, leaving `loot_modules`, `tradable_override` and `sellable_override` NULL.
That count covers the isolated database; Alden's scoped inventory contains ten
items. The run therefore verifies compatibility with existing items, not the
display or effects of newly populated module metadata.

`pre-stop-persisted-state.json` records Firearms rank 2, attribute allocations
1/1/1, XP 3000, credits 300, the unchanged mission checkpoint and rifle magazine
15. Its character position is `(357.9005432128906, 120.32544708251953,
156.51882934570312)`. It was captured **before harness termination**, not after
a successful logout. The adjacent frozen character database corroborates the
training and mission state. A fresh reconnect is still needed to verify their
presentation after this run. Terminal status records auth, game and Xvfb exiting
with code 0 and the client with code -15.

The frozen client log retains the startup tuple error at line 7 and 24 zero-size
projection warnings at lines 26–49. Their presence does not establish a cause
for the attribute display defect. The separate
[tuple investigation](client-startup-tuple-error.md) and
[projection investigation](client-projection-warning.md) retain those limits.
No cave completion, boss encounter, mission reward or normal logout is claimed
from this pass.

## Run 16: reconnect and normal logout

The separate follow-up `playthrough-16` resumed the continuing character on the
corrected candidate, whose full suite passed **1,302/1,302 tests**, none skipped,
in 5.7901 minutes. All 137 recorded runtime binary inputs match the tested build;
the archived source snapshot SHA-256 is
`c9b679ff7c7b3e01d286e1e972f74cc7a0276c20e3b0a5890b7ceab78f559d07`.
The intervening run 15 used a separate diagnostic database and did not change
the continuing character. Run 16 therefore checks the allocations accepted in
run 14, rather than the diagnostic allocation pattern.

Capture `01-trained-reconnect-panel.png` shows Firearms rank 2, zero training
points, rifle 15/1000 and the pending cave objective. Capture
`02-trained-attributes-reconnected.png` shows Body/Mind/Spirit 13/13/13 with zero
attribute points. This verifies reconnect presentation of the saved training.
The separate [allocation reply investigation](client-attribute-allocation-reply.md)
records the fix and run 15's controlled post-Accept verification; reconnect alone
does not reproduce that callback sequence.

Capture `03-logout-countdown.png` shows the actual Logout dialog with ten seconds
remaining. Capture `04-trained-character-selection.png` visibly reaches character
selection. Unlike run 14's final frame, this establishes ordinary character
logout. The frozen `logout-persisted-state.json` retains Firearms rank 2, spent
attributes 1/1/1, XP 3000, credits 300, rifle magazine 15 and the unchanged mission
checkpoint. Logout saved position `(357.8984375, 120.359375, 156.515625)` and yaw
`1.5216131210327148`; the adjacent frozen character database corroborates it.
The harness then stopped, with the same terminal exit-code pattern as run 14.

The evidence manifest adds run 16's frozen captures, logs, terminal status,
database state and binary inputs separately. It retains run 14's original
failure and defect. The cave objective and the remaining Capture the Flag
encounters are still unverified by these runs; neither the successful reconnect
nor normal logout establishes original live encounter balance.
