# Action lifecycle: original client evidence

Research date: 2026-09-12. Target: final live Tabula Rasa preserved 1:1. This
report follows [ability requests](ability-use-client-evidence.md) and
[Lightning damage](lightning-client-evidence.md). The recovered executable
reports 1.16.5.0; independent official package authentication remains open as
described in [client artifacts](client-artifacts.md).

## Sources and method

Original Python 2.4 bytecode was inspected with xdis 6.1.7. No acquired game
module was imported or executed. Literal action tables were parsed with the
strict decoder from the River Recon audit. Uncompyle6 output was used only for
navigation: its rendering of `Recv_PerformWindup` and `Recv_PerformRecovery`
loses a nested conditional, so those branches were reconstructed directly
from jump instructions.

Evidence directory:
`/home/blizz/backups/rasa-net/research/20260912-client-artifacts/action-lifecycle/`.
`extract-static-evidence.py` is a locally authored static extraction script;
`method-manifest.json` records original paths, hashes, timestamps, signatures
and first source lines. Separate `*.raw-dis.txt` files preserve each inspected
method. `lightning-literal-timing-cost-evidence.json` records exact table values
and dictionary store offsets. Source timestamps are 10 February 2009.

| Original member | SHA-256 |
| --- | --- |
| `generated/client/actiondata.pyo` | `9520744380868987a093be20bdbca7de26cd7899e07c50c23f00ae3d764c1ecb` |
| `client/actions/__init__.pyo` | `f3cc8468a9991e641eaf390564ef1ae15e5a877095f19091cb65ba2b4e2413b4` |
| `client/actions/baseactoraction.pyo` | `e2c220c955e0b413797f1c89da4ea69787f49dba7bad4c9ac21b8e5618ea2522` |
| `client/actions/abilities/baseactorability.pyo` | `b4d28fe9b85005cb3b13911b790e9befdf0bb78c3250344626591d0a9ae614ff` |
| `client/actions/targetedaction.pyo` | `b9c96045f3d3d3d670550c81c7216644cb38e3b84adcfae35ab496636bcc56d4` |
| `client/augmentations/actor.pyo` | `2ecb129d5071ac588e076415857ea4e5b3ebae08a964e2828b885089ac316c4f` |
| `client/actions/abilities/lightning.pyo` | `140f22ff022f00a743affdaf0b05e78152778a4f867c5199c5975860cf389a8e` |

## Lightning's base timing

`actionModules[194]`, store offset 38237, identifies `AA_RECRUIT_LIGHTNING`,
module `abilities.lightning`, with charged flag 0. All five skill ranks have:

```text
actionArguments[(194, rank)] = (500, 1063, 700, 1064, 60, 1200, 0, 1)
```

The rank 1–5 dictionary stores are at offsets
72395, 72434, 72473, 72512 and 72551. `ActorActionInfo.__init__`, original
source line 28, names the slots through raw subscript/store instructions:

| Index | Meaning | Lightning value |
| --- | --- | --- |
| 0 | Windup milliseconds | 500 |
| 1 | Windup animation family | 1063 |
| 2 | Recovery milliseconds | 700 |
| 3 | Recovery animation family | 1064 |
| 4 | Maximum range | 60 |
| 5 | Reuse milliseconds | 1200 |
| 6 | Preload flag | 0 |
| 7 | Start reuse timer on perform | 1 |

These are operational inputs to the original action scheduler, not merely
tooltip numbers. They do not establish every animation strike marker,
projectile travel delay or original server processing boundary.

## Client request, windup and recovery order

`Actor.PerformAction`, first source line 1108, first checks action blocking and
the requested **action ID's** remaining reuse time. It then checks the current
action. A different requested action can interrupt the existing action only
when that existing action has `actionInterrupts` set; otherwise an occupied
current action causes failure. See raw offsets 131–170 for reuse and 253–393
for current-action handling. There is no ordinary pending-input queue in this
method. Its charged-action exception is not Lightning's path.

`Actor._PerformCurrentAction`, first line 1160, runs `PreWindup` and
`CanDoAction`, handles crouch requirements, then **sends the request before
starting local windup**: offsets 184–206 call `SendServerRequest`, and 211–272
preload/start `LocalWindup`. Lightning's preload flag is zero. The older
`LocalWindup` docstring says it is called before sending, but the actual caller
instructions establish the opposite order.

