# Original client weapon action evidence

Research date: 2026-09-12. Target: the final original live service, under
[the preservation requirement](final-retail-target.md). This audit uses the
recovered client whose executable reports **1.16.5.0**. That version evidence
does not independently authenticate the shutdown patch manifest. Package
acquisition and limits are recorded in [client-artifacts.md](client-artifacts.md).

## Reproducible source evidence

All original files and intermediate output remain outside the repository under:

`/home/blizz/backups/rasa-net/research/20260912-client-artifacts/`

`weapon-actions/extract-evidence.py` reads selected members of
`final-client-selected/Tabula Rasa 1.16.5.0/trpython.zip`, disassembles code objects
with xdis, and passes `skill-selected/actiondata.pyo` through the locally authored
strict literal-table decoder. It never executes or imports original game code.
Reproduce with:

```sh
/tmp/rasa-bytecode-tools/bin/python /home/blizz/backups/rasa-net/research/20260912-client-artifacts/weapon-actions/extract-evidence.py
```

`weapon-actions/source-manifest.json` records every selected member's complete
SHA-256, original code-object line numbers and arguments, compiled timestamp,
and per-method raw disassembly filename. The original timestamps are
2009-02-10 03:55:09–03:55:17 UTC. `weapon-action-tables.json` contains literal
values and `STORE_SUBSCR` bytecode offsets. Offsets below are relative to the
named original function, except generated table stores, which are module offsets.
Method line numbers refer to original compiled source metadata, not emulator code.

| Original member | SHA-256 |
| --- | --- |
| `client/actions/baseactoraction.pyo` | `e2c220c955e0b413797f1c89da4ea69787f49dba7bad4c9ac21b8e5618ea2522` |
| `client/actions/weaponreload.pyo` | `67247f61a1452f954a49f6bb94381b6a396adf923fa788cd67277d955aca4965` |
| `client/actions/weapondraw.pyo` | `06476347d2967870a1297cf8e0bd3e5f2ec93e863f34cebdd890e51f1a279734` |
| `client/actions/weaponstow.pyo` | `edc36beb8151cfbf999e783ea78553b883ae18fb1722e2238d088d33b2804371` |
| `client/actions/weapons/baseweaponattack.pyo` | `c138c223eceec99a6ba8982c9a07071c806bdc445ea2d72440045586dbe54b8b` |
| `client/augmentations/weapon.pyo` | `38357c191fd8214e1b911ed51cb4b03989105f8b6b71eac54f253558a8f02594` |
| `client/augmentations/manifestation.pyo` | `724f68eb70e5a47338db82977c55abfa09f1b187f86a0b9a560c012a8588886d` |
| `client/augmentations/actor.pyo` | `2ecb129d5071ac588e076415857ea4e5b3ebae08a964e2828b885089ac316c4f` |
| `generated/client/actiondata.pyo` | `9520744380868987a093be20bdbca7de26cd7899e07c50c23f00ae3d764c1ecb` |

Control flow below was checked against raw instructions. The decompiler's
rendering of `Manifestation.StartAutoFire` loses nested branches; its text alone
must not be used to reconstruct that function.

## IDs, arguments and timing catalog

Original `actionModules` identifies attack **1**, stow **129**, draw **130** and
reload **134** at stores 37265, 37481, 37499 and 37517 respectively. The factories
in `client/actions/__init__.pyo` obtain draw/stow/reload arguments from the weapon,
and `CreateWeaponReload` captures that weapon's entity ID. Attack factories also
obtain the action ID from the weapon; specialized weapon actions need their own
modules, not an assumption that every attack is action 1.

`ActorActionInfo.__init__` consumes the eight `actionArguments` fields as:

`(windupMs, windupAnimationFamily, recoveryMs, recoveryAnimationFamily, maxRange, reuseMs, preload, startReuseTimerOnPerform)`.

`WeaponActionData.cs` preserves all **135** original state-action rows and all
eight fields: 20 stow, 22 draw, 93 reload. Missing arguments receive no guessed
fallback. Reload arguments 7 and 8 are absent. The raw table stores are
54689–55430 for stow, 55469–56288 for draw and 56327–59915 for reload, advancing
39 bytes per row. The external `weapon-state-action-canonical.csv` sorts by
action and argument and writes those ten integer/`None` fields with one LF per
row. Its SHA-256 is
`610c9076aa1ceb2ccc3f19fb83ab402fb797585182cf61c86cc689a0b894bef4`.
The catalog test compares that fingerprint against every implemented row.

Examples that rule out a fixed 500 ms duration:

