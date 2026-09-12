# Character progression: recovered client evidence

Research date: 2026-09-12. Target: the final live game before shutdown, as
required by `AGENTS.md`. This audit uses the recovered package whose executable
identifies itself as **1.16.5.0**. Package provenance, selected ZIP CRC checks,
and the remaining independent-authentication gap are recorded in
[client artifacts](client-artifacts.md). Findings establish what this client
contains; retained text alone does not establish which server branches were
still active at shutdown.

## Class advancement protocol and eligibility

The client supports a trainer conversation followed by a separate class-choice
request. The exact path is:

1. `client/augmentations/npc.pyo`, `Recv_Converse`, original source line 116:
   the server's conversation dictionary contains `CONVO_TYPE_TRAINING` with
   `(bCanTrain, dialogId)`. Bytecode offsets 1124–1158 select that tuple and
   forward its values to `UI_SHOW_CONVERSATION_TRAINING`. `CanTrain`, source
   line 245, returns the received boolean (offset 57), not a local level test.
2. `client/ui/conversationwindow.pyo`, `HandleShowTraining`, builds the trainer
   text from `npctrainerdialoglanguage` and passes `bCanTrain` into the class
   preview link. `OnConversationChoiceSelected` forwards it to
   `UI_SHOW_TIER_SELECT` with the NPC's entity ID.
3. `client/ui/tierselect.pyo`, `HandleShowTierSelect`, source line 224, stores
   that boolean at offset 37. `OnAcceptTrain`, source line 396, requires the
   boolean, a living avatar, and `_CanTrainInClass(classId)` before sending the
   choice. The window's `Update`, source line 177, closes it if the NPC or
   avatar is missing/dead or the avatar leaves `IsInMissionShareRange(npc)`.
   This identifies the client range predicate, not its numeric distance.
4. `_CanTrainInClass`, source line 600, checks the living avatar and calls
   `gameuiutil.IsTrainableClass` at offset 61. That function compares the
   requested class's **immediate parent** with the current class. It does not
   use a per-rank level table or spend points. An ancestor or sibling is not
   an eligible advancement.
5. `client/gameui.pyo`, `OnChooseTierAdvance`, source line 1518, offsets 0–18,
   sends the actor method `SelectNewCharacterClass` with `(chosenClassId,)`.
   The request does not include an NPC ID, price, granted skill list, or level.
   A faithful server must retain and validate the relevant server-side state.

The established class tree and all 73 skill owners are in
[the skill audit](final-client-skill-evidence.md). Final-client English UI text
key `(5695, 1)` and `(5695, 2)` explicitly identifies **5, 15, and 30** as tier
levels at which the player visits a trainer. Its dictionary row starts at
bytecode 193251/193269 and is stored at 193268/193286; text constant index 10285.
The numeric key is 5695, not the constant index or the next nearby constant.

`manifestation.Recv_AvailableCharacterClasses`, source line 1200, stores the
server's class list at offset 6 and raises the tier-selection notification.
`Recv_TierAdvancementInfo`, source line 1212, initializes the same list on
load, also at offset 6. The inspected trainer window uses the server boolean
and immediate-parent check; do not incorrectly describe that window as checking
membership in `availableCharacterClasses` itself.

## Level gating and point awards

The client retains two relevant player messages:

| English language-table key | Original text meaning | Constant index; row store offsets |
| --- | --- | --- |
| `(663, 1/2)` | Level advancement waits for visiting a trainer and choosing a new class. | 1118; 20324/20342 |
| `(664, 1/2)` | Level/class advancement waits for spending all remaining attribute and training points. | 1120; 20360/20378 |

The matching names in the repository are `PmCharacterClassesAvailable` and
`PmCharacterClassesUnavailablePointsRemain`. Message 663 corroborates the
contemporary account of a tier advancement gate. **The presence of 664 is not
proof that its additional spending restriction was still enforced.** No final
server sender or authentic final-session capture for either message was found.
The exact pre/post-threshold level representation, XP carry-over processing,
and whether message 664 was obsolete remain open. Do not add a leveling stop
before a functioning trainer path and its rewards are reconstructed.

`manifestation.Recv_AvailableAllocationPoints`, source line 1161, receives
`(attributes, trainPts, skillPts)` and stores the server-provided values. The
skill total is assigned at bytecode 101. The skill window's `_ShowPumpPts`
starts with that received `actor.skillPoints` value and subtracts only pending
local purchases. Neither function reconstructs total points from level.

