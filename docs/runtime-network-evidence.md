# Runtime and network framing audit — 12 September 2026

This is an emulator defect correction supporting the final live preservation
target. It does not introduce gameplay mechanics or establish complete original
client/server fidelity.

## Observed runtime evidence

The previous deployed game image was
`sha256:e9a6eb5c6a13f36b53743c2c69130a1d43391a4f51585d981edccabe29f838ab`.
Its preserved log contains three literal `Out of memory.` lines preceding
restart startup sequences around 18:00 UTC:

`/home/blizz/backups/rasa-net/20260912T180817Z-retail-ability/rasa-game-before-ability.log`

The operational record in [retail-accuracy.md](retail-accuracy.md) corrects an
earlier search that missed those lines. There is no allocation stack trace or
retained packet capture establishing the cause of these historical failures.
Accessible bounded Docker-event and kernel-journal queries supplied no further
cause; the kernel query did not have system-wide journal visibility.

The replacement image
`sha256:39c7ffc84dd58fc269771d29fa27e4c41a4c8373fc516619590ab57f0a5f3fd3`
started at 18:08:19 UTC. Read-only checks around 18:13 UTC found restart count
zero, approximately 315 MiB container memory, no configured container memory
limit, and zero `oom` / `oom_kill` events in its current cgroup. At 18:29 UTC its
restart count was still zero. These are replacement-container observations and
do not establish what caused an older process to exit. This audit sent no
packets to production, changed no production configuration, and restarted no
service.

## Isolated reproduction and correction

Experiments ran in disposable SDK containers with `--network none`, using
loopback sockets inside those containers. Production data was not mounted.
Logs and runtime snapshots are outside the repository:

`/home/blizz/backups/rasa-net/research/20260912-runtime-network/`

| Evidence file | Observation |
| --- | --- |
| `framing-before.log` | Seven tests reproduced oversized/overflowing length handling, undersized headers reaching handlers, signed 16-bit length corruption, and ignored failed decryption. |
| `framing-callback-before.log` | A loopback peer sending a four-byte length header declaring 65537 bytes terminated the test host with `Out of memory.`. This reproduces the historical log symptom without establishing historical causation. |
| `framing-after.log` | Nine socket tests passed after framing/resource corrections, including repeated invalid connections and a valid maximum-size frame fragmented after another frame. |
| `protocol-before.log` | Eight of ten protocol tests failed against the deployed image source: malformed frames escaped `Client.Update`, parsing read into a following frame, compressed checksum validation failed, and a tiny compressed frame caused 4,195,432 allocated bytes when claiming a 4 MiB output. Two valid uncompressed framing tests passed. |
| `protocol-after.log` | First combined socket/protocol/logout run passed 31 tests. |
| `focused-final.log` | Follow-up passed 37 tests, including additional continuation, post-close address, string length, and handshake key cases. A subsequent in-lock disconnected-state recheck is included in the parent's full candidate validation. |
| `current-container-state.txt`, `current-memory-events.txt` | Read-only replacement-container snapshots during the audit. |

The relevant source paths and changes are:

- `Rasa.Utils/Networking/LengthedSocket.cs`: interpret wire sizes as unsigned,
  reject lengths outside the existing buffer/header bounds as malformed data,
  honor failed decryption, and close the offending connection. Invalid framing
  no longer throws the process-fatal `OutOfMemoryException`. Compaction preserves
  the full buffer block for a subsequent fragmented frame. Receive resubmission
  occurs after ownership transfer; cleanup returns event args and buffers.
  Closing disposes the socket while retaining its remote IP for disconnect
  observers. Independent review identified and corrected the address-lifetime
  and nested receive-continuation issues before integration.
- `Rasa.Game/Game/Client.cs`: include frame decoding within the existing
  per-client malformed-message handling, serialize incoming queue access, bound
  each decoder to one frame, and release queued bytes on close. Check encrypted
  block length before decryption and reject invalid padding without an escaping
  callback exception.
- `Rasa.Game/Packets/Protocol/ProtocolPacket.cs`: validate structural headers and
  decode compressed bytes before trusting their declared output size. Compare
  actual output length with the declaration, release temporary storage on
  failure, and calculate the compressed payload checksum within the decompressed
  payload. The reproduced valid compressed ping now round-trips successfully.
- `Rasa.Game/Memory/ProtocolBufferReader.cs`: reject truncated counts and
  out-of-frame string/array lengths before allocating their declared size;
  classify existing rejected protocol encodings as malformed input.
- `Rasa.Game/Packets/Login/Client/ClientKeyPacket.cs`: preserve the existing
  64-byte key-size limit while rejecting negative or truncated lengths before
  passing data to big-integer parsing.

The correction adds no arbitrary decompression-size cap. Storage grows with
actual decoded bytes, rather than an unchecked size declaration. Worst-case
compression expansion, total ingress pressure, exhausted connection pools, and
all application-handler error paths are not proven safe by this bounded audit.
The remaining buffered-logout ordering issue is recorded separately in
[death-retail-evidence.md](death-retail-evidence.md).

The focused test command was:

```sh
docker run --rm --network none -v /home/blizz/servers/rasa-net/src:/workspace-src:ro rasa_net:latest bash -lc 'cp -a /workspace-src/. /app/src/ && dotnet test /app/src/Rasa.Test/Rasa.Test.csproj --no-restore --filter "FullyQualifiedName~NetworkFramingTests|FullyQualifiedName~GameProtocolFramingTests|FullyQualifiedName~LogoutTests|FullyQualifiedName~DisconnectTests" --logger "console;verbosity=minimal"'
```

The protocol baseline instead copied only `GameProtocolFramingTests.cs` into
the existing image source, avoiding unrelated work-in-progress gameplay changes.
Automated framing checks establish implementation behavior. They do not replace
an authenticated original-client session capture, sustained runtime observation,
or evidence for still-missing final live systems.
