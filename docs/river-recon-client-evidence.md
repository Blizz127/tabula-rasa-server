# River Recon and mission 321: recovered client evidence

Research date: 2026-09-12. Target: the final live game, preserved 1:1 under
`AGENTS.md`. The recovered package identifies its executable as 1.16.5.0, but
its community-upload provenance still needs an independent official manifest
or authenticated installation comparison. See [client-artifacts.md](client-artifacts.md).

The original client establishes mission names, objective text IDs, dialogue
package keys, and the meanings of their table fields. It does **not** supply a
complete executable server quest definition. In particular, the examined tables
do not establish rewards, prerequisites, numeric objective ordinals, trigger
conditions, or the state changes performed when a conversation is acknowledged.

## Source artifacts and method

The inspected members came from `trpython.zip` and `data/game.zip` inside the
preserved `Tabula Rasa 1.16.5.0` package. Extracted bytes, exact full text, and
derived records are retained outside Git at:

`/home/blizz/backups/rasa-net/research/20260912-client-artifacts/river-recon-selected/`

| Original member | SHA-256 |
| --- | --- |
| `generated/client/missionconversation.pyo` | `085df01e239a86fec0f6c9d1017d2af60edf54e47f6a235e4204ee8e84c3713b` |
| `generated/client/missionobjective.pyo` | `330f29153493c512a5a90074f4c0ae4ab45cb3f0d4dfc4b56764a0d95d232d5f` |
| `generated/client/objectiveconversation.pyo` | `55b75d207ab585ae16649e2899244be7251326dbe6db578cfeba245ffd208b12` |
| `generated/client/language/english/missiontextlanguage.pyo` | `06292bf592ce1ab241771ac3317125e8545c608781822fb345809b70f863bf9d` |
| `generated/client/language/english/creaturenamelanguage.pyo` | `1fa1ec6c098f3ae2cda4aa37a13ef268f6657bb4fd32cc4e3f5af21ae5126e6c` |
| `client/clientlanguagemanager.pyo` | `5973adcebed50145b3a114dcb085a394459cd35cbbcd5e53f23cb9d76fff5e3b` |
| `client/augmentations/npc.pyo` | `d54178e21d838417110afd39b85a40c08b906082965864aed480233e745f3894` |

These are Python 2.4 bytecode. The mission conversation table carries timestamp
1234238135 (**2009-02-10 03:55:35 UTC**), the objective/conversation enum tables
1234238136 (03:55:36 UTC), and English mission text 1234238145 (03:55:45 UTC).
These are build-header observations, not proof of an official release date.
The decompiler renders February 9 evening in the host's America/Chicago timezone;
that local rendering must not be relabeled as UTC. `extraction-manifest.json`
records each acquired member's archive, length, CRC32, and SHA-256.

No game module was executed or imported. `decode-literal-tables.py` uses `xdis`
to parse constants and a strict whitelist of literal-construction opcodes. It
rejects imports, calls, attribute access, branches, duplicate dictionary keys,
and unknown instructions. It reconstructs data literals only. Its output,
`decoded-literal-tables.json`, preserves each dictionary key/value and its
original `STORE_SUBSCR` bytecode offset. All 5,821 mission-conversation,
3,454 objective, 1,727 objective-conversation, and 32,988 English mission-text
rows were decoded, so the mission-specific inventories below are exhaustive
within these four tables.

`mission429-321-evidence.json` contains the selected entries and full original
English strings, including control characters, line breaks, and both text
variant keys. Those variants match for all selected mission 429/321 strings.
`client-consumer-bytecode.json` retains raw instruction records for the
functions used to interpret the fields. `uncompyle6` output and `decompile.log`
aid navigation; raw bytecode and literal records are the primary observations.

## Table meanings verified in the original client

Original source line numbers here are embedded in the bytecode, not line
numbers assigned to the decompiler's output.

