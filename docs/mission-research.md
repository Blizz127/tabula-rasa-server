# Mission implementation research — 2026-09-12

This is an evidence inventory, not a claim of retail mission support. The
persistence corrections below were validated in isolated databases; that work
did not change live mission, NPC, reward, or character rows or activate quests.
The target is the user's exact final-live preservation requirement in `AGENTS.md`.
The acquired executable identifies itself as 1.16.5.0 and supplies original
client mission structures; see [client artifacts](client-artifacts.md) for
provenance and final-distribution authentication limits.

## Server behavior as inspected on 2026-09-12

The acceptance, conversation, completion and persistence gaps listed here were
addressed by the [2026-09-13 mission-log pass](#mission-log-protocol-and-persistence--2026-09-13);
the content findings remain current.

`MissionManager.LoadMissions` loads only `npc_mission` definitions. The live
SQLite world contains two definitions, zero `npc_mission_reward` rows, and no
objective or mission-script tables. Both definitions match the checked-in
`NpcMissionPreloader` seed exactly:

| Mission ID | Comment | Giver | Receiver | Level | Group | Category | Share/radio |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 321 | Assemble With Lieutenant Perkins | 0 (unknown) | 0 (unknown) | 5 | 1 | 1 | false/false |
| 429 | River Recon | 100 | 101 | 3 | 2 | 2 | true/true |

Creature 100 is Outpost Commander Rogers; 101 is Field Sgt. Witherspoon. Both
`npc_package` comments are literally `test`. These are unsuitable as validated
retail quest definitions. The giver/receiver values above were corrected on
2026-09-13 against the retail client's own mission text — both previously read
`101 | 100`. See
[Mission 321 and 429 corrected from client mission text](#mission-321-and-429-corrected-from-client-mission-text--2026-09-13).

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

## Mission-log protocol and persistence — 2026-09-13

This pass implements the server side of the mission log and NPC objective
conversations that the rebuilt boot camp (and every later quest) depends on.
It adds **no mission content**: no mission, objective, conversation binding,
transition or reward row was inserted, and the two unvalidated seeds 321/429
are now withheld rather than offered. Evidence for the client contract is in
[boot-camp evidence](bootcamp-client-evidence.md#entry-skip-and-protocol-facts-used-by-the-implementation);
research artifacts are under
`/home/blizz/backups/rasa-net/research/20260913-bootcamp/client-code/`.

### Recovered client contract now honoured (proven original behavior)

| Client fact (1.16.5.0) | Server behavior now |
| --- | --- |
| `npc.pyo` `Recv_NPCInfo(npcPackageId)` is the only setter of `npc.npcPackageId`; `conversationwindow.HandleShowObjectiveCompletion` builds dialogue from it | `NPCInfo` (490) is included in every NPC's entity data when a package is known. Previously it was never sent, so no objective dialogue could render. |
| `Recv_Converse` reads key 6 `OBJECTIVECOMPLETE` as `(missionId, objectiveId, playerFlagId)` triples | Objective topics are listed under key 6 for objectives the player actually has incomplete, instead of an always-empty `ObjectiveChoice` entry. Keys 8, 9, 12 and 15 now serialize a value instead of a bare key. |
| Continue sends `CompleteNPCObjective` (431) `(npcId, missionId, objectiveId, playerFlagId)`; the client changes no state locally | New handler. The objective completes only when the mission is active, the objective is revealed and incomplete, the NPC's package and player flag match a binding, and the player is within conversation range. |
| `CompleteNPCMission` (430) and `RewardNPCMission` (540) send `selectionIdx`/`rating` as `None` or an integer | Read as optional integers; the earlier boolean reader rejected index 2 and collapsed 0/1. `RewardNPCMission` and `PerformNPCChoice` (497) are decoded and logged; their conversation types have no server data yet. |
| `MissionStatusInfo` (487) replaces the whole log; `ObjectiveRevealed` (494) replaces a mission entry; `ObjectiveCompleted` (492), `MissionCompleteable` (481), `MissionCompleted` (482), `MissionRewarded` (486), `MissionDiscarded` (483) carry the tuples in `missionlog.pyo` | Packets added with those exact arities and sent to the player's own entity, where the client copies `MissionLog` handlers. The log is sent on world entry and after dropship transfer. |
| `NPCConversationStatus` 0 removes the Converse action; the client stores the status data as mission ids | Status now reflects the individual player's log: completeable mission (4), then an incomplete bound objective (3), then an offerable mission (2). The distinct `MissionComplete` value replaces the earlier `ObjectivComplete` for turn-ins. Visible related NPCs are refreshed after each change. The priority among simultaneous states is an emulator choice. |
| `HandleShowMissionAvailable` sorts offered objectives by `(ordinal, objectiveId)` and plays `offerVOAudioSetId` for NPC offers | The dispense tuple carries each objective's ordinal instead of `None`. `offerVOAudioSetId` is still always `None`: no table stores an offer audio set and the loader never sets one. |
| `Recv_PlayerFlags` stores the value and `HasPlayerFlag` evaluates `id in flags` | `PlayerFlags` (710) sends a list (currently empty) instead of the integer `0xFFFFFFF`, which made every flag test raise. |
| `lootdispenser.pyo` `Recv_CanLootItems(isLootable, canLootPerItem)` later calls `.get()` on the second value | An empty dictionary is sent when nothing is lootable instead of `None`. |
| `shared/gameconstants.pyo`: `MAX_MISSION_COUNT = 30`, `NON_ABANDONABLE_MISSIONS = [1990, 2010, 2011]`, `MAX_CONVERSATION_RANGE = 5`; the offer window shows `ID_MISSION_MAX_COUNT_REACHED` and disables Accept at the cap | The server mirrors these client limits: it silently refuses an `AssignNPCMission` beyond 30 missions the client can see, refuses `AbandonMission` (392, now handled) for the three fixed ids, and refuses `AssignNPCMission`, `CompleteNPCObjective` and `CompleteNPCMission` from another map or beyond 5 m plus a 2 m emulator allowance for movement latency. `RequestNPCConverse` itself is not range-checked. The original server's replies to such requests are unrecovered; these refusals are emulator validation. |

### Emulator storage and rules (not original evidence)

- `character_mission` gains `change_time` (Unix seconds sent as the client's
  `changeTime`; existing rows default to 0). New `character_mission_objective`
  stores each revealed objective's original `missionobjectivestate` value.
  Acceptance writes the mission and its revealed objectives together; each
  objective completion and its reveals commit before any packet is sent.
- New world tables `npc_mission_objective` (id, ordinal, required, revealed on
  acceptance), `npc_mission_objective_conversation` (the client's
  `(mission, objective, npcPackage, playerFlag)` completion key) and
  `npc_mission_objective_transition` (objective completion reveals another).
  They are empty. The original server's script format is unrecovered; this
  layout only represents what the client protocol exposes.
- A definition is offered only if it has objectives, at least one required
  and at least one revealed on acceptance, a completion binding for every
  objective and every required objective reachable through transitions. The server logs the reasons at
  startup. Without the gate, the 321/429 seeds could be accepted and would
  persist as completed with no objectives or rewards. Their giver/receiver
  markers therefore disappear; this removes invalid content, it does not
  restore the original River Recon.
- `npc_mission_reward.type` had no rows or consumer; it is now read as 1
  credits, 2 prestige, 3 experience (amount in `credits`), 4 fixed item,
  5 selectable item. Completion commits the mission state, credits, prestige
  and experience in one save before notifying the client; level-ups then use
  the existing path. Item rewards are not granted: creating and placing items
  cannot yet share that save, so a full or unsuitable inventory could consume
  a mission without its item. Any item reward row therefore withholds the
  definition. Item rows are still validated (known template, an inventory
  category, quantity within the stack size, the template's quality) so the
  reward display is truthful once delivery exists. Rewards remain an evidence
  gap for every mission.
- The offer window lists the objectives revealed on acceptance, in
  `(ordinal, objectiveId)` order. The client renders whatever list it
  receives; which objectives the original offers listed is unverified, and
  this schema cannot express an offer list different from the initial set.
- Reward rows are normalized once at load and the same values build the
  client's reward display and the turn-in payout. A definition is withheld if
  a reward row has an unknown type, a non-positive amount or repeats a
  currency type. Payouts that would overflow a balance are refused before
  anything is written. Turn-in is also refused, and logged, when a mission
  already in a log has a definition that would no longer be offered; a
  definition with nothing required never becomes completeable.
- Definitions flagged radio-completeable or shareable are withheld: the client
  would show Radio/Share buttons whose requests have no server implementation.
  `AssignRadioMission` (408), `AssignSharedMission` (409),
  `CompleteRadioMission` (432), `DeclineSharedMission` (440),
  `RewardRadioMission` (541) and `ShareMission` (547) are decoded and ignored,
  because an undecodable request disconnects the client.
- On world entry, saved active missions are reconciled with the current
  definitions: objectives revealed on acceptance and transitions from
  completed objectives that the saved rows lack are added and saved, so a
  definition change cannot strand a mission; nearby NPC markers are refreshed
  afterwards. Saved missions whose definition no longer exists stay in
  storage, are not sent and do not count toward the 30-mission cap.
- Requests are accepted from players in game, or arriving by dropship once
  the destination map holds them. The pre-existing GM `.teleport` command
  still leaves the player's map fields stale, so mission requests after it
  can be refused by the map and range checks until the next login.
- Completed missions stay in storage with state 4 and are not offered again.
  Abandon deletes the mission and objective rows so the mission can be taken
  again; the D13.4 known issue's workaround ("get the mission again") shows
  re-acquisition after abandoning on live. The final client's text also
  describes some missions as repeatable and others as non-repeatable. This
  emulator has no repeatable flag yet, so withholding every completed mission
  is correct only for non-repeatable missions.

### Still missing for quests

Kill, use-object, item, timer and area objectives; objective counters and
indicators with recovered positions; failure and retry (for example mission
2005); radio and shared missions; repeatable missions; choice and
reward-only conversations; mission-offer voice-overs (`offerVOAudioSetId`;
sets 2773–2776 are a naming inference); item rewards delivered atomically
with completion; and every content row. The boot-camp gaps that block
content are ranked in [boot-camp evidence](bootcamp-client-evidence.md#gap-ranking-after-the-sweep).

## Mission 321 and 429 corrected from client mission text — 2026-09-13

The retail 1.16.5.0 client's generated tables were decoded and read to establish
what the two seeded missions actually say. Source: `data/game.zip` members
`generated/client/missionconversation.pyo`, `missionobjective.pyo` and
`objectiveconversation.pyo`, interpreted through `python/client/clientlanguagemanager.py`
in `trpython.zip`. Full analysis, extraction recipes and row counts:
`/home/blizz/backups/rasa-net/research/20260913-source-sweep/mission-tables/findings.md`.

### Table semantics established (all three are text-id indirection only)

- `missionobjective` — key `(missionId, objectiveId)`, value a one-element list of a
  5-tuple whose entries are `missiontextlanguage` ids: `[0]` objective name, `[1]` body
  (nullable), `[2][3][4]` generic counter labels for `counterId` 0/1/2. 3,454 rows;
  all 7,508 non-null values resolve as `missiontextlanguage` ids with zero exceptions.
- `missionconversation` — key `(missionId, textTypeId)` where `textTypeId` is
  NAMETEXT 1, LOGTEXT 2, OPENINGTEXT 3, FINISHINGTEXT 4, REWARDTEXT 5, REMINDERTEXT 6.
  It is **not** an ordinal or a state. 5,821 rows; **type 5 never occurs**; 1,168
  distinct missions carry types 1–4 and 1,149 also carry 6.
- `objectiveconversation` — key `(missionId, objectiveId, npcPackageId, playerFlagId,
  convoType)`, `convoType` COMPLETION 1, REMINDER 2, CHOICEBODY 3, CHOICE1/2/3 4/5/6.
  1,727 rows. REMINDER serves the *ambient* conversation
  (`HandleShowObjectiveAmbient`).

### 429 "River Recon" — giver and receiver were transposed (corrected)

LOGTEXT, `missionconversation (429,2)=742`, verbatim: *"Outpost Commander Rogers wants
you to search for signs of a lost patrol of Forean Rangers near the top of Pinhole Falls,
then report your findings to Field Sgt. Witherspoon at Lower Eloh Creek, south of Pinhole
Falls."* So the giver is Rogers and the receiver Witherspoon. The seed had `giver_id=101,
reciver_id=100`, i.e. the reverse.

Corroborated independently from `objectiveconversation`: package **116** says *"Get this
new info to Witherspoon"* (sender is Rogers) and package **208** says *"Rogers told me
you'd be reporting in"* (receiver is Witherspoon). Live `npc_package` maps creature
100 → package 116 and 101 → 208, so creature and package agree.

Tier: **`original`** for the LOGTEXT; the package → NPC identity step is **`inferred`**
(high confidence — two independent client strings plus the mission log agree).
Corrected to `giver_id=100, reciver_id=101`.

Objectives 4 and 5 are also present at `original` tier (`(429,4)=(1971,6727,…)` "Report to
Witherspoon."; `(429,5)=(3885,6726,…)` "Recon Pinhole Falls."). Note the ids are **4 and 5,
not 1 and 2**, which independently refutes any "objective id equals ordinal" inference.
They are **not** seeded yet — see the blocking gap below.

`category_id=2` resolves to "Instance (Arieki Communications Tower)", which is semantically
implausible for an open-world Pinhole Falls recon mission. No evidence exists for the
correct value, so it is recorded as **suspect/unknown and deliberately not changed**.
`level=3`, `group_type=2` (DUO), `shareable=1` and `radio_completeable=1` are likewise
not checkable against the client and are unchanged.

### 321 "Assemble With Lieutenant Perkins" — NPCs were borrowed from 429 (set to unknown)

LOGTEXT, `missionconversation (321,2)=14`, verbatim: *"Command Sergeant Price wants you to
help the AFS push back as many Bane Machina as possible near the battlefield entrance. Once
done, reassemble with the Lieutenant at the North West fortification."* The mission name
itself is "Assemble With Lieutenant Perkins".

The seed carried `giver_id=101, reciver_id=100` — Witherspoon and Rogers, who are the
Pinhole Falls NPCs of mission **429**. Tier: **refuted at `original` tier**.

The correct NPCs do exist as client names: `creaturenamelanguage` `name_id` **205** =
"Cmd. Sgt. Price" and **204** = "Field Lt. Perkins". But **neither has a `creature` row**,
and a `creature` row needs `class_id`, `faction`, `level`, `max_hp`, `run_speed`,
`walk_speed` and eight action columns — none of which any source supplies. Inventing them
would violate the no-guessing rule, so both fields are set to **0**, the established
unknown sentinel (`NoContentReferences.MissionGiver` already returns 0, and boot-camp
mission 1990 seeds `giver_id=0`). `MissionGiver`/`MissionReciver` are compared against
`creature.DbId`, so 0 simply matches nothing and the mission stays unoffered — fail-closed
rather than offered by the wrong NPCs in the wrong zone.

Objective **310** is present at `original` tier: `(321,310)=(115, 6522, 1711, None, None)`
— name "Help the AFS defeat the Machina at the Frontlines Entrance", empty body, one
generic counter `counterId 0` labelled "Machina Killed", plus dialogue at `npcPackageId
105` / `playerFlagId 1` / convoTypes 1 and 2. This is what would retire the startup gap
`Mission 321 is not offered, definition incomplete: no objectives`. It is **not** seeded
yet — see below.

### Blocking gap: three objective columns are server-authoritative and NOT NULL

`ordinal`, `is_required` and `revealed_on_accept` cannot be recovered from the client. They
arrive in the server's mission-log payload — `python/client/missionlog.py` unpacks
`(objectiveId, objectiveStatus, ordinal, timeRemaining, counterDict, itemCounters,
isRequired, indicatorList)`, and the dispense payload sends objectives as
`(ordinal, objectiveId)` pairs. Our `npc_mission_objective` declares all three **NOT NULL**,
so the 3,454-row objective skeleton cannot be loaded until either the columns are made
nullable or values are evidenced per mission. Per the reconstruction policy these stay
**explicit gaps** rather than defaults.

Also blocking end-to-end loading of `objectiveconversation`:
`npc_mission_objective_conversation` has **no `convo_type` column**, and its 4-column key
would collide — 202 groups / 789 of the 1,727 rows carry multiple convoTypes, so a naive
import would silently drop **34%**.

### Separately confirmed by the same pass

- **Mission 1990 is corroborated, not refuted.** The client holds exactly two objectives,
  ids **1** and **2**, both named "Approach the Eloh Hologram" with empty bodies, no
  counters and no objective conversations. Its `category_id=10000032` resolves to
  **"Instance (Bootcamp)"**, which upgrades that value to `original`. Ordinals, required
  flags, reveal flags and the transition remain at their footage tier.
- **All seven of the Ellatha boot-camp missions are absent from the final client.** This is
  table-level `original`-tier proof that the boot camp was rebuilt, and it validates the
  rule that pre-rebuild content must never stand in for rebuilt content. Ellatha remains
  usable for the rest of the game.
- **The client defines 1,168 distinct mission ids** (1,120 distinct names; ~1,128 real after
  31 test/placeholder and 9 "No title"). Three-way comparison: client 1,168 ⊃ TaRapedia 734
  name matches ⊃ Ellatha 83 of 98 exact matches.
- **Everything in `npc_mission` except `id` and `comment` is server-authoritative.**
  `python/client/gameuiutil.py` unpacks the server's `constantData` as `(missionLevel,
  groupType, missionCategoryId, bShareable, bRadioCompleteable, rewardInfo)` — an exact match
  for our columns. The client holds only the vocabularies (93 categories, 4 group types), so
  `giver_id`, `reciver_id`, `level`, `group_type`, `category_id`, `shareable`,
  `radio_completeable` and all rewards cannot be recovered from it.
- The three seeded `comment` values (321, 429, 1990) match the client's NAMETEXT strings
  exactly — upgraded to `original`.

### Decompiler caveat affecting all of the above

`uncompyle6` output for this corpus has at least one confirmed defect: `g_kGenderModules`
decompiles as `[4,5,6,7,8,9]` while the bytecode at offsets 373–394 loads six *strings*.
Every conclusion above was therefore re-verified against bytecode. **Literal constants in
the 47 decompiled `.py` files are unreliable; names and control flow are sound.**

