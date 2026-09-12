# Mission implementation research — 2026-09-12

This is an evidence inventory, not a claim of retail mission support. The
persistence corrections below were validated in isolated databases; that work
did not change live mission, NPC, reward, or character rows or activate quests.
The target is the user's exact final-live preservation requirement in `AGENTS.md`.
The acquired executable identifies itself as 1.16.5.0 and supplies original
client mission structures; see [client artifacts](client-artifacts.md) for
provenance and final-distribution authentication limits.

## Current server behavior

`MissionManager.LoadMissions` loads only `npc_mission` definitions. The live
SQLite world contains two definitions, zero `npc_mission_reward` rows, and no
objective or mission-script tables. Both definitions match the checked-in
`NpcMissionPreloader` seed exactly:

| Mission ID | Comment | Giver | Receiver | Level | Group | Category | Share/radio |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 321 | Assemble With Lieutenant Perkins | 101 | 100 | 5 | 1 | 1 | false/false |
| 429 | River Recon | 101 | 100 | 3 | 2 | 2 | true/true |

Creature 100 is Outpost Commander Rogers; 101 is Field Sgt. Witherspoon. Both
`npc_package` comments are literally `test`. These are unsuitable as validated
retail quest definitions.

Current implementation gaps, established by reading the corresponding files:

- `NpcManager.AssignNPCMission` indexes the mission dictionary without checking
  existence, ignores `NpcEntityId`, does not check giver, duplicate/completed
  status, proximity, map, or prerequisites, and sends `MissionGained` without
  storing an accepted mission. Its `> 30` limit also allows a 31st mission if
  state storage is subsequently implemented unchanged.
- `CompleteNPCMission` only logs a TODO. There is no implemented
  `CompleteNPCObjective` client packet/handler, despite opcode 431 being listed.
- NPC conversation menus and overhead markers depend on giver/receiver identity,
  with no player mission state filtering. Thus an unaccepted mission appears
  completable at its receiver; completed missions would still be advertised.
  The receiver marker currently uses `ObjectivComplete` rather than the distinct
  `MissionComplete` enum value.
- `MissionInfo` instances belong to globally loaded definitions. Future player
  progress must use independent state rather than mutate these shared instances.
- The original `CharacterMissionEntry` key allowed only one mission per
  character, and repository `Get` ignored both account and slot. The persistence
  correction below fixes those defects. Character creation still creates an
  unused `missionData` dictionary; it does not restore mission progress to
  `Manifestation.Missions`. Acceptance still does not write a mission row.
- `MissionStatusInfoPacket` exists but has no call site. Reconnect currently has
  no mission-log synchronization.
- `NpcMissionRewardRepository.Get(missionId)` exists but is not used by gameplay.
  No reward semantics or XP reward storage are wired into mission definitions.
- `MissionInfo.Write` now preserves X/Y/Z positions, change time, generic
  three-value counter entries, and nullable objective timers. Empty indicator
  lists are initialized. [Original client evidence](final-client-mission-evidence.md)
  establishes these structures; existing empty objectives do not exercise them
  during gameplay, and live-client rendering remains unverified.
- `NpcMissionEntry.CategoryId` is now `uint`, with paired provider migrations
  below. This removes the byte storage limit without assigning new categories
  to the existing mission definitions. The packet field is also widened; the
  original category table confirms 10000044 as a real Wilderness category.

Relevant local code: `Managers/MissionManager.cs`, `Managers/NpcManager.cs`,
`Managers/CharacterManager.cs`, `Managers/CreatureManager.cs`,
`Structures/MissionInfo.cs`, `Structures/MissionObjective.cs` under
`src/Rasa.Game`, and the mission repositories, structures, and migrations under
`src/Rasa.DBL`.

## Mission persistence correction — 2026-09-12

This change repairs emulator storage and query defects; it does not reconstruct
the original server's database layout or establish retail mission behavior.
The evidence for the defects is the previous model and repository in this
repository. Confidence is high in the corrected storage invariants, based on
the isolated database checks below. Mission-state meanings, objectives,
completion history, and the exact final-live quest lifecycle remain unverified
or unimplemented.

The current implementation is:

