# Character selection protocol audit

The ordinary existing-family creation request matches the original client:
`charactercreation.OnCreateCharacter` (line 83, offsets 105–138) sends
`(slot, family, first, gender, height, appearance, race)`. The server's success
packet also precedes the updated pod, matching the client's transition back to
character selection. The first-login skip test remains based on no previous
logins, no previous game context, and the account's skip entitlement.

Deletion had an independent framing defect. The original selection UI sends
exactly one one-based slot in a tuple (`characterselection.OnDeleteCharacter`,
line 114, offsets 37–55). The server accepted arbitrary tuple lengths and cast
the received unsigned slot to byte without checking overflow: 257 became slot
1. The manager also permitted deletion in non-world states outside selection.
The corrected reader requires a one-field tuple and checked conversion; the
manager requires CharacterSelection. Valid deletion, linked cleanup, the empty
pod update, and stale manifestation cleanup retain their previous behavior.

The new tests exercise the oversized-slot failure against a populated account,
malformed tuples, all seven non-selection states, and a successful valid delete.
The root agent owns serial validation; no build was run by this agent. Exact
original artifact hashes and findings are in
[the evidence manifest](evidence/client-selection-deletion.json).

The first combined run passed 1,245/1,246 cases. Its sole failure was the new
disconnected-session test expecting a failure reply: the transport intentionally
drops packets after disconnect. Persistence assertions passed. The test now
expects no reply only in that state; every other rejected state still requires
one failure packet. No production change was needed. The corrected full suite
passed **1,246/1,246 tests**, none failed or skipped, in 6.0290 minutes. The log is
`/tmp/rasa-retail-20260922-selection-verified.log`, SHA256
`535787464acf67366aa8391c0ec80e5b06c920eb84bf2c788e074bc6cbabd377`.
All 2,050 `src/` entries match the tested snapshot, including 1,972 C# and project
files. Comparing the failed and corrected run snapshots, the only changed
source file is `NewCharacterTests.Deletion.cs`, containing that expectation
correction. The evidence manifest records the snapshot path and hash.

A separate existing-family case was also corrected: the original creation window always
applies `lower().capitalize()` to the family name before sending it, including
when the family editor is disabled. A legacy or GM-renamed family `McDonald`
therefore sends `Mcdonald`. The server now recognizes that as the same persisted
identity with an ordinal case-insensitive comparison and retains `McDonald`
for storage and the success/pod responses. A genuinely different family remains
refused while characters exist. Two regression cases check both outcomes,
including unchanged account ownership and family spelling. No family rename is
performed by this correction; both cases passed in the corrected full suite.

No direct connection to the startup “argument list must be a tuple” error was
found. The audited creation, clone, play and delete senders explicitly build
their tuples. That native callback error needs its own caller trace.
