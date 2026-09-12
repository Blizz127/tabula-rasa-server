# Lightning base damage — 2026-09-12

The preservation target remains the final live game, 1:1. This change replaces
one fixed damage sample with the recovered client's rank and level scaling. It
does not complete Lightning's combat implementation.

## Original evidence

The selected 1.16.5.0 client and its authentication limitations are recorded in
[client artifacts](client-artifacts.md). Original bytecode was read statically
with xdis 6.1.7 and uncompyle6 3.9.3; no acquired game code was executed.
The generated action table was decoded with the strict literal-only reader
described in [mission evidence](river-recon-client-evidence.md), because the
action table does not decompile reliably.

Retained evidence is in
`/home/blizz/backups/rasa-net/research/20260912-client-artifacts/combat-selected/`:
`source-manifest.json`, `action-consumer-manifest.json`,
`lightning-action-literal-rows.json`, `ability-property-names.json`, and
`shared-scaling.raw-dis.txt`. The source action table is in `skill-selected/`.
The original compiled scaling timestamp is 2009-02-10 03:55:27 UTC; decompiler
comments display the host's local timezone instead.

| Original member | SHA-256 |
| --- | --- |
| `generated/client/actiondata.pyo` in `data/game.zip` | `9520744380868987a093be20bdbca7de26cd7899e07c50c23f00ae3d764c1ecb` |
| `shared/scaling.pyo` in `trpython.zip` | `0e3b6fa9af4b8e410f4fe6814c881d76e05500ad7a590dacc037e3e8e44db3d4` |
| `client/actions/abilities/baseactorability.pyo` | `b4d28fe9b85005cb3b13911b790e9befdf0bb78c3250344626591d0a9ae614ff` |
| `client/actions/abilities/lightning.pyo` | `140f22ff022f00a743affdaf0b05e78152778a4f867c5199c5975860cf389a8e` |
| `generated/shared/damagetype.pyo` in `data/game.zip` | `b035ecf40f1fadc606bdc1a517e118a088cf0f9566cf672c6b733a59eb762276` |

`actionModules[194]` identifies `AA_RECRUIT_LIGHTNING`, implemented by
`abilities.lightning`. `abilityData[(194, rank)]` properties 3 and 4 are base
minimum and maximum damage; property 40 is scaling type **2**, and property 41
is damage type **13 (Electrical)**.

| Trainable rank | Base minimum | Base maximum |
| --- | ---: | ---: |
| 1 | 180 | 240 |
| 2 | 240 | 300 |
| 3 | 240 | 300 |
| 4 | 240 | 300 |
| 5 | 240 | 300 |

The active skill catalog limits Lightning to five ranks. Other action rows
6–7 exist, but do not authorize players to train those variants.

`shared/scaling.py`, `ScaleActorAmount`, original source line 20, selects
`ScaleExponentialAmount(baseAmount, actor.GetExperienceLevel() - 1, 8.0)` for
type 2. Raw bytecode offsets 99–124 establish those arguments. The called
function, source line 8, offsets 0–26, computes:

```text
int(baseAmount * pow(2, (experienceLevel - 1) / 8.0))
```

Python's `int` truncates the result. The shared function's other supported
type, linear type 1, is also retained exactly in `AbilityScaling`; no type
means an unchanged amount. `BaseActorAbility.GetAbilityDataDict` applies this
function to the base damage bounds for the client's displayed values.
`LightningAbility.GetAbilityDataDict` also uses it for arc and storm damage.

## Implementation and limits

`ActorActionManager` previously rolled **233–311 for every rank and level**.
Those numbers happen to match rank 1 at level 4 under the original formula;
that numerical match is an inference, not proof of the old code's origin.
Recovery now obtains the correct base range for the player's selected rank
and experience level. Level 1 rank 1 uses 180–240; level 9 doubles those
values; level 17 rank 5 uses 960–1200. Tests check the original data and exercise
the actual recovery path against a registered target.

The existing inclusive uniform roll within those bounds is retained. The
original server's probability distribution and order of rounding/modifiers
remain unverified. Existing armor/health handling is still provisional. A
matching client tooltip formula does not establish the complete server damage
pipeline, including Body/Mind contributions, critical hits, resistances,
cover, immunity, PvP, and active effect modifiers.

Further original data is preserved for the next action/combat reconstruction:

- All five ranks have windup **500 ms**, recovery **700 ms**, range **60**,
  reuse **1200 ms**; `ActorActionInfo.__init__` names the eight argument slots.
  Timing is now reconstructed in [the action lifecycle](action-lifecycle-client-evidence.md);
  native body-distance range, line of sight and complete target enforcement remain.
- Attribute 6 (Power) base activation costs are **25/50/75/100/150**. No
  consumable scaling type is supplied. `CheckConsumables` applies active
  power-cost and actor ability-cost modifiers after scaling; server consumption
  is now implemented at successful recovery; exact original failure/consumption
  ordering and active modifiers remain under reconstruction.
- Rank 2 adds arc radius 12 and base arc damage 210. Rank 3 uses radius 18,
  arc damage 90, extra damage type 7 (Sonic), and extra damage percent 50.
- Ranks 4–5 use arc radius 24/30, arc damage 210, stun chance 50 and duration 3.
  Rank 5 additionally supplies storm duration 6000 ms, interval 2000 ms, and
  base damage bounds 60–90. These properties and the original `OnAbility` and
  `StormEffect.OnTick` consumers are retained; target selection and effect
  semantics still require implementation and original-behavior comparison.

Lightning recovery currently emits an empty arc list and does not implement
these extra effects. This patch must not be described as complete Lightning
or complete retail combat.

The subsequent [effect schema audit](lightning-effects-client-evidence.md)
adds immutable original rank properties and typed damage, arc and storm
serialization. Recovery now preserves each actual hit's amount and effect
lists; it does not substitute one missile-wide amount for all hit records.
Those packet foundations do not yet select arc victims or apply the extra
Sonic/stun/storm mechanics.
