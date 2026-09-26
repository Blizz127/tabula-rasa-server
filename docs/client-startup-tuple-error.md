# Remaining client tuple error: static narrowing (2026-09-22)

Isolated playthrough 04 retains one client error:
`TypeError: argument list must be a tuple`, at line 7 of its saved
`tabula_rasa.log`. It follows input-state/technique warnings and precedes world
art light-template warnings. That log has no timestamps or traceback for this
error, so it cannot establish an exact packet or map-loading callback. The
server records selection at 14:48:19.273 and `MapLoaded` at 14:48:44.414, but those
times cannot be attached to the client error without a new synchronized trace.

The initial static investigation read the original PE files and retained logs only. It did
not attach to processes, run the client, patch binaries, or change runtime code.
The observations do **not** identify Wine as the cause.

The embedded Python DLL's export table identifies
`PyEval_CallObjectWithKeywords` at RVA `0x25e80`. Its error branch at
RVA `0x25ebb` sets this exact message when a non-null argument object is neither
a tuple nor a tuple subtype. A null pointer is accepted and converted to an
empty tuple. The EXE import at preferred VA `0xb2c064` is actually
**`PyObject_CallObject`**, the two-argument wrapper; DLL RVA `0x18ee0` supplies
null keywords and calls `PyEval_CallObjectWithKeywords`.

There are ten direct `FF 15 <IAT-address>` call sites in this EXE. Eight construct
a tuple or pass null before calling. Two pass opaque stored argument objects,
making them the best static candidates to inspect first:

| EXE preferred call VA | Argument source | Assessment |
| --- | --- | --- |
| `0x4cce7b` | Object at `[ecx+4]`, callable at `[ecx]` | No tuple construction/check in wrapper; callback pair constructor at `0x4cce30` stores both objects unchanged and increments references. |
| `0x4e6ddc` | Object at `[ecx+0xc]`, callable at `[ecx+8]` | No tuple construction/check in wrapper; function pointer occurs at preferred VA `0xbdd03c`. |
| `0x51f9b5` | `PyTuple_New(1)` | Proper tuple construction. |
| `0x51fa95` | `Py_BuildValue("(ssi)")` | Explicit tuple format. |
| `0x5368bd` | `Py_BuildValue("(LsOi)")` | Explicit tuple format. |
| `0x5369aa` | `PyTuple_New(0)` | Empty tuple. |
| `0x536c4d` | `Py_BuildValue("(L)")` | Explicit tuple; calls `client.entitymanager.GetClassId`. |
| `0x8dc9f0` | Null | Legal empty argument list, not Python `None`. |
| `0x8dcf44` | `PyTuple_New(1)` | Proper tuple construction. |
| `0x8f86f1` | Helper `0x8b7480` | Helper first constructs an event dictionary, but **wraps it** in a tuple via `0x8b8970` or `0x8b8a30` before returning. The dictionary creation alone is not a defect. |

These are candidates, not a diagnosis. The direct-IAT scan does not enumerate
calls through other wrappers, DLL-internal call paths, or all indirect pointers.
No dynamic capture currently connects either wrapper to the logged error.

## Follow-up: original startup asset callback is the strongest candidate

The second wrapper is now traceable to `gameclient.PreloadAssets`. The original
`trpython.zip` member `client/main.pyo`, `Initialize`, source line 117, contains
the equivalent of:

```python
gameclient.PreloadAssets("startup.cat", None, None)
```

This is original bytecode, not a reconstructed server call. Its three arguments
appear at bytecode offsets 60, 63, 66, followed by `CALL_FUNCTION(3)` at 69. The
retained disassembly is `ui-flow/client-main.dis:353-360`; the extracted member
was compared byte-for-byte with the archive member.

The native `PreloadAssets` implementation at preferred VA `0x4d86a0` explicitly
accepts Python `None` as the callback (comparison at `0x4d86c6`); other values
must pass `PyCallable_Check`. It then retains the supplied callable and argument
objects unchanged through `0x4e42f0` → `0x4e63e0` → constructor `0x4e6d70`.
The resulting functor's RTTI name is
`.?AV?$utlFuncObjCallable0@XVPythonCallback@@@utlFunctorPrivate@@`.
Its vtable at `0xbdd030` identifies `0x4e6dd0` as the invocation method.

The asset backend at `0x5a3400` has immediate-ready dispatch branches at
`0x5a3489` and `0x5a352a`, and a pending branch that queues the functor. Its
vtable+16 predicate returns false unconditionally at `0x4e6e00`; it does not
filter out a Python `None` callback. The invocation method checks the callable
pointer for **NULL**, then passes the stored argument object unchanged to
`PyObject_CallObject`. Python `None` is a non-null object and is not a tuple.
Thus, **if this startup functor is invoked**, the original pair produces the
exact tuple error before Python reaches callable validation. This is a concrete
original Python/native API mismatch and the strongest static explanation found.

