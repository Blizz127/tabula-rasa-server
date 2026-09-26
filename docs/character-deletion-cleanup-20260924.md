# Character deletion cleanup after live world-entry smoke

On 2026-09-24, the recovered 1.16.5.0 client created a disposable character on
the empty live test account, entered a playable Wilderness HUD, logged out, and
deleted that character through Character Select. The pod and `character` row
disappeared, but the live SQLite database retained two ability-drawer rows, five
inventory rows, five skill rows, one teleporter row, and their five item rows.
The exact captures, pre-cleanup SQLite backup, and final database audit are in
[the live smoke record](evidence/live-world-smoke-preservation-20260924a.json).
The test account's family name and selected slot were restored, and only the
deleted test character's residual rows were removed from the live database.

`CharacterRepository.Delete` now removes character-owned appearance, ability,
inventory, logos, mission, objective, counter, content fact, option, skill,
teleporter, title, and clan-member rows, plus item instances exclusively held
by that character. Account-wide lockbox and user options remain account data;
petitions remain historical support records. `CharacterManager` starts a
transaction before these deletes and commits only after the parent character
delete succeeds. EF Core 5 raised an `InvalidCastException` in its mixed
`UInt32`/`Int32` key comparer when all rows were marked for deletion in one
`SaveChanges`; parameterized SQL child deletes within the transaction avoid that
failure. The transaction test injects a failure on the parent DELETE and proves
that child and item rows survive the rollback.

The focused normal-delete and rollback tests passed 2/2. The full
`NewCharacterTests` class passed 86/86 under .NET 5. These checks verify emulator
consistency, not original final-live deletion semantics. MySQL SQL syntax uses
the same parameterized EF Core API and backtick identifiers, but this exact
deletion path has not yet been exercised against a live MySQL database.

The world-entry part of the smoke used the then-running admission configuration
and sent a fresh character to Wilderness. The boot-camp admission policy is
being corrected separately; that result is not evidence that the boot camp is
verified end to end.

After the DIT `20260924c` image and `AllNewCharacters` admission setting went
live, a second disposable character (`Bootprobe`, id 13) entered the playable
Luna Cavern boot-camp HUD. Client deletion removed the character, all 14
`character_id` child-table rows, and its five item instances (ids 140–144).
The empty test account family fields were restored, owner characters remained
intact, and SQLite's integrity check passed. Screenshots, exact input commands,
server event timestamps, database audit, and hashes are in
[the boot-camp smoke record](evidence/live-bootcamp-smoke-preservation-20260924c.json).
The client accepted the `Initiation` offer during a click/Escape input sequence;
the evidence does not isolate which input caused that acceptance. This smoke
verifies admission and deletion, not the full boot-camp progression.
