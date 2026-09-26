# S5 gate mouse-look control boundary (2026-09-24)

Two isolated original-client gate probes restored the same copied character and
world state, started at `(93.2,115.0,137.5)`, issued `H`, `R`, two
`xdotool mousemove_relative -- -200 0` calls about 100 ms apart, then held `W`
for 3000 ms. The first reached `/loc` `(73.9,109.0,139.2)`; the second
reached `(79.7,109.0,144.6)`. The two start screenshots have the same camera
landmarks, while the later screenshots show different gate bearings. Both
movement holds passed the gameplay focus guard. The exact moment or cause of
the divergent heading was not logged. See the [W3 probe record](bootcamp-s5-w3-gate-range-probes-20260924.json)
and its two sealed runner manifests, `s5-gate-range-w3-1` and
`s5-gate-range-w3-panel1`, under
`/home/blizz/private-artifacts/rasa-net-s5-prefix-20260924/rebuilt-harness/`.

The restored 1.16.5.0 compatibility client establishes the following input
path:

| Original client member | Exact location | Finding |
| --- | --- | --- |
| `trpython.zip:client/gameui.pyo` | source lines 197–216, bytecode 646–695 and 850–886 | The root captures mouse input and the WorldDisplay registers `HandleCapturedMouseMove` for movement. |
| Same member | `SyncMouseLook` lines 858–865, `StartMouseLook` 868–877, `StopMouseLook` 880–886 | UI state selects native user-controller mouse-look or cursor mode. |
| `trpython.zip:client/inputhandlers.pyo` | `HandleCapturedMouseMove` lines 135–142, bytecode 0–59 | Each captured event's `LocX` and `LocY` go to native `controller.MouseLookHandler`. The Python method has no pixel-to-yaw scaling or acceleration calculation. |
| `data/game.zip:generated/client/keymapping.pyo` | command table bytecode 825–842; default bindings 8778–9028 | `TurnLeft` and `TurnRight` exist as commands 62/63, but their six default binding entries are empty. |
| `trpython.zip:client/inputhandlers.pyo` | `TurnLeft` lines 419–423 and `TurnRight` 425–429 | Those commands call native `controller.TurnLeft/TurnRight` with key-down state if the player binds them. Their turn rate is not given here. |

The code does **not** establish whether native cursor recentering, mouse
acceleration, frame timing or Wine/XTest event handling made the two turns
different. Neither run recorded cursor position, raw mouse events or camera
yaw. A successful `xdotool` exit only shows that XTest accepted the command.
The evidence therefore does not support a fixed `-400 pixels = yaw` mapping.

For an isolated tester, the original-client-valid control is a closed loop:
confirm gameplay mode, make one short mouse turn, allow a rendered frame, and
measure the actual camera bearing from landmarks/radar or a short ordinary
`W` probe with `/loc`; correct before a long hold. Near an active hostile,
acquire it with the [ordinary auto-fire sweep](bootcamp-s5-original-client-autofire-targeting-20260924.md)
and use the target panel/`SetTargetId` trace as feedback. This is a tester
control recommendation, not a retail gameplay rule, and it has not been
validated as a guaranteed pixel-accurate yaw method.

Restored archive SHA-256: `trpython.zip`
`cd2ffe5a88c5cfd82bcc34cc38cc4ab9aedb479d70fa76c0f4476da7bb7225e6`;
`data/game.zip`
`e78b53640e75954b6b36e88eccfb780fcc79ec7b7ee51edbd213073e26406ca6`.
Member SHA-256: `gameui.pyo`
`451bb70f10faced1261c74ad6452057711f230dc0798faf924b25911257d84d0`,
`inputhandlers.pyo`
`586fc0ea303e60dcc7991bf79d6a787b560e186bd3c0b4d86d4cc42993e5c9be`,
`keymapping.pyo`
`926623c37236857f814644a7b9470abb46e5f52003602cd1984681955db99ea0`.
