# Lightning arcs and effects — 2026-09-12

The final-live preservation target remains incomplete. The selected original
1.16.5.0 client establishes Lightning's effect parameters and incoming packet
shapes. It does not contain the authoritative server's arc selection, damage
pipeline, stun rolls, or storm schedule.

## Provenance and method

Acquisition, embedded version, and authentication limits are documented in
[client artifacts](client-artifacts.md). The package is a community upload of
original client files, with no independently recovered official checksum. Its
version is evidence for this client's behavior, not proof that no later live
server change occurred. No acquired game code was executed or imported.

Static xdis inspection and the strict literal-only table decoder recovered the
rows below. Bytecode headers in this selection are dated **2009-02-10 UTC**;
uncompyle6's displayed February 9 evening timestamps use the host timezone.
Raw files, manifests, extracted rows, inspection scripts, and test logs remain
outside the repository under
`/home/blizz/backups/rasa-net/research/20260912-client-artifacts/`.

`lightning-effects-selected/source-manifest.json`,
`supplemental-source-manifest.json`, and `actions-init-source-manifest.json`
record original archive members, byte lengths, SHA-256, and UTC timestamps.
Related action/scaling evidence is in `combat-selected/` and
[Lightning base damage](lightning-client-evidence.md).

| Original member | SHA-256 |
| --- | --- |
| `trpython.zip:client/actions/abilities/lightning.pyo` | `140f22ff022f00a743affdaf0b05e78152778a4f867c5199c5975860cf389a8e` |
| `trpython.zip:shared/damageinfo.pyo` | `27a36588a0009ef88ee488a936be7b80ea92f9fd349318fa10f72fde75cd8c4e` |
| `trpython.zip:client/gameeffects/damageeffect.pyo` | `cbd6e322882b0c35220146e383e9c21bbc89dc2d01df0af4b8dda7ff5f348732` |
| `trpython.zip:client/gameeffects/stuneffect.pyo` | `657b6302e3642cf45504cf1e2c287aed6742704e6144cb246b6d9123306d1b38` |
| `trpython.zip:client/physicalentity.pyo` | `e897cbe3e96a7a238feacc6949811be96721dd036c581942eb7824252db75982` |
| `trpython.zip:client/actions/targetedaction.pyo` | `b9c96045f3d3d3d670550c81c7216644cb38e3b84adcfae35ab496636bcc56d4` |
| `data/game.zip:generated/client/gameeffectdata.pyo` | `f2078f832ca5c1735d3fbd8f4ce3a0114083d9cc9d43db6aed412412c1461eb3` |
| `data/game.zip:generated/client/methodid.pyo` | `cc75aa76099de1bf4ea924bf68a4d8277fb3ac1e497ae5127b4e7ee7147412d8` |

## Trainable rank data

The authoritative locations within these artifacts are
`actiondata.abilityData[(194, rank)]`, `abilitydata.abilities[(194, rank)]`,
and English `uielementlanguage.lookup[(descriptionId, 1)]` (form 2 has the
same English description). Selected rows are retained in
`actiondata.pyo.lightning-selected.json`,
`abilitydata.pyo.lightning-selected.json`, and
`lightning-ui-literal-rows.json` in `lightning-effects-selected/`.
The last file preserves language bytecode offsets and constant indices;
literal dictionary keys were decoded rather than inferred from adjacent
constants or unreliable decompiled list entries.

| Rank / displayed name | Description ID | Arc radius | Base arc damage | Extra Sonic damage | Stun chance / duration | Storm |
| --- | ---: | ---: | ---: | --- | --- | --- |
| 1 Discharge | 1913 | absent | absent | absent | absent | absent |
| 2 Arc | 1914 | 12 m | 210 | absent | absent | absent |
| 3 Thunderstrike | 1915 | 18 m | **90** | 50% of base damage | absent | absent |
| 4 Field | 1916 | 24 m | 210 | 50% of base damage | 50% / 3 s | absent |
| 5 Storm | 1917 | 30 m | 210 | 50% of base damage | 50% / 3 s | 30 m radius, 60–90 Electric damage every 2 s, duration 6 s |

