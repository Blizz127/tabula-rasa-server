# Original primary weapon attack evidence

Research date: 2026-09-12. Preservation target: the original final live game.
The source is the recovered client whose executable reports **1.16.5.0**;
authentication against an original shutdown patch manifest remains open.
See [client-artifacts.md](client-artifacts.md) for acquisition and
[weapon-actions-client-evidence.md](weapon-actions-client-evidence.md) for
draw, stow, reload and autofire origin contracts.

## Source and reproduction

External research directory:

`/home/blizz/backups/rasa-net/research/20260912-client-artifacts/weapon-attack/`

`extract-evidence.py` statically reads 22 selected original PYO members from
`../final-client-selected/Tabula Rasa 1.16.5.0/trpython.zip`, walks their code
objects with xdis, and decodes generated `actiondata.pyo` with the locally
authored strict literal-table decoder. No original game code is imported or
executed. Reproduce with:

```sh
/tmp/rasa-bytecode-tools/bin/python /home/blizz/backups/rasa-net/research/20260912-client-artifacts/weapon-attack/extract-evidence.py
```

`source-manifest.json` records hashes, compiled timestamps, code-object source
lines/arguments and per-method raw-disassembly filenames. Original compiled
timestamps are February 10, 2009. `weapon-action-tables.json` preserves original
literal values and module `STORE_SUBSCR` offsets for every weapon-module mapping
and its timing rows. Function offsets below are local bytecode offsets; source
lines refer to original code objects, not emulator files. Decompiled prose was
not used as the sole proof of conditionals or literal arrays.

| Original member | SHA-256 |
| --- | --- |
| `generated/client/actiondata.pyo` | `9520744380868987a093be20bdbca7de26cd7899e07c50c23f00ae3d764c1ecb` |
| `client/actions/weapons/baseweaponattack.pyo` | `c138c223eceec99a6ba8982c9a07071c806bdc445ea2d72440045586dbe54b8b` |
| `client/actions/weapons/meleeattackmovement.pyo` | `627984db62cb040d44b8766ca502eb9d3ed5fcd53416d7980a9780a63e5d2101` |
| `client/actions/weapons/meleeattack.pyo` | `b5de62f8518359dbc92bca19df06078f9f41e793f4129a26e7a7344d1889550c` |
| `client/actions/baseactoraction.pyo` | `e2c220c955e0b413797f1c89da4ea69787f49dba7bad4c9ac21b8e5618ea2522` |
| `client/actions/targetedaction.pyo` | `b9c96045f3d3d3d670550c81c7216644cb38e3b84adcfae35ab496636bcc56d4` |
| `client/actions/weapons/constantfire.pyo` | `97946e3b144230b82a6183fb3cc20da29f177146e48cb82fb32d8c11b2f40aa5` |

Actor, manifestation, weapon and factory hashes are also in the manifest and
the preceding weapon-actions evidence document.

## Dispatch and primary catalog

The raw module mapping is decisive: action **1** loads `weapons.baseweaponattack`
(store 37265), and action **174** loads `weapons.meleeattackmovement`
(store 37877). Both module entries have `isCharged=0`.
`MeleeAttackMovement` directly inherits `BaseWeaponAttack`; its module body
loads that base at 16, builds the class at 31, then exports `ActionClass` at 38.
It does **not** use the separate `meleeattack` module that blocks movement.

The other original dispatches remain separate systems:

| Action | Original weapon module | Charged flag |
| ---: | --- | ---: |
| 140 | `flamethrower` | 1 |
| 141 | `rocketlauncher` | 0 |
| 149 | `machinegun` | 1 |
| 179 | `densitygun` | 1 |
| 213 | `constantfire` | 0 |
| 230 | `torqueshell` | 0 |
| 249 | `polaritygun` | 1 |
| 296 | `abilityturret` | 0 |
| 398 | `netgun` | 0 |
| 399 | `injectiongun` | 0 |
| 411 | `groundtarget` | 0 |
| 418 | `blade` | 0 |

`WeaponAttackData.cs` contains all **198** original action-1/action-174 timing
rows: 146 ranged and 52 movement-melee. The eight fields retain their original
meaning: windup milliseconds/animation family, recovery milliseconds/animation
family, maximum range, reuse milliseconds, preload, start-reuse-on-perform.
Action-1 stores span 42404–48059; action-174 stores span 67676–69665, 39 bytes
per row. The complete `weapon-attack-canonical.csv`, sorted by action/argument
and containing those ten integer/`None` fields with LF endings, has SHA-256:

