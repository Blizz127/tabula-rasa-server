# S5 gate: original-client auto-fire targeting (2026-09-24)

This is a control-path audit of the restored **1.16.5.0 compatibility client**,
not proof that its revision is the shutdown build or that the current gate
encounter matches final live. No client or server was run for this audit.

| Original client source | Exact location | Finding |
| --- | --- | --- |
| `data/game.zip:generated/client/keymapping.pyo` | module bytecode 340–347; bindings 2173–2287 | Command 8 is `PerformPrimaryAction`; `mouse.button1` is bound to it in the six stored keyboard/layout combinations. |
| `trpython.zip:client/inputhandlers.pyo` | `PerformPrimaryAction`, source lines 563–570, bytecode 0–49 | Mouse-down calls `gameui.StartPrimaryAction`; release calls `FinishPrimaryAction`. |
| `trpython.zip:client/targeting.pyo` | `OnStartAutoFireWeapon`, source lines 410–417, bytecode 0–12 | Starting auto-fire sets `g_bIsAutoFiring` and refreshes the lock display. |
| Same targeting member | `_UpdateMouseOverTarget`, source lines 1075–1095, bytecode 35–44 and 190–246 | The controller's `FindDirectTarget()` supplies the reticle entity, which becomes `g_mouseOverTarget`. |
| Same targeting member | `_UpdateDirectTarget`, source lines 996–1024, bytecode 42–276 | Auto-fire retains an existing direct target while `CheckLockTarget` accepts it. When auto-fire is already running **and direct target is absent**, the code still allows a new mouse-over entity to become direct target via `SetDirectTarget` at bytecode 205–224. |
| Same targeting member | `SetDirectTarget`, source lines 225–268, bytecode 0–281 | The selected entity must exist, match the target type and be targetable; successful selection sends `SetTargetId` to the server at bytecode 241–259. |

The shortest ordinary input path suggested by this code is to hold primary fire
while sweeping the reticle across the moving Thrax. The client can acquire an
enemy after auto-fire starts and retain it while the target remains valid. Tab
is a lock toggle on the current target, not an initial target-acquisition key;
see [the Tab audit](bootcamp-s5-rebuilt-adaptive-gate-and-target-lock-20260924.json).

This is a **client-code inference**, not a successful gate combat trace. Early
shots may be sent at entity 0 and consume ammunition. `CheckLockTarget` can
still clear a target because of range, occlusion or other controller rules.
An isolated original-client check should record the target panel, `SetTargetId`,
`StartAutoFire`, each `WeaponAttack` target and the copied character state while
using one continuous mouse-down and short camera turns. The earlier
[normal-health boss probe](bootcamp-normal-boss-client-probe-20260924.json) did
use continuous fire and camera turns, but it does not test this S5 gate path.

## Restored source identity

| Archive/member | SHA-256 |
| --- | --- |
| `data/game.zip` | `e78b53640e75954b6b36e88eccfb780fcc79ec7b7ee51edbd213073e26406ca6` |
| `generated/client/keymapping.pyo` | `926623c37236857f814644a7b9470abb46e5f52003602cd1984681955db99ea0` |
| `trpython.zip` | `cd2ffe5a88c5cfd82bcc34cc38cc4ab9aedb479d70fa76c0f4476da7bb7225e6` |
| `client/inputhandlers.pyo` | `586fc0ea303e60dcc7991bf79d6a787b560e186bd3c0b4d86d4cc42993e5c9be` |
| `client/targeting.pyo` | `4483127df8819e89f8cf4b778b7be45079cc74f17727f1f984573879dcbabdb7` |

Archives are under `/home/blizz/private-artifacts/rasa-client-1.16.5.0/extracted/Tabula Rasa 1.16.5.0/`.
