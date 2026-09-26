# Starter equipment and empty character skills — 2026-09-12

## Final-week loadout correction — 2026-09-22

Creation now saves the observed initial equipment arrangement: Recruit Boots
122854, Legs 122855 and Vest 122856 already worn; boot-camp Pistol 122875 in
weapon drawer slot 0, with 20 loaded cartridges and 1,000 reserve cartridges
(template 28). The Equipment backpack starts empty. This replaces the emulator's
loose common Motor Assist helmet/vest/legs, generic pistol 145 and 100 cartridges.
The outfit uses the original preview's white tint. Existing character inventories
are not rewritten.

The retained final-week video `7Lrst9SG3pk` shows the first HUD at **224.800 s**
(A2-011: Pistol 20/1000), the empty Equipment backpack at **304.733 s**
(A3-019), and comparisons against the equipped Recruit boots, legs and vest at
**321.467**, **324.467** and **332.467 s** (A3-035/040/046). The pistol comparison
at **334.467 s** (A3-047) reads 88 physical damage, a 20-round magazine and
Firearms 1. Original class 29803 matches those values; generic class 6048 deals
55. Template 122875 maps to 29803 in the original `itemclass.itemTemplateItemClass`.
The video identifies item names and stats, so this template selection remains
an inference rather than an observed numeric template ID. All four comparison
tooltips show Not Tradeable and Not Sellable. Paired world migrations
`20260922110000_RecruitLoadoutFlags` correct those flags on the four templates.
Fresh databases receive the same correction through migration history.

These screenshots were inspected again directly from the retained video crops;
the online YouTube page could not be fetched during this pass. Their hashes,
original client table locations and all reconstructed fields are in
[the machine-readable loadout manifest](evidence/new-character-loadout.json).
The client class allows 50,000 cartridges per stack, so the 1,000 reserve fits;
one stack in the first consumables slot is inferred because that tab was not
shown. The denominator is confirmed to be reserve ammo: original
`weapondrawerwindow._UpdateAmmoDisplay`, source lines 896–900, bytecode 219–316,
formats current magazine / `GetCountPersonalInventoryItemsByClass(ammoClass)`.
That inventory function (source 1423, offsets 9–26 and 93–106) counts matching
stacks only in personal inventory; it does not include the loaded magazine.
Full initial durability and pistol/ammunition tint also remain labelled
estimates. The recording's final-week date is inferred from chat, and the exact
shutdown executable revision remains unverified.

`ItemEntry` does not copy `CurrentAmmo` when inserting an item. Creation therefore
saves the loaded magazine explicitly through the existing ammo repository method
inside its transaction. Tests compare persisted inventory slots, counts, ammo,
durability and color against the manifest, and separately compare the granted
templates with the real seeded client mappings, slot assignments, magazine size,
durability and stack capacity. Trade-flag migration tests apply both providers'
SQL to an isolated SQLite table and verify rollback and unaffected templates.

Cloning already used the same initial inventory helper and reset Recruit skills.
It now receives this kit and an initial Lightning/Sprint drawer. Applying the
new-character kit and drawer to clones is explicitly **inferred**; no recovered
clone arrival capture establishes the original cloned loadout. A complete
original-client creation, reconnect and equipment-swap playthrough is still due.

The final-live preservation target in `AGENTS.md` applies. This investigation
identified a saved-character problem exposed by the equipment eligibility
implementation. The historical repair below did not establish original grants;
the subsequent [new-character audit](new-character-client-evidence.md) recovered
dated evidence and fixes creation for future characters.

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

The creation path inspected during this diagnosis created appearance and items,
but did not initialize skills. `CreateCharacterManifestation` loads persisted
skills through `MapChannelManager.GetPlayerSkills`; an empty table therefore
produces no trained skills. The original requirement checks correctly reject
this character's gear for missing training.

There was a separate creation defect: `GiveBasicItems` granted template 145 while
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

## Initial-grant evidence and subsequent correction

The contemporary [boot-camp walkthrough](https://www.playtabularasaonline.com/index.php?pg=starters-guide_bootcamp1)
shows the starter pistol being equipped and Sprint assigned before completing
the first tutorial mission. Its [skill-window screenshot](https://www.playtabularasaonline.com/images/starter-guide_bootcamp_007.jpg)
shows a level-1 Recruit, Firearms 1, Sprint 1, and zero displayed training
points. It does not show every Recruit skill or identify the client revision.

The [boot-camp bypass account](https://www.playtabularasaonline.com/index.php?pg=starters-guide_bootcamp_skip)
distinguishes the Lightning ability from the Power Logos required to use it.
These were supporting observations, insufficient by themselves to establish
the complete final-live creation/skip reward sequence. A subsequent recovery
of the September 15, 2008 Skill revision explicitly establishes all five
Recruit skills starting at rank 1. The [new-character correction](new-character-client-evidence.md)
records this stronger evidence and now initializes all five ranks in creation,
leaving zero unspent points, and fixes the pistol's durability source. It does
not award Power Logos or implement the unresolved boot-camp skip reward path.

The same diagnosed character was then reconciled to that established initial
state at **22:10:59 UTC**. A stopped-game transaction required the exact known
level-1 Recruit and its two previously saved ranks before adding Hand to Hand
1, Lightning 1 (ability 194), and Sprint 1 (ability 401). Its available points
are now zero. Table hashes confirmed that only `character_skills` changed.
No inventory, Logos, quest, appearance or other character state was rewritten.
The tested candidate's actual initializer and equipment checker validated
the saved result against fresh snapshots. Private records are in
`/home/blizz/backups/rasa-net/20260912T220953Z-retail-creation/`.
