# Original-client zero-size projection warning — 2026-09-22

During the isolated emulator playthrough06, the original client log recorded
`View dimension(s) were 0 when trying to build projection matrix`, beginning
at line26 and repeated through line52 in the inspected prefix. The startup
tuple error was separately present at line7. These are observations of the
emulator harness, not evidence of behavior on the original live service.

Read-only disassembly locates the warning in the original `tabula_rasa.exe`:

| Original executable location | Meaning |
| --- | --- |
| Preferred base `0x400000` | All addresses below use this base. |
| String `0xbe8350` | Exact warning text. |
| Sole string reference `0x5e79e7` | Pushes the warning and original source line246 (`0xf6`). |
| Source-path string `0xbe828c` | `.\graphics\gfxView.cpp`. |
| Function `0x5e7360` | Builds/refreshes the view projection. |
| Checks `0x5e7435`–`0x5e7453` | Branch to the warning when DWORD dimension at view+`0x21c` or view+`0x220` is zero. |

Nine direct call sites reach that function, including generic view and render
paths; their addresses are recorded in the [evidence manifest](evidence/client-projection-warning.json).
This establishes a native graphics-view dimension guard, separate from the
Python tuple rejection. It does **not** identify which runtime view was zero:
the main scene, an inventory/loot preview, or another render target. The
triggering lifecycle and responsibility remain unknown. Opening the crate
preceded the reported observation, but that alone does not establish its cause.

No client patch, server-protocol workaround, or assertion that Wine caused the
warning is justified. Identifying the affected view would require its runtime
object and call stack when the guard fires. This investigation did not attach
to any process, start a debugger, modify the active harness, or run a build.

The log at
`/home/blizz/backups/rasa-net/research/20260922-client-playthrough/run-client/tabula_rasa.log`
was still mutable; its observed line numbers are recorded without a purported
stable log hash. The manifest hashes only the static original executable. Its
1.16.5.0 compatibility label does not independently establish the exact client
revision running at original service shutdown.