- [`CharContext.SetupCharacterMissionTable`](../src/Rasa.DBL/Context/Char/CharContext.cs)
  configures `(CharacterId, MissionId)` as the composite primary key.
  [`CharacterMissionEntry`](../src/Rasa.DBL/Structures/Char/CharacterMissionEntry.cs)
  no longer declares `CharacterId` as a standalone generated key. Multiple
  missions can coexist for a character, and the same mission can coexist for
  different characters. Duplicate character/mission pairs remain invalid.
- [`CharacterMissionRepository.Get(accountId, characterSlot)`](../src/Rasa.DBL/Repositories/Char/CharacterMission/CharacterMissionRepository.cs)
  keeps its existing API and untracked results. Its SQL query now requires a
  matching character ID, account ID, and character slot. Unknown account/slot
  combinations and orphan mission rows are not returned. The correction does
  not delete or reassign any stored rows.
- [`NpcMissionEntry.CategoryId`](../src/Rasa.DBL/Structures/World/NpcMissionEntry.cs)
  uses `uint` so storage can represent category identifiers above 255.
  Widening the representation supplies no new quest data, category assignment,
  rewards, or prerequisites.

Following `docs/setup.md`, the migrations were generated with `dotnet-ef 5.0.1`
and the repository's EF Core 5.0.1/Pomelo 5.0.0-alpha.2 dependencies in an isolated
.NET SDK 5.0.408 container. Generated migrations, designer files, and snapshots
were copied unchanged from the tooling output. The schema and data changes were
kept separate; these migrations contain no quest-content inserts or updates.

| Context | Generated migration | Effect |
| --- | --- | --- |
| SQLite Char | [`20260912172745_CharacterMissionCompositeKey`](../src/Rasa.DBL/Migrations/SqliteChar/20260912172745_CharacterMissionCompositeKey.cs) | Rebuild the mission table with a two-column primary key and no generated character ID. |
| MySQL Char | [`20260912172800_CharacterMissionCompositeKey`](../src/Rasa.DBL/Migrations/MySqlChar/20260912172800_CharacterMissionCompositeKey.cs) | Remove the old auto-increment identity and replace the primary key with the two-column key. |
| SQLite World | [`20260912172817_WidenMissionCategory`](../src/Rasa.DBL/Migrations/SqliteWorld/20260912172817_WidenMissionCategory.cs) | Record the CLR model change. `Up` and `Down` are empty because both widths map to SQLite `INTEGER`. |
| MySQL World | [`20260912172835_WidenMissionCategory`](../src/Rasa.DBL/Migrations/MySqlWorld/20260912172835_WidenMissionCategory.cs) | Widen `category_id` from `tinyint unsigned` to `int unsigned`. |

Upgrade preserves each existing mission's character ID, mission ID, and state
verbatim. The old key already guarantees uniqueness of the new pair, so no
deduplication or state conversion is required. Existing category values also
remain unchanged. SQLite applies pending migrations on server startup; MySQL
requires the explicit updates described in `docs/setup.md`. Isolated validation
does not establish that a live database has received these migrations.

Validation on 2026-09-12:

- Six [`CharacterMissionPersistenceTests`](../src/Rasa.Test/CharacterMissionPersistenceTests.cs)
  passed against isolated in-memory SQLite databases. They verify account and
  slot isolation, untracked reads, multiple missions surviving context reload,
  updates leaving other missions/characters intact, database rejection of a
  duplicate pair, and preservation of old rows through the actual Char
  migration chain. They also verify a compatible downgrade/upgrade and that an
  incompatible SQLite downgrade fails without losing either mission or its
  applied-migration record. Synthetic states include `uint.MaxValue` to ensure
  migration does not reinterpret stored bits; this is not a retail state claim.
- [`MissionCategoryPersistenceTests`](../src/Rasa.Test/MissionCategoryPersistenceTests.cs)
  passed, reloading categories 255, 10000044, and `uint.MaxValue` from isolated
  SQLite storage. This tests storage/materialization, not a full World seed
  migration or the validity of every number as a retail category. All seven
  focused tests completed successfully in approximately four seconds.
- A disposable MySQL 8.4.11 database applied the complete previous Char
  migration chain and then the generated new Char SQL. Legacy rows, including
  state `4294967295`, survived. A second mission for the same character could
  be inserted and updated independently; a duplicate pair returned MySQL
  error 1062. The generated provider SQL handled removal of auto-increment
  before dropping the old key.
