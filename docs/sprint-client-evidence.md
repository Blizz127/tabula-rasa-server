# Sprint: original client reconstruction

Research date: 2026-09-12. The governing target is final live Tabula Rasa,
preserved 1:1. The recovered executable identifies itself as 1.16.5.0;
independent official package authentication remains open as described in
[client artifacts](client-artifacts.md).

## Original values and their consumers

`generated/client/actiondata.pyo`, SHA-256
`9520744380868987a093be20bdbca7de26cd7899e07c50c23f00ae3d764c1ecb`,
contains Sprint's actual properties. `abilitydata` supplies labels/descriptions,
not these numeric properties. All tables below were parsed from raw Python 2.4
literal instructions by the strict decoder; no acquired code was executed.

Action **401**, `AA_RECRUIT_SPRINT`, maps to `abilities.sprint`. Skill **165**
has five trainable ranks. Its empty Logos requirement is separately verified in
[the skill audit](final-client-skill-evidence.md).

| Rank | Movement property 82 | Movement multiplier | CHI activation cost | CHI drain per interval, property 86 | Interval, property 9 | Duration property 8 | Bead property 97 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 120 | 1.20 | 30 | 30 | 2 | 3600 | 100 |
| 2 | 130 | 1.30 | 27 | 27 | 2 | 3600 | 100 |
| 3 | 140 | 1.40 | 25 | 25 | 2 | 3600 | 100 |
| 4 | 150 | 1.50 | 20 | 20 | 2 | 3600 | 100 |
| 5 | 160 | 1.60 | 18 | 18 | 2 | 5000 | 100 |

These appear in `actiondata.abilityData[(401, rank)]` and
`actiondata.actionAttributeCost[(401, rank)]`. The latter contains the single
pair `(5, cost)`; `generated/client/actor.pyo` identifies attribute 5 as **CHI**
and attribute 6 as **POWER**. They are distinct resources.

`actionArguments[(401, rank)]` is `(0, None, 0, None, 0, 0, 0, 1)` for all five
skill ranks. `client/actions/__init__.pyo`, `ActorActionInfo.__init__`, maps its
positions to windup milliseconds, windup animation, recovery milliseconds,
recovery animation, range, reuse milliseconds, preload, and
start-reuse-on-perform. Consequently these skill ranks have zero base windup,
recovery delay, and cooldown. There is no consumable scaling property for them.

The original English descriptions at UI keys 2578, 2604, 2605, 2606, and 2607
explicitly describe a self-targeted toggle with **+20%, +30%, +40%, +50%, +60%**
movement speed. This establishes dividing property 82 by 100, rather than
adding its percentage on top of another full-speed bonus. The current
emulator's previous `2.0 + rank × 0.1` formula is inconsistent with those texts.

`SprintAction.GetAbilityDataDict`, original source line 34, computes
`adrenPerSec = DRAIN_PER_TICK_ADRENALINE / (INTERVAL × 10.0)`; its raw multiply
and divide occur at offsets 47 and 48. The resulting tooltip percentages per
second are **1.5, 1.35, 1.25, 1.0, 0.9**. This confirms that the interval is in
seconds and a CHI unit represents one tenth of a percentage point in that
display. The capacity inference below uses additional independent entries;
general adrenaline acquisition, decay, and starting amount remain separate
evidence gaps.

`BaseActorAbility.CheckConsumables` checks the action's attribute-cost list
against the actor's current resource before performing, applying any supported
cost modifiers. It does not locally spend resources. The original server's
ordering of activation deduction versus the first periodic deduction still
requires an authentic trace or server script; the data identifies the amounts,
but the client alone is not that trace.

The duration values are seconds: `BaseActorAbility.GetAbilityDataDict` uses
`DURATION` directly as seconds and converts `DURATION_MS` by 1000. Yet Sprint's
authored text calls its duration open-ended. The 3600/5000 values must therefore
not become a guessed short burst or an invented visible countdown. Whether
the original server used them as long internal caps is a separate server-side
detail; retain the raw values and this distinction explicitly.

