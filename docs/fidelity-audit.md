# 1:1 fidelity audit — every thing is what the client says it is

Owner goal (2026-09-19): *"make sure each spell, item, weapon is recognized as it is, compared from the game or
other sources. Lightning not being recognized as Lightning, crates not being set up as a crate, etc. Make sure
everything is 1:1."*

## Why this audit exists

Every boot-camp defect of 2026-09-17 to 2026-09-19 had the same shape: **the server treated something as a
different kind of thing than the client does.** None of them was caught by the test suite, because each test
checked the server against itself.

| Defect | What the client actually is | What the server assumed |
| --- | --- | --- |
| Supply crate window never opened | class 26714 carries only augmentation 64 `TreasureDispenser`, which has no `Recv_` handlers | that it could receive `LootDispenser` (50) window packets |
| Practice dummy could not be targeted (1) | `InertDestroyable` gets its target category only from `Recv_TargetCategory` | objects needed none; the packet was typed `Factions` and could not say `OBJECT` |
| Practice dummy could not be targeted (2) | `canBeDamaged` starts from `usabledata` (all `None` for 29365) and is set by `Recv_DamageInfo` | `DamageInfoPacket` existed and was never sent |
| Lightning did not count on the Target Dummy | the binding names action 194 | every destroying hit was reported as action 0 |
| Target Dummy unreachable | `adv_bootcamp.map` lanes 5.35 m apart, a sandbag pair flanking each target | a labelled-match position inside a sandbag emplacement |
| Target Dummy "can't see it" (2026-09-19) | the practice dummy stands in front of its sandbags, 1.45 m left of the lane centre; the Target Dummy now does the same one lane over (379.21, 186.8) | the lane fix put it on the centre line, which is inside the overlapping sandbag pair |

So the audit compares what the server believes against the **client's own tables and code**, and every
comparison that passes becomes a standing test, so the next mismatch fails CI instead of a play session.

## Results so far (2026-09-19)

| Area | Checked | Result |
| --- | --- | --- |
| Entity classes: server `entityclass` vs client `entityclass.pyo` | 15,823 shared rows | **2 augmentation mismatches** (20684 `MisCavesofDonn_DyingForean` client [52] / server [8]; 28699 `DELETEME_BROKEN`), **5 corrupted names** — an old import turned the word `None` into `0` (`UsableItemDispElohLogosNoneV01` → `…Logos0V01`, and four more); 15 server-only rows |
| Placed usables vs their class augmentations | 10 placements | crate, both dummies, corpse, bomb, wreck consistent; **mission 430's four mortars are undamageable** (below) |
| Spawned creatures: class has `Creature` and is targetable | 194 | **194 / 194 correct** |
| Item templates: class is an `Item`; weapons have a weapon row | 4,999 | crate set 5/5 and mission rewards 14/14 clean; **2,495 armour templates have no armour value** (below) |
| Entity classes, every column, against the decoded client table | 15,823 | mesh, collision role, target flag all equal; the 5 names and 2 augmentation lists **fixed** (below) |
| `armorclass`, `weaponclass`, `itemclass` against the client's tables | 3,377 / 2,946 / 9,115 | **identical**, every column |
| Item template → class, skill / race / attribute requirements | 30,225 / 19,564 / 70 / 5,293 | **identical** |
| Equipable class → slot; slot numbers | 6,935; 28 | **identical** |
| Server enums on the wire against the client's constant modules | 16 enums | values identical except **`CharacterState.ToolReady` 22, client 24** (fixed); method ids 978/978, action ids 284/284, augmentations 64/64, creature flags 148/148 |

### Mission 430 "Mortar By Numbers" could not be completed — fixed

Its four launchers are placed as destroyables of class **7478 `ArchBaneGenObjMortarlauncherBaseV01`** — which has
**no augmentations and `target_flag` 0**: plain scenery to the client, which can neither target nor damage it.
The client has no destroyable mortar class (only 7478 and its `…Destroyed` variant 7479, both statics). The
client's creature-name table, however, has **"Bane Mortar" (8186)** and its Light/Heavy/Ultraheavy siblings: in
the original the mortars were **creatures**, like the turret emplacements (`Emplacement_Bane_Turret_Standard`,
Creature + Harvestable, targetable). The world seed has none, and the class the original used is not recovered.

TaRapedia's "Mortar" page (rev. 18949, 2007-12-08) settles the kind: *"An automated Bane mortar turret. Like all
fortifications, these have exceptionally heavy armor for their level … they regenerate armor at a rapid rate."*

No client class names a mortar creature, and no class of any kind reuses the launcher meshes (28988, 19367). The
nearest evidence for a reconstruction: `Emplacement_Bane_Turret_Standard` (7482, Creature + Harvestable,
targetable) uses mesh **19366**, next to the destroyed launcher's 19367; the client has a creature weapon class
`Weapon_Creature_Bane_Mortar_Launcher` (10604); and the name "Bane Mortar" (8186). A creature of class 7482,
named 8186, carrying weapon 10604, at the four launcher positions — **built** (`WildernessMortarCreature`,
OD-55, pending owner review): level 5 (the mission's), 1,500 hit points (the world's only fortification creature,
AFS_Turret_Mini), stationary, 60 s respawn. The four objectives are kill bindings on the placements.

Building it exposed a runtime defect: a kill binding that names a **placement** never completed — the kill hook
compared creature ids only. That also blocked the boot camp's 1994/1 (Tizzik Gi). Fixed
(`MissionManager.KillBindingNames`, GAP-KILL-BINDING-PLACEMENT).

The mortars fire back (`WildernessMortarFire`). Their weapon attacks with action 411 `WEAPON_GROUNDTARGET`, which
on the client is a rocket-launcher attack and so an ordinary weapon attack at a target: windup 500 ms, recovery
954 ms and range 60 come from the client's own action arguments; the 16–32 damage is an analogue (OD-55).

### 2,495 armour pieces gave no armour — fixed

The worn-armour total was built only from `itemtemplate_armor.armor_value`, which has 15 rows, so every other
armour template added **0** when worn. Worn armour is now the client's own `armorclass.max_damage_absorbed ÷ 10`,
**truncated**, for every piece (`ManifestationManager.BodyArmor`).

The scale is observed: the boot-camp crate's gloves read **"Body Armor: 28"** (A3-034) and absorb 281. The
rounding comes from a transcribed original tooltip, "Pulsar Reflective Armor Gloves, Min Level 5, Body Armor: 73"
(TaRapedia): the level-5 Reflective gloves absorb 526, 631, 736 or 841, and only truncating 736 gives 73. The 15
seeded values, which round, are Infinite Rasa's (source sweep 2026-09-13 §5), not original, and are no longer
read. `min` and `max` are equal for all 3,377 classes.

### The boot-camp crate held the wrong armour — fixed

The footage tooltip for the crate's gloves reads Body Armor 28, Motor Assist Armor 1, **Min Level 1**. Exactly one
gloves template on the server matches all three: **15803** `Armor_T1_MotorAssist_V06_UNC_Gloves_01_to_02`
(absorb 281, regen 1 — the tooltip's "Regen Rate: 1 per sec" too). The seeded 13096 was the common row (23) and,
by the client's own requirement data, needs level 15; the seeded boots needed level 30. `BootcampCrateUncommonGear`
gives the crate 15803 and the rest of the level-1 uncommon band without a level requirement — boots 12209, legs
26879, vest 12208 (OD-54, pending owner review). Their own tooltips match the client classes; exact templates remain inferred because multiple
templates share those classes. See the [2026-09-22 frame audit](bootcamp-equip-audit.md) for the observations,
alternative templates and the limits of the earlier gloves-only reconstruction.

`BODY_ARMOR_DIVISOR` (1.5) in `shared/gameconstants` is not that divisor: the client never uses it, and it
matches the server's existing Body-to-armour bonus (Body ÷ 1.5 %).

### Entity-class drift — fixed

Migration `EntityClassClientFidelity` puts seven rows back to the client's table: four names an old import
corrupted by replacing "none", case-insensitively, with "0" (`UsableItemDispElohLogosNoneV01`,
`ArchElohLogosSignNone`, `ItemElohLogosNone`, and `PropEarthSignOnewayV01`, which lost its "nOne"); one trailing
space the import trimmed; **20684 `MisCavesofDonn_DyingForean`**, an NPC (52) to the client and a stateless switch
(8) here; and **28699 `DELETEME_BROKEN`**, which the client gives no augmentations. None of the seven is placed or
spawned, so nothing in play changes. The seed (`EntityClassPreloader`) carries the same values for new databases.

### Wire constants — one wrong, one misused

- `CharacterState.ToolReady` was 22; the client's `TOOL_READY` is **24** (22 is unused). Nothing assigned it yet.
- `ActorInfo.desiredPostureId` was sent the state's **type** (Control = 5, Posture = 1…). The client looks it up
  as a state id and acts only on `CROUCHED` (14); a `Normal` actor was announced with the id of `DEAD`. It now
  sends `Standing`, or `Crouched` for a crouched actor.

Pinned by `ClientConstantTests`. Names still differ where the value does not (the server's `Laser` is the client's
`LIGHT`, `Electrical` is `ENERGY`, `Physical` is `NORMAL`); only the number goes on the wire.

### Abilities

Lightning (194) and Sprint (401) are built from the client's own `actiondata.abilityData` rows: costs 25–150
power, windup/recovery/reuse 500/700/1200 ms, damage 180–240 then 240–300, damage type 13 (`ENERGY`) with a
`SONIC` (7) extra at rank 3+; Sprint's 3600/5000 s duration, 2 s drain interval and adrenaline costs. Both agree.

The client's action tables are now loaded whole (Ellimist's `474d1ce`, cherry-picked; every row checked against
the decoded client — 1,813 levels, 405 costs, 4,259 properties, no differences). From them the **class damage
abilities** resolve through Lightning's lifecycle with every number from their own row: Force Blast (158),
Shrapnel (178), Tectonic Strike (229), Vortex (231), Rushing Blow (302), and the Explosive (137) and Concussive
(234) Wave signature abilities. Each lands on its target, on everything within `RADIUS_AROUND_TARGET` of it, or
on everything within `RADIUS_AROUND_SOURCE`/`CONE_RADIUS` of the performer, and answers with one recovery in the
shape `damagebase.py` reads.