| Action, argument | Windup ms | Recovery ms | Reuse ms | Raw table store |
| --- | ---: | ---: | ---: | ---: |
| Stow 129, 2 | 0 | 1132 | 0 | 54728 |
| Draw 130, 3 | 0 | 900 | 0 | 55547 |
| Draw 130, 9 | 0 | 1333 | 2000 | 55781 |
| Draw 130, 17 | 0 | 0 | 0 | 55976 |
| Reload 134, 1 | 1500 | 0 | 0 | 56327 |
| Reload 134, 92 | 0 | **3996** | 0 | 59798 |

Reload windup has a weapon-specific override described below. Do not add its
table windup to that override or discard argument 92's distinct recovery.

## Reload: admission, timing, ammo and cancellation

`WeaponReload` class line 17 sets `scaleWindup=1`, `performCrouched=1`,
`doLocalDoAction=False`, and **`actionInterrupts=1`** at stores 9, 15, 21 and 27.
It inherits `noInterrupt=0` and `moveInterrupts=0`. Its windup does not turn into
a locally predicted successful reload when the duration elapses: it waits for
server resolution.

`CheckAction` line 71 rejects death, swimming, a weapon that is not ready,
blocked actions, a missing captured weapon, and failed condition checks.
The ready check is offsets 48–71. Offsets 168–299 then distinguish a jammed
weapon: jammed reload bypasses ordinary ammunition/full-clip checks. Otherwise
ammo class and current count must exist, inventory ammunition must be available,
and current ammunition must be below clip capacity. The jam branch is evidence
of an unjamming path; its complete mechanics are not reconstructed here.

`__init__` line 31 stores the captured weapon ID at 107–113 and overrides
`windupTimeMs` with `weapon.GetReloadTimeMs()` at 138–150. `Weapon.Recv_WeaponInfo`
line 255 stores its server-supplied `reloadTime` at 36–42;
`Weapon.GetReloadTimeMs` returns that stored value. It is not necessarily the
generated action tuple's windup.

`WeaponReload.GetWindupTimeMs` line 49 computes:

```text
max(0, weaponReloadTimeMs) /
    max(0.01, 1 + sum(applicable skill reload modifiers)
                   + sum(applicable module reload modifiers))
```

The additions occur at offsets 132 and 183, clamp at 199–214 and division at
217–224. Effects are `SKILL_LIMITED_BY_TYPE_RELOAD_MODIFIER_EFFECT` and
`MODULE_MODIFY_RELOAD`; each receives the weapon's skill and tool type. The
`0.01` value is original `shared/gameconstants.pyo` `MIN_HASTE_MODIFIER`,
`LOAD_CONST` 48 / `STORE_NAME` 51. The modifier modules and exact consumers are
included in the external extraction. `BaseActorAction.PlayWindupAnimation`
line 455 uses `scaleWindup` to pass this duration into animation playback
(90–102, 169–187); it does not apply a second action-duration multiplier.

`WeaponReload.DoAction(self, actor, newAmmoCount)` line 148 has one required
payload argument, with no default. It stores `_newAmmo` at 0–6 and calls base
`DoAction` at 9–24. `OnServerResolution` line 155 retrieves the captured weapon
and calls `SetCurrentAmmo(_newAmmo)` at 47–62 if `_newAmmo is not None`.
Therefore the success payload is exactly:

```text
PerformRecovery(134, argumentId, numericNewAmmoCount)
```

Numeric **zero** is a valid update; `None` skips the update. The new
`WeaponReloadRecoveryPacket` retains zero as a number. The old generic
three-argument helper replaced zero with `None` and cannot express this contract.
Tests decode the entire payload for both zero and a nonzero count.

`CancelWindup` line 163 calls base cancellation and clears reload UI/animation
without changing ammunition. `SendServerRequest` line 176 sends
`RequestWeaponReload()` with an empty tuple (offsets 6–12). The original request
does **not** carry an automatic/manual boolean; that distinction is emulator
bookkeeping described below. The current implementation must retain the captured
weapon through completion and conserve ammunition across inventory stacks.
Client data proves the final weapon-count payload and cancellation behavior;
the historical server's exact inventory-stack selection order and database
transaction boundaries remain unobserved.

## Draw/stow and native readiness timing

Both classes set `forceStrike=1` and `performCrouched=1`. `DoStrike` line 21
calls `actor.SetWeaponReady(True)` for draw and `False` for stow (0–12).
Their `SendServerRequest` methods line 25 send empty `RequestWeaponDraw()` and
`RequestWeaponStow()` tuples. Every draw/stow table row has zero windup; duration
belongs to recovery.

`BaseActorAction.LocalDoAction` line 578 starts the recovery animation, obtains
its STRIKE marker (230–266), schedules `LocalEndAction` after the recovery
duration (322–358), and starts reuse for `recoveryMs + reuseMs` (371–411).
`Finish` line 229 forces `OnStrikeMarker` if `forceStrike` is set and no strike
has fired (27–60), then clears the current action (140–146).

