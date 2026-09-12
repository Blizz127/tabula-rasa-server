# Ability use and request protocol — 2026-09-12

The target is the final live game preserved 1:1. This correction uses the
recovered 1.16.5.0 client described in [client artifacts](client-artifacts.md).
Its original client logic is stronger evidence than earlier emulator defaults;
the community package still lacks independent official checksum authentication.

## Skill ability requirements

[The preceding skill audit](final-client-skill-evidence.md) records the exact
member hashes and bytecode locations for the complete 73-skill catalog, 53
mapped skill abilities, maximum ranks, and Logos sequences. This pass joins
those active abilities to `logosstone.logosSequences` in
`AbilityRequirements.RequiredLogos`. The 54-entry original Logos table includes
an entry outside the active skill join; it is not silently converted into an
extra skill. Sprint's active skill ability has an empty Logos sequence.

The complete catalog was independently compared with the original literal
bytecode using the strict decoder described in
[mission evidence](river-recon-client-evidence.md), avoiding decompiler index
substitution errors. All 73 rows and their complete ordered Logos joins matched,
with zero differences. `skill-selected/raw-literal-table-evidence.json` has
SHA-256 `af818d4307003aa27441ad62d068d6918e1fe45dfd5114b9b43f91e11a770952`;
`raw-catalog-comparison.json` retains the comparison results.

The original skill window's `_CanUseAbility` accepts a selected rank at or below
the maximum advertised learned rank. The server therefore permits casting a
lower learned rank, while rejecting zero, negative, excessive, unknown, or
unlearned skill requests. Class ancestry uses the previously verified catalog.
Canonical skill identity determines ownership; a corrupted stored ability-ID
field does not authorize an unrelated ability.

`Manifestation.HasLogosForAbility`, original source line 1553, requires membership
of every Logos ID in the ability's sequence. Collection order is immaterial.
The server now applies that use requirement. It does not turn it into a
skill-purchase requirement. Read-only comparison confirmed the live Logos ID
crosswalk for Area 1, Chaos 4, Defend 7, Power 23, Vortex 48, Transform 331, and
Teleport 384. Complete acquisition/spawn accuracy remains separate work.

`BaseActorAction` defaults `canDoWhileDead` to false and checks it in
`CheckAction`, original line 329. A static scan of all client action members
found explicit overrides in AI/debug actions, Resistance, Sacrifice, and
SelfRes. Resistance and Sacrifice both explicitly retain false. SelfRes sets
true but maps to action **417**, `POLY_SELF_RES`, outside the active 53 skill
abilities. The current skill request gate rejects dead actors without declaring
that every original action, consumable, or polymorph ability is forbidden while
dead. Crouching remains permitted, matching `BaseActorAbility.performCrouched`
and the [official D11 correction](death-retail-evidence.md).

## Original request contract

Static Python 2.4 disassembly and decompiler output are preserved outside Git in
`/home/blizz/backups/rasa-net/research/20260912-client-artifacts/ability-use/`.
`source-manifest.json` and `method-evidence.json` record hashes, original source
filenames and method lines. Tools: xdis 6.1.7 and uncompyle6 3.9.3. No acquired
game code was executed or imported.

| Original `trpython.zip` member | SHA-256 |
| --- | --- |
| `client/actions/abilities/baseactorability.pyo` | `b4d28fe9b85005cb3b13911b790e9befdf0bb78c3250344626591d0a9ae614ff` |
| `client/actions/baseactoraction.pyo` | `e2c220c955e0b413797f1c89da4ea69787f49dba7bad4c9ac21b8e5618ea2522` |
| `client/actions/targetedaction.pyo` | `b9c96045f3d3d3d670550c81c7216644cb38e3b84adcfae35ab496636bcc56d4` |
| `client/augmentations/actor.pyo` | `2ecb129d5071ac588e076415857ea4e5b3ebae08a964e2828b885089ac316c4f` |

`BaseActorAbility.SendServerRequest`, original first line 249, constructs
`(actionId, actionArgId, target, itemId)` at bytecode offsets 38–62. At offsets
0–35, it selects either `_targetLocation` or `targetId`; at 65–100 it optionally
appends the actor body's yaw. `TargetedAction.SetTarget`, original line 536,
sets target to `None` for no-target/party actions and to the actor ID for
self-target actions. `PreWindup`, line 143, obtains location targets through
the native controller's `GetSplatLocation`.

The previous C# decoder assumed every target was a Python long, narrowed it to
32 bits, discarded item identity during dispatch, and ignored optional yaw.
The decoder now retains optional entity and item IDs with 64-bit width, optional
three-coordinate location, and optional finite yaw. It accepts a tuple or list
as the location sequence; the inspected Python forwards the native position
object, so an authentic capture is still needed to establish which sequence
encoding that native method emits. Accepting both does not change coordinate
values. Invalid outer/coordinate lengths and nonfinite coordinates are rejected.
Those explicit validation failures use the existing handled
`InvalidClientMessageException` path so the session closes without escaping the
handler as an unhandled format exception. General malformed-packet isolation
outside this request remains a separate audit item.

The action queue now retains location, item identity and yaw for subsequent
effect work. Existing entity-target consumers still use zero as their absent-ID
sentinel. This change does not make existing recovery handlers understand every
target shape or authorize a particular location for a particular ability.

`Actor.Recv_UserActionFailed`, original line 1902, receives
`(actionId, actionArgId, msgId)`. Offsets 33–95 treat `msgId=None` as no localized
message, and the subsequent instructions remove a matching pending resolution.
The new failure packet uses that supported optional-message form when rejecting
a skill request. `Recv_ActionFailed`, line 1939, is a distinct current-action
cancellation method. This pass does not guess localization codes or claim the
original server's complete failure/cancellation ordering from these consumers.

## Implementation scope and remaining fidelity work

`ManifestationManager.RequestPerformAbility` checks the active game session,
map presence, pending removal, and skill requirements before queueing. Unrelated
weapon/object/debug actions cannot be injected through this request to bypass
their own handlers. Item-granted, mech, polymorph, and other non-skill abilities
need their original authorization paths; this skill gate does not implement
them. Many recognized skill effects themselves also remain absent.

The current Lightning damage and Sprint effect retain placeholder formulas and
timing, explicitly visible in their existing handlers. This pass does not claim
to have corrected those effects. Cooldowns, power/adrenaline/item consumption,
range, target eligibility, area geometry, interruptions, post-queue revalidation,
and exact effect payloads remain required work. The decoded client now provides
concrete source paths for that reconstruction rather than permission to guess.

Focused tests exercise learned and lower ranks, every member of a multi-Logos
requirement, class ownership, signature limits, dead/crouched actors, unrelated
action rejection, queue field preservation, failure serialization, 64-bit IDs,
`None` targets, location sequences, optional yaw, and malformed shapes. A
synthetic queue fixture tests transport fields only, not target eligibility.
The isolated focused run passed all 50 selected ability/skill cases. Full
candidate validation and deployment are recorded in [the work log](retail-accuracy.md).
Real-client interaction is still unverified.
