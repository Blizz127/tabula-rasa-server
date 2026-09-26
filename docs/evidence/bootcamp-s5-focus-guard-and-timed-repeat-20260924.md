# S5 client focus guard and one timed route repeat, 2026-09-24

## Harness change

`tools/client_focus_guard.py` classifies a 1280×1024 compatibility-client screenshot as `gameplay`, `ui_mode`, or `unknown`. It checks the health HUD pixel, the chat-entry strip, and the `UI MODE` header. The isolated `playthrough-harness-courtyard-traced.py` now takes **two gameplay screenshots separated by 0.5 s** before any `hold`; a chat/unknown result refuses the keydown. `check_focus` returns one immediate classification. `recover_focus` sends Escape when UI Mode is visible and verifies two gameplay frames before returning success. A world click is available only by explicitly setting `world_click: true`; it was not needed or used here. The parent runner binds this helper read-only into the bubblewrap client namespace. These are client automation controls outside the Game server and do not change game behavior.

This classifier is specific to the captured 1.16.5.0 client, 1280×1024 Xvfb layout. `unknown` blocks movement. A single immediate `check_focus` can be stale after Return: the first untimed probe observed `gameplay` just after chat opened, followed by `ui_mode` on the next screenshot. The two-frame hold gate handles the observed delay. Do not infer retail UI rules from these probe pixels.

Usage in a running isolated harness command queue:

```json
{"action":"check_focus"}
{"action":"recover_focus"}
{"action":"hold","keys":["w"],"duration_ms":1500}
```

If a hold returns `Movement withheld`, inspect `focus-probe.png`, call `recover_focus`, and retry only after the recovery result says `gameplay`. `recover_focus` with `world_click: true` is an explicit fallback that may act in the game world; it was not verified in these probes. For the existing adaptive command shims, the corresponding operations are `focus`, `recover`, and `hold w 1500`. Run `python3 -m unittest tools/test_client_focus_guard.py -v` from the repository for the offline check.

The offline check passed four sealed examples: normal play `23-wreck-approach.png` → `gameplay`; prior failed-run `26-wreck-near.png`, repeat `11-rail.png`, and `13-strafe-south.png` → `ui_mode`. An unexpected-size image → `unknown`. Two isolated **untimed** client probes then deliberately opened chat. In the second probe, two 100 ms W holds were both refused while chat had focus, both Escape recoveries reached stable gameplay, and a subsequent 100 ms S hold succeeded. One immediate single-frame check after Return remained stale, which motivated the two-frame gate. No mission timer started in these probes.

## Authorized one-session timed repeat

The only subsequent timed attempt used the copied `s5-corpse-agent-09/char-after-use.db` as a source. Before launch only, its copied mission 1995 objective 1 timer row was removed, objective 3 set active, and the known ordinary-reached corpse login point `(-94.73047,84.99609,65.640625)` retained. The source database SHA-256 was `0dfc685b6de058b5bcaf4ea0e734ade57282d8505e9e58a69bb9fe938cee078f`; the prepared checkpoint database was `49682db4733485c2d105a4950e98b63ca8ba8cb35a55dd5b3ec211fad07b3fb6`. The client used the corpse once; copied SQLite then held objective 3 complete, objective 1 active, `timer_remaining_ms=600000`, and a new timer anchor. **No DB position, mission, or timer edit occurred after corpse use.** There was no second timed repeat, live Game/DIT change, or server content change.

| Client evidence | Measured or observed result |
| --- | --- |
| `01-corpse-use.png`, copied DB after use | Corpse use started the unreset countdown. |
| `07-where-value.png` | At `00:05:46`, client `/where` read about `(-125.273,83.547,38.379)` at the high-bank fork. The `.where` text initially remained in chat; one more Return submitted it. Focus recovery restored gameplay before further holds. |
| `10-where.png` | At `00:03:32`, client `/where` read about `(-149.520,84.770,30.563)`. This was about 12 m north in Z of the previously traversed dry-bank waypoint near `(-149.371,84.820,18.848)`. The player faced rock/bridge geometry. |
| `12-correct-bank.png` | A back/left/diagonal correction put the character visually on the bridge deck with `00:01:53` left. No coordinate was taken there, so exact route alignment is unknown. |
| `13-wreck-rush.png` and `15-expired-where.png` | A fixed-key rush continued west/north along the bridge/rock, leaving `00:00:04` at the first screenshot. At expiry `/where` read `(-232.035,99.344,39.828)`, roughly 110 m north in Z of the inferred bomb; the client displayed `Mission Failed: Calling for Reinforcements`. |

The copied database after expiry had mission 1995 state 2; objective 1 status 3; objectives 2 and 3 status 2; `integrity_check=ok`. The game log had **one** `RequestUseObject` (corpse) and no bomb-use request. This attempt demonstrates the guard kept held movement out of chat but did **not** finish the mission. The route failed at a heading-dependent bridge turn, not because the 600-second timer was proven inadequate. The dry-route note remains a waypoint plan; held-key sequences must not be treated as reliable navigation commands. The original final-live route remains unverified.

The isolated runner stopped. The copied character DB was restored from `s5-focus-pre-timed-char-20260924.db` to SHA-256 `7fac011247a76d45ec20b19223c4827e9fc909bf9897c9acf570dad5da105ec9` (Wilderness preflight position, mission 1995 completed objectives, SQLite integrity `ok`). No further timed attempt is pending from this work.

## Sealed sources and hashes

Captures are under `/home/blizz/backups/rasa-net/research/20260922-client-playthrough/`. The timed directory is `s5-focusguard-timed-20260924/`; `screenshot-hashes.json` lists all 17 retained screenshots. `command-ledger-redacted.json` lists all 67 command files and their SHA-256 hashes, with the login arguments redacted in the ledger. Original command files remain in the isolated capture directory.

| Source | SHA-256 |
| --- | --- |
| `tools/client_focus_guard.py` | `850ad15f540a744b350c275ce6f5563b7ff94b2ee7e548e7d2e6c0feb0e977ce` |
| `tools/test_client_focus_guard.py` | `fc9c238fc73dbcb8fd6ab752bfd0ed023ce7e7ab22ab08c9a23d59345300366d` |
| `playthrough-harness-courtyard-traced.py` | `cc53743b03aba2fe33ea49ff64b50243f127e2dab2677735791ac89c41b4a0d3` |
| `run-s5-corpse-agent-20260924.py` | `1c5ec8f67864206da45cf8af6d0ffa92eb7617354d5d268b36f0cf0618b8b070` |
| Timed `command-ledger-redacted.json` | `78dd1846a8f62c95e919dc05db91b1c94239e3735dec89a239a58959d1774782` |
| Timed `screenshot-hashes.json` | `d14c3d11c225dcbb8eb611503454d7065763e2e8b3d5f7d5218a5e6db2a469ca` |
| Timed `game.log` | `2c6f25c722de7ba4946e27e243f66b990ee9daee5eda58426e70d5588282064a` |
| Timed `objective-after-expiry.json` | `66b47e58cf41bff26a741960045dd50266e0f9e29909b63108d7c7b754c0ac16` |
| Timed `char-after-expiry.db` | `91918094fbb1c31f3dd654ebf1986de07a90f79d72cd33ce816c79f6010d3d4e` |
| Timed `15-expired-where.png` | `39da03b0689d6328d4194ca20c595d18c879edca938bcc5ff5ca7227dbc6e7e1` |