The earlier bare pair constructor/invoker at `0x4cce30`/`0x4cce70` has no direct
`E8`/`E9` references or stored absolute pointer references in the executable
scan. Equivalent pair storage is inlined in the reachable functor chain above.
That reduces the bare wrapper's priority; it does not prove it unreachable
through every computed or indirect route.

Attribution of the saved playthrough 04 error remains **inferred**: no runtime
stack or synchronized timestamp establishes that this functor caused that
particular line. This does not establish original-live occurrence or Wine
causation. No packet change, client patch, or error suppression is justified by
this static result alone.

## Hardware-breakpoint procedure

In a fresh isolated run, resolve the actual loaded base of `python24.dll` and
set a **hardware execution breakpoint** at base + `0x25ebb`. This avoids changing
the on-disk binary or inserting a software breakpoint into its instruction
bytes. Confirm hardware breakpoint support in the chosen Wine/debugger setup;
the bounded attempt below accepted the breakpoint but did not validate a hit.

At that exact instruction, after the function's saved-ESI push:

- `ESI` is the rejected argument object. Read its type pointer at `ESI+4` and
  the type's name pointer at type + `0xc` for this 32-bit CPython layout.
- `[ESP+8]` is the callable; `[ESP+0xc]` is the argument; `[ESP+0x10]` is keywords.
- `[ESP+4]` is the immediate return address. If it is DLL base + `0x18ef1`,
  execution came through `PyObject_CallObject`; then `[ESP+0x14]` is the native
  caller's return address. The two opaque wrappers return to EXE preferred
  VA `0x4cce81` or `0x4e6de2` respectively (adjust for the actual EXE base).

Capture registers, native stack/backtrace, callable/argument type names and
nearby readable object memory, plus the simultaneous client/server log offsets.
Specifically compare both objects against the loaded `_Py_NoneStruct`. A native
return of EXE base + `0xe6de2` with both objects equal to Python `None` would confirm
the identified functor path; retain the stack and asset state to test which
preload request dispatched it.
Avoid invoking Python functions from the debugger merely to print objects.
That capture will distinguish an invalid stored callback argument from another
path before anyone changes packets or suppresses the error.

## Independent runtime attempt: inconclusive

The follow-up used `tuple-diagnostic-isolated/`: a separate copied Wine prefix,
copied writable client files, read-only bound asset directories, its own Xvfb
display and `bwrap --unshare-all` namespace. No authentication server was
started. It did not attach to or alter the active gameplay harness, shared
prefix, server, or production database. The executable, Python DLL and Python
archive retained hashes identical to the original artifacts after the attempt.

WineDbg's GDB proxy loaded Python at `0x1e000000`; GDB disassembled the expected
rejection instruction and reported `Hardware assisted breakpoint 1` at
`0x1e025ebb`. The client then produced no startup log or breakpoint hit during
the bounded wait. Interrupting only the owned debugger created a new debug
thread whose Wine entry address was `0x00006fffffd3961c`; the 32-bit remote view
stopped with `SIGSEGV` at `0xffd3961c`, with `ESI=0`. That stop was **not** the
tuple rejection and supplied no usable callback objects. It is a failed
diagnostic, not evidence against the static candidate or evidence that Wine
caused the original logged error.

The shell cleaned up the private wineserver and display, the namespace exited,
and every recorded owned process ID was absent afterward. Exact commands,
debugger logs, artifact hashes and the negative result are retained in
`tuple-diagnostic-isolated/result.json` and the evidence manifest. Further
runtime confirmation needs a debugger setup that can resume and interrupt this
32-bit client successfully; no client patch or server workaround was made.

## Post-load replay

A read-only census found nine archive members containing `PreloadAssets`; all
were byte-matched to retained original disassemblies. Only `main.Initialize`
directly supplies the `None, None` pair. `BaseActorAction.PreloadAssets` skips a
`None` callback, and its normal actor action callers construct argument tuples.
The remaining matches use the different class/ability preload APIs. No normal
post-load UI action was identified that repeats the startup misuse. Calling
`Initialize` again would repeat manager/UI setup and is not a faithful replay.
A software breakpoint could leave disk artifacts unchanged while replacing an
instruction in scratch process memory, but it does not itself solve the
observed resume/interrupt failure. No new debugger run or attach was attempted.

The separate playthrough 06 native projection warning is documented in
[its focused record](client-projection-warning.md).

Exact file hashes, preferred bases, exports, call-site RVAs and limitations are
in [the focused evidence](evidence/client-startup-tuple-error.json).