English mission text for **Training Day** supplies useful qualitative evidence:
key `(13609, 1/2)` says class training gives additional attribute/pump points
and that some new skills need at least one pump to use. Text constant 16308;
row stores 341732/341750. It gives **no quantities**. The final-client level-up
message, key `(45, 1/2)`, substitutes server-provided `attributePts` and
`skillPts`; it does not fix those numbers in client data.

The current server formula remains:

`2 × (level − 1) + 5 + 2 at each of levels 5/15/30 + 4 at level 50`,
less cumulative purchased-rank costs. Client code directly supports normal
incremental rank costs 1, 2, 3, 4, 5, but **does not prove this total formula**,
the initial five-point accounting convention, or awarding tier bonuses merely
on reaching a numerical level. These were not changed by this research pass.

[TaRapedia's Leveling Up article](https://tabularasa.fandom.com/wiki/Leveling_Up)
describes two skill points per normal level, two additional points upon class
training, a clone credit before training, and XP continuing to accrue while
level advancement waits for the trainer. This is supporting community evidence;
the fetched current page is not independently dated to the final live patch.
The original client corroborates the trainer workflow and tier levels, but not
every numeric or timing detail from that article.

## Signature grants and accounting

The original active catalog has eight signature skills, all capped at rank 1:

| Class | Class ID | Signature skill ID | Ability ID |
| --- | --- | --- | --- |
| Grenadier | 8 | 47 | 234 |
| Guardian | 9 | 92 | 305 |
| Sniper | 10 | 149 | 281 |
| Spy | 11 | 110 | 252 |
| Demolitionist | 12 | 20 | 137 |
| Engineer | 13 | 157 | 260 |
| Medic | 14 | 154 | 193 |
| Exobiologist | 15 | 156 | 176 |

These are verified in `skilldata`, `abilitydata`, and `gameuiutil`, with hashes
and table offsets in the earlier skill audit. The original skill window gives
signatures a distinct widget, omits purchase buttons and skips their button
update loop. `_DisplaySkillLevel` shows rank 1 only when the received skill
state says the skill is trained. Ability activation/dragging also requires the
server's ability state; the UI does not synthesize a signature grant locally.
This establishes a **separate server grant path**, not its precise award event
or point accounting.

[The Reanimation Wave article](https://tabularasa.fandom.com/wiki/Reanimation_Wave)
states that Exobiologist training unlocks its signature, subject to Logos,
without training/skill-point investment. That supports a free grant after
class advancement, and agrees with the client omitting purchase controls. It
does not by itself prove the final protocol representation or all eight
classes' server implementation.

Open facts needed before implementing the grant: whether rank 1 is inserted
at the advancement transaction or another event; whether the original points
ledger excludes signatures or compensates their nominal one-point rank cost;
how respec and cloning preserve/regrant signatures; and which packets update
existing and newly learned skill/ability state. The current `GetAvailablePoints`
subtracts one for a stored signature at rank 1. If a free signature is later
added without revisiting that ledger, it would consume one available point
unless separately compensated. **Do not silently add an automatic signature
grant or change this accounting from the community assertion alone.**

## Creation, boot camp, and cloning audit

`client/inputstate/charactercreation.pyo`, `OnCreateCharacter`, source line 83,
has three separate user methods: first-family `CreateCharacter`, ordinary
`RequestCreateCharacterInSlot`, and `RequestCloneCharacterToSlot`. The clone
call includes `(sourceSlot, destinationSlot, characterName, gender, height,
appearanceData, raceId)`; the method name loads at offset 70.

The class trainer's clone button requires a positive received clone-credit
count and requests logout for cloning. The character selection interface
selects an empty destination slot, then enters character creation with the
source slot. Original tutorial text keys `(5647, 1/2)` and `(5660, 1/2)` explain
logging out to clone and choosing a new first name, with optional appearance
and gender changes. Neither text defines the full copied-state list or awards.

Boot-camp skipping is a separate server-controlled option. The selection UI
offers it only for a character without prior logins/map context and when the
received `GetCanSkipBootcamp()` state permits it. `OnSelectCharacter` sends
`RequestSwitchToCharacterInSlot(slotNum, bSkipBootcamp)`. This proves the option
and its message shape, not its starting skill/Logos/XP rewards.

Read-only inspection of the current emulator found these gaps:

- `CharacterManager.RequestCloneCharacterToSlot` contains a commented-out
  implementation and performs no clone. Its commented copy list is emulator
  intent, not original evidence.
- `CharacterManager.InternalCreateCharacter` creates the database record,
  appearance and starter items; no initial skill-grant path was found there.
  `CharacterRepository.Create` sets class 1; the database model defaults to
  level 1 and zero clone credits. The exact original boot-camp reward sequence
  must precede any unconditional starter skill/Logos grants.
- `ManifestationManager.GainExperience` directly increments levels while XP
  meets thresholds, without checking class tier. It derives point messages
  from the existing formula.
- `SelectNewCharacterClass` exists as opcode 177 but has no implemented request
  handler in this audit. `AvailableCharacterClasses`/`TierAdvancementInfo` are
  named among unimplemented manifestation messages. `DebugChgPlayerClass`
  directly changes the class and is not the original trainer advancement path.

This pass changes documentation only. Implementing a faithful trainer requires
the request/conversation path, advancement transaction, reward accounting,
skill/ability updates, and clone behavior together; the recovered UI must not
be mistaken for recovered server scripts.

## Reproducible evidence and limits

All acquired code was **statically parsed or disassembled**, never imported or
executed as game code. External research directory:

`/home/blizz/backups/rasa-net/research/20260912-client-artifacts/progression-selected/`

`progression-artifact-manifest.json` records archive-member filenames, SHA-256,
lengths, embedded source paths, Python 2.4 magic, and header timestamps. Selected
new modules used above:

| Original archive member (slashes become hyphens in local names) | SHA-256 |
| --- | --- |
| `client/ui/tierselect.pyo` | `84ecd46e76db93b95c42921861462dd144e4a88965a6612692052ab19edd2657` |
| `client/augmentations/npc.pyo` | `d54178e21d838417110afd39b85a40c08b906082965864aed480233e745f3894` |
| `client/ui/conversationwindow.pyo` | `6e3f6ce004afef767fcbd25cacda453afaff70c6d8d0b5dd7d3fbc0ee3c5c9af` |
| `client/inputstate/charactercreation.pyo` | `60a2b2cf3d3ab57dd83ac743954cf1a20e4c07916284c6a914bcf8f0f21cbccc` |
| `client/inputstate/characterselection.pyo` | `15b0e3f85e095f213e09a1d50925faf3d3e51f5b777b5f7afdedf0197043c361` |
| `generated/client/language/english/uielementlanguage.pyo` | `db0b950f257c62aac9a508eabd99304b0ffdde5b6478326b382a90e5c5ed35ce` |
| `generated/client/language/english/missiontextlanguage.pyo` | `06292bf592ce1ab241771ac3317125e8545c608781822fb345809b70f863bf9d` |
| `generated/client/language/english/playermessagelanguage.pyo` | `b9b27bed2053b422d29069e1a8e97e26bba636512ae328d684bef16eae63fba8` |

The listed modules' header timestamps fall on **2009-02-10 UTC**. A module
timestamp does not date every retained message or establish server deployment.
Shared skill/game UI/manifestation artifact hashes are in the prior skill audit.

Method-level xdis output is preserved as `tierselect-gates.dis`,
`class-selection-send.dis`, `class-parent-gate.dis`,
`advancement-server-receivers.dis`, `npc-training-source.dis`,
`trainer-ui-forwarding.dis`, `creation-clone-send.dis`, and
`character-selection-send.dis`. Positions above refer to original bytecode
offsets and embedded source lines, not decompiler output lines.

`extract_literal_language_rows.py` recognizes only fixed dictionary-literal
instruction shapes after xdis reads the original marshal data. It parses
11,958 UI, 32,988 mission, 2,928 player-message and 640 trainer-dialog rows,
then saves relevant rows with exact keys, constant indices, and instruction
positions in `*-progression-literal-rows.json`. This avoids an observed
uncompyle6 defect that prints unescaped apostrophes in Unicode language strings;
those textual decompiles are unsuitable for blind parsing as Python. Duplicate
language keys ending in 1/2 are retained as separate original rows rather than
assigned an unverified meaning.

Confidence is high for the observed client data, message signatures and UI
branches; moderate for qualitative trainer/level-gating text; insufficient for
exact final grant amounts, signature accounting, complete clone copying, and
the retained message-664 restriction. Original final server scripts, a captured
advancement/clone transaction, or independent contemporary direct evidence is
still needed for those rules.