## Live patch reconciliation

The official D13 live notes listed Sprint's displayed adrenaline consumption
as roughly half its actual cost. The subsequent
[official D14 live notes](https://web.archive.org/web/20090218143923id_/http://eu.playtr.com/en/news_article/deployment_14_patchnotes_known_issues_november_2008_live),
published **13 November 2008, 14:41**, explicitly state that the consumption
tooltip now matches the implemented behavior. D14 is therefore stronger than
the obsolete D13 known issue for interpreting the later recovered client.
Do not double the final-client tooltip cost to reproduce an already-fixed
tooltip discrepancy.

The D14 raw page is preserved under
`/home/blizz/backups/rasa-net/research/20260912-death-retail/` as
`20090218143923-deployment_14_patchnotes_known_issues_november_2008_live.html`,
SHA-256 `fd05ed09e00f7c0ebdc911eb70b6e44092ec1f164f77da56c346237dbb74e1c4`.
Its archive capture date is not its live release date.

The [original official abilities page](https://web.archive.org/web/20080828130731id_/http://eu.playtr.com/en/afs_intel/abilities)
also says Sprint consumes adrenaline over time. Local raw capture
`20080828130731-abilities.html` in the same directory
has SHA-256
`e6b125d927f67ad5a7aff8f97f70e69f99b2af4489589518d675de9be524ec4e`.
It provides qualitative corroboration, not final numerical values.

## Adrenaline capacity: strong inference, not a recovered server constant

All eight final-client signature actions have raw
`actionAttributeCost[(abilityId, 1)] = [(5, 1000)]`. Their original English
descriptions independently identify the cost as **100% adrenaline**. The
following offsets are the actual `STORE_SUBSCR` instructions in each module's
top-level code, not decompiler-rendered list positions.

| Signature | Ability ID | Cost store offset in actiondata | Description UI key | Description store offset in English UI lookup |
| --- | --- | --- | --- | --- |
| Explosive Wave | 137 | 113102 | 1064 | 33680 |
| Concussive Wave | 234 | 115592 | 1629 | 53228 |
| Shield Wave | 305 | 118520 | 2426 | 80408 |
| Cloak Wave | 252 | 116552 | 3167 | 106220 |
| Crit Wave | 281 | 117656 | 3123 | 104636 |
| Regeneration Wave | 193 | 114344 | 3177 | 106580 |
| Reanimation Wave | 176 | 113600 | 3183 | 106796 |
| Base Wave | 260 | 116936 | 2425 | 80372 |

The language keys above are `lookup[(descriptionKey, 1)]` in
`generated/client/language/english/uielementlanguage.pyo`, SHA-256
`db0b950f257c62aac9a508eabd99304b0ffdde5b6478326b382a90e5c5ed35ce`.
The actiondata hash is given above. `signature-adrenaline-raw.json` preserves
these raw costs, descriptions and offsets; SHA-256
`96c1634c3db7b58ac2d0c49fea5a808822f8e4e2e62c5645bee156e613f07236`.

The [official abilities page](https://web.archive.org/web/20080828130731id_/http://eu.playtr.com/en/afs_intel/abilities)
also describes all eight signature skills as consuming or requiring a full
adrenaline pool. The archived page predates the recovered client, so it
corroborates the full-pool meaning, not the final values of every other ability
on that page. Together, all eight 1000-unit costs, all eight final-client 100%
descriptions, and Sprint's independent percentage conversion provide strong
evidence for a **normal unmodified CHI capacity of 1000**. This is an inference
from original data and authored descriptions: no named original server cap
constant has been recovered. `AvatarStatusWindow` divides current CHI by the
server-supplied `currentMax`; that display consumer alone does not establish a
fixed maximum.

The implementation consequently separates normal CHI capacity from POWER;
the previous POWER-derived capacity was incompatible with these resource
units. This does **not** establish initial adrenaline, respawn/reset behavior,
combat gains, passive decay, or every possible capacity modifier. Those
mechanics still require separate original evidence.

## Effect attachment, cancellation, and presentation

`generated/client/gameeffectdata.pyo` maps effect **247** to
`actions.abilities.sprint.SprintEffect`, with icon 30052. `SprintEffect` sets
`allowDetach = 1`. Its `OnAttach(effectTarget, beadModifier)`, source line 20,
requires **one scalar argument** and saves it. `Actor.GetAccuracyModifier`
multiplies the returned modifier. Property `EFFECT_FAST_MAXBEAD_MODIFIER = 100`
corresponds to a neutral multiplier 1.0 for all ordinary skill ranks.

`PhysicalEntity.Recv_GameEffectAttached` accepts six fixed arguments
`(typeId, effectId, level, sourceId, announce, tooltipDict)` followed by variadic
effect arguments. It forwards those arguments to `AttachGameEffect`, then
`BaseGameEffect.Attach`, which calls `SprintEffect.OnAttach`. Thus Sprint's
wire tuple has a **seventh scalar double**, not a seventh empty list. The old
packet's empty list would become the bead modifier itself and cannot serve as
the scalar consumed by the client's accuracy calculation.

`BaseGameEffect.SetTooltipDict` reads optional `duration` in **seconds** and
adds it to `gameclient.Time()`. Absence means no displayed expiration timer.
`damageType` and `attrId` are optional language substitutions; no Sprint
damage-type or attribute-tooltip metadata is established by its action table.
They must not be filled with unrelated guessed IDs merely to populate a packet.

`gameui.OnCancelEffect`, source line 1525, sends
`RequestDetachGameEffect(effectId)`, and `Recv_GameEffectDetached(effectId)`
removes the corresponding client effect. Only an effect belonging to that
actor and supported as removable should be accepted for that request.

Although the description calls Sprint a toggle, `SprintAction` does not
override the base class's `isToggle = False`. The generic client precast toggle
branch therefore does not prove how repeated Sprint activation was handled
on the original server. Right-click effect cancellation is directly evidenced.
Duplicate requests must not accumulate speed multipliers or independent Sprint
drains; exact repeated-activation behavior remains a trace-level question.

### Successful attachment must also resolve the action request

`PhysicalEntity.Recv_GameEffectAttached`, original source line 601, calls
`AttachGameEffect` (line 488), which ultimately calls `SprintEffect.OnAttach`.
This path creates the effect and stores its bead modifier; it neither calls
`Actor.Recv_PerformRecovery` nor removes anything from the actor's unresolved
request list. Sending only the effect packet therefore leaves a successful
Sprint request unresolved. This does not prove an endless current-action lock:
Sprint has zero local windup/recovery delays and its local end can clear the
current action independently. The pending request leak is the confirmed gap.

`SprintAction`, line 27, inherits `TargetedAction.DoAction`, whose arguments
after the actor are `hits, misses, missdata, hitdata` (line 361). It sets
`targetType = TARGET_SELF` and `targetGameEffect = SprintEffect`.
`TargetedAction.OnServerResolution`, line 447, calls `DoHits` for a nonempty
hit list, then announces the target effect on those hit entities (raw offsets
82–173). Sprint inherits `BaseActorAbility.DoHits`, which calls `DoAbility`;
the latter, original line 195, only returns `None` (raw offsets 0–3). It does
not read damage or per-hit data.

The connected success response is consequently:

```text
PerformRecovery(401, rank, [actorEntityId], [], [], [])
```

It acknowledges the actual self target and satisfies the inherited four-list
consumer without inventing a damage payload. The exact original server's
unused hitdata representation has not been captured; the chosen empty list
is justified by the inspected consumer, not presented as a recovered wire
capture. `Actor.Recv_PerformRecovery` removes the resolved action object from
the pending list as detailed in [action lifecycle evidence](action-lifecycle-client-evidence.md).

The effect attachment now uses `announce = False`, followed by recovery with
the self hit. This matters because `PhysicalEntity.AnnounceGameEffectAttach`,
line 779, announces an unannounced effect and returns; if every matching effect
is already announced, it queues a delayed announcement instead (raw offsets
22–48 and 57–84). Keeping the previous immediate announcement and also sending
the self-hit recovery would trigger that delayed path unnecessarily. The
original effect receiver retains its fallback announcement when the source
entity is unknown. Raw methods and source hashes are also retained in the
external `action-lifecycle/` manifest and extraction script.

## Connected implementation and remaining verification

The ordinary five-rank Sprint path now checks CHI before queueing and again at
recovery, deducts the rank's activation cost, attaches effect 247 with scalar
bead modifier 1.0, applies the original movement multiplier, and processes the
rank's drain cost at two-second boundaries. Actor movement is restored on
expiration, an unaffordable drain, or an authorized cancellation. Duplicate
activation is rejected without another charge or multiplier. Other actors'
effects and effect types without an implemented removable path cannot be
cancelled through this request.

Effect elapsed time now uses the actual map-loop interval. Previously the
effect manager ran only at a 500 ms update gate but advanced by one main-loop
delta, so a hardcoded duration of 500 did not represent 500 ms of wall time.
Server duration is now measured in milliseconds; the long raw 3600/5000-second
values are used as internal caps, while the tooltip retains the authored
open-ended presentation. Using those values as server caps remains an
explicit inference, as discussed above.

The current drain scheduler charges on activation, then at elapsed times
2, 4, 6 seconds, and so on. It detaches before an unaffordable full tick and
leaves any residual resource untouched; a payment that reaches exactly zero
permits the current interval until the next boundary. The final client's data
supports the amounts and interval, but does not establish this exact server
phase, last partial interval, or repeated-activation rejection. An authentic
trace or original server logic is still needed to verify those details. These
choices must not be presented as proof that Sprint is fully preserved 1:1.
Composition with future movement/cost modifiers, resource gain/decay,
cross-map effect restoration and original death/logout effect rules also
remain separate work.

The packet now writes effect arguments as flat tuple members and omits
unsupported damage/attribute tooltip substitutions. Optional tooltip duration
is encoded in seconds. CHI updates preserve the source actor's full 64-bit
entity ID.

`SprintTests` passed **13/13** after the success-response correction in the
existing .NET 5 container with the network
disabled and source mounted read-only. Coverage includes all five ranks
through request and recovery, pre-queue and recovery-time resource checks,
duplicate rejection, elapsed-time draining and residual-resource cancellation,
owned cancellation, all due expirations in one update, the long internal cap,
the scalar attachment argument, optional tooltip seconds and 64-bit CHI source
IDs. The subsequent success-response regression adds self-hit recovery
serialization and checks all five ranks for attachment before exactly one
recovery, deferred effect announcement, and no success response on failed
attachment. These tests verify the implementation and packet contract; they do not
replace the missing original-server observations. The isolated run log is
`/tmp/rasa-sprint-recovery-tests.log`; no live service or database was changed by this
research/test pass.

## Distinct mech entry

Action data also retains `(401, 6)`: movement 600, duration 2, interval 1,
adrenaline drain 0, bead modifier 150, and reuse time 10000 ms. Its text IDs
5950/5951 describe a mech speed boost, with numbers that disagree with those
raw parameters. This is **not a sixth trainable Recruit Sprint rank**. Preserve
the separate non-skill authorization and the text/data discrepancy for the
mech reconstruction; do not silently extend the five-rank skill table.

## Reproduction artifacts

External directory:
`/home/blizz/backups/rasa-net/research/20260912-client-artifacts/sprint-reconstruction/`.

`source-manifest.json` records copied module hashes, lengths, embedded filenames
and timestamps. `sprint-settings-raw.json` preserves the exact property names,
rank rows, costs, timing tuples and original dictionary store offsets.
`actiondata-sprint-raw.json` and `gameeffectdata-sprint-raw.json` record narrower
raw selections. `sprint-methods.dis` preserves the original argument and
tooltip-calculation bytecode. Extraction uses xdis 6.1.7 and the strict literal
decoder from the River Recon audit; decompiled readable files are supporting
navigation aids, not a substitute for those raw checks.
