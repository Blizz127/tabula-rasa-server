# Character name validation: the original client's rules

> **Reconstructed 2026-09-26.** `src/Rasa.Game/Data/CharacterNameRules.cs` has cited this document since it was
> written on the deployed-but-uncommitted 2026-09-22..24 tree (commit `234d703`, "A new character is made the way
> the original creation screen makes one, and a hybrid race is earned."). That tree's own narrative write-up was
> never committed and is lost; there is no earlier version of this file to recover. What follows is reconstructed
> from `docs/evidence/character-names.json` (recorded 2026-09-22) and the current `CharacterNameRules.cs`
> implementation it backs. Nothing here goes beyond what that evidence file states.

Research date 2026-09-22. Compatibility client 1.16.5.0; the exact shutdown revision is unverified (see
`docs/setup.md` and AGENTS.md).

## Sources

| Key | Kind | Location | Notes |
| --- | --- | --- | --- |
| `messages` | original client | `generated/client/language/english/playermessagelanguage.pyo`, sha256 `b9b27bed2053b422d29069e1a8e97e26bba636512ae328d684bef16eae63fba8`, compiled epoch 1234238147 | Creation error text strings |
| `failure_mapping` | original client | `client/clientmethod.pyo`, sha256 `61539c55746134786400491c76ac428a684d2f9e55c0ec4a68d4d09915210e78`, `Recv_UserCreationFailed`, bytecode offsets 28-210 | Maps failure codes to messages |
| `creation_ui` | original client | `client/ui/charactercreationwindow.pyo`, sha256 `5fdc95af80cc5c792550c3ad2354ecfd1a037b495d8d885cae65ba9b5deee502`, `_CreateCharacter`, original lines 708-711, offsets 171-216 | Name is lowercased/capitalized and matched against existing family before send |
| `scope_report` | contemporary firsthand report | forums.mmorpg.com/discussion/157923/pegasus-server-problem, Rayx0r, 2007-12 | Surname uniqueness and a "John Aelric" example; release-era, final-live scope not independently captured, raw download returned 403 |
| `last_name_revision` | contemporary secondary wiki | archive.org tabularasafandomcom wiki, "Last Name", revision 35315, 2008-10-23T03:42:45Z | "delete all characters to change family" |

## Rules

Five fields are read directly from the shipped client text/bytecode (`original` tier, high confidence):

| Field | Value | Source | Location |
| --- | --- | --- | --- |
| `minimum_length` | 3 | messages | message 134, assignment offset 2972 |
| `maximum_length` | 20 | messages | message 133, assignment offset 2936 |
| `capital_initial` | true | messages | message 135, assignment offset 3008 |
| `letters_only` | true | messages | message 135, assignment offset 3008 |
| `maximum_consecutive_equal_letters` | 2 | messages | message 135, assignment offset 3008 |

Five further fields are `inferred` (logical reconstruction from the same client text plus dated contemporary
sources), medium confidence, each with a stated gap:

| Field | Value | Source | Unverified |
| --- | --- | --- | --- |
| `unicode_and_case_algorithm` | UTF-16 length, .NET uppercase/letter classification, invariant case-insensitive repetition | messages (encoding of message 132, format text of message 135) | Original native Unicode categories, normalization and supplementary characters |
| `first_name_unique_scope` | account/family scope | scope_report | Final-live server lookup scope and deleted-name reservation |
| `name_lookup_case_sensitive` | false | creation_ui (names lowercased and capitalized before send; existing family lookup) | Original locale/accent collation; the implementation uses provider-independent `OrdinalIgnoreCase` |
| `family_change_requires_no_characters` | true | last_name_revision | Final-live server-only policy changes |
| `duplicate_first_name_error` | failure code 7 | failure_mapping (offsets 104-114 map 7 to `PM_NAME_IN_USE`) | Exact original duplicate-request response among reserved/in-use errors |

## What `CharacterNameRules.cs` implements

`CharacterNameRules.Validate` (`src/Rasa.Game/Data/CharacterNameRules.cs`) checks, in order: non-null, length
3-20 (`MinLength`/`MaxLength`), a leading capital letter, every character a letter, and no run of three (or more)
case-insensitively-equal letters — matching the five `original`-tier rules above exactly (the "no letter three
times" phrasing corresponds to `maximum_consecutive_equal_letters = 2`, i.e. two is the most that may repeat).
First-name uniqueness, the case-insensitive lookup, and the family-change and duplicate-name error paths are
implemented in `CharacterManager`/`CharacterRepository` per the `inferred` rules above.

## Gaps

The evidence file records these as unresolved:

- Original reserved/profanity word list.
- Final-live duplicate-name capture.
- Original Unicode normalization/collation.
- Original deleted-name reservation lifetime.

## Implementation-only choices (not claimed as original)

- Family state is read within the creation transaction.
- Existing names are retained without being rewritten.
- Uniqueness is enforced with a single-process creation lock; there is no cross-process uniqueness claim.