Consequently readiness can change at a native animation strike **during**
recovery, while the action remains busy. A forced strike provides a fallback at
finish. The current reconstruction's server readiness transition at recovery
completion is an **inference**, not proof that the original server waited that
long. It prevents firing through the established busy period; exact native
marker time and original `WeaponReady` packet ordering still require animation
data or an authentic trace. An immediate readiness change plus an untracked
fixed 500 ms queue does not reproduce the demonstrated lifecycle.

Cancellation does **not** call `Finish` or force the strike. Raw
`Actor._CancelCurrentAction` calls `_ClearCurrentAction` at 9–15;
`_ClearCurrentAction` calls `currentAction.Cancel` at 30–42 and clears the
reference at 46–52. Base `Cancel` stops its timer, windup, recovery and FX.
`CancelRecovery` clears animation/FX without changing readiness. Therefore a
resolved draw/stow cancellation must not automatically apply the final ready
state merely because a recovery packet was sent. Whether a native strike had
already changed readiness remains the same animation evidence gap.

## Autofire origin, requests and unresolved actions

Raw `Manifestation.StartAutoFire` line 790 chooses draw when holstered, reload
when out of ammunition, otherwise attack. A current action or pending weapon
drawer switch causes a 100 ms retry. It sets **`action.localAction=1`** at
556–562 before `PerformAction`. After that initial action, it sets autofire
active and sends `StartAutoFire(yaw)` followed by the immediate keepalive
(716–784), including when the first action was draw or reload. Charged weapon
actions have a separate charging path and are not reconstructed by a simple
periodic shot loop.

`Actor._PerformCurrentAction` skips `SendServerRequest` for local actions;
manual actions use `Actor.SendServerRequest` and append an unresolved action.
See [action-lifecycle-client-evidence.md](action-lifecycle-client-evidence.md)
for the exact raw generic consumers. This establishes the origin distinction:

| Origin | Specific draw/reload/stow request | Unresolved entry | Unresolved failure cleanup |
| --- | --- | --- | --- |
| Manual action | Sent | Appended | `UserActionFailed` when still unresolved |
| Server-inferred action initiated by autofire | Skipped | Not appended | No `UserActionFailed` |

`Actor.InterruptAction` line 1227 checks current action identity and
`noInterrupt`, sends `RequestActionInterrupt(actionId, argumentId)` at 88–106,
then cancels locally at 110–116. It has **no localAction exemption**: an
autofire-origin reload can still request interruption. Server cancellation must
not pop an unrelated unresolved action for it.

`Actor.PerformAction` line 1108 checks `currentAction.actionInterrupts` and a
different action pair (253–311), then interrupts it (315–343) before starting
the new action. Reload can therefore be interrupted by an eligible different
action, including an ability; this flag is not limited to movement or weapons.
Explicit interruption uses the generic action-pair contract. Interrupting a
completed server resolution must not undo its already applied ammunition.

`_AutoFireKeepAlive` line 925 sends the configured delay immediately and
schedules itself with the same delay (0–58). The original default is
**2500 ms**. `StopAutoFire` line 892 cancels local retries and keepalive and
sends `StopAutoFire()` when autofire was active. The emulator's four-times
keepalive grace and initial 10000 ms grace are retained, **unverified server
policy**; 2500 ms does not prove either grace value.

## Attack/refire evidence and remaining gaps

`BaseWeaponAttack.__init__` line 65 captures the weapon entity ID and distinguishes
an alternate action by comparing action/argument pairs. For a primary action,
offsets 195–226 copy `Weapon.GetReuseOverride()` into `self.baseReuseTimeMs`.
However inherited `BaseActorAction.GetReuseTimeMs(actor)` replaces that value
with `actor.GetActorActionReuseTimeMs(self.info)` at 22–37. The actor function
reads **`actionInfo.reuseTimeMs`** at 0–6 and multiplies applicable
`CALLED_SHOT_ARM` modifiers at 34–50. `BaseWeaponAttack` supplies no overriding
`GetReuseTimeMs` method. This observed distinction must not be silently erased
by assuming the transmitted override/DB `Refire` is the exact client cadence.

Recovery is similarly multiplied by `CALLED_SHOT_ARM` in base
`GetRecoveryTimeMs` (34–50) and clamped to zero (57–69). Base windup is clamped
to zero. For the ordinary, noncharged base action, local reuse begins at recovery
start with `recovery + reuse`, not just `reuse`. Original action 1 argument 1
has `(windup=0, recovery=500, reuse=0)` at store 42404; argument 3 has
`(0,660,250)` at 42443. The external JSON preserves all 146 action-1 rows for
future reconstruction, but the state-action catalog intentionally does not
claim to implement their complete attack lifecycle or specialized action modules.

