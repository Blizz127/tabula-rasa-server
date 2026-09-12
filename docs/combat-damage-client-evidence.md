# Combat damage and actor attributes — 2026-09-12

The target remains 1:1 final-live preservation. This pass establishes the
resistance conversion curve, original weapon/armor class data, damage report
semantics, and the actor attribute constructor protocol. The authoritative
damage pipeline and player/creature stat formulas remain incomplete.

## Original sources and retained artifacts

The original client package's embedded version is **1.16.5.0**. Acquisition
and the lack of an independent official package checksum are recorded in
[client-artifacts.md](client-artifacts.md). This package is strong versioned
client evidence, not proof of every final server hotfix. Python bytecode was
statically parsed with xdis and uncompyle6; acquired modules were not imported
or executed. Generated tables were decoded with the strict literal-only reader,
not executed or copied from unreliable decompiler list output.

All new raw material is outside the repository in
`/home/blizz/backups/rasa-net/research/20260912-client-artifacts/combat-damage-selected/`.
`source-manifest.json` records archive members, SHA-256, byte lengths, and
UTC compilation timestamps. Additional manifests are `item-source.json`,
`combatmessages-source.json`, `damage-ui-sources.json`, and
`attributes-supplemental-sources.json`. Timestamps are **10 February 2009 UTC**;
the decompiler's February 9 evening comments use the host timezone.

| Original member | SHA-256 |
| --- | --- |
| `trpython.zip:shared/damageresistance.pyo` | `18518f0c74c8f80dfbb7359ab5ceafe4944af7679e8553db5dbc3f54b5a76410` |
| `trpython.zip:shared/gameconstants.pyo` | `e0fc260a81d20fe01023ad79e5460773d46aba3d7feeba7291373cba0026ed40` |
| `trpython.zip:client/augmentations/actorattribute.pyo` | `66b7b5aaff68d00e9c5fdbef5370fce78f17bcb57ba2d479a3f51efd58329f48` |
| `trpython.zip:client/augmentations/weapon.pyo` | `38357c191fd8214e1b911ed51cb4b03989105f8b6b71eac54f253558a8f02594` |
| `trpython.zip:client/augmentations/armor.pyo` | `f5913c6a6599ee87c7693cd608e630cd09d3f2d36ec20aca5e6b1c7e75865ed6` |
| `trpython.zip:client/actions/weapons/baseweaponattack.pyo` | `c138c223eceec99a6ba8982c9a07071c806bdc445ea2d72440045586dbe54b8b` |
| `data/game.zip:generated/client/weaponclass.pyo` | `49e514546f8e30ee906b9e63c04e2c541af98bc681b8005e3a9f4f225f943591` |
| `data/game.zip:generated/client/armorclass.pyo` | `c01b8bf0130c349b600ee202f8485ec10005fff553921fda2a28f202b6e6ea33` |
| `data/game.zip:generated/client/itemclass.pyo` | `cd0e4367ff60ede526ef5429e3fdbf73f99873a15bcb4c426fed28e7bbc13ed9` |

The actor attribute source was previously retained in `combat-selected/`;
the actor consumer is in `lightning-effects-selected/`. Selected raw
disassemblies are retained in the new directory, with each method's original
source line and bytecode offsets.

## Resistance: a final-version formula with independent live evidence

`shared.damageresistance.GetDamageMultiplierFromResistValue`, original source
line 6, implements exactly:

```text
if resistanceValue < 0:
    multiplier = (100.0 - resistanceValue) / 100.0
else:
    multiplier = 1.0 / ((100.0 + 2.0 * resistanceValue) / 100.0)

resistedPercent = (1.0 - multiplier) * 100.0
```

The negative branch is bytecode offsets 13–24, positive branch 29–48.
`GetResistPercentFromResistValue`, line 17, performs the final conversion.
`client.ui.attributeswindow` uses that shared conversion for resistance
tooltips. `Actor.Recv_ResistanceData` receives resistance values from the
server; it does not calculate equipment, skill, or creature contributions.

