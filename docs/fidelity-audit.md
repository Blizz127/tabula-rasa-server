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

### Mission 430 "Mortar By Numbers" cannot be completed

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
named 8186, carrying weapon 10604, at the four launcher positions, is the candidate — **inferred**, so it waits on
an owner decision before it is built. Open: **GAP-W3-430-MORTAR-CREATURE**.

### 2,495 armour pieces gave no armour — fixed

The worn-armour total was built only from `itemtemplate_armor.armor_value`, which has 15 rows, so every other
armour template — including 15 sold by vendors — added **0** when worn. Worn armour is now the seeded value where
one exists, and otherwise the client's own `armorclass.max_damage_absorbed ÷ 10`, rounded half up
(`ManifestationManager.BodyArmor`).

The scale is observed: the original client's tooltip for the boot-camp crate's gloves reads **"Body Armor: 28 |
Regen Rate: 1 per sec"** (A3-034, t=319.6), and the uncommon Motor Assist gloves absorb 281. All 15 seeded values
follow the same rule; truncation misses 6 of them. `min` and `max` are equal for all 3,377 classes. What is *not*
observed is how an exact half rounds: 238 classes end in 5, and half-up is inferred. Recorded as
**GAP-ITEM-ARMOR-ROUNDING** (inferred tier).

A related open point: the crate's gloves in the footage are the *uncommon* V04–V07 row (281 → 28); the seeded
crate gives template 13096, the common V01 row (234 → 23). Item names resolve through the prefix families of
2026-09-15, so which template the original crate held is not settled. Open: **GAP-BOOTCAMP-CRATE-TIER**.

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

Every class ability beyond Recruit (the T2–T4 ability skills of `skilldata`; `abilitydata` has 1,084 `(action, rank)` rows) is
**unimplemented**: `PerformAction` logs "unsuported" and nothing happens. That is a missing system, not a
misidentified one; the client data for each is complete, the original server's resolution rules are not.
Open: **GAP-CLASS-ABILITIES**.

### Weapons

Damage, clip size, ammunition class and damage type come from `weaponclass`, which is identical to the client's.
Range, windup, recovery and refire come from the server-only `itemtemplate_weapon` (2,444 rows): the client's
weapon classes carry `range_type` 0 and zero overrides in every row, so nothing client-side contradicts them.

## Still to audit

1. **Placed NPCs and their dialogue packages**, continuing GAP-W3-UNBOUND-CONVERSATION-PACKAGE.
2. **Creature flags.** `CreatureInfo` is sent an empty flag list. The client reads `BIOLOGICAL`, `MECHANICAL`,
   `MACHINA`, `CAN_BE_REVIVED` and the epic/boss indicators from it (harvest, salvage, heal disc, repair tool,
   corpse abilities, overhead markers). With none, the client is permissive rather than broken. No per-creature
   flag data survives anywhere in the research; open: **GAP-CREATURE-FLAGS**.

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
- **`LockToActor` / `UseInterruptible` / `UseInterrupted` — recorded, not built.** They mark a usable as in use by
  another player (`IsAlreadyInUse`) and play its channelling effect: `usabledata.specialFX` has one for 393
  classes, and of what the world places only the **Logos shrines** (state 81) carry one. The client code gives
  their meaning but not the order the original server sent them in, and nothing else in the research does.
  Open: **GAP-USABLE-ACTOR-LOCK**.
- **`BlockInfo`** is a no-op in the base usable. The game-effect and wargame handlers are unimplemented systems
  rather than mis-identified things, and belong to their own work.