`BaseWeaponAttack.Windup` line 92 also sets weapon-ready when needed.
`LocalDoAction` line 158 applies heat/recoil before inherited recovery;
`SendServerRequest` line 232 sends
`RequestWeaponAttack(actionId, argumentId, target, isAltAction)` at 38–71.
These are not equivalent to an unrestricted request that merely fires the
currently equipped weapon. Authoritative ammo debit timing, target validation,
attack windup/recovery ownership, heat/jam/condition, charged and alternate
weapon actions, effect modifiers, and exact refire override behavior remain
separate fidelity work. Existing immediate shot/ammo behavior is not established
as original by this audit.

The prior autofire timer also subtracted one main-loop delta only when a
100 ms map timer fired, and iterated a global timer list from each map. Actual
elapsed monotonic deadlines are needed independently of the still-unverified
refire values. A passing test verifies a repair's implementation; it does not
supply missing historical server evidence.

## Validation of this audit's new files

The isolated .NET 5 test run passed all **6** `WeaponActionDataTests` cases:
the complete raw-table fingerprint, distinct draw/stow timing, unusual reload
92, rejection of missing catalog rows, and numeric ammo recovery values 0/30.
Tests ran in an ephemeral container with the host source mounted read-only;
the log is `/tmp/rasa-weapon-action-data-tests.log`. This validates the new
catalog and codec, not the unverified server mechanics listed above. Runtime
integration has its own tests and review.

## Integrated server behavior and remaining data limits

`WeaponActionManager` now owns one captured weapon action per player. It uses
the original draw/stow recovery durations, the weapon's advertised reload
windup, and the original reload recovery. Successful reload resolution starts
that action's recovery at the actual processing time: unlike predicted draw,
reload has `doLocalDoAction=False`. A delayed argument-92 reload therefore still
gets its full 3996 ms recovery after ammunition arrives. Tests cover this late
resolution explicitly. The original action-ID reuse is shared across arguments,
including draw argument 9's 2000 ms extra reuse. Reuse corrections preserve zero
as an authoritative expired timer.

Manual requests and autofire-origin actions retain separate pending-request
cleanup. Matching interruption cancels unfinished reload without spending
ammunition; interruption after resolution retains the committed magazine and
reuse. An eligible ability or different weapon action can interrupt reload.
Draw/stow retain their busy recovery and the native-strike limitation above.
Reload requires an armed weapon and rechecks the captured item reference, map,
living actor, readiness and current ammunition before completion. A stale or
removed equipped item cannot redirect the accepted reload to another weapon.

Reload now accumulates ammunition across stacks while retaining rounds already
loaded. The previous implementation overwrote the cumulative count on each
stack. The new transfer commits the magazine, every reserve-stack decrement,
and removal of emptied personal-inventory links in one database transaction.
Conditional writes validate expected counts and account/character ownership,
inventory type and slot. A late failure rolls back earlier writes; memory and
outgoing inventory changes occur only after a committed transfer. Ascending
personal-inventory stack order is retained from the emulator. This conservation
and persistence repair does not claim to recover historical stack selection or
original database transaction boundaries.

Inventory ownership uses the persisted character ID, distinct from its account
roster slot. Review verified this against login loading and starter inventory
records. Moving an item now updates its in-memory destination slot and its
character ownership before the existing location write. Home storage retains
character ID zero; personal/equipped/weapon-drawer destinations use the active
character ID. Previously a home withdrawal could persist a personal item with
character ID zero, making it absent after relog, and an ordinary move left a
stale source slot on the item. Tests exercise different character/roster IDs,
home withdrawal and personal movement, reload from the new slot, and reopened
persisted counts and locations. Clan ownership conventions are unchanged.

Autofire now registers even when its first action draws or reloads, avoids
duplicate sequences, retries a busy action on the observed 100 ms cadence and
stops removed/disconnected/dead actors. Its global list advances once per
main-loop elapsed interval, independent of occupied map count. It retains the
existing weapon `Refire` values and four-times keepalive grace; complete shot
timing, request validation and the original grace policy remain unverified.

A read-only audit of the current world database found every nonzero draw/stow/
reload argument in the recovered catalogs; zero/absent action arguments receive
no invented timing. However **all 2440 current weapon-template rows advertise
1500 ms reload time**. That uniform emulator dataset is not proof of original
per-weapon reload times. The client demonstrably consumes the advertised value,
but reconstructing those instance/template values and reload modifiers remains
required. Jam clearing, swimming/action-block state, specialized/charged/alternate
attacks, native strike timing and direct shot admission remain separate gaps.