Raw property IDs are `ARC_RADIUS=63`, `ARC_DAMAGE=64`,
`EXTRA_DAMAGE_TYPE=44` (value 7, Sonic), `EXTRA_DAMAGE_PERCENT=45`,
`STUN_CHANCE=56`, `STUN_DURATION=57`, `EFFECT_DURATION_MS=100`,
`EFFECT_INTERVAL_MS=101`, `EFFECT_DAMAGE_MIN=102`, and
`EFFECT_DAMAGE_MAX=103`. Ranks 2–5 also contain
`PERCENTAGE_CHANCE=37` with value **100**. Its name and value are established;
its authoritative server consumer has not been recovered, so it is not
silently assigned to an arc probability roll.

All ranks use damage type 13 (Electric) and scaling type 2.
`LightningAbility.GetAbilityDataDict`, original source line 47, applies
`ScaleActorAmount` separately to arc damage and both storm damage bounds.
The recovered formula is `int(base * 2 ** ((level - 1) / 8.0))`.
The three-second stun value is not milliseconds; the storm's raw values
**6000** and **2000** are milliseconds. Tooltip IDs 1915–1917 establish the
Sonic percentage's base-damage denominator and the units above.

Player skill 49 caps at five ranks. Action variants 6–7 are retained client
data, not further trainable ranks. Variant 7 is named “Orson: Lightning” by
name ID 6132 and carries a different storm duration. It must not override the
rank-five row.

## Exact client packet consumers

`shared.damageinfo.DamageInfo.ClientInfo` (source line 85) emits a tuple of
**12** fields. `Init` (line 94) consumes it in the same order:

```text
rawInfo = (damageType, reflected, filtered, absorbed, resisted, finalAmt,
           isCrit, deathBlow, coverModifier, wasImmune,
           targetEffectIds, sourceEffectIds)
arcHit = (targetEntityId, rawInfo)
```

The final two fields are lists. Despite their original `EffectIds` names,
`Actor.AnnounceDamage` (line 2851) passes each entry to
`PhysicalEntity.AnnounceGameEffectAttach(typeId)` (line 779), which looks up
effects by **type**, not effect instance. No separate Sonic damage component
exists in this tuple. This is not evidence to add Sonic into Electric damage
or arbitrarily send it as a second primary hit.

`DamageBase.DoAbility` (line 41) pairs each primary hit entity with
`(rawInfo, onHitData)`. `LightningAbility.OnAbility` (line 41) unpacks
`onHitData` as a one-element tuple containing the arc list:

```text
PerformRecovery(
    194, rank, primaryHitIds, misses, missData,
    [(rawInfo, ([arcHit, ...],)), ...]
)
```

For each primary target, `OnAbility` attaches client effect **95**, level 1,
using the caster as source and `(arcData,)` as arguments.
`ArcEffect` inherits `ClientDamageOthersEffect.OnAnnounceAttach` (line 78):
it resolves each supplied target, constructs its damage information,
announces damage, and attaches its FX family to the original target using
the resolved secondary IDs. It does not choose those targets or roll damage.

`StormEffect.OnTick` (line 24) separately iterates `(targetId, rawInfo)`
entries in `dotData`, announces each damage record, then attaches client arc
effect 95 level 1 to `effectTarget` with the supplied `arcData`.
`PhysicalEntity.Recv_GameEffectTick` (line 651) resolves an **effect instance**
and forwards its remaining arguments. `methodid.GameEffectTick=278` is
independently retained in `methodid-gameeffecttick.json`. The wire shape is:

```text
GameEffectTick278(effectInstanceId, [dotHit, ...], [arcHit, ...])
```

There is no nested tuple around the two lists, and no caster ID or effect type
in this payload. The addressed physical entity owns the effect; the existing
effect stores its source. This consumer alone does not establish where the
server places a storm, whether it follows a moving actor, or when it ticks.

`Recv_GameEffectAttached` (line 601) takes
`(typeId, effectId, level, sourceId, announce, tooltipDict, *args)`.
The storm and stun classes require no extra attach arguments. Generated
`gameeffectdata` identifies these concrete effect classes and presentation:

| Effect type | Original class | Level / FX family |
| ---: | --- | --- |
| 95 `ARC_EFFECT` | `actions.abilities.lightning.ArcEffect` | 1 / 406 |
| 100 `LIGHTNINGSTORM_EFFECT` | `actions.abilities.lightning.StormEffect` | 1 / 405 |
| 86 `STUN` | `gameeffects.stuneffect.StunEffect` | 1–5 / 683 |

