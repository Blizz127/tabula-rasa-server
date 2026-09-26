# Creation and clone request validation

Research and implementation: 2026-09-22. This is a protocol/robustness correction,
not a reconstruction of the original server's malformed-request policy.

The recovered compatibility client sends `RequestCloneCharacterToSlot` with
exactly seven arguments: source pod, destination pod, first name, gender,
height, appearance dictionary and race. `client/inputstate/charactercreation.pyo`,
`OnCreateCharacter`, original line 92, offsets 70–97, establishes the order and
tuple length. Its SHA-256 is
`60a2b2cf3d3ab57dd83ac743954cf1a20e4c07916284c6a914bcf8f0f21cbccc`.
The same method's offsets 19–43 and 111–138 establish the first-family and
existing-family creation messages. Provenance limits are recorded in
[client artifacts](client-artifacts.md); compatibility with 1.16.5.0 is not
independent proof of the shutdown build revision.

`shared/gameconstants.pyo`, original lines 190–192, offsets 688–703, declares
default height 1.0 and bounds 0.9–1.06. SHA-256:
`e0fc260a81d20fe01023ad79e5460773d46aba3d7feeba7291373cba0026ed40`.
`client/ui/charactercreationwindow.pyo::Init`, line 153, offsets 810–848, uses
these bounds for the height slider. These are original client values, with
high confidence; no new height limit is introduced.

The server previously ignored the clone tuple length and narrowed source pod,
destination pod and gender with unchecked byte casts. For example, pod 257
became pod 1. Clone parsing now rejects these overflows before mutation and
checks the seven-field tuple. Clone validation also handles a null first name
and undefined gender/race values consistently with ordinary creation.

Both creation paths compared height only with `<` and `>`. IEEE NaN passes both
comparisons and was therefore accepted. They now explicitly reject NaN.
The protocol's single-precision form (tag `0x3F`, `PythonReader.ReadDouble`)
represents the minimum 0.9 as 0.8999999761581421, which the previous comparison
rejected. Both paths now accept this representation as well as the double
endpoint. This is a boundary conversion correction, not a changed height range;
the immediately lower single-precision value remains invalid. Infinity already
fails the bounds check.
Rejecting malformed requests is an emulator integrity rule, not an observed
retail error-response behavior. Error presentation and original server naming
policy remain separate fidelity questions.

`CharacterClonePacketTests` covers the recovered message via its registered
handler, exact consumption of appearance/race fields, malformed tuple lengths,
overflow in both pods and gender, nonfinite/out-of-range heights, valid bounds
and null/undefined scalar values. Original-client interactive comparison is
still outstanding.

The original bytecode and static disassembly are retained outside Git:

- `/home/blizz/backups/rasa-net/research/20260912-new-character/client-inputstate-charactercreation.pyo.selected.dis`
- `/home/blizz/backups/rasa-net/research/20260912-client-artifacts/combat-selected/shared-gameconstants.raw-dis.txt`
- `/home/blizz/backups/rasa-net/research/20260912-new-character/client-ui-charactercreationwindow.pyo.selected.dis`