- The generated MySQL World widening SQL was applied to an isolated
  `npc_mission` table with the previous column definitions. Categories 1 and 2
  survived unchanged, and 10000044 and `4294967295` stored exactly afterward.
  This was a focused schema fixture, not a complete MySQL World migration-chain
  test. The disposable MySQL container was removed after validation.

The combined candidate subsequently passed all 105 tests and both SQLite
migrations were verified on the deployed server. Non-migration table counts
were unchanged and both databases passed integrity checks. Deployment records
and the isolated MySQL SQL/scripts are retained in
`/home/blizz/backups/rasa-net/20260912T174516Z-retail-mission`.
The focused MySQL run did not retain separate log files; its results were
returned in the research session's tool transcript. The combined candidate
build/test logs are preserved. See [the work log](retail-accuracy.md).

Downgrade limits are intrinsic to the old representation. Once a character has
multiple missions, the old single-column primary key cannot preserve them.
SQLite's generated downgrade rejects that conflict without discarding rows in
the tested transaction. Do not assume the same rollback behavior for MySQL DDL;
use a consistent pre-upgrade database backup when restoring the old schema.
Likewise, MySQL's old byte category column cannot preserve values above 255.
SQLite's empty category downgrade leaves stored values intact, but the old byte
application model cannot faithfully represent wider values. Do not silently
drop missions, truncate categories, or guess replacement values to downgrade.

No acceptance, objective progression, reward transaction, completed-history
semantics, or reconnect restoration was activated by this correction. The
existing two seed definitions remain unvalidated content. Passing these tests
establishes database behavior only; the two-character, client-visible quest
acceptance criteria at the end of this document are still outstanding.

## Older InfiniteRasa implementation

Inspected the public `InfiniteRasa/Game-Server` experimental branch at commit
`4a9ab5f1fcdf6a18ab6911c384189cc41ddae651`. It contains one mission script,
mission 429, in ten rows. This is a useful implementation reference but is not
an original retail server or a demonstrated retail packet capture.

Sources:

