# Hybrid race eligibility, 2026-09-22

New accounts now offer Human only. Creation and cloning read persisted account
unlocks inside their transaction; a forged hybrid request fails without creating
anything or consuming a clone credit. The selection packet reads the same state.
Existing hybrid characters remain playable. The migration carries forward only
completed unlock missions, never an existing character's race: previous emulator
behavior let every account create hybrids without earning them.

[Paul Sage's January 29 2008 interview](https://www.mmorpg.com/interviews/hybrid-video-and-exclusive-qanda-2000115533)
explicitly requires a separate race mission chain for both new and cloned
characters. The original client's `Recv_BeginCharacterSelection`, original line
803, passes the supplied race list to `SetEnabledRaces` at bytecode offsets 58–64.
This proves that the client supports account-dependent eligibility. A
[contemporary player's February 4 report, post 260](https://arstechnica.com/civis/threads/tabula-rasa-who-is-playing.138412/page-7)
confirms account scope. These are release-era evidence; late client mission text
corroborates continuity but cannot prove the shutdown server's exact configuration.

The client `missionconversation.pyo` and English `missiontextlanguage.pyo` tables,
compiled February 10 2009, identify these finishing-text joins:

| Mission | Race | Original join / text bytecode offsets | Basis |
| --- | --- | --- | --- |
| 1861, Traitor on the Run | Forean (2) | 91352 / 490556 | Finishing text explicitly grants Forean genetic access for cloning. |
| 1851, Remedy | Brann (3) | 90542 / 486452 | Finishing text describes Brann DNA vaccination; unlock mapping is inferred with the contemporary firsthand guide below. |
| 1899, Genome Sweet Genome | Thrax (4) | 94682 / 505856 | Finishing text explicitly authorizes Thrax hybrid cloning. |

The [February 5 2008 Brann walkthrough](https://www.tentonhammer.com/guides/brann-hybrid-access-mission-guide)
identifies Remedy as the last mission and says its final turn-in unlocks Brann for
the account's future clones and new characters on that server. The original
client's mission 1851 objective 2 says to be killed by a Caretaker, matching the
walkthrough and distinguishing the mission from a generic vaccination quest.

The grant is staged with mission completion, so failed saves cannot consume the
mission while losing eligibility. Persistent account rows survive deletion of
the earning character. This lifetime is an inference from account ownership;
no direct deletion capture has been found. SQL schema and row representation are
implementation choices, not reconstructed original server internals.

[The machine-readable manifest](evidence/race-unlocks.json) records per-field
provenance, client hashes, exact offsets, confidence and gaps. Tests verify the
mapping against this manifest, successful and refused creation/cloning,
completion rollback/retry, duplicate grants, account isolation, deletion survival,
and migration backfill and rollback.

These changes do not make the later hybrid mission chains fully playable: their
NPCs, objectives and other rewards still need progression-ordered reconstruction.
No extra clone credit or unlock notification is guessed here. The original
shutdown server reward tables and exact final client revision remain unverified;
`1.16.5.0` is still the emulator compatibility requirement.

## Migration tooling

The schema migrations and target models were scaffolded with `dotnet-ef 5.0.1`
under the .NET 5 SDK in an isolated source copy; stable migration IDs are
`20260922110000_AccountRaceUnlocks` for both providers. The earned-unlock backfill
is a separate data-only migration, `20260922110100_BackfillRaceUnlocks`. Its insert
is idempotent, and reversing only that data step keeps earned grants. Reversing
the schema step removes the table.

SQLite scaffolding also detected pre-existing, unrelated `petition.pos_x`,
`pos_y`, and `pos_z` type drift from `double` to `REAL`. Those alterations were
excluded, and the prior types retained in that entity's generated designer and
snapshot sections. This change deliberately leaves that existing drift for its
own review. The race schema migration creates only `account_race_unlock`.
MySQL scaffolding used a disposable MariaDB 10.5 container to satisfy the existing
factory's server-version auto-detection; it did not contact a deployed database.

The exact backfill SQL was also executed against disposable MariaDB 10.5.29
tables with the unsigned composite-key schema. It passed duplicate-completion
deduplication, account isolation, filtering of noncompleted/unknown/orphan
missions, repeat execution, incremental grants, and schema rollback retaining
the source characters. Existing hybrid characters alone granted nothing. The
local validation log is `/tmp/rasa-race-mariadb-validation-20260922.log`.
This checks the provider SQL behavior, not original-server fidelity.