`d7c012fc12003fb92e5ebdf8e9a4962eac4c495f1a295d58e81d7e61476f882e`

The test compares every implemented row against that external fingerprint.
Absent arguments and specialized actions receive no guessed fallback.

| Action, argument | Windup ms | Recovery ms | Reuse ms | Start reuse on perform | Raw store |
| --- | ---: | ---: | ---: | --- | ---: |
| 1, 1 | 0 | 500 | 0 | Yes | 42404 |
| 1, 3 | 0 | 660 | 250 | Yes | 42443 |
| 1, 66 | 0 | 1500 | 0 | **No** | 42521 |
| 174, 3 | 200 | 133 | 250 | Yes | 67754 |
| 174, 32 | **4** | **5** | **2** | Yes | 68612 |
| 174, 290 | 1466 | 0 | 0 | Yes | 69665 |

The unusually short 174/32 values are raw literal values and are preserved.
The false reuse flag on 1/66 is the sole exception among these 198 rows.
No action-1/action-174 entries occur in `actionAttributeCost`; this does not
remove ammunition or weapon-effect costs defined elsewhere.

## Action flags, timing and interruption

`BaseWeaponAttack` class line 32 sets hostile targeting, `TARGET_NON_FRIENDLY`,
nonactor targets allowed, facing on windup/recovery, `scaleWindup=1`,
`checkRange=0`, `doBlindShots=1`, `performCrouched=1`, and `useAmmoInAction=1`.
It inherits the following raw base flags:

| Flag | Action 1 | Action 174 | Evidence |
| --- | ---: | ---: | --- |
| `actionInterrupts` | 0 | 0 | Base class store 87 |
| `moveInterrupts` | 0 | 0 | Base class store 81 |
| `noInterrupt` | 0 | 0 | Base class store 75 |
| `doLocalDoAction` | 1 | 1 | Base class store 123 |
| `checkRange` | 0 | 1 | BaseWeaponAttack store 51; MeleeAttackMovement store 9 |
| `delayResolution` | 1 | 0 | Base class store 99; MeleeAttackMovement store 21 |
| `useAmmoInAction` | 1 | 0 | BaseWeaponAttack store 69; MeleeAttackMovement store 27 |
| `blockMovement` | 0 | 0 | No override in these two classes |

Explicit matching-pair interruption is allowed; movement does not itself
interrupt these actions. An ordinary new action does not automatically cancel
them through the `actionInterrupts` flag. Reload's contrasting value of 1 is
covered in the preceding evidence document.

Base constructor line 96 copies `info.recoveryDelayMs` to `baseRecoveryTimeMs`
(43–55), `info.windupDelayMs` to `windupTimeMs` (58–70), and reuse (73–100).
Neither selected primary class overrides windup or recovery. Base windup is
clamped to zero. Recovery is multiplied by applicable `CALLED_SHOT_ARM` reuse
modifiers and clamped to zero; base reuse uses the same actor modifier family.

`BaseWeaponAttack.__init__` line 65 does copy the weapon's server-supplied
reuse override into `baseReuseTimeMs` (195–226). However
`BaseActorAction.GetReuseTimeMs(actor)` replaces it with
`actor.GetActorActionReuseTimeMs(self.info)` at 22–37, and that actor method
reads `actionInfo.reuseTimeMs`, not the instance override. This observed
distinction means current database `Refire` values are not proof of primary
client prediction. No corresponding instance windup/recovery override is used
by these classes.

The manual request is sent before local windup. At windup completion,
`LocalDoAction` predicts recovery, schedules its end after `GetRecoveryTimeMs`,
and, when the original flag is true, calls `SetActionReuseTime(actionId,
recoveryMs + reuseMs)` (361–411). Reuse is keyed by **action ID**, shared across
arguments. Thus 174/3 has 200 ms windup, 133 ms recovery and another 250 ms reuse;
1/3 has 660 ms recovery plus 250 ms reuse. For 1/66 the client does not start
this timer at all; `LocalEndAction` also has no replacement reuse call.
It still remains busy for its 1500 ms recovery.

