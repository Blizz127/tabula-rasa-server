# Starter equipment and empty character skills — 2026-09-12

The final-live preservation target in `AGENTS.md` applies. This investigation
identified a saved-character problem exposed by the equipment eligibility
implementation; it does not establish original automatic starter grants.

## Confirmed requirements and current defect

The affected level-1 Recruit had no `character_skills` rows. The owned equipment
had the following requirements in the running world database:

| Template | Class | Minimum level | Required training |
| --- | --- | --- | --- |
| 145 | 6048 | 1 | Firearms (skill 1), rank 1 |
| 13126 | 15602 | 1 | Motor Assist Armor (skill 19), rank 1 |
| 13186 | 15662 | 1 | Motor Assist Armor (skill 19), rank 1 |
| 13156 | 15632 | 1 | Motor Assist Armor (skill 19), rank 1 |

None has a race requirement. These equipment checks do not require a quest or
Logos. Ammunition template 28 has no equipment-training requirement and belongs
in the ammunition inventory rather than an equipment slot. Requirement table
provenance and original consumers are described in the
[world equipment audit](world-equipment-client-audit.md) and
[equipment eligibility audit](equipment-eligibility-client-evidence.md).

`CharacterManager.RequestCreateCharacterInSlot` creates appearance and items,
but does not initialize skills. `CreateCharacterManifestation` loads persisted
skills through `MapChannelManager.GetPlayerSkills`; an empty table therefore
produces no trained skills. The original requirement checks correctly reject
this character's gear for missing training.

There is a separate creation defect: `GiveBasicItems` grants template 145 while
reading maximum durability from template 17131. The inspected pistol had
120 HP against its own maximum of 100. This is not the red-requirement cause:
the original condition predicate rejects zero condition, not excess condition.
The character operation below did not change item durability or placement.

## Authorized character update

At the user's request, the affected character received Firearms 1 and Motor
Assist Armor 1 under the existing normal-training rules. This consumes two of
the five available points and leaves three. It is a character training update,
not a free grant, global initialization change, or evidence of the original
tutorial award sequence. The original requirement tables remain unchanged.

A private .NET verifier used the deployed server's `SkillTraining.TryPlan` and
`EquipmentRequirements.Check` against consistent character/world snapshots.
It reproduced four missing-skill failures, validated the two-skill training
batch, and verified all four items after reading the saved update. Repeating
the same training request proposes no further changes. These checks verify
eligibility under the deployed rules, not an observed client equip interaction.

The update inserted exactly two rows in one transaction while game was stopped.
All other character-database tables matched their pre-update contents. Three
SQLite backups passed integrity checks. Private scripts, snapshots, validation
logs and service metadata are retained in
`/home/blizz/backups/rasa-net/20260912T212342Z-blizz-training/`.

## Initial-grant evidence remains incomplete

The contemporary [boot-camp walkthrough](https://www.playtabularasaonline.com/index.php?pg=starters-guide_bootcamp1)
shows the starter pistol being equipped and Sprint assigned before completing
the first tutorial mission. Its [skill-window screenshot](https://www.playtabularasaonline.com/images/starter-guide_bootcamp_007.jpg)
shows a level-1 Recruit, Firearms 1, Sprint 1, and zero displayed training
points. It does not show every Recruit skill or identify the client revision.

The [boot-camp bypass account](https://www.playtabularasaonline.com/index.php?pg=starters-guide_bootcamp_skip)
distinguishes the Lightning ability from the Power Logos required to use it.
These are supporting observations, not proof of the complete final-live
creation/skip reward sequence. Do not infer a universal five-skill grant or
award Logos from this character repair. The initial ledger and grant timing
remain open as recorded in the
[character progression audit](character-progression-client-evidence.md).