The [official live Deployment 14 notes](https://web.archive.org/web/20090218143923id_/http://eu.playtr.com/en/news_article/deployment_14_patchnotes_known_issues_november_2008_live),
**Game Mechanics**, independently announce diminishing returns for both
players and creatures and give the following examples:

| Resistance value | Displayed mitigation |
| ---: | ---: |
| 10 | 16.67% |
| 25 | 33.33% |
| 50 | 50.00% |
| 75 | 60.00% |
| 100 | 66.67% |
| 150 | 75.00% |
| 200 | 80.00% |
| 250 | 83.33% |

These live examples agree with the February client function. Earlier direct
percentage interpretations are obsolete. The official capture is retained in
`20260912-death-retail/20090218143923-deployment_14_patchnotes_known_issues_november_2008_live.html`
(SHA-256 `fd05ed09e00f7c0ebdc911eb70b6e44092ec1f164f77da56c346237dbb74e1c4`),
with the relevant text at lines 90–99 of its `.html.txt` companion. The
September/October, November 14.5, and February 16.4/16.5 retained notes did
not supply a replacement curve; this is not a claim that every hotfix survives.

`DamageResistance` now preserves this exact double-precision conversion,
including negative vulnerabilities and fractional values. It intentionally
does not round damage, clamp resistance to 0–100, or guess when resistance
applies relative to absorption, reflection, filters, critical hits, or armor
piercing. It is a helper for the future damage pipeline, not a claim that
runtime resistance has been implemented.

## Original weapon, armor, and item data

Strict decoded tables and summaries are retained as
`generated-client-*.pyo.literal.json`, their `.summary.json` companions, and
`raw-data-audit-summary.json`.

`weaponclass.lookup` has **2,946** class rows. `Weapon.__init__` names its first
18 slots as:

```text
templateId, attackActionId, attackArgId, drawArgId, stowArgId, reloadArgId,
ammoClassId, clipSize, minDamage, maxDamage, damageType, velocity,
weaponAnimConditionCode, windupOverride, recoveryOverride, reuseOverride,
reloadOverride, rangeType
```

Damage type is slot **10**, separate from the two damage amounts at slots
8–9. The rows use eight types: Physical 1, Fire 2, Ice 3, Virulent 4, EMP 5,
Laser 6, Sonic 7, and Electric 13. For reproducible examples, class 629 has
damage 40/40, type 1, action 174/1; class 3419 has 7800/7800, type 6,
action 1/78; class 4082 has 3000/3000, type 13, action 1/87. These examples
are class rows, not claims that each is a player-obtainable weapon.

Of the weapon rows, 2,934 have equal minimum/maximum values and 113 have
both amounts zero. Therefore a blanket randomized weapon damage formula or
replacement of zero with a guessed baseline is not established by this table.
The table includes NPC, player, special, and potentially obsolete classes.
Availability, template selection, rarity, item modules, and per-instance
server modifications must be reconciled before importing it wholesale into
the active world database.

`armorclass.lookup` contains **3,377** rows. `Armor.__init__` consumes each as
`(minDamageAbsorbed, maxDamageAbsorbed, regenRate)`. All selected-client rows
have equal first/second values. These are armor class data; the getter names
alone do not prove per-hit random absorption, the actor armor pool formula,
or how multiple worn pieces combine. `Item.GetCurrentHitPoints` and
`Recv_ItemInfo`/`Recv_ItemStatus` separately track item durability, so item
durability must not be conflated with the actor's combat Armor attribute.

`itemclass` supplies 9,115 class rows, 5,293 class requirement rows, 30,225
template-to-class mappings, 70 race requirement rows, and 19,564 template
skill requirement rows. `Item.Recv_ItemInfo` additionally receives instance
template, durability, quality, class module IDs, and loot module IDs from the
server. Client tables alone do not reconstruct those instances or their
fully modified weapon and armor stats.

## Resolved damage reports and the remaining partition gap

The complete twelve-field `DamageInfo` format is documented in
[Lightning effect evidence](lightning-effects-client-evidence.md).
`BaseWeaponAttack.DoHits`, original source line 188, consumes each weapon hit
as `(entityId, rawInfo, onHitData)`. Its damage type is part of `rawInfo` and
is passed to the original damage announcement/presentation path.

`client.combatmessages.OnCombatDamage` computes
`int(finalAmt + absorbed + resisted)` and reports the components separately.
`OverheadWindow.HandleFloatDamage` displays `finalAmt + absorbed` in its
simple mode, with separate values in its detailed mode. Language IDs 888,
889, and 890 name damage, absorbed, and resisted amounts. Their strict rows
are in `damage-player-message-rows.json`. This establishes that absorption
must not also be counted inside `finalAmt`.

The emulator still uses this provisional calculation for all damage types:
subtract from armor first, then subtract any remainder from health. It does
not yet resolve original resistance, armor piercing, filters, reflection,
critical hits, body type, or damage-type-specific behavior. Final-client
tooltip rows 1224, 1225, 1241, 2483–2486, 2692–2694, and 3146–3148
describe armor-bypassing weapon/ability behavior. Their exact keys, text,
and bytecode offsets are in `combat-ui-literal-rows.json`. The universal
armor-first rule cannot represent these systems.

Within that existing calculation, the hit report now records the actual
armor decrement in `Absorbed` and `DamageA - armorDecrease` in `FinalAmt`.
The armor and health decrements themselves are unchanged. Reporting the
remaining amount before the current-health clamp preserves the previous
overkill choice; original server overkill reporting remains unverified.
No reflected or resisted values are invented for unimplemented mechanics.

Normal weapon launches now propagate the weapon class's damage type through
`Missile` to the resolved hit. Unspecified callers retain their prior fallback
(Lightning Electric, others Physical), so untyped NPC action damage remains
a reconstruction gap. `WeaponAttackRecovery` snapshots and writes each hit's
actual type, amount, and effect lists through `DamageInfoData`, instead of
hardcoding Physical and repeating `Missile.DamageA`.

## Actor attribute protocol and unproven formulas

The generated actor table identifies Body 1, Mind 2, Spirit 3, Health 4,
Chi 5, Power 6, Awareness 7, Armor 8, Speed 9, and Regen 10. It does not
contain player race growth or creature health scaling formulas.

`ActorAttribute.__init__`, original source line 31, has this actual argument
order, directly established by its code object's argument names and stores:

```text
actorId, type, normalMax, currentMax, current, refreshAmount, refreshPeriod
```

`Actor.Recv_AttributeInfo`, line 752, prepends `(actorId, type)` and passes the
received tuple unchanged (bytecode offsets 38–60). Its older comment lists
a different order and is incorrect. `AttributeInfoPacket` now sends
`(normalMax, currentMax, current, refreshAmount, refreshPeriod)` and snapshots
the values. Previously it swapped normal maximum and current, corrupting
depleted resource values and baseline display.

The corrected order exposed two existing stat-initialization self-assignments:
Body and Mind kept their initial zero current values while Spirit used its
calculated maximum. All three non-consumable primary attributes now use their
calculated current maximum. Stat recalculation also stops replacing remaining
Armor with an armor regeneration-rate accumulator; it preserves the current
resource and clamps to the recalculated maximum, as the neighboring resource
paths already do. These are consistency repairs to the existing stat model,
not verification of its historical growth, bonus or regeneration formulas.

`UpdateHealth`, `UpdateArmor`, and `UpdatePower` instead take
`(current, currentMax, refreshAmount, whoId)`. Their shared client receiver
sets refresh period to **1 second** (source line 942, offsets 55–58).
`ActorAttribute._EvaluatePredictedRefresh` uses elapsed `gameclient.Time()`
seconds times refresh amount divided by refresh period; the one-second
scheduled callback is separate from that rate denominator. This is client
prediction overridden by server updates, not an authoritative regeneration
formula.

Current creature initialization supplies refresh period **1000**, while
players start at zero; the emulator does not use `RefreshPeriod` to calculate
server regeneration. Thus its initial creature display and subsequent
resource updates use inconsistent prediction rates. This pass does not
blindly divide the packet field by 1000 or invent creature regeneration.
The source rates and actual regeneration lifecycle must be reconstructed
together. Health and armor update packets now retain their values when queued,
so later damage, refill, or death mutations do not overwrite earlier reports.

The current player `HealthBaselinePerLevel`, race growth, health/power/regen
formulas, and armor aggregation originate in emulator code and are not proven
by these client modules. `client.augmentations.creature` supplies presentation
and behavior hooks; it does not calculate creature attribute maxima. The
world creature statistics and generic fallback stats still require original
data or captures for each creature/rank/level combination.

The following literal shared constants are recovered in
`gameconstants-damage-literal-scalars.json`, including exact assignment
offsets. They are useful leads, not permission to invent their missing
server expressions:

| Name | Value |
| --- | ---: |
| `BODY_ARMOR_DIVISOR` | 1.5 |
| `MIND_DAMAGE_DIVISOR` | 3.0 |
| `SPIRIT_CRIT_DIVISOR` | 15.0 |
| `BASE_CRITICAL_CHANCE` / `CRITICAL_DAMAGE_MODIFIER` | 5 / 1.5 |
| `PVP_DAMAGE_MODIFIER` / `PVP_CRITICAL_DAMAGE_MODIFIER` | 0.5 / 1.25 |
| `EMP_DAMAGE_MODIFIER_BIOLOGICAL` / `MECHANICAL` / `MACHINA` | 0 / 1 / 0.5 |
| `VIRULENT_RESIST_FROM_ARMOR_MOD` | 0.75 |
| `ARMOR_DECAY_PER_POINT_ABSORBED` | 0.0025 |
| `IN_COMBAT_REGEN_MODIFIER` | 0.2 |

The scan in `consumer-search.json` found no Python consumer of the listed
Body/Mind/Spirit divisors in this client. The exact baseline subtraction,
stacking order, rounding, and native/server consumers remain missing.
[Official D10.6 live notes](https://web.archive.org/web/20080725184546id_/http://eu.playtr.com:80/en/news_article/deployment_106_patchnotes_known_issues_23rd_july_2008_live),
**Classes, Combat & Creatures → Bug Fixes**, independently confirm a Mind
damage bonus for Lightning and several other abilities; they do not supply
its numerical formula. Existing emulator coefficients must not be promoted
to final-live truth merely because similarly named constants exist.

Player lethal damage still triggers the emulator's immediate health refill
placeholder. This pass does not activate or claim original player death,
hospital recovery, trauma, or regeneration. Correct packet snapshots expose
the state at each update; they do not repair that missing gameplay lifecycle.

## Validation and next implementation paths

`CombatDamageTests` compares the shared resistance helper with all eight
official live examples, negative/fractional cases, and high resistance. It
checks damage type through launch and impact, actual per-hit serialized
fields, armor-only/mixed/unarmored reporting, initial attribute field order
with unequal values, and queue snapshots across further damage/refill.
All **52** focused tests passed, including Lightning and creature death regressions.
The isolated container has `--network none`; test log is
`combat-damage-selected/focused-tests.log`. No live DB writes or deployment
are part of this work.

The integration follow-up corrected older `MissileRecoveryTests` expectations
that still counted absorbed armor inside final damage. All **10** focused
missile tests passed. Weapon attack, weapon melee, and Lightning now verify
both their resolved hit records and complete packet tuples: 40 absorbed,
20 remaining, total 60 in the existing test scenario. Follow-up log:
`combat-damage-selected/missile-recovery-tests.log`.

Next grounded work is to reconcile the recovered class/template rows with
each active world record; recover instance module and resistance inputs;
trace authoritative armor piercing, damage-type filters, and modifier order;
then connect the proven resistance helper and resolved report fields to that
pipeline. Player/creature maxima, race growth, health/power/armor regeneration,
and overkill semantics remain explicit server evidence gaps.