Unlike reload, both primary classes predict `DoAction` locally. Successful
server resolution acknowledges the same action; it must not restart the already
predicted windup or be treated as an unrelated request. Generic cancellation,
unresolved-action FIFO cleanup within each action pair, and server packets are documented in
[action-lifecycle-client-evidence.md](action-lifecycle-client-evidence.md).
Historical server cost/packet ordering and snapshot cadence remain unobserved.

## Request target and alternate-action contract

`BaseWeaponAttack.SendServerRequest` line 232, offsets 38–71, constructs:

```text
RequestWeaponAttack(actionId, actionArgumentId, target, isAltAction)
```

There are exactly four tuple members. Its preceding branch chooses
`_targetLocation` for `TARGET_LOCATION`, otherwise `targetId` (0–35).
For the two primary classes, the target is an entity ID or `None`.
The generic location branch is retained by the decoder as a distinct coordinate
field; this does not claim that primary 1/174 supports location targeting or
that a specialized location action is implemented. Native marshal integer and
long encodings must both be accepted without narrowing a 64-bit entity ID.

The corrected `RequestWeaponAttackPacket` retains nullable `ulong TargetId`,
optional three-coordinate `TargetLocation`, and **the fourth boolean**. The
previous decoder accepted only long IDs and discarded that boolean. It now
rejects wrong tuple arity, invalid target forms, invalid coordinate count and
nonfinite locations. Transport decoding and gameplay eligibility are separate:
decoding an alternate flag or location is not authorization to execute it.

`CreateWeaponAttack` reads the weapon's primary action and argument (0–21);
`CreateAltWeaponAttack` reads the server-advertised `altActionId`/`altActionArg`
(0–36). `BaseWeaponAttack.__init__` compares its pair with the weapon's primary
pair and sets `isAltAction` from inequality (129–147). Therefore a client
request cannot choose an arbitrary valid catalog pair or relabel primary as
alternate. Placeholder alternate template metadata is not final-game evidence.

`Actor.PerformAltWeaponAttack` first draws a holstered weapon, reloads an
ammo-using alternate action when `OutOfAmmo`, or performs that alternate action
(75–155). The server should not invent an alternate action from a missing
primary target. Likewise a manual attack request does not itself substitute
draw/reload; original autofire explicitly chooses those preliminary actions.

## Target eligibility and blind shots

`BaseWeaponAttack.SetTarget` simply records the supplied entity ID (0–6).
Its inherited `TargetedAction.CheckAction` performs the actual eligibility
checks before `SendServerRequest`:

- A dead target becomes `None` because blind shots are allowed (182–251).
- Missing targets are permitted when `doBlindShots` is true (292–326).
- `TARGET_NON_FRIENDLY` accepts hostile, neutral and object categories
  (485–516). A friendly/wrong-category target becomes `None` (555–584), rather
  than receiving damage or necessarily rejecting the entire shot.
- Range checking, when enabled, uses `GetMaxRange` and native body distance
  (607–684), and rejects out-of-range targets. This check is enabled for 174.
- Native line of sight and frontal checks (685–903) can clear the target and
  report the LOS message while retaining a blind shot.

`TargetedAction.GetDistance` calls `actor.body.GetDistance(target.body)` at
34–52. `actions.GetMaxRange` reads the generated row's `maxRange` at 36–42;
its range-effect iterable is empty in this client function. A guessed center
distance or guessed target radius is not equivalent to the original native
body geometry. Client-side checking also does not prove the complete historical
server eligibility implementation.

The current bounded runtime reconstruction can preserve blind-shot behavior
and captured target identity while supporting its existing hostile creature
path. Objects, neutral categories, player/wargame rules, exact body geometry,
LOS/frontal tests, and changes between request and impact remain explicit gaps.
Re-resolving an entity number to a replacement object must not redirect an
already accepted attack.

## Ammunition and specialized behavior

`BaseWeaponAttack.CheckAction` line 118 requires weapon-ready (57–76) and the
captured entity to be a Weapon (81–103). For a primary action it rejects jam
(118–134) and a present ammo class with current ammunition at or below zero
(139–183). `Weapon.OutOfAmmo` line 244 instead compares current ammunition with
server `ammoPerShot` (19–31), which the autofire/alternate selection paths use.
Weapons without an ammo class must not acquire an invented ammunition cost.