`Actor.SendServerRequest`, first line 3363, first delegates to the action's
request sender, then appends that action object to
`__unresolvedActions[(actionId, actionArgId)]` at offsets 26–56. The ability
request tuple and optional target/item/yaw fields are documented separately in
[the request audit](ability-use-client-evidence.md).

`BaseActorAction.LocalWindup`, first line 370, calls `Windup`. For an uncharged
local player action, `Windup`, first line 378, schedules `LocalDoAction` after
`GetWindupTimeMs(actor)`: offsets 283–337. Lightning therefore predicts the
start of recovery 500 ms after local windup, without waiting for the server
resolution packet.

`LocalDoAction`, first line 578, transitions to recovery, plays the recovery
animation and effects, and schedules `LocalEndAction` for the recovery delay
at offsets 322–358. `LocalEndAction`, first line 684, transitions to idle and
calls `Finish`; `Finish`, first line 229, calls `actor._ClearCurrentAction()`
at offsets 140–149 after handling pending strike/resolution work. Receiving a
resolution packet while the scheduled local end is pending does not itself
skip that delay: `FinishSoon`, first line 302, offsets 0–19, leaves the
scheduled method in place.

Thus the ordinary unmodified Lightning path occupies the current-action slot
for approximately **500 + 700 = 1200 ms**. Another ordinary ability is blocked
during both portions. This is distinct from Lightning's own reuse deadline.
Network delays, an already-finished prediction and strike handling can affect
the visible reconciliation; this is not proof of an original server queue or
permission to resolve damage repeatedly.

## Reuse starts with recovery and includes recovery time

`BaseActorAction.LocalDoAction`, source lines 635–639, raw offsets 361–414:

```text
if startReuseTimerOnPerform:
    delayMs = GetRecoveryTimeMs(actor) + GetReuseTimeMs(actor)
    actor.SetActionReuseTime(actionId, delayMs)
```

The addition occurs at offset 392. For unmodified Lightning this sets **1900
ms from the start of recovery**, normally **2400 ms from windup start**.
Treating 1200 ms as a cooldown that starts at request receipt omits both the
windup and recovery portions of this original-client rule.

`Actor.SetActionReuseTime`, first line 3213, converts milliseconds to seconds
and stores `(expireTime, durationMs)` keyed by **action ID**, at raw offsets
0–44. Consequently all selected Lightning ranks share one cooldown.
`HasActionReuseExpired`, first line 3179, expires it when current time is at
least the stored deadline. `GetActionReuseTime`, first line 3192, reports
remaining milliseconds for the UI; its integer display rounding is distinct
from the expiration comparison.

The client supports two different incoming timer contracts:

- `Recv_ActionReuseTimes(reuseTimeList)`, first line 3162, iterates
  `(actionId, remainingMilliseconds)` pairs and directly calls
  `SetActionReuseTime`. Its original docstring describes persisted times sent
  during player initialization. The wire arguments are one list inside the
  outer call tuple.
- `Recv_ActionReuseTimerRestarted(actionId, actionArgId)`, first line 3171,
  calls `GetActorActionReuseTimeMs(actionInfo)` and sets that value. This adds
  **no recovery time**, so sending it at recovery start would set only 1200
  ms for unmodified Lightning.

Using an `ActionReuseTimes` entry to correct the live player's 1900 ms deadline
is supported by the consumer's logic. The recovered client alone does not
prove that the original server sent this initialization-shaped message after
every successful cast; original broadcast cadence remains an evidence gap.
Persistence across logout, reconnect or map changes is a separate requirement,
and the existence of the initialization handler does not implement it.

`BaseActorAction.GetRecoveryTimeMs`, first line 193, multiplies recovery by
each `CALLED_SHOT_ARM` effect's `GetReuseModifier(self)` and clamps the result
to at least zero. `Actor.GetActorActionReuseTimeMs`, first line 3235, similarly
multiplies reuse time, passing the actor to that effect method. These actual
call arguments are preserved in raw disassembly; their difference must not be
silently normalized. Base timing is insufficient once these effects exist.

## Movement, cancellation and resolution packets

`LightningAbility`, first line 37, explicitly sets `stopMovement = 0` and
`moveInterrupts = 0`. It inherits `actionInterrupts = 0` and `noInterrupt = 0`
from `BaseActorAction`. Therefore movement does not stop or interrupt this
ability, and another ordinary ability does not automatically replace it.
An explicit interrupt request is allowed by the client unless a more specific
action overrides `noInterrupt`.

