# Original client login and creation flow

Static recovery from compatibility client 1.16.5.0, recorded 2026-09-22. Exact
shutdown-build equivalence remains unverified. [The evidence manifest](evidence/client-creation-ui-flow.json)
contains hashes, original Python lines/bytecode offsets, native addresses and
retained disassembly paths. No client launch, executable modification or .NET
build was performed for this investigation.

## Supported launch and intro controls

The original native command-line tokenizer (`tabula_rasa.exe` VA `0x401620`)
splits on spaces and then the first `=`. Supported options include
`/NoPatch`, `/NoEULA`, `/AuthServer=HOST:PORT`, `/autologin=FILE`,
`/user=USERNAME`, `/password=PASSWORD`, `/server=INTEGER_ID` and
`/character=NAME`. Quoted spaces are not handled specially by this tokenizer;
keep argument values, including an autologin file path, free of spaces.
The autologin file uses section `[AutoLogin]` and keys `User`, `Password`,
`Character`, `Server`; individual command-line values override those fields.

In `client.cfg` under `[Options]`, the original-supported
`Client.Developer.SkipToLogin = True` routes directly to login.
`Client.UserInterface.ShowIntroMovie = False` skips only the cinematic after
both logos. The normal sequence is `ncsoft.bik`, `dglogo.bik`, optional
`intro.bik`, then login; Esc, Space and Enter stop the current movie on key-down.
The cinematic option is saved false when the intro is first entered.
The archived `client.cfg` already contains prior user configuration, including
this false value; it is not evidence of pristine retail defaults.

Both automatic username and password must be nonempty to connect automatically.
Otherwise the manual window uses `Client.Networking.LoginUser` for its remembered
username, not `Client.AutoLogin.Username`. Its OK button enables only after both
text fields contain something. The auth endpoint comes from `login.cfg` section
`[Auth]`, key `Host`, and must include `host:port`; the original Python code
splits on `:` and reads the second element directly.

## Screens and packets

After credentials, the client requests the server list. Explicit automatic server
selection overrides the remembered server; a missing requested ID produces a
last-server-unavailable dialog. A server-list port of zero becomes 8001, so an
isolated harness using another port must advertise it explicitly.

`BeginCharacterSelection` initializes race/skip eligibility and enters the
selection screen. Empty-slot Create uses one-based slot numbers. Pod updates
arriving while that window is hidden are ignored, as are slot zero and `None`;
keep selection entry before slot updates.

The first family name is entered beside the character name on the creation
screen. When the original family value is `None`, Accept displays
`ConfirmSetCharacterLastName` before sending creation. An existing non-`None`
family locks its field. An empty string is not equivalent to `None` in either
branch. Name field roles can swap with locale; the client lowercases and
capitalizes both names before sending.

| Situation | Original user method | Tuple fields |
| --- | --- | --- |
| Family is `None` | `CreateCharacter` | family, character, gender, height, appearance, race |
| Existing family | `RequestCreateCharacterInSlot` | slot, family, character, gender, height, appearance, race |
| Clone | `RequestCloneCharacterToSlot` | source slot, destination slot, character, gender, height, appearance, race |

`/character=NAME` compares against the pod's character name case-insensitively,
clears itself on a match, and plays with `skipBootcamp=false`. It therefore
bypasses the skip prompt. Boot-camp skipping is a selection/play request option,
not a creation tuple field.

## Tuple-error diagnosis

The logged `TypeError: argument list must be a tuple` is raised by the original
`python24.dll!PyEval_CallObjectWithKeywords` at RVA `0x25ebb`. Its guard at RVA
`0x25e9a` rejects a non-null argument object that is neither a tuple nor tuple
subtype. A null pointer is accepted and converted to an empty tuple, so this is
not simply a missing-arguments error. `PyObject_CallObject` at RVA `0x18ee0`
forwards into that guard.

The executable has ten direct calls to that API, listed in the manifest. The
current log has no identifying call stack, and no concrete malformed caller has
been established statically. A debugger stack at the loaded `python24` base plus
`0x25ebb` would distinguish the caller without changing original code. The
observed message alone does not establish that login or creation failed.
