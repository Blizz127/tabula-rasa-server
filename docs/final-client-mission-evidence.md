# Client mission structures and integer encoding — 2026-09-12

This records the evidence behind the mission packet corrections. The source is
the acquired 1.16.5.0 package described in [client artifacts](client-artifacts.md),
with its community-distribution authenticity limits. No acquired game executable
or Python module was run or imported. Static Python 2.4 disassembly was inspected
with xdis 6.1.7, using uncompyle6 3.9.3 for readable reconstruction. Raw bytecode
and constants resolve ambiguities introduced by decompilation.

The member evidence and derived inspection files are retained outside Git under
`/home/blizz/backups/rasa-net/research/20260912-client-artifacts/root-mission-bytecode/`.
`mission-evidence.json` records member hashes, original source filenames, bytecode
version, method first lines, names, and the category constant neighborhood.

| Original package member | SHA-256 |
| --- | --- |
| `trpython.zip`: `client/missionlog.pyo` | `5e43f82a7bab34c15971a0b74dca9585d5b878abcb332e4eeef1540760aeb375` |
| `data/game.zip`: `generated/client/language/english/missioncategorylanguage.pyo` | `24adbbb7778e0150ce72d268574f65c6a24ef1fa1d5a8b6584ae91b15c3c3074` |

## Observed client contract

`MissionLog._SetMissionDataFromServer`, original first line 428, and
`FilterMissionInfo`, first line 32, unpack five mission fields: status,
completable flag, constant data, change time, and objective list. Each objective
has eight fields: objective ID, status, ordinal, remaining time, generic counter
dictionary, item counter dictionary, required flag, and indicator list.

The remaining-time check in `_SetMissionDataFromServer` at bytecode offsets
70–106 explicitly compares with `None`. Numeric zero produces an expiry at the
current client time; `None` keeps an objective untimed. Accordingly,
`MissionObjective.TimeRemaining` is nullable and serializes `None` by default;
explicit zero remains zero. `MissionInfo.ChangeTime` is now emitted instead of
being replaced with a literal zero. This establishes the field's transport,
not when a future mission engine should assign it.

`_UpdateObjectiveCounter`, original first line 487, offsets 169–217, retrieves
generic counters through dictionary `.get` and unpacks each value as
`(count, initial, target)`. Item counters have the distinct `(count, target)`
shape. The server now initializes and writes an actual generic dictionary with
three-value entries, preserving the existing two-value item entries.

`_UpdateIndicators`, original first line 544, offsets 385–423, unpacks each marker
as `(position, radius, indicatorId, show3DEffect)` and passes the position through
to marker creation. Server positions now serialize X/Y/Z rather than X/X/X.
Objectives also start with an empty indicator list, so an objective without a
marker is valid and does not dereference a null list.

The generated category-language constants associate **10000044** with the
Wilderness battlefield category. This independently confirms that the old byte
fields cannot represent real client categories. Both the database model and
packet structure now use `uint`. This proves the category exists, not that every
mission in an older emulator belongs to it. No live mission's category or other
content fields were rewritten. Schema details and validation are in
[mission research](mission-research.md).

## Signed integer defect

The existing compact writer handled only values above 12 with a width marker;
negative values incorrectly fell into the embedded nonnegative form. The reader
also treated the 0x1D signed-byte payload as unsigned. Both are corrected, with
literal wire-vector tests for signed boundaries, negative values followed by
other tuple fields, and unsigned IDs with their high bit set. Unsigned values
above `int.MaxValue` keep a full four-byte payload to avoid truncation when
their bit pattern resembles a small negative integer.

The supporting independent implementation is
[`pym_unpackInt` in the pinned C++ compact decoder](https://github.com/InfiniteRasa/Game-Server/blob/4a9ab5f1fcdf6a18ab6911c384189cc41ddae651/src/UnpackObjects.cpp#L151),
which reads 0x1D/0x1E/0x1F payloads as signed 8/16/32-bit integers. This is emulator
protocol evidence, not an authentic retail capture. Its separate outbound
marshaller uses another marshal form; do not claim byte-for-byte equality with
that outbound writer. The original client mission bytecode establishes logical
tuple shapes but does not itself prove this native compact codec.

## Verification and limits

Packet tests inspect complete `MissionGained` and `MissionStatusInfo` payloads,
including category 10000044, a nonzero change time, zero/positive/absent timers,
both counter shapes, distinct positive and negative marker coordinates, empty
markers, and multiple missions. Those fixtures use synthetic progress to test
the contract; they are not reconstructed retail quest definitions. Integer
tests include literal expected bytes, avoiding reliance on round trips alone.

These changes repair persistence and serialization foundations. They do not
implement quest acceptance, objective events, counter updates, reconnect log
restoration, rewards, NPC eligibility, or authenticated final mission content.
Real-client rendering and interaction remain unverified.
