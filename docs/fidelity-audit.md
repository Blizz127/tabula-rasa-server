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
26879, vest 12208 (inferred from the gloves; OD-54, pending owner review).

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

1. **Placed NPCs and their dialogue packages**, continuing GAP-W3-UNBOUND-CONVERSATION-PACKAGE (18 objectives
   left) and GAP-W3-MISASSIGNED-PACKAGE (Reyko).

Done since: **class abilities** — all 53 abilities an active skill grants (`AbilityRequirements.RequiredLogos`,
the client's logos sequences) resolve through a server handler: direct damage, a game effect, or Sprint's own
path (checked 2026-09-19). The last ten are OD-56 stand-ins, described above.

### Misassigned dialogue — Brody fixed (2026-09-19)

Field Lt. Brody carried package 550, the found commander's "Man, am I glad you showed up ... get back to Brody", so
mission 670 "Missing Strike Team" completed its first objective at Brody and could never complete its second
("return to Field Lieutenant Brody", package 145, carried by no one). **DevilsDenFransisco** places Captain
Fransisco (client name 6587) in the Devil's Den at TaRapedia's coordinates (-59, 96, 50; navmesh floor 95.82) with
550, and gives Brody his own 145, which also binds 361/1. The appearance is OD-45's AFS analogue; his following
the player through the instance is not built (GAP-ESCORT).

Captain Reyko still speaks the Irendas console's line (1213). Moving him to his own 1200 would bind 1113/7,
1125/1 and 1310/1 but unbind 1112/1 "Into The Hive", which today completes at him; no source gives the
console's position. Left for the owner to decide.

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