| Table | Key and stored value | Consumer |
| --- | --- | --- |
| `missionconversation.lookup` | `(missionId, textTypeId)` → `[textId]` | `clientlanguagemanager._BuildMissionText`, original line 569. |
| `missionobjective.lookup` | `(missionId, objectiveId)` → `[(nameTextId, bodyTextId, counter0TextId, counter1TextId, counter2TextId)]` | `BuildObjectiveNameText` line 587 selects element 0; `BuildObjectiveBodyText` line 603 selects 1; `BuildObjectiveGenericCounterText` line 632 selects `counterId + 2`. |
| `objectiveconversation.lookup` | `(missionId, objectiveId, npcPackageId, playerFlagId, conversationType)` → `[textId]` | `BuildObjectiveConversationText`, original line 618. |
| English `missiontextlanguage.lookup` | `(textId, variant)` → `[text]` | `BuildModuleText`, original line 238, and the mission-text callers above. |

Mission text types are name 1, log 2, opening 3, finishing 4, reward 5, and
reminder 6. Objective conversation types are completion 1, reminder 2, choice
body 3, and choices 1–3 as types 4–6. These come from the original generated
`missiontexttype.pyo` and `objectiveconversationtype.pyo` enum assignments.

The NPC package ID is distinct from creature ID and creature-name ID.
`client/augmentations/npc.pyo::Recv_NPCInfo`, original line 79, stores the
package supplied by the server. `conversationwindow.HandleShowObjectiveCompletion`,
line 720, reads that NPC's package and combines it with the server-supplied
mission ID, objective ID, and player flag to select completion text. Its ambient
counterpart, line 887, selects reminder text. Merely finding a completion-text
key does not prove that talking to that NPC completes the objective: server
eligibility and resulting state transitions are separate facts.

## River Recon: mission 429

Mission-level text bindings in `missionconversation.pyo`:

| Text type | Text ID | Bytecode store offset | Original content establishes |
| --- | ---: | ---: | --- |
| Name | 741 | 7418 | River Recon. |
| Log | 742 | 7436 | Rogers requests investigation of the lost Forean patrol at Pinhole Falls, followed by a report to Witherspoon at Lower Eloh Creek. |
| Opening | 743 | 7454 | Investigate the top of the falls; Witherspoon coordinates AFS efforts farther down the river. |
| Finishing | 744 | 7472 | The patrol was killed and the Bane plan to advance on Alia Das; the speaker will inform Rogers. A reward is mentioned without identifying it or its amount. |
| Reminder | 6725 | 7490 | Scout the falls and report to Witherspoon. |

There is no `(429, REWARDTEXT)` row. That absence does not establish that the
mission has no rewards: the original UI displays reward objects supplied by
the server, separately from these localized texts.

The complete objective-text inventory is:

| Objective ID | Name/body text IDs | Counter label IDs | Bytecode store offset | Text meaning |
| --- | --- | --- | ---: | --- |
| 5 | 3885 / 6726 | `None, None, None` | 8087 | Recon Pinhole Falls; search the top of the falls for the missing patrol. |
| 4 | 1971 / 6727 | `None, None, None` | 8054 | Report to Witherspoon; report the Bane sighting. |

The narrative supports the sequence **5, then 4**. Table storage order is 4,
then 5 and does not establish gameplay order. The original `MissionLog` instead
sorts the objective list using ordinals sent by the server. Exact numeric
ordinals, reveal timing, required flags, timers, markers, and triggers are not
fields in this generated text table. The `None` entries mean no generic-counter
labels are assigned here, not proof that the server never maintained hidden
progress for these objectives.

Every objective-conversation binding for mission 429 is:

| Objective | NPC package | Player flag | Type | Text ID | Bytecode store offset | Content |
| ---: | ---: | ---: | --- | ---: | ---: | --- |
| 5 | 726 | 1 | Completion | 3886 | 5834 | A dying member of the patrol describes the ambush and asks that the human commander be warned. |
| 4 | 208 | 1 | Completion | 2039 | 5807 | The speaker expects the player's report after hearing from Rogers. |
| 4 | 116 | 1 | Completion | 2263 | 5753 | Redirect the player to Witherspoon with the Bane sighting. |
| 4 | 116 | 1 | Reminder | 2767 | 5780 | Redirect the player to Witherspoon after learning the patrol was killed. |