Their stuns (`STUN_CHANCE`/`STUN_DURATION` under the client's `STUN` effect 86) and knockbacks
(`KNOCKBACK_DISTANCE` under `KNOCKBACK` 8; the push direction and navmesh limit are inferred) are applied, and
`CONE_RADIUS` is used as what `client/targeting.py` shows it to be — the cone's angle in degrees, over the
ability's range (half-width inferred). Lightning ranks 4–5 now stun, 50% for 3 s, as their row says.

The two tier-2 effect abilities are built on their client contracts. **Ruin** (162) attaches `DECAY` (82), whose
own tooltip reads "Deals dmgMin - dmgMax damage every interval seconds": the row's damage is rolled every
`INTERVAL` for `DURATION` and sent as `GameEffectTick(effectId, [(target, rawInfo)])`, what `DamageOverTime.OnTick`
reads; pump 5's movement modifier slows the target (inferred as a percent). **Rage** (307) is a toggle: `RAGESOURCE`
(236) on the soldier pulses every `INTERVAL`, putting `RAGE` (235) on the soldier and, at the pumps with a radius,
the squad in reach; `RAGE` adds `DAMAGE_PERCENT` to their damage and `RESIST_MODIFIER` to their resistance rating,
which the server converts with the client's own rating / (rating + 50).

Tier 3 so far, each on its client contract (effect types, recovery shape and tooltip names from the client; the
numbers from its row):

| Ability | What it does here | Inferred |
| --- | --- | --- |
| Bio Augmentation (421) | +`EFFECT_MODIFIER` to `ATTRIBUTE_ID` on a friendly player for `EFFECT_DURATION_MS` | the amount is flat (tooltip has no %) |
| Scourge (380) | `DAMAGE_AMOUNT` to every hostile within `EFFECT_RADIUS` each pulse for `DURATION`, via `AnnounceDamage` | 1 s pulse |
| Shield Extender (446) | target and squad within `EFFECT_RADIUS` lose `EFFECT_MODIFIER`% of each hit to a pool of `EFFECT_DAMAGE_MAX` | 1 s pulse |
| Reconstruction (188) | heal/HoT, ±Spirit, adrenaline, ±max health on the squad; damage/DoT on enemies in `RADIUS_AROUND_SOURCE` | — |
| Tactical Evasion (10000005) | cleared hate + mag flash; smoke screen (−ranged %); a delayed retreat to the cast point | the retreat fires when it runs out |
| Fire Support (387) | a strike after `DELAY_TIME_MS` on a point or target; stun or a napalm pool by pump | which strike effect goes with which pump |
| Cure (186) | cleanse, debuff guard for `DURATION`, resuscitation at `ATTRIBUTE_MAX_CHANGE`% beside the biotechnician | trauma is not a debuff it removes |

Tier 4, the same way (effect types, recovery shapes and tooltip names from the client; numbers from the row):

| Ability | What it does here | Inferred / not acted on |
| --- | --- | --- |
| Reflection (177) | `DAMAGE_PERCENT` of a hit of a reflected type back at the attacker; types accumulate over the pumps | — |
| Conversion (233) | +`DAMAGE_PERCENT` taken; `HEAL_PERCENT` of each hit heals the squad in `EFFECT_RADIUS` | — |
| Shield Wave (305) | each in radius absorbs `EFFECT_MODIFIER` (level-scaled) | — |
| Regeneration Wave (193) | +`ATTRIBUTE_PERCENT` regeneration for the squad | — |
| Resistance (386) | aura of `RESIST_MODIFIER` rating for the squad | — |
| Disease (246) | −attribute %, or no regeneration / no healing | creature attributes are shown, not felt |
| Viral Conversion (187) | the medic's virulent damage becomes the row's type | — |
| Sacrifice (385) | ±damage %, ±resistance rating, threat turns creatures on the grenadier | threat as turning (no threat list); held until used again |
| Self Destruct (267) | blast in `EFFECT_RADIUS` when it ends or is used again, then back to the mark | the user's own damage (not in the row) |
| Scatterbombs (232) | every hostile within scatter + burst | the reach as their sum; crit proficiency |
| Shredder Ammo (390) | +`DAMAGE_AMOUNT` of its type on weapon hits once per interval | 1 s interval |
| Called Shot (430) | marks; the next hit springs leg / arm / chest / eye / head | that the next hit springs it |
| Controlled Fission (381) | burst around the target after `DELAY_TIME_MS` | the damage threshold (not in the row) |
| Explosive Nanites (383) | `USE_COUNT` explosions, one per hit taken | `USE_DROPOFF` as % weaker each |
| Polarity Field (388) | negative rating on one type, the client's (100 − r)/100 | rating × pump |
| Feedback (298) | a marked creature takes damage each time it attacks | creatures neither heal nor use items |
| Reality Ripper (301) | pulls and damages every `INTERVAL` around its point | pull strength (halfway); the rip is not a destroyable object |
| Cloak Wave (252) | the squad is invisible to creature targeting until they act | speed reduction (not in the row) |
| Traitor (393), Hack (303) | the creature is AFS for `DURATION`, fighting its former allies | — |
| Mind Control (304) | flee / random / allies / assist, re-decided every `INTERVAL` | pump 5's spread to attackers |
| Cadaver Immolation (251) | a corpse bursts after `DELAY` | — |
| Reanimation (240), Wave (176) | biological corpses stand up on the user's side for `DURATION` | — |
| Paint Target (295) | `EFFECT_ARMOR_PIERCE_PERCENT` of each hit past the armour | cover and armour recharge have nothing to act on |

The last ten, built with **labelled stand-ins** at the owner's word ("build them labelled", 2026-09-19, **OD-56**):

| Ability | Stand-in | What is not original |
| --- | --- | --- |
| Turret (197) | AFS mini turret (creature 9), its shots set to the row's damage | the turret creature |
| Trap (384) | the same turret, −75% damage, drawing hostiles in `EFFECT_RADIUS`, bursting when it dies or ends | the turret creature |
| Bot Construction (262) | friendly Hominis Machina (creature 8) | the bot creatures (Flame/Rocket/Repair/Shield/Multi) |
| Spotter (389) | military-surplus soldier (creature 10) | the Pistoleer/Rifleman/... creatures |
| Create Clone (253) | human NPC of the player's gender wearing their appearance | the clone's attacks |
| Crab Mines (282) | no creature: seeks the nearest hostile within 30 m, bursts after 2 s | seek range, travel time, the mine as a target |
| Hortimunculus (185) | a plant where the corpse lay: heals the squad 5% a pulse, +`RESIST_PERCENTAGE`, decaying | heal amount; the plant as a target |
| Polymorph (392) | creatures ignore the spy for `DURATION` | the creature body and its attacks |
| Base Wave (260) | squad +`RESIST_MODIFIER` rating and 5× armour recharge (no variant needed) | — |
| Crit Wave (281) | squad +`EFFECT_MODIFIER`% crit chance (no variant needed) | — |

**Critical hits** now exist: 5% base + Spirit's 0.065% a point + Crit Wave, +50% against creatures and +25% against
players. The 5% and both multipliers are the client's own `shared/gameconstants.pyo` (sha256 `e0fc260a…`):
`BASE_CRITICAL_CHANCE = 5`, `CRITICAL_DAMAGE_MODIFIER = 1.5`, `PVP_CRITICAL_DAMAGE_MODIFIER = 1.25`, agreeing with
TaRapedia's "Critical Hit" page (2008-10-19). The same file has `SPIRIT_CRIT_DIVISOR = 15.0`, which the client never
reads; how the server used it is lost, so the Spirit share stays TaRapedia's 0.065% a point (1/15 would be 0.067%).
The page's per-type secondary effects and overkill are not built.

**Regeneration (GAP-REGENERATION, closed 2026-09-19).** Health, power and armour now regenerate every second, on the
client's constants in `shared/gameconstants.pyo`: `HEALTH_REGEN_PERIOD`, `POWER_REGEN_PERIOD` and `ARMOR_REGEN_PERIOD`
are 1; `IN_COMBAT_REGEN_MODIFIER = 0.2`. The client predicts regeneration itself (`elapsed × refreshAmount ÷
refreshPeriod`), and its `Actor.UpdateAttribute` puts the period back to 1 on every UpdateHealth/Power/Armor, so the
combat penalty rides on the amount: a fifth, rounded to the whole number the wire carries. The server adds the same
whole amounts each second, so a later update does not snap the bar back.

| Part | Value | Tier |
| --- | --- | --- |
| Period, all three | 1 s | original (client constants) |
| In combat | × 0.2 (`IN_COMBAT_REGEN_MODIFIER`), rounded | original constant; the rounding is the wire's |
| Armour amount | the worn pieces' `RegenRate` (the tooltip's "Regen Rate: 1 per sec") | original |
| Base Wave | `EFFECT_ARMOR_REGEN_MODIFIER` 500 = five times the armour recharge | original row; percent reading from Paint Target's "Armor Recharge: %s%%" (100, then 0) |
| Health amount | 2 × Regeneration Rate ÷ 100 a second | inferred (this emulator's existing formula) |
| Power amount | the same as health: "Regeneration Rate — Improves natural Health and Power regeneration" (`ID_TOOLTIP_ATTRIBUTES_REGEN`) | inferred |
| Combat lasts | 15 s after the last damage dealt or taken; `PlayerEnteredCombat`/`PlayerExitedCombat` toggle the indicator | inferred (no constant names it; follows `ellimist/development` 94b4485) |

Regeneration Wave's bonus now acts. Creatures still do not regenerate, so Disease's "stops regeneration" and Paint
Target's "Armor Recharge" (both only land on creatures) still act on nothing.

Chaff (175) is granted by no skill and is not a class ability.

### Weapons

Damage, clip size, ammunition class and damage type come from `weaponclass`, which is identical to the client's.
Range, windup, recovery and refire come from the server-only `itemtemplate_weapon` (2,444 rows): the client's
weapon classes carry `range_type` 0 and zero overrides in every row, so nothing client-side contradicts them.

## Still to audit

1. **Placed NPCs and their dialogue packages**: three objectives left (GAP-W3-UNBOUND-CONVERSATION-PACKAGE) — an
   analyser's readout, an unnamed villager and an Eloh artifact. GAP-W3-MISASSIGNED-PACKAGE and
   GAP-W3-TARAPEDIA-GIVERS are closed.

Done since: **class abilities** — all 53 abilities an active skill grants (`AbilityRequirements.RequiredLogos`,
the client's logos sequences) resolve through a server handler: direct damage, a game effect, or Sprint's own
path (checked 2026-09-19). The last ten are OD-56 stand-ins, described above.

### Dialogue: nine objectives left, and every NPC now wears a body (2026-09-19)

Field Lt. Brody carried package 550, the found commander's "Man, am I glad you showed up ... get back to Brody", so
mission 670 completed its first objective at Brody and could never complete its second. **DevilsDenFransisco** puts
Captain Fransisco in the Devil's Den at TaRapedia's coordinates with 550 and gives Brody his own 145, which also
binds 361/1. **QuestNpcDialogueBatch** then closed eight more, each at TaRapedia's own /loc, every reading within
0.5 m of the navmesh floor under it:

| Objective | Speaker | Where |
| --- | --- | --- |
| 382/1 | Recon Officer Tyler | Minos Caverns, 58, -30, -49 |
| 969/3 | Field Sgt. Kalinowski | Fort Haroun, 565, 232, 355 |
| 969/4 | Mohindra | Fort Haroun main room, 603, 224, 342 |
| 977/3 | Major Ston | Fort Haroun, 571, 224, 333 |
| 1040/2 | Beta Squad Cmdr Petrie | Martyr's Canyon, -147, 408, -882 |
| 1040/3 | Delta Squad Cmdr Locke | Maligo Creek, 164, 323, -771 |
| 1119/1 | Surveyor Miras | Kardash Atta Colony, 4, 273, -200 |
| 1183/1 | Sergeant Phenix | Phanin Research Facility, -11, -8, 8 |

Captain Reyko carried 1213, the Irendas console's own line, and now carries his own 1200, which binds 1113/7,
1125/1, 1310/1 and 20000003/7. That leaves 1112/1 waiting for the console, which no source places — the owner chose
that trade.

**Nine objectives remain, each blocked on a source, not on work:** 321 is not offered at all; 332's two delivery
points, 451/3, 977/2 and 836/1 name speakers no page places; 442/2 is an analyser's readout, and the Duncan already
in the world speaks for another mission (one creature carries one package); 1186/1 is an Eloh artifact, not a
person; 1112/1 is the console.

**Every created NPC was naked and headless** until 2026-09-19: entity class 3846 `NPC_Human_Swapset_Male` is a body
assembled from clothing pieces, and fifty of them carried no `creature_appearance` rows. Each now wears a shipped
analogue set — Rogers' officer set, Dr. Munson's for the doctors — and a schema test fails if a 3846 creature has
none (GAP-NPC-BODY; the footage's own clothing is still unmatched).

### Against TaRapedia, everything we seeded (2026-09-19)

Every NPC and mission the content work created, checked field by field against its TaRapedia page.

| Checked | Result |
| --- | --- |
| NPC level, 69 with a page | **1 differs**: Outpost Commander Rogers, 20 here against the page's 5 — ours is measured from footage (B3-020), the page is a 2007 transcription, and the final state is the target. Kept. |
| NPC position, 69 with a page | **1 differs by more than 8 m**: Rogers again, 14.3 m — ours is measured from the same footage; the page's point is where the world seed's own (never-spawning) Rogers sits. Kept. |
| Mission experience, 77 missions | **all match** |
| Mission credits | **all match** |
| Mission giver and reward giver | **41 differ**; all corrected — 18 moved to an NPC already standing, and the other 23 name NPCs that are now created |

The client's tables record no giver or receiver — that is why these were reconstructed — and each was taken from
the NPC whose dialogue package completes an objective, or from the NPC the last objective's text names. Neither is
the same thing as the hand that gives the mission, and TaRapedia's infoboxes record both directly. **TarapediaGiverAudit**
moves eighteen: Proving Justice, Proving Conservation and Proven Conservation to Wardens Lagori and Kahlee and Ranger
Urialia; A Father's Goodbye to Information Spec. Saviours; Unity Among Men and Elixir Vitae to Dr. Eleanor Corman; A
Visit To The Elders to Council Advisor Todae; Childhood's End to Council Luminary Doyan; Find Me A Rock to Major Ston;
Incommunicado to Colonel Bosley; A Tale of Two Brothers and Key Information to Surveyor Miras; Security Threat to
Engineer Tralos; Into the Facility to Sergeant Phenix; Conscientious Objector to Warrior Apirka; Gun Control - Part IV
to Retread Lou; and Too Close For Comfort to Outpost Commander Rogers, whose row named Council Elder Moawi although its
own provenance line read "TaRapedia's infobox names Outpost Commander Rogers as the giver".

**TarapediaMissingNpcs** then created the twenty-four NPCs the other mismatches name, so every mission we seed is now
given and turned in where TaRapedia says. Eighteen stand at their page's own /loc, and each of those readings lands on
the navmesh floor of the zone the page names — which settles the map as firmly as the zone does: Retread Vincent's
reading only matches the Marshes, and the Computer Access Terminal's x and z sit twelve metres from Surveyor Miras in
the Kardash colony while its y matches no floor, so the colony's floor is used.

Six have no page at all and are **labelled guesses**, at the owner's word, placed where the mission's words and the
client's own markers put them: Field Lt. Perkins at the north-west fortification of the Pravus instance, Base Guard
Kapler at the Hydro Plant control point, Lieutenant Seguine in Nidu Dav (the village Receptive Liaison Brice's /loc
locates), Line Capt. Dobbs at the Thoria Das waypoint, Field Lt. McMurray at the Central Trench waypoint (the Western
Trench is the other candidate) and Rohish at Fort Condor. Each says so in its own row.

Two are machines rather than people — the Operations Mainframe at the Viro Relay Tower and the Computer Access
Terminal at Research Outpost Alpha — on `UsableNPCCormanComputerV01`, a computer that carries the NPC augmentation and
so can be spoken to. The terminal is what mission 1112 "Into The Hive" had been waiting for.

Six more dialogue packages are bound with them, leaving **three** objectives open: 442/2 (an analyser's readout, and
the Duncan in the world speaks for another mission), 451/3 (a villager at Dagdha's Urn no page names) and 1186/1 (an
Eloh artifact, not a person).

### Merged from InfiniteRasa (2026-09-20)

`origin` is InfiniteRasa/Rasa.NET, which this branch forked from in December 2023; it has one commit since (PR #95,
Ellimist's work) against our 309, and a straight merge conflicts in 94 files because both sides rewrote combat and
content. Taken piece by piece instead, and only where upstream is better:

| Taken | Why |
| --- | --- |
| Three stability fixes | the auth server's double teardown, a socket receive race on the login hand-off, and a pooled-buffer leak on every Alt+F4 |
| Map markers | 307 of them; the map's Acquired / Not Acquired dots had never shown anything |
| Minions | the nine subordinate commands, carried here by Bot Construction, Spotter and Create Clone (OD-56), which upstream had no abilities to summon |
| Crafting recipes | 160 recipes, 353 inputs, and fabrication at the Kraftwerks stations |
| Item templates regenerated | 30,225 rows priced from the item class's loot value, against 4,985 stubs priced at a credit each; the prices are analogues since 2026-09-26 (OD-96), because the rule was fitted to InfiniteRasa's own emulator dump |
| Service NPCs, mission NPCs, class trainers, bosses | 324 + 177 + trainers + 62, at the client's own markers; the 38 per-class trainers were retired on 2026-09-26 as pre-D12 (`SingleClassTrainers`) |
| Skill level requirements | a level 1 recruit could train a tier 4 skill and use that class's abilities |
| Dropship pads, logos shrines | the Crucible pad had no position at all; two shrines had no stone |
| `tool_type` | every row carried 15, which the client's table does not have, so tooltips printed a damage line on tools and reload modifiers did nothing |

**Left alone, and why.** Upstream's `MainLoop` clock is wall-clock time; ours is monotonic, so timers cannot jump.
Its weapon heat retune calls its own numbers "a working default, not a reconstruction", and ours are a placeholder
too — one guess is not worth swapping for another. Its `AbilityManager` predates our ability work. Its four
placeholder bot creatures are invented by its own account, and the real abilities carry minions here.

**Held back from its data, to protect ours.** The 25 mission NPCs it places at a town's map marker that we had
already placed at TaRapedia's own /loc — ours are the more precise, and a test now fails if any upstream NPC shares
a name with one of ours. The three bosses and one service NPC it puts inside the boot camp, which is per-character
instanced content here with its own reconstructed cast.

**Superseded of ours.** The 14 item templates we seeded carried prices of 0 (GAP-W1/W2-ITEM-PRICES) and
analogue flags; upstream's rows are derived from the item class's loot value, matching all 188 templates the C++
server dump prices, and its flags are the ones that dump and the original server table agree on. **Corrected
2026-09-26:** that "C++ server dump" is InfiniteRasa's emulator, not the original server, so matching it proves only
how the emulator priced items. The prices are analogues (OD-96) and the two gaps are open again; see "Class trainers
and the economy's lineage (2026-09-26)" below.

### Creature flags — fixed

`CreatureInfo` was sent an empty flag list, and the harvest code read class flags that nothing loaded. Ellimist
had reconstructed them on `ellimist/development` (0e2a53c: species from the class name, biological or mechanical
checked against each species' harvest parts); only the harvest half had reached this branch. That commit is
cherry-picked, and creatures are now introduced with their class's flags.

## Packet contract — first pass (2026-09-19)

Every `Recv_` handler the client defines on the augmentations our spawned things carry, against whether the
server constructs that packet anywhere. **Unverified list**: the check matches `new <Name>Packet` by name, and at
least `StateChange` is a false positive (creature death does send it), so each entry is confirmed by hand before
anything is built.

| Client module | `Recv_` | Sent | Never constructed |
| --- | ---: | ---: | --- |
| physicalentity | 14 | 5 | CallGameEffectMethod, ExamineResults, GameEffectAttachFailed, GameEffectTick, GameEffectUpdateTooltip, GameEffects, PerformObjectAbility, ServerSkeleton, WorldPlacementDescriptor |
| actor | 50 | 36 | ActionBlockChange, ActionReuseTimerRestarted, AnnounceMapDamage, DeadOnArrival, MadeDead, PostTeleport, SetTrackingTarget, StateChange*, StateCorrection, TargetId, TeleportFailed, ToBePerceivedModifier, ToPerceiveModifier, WargameData |
| creature | 4 | 1 | Bark, BattlecryNotification, UpdateEscortStatus |
| npc | 4 | 3 | Train |
| usable | 11 | 7 | BlockInfo, LockToActor, UseInterrupted, UseInterruptible |
| inertdestroyable | 1 | 1 | — (`TargetCategory`, fixed 2026-09-19) |
| door | 3 | 3 | — |
| lootdispenser | 8 | 8 | — |

Checked by hand (2026-09-19):

- **`UpdateEscortStatus` — fixed.** The client's `IsEscort` and its overhead escort marker are set only by
  `Recv_UpdateEscortStatus(bIsEscort)`. A creature materialized from an escort placement (Ranger Milpas, mission
  1390) is now introduced with it.
- **`LockToActor` / `UseInterruptible` / `UseInterrupted` — built for Logos shrines (inferred order).** They mark a usable as in use by
  another player (`IsAlreadyInUse`) and play its channelling effect: `usabledata.specialFX` has one for 393
  classes, and of what the world places only the **Logos shrines** (state 81) carry one. The client code gives
  their meaning; the order comes from the client too: `ClanControlPoint.OnBeforeUseInterruptible` shows
  `UseInterruptible` starting a use, and `usable.py` plays it only for the actor `LockToActor` named. A shrine is
  now locked and plays its effect at windup, and is released on completion or interruption. Control points are
  left out, because their handler also takes a clan id. **GAP-USABLE-ACTOR-LOCK** (closed, inferred).
- **`BlockInfo`** is a no-op in the base usable. The game-effect and wargame handlers are unimplemented systems
  rather than mis-identified things, and belong to their own work.

## World placement sweep (2026-09-21)

Three live reports on 2026-09-20 — *"commander rogers is giving me quests to go to outpost commander rogers"*,
*"You received 30 3147"*, *"npc placement seems off too"* with a shot of an NPC standing in a cot at Alia Das —
and one question, *"ran into cellar arena not sure what that is"*. Each is answered by a measurement rather than
a guess.

### Every body against the floor — 126 corrected

`WorldFloorSweepTests` probes every `content_placement` **and every `spawnpool`** on every map we hold a navmesh
for, against the floor the 2026-09-17 batch measured (`navmesh surface − 0.276 m`). The earlier sweep read
placements only — 107 rows — so the 819 spawn pools taken in from upstream had never been measured at all.

| | probed | more than 0.5 m off | worst |
| --- | ---: | ---: | --- |
| content_placement | 153 | 13 | Lt. Galloway, 40.4 m (see the 2026-09-26 correction below: her TaRapedia Y was sourced, and the map was wrong) |
| spawnpool | 819 | 113 | Torcastra Prison vendor, +5.90 m |

Two are deliberate and stay where they are: the boot camp bomb mounted on the wreck hull, and the mini turrets
on their posts. Upstream's service NPCs are the bulk of the rest: vendors, field medics and hospital markers two to six metres in the
air, their Y evidently taken from a map marker rather than from the ground. Both A.F.S. Outpost Lexington
hospitals stood +4.72 m up; upstream's Boargar General spawn was 15.66 m *under* the Wilderness terrain.
Corrections are in `WorldFloorSweep`; no sourced X or Z changes, and the five rows whose column had to be
scanned are the ones whose Y was never sourced in the first place.

*Correction, 2026-09-26.* Two of those five were not what the sweep took them for. Field Lt. Bagby and Lt.
Galloway's TaRapedia pages give a Y as well as x and z, and their infoboxes say `Instance=Treeback Camp`: the
readings are coordinates in the Treeback Camp instance (1397), 0.12 m and 0.02 m off its floor, not in the
Palisades, where the same x,z is 34–40 m under the terrain. The sweep lifted them onto the overworld; the
`WorldDefectsFix` migration moves them into the instance at the wiki's own x,y,z (see "World-data defects"
below).

### Standing inside the furniture — 7 corrected

A cot is half a metre tall, so an NPC in one passes a floor check. `PropOverlapAuditTests` reads the furniture
the client's own map files place (`mapprops/`, 31,380 props over 72 maps, boxes rotated into each prop's frame)
and finds the bodies inside them. Sixteen were: nine are deliberate or original (mission 430's mortars stand on
the client map's own mortar launchers; two camp dummies on the gate catwalk; the original server's own vendor
behind her counter), and **seven were upstream coordinates that put an NPC inside a cot, a bed, a chair, an
armour mannequin, a console or a workstation** — including the Ranger Trainer at Alia Das, the report's own NPC.
They are pushed clear of the box and re-seated on the floor in `PropOverlapFix`.

### Against TaRapedia — 165 readings, 20 corrected

The wiki had only ever been read page by page for cited NPCs. This sweep reads its **complete pre-shutdown
history** (3,516 pages, each at its last revision before 2009-03-01): 430 carry the NPC infobox and 165 a
location table with coordinates.

| against this world | count |
| --- | ---: |
| within 25 m of ours — corroborated | 45 |
| same zone, further than 25 m | 24 |
| our body on a different map from the zone named | 3 |
| documented by the wiki, not in this world at all | 62 |
| zone not resolvable to one of our maps | 21 |

Twenty of the 24 are moved (`TarapediaNpcPositions`). They are upstream's mission NPCs, whose coordinates
upstream derived from the sentence of a mission, against a wiki reading that carries the revision date it was
set on: Brigadier General Beacham stood 57 m from the reading and away from Rogers, whom the same wiki puts
beside him; the Irendas Penal Colony group stood ~200 m east of the colony; Fort Defiance's staff ~100 m out.
The wiki's Y is a player's standing height, so only X and Z are taken and Y is the navmesh floor under them.

Four are deliberately **not** moved, and the reasons are worth keeping: pools 171 and 184 are the original
server's own spawns, and an original coordinate outranks a community transcription of one; Corporal Orton's
disagreement is a map change rather than a move; and Ranger Cyrida's wiki reading (38, 154, 48) has no walkable
ground under it while ours (−46, 154, 48) does — same Y, same Z, so the wiki's X looks to have lost its sign.

### The NPCs the wiki has and we did not — 51 created

Sixty of the 165 had no creature here at all, and the Ellatha pass had since created two of them. Fifty-one are
created in `TarapediaMissingNpcBatch`, across 19 maps: Field Officer Hogan and Medical Officer Mayes at Foreas
Base, the Temple of Paludos elders, the Penumbra Headquarters staff, Master Phanin in his research facility,
the Irendas lookout scouts, and the rest. 26 Human, 9 Brann, 7 Forean, 3 Thrax, 2 Thrax Grenadier, 2 Lightbender, 1 Caretaker, 1 AFS Mech.

Name and coordinates are the wiki's, each row carrying the revision id and date it was last set on before the
shutdown; Y is the navmesh floor under that x,z; level, species, gender and faction are its infobox. The entity
class is an analogue under OD-45 — the class this world already uses for that species and gender — and the body
an analogue under OD-11, the set a shipped NPC of that class wears, because a swapset with no appearance rows
renders naked and headless. None carries dialogue: the wiki calls 30 of them mission givers, but those missions
are not seeded here.

Seven needed their reading resolved before they would land: Master Phanin's zone is an instance whose floor
matches his wiki Y to 0.4 m; Researcher Endala Berish's "Indra Pass" is the Ashen Desert surface, not the
caverns 103 m below; Information Spec. Nye's row reads 331, 522.6, 37, where 522.6 is a z on the Palisades and
the resulting spot is 80 m from Watchman Hillenmeyer at Lake Elinor on ground of the same height; Base Cmdr.
Matlin's Y of 1318 is not a height on any map here. Two are labelled map guesses (Downed Prisoner, Rijii).

Nine stayed out at first: eight whose pages named no zone that resolved to a map here, and **Commander Elvers**, whose page
puts him at Denzil's Caldera on the boot camp map — what stands in the reconstructed camp is the manifest's
business, and Ellatha's pass left him out too ("Spawns in 3 different spots" instead of a coordinate). He needs
an owner decision.

Eight of those nine turned out to be a **parser failure, not a source failure**: those pages write the location
row as plain table cells (`| Foreas | Valverde | Marshes | Paludos`) where the rest use wiki links, and the
reader only read links. Read properly, all eight name a zone this world carries, and each lands on the navmesh
floor within a metre of the wiki's own Y — the closest corroboration this source has given. They are created in
`TarapediaLastNpcs`, which also corrects five bodies the same re-read moved out of the "zone unknown" bucket:

- **Lieutenant Holloway** stood on the Pools map at a height of 862 m; the wiki puts him in Retread City in the
  Marshes, where the floor is 216.8 m against his wiki Y of 216. A map change, on the map's own ground.
- **Rijii** had been created on the Plains because his zone read "Brann LZ" and that was the only walkable
  ground at his x,z. His page reads Arieki / Torden / Mires / Brann LZ, *"In the same room as the Brann LZ
  Waypoint"*, and the Mires floor there is 238.28 against his wiki Y of 238.5. He moves to the Mires.
- **MP Price**, **Sergeant Elway** and **The Director** stood 116, 46 and 104 m from the reading.

Not taken, and worth recording: **Ranger Taavik**, **Shaman Geli**, **Sergeant Starling** and **Retread
McCormick** all carry the identical coordinate −268, 216, −812, which is Retread Karl's. The first three were
edited within minutes of each other on 2007-12-15 (revisions 21096, 21097, 21099) and their pages say Paludos
while that coordinate is Retread City. That is one editor's copy-paste, not four NPCs on one spot.

**GAP-TARAPEDIA-MISSING-NPCS** (closed for 59 of 60; Commander Elvers is the one left, and he is an owner
decision).

### What the audits caught in the sweep itself — 3 rows

The two guards found three rows the sweep had left wrong, which is the point of shipping them together. Snapping
the Edmund Range and Proving Grounds wargame vendors down from 2.4 m in the air landed them inside an ammo
crate, because the prop check had run before the snap rather than after; and Ranger Kaely's new spot at New
Velon Village has two walkable levels 1.2 m apart, where the probe took the lower and left her reading as buried
under the upper. `WorldSweepCorrections` puts all three right.

### The giver the briefing names — 3 missions

Of the 21 missions where giver and receiver are the same creature, 18 are meant to be. Three were not:

- **1392, 1393 Conscientious Objector Part Two** — given *and* received by Outpost Commander Rogers, so the
  mission asked the player to carry a report to the NPC who had handed it over. The chain turns on entity class
  25696, *"Apirka's Report detailing your involvement in the case of the deserter, Ranger Milpas"*: the giver is
  Warrior Apirka (creature 43), the receiver stays Rogers.
- **976 Restraining Order** — ours hung on Colonel Li Hua. The client's mission text (5334–5336) reads *"Colonel
  Bruce wants you to help the ground forces out by taking out 3 Bane Stalkers. Return to him at Fort Haroun"*.
  Colonel Bruce (client name 8657) had no creature at all, which is why the mission was on the nearest colonel.
  He is created and takes both ends. His coordinates are **inferred** — no source gives them — and he stands
  beside Dr. Torpor, the mission NPC already at Fort Haroun.

### "You received 30 3147" — fixed

`PmGotLootFromUnknown` substitutes `%(loot)s` verbatim, and the server was passing the item's entity class id as
that string. The client resolves item names itself: `Recv_GotLoot` reads `(entityClassId, quantity, itemId)` and
calls `GetEntityClassName`. A vendor purchase now sends `GotLoot` the way a kill payout does, so the line reads
"You received 30 Standard Grade Cartridges."

### CELLAR Arena — original content, and reachable after all

Not ours and not misplaced: the client's own tables carry game context `20000009 = "CELLAR Arena"` (map
`adv_afs_arena`), and what can be run into in the world is entity class `29211 ArchHumBaseHolosignChallengeArena`,
an AFS holo-sign that the client's map file places. It is signage, not the door — it has an empty augmentation
list and no `usabledata` row.

**A correction to the first version of this section, which said the way in was not in any data we hold.** It is:
a player walked onto a battlefield-entrance pad in the barracks of six AFS bases. Two original sources agree —
`uimapmarker.maplinkmarkers[2232]` carries eight type-8 BATTLEFIELD_ENTRANCE markers, and exactly six outdoor
maps carry the reciprocal marker back — and NCsoft's own patch note names the six: Foreas Base, Alia Das, Fort
Defiance, Mt. Hellas, Tantalus, Fort Intrepid. `MapLinkPreloader` seeded all fourteen rows in `ae831e9`, and
`rasaworld.db` has them enabled (ids 9, 17, 51, 77, 99, 104, 134 inbound; 135–141 back out).

What is actually wrong there is smaller and worth fixing: the arena's **hospital sits on the wrong map**.
`TeleporterPreloader` puts "Hospital: CELLAR Arena Medic" at 24.9, 40.0, 114.79 on context **2259**, while the
medic that defines it — spawn pool 500006 — stands at those exact coordinates on **20000009**, and the client's
own marker (uimapmarker 134419591463366, UI map key 2232) says 20000009 too. Map 2259 holds nothing else at all.
So a player who dies in the arena has no respawn point in it. Corrected in `CodexPlacementFixes`.

## The mission wiring audit (2026-09-21)

Every one of the 77 seeded missions read against the client's own mission text, and the whole of TaRapedia's
pre-shutdown history (926 mission pages) read against both. The live report that started it — *"commander rogers
is giving me quests to go to outpost commander rogers"* — was one case of a pattern.

### Objectives that completed at the wrong NPC — 6 fixed, 3 to go

A conversation objective completes by talking to whoever carries its NPC package. In six missions the package sat
on the wrong body, usually the giver's, so the mission sent the player back to the person who had just briefed
them. Four needed nothing but moving the package to an NPC already standing here:

| mission | was on | belongs to |
| --- | --- | --- |
| 347 | Shaman Horea | Base Guard Kapler (199010) |
| 640 | Colonel Whitaker | the Operations Mainframe (199511) |
| 940 | Sgt. Jeansonne | Sgt. Ricardo (199412) |
| 1063 | Xenori | Lieutenant Liu (199510) |

Three more were hung on a giver because the NPC the client names **did not exist in this world at all**: Field
Sgt. Hayes (332), Ashwon (969) and Sirth (977). They are created in `MissionSpeakers` at the client's own UI map
markers, each landing within 0.4 m of the marker's height. The tenth, mission 1863's Koffman's corpse, is still
open: the client has no map markers at all for the Marshes and no source gives it a coordinate.

### Missions that cannot be finished — 3, now diagnosed

442, 451 and 1186 are offered, accepted, and impossible: the required objective's only route is a conversation on
packages 1486, 569 and 1300, which no body carries. None of the three is a forgotten NPC — they are **props**.
1486 is the *Blood Analyzation Terminal* in the Twin Pillars hospital, which the client names itself
(`usablenameoverride` 73). 1300 is the Eloh obelisk in the Kardash Atta Colony, whose own conversation text ends
"The obelisk is too large to move on your own". 569 is a directions villager at Daghda's Urn whom no client table
names. Building them means creating usables, which is content: **GAP-MISSION-PROP-SPEAKERS** (open).

### Order, receiver, rewards

Five missions listed their steps out of the order the client's own conversation chain implies (332, 682, 955, 969,
1992) — the log sorts by ordinal, so the player read them wrong. 1904 was received by Retread Lou while its own
log, objective and package all name Retread Duvall. 429 paid nothing and 1390 was missing its 2,000 XP; TaRapedia
supplies both, and it is trustworthy for this column because all 53 XP figures and all 55 credit figures already
seeded match it exactly.

**Reward items are a firm negative.** The wiki names them by manufacturer — "Olympia Reflective Armor Vest" — and
the manufacturer is a module display pattern, not a template: no client table maps a template to a module. One
armour icon resolves to 492 templates and narrowing by level requirement reaches three, not one. Of 400 reward
lines, 67 resolve (nearly all consumables, which the client names directly) and 305 do not.
**GAP-MISSION-REWARD-ITEMS** (open).

*2026-09-26:* the negative holds for single names but not for whole lists. The client stores mission rewards as runs
of consecutive template ids, and a specific 3-4 entry list matches exactly one run. Seven missions now carry their
post-1.4 reward items at `inferred` tier (`MissionRewardItems`: 1541, 1673, 1040, 983, 970, 1068, 1863). The
manufacturer prefix is still unmapped (**GAP-MISSION-REWARD-MODULES**). The Logos missions stay without an item
because the shrine grants the Logos. See retail-accuracy.md, 2026-09-26.

### What the wiki could still give us

859 of its missions are not seeded here. 164 have both giver and receiver already standing in the world, and of
those exactly **one** is ready to seed outright (1659 *Logos: Vortex*) with 26 more a single named gap away — 22
of them Logos missions whose only gap is the mission level. That is the next content batch worth shipping, and it
waits on one decision, because **the client has no mission-level field anywhere** and TaRapedia states a level for
only 126 of 926 pages: **GAP-MISSION-LEVEL** (open).

### Redirect lines on the giver (2026-09-26)

The client stores a COMPLETION-type conversation row, with an identical REMINDER, on some givers' own packages
whose text sends the player elsewhere. This server completes an objective through *any* row of the speaker's
package, so each such row is an alternate turn-in the original text argues against. Seeding the ready missions
found two: 434's on Witherspoon ("we're good from this point. You need to do your magic down south") and 441's on
Randolph ("Did you get in touch with the Twin Pillars infirmary yet?"), both live the moment the mission is
accepted. They are kept out of completion by `MissionRedirectConversations`. The same shape already shipped on
429 (Rogers' 116 row, "Get this new info to Witherspoon") and is still a completion route there after the recon.
What the original did with these rows - an ambient reminder, which this server never sends, or nothing - is
unrecorded. **GAP-READY-REDIRECT-COMPLETION** (open).

### Torden conversation missions, re-checked before seeding (2026-09-26)

The Torden dossiers (`research/20260926-torden-missions`) were built against the database before the recovered
2026-09-22..24 migrations; every proposed row was checked again against the chain development now carries. Nothing
had moved: the nine missions were still unseeded, their client skeleton rows still carried NULL flags, every
giver and receiver still spawned, and every completion package was still carried. The prerequisites 887 and 1063
exist, so 1014 and 1064 are gated; the other four recorded gates name missions that are not seeded or not known.

Reachability was measured with the navmesh query the world audits use (`docs/evidence/torden-conversation-missions-navmesh.json`).
Every NPC the nine missions use is reachable from its hub's waypoint or hospital. Science Officer Clark is not: he
stands 7.6 m from the "Wedge Rock Outpost" map label he was placed from, on a surface no path from the outpost, its
hospital or Fort Defiance reaches, so 936 is held rather than seeded onto a giver no one can talk to.

Rewards are TaRapedia's amounts, each labelled with its era: six missions have only pre-1.4 records, which the
earlier W3 batches seeded the same way (GAP-TORDEN-REWARD-ERA); 1014's only figure is a closed-beta XP and is left
out. No pre-1.4 item list is seeded. 1326 and 1330 carry their post-1.4 Class VII consumables as choose-one rows,
inferred in structure and quantity (GAP-TORDEN-1326-1330-ITEM-CHOICE). Their level, 20, is the one analogue: the
Pools has no band, and the value is 1068/1541's under OD-60, pending owner review. To allow that label the provenance
registry now classifies `npc_mission`, whose columns are all required.

### Seeded content the final live game had withdrawn (2026-09-26)

Some seeded content was never in the final game. 767 Mighty Miasma, seeded 2026-09-16 at Dr. Munson under the OD-47
kill-count rule, is one of the three Munson sample missions that the official Deployment 11 live notes (2008-08-15)
say "are no longer available". The same notes permanently disable 769 Predatory. The earlier batch had checked the
mission's sources, TaRapedia and Ellatha, which are mostly pre-D11. It had not checked the patch notes that retired
the mission. `WildernessMunsonWithdrawal` removes 767, and 751, 780 and 769 are recorded as never to be offered.

The rest of those notes, checked against this tree:

- Body Count is not seeded.
- There are no Predators or Juggernaut in the named maps.
- Richards is already at the Pinhole entrance.
- Mama Miasma's client target is already 3.
- The trainer cull is superseded by D12.

The same review corrected three things:

- 771's manifest row named creature class 24084 where the item is 11153 Shield Drone Scraps.
- 771's seeded 300 credits are a 2007 beta figure. Ellatha's later page says 600.
- 787 had no gate, although its own client opening presupposes 758.

**Lesson for later batches: check every mission against the D11-to-shutdown patch notes before seeding it, not only
against its wiki page.**

### The missing givers, and a probe that invented a floor (2026-09-26)

Four mission givers the dossiers called unplaceable have dated `/loc` readings in Ten Ton Hammer's area guides,
a source no earlier pass had searched (`research/20260926-missing-npcs`). Each reading was checked against the floor
directly under its x,z, not against the nearest polygon: Simpson's y matches the command-centre floor to 0.10 m,
Tarina's to 0.015 m, Hanna's to 0.28 m and Warrior Mela's (in the same guide) to 0.09 m, each on a surface a complete
route joins to the settlement's waypoint (`docs/evidence/missing-mission-givers-navmesh.json`). They are placed or
moved there (`MissingMissionGivers`); Dekay, who has no coordinate at all, stands at the midpoint of the "between the
wormhole and waypoint" the guide describes, measured with a 26 m uncertainty.

Col. Almos did not pass. The proposal gave him an "upper level" at 176.12 m, but that level came from
`NavMeshQuery.GroundHeight`, which snaps to the nearest polygon up to 4 m sideways: under his x,z there is only the
terrain at 174.84, 3.16 m below the reading. Nowhere within his 8 m uncertainty does the floor come within 0.3 m of
the reading's 178.0, and the reading's y and z equal the client's "Viands Village" label, so he and 551 are held
(**GAP-ALMOS-HEIGHT**, OD-67). The earlier "levels" columns of the research probes should be read with that in mind:
a level listed for a column is not necessarily a surface in that column.

The same pass corrected a date: Dr. Elise Corman's TaRapedia coordinate, cited as "rev 34276, 2008-09-25", has stood
unchanged since the page was created (rev 983, 2007-06-30); the 2008 revision only renamed headings. Her position
stands, but it is a pre-D11 reading.

### Liaison Logos missions: one audit list, four givers (2026-09-26)

The segment-3 audit listed eleven unseeded `Logos:` missions under Receptive Liaison Langerman. Before seeding,
each was traced through TaRapedia's full page history, its giver's NPC page and the world's shrine rows. Only four
are Langerman's. Two are Standley's in Twin Pillars, four are Noonan's at Foreas Base in the Divide, and one is
Arizpe's in the Palisades. Every shrine stands on its giver's map, and every shrine's `logos` id equals the client's
`logosstone` constant for its word. They were seeded on those givers (`LiaisonLogosMissions`).

The rewards did not survive as well. TaRapedia's amounts were typed in late 2007 and only reformatted after D11.
Ellatha's early-2008 pages disagree with five of the six credit amounts they cover. The six TaRapedia pages that can
be checked all had their creation-time credits corrected later or contradicted. So five credit amounts and the
Divide's 200s are left out, and the rest are labelled pre-1.4 (**GAP-LIAISON-LOGOS-REWARDS-MISSING**, **-REWARD-ERA**).

The same weakness sits under the already-seeded 1649/1650 (200 credits each), which was not changed.
**GAP-LIAISON-LOGOS-UNSEEDED** lists the Logos missions still missing: six are seedable the same way.

## The parallel audit, and what it shipped (2026-09-21)

Fifteen investigations ran at once over the client's own data, the wiki's full history, a recovered pin-map and
this server's source. The headline is reassuring: **every static table copied from the client is exact** — 19
tables compared row for row, zero value mismatches, and `Lightning` verified end to end (SkillId 49 →
`AA_RECRUIT_LIGHTNING` → damage-type property 13 → the client displays "Electric"). Every defect found was in a
row the emulator *derived* or in server code formatting a name the client should format.

### The mute world speaks once

`BarkPackage` existed but only a GM command reached it, so 859 bark rows and 1,718 localised lines had never
been heard. Bark 852 is original end to end — the client's table gives it a 10 s bubble, `stringtable` names the
file `boot_camp_major_mcallister_bark.ogg`, and the audio set is "Bark English Male Bootcamp McAllister" — so
Major McAllister now shouts *"Soldier! If you're done communing with your alien buddies, we could use a hand
saving our asses around here!"* when the recruit comes back from the Eloh holograms. The trigger moved from the
design's first proposal, which sat 48.3 m from him against the client's hard 20 m bark range: it would have
shipped silent.

**Bark 232 shipped nothing, deliberately.** Its line — "Get a charge over here to clear out this wreckage!" —
belongs at the crashed dropship, and the NPC chosen to say it, Corporal Van Valkenberg, does not exist in the
world at that moment: his placement carries `present_condition 198907`, `bootcamp.dropship_destroyed = 1`, so he
and the three other reinforcements only materialise *after* the detonation. Nothing else stands within 100 m of
the wreck during the demolition; the nearest present body is Conrad's corpse, 187 m away. The bark is weak
evidence that the original had someone at that pad and this reconstruction does not.
**GAP-BOOTCAMP-DROPSHIP-SPEAKER** (open).

### Everything wearing its clothes properly

421 outfit rows over 159 NPCs were filed under the wrong slot — the officer boots under GLOVES, torso and legs
keyed to each other — and every affected NPC is somewhere a player can reach, including all nine humans in the
boot camp. Five shipped creatures carry the defect; the other 154 are ours, each a copy of one of three donor
bodies taken as an OD-11 analogue and carried forward batch after batch. The donors are corrected at source and
`AppearanceSlotAuditTests` now reads every row in the world against the client's own
`equipableClassEquipmentSlot`, so the next batch cannot inherit it.

### Weapons that can be looked at

3,381 of 5,825 weapon templates had no `itemtemplate_weapon` row at all, and the tooltip packet dereferences
`WeaponInfo` unconditionally — a tooltip on any of them would have thrown. The rule that derives a row from its
weapon class reproduces 2,441 of the 2,444 existing rows exactly, and the three it disagrees with turned out to
be the defect rather than counter-examples. 120 blades read "Ranged" and now read Melee, which is the first
melee weapon this world has had and wants an in-game check, since `attack_type` is what knockback keys on.

### Names, again

The server has no display names: `entityclass.class_name` is the client's *internal* name and the player-visible
one lives only in a client language table, which is why a server-built sentence can never name an item in any
language. That is the same fact behind "You received 30 3147". Crafting was saying "You need 50
Ammo_Nucleotides_1_Pyrimidines"; it now sends the client's own messages (236, 237) and the helper that turned a
class id into a word is gone. Fifteen classes labelled `Missing_ItemClassId_N` take the names the client gives
them — and with them goes an accident, a `StartsWith("Mis")` rule that had been making Rifle Ammo and the Botany
Kit mission items on the strength of a label no client ever saw.

## Systems the server never drove (2026-09-22)

Each of these was built only after the client's own bytecode was read for what it expects; two of the four
survey premises turned out to be wrong and were replaced by what the client actually needs.

- **Missions are tracked, and stay tracked.** The client already tracks a mission the instant
  `Recv_MissionGained` arrives (`missionlog.py:286`) and persists the tracked set as character options
  `MissionTrack0..29` — options this server stored and echoed but never *wrote*, so every zone change re-sent
  blank slots and the tracker emptied. The accept path now writes the changed slots in the same transaction
  that saves the mission; unticking in the log still sticks. The 30-slot cap and drop-the-newest overflow are
  the client's own.
- **Missions on the map.** The only server-driven mission marker the client has is the objective indicator
  (`MISSION_INDICATOR`, many per objective, self-removing on completion, gated on the tracked list) — there is
  no "available mission" map kind and no "ready to turn in" state, and neither can be invented. 116 of 124
  objectives now derive a marker from data the world already holds: a conversation's speaker, a shrine, an area
  centre with its radius, a usable, or the spawn pools of a kill target; the 8 that cannot are named. Markers are
  filtered to the player's own map because the client's indicator tuple carries none. Turn-in shows through the
  receiver's existing conversation status: overhead icon, radar pip, and a map marker once the NPC streams.
- **Teleport effects, corrected premise.** `teleporter.type` is the *waypoint kind*, not the effect key, so the
  survey's "203 pads play the wrong effect" was unfounded. The real bug was next door: onlookers were sent
  `PreTeleport` and nothing else, and the departure effect it attaches is released only by the arrival beat —
  so on every other screen it stayed glued to the body for as long as the entity lived. Onlookers now get post
  and arrival; the owner is deliberately excluded, since its own client runs both and a second post replays the
  flash as DEFAULT. The one teleport whose effect the client names — Tactical Retreat, `TACTICAL_EVASION = 7` —
  now plays it.
- **Corpses.** Nothing told a client that an entity it met for the first time was already dead, so a body
  you did not watch die stood upright and was looted on its feet. `DeadOnArrival` is sent at introduction —
  *before* `ActorInfo`, because its handler announces only `if not IsDead()` and `ActorInfo` would already have
  made it so.
- **Effects catch-up and aiming.** `Recv_GameEffects` is the bulk send at entity creation; without it every
  buff and debuff was invisible to anyone who did not witness it land. The survey's "everything needed is in
  hand" was wrong — the effect carried no source, no tooltip, no arguments — so the announcement is kept on the
  effect and replayed byte-identical with a fresh countdown. `TargetId` now broadcasts on change, clearing with
  `None` rather than `0` (a `0` leaves the weapon synced to a nonexistent entity); creatures and players are
  aimed correctly the moment they appear.
- **The duel.** Right-click → Challenge to Duel sent opcode 626 into nothing. The 21-opcode wargame protocol is
  now read from the bytecode and the duel slice built end to end: the two dialogs, every refusal in the client's
  own words, accept/decline/revoke/timeout, the tracker with clock and score, friendly fire scoped to the pair,
  and the losing blow refused as a death — the client's `WARGAME_FLAGS_DUEL` carries none of the death bits, so
  the loser is left standing at 1 HP. Team tokens are evidenced (`WargameTeamIds` shirt 0 / skin 1), not
  inferred; PvP damage is halved by the client's own `PVP_DAMAGE_MODIFIER`. Server choices are labelled: a 60 s
  challenge timeout, first-blood / five minutes when the client sends 0/0, separation cancels rather than
  forfeits. Squads, clan feuds, and duel *abilities* (the ability paths are creature-typed throughout) are left
  out and said so.
- **Footlockers.** All 35 announced a control point's state, which the LOCKBOX augmentation rejects, so the
  container never entered a state; each now announces the one state its class owns, and the two clan lockboxes
  are driven as clan lockboxes. Teleporter 425's item class is left as an owner decision between two wormhole
  classes.

## World-data defects (2026-09-26)

Two read-only audits of the deployed world (`research/20260926-world-defects`, and the "World-data defects" of
`research/20260926-torden-missions`) found rows placed wrong by an earlier import or an earlier correction.
`WorldDefectsFix` corrects the ones with evidence and leaves the rest as named gaps. Replayed against a copy of
`rasaworld.db` with the rows applied, the five world audits pass: 1,058 bodies probed, no defect, no new suspect.
The navmesh probes are in `docs/evidence/world-defects-20260926.json`.

| row | was | now | evidence | tier |
| --- | --- | --- | --- | --- |
| placement 199106 Field Lt. Bagby | Palisades 1244, lifted to y 137.79 | Treeback Camp 1397 (-337.2, 103.6, 353.7) | client 1788 log "at the Treeback Camp"; TaRapedia 9003 `Instance=Treeback Camp`, rev 22867 (2007-12-20); post-D11 Treeback Camp page rev 32558 (2008-08-30) | map inferred (high); position inferred |
| placement 199107 Lt. Galloway | Palisades 1244, lifted to y 140.44 | Treeback Camp 1397 (-122.2, 100.3, 128.2) | client 1789 log "out of the Treeback Camp"; TaRapedia 9010 rev 22879 | map inferred (high); position inferred; stationary stands in for a scripted NPC |
| pool 520046 "Warnet Queen - Palisades" | level-28 hostile boss on the Divide's "Foreas Base" label, 0.5 m from the receiver of seeded 1743 | removed | no source places a Warnet Queen at Foreas Base or on the Palisades overworld (post-D11 Palisades Targets of Opportunity rev 35441 has none) | — |
| pool 510137 Valerie Corman | ring on the "Cumbria Research Facility" label | (-772.7, 140.17, 774.9) | TaRapedia rev 33050 (2008-09-13) and codex-tr.net POI 1425, 4.1 m apart | x,z inferred (high); y measured |
| pool 510133 Ranger Jorai | on the "Viands Village" label | (-294.0, 172.707, -577.0) | TaRapedia rev 16456 (2007-11-26), pre-D11 only | x,z inferred (medium); y measured |
| pool 510068 Lieutenant Epp | on "Waypoint: Nyxroq Post" | (-180.0, 235.229, -244.0) | TaRapedia rev 35626 (2008-11-11), 2.2 m from the client's "Hospital: Nyxroq Post" marker | x,z inferred; y measured |
| pool 510085 Colonel Whitaker | a second Whitaker 107 m from placement 199500 | counts 0/0 | TaRapedia has one Whitaker, at 199500's reading | inferred |

Missions 1788 and 1789 still resolve their receivers: 1397 is a shared context (`content_map_setting` lists only
1985), every map in `map_info` gets a channel that materializes its placements, and map links 21/35 join it to
the Palisades.

Not moved, each with a gap: Field Ranger Kearney and Warrior Mela, for whom no positional source exists, keep
upstream's marker positions as labelled analogues (OD-59; `GAP-KEARNEY-POSITION`, `GAP-MELA-POSITION`). Science
Officer Clark and Security Guard Norton look as if they had post-D11 readings, dated 2008-09-25, but those
revisions only move the pages' 2007-08-17 beta numbers into a new table template. Clark's current spot is probably
unreachable (the navmesh route from the Wedge Rock crafting station stops 36 m short of him and 31 m below), so `GAP-CLARK-POSITION`
asks for an owner decision. Perdu, Obahmi, Franks, Foletto, Nicholson, Orto and Creelig have no post-D11 reading
(`GAP-TORDEN-MARKER-POSITIONS`). The client has two "Colonel Whitaker" names, 5428 and 8919, and which one the
Irendas Whitaker used is not recorded (`GAP-WHITAKER-NAME-ID`).

## Official live notes against the seeded missions (2026-09-26)

Every live note from 1.4 (2008-01-29) to D16.5 (2009-02-17) was compared with the 114 seeded missions
(`research/20260926-notes-audit`). 73 missions have no note. Of the rest, the audit found:

| Finding | Missions | Action |
| --- | --- | --- |
| Seeded value contradicted by a note | 2016 level (D14: 50), 682/3 killer-only credit (1.6, D8) | corrected, `OfficialNotesCorrections`, original tier |
| Note describes the target population | 976 (1.6: any Mires Stalker) | binding already creature-wide; OD-48 note, gap |
| Accept item missing | 1125 (1.7: one keycard) | gap; no accept-time grant exists |
| Position predates the note | 1745 Arizpe and 408 Jamison (D12), Johnson/321 (1.7), Quillas/1390 (1.7) | gaps, nothing moved |
| Relationship not modelled | 983/1041 alternative courses (1.7, D8) | gap |
| Consistent | 1112, 422, 366-368/411-413, 771/787, 1789, 2016's receiver, 1992 | none |

Kill credit on shared maps was killer-only for every binding. The notes establish one exception, 682/3, now a
per-binding flag (`shared_kill_credit`). Any later note that names another mission needs its own flag and citation;
the flag is not a default. The shared credit reaches the whole map channel, which the notes neither confirm nor
limit (`GAP-NOTES-682-SHARED-CREDIT-REACH`). Details: `docs/retail-accuracy.md`, "2026-09-26 — Official live notes
checked against the seeded missions".

## Client-contract defects (2026-09-26)

The segment-3 systems audit (`research/20260926-segment3-audit`) found server behaviour that contradicts the client's
own code or tables. Each was re-read from the 1.16.5.0 client before it was changed.

| system | was | now | client evidence | tier |
| --- | --- | --- | --- | --- |
| local teleporters (42 pads) | never gained: the proximity switch had no case for them | gained and used like map waypoints; window limited to this map | `waypointtype` LOCALWAYPOINT 1; `Recv_WaypointGained` PM_GAINED_WAYPOINT; `Recv_EnteredWaypoint` | rule original; 2 m radius inferred |
| teleporters 595/597 | type 1, standing on "AFS Field Medic" hospital markers | type 5 (`LocalTeleporterGraveyards`) | uimapmarker HOSPITAL markers at 0.0 m; no waypointlanguage 595/597 | inferred |
| dropship window | every pad from level 1; first pad per map at a boot-camp hospital's coordinates | gained pads only, each at its own position | uielementlanguage 5697; `SetupWaypointLocationRows` places by the sent position | original |
| Palisades control-point hospital | graveyard 136 / waypoint 216, shared with the Wilderness LZ | 221 / 226, Fort Dew | Fort Dew control-point, waypoint and medical-vendor markers; waypointlanguage 225/226 | inferred |
| Divide, Palisades, Devil's Den hospitals | Foreas Base, Cumbria and Devil's Den not offered | offered (202/93, 219/112, 41/388) | graveyard and waypoint id blocks; one graveyard per place | inferred |
| buyback price | 0 on every template | the sell price | `inventorywindow.OnSlotEntered` + `tooltipwindow` line 908 | original |
| repair charge | round((max - cur) × sell / 100) | `vendorwindow._GetRepairPrice` | lines 1081-1094; `GetCondition`; REPAIR_GLOBAL_MODIFIER 1.0 | original |

Left open with gaps: the local pads' ids have no client names (`GAP-LOCAL-TELEPORTER-IDS`, OD-90), the local-pad
radius, three type-1 rows that are probably not local teleporters, the dropship hover condition and gain message, the
shared entrance-hospital waypoint 120 and the Wilderness LZ marker binding. Details: `docs/retail-accuracy.md`,
"2026-09-26 — Client-contract defects".

## Class trainers and the economy's lineage (2026-09-26)

The segment 3 systems audit (`research/20260926-segment3-audit`, SEG3-CLASS-TRAINER-PLACEMENT and
SEG3-ECONOMY-PROVENANCE) found two things that looked sourced but were not. Both were re-checked against the 1.16.5.0
client before anything changed. Evidence: `docs/evidence/class-trainer-evidence.json`; research captures in
`research/20260926-trainers-economy`. With the rows applied to a copy of `rasaworld.db`, the world audits pass: 1,060
bodies probed (1,059 before, plus Stratton), no defect, and the one suspect (pool 41, a Boargar spawn) was there before.

**Class trainers: one per hub, as D12 left them (`SingleClassTrainers`).**

| row | was | now | evidence | tier |
| --- | --- | --- | --- | --- |
| pools 501001-501038, the per-class trainers | 38 creatures ("Soldier Trainer: Alia Das" ... "Specialist Trainer: Ashen Desert"), six ringed round Kincaid, no package | counts 0/0; rows kept | D12.5 live notes (2008-09-18): per-class trainers "replaced by one single trainer"; one TRAINER marker per hub on every live map; per-class markers only in the unused wargame template 2269 | original |
| creature and placement 199604, Training Officer Stratton | — | on marker 987 "Class Trainer: Daghda's Urn" (-599.685, 276.605, 871.195) | TaRapedia Daghda's Urn rev 35503 (2008-11-04); client name 10606 | name original; hub inferred (high); position inferred from the marker; body, level, hp, facing analogue (OD-95) |

The upstream generator had read the client's per-class dialogue groups (ids 1-336) and marker texts as the live cast.
But those texts are either used by no marker (973-979, 1081-1092) or used only on the wargame copy (977/978). None of
the dialogue groups names Torden Mires or the Ashen Desert, where upstream also stood trainers.

`ClassAdvancement` recognised trainers by npc package, and only Kincaid's (2588) is recovered. The client does not
need a package to train. `npc.CanTrain` reads only `CONVO_TYPE_TRAINING`, and the Converse action appears for any
conversation status but NONE. So a trainer whose package is unknown is now recognised by creature id
(`TrainerCreatureIds`). `CreatureManager.ApplyPlacementNpc` gives his placement an NPC record, and
`ClassAdvancement.IsClassTrainer` is used for the conversation, the Train status and the 20 m range check.

Not placed, each with a gap: the single trainers of Twin Pillars, Foreas Base and New Cumbria, whom no source names.
The client's unassigned late names ("Training Officer" Lebowicz, Buckmaster, Delany, Walker, Howell) would fill them
neatly, but they are candidates, not evidence.

**Economy: emulator data labelled as emulator data (OD-96).** The manifest had filed InfiniteRasa's
`gameserver_dev_Full.sql` as `official_notes`, "the original game server's own schema and seed data". It is the C++
emulator's development dump. The manifest now has a source kind `emulator_db`, which can support no original value.
No data changed, but the labels did:

| data | was labelled | now | why |
| --- | --- | --- | --- |
| creature_loot 1-21 (the dump's seven type-20 rows on three Thrax) | item, chance and stack bounds `original` | `analogue`, counterpart the dump's row | an emulator's table; no original drop table survives (`GAP-CREATURE-LOOT`) |
| every item template's buy/sell price (`Regenerate_item_template`) | "derived but sourced" (GAP-W1/W2-ITEM-PRICES closed) | `analogue`, gaps reopened | buy = loot_value and sell = floor(buy/4)+1 were fitted to the same dump's 188 prices, so the agreement is circular (`GAP-ITEM-PRICES-EMULATOR-DERIVED`) |
| vendor stock (`vendor_item`) | unlabelled | `analogue` | 2023 world seed and upstream pass; the client's `vendordata` gives each package a type, not a stock (`GAP-VENDOR-STOCK-UNSOURCED`) |

**The "Test Vendors" of Alia Das.** Pools 21-29 ("Test Vendor 1-9", x 829-853) have spawned nothing since the 2023
seed (counts 0/0). Pool 36 ("Test Vendor 5") is not unsourced in the way that matters. It stands 0.15 m from the
client's marker 971 "Weapons Vendor: Alia Das", and its package 10 is a WEAPONS vendor in the client's `vendordata`.
It is kept as Alia Das' weapons vendor, and its missing name, test body and seed stock are `GAP-ALIA-DAS-WEAPONS-VENDOR`
(OD-97). Removing it would have left the starting hub without the vendor its own map marks. Its Laser Chaingun (4018)
has no other seller. The five "Test Vendor 3" medical vendors at the other Wilderness hospitals (pools 31-35) need the
same review (`GAP-WILDERNESS-HOSPITAL-TEST-VENDORS`).

**Seen along the way, not changed here.** Several W3 rows cite the emulator dump for "the world seed's quest NPC"
conventions (555 hp, run 9, walk 5). `WorldPlacementFloorSnap` and `WorldFloorSweep` take their 0.276 m floor offset
from "the original server's own 217 creature spawn points". Those are spawn pools of emulator lineage (the world seed
and the upstream passes), not the original server's. Both are emulator conventions rather than original data and
deserve the same relabelling.

## Kill experience modifiers (2026-09-26)

SEG3-KILL-XP-MODIFIERS in the segment 3 audit, checked against the 1.16.5.0 client before anything changed. Squadmates
in range now share a kill at the client's own XP-bar share (original). An outlevelled kill loses experience on a
ramp built from the original `DANGER_PENALTY_*` constants and TaRapedia's zero at ten levels (inferred, OD-105). A crit
kill pays its observed second chunk but has no trigger yet. The audit's level-difference reading of B3-060 is
withdrawn: its 40 credits belong to the previous kill, and the line fits the client's unbuilt damage-ranked partial
credit instead. Rules, tiers and gaps: `docs/evidence/kill-rewards.json`; account: `docs/retail-accuracy.md`, "Kill
experience: squad share, danger penalty, crit kills".

## Clone workflow (2026-09-26)

The segment-3 audit (SEG3-CLONE-CREDIT-SOURCES, SEG3-CLONE-COPY-RULES) found the Clone Credit item inert and the copy
list unverified. The client's clone contract was re-read from the 1.16.5.0 bytecode, and the copy list was checked
against every dated source (`research/20260926-clone-credits`; rules in `docs/evidence/class-trainer-evidence.json`).

| behaviour | was | now | evidence | tier |
| --- | --- | --- | --- | --- |
| right-click Use of a Clone Credit (`RequestUseCloneCredit`, 706) | unhandled: the token did nothing | one token from the backpack spent, one credit added and saved, `CloneCredits` sent (client: PM 955 and tutorial) | `clonecredit.pyo` InventoryUse; `manifestation.Recv_CloneCredits`; TaRapedia Clone Credit rev 31080 (2008-06-03, unchanged to 2008-10-16) | original contract, observed effect |
| token used from the footlocker or clan lockbox | unhandled | ignored (OD-115) | the lockbox windows can send it; no source shows it honoured (`GAP-CLONE-TOKEN-LOCKBOX-USE`) | gap |
| template 111219 `not_tradable_flag` | 0, the uniform placeholder of `Regenerate_item_template` | 1 (`CloneCreditNotTradable`) | TaRapedia: "not able to be traded" | observed |
| clone credit rewards of the ToO and hybrid missions | none | none; recorded for their missions (OD-116) | TaRapedia rewards for 1449 (2008-10-21), 1582, 1630, 1619, 1861, 1851, 1899; none of these missions is seeded | gap |
| completed missions on a clone | carried | carried (unchanged) | TaRapedia Cloning (2007-11-28), Beginners Guide (2008-10-23), official 1.4 ToO clone rule | observed |
| open missions on a clone | not carried | not carried (unchanged) | same, plus IGN "wipes the quest log" and the official site's guide | observed |
| clone's arrival | the source's position | unchanged | "Your Clone and You": "Location upon cloning" not reset | observed |
| clone's own tier credit | none if cloned at or past the gate | unchanged, now tested | TaRapedia Cloning: "clone at level 14.9, not at 14.9999999" | observed |

The copy-list "conflict" was a reading of "missions reset" as the whole history. Every source that separates open
and completed missions agrees with what the server already did. The code's "2009-01-25 Beginners Guide" is
TaRapedia's Beginners Guide, whose last revision before shutdown is 35313 (2008-10-23). "Your Clone and You" is a
player guide the official site republished. Its text did not change from 2007-12 to 2009-01, so its "Attributes"
not reset predates the official 1.4 notes that reset them. Still open: the ToO auto-completion for clones
(`GAP-CLONE-TOO-AUTOCOMPLETE`), friends, ignores, clan and titles (`GAP-CLONE-SOCIAL-STATE`), the token's other flags
(`GAP-CLONE-TOKEN-FLAGS`) and any clone-credit vendor (`GAP-CLONE-CREDIT-VENDOR`, none found).

## Creature loot against the footage (2026-09-26)

The segment 3 systems audit ranked creature loot first (SEG3-CREATURE-LOOT). Every creature without rows dropped three
cartridges on a coin flip, and the only rows were an emulator's. The footage was re-read for every loot line and every
kill around it. The result is `docs/evidence/creature-loot-footage-ledger.json`, and `CreatureLootFootage` is built
from it. `CreatureLootFootageTests` recomputes each seeded value from the ledger.

| creature | was | now | evidence | tier |
| --- | --- | --- | --- | --- |
| Thrax Infantry Initiate (198507, 198513) | emulator rows: cartridges 12% 1-35, Motor Assist 0.5% each, med pack 5% | Thrax Skull 52.38% x1; five standard-grade ammunition rows 1.9% each with observed stack ranges; Motor Assist and med pack kept | 22 skulls and 4 ammunition stacks in 42 credited kills | chance measured (ammunition split inferred); stacks measured; kept rows analogue (OD-96) |
| Thrax Soldier (3), the Wilderness Thrax stand-in | the same emulator rows | the Initiate's rows | Thrax Infantry Trainee: 3 skulls, 1 rockets in 4 kills (not seeded as a creature) | inferred (OD-112) |
| Young Forest Boargar (44) | stand-in drop | Boargar Ear 100% 1-2 | 2 ears in 2 kills | measured (OD-112) |
| every other creature | three cartridges on a coin flip, unlabelled | the same, labelled | none | analogue (OD-110, pending owner review) |

**Refuted along the way.** The audit's plan expected ammunition matching the killer's weapon. The ledger shows
otherwise. Five of the seven ammunition drops whose receiver's weapon is known are power cells or rockets to
cartridge weapons: a Shinobi Rifle, an AccuMax Shotgun and a Teleract Rifle, each firing Standard Grade Cartridges by
its own tooltip. All five standard-grade types drop. Implementing weapon-matched ammunition would have invented a rule
that the evidence contradicts.

**Seen, not seeded.** Schematics, random gear (Wellcare Motor Assist legs, an experimental shotgun), crafting
resources, holiday snowballs and snowball launchers, and the last patch's player-named red weapons. Squad loot goes
to one member, with need and greed rolls, and the server has no such distribution. Each has a `GAP-LOOT-*` entry.

## Shutdown broadcast (2026-09-26)

Two recordings of the EU server's last minute (fxAtDpxypSw, _gwh1__XecI) were checked against the 1.16.5.0 client's
admin-message and disconnect paths before anything was built. Rules, tiers and gaps:
`docs/evidence/shutdown-broadcast.json`; account: `docs/retail-accuracy.md`, "The shutdown broadcast".

| behaviour | was | now | evidence | tier |
| --- | --- | --- | --- | --- |
| admin message (`AdminMessage`, 24) | packet defined, never sent | free text to the world or one map, filter SYSTEM_GM; console `announce`/`announcemap`, chat `.announce`/`.announcemap` at GameMaster (OD-120, OD-124) | `communicator.Recv_AdminMessage` prints uielement 4146 before the text; German footage shows a localized header before English text | original contract, observed use; filter inferred |
| shutdown countdown | none; `exit` only stopped the process | console `shutdown start`: "Server Shutting Down in 10...", 9…1, disconnect at 37.1 s, then stop (OD-121-123) | fxAtDpxypSw t=122-159.1, corroborated by _gwh1__XecI t=546-584.9 | observed text, measured cadence |
| disconnect presentation | a socket close | unchanged: a close the client did not request is what draws its dialog; the countdown sends nothing before it | `exitgame.GameOnDisconnect` → `inputhandlers.OnDisconnect` (uielement 9), then `ClearChatInfo` | original |
| zone-loss alert, Neph broadcast, Last Stand | none | none: gaps | only "ALERT: PLATEAU IS LOST!" observed; the rest is chat or off camera | gap |

## Instancing (2026-09-27)

The segment-3 audit's `SEG3-INSTANCING` was checked against the 1.16.5.0 client before anything was built. Tiers and
gaps: the manifest rows of `MissionContextSquadInstancing` and OD-125 to OD-129. Account: `docs/retail-accuracy.md`,
"Instancing".

| behaviour | was | now | evidence | tier |
| --- | --- | --- | --- | --- |
| which maps are instances | only 1985 | the 53 loaded MISSIONCONTEXT (type 5) contexts per-squad; 2375 and battlefields shared | `gamecontext.pyo` type column; `wonkavatorwindow` Instance/Persistent widgets | original |
| squad ownership | none | one copy per squad or solo player; invite quirk kept (OD-125) | TaRapedia Operation rev 32773; live D10/D13 known issue | observed/official; binding inferred |
| leaving the squad | nothing | back to the previous map with PM 1058; `partyExclusiveMap` sent | party.pyo OnLeaveParty (PM 946), PM 945, PM 1058 | original contract |
| way out | none (SelectWaypoint refused 0) | "Leave current adventure" (waypoint 0) from the pad (OD-129) | `Recv_EnteredWaypoint` None waypoints, PM 315 | original contract; arrival inferred |
| empty instance | destroyed at once | kept 600 s for its squad (OD-126) | live 2007-11-29, 2008-08-22; TaRapedia "ten minutes" | measured upper bound |
| instance chooser | unimplemented (685/687/688 unused) | built; copies open only at a configured capacity, none set (OD-127) | clientmethod 488/495/502, waypointwindow.ShowInstances; live 2007-07-24, 2007-08-21 | original contract; capacity gap |
| copy number | 1 or the private id | lowest free per context; boot camp unchanged (OD-128) | loading screen and map window "Name(n)"; "Earth 1"-"Earth 5" chat | original display; rule inferred |
| waypoint window rows | one identical row per waypoint | one row per copy | waypointwindow.ShowWaypoints line 265 | original |

## Instance travel and death (2026-09-27)

Batch 2 of the instance inventory, checked against the 1.16.5.0 client's link and hospital markers. Tiers and gaps:
the manifest rows of `InstanceTravelLinks`, OD-130 to OD-134 and `docs/evidence/hospital-catalog.json`. Account:
`docs/retail-accuracy.md`, "Instance travel and death".

| behaviour | was | now | evidence | tier |
| --- | --- | --- | --- | --- |
| Warnet, Ustor Yard, Sanctus Grotto, Refuge doors | no link (no client exit marker) | door each way on the client's entrance marker; arrival on the entrance hospital (OD-130) | `maplinkmarkers` type 7; TTH 2008-01-24/2008-03-21; Massively 2008-02-28; world seed 130/131 | trigger original; arrival inferred (Refuge low) |
| Last Stand exits | no link | to the CELLAR's unpaired centre marker (OD-134) | `maplinkmarkers[2378]`, `[2232]` | trigger original; arrival inferred |
| CELLAR north end | no link (client names 2361) | to Edmund Range 2374's Staging Area; way back analogue (OD-131) | D15.7, D16/D16.4, D16.3 | trigger original; destination inferred |
| Eloh Vale entry | none | unchanged (`GAP-ELOH-VALE-DROPSHIP-ENTRY`) | missions 1198/1199/1429; TaRapedia rev 32028 | gap |
| instance start portable waypoints | none | unchanged (`GAP-INSTANCE-PORTABLE-WAYPOINTS`) | notes 1.4, 1.6, D8; entityclass 28474 | gap |
| instance hospitals | 105 on 42 maps; instances revive in place | 127 on 55 maps (OD-132) | graveyardlanguage, waypointlanguage, uimapmarker, world seed rows, navmesh | inferred joins |
| Eloh Temples hospitals | every gained one | the current section's only | D10.5 live notes | rule original; section inferred |
| retired rows | test maps 1991/2233/1737 loaded; 2361 spawn pools | removed (OD-133) | gamecontext; D16.3 | original |

## Crater Lake Research Facility (2026-09-27)

The Crater Lake section of the wilderness instance dossiers, checked against the 1.16.5.0 client and the repo navmesh.
Tiers and gaps: the manifest rows of `WildernessCraterLakeResearchFacility`, OD-140 to OD-144 and
`docs/evidence/crater-lake-research-facility.json`. Account: `docs/retail-accuracy.md`, "Crater Lake Research Facility".

| behaviour | was | now | evidence | tier |
| --- | --- | --- | --- | --- |
| Captain Velns' package | none (package 23 carried by nobody) | 23 | objectiveconversation (450,4,23) | original package; speaker inferred |
| 450 The Dead Live | unseeded | 107 -> Velns, 900 credits, no XP (OD-140, OD-144) | client tables; TaRapedia; Ellatha | inferred, pre-1.4 amount |
| Lt. Casper | shared spawnpool, 500 respawn | per-copy placement 1721100, no in-copy respawn (OD-142) | Ellatha /loc; TTH; D11 PTS notes | inferred; height measured |
| Overseer Tyryd | absent | creature 1721001 at the key-drop /loc (OD-141) | client name 7002; Ellatha; TaRapedia | inferred; stats analogue |
| 960 Logos: Movement, Around, Chaos | unseeded | shrines 50/45/4, 18,000 XP, 1,500 credits, ungated pen (OD-143) | logosstone; TaRapedia; Ellatha | inferred, pre-1.4 amounts |
| 1055 terminals | proposed | held: HQ and greenhouse floors are navmesh islands, DT3 reading contradicted | navmesh probes; client map | gap |
| 1065 escort, radar dish, 489, 1054, population, D11 bosses/crates | proposed or unrecorded | held | loader rule; entityclass 6273 aug 9; D11 notes | gap |

## Pravus Research Facility (2026-09-27)

The Pravus section of the wilderness instance dossiers, checked against the 1.16.5.0 client, the footage and the rebuilt
navmesh. Tiers and gaps: the manifest rows of `PravusResearchInstance`, OD-135 to OD-139 and
`docs/evidence/pravus-research-instance-20260927.json`. Account: `docs/retail-accuracy.md`, "Pravus Research Facility".

| behaviour | was | now | evidence | tier |
| --- | --- | --- | --- | --- |
| Pravus navmesh | entrance ramp cut off by the plateau terrain | joined (terrain cut, 273 triangles) | client map and terrain; probes | measured |
| population | four NPCs | ten creature types in five region pools, per copy (OD-135, OD-136) | A4udsM0rcLo target frames | observed types/levels; positions measured; stats analogue |
| entrance, Tarmok, production | absent | keypass Trainees, level-9 boss, Prototype every 2.5 s (OD-137) | footage; TTH; TaRapedia | observed/inferred; rate inferred |
| Johnson | pre-1.7 /loc, no package | beside Perkins, package 106 | 1.7 live notes | inferred; package original row |
| Nylla, Parsons | no packages | 420, 450; Nylla also on the gangplank (OD-138) | objectiveconversation; TaRapedia | inferred |
| 593, 575, 323, 924 | unseeded | offerable (575/4-5 held; 593 without 574, OD-139) | client tables; TaRapedia; footage | inferred, pre-1.4 amounts |
| destroy counters | a destroying hit completed the objective | it advances the objective's counter | client counter "Infestation Remaining" | inferred |
| 575/1-2 and 323 client rows | would complete on the first conversation | withheld | objectiveconversation texts | inferred |

## Divide operations: Minos Caverns, Timora Mines, Torcastra Prison (2026-09-27)

The Divide instance dossier, checked against the 1.16.5.0 client and maps and the rebuilt navmeshes. Tiers and gaps: the
manifest rows of `DivideOperationsInstances`, OD-145 to OD-152 and `docs/evidence/divide-operations-instances-20260927.json`.
Account: `docs/retail-accuracy.md`, "The Divide operations".

| behaviour | was | now | evidence | tier |
| --- | --- | --- | --- | --- |
| Minos navmesh | 27 islands; entrance, Horlo, canyon, Bane base apart | one mesh at 0.2 x 0.1 m (OD-148) | client map collision; probes | measured |
| Timora navmesh | Fuel Egress / Command Center an island | joined (terrain cut at one chunnel entrance, OD-149) | client map and terrain; probes | measured |
| Kearney, Pastre, Hamilton | pools on hospital/vendor markers | placements at TTH /locs; pools 0/0 | TTH 13803, 15113 | inferred / measured |
| Hamilton's package | 1527 (the computer bank) | 1526 | objectiveconversation 356/4, 356/11 | original |
| Timora and Torcastra NPCs, bosses | absent | Morrow, Sanchez, three bosses, Horlo, Cisco, Torqua, Ferme (OD-145) | client names; TTH; TaRapedia | names original; bodies and stats analogue |
| 340, 1905, 792, 392 | unseeded | offerable (1905/1 held, OD-146; 392 without 383, OD-147) | client tables; TTH; TaRapedia | inferred, 392 pre-1.4 amounts |
| Tyler | stationary | walks 392 to the Field Medic | client log 527; TTH | inferred |
| 403, 404, 1276, 384, 391, 397, 594, 356, 1860, 1861 | unseeded | held (OD-151, OD-152) | dossier gaps | gap |


## Concordia Palisades: the dossier's finishable missions (2026-09-27)

The Palisades mission dossier (`research/20260927-palisades-dossiers`), checked against the 1.16.5.0 client tables and
map, TaRapedia's revision histories, the D10/D12.5 live notes and the repo navmesh. Tiers and gaps: the manifest rows and
changes of `PalisadesDossierMissions` and OD-153 to OD-160. Account: `docs/retail-accuracy.md`, "Concordia Palisades".

| behaviour | was | now | evidence | tier |
| --- | --- | --- | --- | --- |
| 1812, 1813 (Arizpe) | unseeded | shrines 333/322, 9,000 XP, 1,800 credits each (OD-159) | logosstone; TaRapedia 2008-01-05 | inferred, pre-1.4 amounts |
| 1988 A Spiritual Pilgrimage | unseeded | Brocail; Knowledge, Man, Planet; 20,000 XP, 3,000 credits, AccuMax choice | D10 notes; client log; TaRapedia 2008-07-29 | inferred, post-1.4 |
| 2014 Crash Course | unseeded | Matlin; Derac 136 -> Gantic's datapad -> Matlin 1214; armor choice, no amounts | D12.5 notes; objectiveconversation; TaRapedia 2008-10-25 | inferred; packages original |
| 1795 Bloody Booty | unseeded | Mullen; Barbrix's Boot; 19,000 XP, 2,850 credits (OD-159) | client item class 28451; TaRapedia | inferred, pre-1.4 amounts |
| Gantic, Barbrix | never respawned once killed | back after 60 s (OD-160); stats unchanged (OD-156) | OD-48/OD-55 delay | analogue |
| package 134 | on the Valverde Pools duplicate 510196 | on the Palisades Orton 199086; duplicate's pool 0/0 (OD-158) | objectiveconversation 331/337; client log of 331 | inferred |
| teleporter 624 text | "I thnk Viands Village" (upstream) | "Waypoint: Viands Village" | uimapmarker 133182640965832 | original |
| mission levels | none | giver's level: 25, 25, 15, 30, 20 (OD-155) | TaRapedia NPC pages | analogue |
| 368 | skips its Warnet kill and timer | unchanged (OD-157) | client objective 368/1 counter | gap |
| 1799-1801, 337, 366 <- 1988, Aldrin | proposed | held / left (OD-153, OD-154) | dossier; D12.5 notes | gap |

## DIT reference review: shrines, boss levels, Crucible, missions (2026-09-27)

The DIT bot project's reference (`research/20260927-dit-tr-videos`) reported six discrepancies; each was checked against
the 1.16.5.0 client and maps and dated TaRapedia, Giddy Gamer and Ellatha pages (`research/20260927-dit-discrepancies`).
Tiers and gaps: the manifest changes of `LogosGiveNegativeSwap` and `TarapediaBossLevels`, OD-171 and OD-172. Account:
`docs/retail-accuracy.md`, "DIT reference review".

| claim | verdict | was | now | evidence | tier |
| --- | --- | --- | --- | --- | --- |
| Give/Negative swapped | confirmed | Give (15) on the Foxtrot hilltop, Negative (33) in the Xanx cave | exchanged; stones unchanged | client map pedestals and cavern; TaRapedia 2007-09..2008-09 and its 2007-12..2008-08 wrong-glyph bug; Giddy 2007-11-26 | inferred (coordinates original) |
| 21 shrines > 5 m off | 2 confirmed, 19 refuted | all 20 pedestalled rows on client pedestals | unchanged | client map pedestals; reference typos, rounding, old Caves of Donn frame | original |
| enemy levels wrong | partly | 12 bosses at upstream's per-zone value | TaRapedia's level (OD-171); hp kept (OD-172) | TaRapedia histories; gamecontext suggested ranges | inferred |
| other boss levels | unverified | upstream values | unchanged (`GAP-UPSTREAM-BOSS-LEVELS`) | no level source | gap |
| later boot camp | confirmed, intended | D11 set | unchanged | final client mission tables; bootcamp-client-evidence | original |
| Crucible = 1993 | confirmed | client names throughout | unchanged; catalog and survey labels wrong (research) | gamecontext 1993 "Ligo - Crucible" | original |
| 765 reference missions | research | — | 755 in the client (14 misspelt), 10 retired | client mission tables; TaRapedia | — |
## The Divide and Concordia Palisades: ambient populations (2026-09-27)

Built under OD-161 from two 2007 Divide recordings (cited stills), TaRapedia's dated revisions, the BradyGames guide,
Queen Stazzle's sightings and the 1.16.5.0 map and tables. Tiers and gaps: the manifest rows and changes of
`ConcordiaAmbientPopulations` and OD-161 to OD-170. Account: `docs/retail-accuracy.md`, "The Divide and Concordia
Palisades".

| behaviour | was | now | evidence | tier |
| --- | --- | --- | --- | --- |
| Divide ambient creatures | none (vendors, NPCs, upstream bosses only) | 14 pools, 37 creatures: Xanx, Thrax Privates/PFCs, Caretakers, Filchers, Warnets, Class IV Stalkers | ToO 1582; zuMHH5CxRj0, 3nZ7eca_vRA stills; TaRapedia; Brady; final map | species inferred (2 slots observed), places inferred/measured, 3 analogue |
| Palisades ambient creatures | none | 8 pools, 23 creatures: Boargar, Warnets, Fithik, Hunters, Thrax Technicians; no Stalkers | ToO 1809; client logs 351/353; TaRapedia; final map | inferred; Hightower spot analogue |
| creature levels, stats, speeds | - | band floors 12/15; world-seed counterparts (OD-162, OD-163) | TaRapedia 'Mob Levels=12-18'; seeded 368 | analogue |
| pool counts and respawn | - | footage/text sizes where recorded, else 3/4/2; respown_time 20; Stalkers 15 min | 3nZ7eca_vRA 3:00; TaRapedia The Tallest | observed 1, inferred, analogue (OD-164) |
| 358, 371, 372, 774, 755 (Divide) | unseeded | defined; kill count or sample drops from the new creatures and Rotting Sal | client objectives; TaRapedia | inferred; pre-1.4 amounts |
| 342, 1808 (Palisades) | unseeded | defined; Boargar + Shahrbaraz; twenty Fithik and the east mouth | client objectives; TaRapedia | inferred (OD-169, OD-170) |
| 368 | skipped its Warnet kill in Kogari's first conversation | counts five Warnets; row withheld; timer still held | client 368/1 counter; TaRapedia; Brady | inferred (OD-168) |
| Hominis Machina, Amoeboids, Predators, 1582 | - | held | no place / no final-era source | gap |
## Torden Plains, Torden Mires and Palisades from footage (2026-09-27)

Built under OD-176 from the Raisuly 1080p captures #10, #12, #16 and #17 (2008-11-22 to 2009-02-27), read by eye on
crop sheets, with radar registrations for places. Tiers and gaps: the manifest rows and changes of
`FootageAmbientPopulations` and OD-176 to OD-181. Account: `docs/retail-accuracy.md`, "Torden Plains, Torden Mires and
Palisades from footage".

| behaviour | was | now | evidence | tier |
| --- | --- | --- | --- | --- |
| Torden Plains ambient creatures | none (vendors, NPCs, upstream bosses only) | 11 pools, 18 creatures: Flaregashers, Striders, a Hunter/Thrax/Howler group, Beam Mantas, Atta Harvesters and a Soldier, Thrax Technicians | rCt23ux-kyU target frames and radar | species/levels observed, places measured +/-50 m |
| Torden Mires ambient creatures | none | 8 pools, 13 creatures: Thrax Technician Sergeant, Scavengers, Kael Master Sergeants, Caretaker and Lightbender Comm Officers | C2rGwo6fLw0; TaRapedia Thrax Soldier (Scavenger pairs) | observed/measured; one count inferred |
| Palisades new sites | none | 6 pools, 16 creatures: Treemites, three Cumbria Weald Bane squads, a Boargar, Gantic's escort | Nsz75UOlZz0, IDAug5iUa9c | observed/measured; Treemite split inferred |
| Palisades analogue levels/names (1244001/2/4/5) | 15, generic names (OD-162) | 18/16/16/17, Mature Forest Boargar, Irate Warnet Soldier, Hunter Corporal, Thrax Technician PFC | target frames | observed; names original |
| Palisades Warnet pool 1244201 | analogue place east of Hightower, 3 | Forean Ruins (419.2, 368.8), 1 | Nsz75UOlZz0 t=27-34 | measured, observed (OD-179) |
| creature stats, respawn, group sizes | - | world-seed counterparts; 20; the fewest the footage shows | OD-177, OD-178 | analogue / observed lower bound |
| 1067 Can't Survive Without My Radio | unseeded | defined at Major Ston; comm devices from the two officers; 66,000 XP / 5,800 cr | client objectives; TaRapedia rev 33372 (post-1.4), walkthrough spots | inferred; level and drop analogue |
| unregistered species, Abyss, Plateau, Howling Maw | - | recorded in the evidence file, not seeded | footage study | gap |

## Use-object range (2026-09-28)

`GAP-USE-OBJECT-RANGE`. `DynamicObjectManager.RequestUseObject` adds the requesting player to a usable's
`TriggeredByPlayers` (Logos shrines, footlockers and the other usables at lines 91, 107 and 119) without checking
the player's distance. Only waypoints check (`IsNear2m`). A client can therefore start a shrine's 10 s channel
from any distance. The original server's use range is not recorded. The client may enforce its own click range,
and that hasn't been checked either. Needed: the client's use-range constant (1.16.5.0 scripts) or original footage
of a failed out-of-range use. Until then the server does not enforce a range. DIT's Logos-hunting bots walk to
within 2.5 m, as a player would. Found by the DIT tr-chat lane, 2026-09-28.
