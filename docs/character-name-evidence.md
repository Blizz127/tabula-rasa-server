# Character naming — 2026-09-22

Creation and cloning previously accepted `\w{3,20}`: digits, underscores,
lowercase initials, triple repeated letters, and a trailing newline could pass.
The original client's creation error text contradicts these formats. Both
creation forms, cloning and the existing rename path now share one validator.
No existing saved names are rewritten.

The recovered client's English `playermessagelanguage.pyo` has messages 133–135
at assignment bytecode offsets 2936, 2972 and 3008: maximum 20 characters,
minimum 3, capital initial, letters only, no more than two repeated letters in
succession. `clientmethod.Recv_UserCreationFailed`, original first line 833,
offsets 44–78, maps creation failures 2/3/4 to these messages. This ties the text
to character creation rather than assuming that a generic rename rule applies.
`charactercreationwindow._CreateCharacter`, original lines 708–711, offsets
171–216, lowercases then capitalizes both supplied names before sending them.
The server validates rather than silently rewriting a malformed request.

The language table is compiled 2009-02-10; client provenance and the unverified
exact shutdown revision remain those in [client artifacts](client-artifacts.md).
Hashes and precise locations are in the [rule manifest](evidence/character-names.json).
The Unicode letter/case implementation, case-insensitive repetition and lookup
comparison remain inferred: original native server validation/collation was not
recovered. Modern .NET Unicode categories and ordinal case folding may differ
from the original at non-ASCII boundaries. The original reserved/profanity list is also missing;
this change does not invent one.

The creation transaction now refuses an already-used first name within the
account. A new character in another family may use the same first name, and the
rename query uses the same scope. This scope is **inferred** from a
[contemporary firsthand explanation](https://forums.mmorpg.com/discussion/157923/pegasus-server-problem)
by Rayx0r in December 2007 (the comment beginning “just gonna throw this out
there”). It distinguishes account-family uniqueness from first-name uniqueness.
An earlier participant's contrary assumption is corrected by that explanation
and acknowledged by the original questioner. The late client retains separate
first-name and family-name reservation messages but cannot prove their exact
server lookup scope. Final-live scope therefore remains an explicit evidence
gap. The reported release-era deleted-name reservation bug is not reconstructed:
there is no evidence it survived to shutdown, nor its original lifetime.

Family reservation now compares case-insensitively in managed code on both
providers. SQLite's ASCII-only `lower()` cannot implement this check: even the
exact spelling `Élodie` previously evaded a comparison against its .NET-lowercased
form. The regression cases cover accented first and family names. Accent
removal and Unicode normalization are not guessed here.
Creation consults persisted family/character state inside its transaction;
an empty stale account cache can no longer replace an existing family's name.
The old ability to select another family after deleting all characters is
retained. The archived TaRapedia `Last Name`, revision 35315 of
2008-10-23T03:42:45Z, supports that behavior; its secondary status is recorded.

The new packet-format regression cases were run against the preceding verified
source snapshot: 8 of 20 failed, reproducing the permissive format defect.
Integration tests cover duplicate create/clone refusal without spending credit,
shared first names across families, case-insensitive surname reservation, and
stale-cache family replacement. These tests verify implementation, not lost
server rules. The existing single-process creation lock serializes creation;
this work does not claim cross-process database uniqueness for historical names.

Local analysis artifacts are retained in
`/home/blizz/backups/rasa-net/research/20260922-character-names/`.
The forum was read through the web tool on 2026-09-22; a separate raw HTML
download returned HTTP 403, so no local raw-capture hash is claimed.

Final integrated validation passed 1,202/1,202 tests with none skipped, including
the naming and transaction cases above. Log: `/tmp/rasa-creation-final-20260922.log`.