Player flag **1** is an exact dialogue-key observation. Its server-side storage,
scope, reset rules, and effect are not defined by this table. In particular, the
package-116 completion-text row must not be turned into an invented alternate
objective-completion route: its text expressly directs the player elsewhere.

## NPC package cross-reference and a concrete local defect

Package **116** is strongly established as Outpost Commander Rogers by several
independent rows in the same original client:

- Mission 508, objective 8, uses `(508, 8, 116, 1, COMPLETION)` → text 2244.
  Objective text 2240 names Rogers, body text 6758 places him at Alia Das, and
  mission log text 1301 tells the player to deliver the survey disks to him.
- Mission 1392, objective 1, uses package 116 → text 12009. Its objective body
  12008 explicitly identifies Rogers in the Alia Das command tent.
- Mission 1393, objective 1, independently uses package 116 → text 12017 and
  objective body 12016 identifies the same NPC and location.

Package **208** is strongly established as Witherspoon by River Recon's report
objective and mission 434's package-208 reminder. Mission 434 log/opening text
762/763 explicitly identifies Witherspoon as the officer sending that player
onward with his field report.

Package **726** appears in only one row of the entire objective-conversation
table: River Recon's objective 5 ambush report. The older emulator's creature 16
also used package 726, but that agreement is supporting evidence only.

The read-only local snapshot in `local-npc-crosswalk-before.json` observed:

| Local creature ID | Stored identity/name ID | NPC package before correction | Finding |
| ---: | --- | ---: | --- |
| 100 | Outpost Commander Rogers / 2973 | 726 | Incorrectly assigns the dying patrol member's dialogue package to Rogers. Correct package: 116, supported by the original-client cross-references above. |
| 101 | Field Sgt. Witherspoon / 3072 | 208 | Agrees with the original-client cross-reference. |
| 112 | Comment says Dying Forean / 9729 | No package row | Original creature-name text 9729 is **Dying Forean Prisoner**. This does not establish that local creature 112 is the recon NPC. |

Original name table keys 2973 and 3072 exactly name Rogers and Witherspoon.
Changing Rogers's package to 116 is therefore a high-confidence correction to
the local content association. It supplies the right client dialogue identity;
it does not activate a quest or establish exact NPC placement. The Dying Forean
class, name override, package assignment, and spawn still need reconciliation.

### Implemented correction and validation

[`NpcPackagePrelader`](../src/Rasa.DBL/Services/Preloader/NpcPackagePrelader.cs)
now seeds local creature 100 with package 116 and retains creature 101 with
package 208. The misspelling in the existing class/file name is unchanged.
`dotnet-ef 5.0.1` under .NET SDK 5.0.408 generated two empty World migrations,
following the repository's data-migration workflow in `docs/setup.md`:

- SQLite: [`20260912175916_CorrectRogersNpcPackage`](../src/Rasa.DBL/Migrations/SqliteWorld/20260912175916_CorrectRogersNpcPackage.cs).
- MySQL: [`20260912175935_CorrectRogersNpcPackage`](../src/Rasa.DBL/Migrations/MySqlWorld/20260912175935_CorrectRogersNpcPackage.cs).

Their data operation changes `npc_package.package_id` to 116 only where `id`
is 100 **and the existing package is 726**. It does not insert missing rows or
alter other NPCs, comments, coordinates, quests, or rewards. Both model snapshots
were unchanged. `Down` intentionally does not restore package 726: doing so
would also overwrite already-correct rows and fresh seeds that the conditional
`Up` never changed. Use a pre-change backup if restoration is needed.

Seven focused [`NpcPackagePersistenceTests`](../src/Rasa.Test/NpcPackagePersistenceTests.cs)
passed on isolated SQLite in approximately three seconds. They execute the
actual preloader operations and each provider migration's operations. They
verify the fresh Rogers/Witherspoon mappings; correction of old package 726;
preservation of existing 116 and an alternative fixture value; preservation of
Witherspoon and a different NPC using 726; and repeated application without
further changes. Testing the MySQL migration's portable SQL in SQLite is not a
substitute for the additional MySQL check.

