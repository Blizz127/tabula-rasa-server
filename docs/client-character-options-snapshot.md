# Character option snapshots and untracking

Original-client run 10 exposed a persistence defect after the mission-tracker
admission repair. Capture `06-checkbox-untracked.png` showed an unchecked Track
box and an empty tracker. Normal logout reached selection in
`07-verified-untracking-selection.png`, but the saved option 55 remained
`1,992`. Reentry in `08-untracking-reentry-baseline.png` restored the unwanted
tracked mission. These are isolated emulator observations, not original-live
service captures.

The original sender saves a complete **non-default snapshot**. In
`clientmethod.SaveCharacterOptions`, line 620, offsets 74–83 compare each value
with its registered default. Only different values enter the list at offsets
87–105. The sender sorts that whole list, compares it with its previous cache,
and sends it at offsets 141–159 even when it is empty. Meanwhile,
`gameui.SaveMissionTrackingData` writes all 30 native tracking options, using
zero for unused slots. Clearing the last tracked mission therefore removes its
entry from the save packet; it need not send an explicit zero.

The native caller confirms this is not a partial changed-options list. Export
binds to `0x495730` and dispatches the observer's virtual slot `+0x18`. The
original RTTI identifies `OptionObserverSerialize@TRasa`, vtable `0xbe0674`,
whose export function is `0x5360c0`. It selects the literal `Character.` at
`0x536137` and calls `0x561a60` at `0x53617c`. That callback recursively traverses
the options tree through `0x4efd40`, collects matching names, and sends the full
list to Python at `0x561b43`. Its option flag at `+0x54` is the exportable boolean
supplied to AddOptionUnicode, stored at `0x494229`–`0x494230`; the original
default loader passes True for every character option. No changed-value filter
appears in this traversal. Exact original executable and disassembly hashes
are retained in [the evidence manifest](evidence/client-character-options-snapshot.json).

The server previously ignored an empty save and merged every nonempty save,
retaining rows omitted because they returned to defaults. It now replaces this
character's snapshot: supplied values update existing rows, omissions delete
rows, and new values are inserted in one database save. An empty list clears
the snapshot. The in-memory list is replaced only after persistence succeeds.
Raw strings are preserved, including native comma grouping; other characters
and mission progress remain untouched. Missing rows let the original client
use its own registered defaults.

Four regressions cover an empty reset, omission from a nonempty snapshot,
raw-string preservation with an independent cache, and failure rollback. The
failure case uses a SQLite trigger to abort a mixed delete/update/insert save
and checks that both database rows and the old cache survive. Each request is
decoded from the original tuple/list wire shape. The targeted suite passes 179/179 tests (50.8846 seconds). In original-client
run 11, visibly unchecking tracking and logging out now leaves no character-option
rows. Run 12 starts a fresh original-client process and correctly keeps the
tracker empty and journal checkbox unchecked, with all mission/objective rows
unchanged. No gameplay progress was edited for these checks. These results
verify this emulator integration; full-suite results are recorded below.


Final combined verification: **1281/1281 tests passed**, none skipped, in
**6.8934 minutes**. All **2061 source entries** match the tested snapshot.
Logs, TRX and hashes are retained in `verification-options-final/` beneath the
original-client playthrough research directory and linked by the evidence
manifest. These tests verify implementation; the original-client observations
and surviving-source comparisons establish their separate fidelity bounds.

Run 12 also rechecked the mission through the normal journal checkbox. Normal
logout saved option 55 as `1,992` again, directly corroborating the native
formatter trace without a diagnostic database edit. The synthetic character is
left at selection, ready to continue the final Hartmann conversation.