`StunEffect.OnAnnounceAttach` (line 16) requests uncontrolled state and a
movement block. `OnDetach` (line 25) releases both. This confirms the client
effect's control behavior; server movement/action rejection, refresh,
immunity, and stacking still require evidence and implementation.

Two traps remain explicit: `LIGHTNING_DAMAGE=349` references a
`LightningDamage` class that is absent from this client's Lightning module,
so that stale table row is not an implemented extra-damage protocol.
Also, `AttachClientEffectByType` defaults to five seconds for local visual
effect cleanup. That visual lifetime does not replace the storm's six-second
gameplay duration or define an arc damage schedule.

## Primary targeting and remaining server evidence

`DamageBase` is hostile, uses `TARGET_NON_FRIENDLY`, allows non-actors, and
does not allow blind shots. `TargetedAction.CheckAction` (source line 158)
accepts **HOSTILE, NEUTRAL, or OBJECT** for this target mode. Dead targets
are rejected. DamageBase's further object checks require a damageable,
non-destroyed object. These facts do not mean every non-player actor is an
enemy or that every creature is damageable.

The range check rejects native `actor.body.GetDistance(target.body)` values
strictly greater than `GetMaxRange(actor)`; the exact boundary is accepted.
The native body's distance implementation is not in the recovered Python,
so center-to-center `Vector3.Distance` is not established as an exact match.
`BaseActorAction.GetMaxRange` calls `client.actions.GetMaxRange`, whose
source line 292 method returns action table `maxRange` (**60** for ranks 1–5).
Its apparent range-modifier loop iterates a literal empty tuple; raw bytecode
offsets 58–61 establish that fact. The client also requires line of sight and
that the target be in front of the user controller.

The `TargetedAction.CheckAction` decompilation contains corrupted boolean
expressions. Conditions above were checked against the retained raw
`client-actions-targetedaction.pyo.selected-dis.txt`: non-friendly category
branches at offsets 485–516, distance comparison at 663, LOS/front checks at
685–902. Do not copy the malformed decompiler text into the server.

`Actor.Recv_TargetCategory` (line 789) receives the category from the server;
`GetTargetCategory` (line 811) returns that stored category. The client does
not recover the server faction/reaction matrix. Neither the primary target
checks nor native UI area targeting establish the secondary arc selection
algorithm.

Exact remaining gaps are: arc origin/radius geometry, maximum targets,
ordering and chaining, repeat-hit exclusion, target factions and line of
sight for secondary hits, Sonic delivery and mitigation ordering, stun roll
scope and immunities, storm placement/movement, first/last tick inclusion,
reselection per tick, interruption/owner death cleanup, and full damage
roll/modifier rules. These require original server artifacts or authentic
final-era gameplay/packet captures. They remain open rather than becoming
guessed gameplay.

## Implementation and validation

`LightningEffectData` now preserves the five ranks' optional properties and
scaled arc/storm bounds as immutable data. `DamageInfoData` serializes the
complete twelve-field snapshot; `LightningArcHit` serializes the shared
target/damage pair; `LightningStormTickPacket` snapshots the two lists and
emits the proven opcode and shape.

`LightningRecovery` now snapshots each primary hit and its own arc list,
writes the hit's actual damage type and **FinalAmt**, and preserves effect
lists. Previously it hardcoded Electric, reused `Missile.DamageA` for every
hit, and discarded arc/effect data. MissileManager assigns Electric to the
actual Lightning hit record. No extra amounts, target chains, stun effects,
or storm timers are invented by these serializers. Runtime arc lists still
remain empty until the missing effect mechanics are reconstructed.

Focused tests cover all rank rows, absent properties, rank-three arc damage,
scaling, excluded non-player variants, all twelve damage fields, large entity
IDs and damage values, two distinct primary hits and their arc lists, empty
effect/tick lists, and mutation after packet enqueue. Verification is isolated
with `--network none`; no production probes or deployment are part of this
change. All **24** focused `LightningEffectTests` and `LightningAbilityTests`
passed. Test log: `lightning-effects-selected/focused-tests.log`.

Confidence is high for the literal values and these consumers in this client,
and explicitly incomplete for the final authoritative server mechanics.