The generated MySQL migration SQL also passed three fixture cases on an
isolated MySQL 8.4.11 database: initial Rogers packages 726, 116, and 999 yielded
116, 116, and 999 respectively. The other NPCs and comments remained unchanged,
and each case recorded the applied migration. This was a focused data-migration
test, not a claim of full gameplay verification. The disposable database was
removed after validation; this research step made no live database edits.

The combined candidate subsequently passed all 135 tests and was deployed.
The live World migration record and package rows were checked: Rogers 100 now
uses 116 and Witherspoon 101 retains 208. Backups, integrity checks, and deployment
details are in [the work log](retail-accuracy.md).

Logs and reproducible fixture material are retained in the research directory's
`validation/` subdirectory: `generation.log`, `focused-sqlite-tests.log`,
`mysql-script-generation.log`, `mysql-rogers-up.sql`, `verify-mysql.py`, and
`mysql-validation.log`. `source-hashes.json` records the validated repository
files. Deployment and client-visible verification remain separate checks.

## Assemble With Lieutenant Perkins: mission 321

Mission bindings are name 15, log 14, opening 16, finishing 13, and reminder
6521, at `missionconversation.pyo` store offsets 326, 344, 362, 380, and 398.
There is no reward-text row. The original log says Command Sergeant Price wants
the player to help repel Machina near the battlefield entrance, then reassemble
with the lieutenant at the northwest fortification. The finishing text thanks
the player for helping hold the line and refers to an unspecified reward.

The sole generated objective is **310**, with tuple
`(115, 6522, 1711, None, None)` at store offset 200. Text 115 describes helping
AFS defeat Machina at the frontlines entrance. Body 6522 is an empty string.
Counter **0** has text label 1711 for Machina kills; no required kill count,
eligible creature classes, radius, or credit rules are present.

The two objective-conversation keys are `(321, 310, 105, 1, COMPLETION)` → 131
and `(321, 310, 105, 1, REMINDER)` → 3139, at store offsets 29 and 56. Both
texts urge the player to reach the frontlines and help the troops. Original
name-table keys 205 and 204 identify Cmd. Sgt. Price and Field Lt. Perkins.
The text supports those named characters; the examined tables do not provide
an explicit package-to-actor mapping for package 105 or a receiver package for
Perkins. Do not assign mission 321 to Wilderness NPCs because of the old seed.

## What remains before an authentic playable quest

The original enums confirm mission states active 0, success 1, failed 2,
not-assigned 3, completed 4, and objective states not-assigned 0, incomplete 1,
completed 2, failed 3, inactive 4. These labels alone do not define the server's
transition rules or the difference between success, completion, and reward.

Original `shared/gameconstants.pyo` sets the mission-log limit to 30 and lists
only missions 1990, 2010, and 2011 as non-abandonable. Neither 429 nor 321 is in
that client UI exception list. This is client behavior evidence, not proof of
all server-side abandon/reoffer rules. The client group enum is solo 1, duo 2,
group 3, raid 4; no per-mission group assignments are in these text tables.

`NPC.Recv_Converse` receives available missions and eligibility from the server.
`ConversationWindow.HandleShowMissionAvailable`, original line 561, unpacks
`(level, rewardInfo, offerVOAudioSetId, itemsRequired, objectives, groupType)`.
The mission log receives category, shareability, radio-completion flag, rewards,
objective ordinals, timers, counters, required flags, and indicators in server
packets. These consumers explain why the recovered client can display a quest
without containing all the server facts needed to reconstruct it.

For mission 429 the remaining content evidence needs are exact rewards and
selection choices, XP award rules, prerequisites, level, group/category
assignment, share/radio behavior, accept/reoffer/abandon rules, true Dying Forean
identity and position, objective reveal/completion transitions, and marker data.
The old emulator's 1,000 XP and 250 credits remain unverified amounts. For 321,
the Machina counter target and kill-credit rules are additional gaps.

Storage and serialization groundwork does not fulfill acceptance, objective
handling, transactional rewards, reconnect restoration, and per-character NPC
eligibility. A final validation must still play the quest with two independent
characters, reconnect at each stage, exercise wrong-NPC and repeated requests,
and compare the resulting dialogue, progress, markers, and reward with original
evidence. Keep the full preservation goal active.