`Actor.InterruptAction`, first line 1227, checks the exact current action ID
and argument ID and `noInterrupt`. Offsets 88–119 send
`RequestActionInterrupt(actionId, actionArgId)` and immediately cancel the
current local action. `AttemptInterruptAction`, first line 1210, calls this
path for movement only when `moveInterrupts` is true. These are distinct
triggers; Lightning's movement override must be honored.

| Incoming client consumer | Arguments | Proven local behavior |
| --- | --- | --- |
| `Recv_ActionInterrupt`, source line 3261 | `sourceId, actionId, actionArgId` | Finds a matching current or last action, honors `noInterrupt`, announces interruption and cancels a matching current action. |
| `Recv_ActionFailed`, line 1939 | `actionId, actionArgId` | Cancels a matching current action. |
| `Recv_UserActionFailed`, line 1902 | `actionId, actionArgId, msgId` | Optionally displays the message, stops relevant local autofire/UI, and removes the first unresolved action for that pair. It does not call current-action cancellation. |
| `Recv_PerformWindup`, line 1249 | `actionId, actionArgId, *args` | Starts or reconciles windup and forwards action-specific arguments. |
| `Recv_PerformRecovery`, line 1319 | `actionId, actionArgId, *args` | Calls the action's resolution method and removes that resolved object from the unresolved list. |

The two kinds of failure perform different work. `ActionInterrupt` and
`ActionFailed` do **not** remove an unresolved request. For a cast interrupted
before resolution, cancelling the current action and cleaning up its pending
request both matter. `UserActionFailed` performs the latter with its
`actionList.pop(0)` at offsets 242–254. After a successful recovery has removed
the request, another failure notification must not pop a later request for
the same pair. The original packet ordering and localization for each failure
reason are still not established by a capture.

The raw windup and recovery consumers defer an incoming **different** action
pair by 100 ms if a nonlocal action still occupies the current slot; they
cancel a conflicting local-only action instead. Windup offsets 21–132 and
recovery offsets 55–166 show this nested branch. A matching pair proceeds;
an already-resolved matching recovery is separately deferred. These handlers
reconcile incoming packets and must not be mistaken for a server action queue.
`ActionLockGuard`, first line 3626, simply returns zero in this build.

For Lightning, `TargetedAction.DoAction`, first line 361, accepts
`(actor, hits, misses, missdata, hitdata)` after the two fixed recovery fields.
It saves those four collections before calling `BaseActorAction.DoAction`.
The base method marks server resolution, starts local recovery only if needed,
and coordinates strike/resolution presentation. Arc and storm payloads require
their separate combat audit; a lifecycle correction does not establish them.

## Power costs and modifier order

The five raw `actionAttributeCost[(194, rank)]` entries are:

| Rank | Attribute | Base cost | Original store offset |
| --- | --- | --- | --- |
| 1 | 6, Power | 25 | 114368 |
| 2 | 6, Power | 50 | 114392 |
| 3 | 6, Power | 75 | 114416 |
| 4 | 6, Power | 100 | 114440 |
| 5 | 6, Power | 150 | 114464 |

No trainable rank supplies `CONSUMABLE_SCALE_TYPE`. The data therefore has
no level scaling of these costs, unlike Lightning's separate damage scaling.
`BaseActorAbility.CheckConsumables`, first line 203, applies this order:

```text
cost = ScaleActorAmount(actor, baseCost, consumableScaleType)
moduleMultiplier = 1.0 + sum(effect.GetPowerCostMod(actionId, rank))
cost = int(cost * moduleMultiplier * ((100 + actor.GetAbilityCostModifier(attrId)) / 100.0))
reject if actor.GetAttribute(attrId).current < cost
```

The module effects come from `MODULE_POWER_COST_MOD`. Raw offsets 94–109
perform scaling, 112–174 sum their modifiers, 175–182 multiply the cost, and
185–215 apply the actor's percentage modifier and truncate with `int`.
The resource comparison is at offsets 218–254. The original actor modifier
lookup defaults to zero (`GetAbilityCostModifier`, first line 923), and its
change method adds deltas (`ChangeAbilityCostModifer`, line 1024).

This function checks affordability; it does not deduct Power. The original
client's `BaseActorAbility.OnServerResolution`, first line 173, announces an
attribute change for the ability's cost attributes but likewise does not
establish the server's deduction phase. `Recv_UpdatePower`, line 981,
receives `(current, currentMax, refreshAmount, whoId)`.