`Weapon.UseAmmo(amount)` line 335 subtracts only when an ammo class exists and
current count is sufficient (0–51); default amount is 1. `SetCurrentAmmo` line
328 stores the absolute count and posts the ammo-count event (16–55).
`Recv_WeaponAmmoInfo` supplies an authoritative absolute update.
**Action 174 sets `useAmmoInAction=0`** and is not an ammunition-consuming
substitute for ordinary ranged attacks.

`BaseWeaponAttack.LocalDoAction` line 158 enters visual combat, applies primary
weapon heat and recoil (52–81), then calls inherited local recovery (116–131).
Neither that function nor the selected primary server-resolution path calls
`Weapon.UseAmmo`. In the selected 22 modules, the actual `UseAmmo` caller is
`ConstantFireEffect.OnTick`. Exact primary authoritative ammo debit timing is
therefore still an **ordering inference**, including choosing successful windup
resolution as the debit point. The server must nevertheless retain the captured
weapon and persist a conserved count rather than debit whichever weapon is
equipped later.

Charged flags and constant-fire effect ticks demonstrate why specialized
weapons require separate reconstruction. For example `ConstantFireAttack`
loops windup, and `MachinegunAttack` selects a constant-fire effect; replacing
these with ordinary action-1 cadence would change their behavior. Full heat,
jam, condition, ammo modifiers, charging, secondary effects and alternate
template data remain outside the two primary paths in this pass.

## Verification

An isolated .NET 5 run passed all **16** new catalog/decoder cases, including
the complete 198-row fingerprint, distinct timing stages, the false reuse
flag, unusual literal values, missing/specialized rows, compact/64-bit/None
targets with both alternate flags, coordinate transport and malformed inputs.
Log: `/tmp/rasa-weapon-attack-data-tests.log`. Source was mounted read-only and
copied into an ephemeral container. These tests verify data/codecs; runtime
integration has separate tests and none of these tests supply missing original
server evidence.

## Runtime integration

`WeaponAttackManager` captures the accepted primary action pair, equipped item,
map, eligible target object, magazine cost, and three monotonic timing stages.
Manual requests must match the actual equipped primary pair; unsupported action
classes, alternate requests and location requests cannot silently execute an
ordinary primary shot. The normal primary and movement-melee classes do not
interrupt on movement. Existing actions block admission except an eligible
manual attack may interrupt the original interruptible reload.

Windup resolution rechecks the captured weapon/map/readiness/jam and the target's
object identity. Invalid targets produce the original blind-shot recovery. The
existing hostile-creature damage path remains; native body range, LOS/frontal,
objects and wargames are not claimed as reconstructed. Ammo spending uses one
conditional database update with expected count, account, character and weapon
drawer ownership checks. Memory and the queued absolute ammo update change only
after success. A failed or interrupted unresolved attack spends nothing; a
resolved attack retains its debit and shared action-ID reuse when interrupted.
No ammunition class means no debit, and action 174 keeps its explicit no-debit
rule. Advertised ammo-per-shot values remain part of the unresolved template
stats audit, rather than being replaced with guessed values.

The normal loop now waits for original windup/recovery/reuse instead of the
uniform template refire placeholder. For example 1/203 resolves after 566 ms
and remains busy until 1499 ms from acceptance. Unlike the locally unpredicted
reload, primary timing retains its scheduled deadlines after a delayed update;
it resolves once, without applying catch-up shots. The 1/66 false reuse flag
remains false even though its 1500 ms recovery still blocks another action.
The 174/32 row retains literal 4/5/2 ms stages.

Successful recovery uses the original action's receiver without queuing a
second windup. Damage resolution at windup completion and the ammunition
transaction point are explicit ordering inferences. Flight/impact delay, full
damage modifiers, heat, condition, jamming transitions, primary visual effects,
charged/constant-fire/alternate actions, and original-server packet cadence
remain necessary work. Original action dispatch selects a class by action ID
before passing its argument; no recovered action-1 row selects a charged class.
Specialized charged paths use separate IDs such as 140/149/179/249.

`WeaponAttackLifecycleTests` adds 27 runtime cases with an isolated real SQLite
repository and controlled monotonic time. They exercise request-versus-tracked
targets, blind shots, reused entity IDs, movement, exact timing boundaries,
shared reuse, interrupts, stale ownership/count checks, delayed updates and
end-to-end autofire independent of the template placeholder, including the
first repeat after a duration that is not a multiple of the busy-retry interval. Combined candidate
validation and deployment results are recorded in [retail-accuracy.md](retail-accuracy.md).
