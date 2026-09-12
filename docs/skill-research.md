# Skill training source audit — 2026-09-12

Target: the **final live game immediately before shutdown**, preserved 1:1 per
`AGENTS.md`. Client **1.16.5.0** is the current emulator compatibility requirement,
pending original final-build verification. This is a source audit, not a claim
that the current server implements every skill. A subsequent same-day recovery
of client files with embedded executable version 1.16.5.0 now verifies all 73
skill IDs and owning classes below, class ancestry, ordinary rank costs, and
signature rank caps. See [the original-client audit](final-client-skill-evidence.md)
and [artifact provenance](client-artifacts.md); those findings supersede the
earlier client-unavailable observations retained below.

## Firearms ID correction

**Use skill ID 1 for Firearms.** The recovered client now directly confirms this
ID in `generated/client/skilldata.pyo`; the earlier emulator comparison follows.

- [Rasa.NET commit 7f1fdd7769317572e9b5858d9c4c28368ce3dfdc](https://github.com/InfiniteRasa/Rasa.NET/commit/7f1fdd7769317572e9b5858d9c4c28368ce3dfdc)
  introduced `SkillId.cs` under the message “Removing some magic numbers”. It
  assigned `Firearms = 2`, but left `ManifestationManager.SkillIById` beginning
  with 1 and the inverse table mapping ID 1 to index 0 and ID 2 to -1. The diff
  gives no evidence of a deliberate protocol change.
- [C++ manifestation.h at 4a9ab5f1fcdf6a18ab6911c384189cc41ddae651](https://github.com/InfiniteRasa/Game-Server/blob/4a9ab5f1fcdf6a18ab6911c384189cc41ddae651/src/manifestation.h)
  explicitly defines `SKILL_ID_T1_RECRUIT_FIREARMS` as 1 and its index as 0. Its
  forward and inverse tables agree. This is the experimental branch HEAD
  inspected on 2026-09-12 (commit date 2016-08-18).
- The same C++ index maps to ability ID -1: Firearms is a passive training,
  so it should not be advertised as a usable ability.

The likely explanation is an enum transcription error. Correcting the enum
aligns it with the pre-existing tables; accepting both IDs as independently
trainable skills would introduce a second purchase and an inconsistent budget.
Existing saved ID 2 records, if any are discovered, need explicit reconciliation
before migration; the spelling of an enum alone does not rewrite persisted IDs.

## Class and skill membership comparison table

The IDs and names below are facts about the C++ header linked above, with class
IDs from its [manifestation.cpp](https://github.com/InfiniteRasa/Game-Server/blob/4a9ab5f1fcdf6a18ab6911c384189cc41ddae651/src/manifestation.cpp).
Names are normalized for readability; “signature” reflects its `SIG_` prefix.
All 73 header entries are accounted for. The recovered 1.16.5.0 client now
confirms every ID and owning class in this catalog. Its internal names can still
differ from displayed names; the decoded language tables should establish final
presentation text.

| Class ID | Class | Tier | Parent | Own skill IDs and header names |
| --- | --- | --- | --- | --- |
| 1 | Recruit | 1 | — | 1 Firearms; 8 Hand to Hand; 19 Motor Assist Armor; 49 Lightning; 165 Sprint |
| 2 | Soldier | 2 | Recruit | 21 Reflective Armor; 22 Machine Gun; 25 Shrapnel; 147 Rage |
| 3 | Specialist | 2 | Recruit | 14 Tools; 30 Hazmat Armor; 31 Leech Gun; 36 Ruin |
| 4 | Commando | 3 | Soldier | 24 Launchers; 28 Force Blast; 39 Graviton Armor; 77 Rushing Blow; 164 Scourge |
| 5 | Ranger | 3 | Soldier | 48 Stealth Armor; 54 Tactical Evasion; 55 Net Gun; 162 Spotter; 163 Fire Support |
| 6 | Sapper | 3 | Specialist | 57 Mech Armor; 58 Polarity Gun; 111 Crab Mines; 160 Hack; 174 Shield Extender |
| 7 | Biotechnician | 3 | Specialist | 34 Cure; 35 Reconstruction; 66 Bio Armor; 67 Injection Gun; 173 Bio Augmentation |
| 8 | Grenadier | 4 | Commando | 40 Propellant Gun; 47 Concussive Wave (signature); 79 Scatterbombs; 80 Tectonic Strike; 155 Sacrifice |
| 9 | Guardian | 4 | Commando | 23 Staff; 26 Reflection; 43 Conversion; 89 Vortex; 92 Shield Wave (signature) |
| 10 | Sniper | 4 | Ranger | 50 Torque Shell Rifle; 149 Crit Wave (signature); 150 Shredder Ammo; 151 Target Painting; 166 Called Shot |
| 11 | Spy | 4 | Ranger | 82 Blade; 102 Polymorph; 110 Cloak Wave (signature); 148 Traitor; 161 Polarity Field |
| 12 | Demolitionist | 4 | Sapper | 20 Explosive Wave (signature); 63 Reality Ripper; 113 Controlled Fission; 114 Self Destruct; 159 Explosive Nanites |
| 13 | Engineer | 4 | Sapper | 32 Turret; 121 Feedback; 157 Base Wave (signature); 158 Trap; 172 Bot Construction |
| 14 | Medic | 4 | Biotechnician | 37 Viral Conversion; 135 Disease; 152 Mind Control; 153 Resistance; 154 Regeneration Wave (signature) |
| 15 | Exobiologist | 4 | Biotechnician | 68 Hortimunculus; 72 Cadaver Immolation; 73 Reanimation; 136 Create Clone; 156 Reanimation Wave (signature) |

The class ancestry is corroborated by the [retail class overview](https://www.playtabularasaonline.com/index.php?pg=class-info_class-overview),
which describes retaining the skills of earlier classes and selecting tiers at
levels 5, 15, and 30. A later class therefore needs access to its ancestors'
skills as well as its own; checking only equality with a skill's class would
incorrectly reject inherited training. The tier level is not proof of each
rank's minimum character level.

## Source age and disagreements

The [older skills overview](https://www.playtabularasaonline.com/index.php?pg=class-info_skills-overview)
contains obsolete names and places Polarity Field under Ranger. It must not be
imported as a 1.16 specification. The [Deployment 1.7 account](https://www.tentonhammer.com/articles/patch-1-7-briefing)
describes Fire Support replacing Carpet Bombing, Spotter replacing Reinforcements,
and Tactical Evasion replacing Polarity Field, with Polarity Field moving to Spy.
This explains the disagreement and corroborates the C++ Ranger/Spy membership.
The [Ellatha Ranger table](https://www.ellatha.com/tr/class_skillslist.asp?Class=Ranger)
also lists the newer five skills. The [Ellatha Spy table](https://www.ellatha.com/tr/class_skillslist.asp?Class=Spy)
contains both Magnesium Flash and Polarity Field, illustrating that a maintained
fansite may retain replaced rows alongside their replacements.

[Ellatha's ability catalog](https://www.ellatha.com/tr/abilitieslist.asp?order=Class)
corroborates the header membership for Biotechnician, Commando, Demolitionist,
and Engineer on the retrieved first page. These are useful corroborating
observations, not a substitute for the client database. The
[Deployment 10 patch-note reproduction](https://www.ellatha.com/tr/news.asp?id=1210&title=Deployment+10+-+Patch+Notes)
also uses the newer ability names. It distinguishes rank from skill proficiency:
training more ranks can improve an effect at earlier ranks too. Ability behavior
must preserve that distinction when implemented.

## Rank costs, initial skills, levels, and Logos

- The C++ implementation and existing Rasa.NET table use cumulative costs
  `0, 1, 3, 6, 10, 15` for ranks 0 through 5. A purchase from rank 2 to rank 4
  costs 7 points, including the intervening rank. These are not minimum player
  levels.
- The recovered client caps all eight signatures at rank 1 and provides no
  ordinary skill purchase controls for them. The server now rejects signature
  increases through normal training. The original grant and accounting path
  remains unverified; see the client evidence document.
- The C++ player creation path has a commented-out block that would clamp each
  Recruit skill to rank 1. Its points calculation adds five points with the
  explanation that Recruit skills start at rank 1. These show implementation
  intent, but the disabled initialization does **not** establish the exact retail
  creation/tutorial award sequence. Do not replace tutorial progression with
  unconditional free Lightning/Logos solely on this basis.
- No authoritative per-skill/per-rank minimum player-level table was obtained.
  Do not infer rank requirements from gear levels or impose a guessed universal
  progression. A threshold of 5/15/30 for class promotion is a different rule.
- The recovered client supplies 54 ability-to-Logos sequences with numeric
  protocol IDs. Its ability-use checks consume those IDs, while the normal
  purchase buttons do not gate on Logos. Preserve that distinction; do not add
  skill-purchase prerequisites based only on ability-use requirements. The
  complete static skill/ability/Logos join is recorded in the client audit.

The [Skills window account](https://tabularasa.fandom.com/wiki/Skills_window)
describes previewing multiple changes before Accept, and removing a preview
before acceptance. It supports validating and applying the submitted batch
together. It does not authorize decreasing an already learned rank through
ordinary training; a later respec is a separate operation.

## Remaining evidence collection

Initial bounded checks found no extracted client tables or game client in this workspace,
`/home/blizz/Games`, `/home/blizz/Downloads`, `/mnt`, or `/media`. This is not a
claim that no copy exists elsewhere. The upstream setup guide's old forum link
did not yield a downloadable client in this audit. The old launcher's configured
index, `https://launcher.dahrkael.net/index.tri`, failed TLS negotiation.

[Dahrkael/TRRM](https://github.com/Dahrkael/TRRM) is a small open-source Tabula Rasa
resource manager and viewer. Its `TRData/PackedFile.cs` and `TRData/TRData.cs`
provide leads for reading packed client resources when a versioned client becomes
available. It does not include the game tables itself. An original manual is
indexed at replacementdocs, but retrieval returned HTTP 403; no manual rule is
claimed as verified here.

The subsequent [client acquisition](client-artifacts.md) supplied the missing
skill/class/Logos resources and their hashes. Next evidence needed: original
signature grants and point accounting, complete server-side effects, independent
client authenticity, and comparison of valid/rejected training in the client.
A successful server unit test cannot prove those client-facing rules or effects.