Charging at the first successful server recovery is an explicit reconstruction
choice consistent with these consumers, **not a recovered original-server
transaction boundary**. Revalidation before the charge prevents a queued
request from spending an unavailable resource. Refunds, interruption at the
resolution boundary, failures after spending, regeneration and equipment/effect
modifiers need original-server evidence or authentic gameplay traces.

## Generic pending-action interruption

A follow-up raw check found no separate interruption contract in
`client/actions/useobject.pyo` (SHA-256
`47e1df251f02553426b25dafbca4faf1f154bf0decffe1af7d67d8a6a970a898`).
`UseObject`, original source line 16, sets `moveInterrupts = 1` and inherits
the generic cancellation implementation. `weaponreload.pyo` (SHA-256
`67247f61a1452f954a49f6bb94381b6a396adf923fa788cd67277d955aca4965`)
sets `actionInterrupts = 1` and disables local predicted recovery.
Its `CancelWindup`, line 163, calls the base cancellation and restores reload
presentation; it does not request successful completion. Weapon draw/stow
likewise have no alternative cancel contract. Original methods, timestamps and
hashes are in `legacy-cancel-source-manifest.json` and the matching raw files.

The emulator's old generic queue handled an interrupt by advancing the pending
action directly to recovery. That could complete a reload or object operation
early. Cancelled entries must be removed without running success recovery,
while preserving object-specific pending-user cleanup. In this codebase,
Logos/control-point recovery previously removed interrupted users from
`TriggeredByPlayers`; moving cancellation out of those methods must retain
that cleanup explicitly. This is an emulator bookkeeping requirement,
independently of still-missing original object/weapon mechanics.

## Implementation and validation scope

This pass adds `ActionFailedPacket`, `ActionInterruptPacket` and
`ActionReuseTimesPacket` with the argument shapes above. `UpdatePowerPacket`
preserves the source entity's full 64-bit ID and snapshots attribute values
when queued, so later resource changes do not rewrite earlier notifications.
The new packet tests check exact arity, order, nested timer-list shape, empty
timer lists, full entity IDs and independently queued Power snapshots.

The parent lifecycle integration uses a monotonic timeline for the original
500 ms windup, 700 ms recovery and shared action-ID reuse deadline. Busy
Lightning blocks other ordinary skill requests; a matching interrupt removes
pending resolution instead of accelerating it into damage. Charging at
successful recovery, cancellation before charging, retaining an already-started
reuse timer after later interruption, and live timer correction must retain
the provenance qualifications above. Generic pending-action interruption also
removes cancelled entries without success recovery and retains object-user
cleanup. Other non-Lightning action timing and full combat eligibility/effects
remain separate work. Successful Sprint additionally sends the inherited
self-hit recovery described in [Sprint evidence](sprint-client-evidence.md).

Admission failure sends both current-action cancellation and unresolved-request
cleanup. A duplicate matching an already accepted current pair is ignored,
because the client removes the **first** unresolved request for that pair;
sending its failure would discard the earlier accepted request. Rejected or
interrupted unresolved Lightning also receives the actual remaining server
reuse time, including zero. `SetActionReuseTime`, original lines 3219–3220,
accepts zero, and `HasActionReuseExpired` removes expired entries. This corrects
a late response after the client has already predicted recovery/reuse without
clearing a genuine surviving cooldown. Exact original correction cadence is
still unobserved.

The game loop now measures elapsed time with `Environment.TickCount64`, the
same monotonic source already used for logout, instead of calendar time.
Clock corrections therefore do not add or subtract cast/effect duration.
The 100 ms map-loop cadence is unchanged. Weapon-fire attempts and queued
legacy action completion wait while Lightning is current. The original
weapon reload's ability to be interrupted by a different new action still
needs full integration with that legacy queue.

Admission currently handles registered hostile creatures on the same map and
revalidates the exact target object at recovery. It rejects friendly AFS
creatures and player targets, which are currently advertised as friendly;
wargames and damageable objects need their own reconstructed state/handling.
The recovered range is 60 in native `body.GetDistance` units. Native collision
geometry, line of sight and frontal checks have not been replaced with guessed
center-point geometry, and remain server validation gaps.

Focused packet tests run in an ephemeral existing .NET 5 container, with the
network disabled and workspace source mounted read-only. The log is
`/tmp/rasa-action-lifecycle-packet-tests.log`; combined runtime validation is
recorded separately in [the preservation work log](retail-accuracy.md). No
production service or database was changed by this research pass. Passing
tests establish implementation behavior, not completion of 1:1 combat.
