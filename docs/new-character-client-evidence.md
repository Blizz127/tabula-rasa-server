# New-character Recruit initialization

Research date: 2026-09-12. Target: the final live game before shutdown, under
`AGENTS.md`. This correction covers starting skill state and consistent creation
saves. The complete creation/boot-camp experience remains under reconstruction.

## Starting training

All five Recruit skills start at rank 1. The recovered original-client catalog
establishes their identities and ability mappings:

| Skill | Skill ID | Initial rank | Ability ID | Required Logos |
| --- | ---: | ---: | ---: | --- |
| Firearms | 1 | 1 | passive | none |
| Hand to Hand | 8 | 1 | passive | none |
| Motor Assist Armor | 19 | 1 | passive | none |
| Lightning | 49 | 1 | 194 | Power, ID 23 |
| Sprint | 165 | 1 | 401 | none |

Numeric ranks come from a dated community revision, corroborated by original
official descriptions and contemporary gameplay images. They are not a grant
table recovered from the client. The evidence is:

- TaRapedia **Skill**, page 26, revision **33141**,
  **2008-09-15T12:07:51Z**, explicitly distinguishes Recruit skills starting at
  rank 1 from other classes' initially untrained skills. Its respec description
  retains those five initial ranks. This revision follows the tutorial rewrite.
- **Recruit**, page 13, revision **33024**, **2008-09-12T18:12:12Z**, lists the
  same five skills and cites the official Recruit page.
- The [official Recruit page, archived 2008-12-26](https://web.archive.org/web/20081226103638id_/http://www.rgtr.com:80/game_intel/abilities/tier_1_recruit.html)
  describes basic firearms, light armor and hand-to-hand training, with Sprint
  and Lightning for Logos-sensitive recruits. It corroborates the skill set,
  but does not itself specify numeric ranks.
- The [contemporary boot-camp skill-window image](https://www.playtabularasaonline.com/images/starter-guide_bootcamp_007.jpg)
  shows a level-1 Recruit with Firearms 1, Sprint 1 and zero unspent training
  points. It shows only part of the skill list and has no verified client build.
- The [original-client skill audit](final-client-skill-evidence.md) joins all
  five skill IDs to class 1, their abilities and their Logos requirements.
  [Sprint's original action](sprint-client-evidence.md) is 401; 430 belongs to
  Called Shot. These are separate abilities.

The existing ledger's initial five-point allowance now accounts for these
five assigned ranks, leaving **zero unspent skill points at level 1**. The
ordinary training purchase path remains separate. Learning Lightning does not
grant Power Logos, assign a drawer slot, or bypass its ability-use requirement.
The exact later level/class bonus timing and tutorial/skip rewards remain open.

## Archive provenance and version conflicts

The dated wiki revisions were recovered from the
[Internet Archive wiki-history collection](https://archive.org/details/wiki-tabularasafandomcom),
file `tabularasafandomcom-20220702-history.xml.7z`, 4,312,586 bytes:

- SHA-1: `736745348498f28ae6ea92c2d25ec125cb9221ae`, matching archive metadata.
- SHA-256: `b21426bce3b92f215f3acba958675a2aaa609e2113cc2a9d5ac4b000c424d1a2`.

The XML is malformed near its end: mismatched tag at line 1,789,127, column 2.
Only 10,778 completely parsed pages were retained. Of those, 10,350 have a
revision before the exclusive cutoff `2009-03-01T00:00:00Z`. No malformed
remainder was silently repaired or treated as evidence. Revision dates belong
to the archived wiki records; the dump was acquired later, in 2022.

Original files, metadata, the reproducible parser, selected dated records and
bytecode-derived tables are retained outside Git in
`/home/blizz/backups/rasa-net/research/20260912-new-character/`.
The official December 26 Recruit HTML has SHA-256
`d31b0b10eb21289287b8524aa0a7c4a23e07c1f5aa99fc19a8055edc576ce909`.
Client provenance and independent-authentication limits remain those recorded
in [client artifacts](client-artifacts.md); original modules were only parsed
statically, never executed as game code.

The history also exposes an important tutorial version boundary:

- **Updates/2008-08-14**, revision **35870**, **2009-01-05T09:11:51Z**, preserves
  Deployment 11 notes describing a rebuilt boot camp with reversed progression,
  new missions, loot and spawns. It removes initial clothing choices in favor
  of Recruit outfits and unlocks skipping after another character completes it.
- **Bootcamp**, revision **32488**, **2008-08-29T06:24:19Z**, explicitly marks its
  old walkthrough as needing an update after the rewrite. Its old mission list
  must not be imported as the final live tutorial.
- A later edit date alone does not prove that an entire guide was updated. The
  October 2008 Beginners Guide still refers to earlier creation and tutorial
  content. Retained client strings likewise include obsolete missions.

The commented Recruit-rank initialization in
[the other emulator](https://github.com/InfiniteRasa/Game-Server/blob/4a9ab5f1fcdf6a18ab6911c384189cc41ddae651/src/manifestation.cpp)
corroborates implementation intent. Its SQL defaults are zero, and the clamp is
disabled, so it is not evidence of a working original creation sequence.

## Implementation and verification

Creation now inserts the character, appearance, five skill records, existing
starter items and first account lockbox tab within one database transaction.
Success is published after commit. Failure rolls back all these writes,
including a newly chosen family name. Creating a second character preserves
existing characters' training and the account's bank balance/unlocked tabs.

The starter pistol's HP was taken from template 17131 while granting template
145. Each starter item now uses its own class's maximum HP. The existing item
selection is unchanged pending the complete final-live loadout audit; this is
not proof that every item, quantity, placement or grant event is correct.

Seven handler integration cases use actual repositories and SQLite, including
successful reload, equipment requirements, initial points/abilities, preservation
of a previous character and bank, and injected failures saving skills, items,
inventory locations, bank initialization and appearance. All **54 focused
creation/training/persistence/requirement checks passed**. Full-image verification
and deployment are recorded in [the work log](retail-accuracy.md).

Existing saved characters are not globally reset by this change. The earlier
targeted training repair is recorded in [starter equipment](starter-equipment-research.md).
Any historical repair must compare the saved state before applying missing
initial ranks and must preserve earned training and unrelated progression.
The diagnosed level-1 character was subsequently reconciled under that guard:
only its missing Hand to Hand, Lightning and Sprint rank-one rows were added.
The persisted result matches the creation initializer and has zero unspent
points; all other character-database tables are unchanged.

Still needed for this first segment: complete initial item/equipment placement,
allowed appearance/race unlocks, first-login state and original starting point,
boot-camp completion/skip persistence and rewards, and observed client creation,
reconnect and tutorial playthrough. Passing server tests does not certify the
full segment as 1:1.
