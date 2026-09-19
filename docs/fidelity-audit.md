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

### Mission 430 "Mortar By Numbers" cannot be completed

Its four launchers are placed as destroyables of class **7478 `ArchBaneGenObjMortarlauncherBaseV01`** — which has
**no augmentations and `target_flag` 0**: plain scenery to the client, which can neither target nor damage it.
The client has no destroyable mortar class (only 7478 and its `…Destroyed` variant 7479, both statics). The
client's creature-name table, however, has **"Bane Mortar" (8186)** and its Light/Heavy/Ultraheavy siblings: in
the original the mortars were **creatures**, like the turret emplacements (`Emplacement_Bane_Turret_Standard`,
Creature + Harvestable, targetable). The world seed has none, and the class the original used is not recovered.
Open: **GAP-W3-430-MORTAR-CREATURE**.

### 2,495 armour pieces give no armour

The worn-armour total is built only from `itemtemplate_armor.armor_value`, which has 15 rows. Every other armour
template — including 15 sold by vendors — adds **0** when worn. The 15 existing values all equal the client's
`armorclass.max_damage_absorbed ÷ 10`, rounded. The 2026-09-13 sweep traced that `÷10` to another emulator with no
client counterpart and warned against extending it. The footage now bears on it: the original client's tooltip
for the crate's gloves reads **"Body Armor: 28 | Regen Rate: 1 per sec"** (A3-034, t=319.6). The client never
reads the absorption values for display, so 28 was server-sent, and it is on the ÷10 scale. Still open before
any bulk fill: the exact rounding, and which template the original gloves were (item names resolve through the
prefix-family structure of 2026-09-15, not by template id). Open: **GAP-ITEM-ARMOR-VALUES**.

`BODY_ARMOR_DIVISOR` (1.5) in `shared/gameconstants` is not that divisor: the client never uses it, and it
matches the server's existing Body-to-armour bonus (Body ÷ 1.5 %).

## Still to audit

1. **Packet contract per augmentation.** For each augmentation carried by anything the server spawns, the
   client's initial-state `Recv_` methods versus what the introduction actually sends. This is the
   generalisation of the target-category and damage-info bugs, and it would have caught three of them.
2. **Abilities.** Every ability a class can learn, against the actions the server can resolve (the missile
   path still logs "unsupported missile actionId ... using default" for anything it does not name).
3. **Weapons.** Weapon templates against the client's `weaponclass` — ammunition, damage type, range.
4. **Placed NPCs and their dialogue packages**, continuing GAP-W3-UNBOUND-CONVERSATION-PACKAGE.

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

The ones most likely to be *identity* defects of the kind this audit is for — a thing the client mis-classifies
because it was never told what it is — are `UseInterruptible` / `UseInterrupted` / `LockToActor` on usables and
`UpdateEscortStatus` on escorted creatures. The game-effect and wargame handlers are unimplemented systems rather
than mis-identified things, and belong to their own work.
