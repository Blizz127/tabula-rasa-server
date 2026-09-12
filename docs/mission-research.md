# Mission implementation research — 2026-09-12

This is an evidence inventory, not a claim of retail mission support. No mission,
NPC, reward, or character database rows were changed during this investigation.
The target is the user's exact final-live preservation requirement in `AGENTS.md`.
Client 1.16.5.0 is the current emulator compatibility requirement; its identity
with the final live client still needs original-build evidence.

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
- `CharacterMissionEntry` uses only `character_id` as primary key; that schema
  cannot represent multiple missions for one character. Its repository `Get`
  ignores both parameters and reads every character's rows. Character creation
  reads those rows and creates an unused `missionData` dictionary; it does not
  restore them to `Manifestation.Missions`.
- `MissionStatusInfoPacket` exists but has no call site. Reconnect currently has
  no mission-log synchronization.
- `NpcMissionRewardRepository.Get(missionId)` exists but is not used by gameplay.
  No reward semantics or XP reward storage are wired into mission definitions.
- `MissionInfo.Write` serializes indicator position as X/X/X, discards
  `CounterDict` as `None`, and dereferences `IndicatorList` without a default
  initialization. These are concrete serialization defects to fix when adding
  objectives. The current empty objective lists hide them.
- `MissionConstantData.CategoryId` and `NpcMissionEntry.CategoryId` are bytes;
  the C++ reference uses a category ID of 10000044, which cannot fit.

Relevant local code: `Managers/MissionManager.cs`, `Managers/NpcManager.cs`,
`Managers/CharacterManager.cs`, `Managers/CreatureManager.cs`,
`Structures/MissionInfo.cs`, `Structures/MissionObjective.cs` under
`src/Rasa.Game`, and the mission repositories, structures, and migrations under
`src/Rasa.DBL`.

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

River Recon is the best current candidate because the internal mission/objective
IDs, a talk-based sequence, and all three NPC identities have a concrete prior
implementation and local counterparts. Before enabling it as a complete retail
quest, obtain matching client language/script tables or a retail-era capture to
verify mission 429, objectives 5/4, NPC conversation packages, rewards,
prerequisites, category, and radio/share behavior. Do not invent missing values
or use the old 1,000 XP/250 credits as verified rewards.

The next code increment can establish mechanisms without guessing content:

1. Separate immutable mission definitions from per-character progress; persist
   by composite (character ID, mission ID) identity with objective counters and
   completed history. Correct the unfiltered repository and add reconnect state.
2. Make offer/accept/objective/turn-in validation use the same server-owned
   eligibility state, including NPC identity, map and interaction range. Prevent
   duplicates and replayed rewards. Use a transaction for reward and completion
   persistence so a crash cannot duplicate rewards or consume a mission unpaid.
3. Add objective completion and mission rewarded packets, correct coordinate
   serialization, represent wide category IDs, and update nearby NPC markers
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