- [Pinned mission SQL](https://github.com/InfiniteRasa/Game-Server/blob/4a9ab5f1fcdf6a18ab6911c384189cc41ddae651/sql/gameserver_dev_Full.sql#L38466)
- [Pinned mission structures and constants](https://github.com/InfiniteRasa/Game-Server/blob/4a9ab5f1fcdf6a18ab6911c384189cc41ddae651/src/mission.h)
- [Pinned mission progression and serialization](https://github.com/InfiniteRasa/Game-Server/blob/4a9ab5f1fcdf6a18ab6911c384189cc41ddae651/src/mission.cpp)
- [Pinned NPC handlers](https://github.com/InfiniteRasa/Game-Server/blob/4a9ab5f1fcdf6a18ab6911c384189cc41ddae651/src/npc.cpp)

The script describes this sequence:

| Stage | Reference behavior | Evidence scope |
| --- | --- | --- |
| Offer | Mission 429 from old creature 13, Rogers | SQL and creature rows |
| Accept | Activate objective 5 | SQL `OBJECTIVE_ANNOUNCE` |
| Objective interaction | Speak to old creature 16, Dying Forean; objective 5, player flag 1 | SQL `COMPLETEOBJECTIVE` |
| Progress | Complete objective 5; reveal objective 4 | SQL final-state rows |
| Turn in | Old creature 17, Witherspoon | SQL `COLLECTOR` |
| Rewards | 1,000 XP; 250 credits | Emulator reference only; not retail corroborated |
| Metadata | Level 5, category 10000044 | Emulator reference only |

The C++ serializer hard-codes solo, not shareable, and not radio completable.
Those flags are implementation defaults, not sufficient proof for retail data.
It distinguishes objective statuses 0 unassigned, 1 incomplete, 2 complete,
3 failed, 4 inactive, and mission statuses 0 active, 1 success, 2 failed,
3 unassigned, 4 completed. Its mission-log limit is 30.

**Do not directly import the numeric commands into the C# enum.** The old header
assigns CompleteObjective=2, Collector=3, RewardItem=4, RewardXP=5,
RewardCredits=6, RewardPrestige=7, Category=8, Level=9, NoOp=10. Current C# uses
Collector=2, RewardItem=3, RewardXP=4, RewardCredits=5, RewardPrestige=6,
Category=7, Level=8, NoOp=9, CompleteObjective=10. An explicit conversion is
required.

C++ handles acceptance, talk objectives, and completion and emits
MissionGained (485), ObjectiveRevealed (494), and MissionRewarded (486).
It checks an active log entry before completion and removes it after reward.
However, it leaves character restoration as TODO, omits item rewards, ignores
selection/rating during completion parsing, and does not provide a complete
retail-safe authority/transaction model. Copying it verbatim would retain bugs.

## Content corroboration and contradictions

Indexed TaRapedia mission lists place River Recon under Rogers at Alia Das, and
Witherspoon at Lower Eloh Creek. They place Assemble With Lieutenant Perkins
under Cmd. Sgt. Price in Pravus Research Facility. These observations contradict
the current two test definitions; they do not establish rewards, prerequisites,
or exact 1.16.5.0 objective data. Full individual mission pages were unavailable
in this session. Sources: [Wilderness mission list](https://tabularasa.fandom.com/wiki/Wilderness_mission_list),
[Concordia mission list](https://tabularasa.fandom.com/wiki/Concordia_mission_list).

The indexed [DaOpa mission database](https://www.ellatha.com/tr/missionslist.asp)
also puts Assemble With Lieutenant Perkins in Pravus Research. Its front page
lists 98 mission records and labels the database as under development. Direct
retrieval returned HTTP 403, so no detailed rewards or NPC relationships were
established there.

The local/old creature crosswalk for the candidate River Recon route is:

| Identity | C++ ID | Local ID | Local spawn/context | C++ spawn/context |
| --- | --- | --- | --- | --- |
| Rogers | 13 | 100 | (870,294.21,385.5), 1220 | (858.855,294.21,388), 1220 |
| Dying Forean | 16 | 112 | (306.76562,270.90234,436.76562), 1220 | (306.136,271.4,438.07), 1220 |
| Witherspoon | 17 | 101 | (870,294.21,388), 1220 | (501.5,238.8,214), 1220 |

Witherspoon currently stands beside Rogers at Alia Das. Moving him to the
Lower Eloh Creek area agrees with both the older emulator and indexed retail
mission list; exact coordinates still need client/map corroboration. Local
creature 112 has class 7035/name ID 9729, whereas old creature 16 uses
class 22637/name ID 0. Do not replace the local NPC wholesale based on its name.

## Strongest next implementation path

River Recon remains the best current candidate. The recovered original client
now confirms mission 429, objective text IDs for 5/4, and dialogue package keys;
see [River Recon client evidence](river-recon-client-evidence.md) for exact
records, cross-references, and the correction to Rogers's package. Its narrative
supports recon followed by reporting, but the examined client tables do not
contain the server's complete quest script. Before enabling it as a complete
retail quest, establish rewards, prerequisites, level/category/group and
radio/share assignments, the Dying Forean's exact identity and placement, and
objective transitions. Do not invent missing values or use the old 1,000 XP/250
credits as verified rewards.

The next code increment can establish mechanisms without guessing content:

1. Separate immutable mission definitions from per-character progress. The
   composite identity and scoped repository reads are now implemented; add
   objective-counter storage, supported completed-history semantics, lifecycle
   writes, and reconnect restoration when their required behavior is grounded.
2. Make offer/accept/objective/turn-in validation use the same server-owned
   eligibility state, including NPC identity, map and interaction range. Prevent
   duplicates and replayed rewards. Use a transaction for reward and completion
   persistence so a crash cannot duplicate rewards or consume a mission unpaid.
3. Add objective completion and mission rewarded packets, verify the corrected
   objective serialization in a running original client, and update nearby NPC markers
   when a player's state changes. Do not assume selection index and rating are
   booleans: the current C# completion parser says they are; C++ merely ignores
   them and supplies no corroboration of type.
4. Add the verified River Recon content and correct NPC placement as a distinct
   migration when source evidence is sufficient. Existing test mission 321
   should not become a Wilderness quest because its seed happens to be present.

Acceptance evidence for a working first quest must include two independent
characters, logout/reconnect at each stage, duplicate acceptance/completion,
wrong NPC/map/distance, a full log, reward-selection/inventory conditions,
correct on-screen objective text and markers, and exactly-once reward after
restart. Passing packet/unit tests alone will not prove that the client renders
or plays the retail quest correctly.
