# Seed loading performance — 2026-09-22

This correction changes how existing seed values are loaded, not their gameplay
meaning. It was needed to finish verification of the creation/boot-camp changes.

An isolated baseline test run exceeded fourteen minutes without completing
`BootcampOpeningTests.MigrateWorld`. A managed stack capture showed the class
initializer inside EF migration execution, `sqlite3_prepare_v2`, and UTF-8
conversion of SQL text. The 30,225-row `Regenerate_item_template` seed emitted one
large multi-statement command. Repeated conversion of the unconsumed SQL tail is
the likely source of the excessive allocation and CPU time. The run was stopped;
it did not produce a passing or failing suite verdict. The diagnostic capture is
`/tmp/rasa-baseline-managed-stacks.txt`.

`PreloaderBase` now emits insert operations of at most 256 rows. Column names,
row order, boxed values and the enclosing migration transaction are preserved.
This is an implementation batch size, not a gameplay rate or content estimate.
Regression tests compare every field of the 30,225-row seed for both provider
operation outputs. A SQLite test checks that the operations produce separate SQL
commands, inserts all rows, and verifies rollback of the entire transaction.

A separate focused test run reached the starter-loadout catalog check and spent
minutes enumerating `ItemTemplateItemClassPreloader`. Its managed stack stopped
at `Enumerable.ToList` entering the generated iterator; it was not executing SQL.
The source consisted of 30,225 separate `yield return` expressions in one method.
This is the same oversized-iterator pattern already avoided by
`ItemTemplateRegeneratedPreloader`. The diagnostic capture is
`/tmp/rasa-focused-managed-stacks.txt`. The mapping is converted to a primitive
data array with a small iterator. Both representations have 30,225 ordered pairs
and SHA-256 `53f5689cd904ded92245e319edf939be66c6a8577b8de066155468b2a65d605a`
when serialized as little-endian Int32 pairs; boxed numeric types also remain
Int32. The [parity record](evidence/item-class-seed-parity.json) pins the previous
repository commit and source hashes, and
[`verify-item-class-seed.py`](evidence/verify-item-class-seed.py) reproduces the
comparison/conversion. A standing test pins the row count and sequence hash.

These traces establish emulator loading defects and implementation behavior;
they provide no additional evidence that the underlying reconstructed seed
values match retail. Their existing provenance and fidelity gaps still apply.

The final isolated .NET 5 full suite passed 1,130/1,130 tests with none skipped in
4.9523 minutes, including the seed parity and rollback checks. The log is
`/tmp/rasa-retail-20260922-final.log`. This is a whole-suite result, not a controlled
performance benchmark against the interrupted baseline.
