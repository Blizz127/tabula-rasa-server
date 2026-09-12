# Personal and home item destruction

Research date: 2026-09-12. Emulator baseline: `21d7d40`. The preservation
target remains the original final live service. This pass repairs ownership,
quantity handling, and atomic persistence for the existing personal and home
destruction requests. It does not establish the original database transaction
boundaries or complete every item-consumption mechanic.

## Original client evidence

The recovered `Tabula Rasa 1.16.5.0/trpython.zip` is retained under
`/home/blizz/backups/rasa-net/research/20260912-client-artifacts/final-client-selected/`.
Its compatibility version is not an independent proof of the shutdown client's
exact revision; see [final-retail-target.md](final-retail-target.md).

Selected original members, bytecode, source manifests, analysis script, and
validation results for this pass are retained outside Git in
`/home/blizz/backups/rasa-net/research/20260912-item-consumption/`.
`extract-client-evidence.py` uses xdis 6.1.7 to inspect Python 2.4 code objects.
Acquired game code is never executed or imported. Uncompyle6 3.9.3 output is a
reading aid; control flow below was checked against the raw instructions in
`consumption-consumers-bytecode.json` and `consumption-consumers.raw-dis.txt`.

| Original member | Compiled UTC, 2009-02-10 | SHA-256 |
| --- | --- | --- |
| `client/inventory.pyo` | 03:55:09 | `fea5b3bc09103b261cdccd8b9396b0cef0e93c0b484fcb1039655da57990bd70` |
| `client/augmentations/item.pyo` | 03:55:15 | `1b5995e918b08c7ca2872d07c80ce55686f4f9057da94af636db4c6f27eb8691` |
| `client/ui/inventorywindow.pyo` | 03:55:25 | `dc0ec8d20babe216848b2b64c087d9a0a49e53ca2b0d0cf8dede5bb9abc17830` |
| `client/ui/lockboxwindow.pyo` | 03:55:25 | `f4209434382cd297d70e2c31090ebff69869e3a99b56d65a41d188d35ce0e5ae` |
| `client/ui/quantitychooser.pyo` | 03:55:25 | `c577e53e37fab87bafe906564acc12213399eea6b68474ec4759e7b0ed785a42` |

`original-manifest.json` also records the specialized personal/home quantity
chooser modules, byte lengths, archive paths, and embedded original filenames.
The decompiler's February 9 evening header is the host's America/Chicago local
time; the table uses UTC module timestamps.

High-confidence facts for this recovered client are:

- `client.inventory.DestroyItemInInventory`, original line 887, checks which
  inventory contains the entity and forwards `(entityId, quantity)` unchanged.
  Personal uses `PersonalInventory_DestroyItem` (bytecode 22–40), home uses
  `HomeInventory_DestroyItem` (70–88). Clan has a separate branch and request.
- Personal `inventorywindow.OnDNDDropTrash`, line 762, and home
  `lockboxwindow.OnDNDDropTrash`, line 601, retain the drag's entity ID and
  quantity. Their delete callbacks, lines 903 and 742 respectively, forward
  those values to `DestroyItemInInventory`. The original client owns its
  confirmation prompt and `User.UserInterface.ConfirmItemDeletion` option.
- Personal `_GetQuantity`, line 1118, starts at one and uses the stack count
  for a stackable item unless Control is held. This supports quantities smaller
  than the whole stack; destruction is not inherently an all-or-nothing request.
- The shared `quantitychooser.Setup`, line 79, permits numeric input from zero
  through the maximum. `OnOkPressed`, line 121, parses `int(text)`, clamps values
  above its maximum, and invokes its callback with the result. Raw offsets
  106–143 also show a parse-error path assigning zero and reaching the callback;
  the decompiled `else` presentation should not be used to infer otherwise.
  This shared dialog does not by itself prove every trash path exposes zero,
  or what the original server did with it. The emulator therefore treats a
  representable zero as a harmless no-op instead of a malformed connection.
- `Item.Recv_SetStackCount`, line 145, consumes the replacement count and
  publishes the corresponding stack-count event. `Recv_InventoryRemoveItem`,
  line 325, passes `(type, entityId)` to `_RemoveItem`, line 597, which removes
  that inventory's mappings and updates the UI.

The original receivers establish the request/reply fields and separate
personal/home inventory identities. They do not specify the original server's
SQL schema, its error response to an overdrawn request, or the exact ordering
of a full removal and physical-entity destruction in a retail capture.

## Proven emulator defects and implementation

At the baseline, `ReduceStackCount` compared an item's character owner with
the account's selected roster slot. A normal character ID such as 101 and
roster slot 2 therefore failed the ownership check. Home ownership zero also
failed it. The method then subtracted unsigned values before checking the
requested amount, so excessive quantities could wrap into very large stacks.

Full destruction changed memory and sent entity removal before writing the
database. It always deleted from personal inventory using the character ID,
even when called for home storage, and selected the inventory row by slot
rather than the exact item identity. Partial destruction similarly changed
the client and memory before an unconditional database update.

`ReduceStackCount` now requires a live in-game character session and checks:

- the actual registered `Item` instance and its persistent item ID;
- the selected personal or home slot contains that same entity;
- the item's owner is the character ID for personal inventory or zero for
  home storage;
- the amount is positive and does not exceed the in-memory stack.

No alive/dead gameplay restriction, bank-access policy, or new confirmation UI
is introduced here. Unsupported inventory types, absent entities, stale slot
references, zero amounts, and excess amounts cause no mutation.

`ItemRepository.TryConsumeItemStack` repeats the decisive constraints in a
database transaction. Its conditional update requires the expected stack and
an inventory link matching **account, character/home owner, inventory type,
slot, and item ID**. It updates only `items.stack_size`. For full consumption,
it removes only that exact inventory link, then commits both changes together.
If either affected-row count differs from one, disposing the uncommitted
transaction rolls back the earlier update. A database error also leaves the
transaction uncommitted; the handler does not publish success or change memory.

Only a committed result changes the in-memory stack. Partial consumption sends
the new `SetStackCount`. Full consumption clears the verified slot, unregisters
the item entity, and sends `DestroyPhysicalEntity` followed by
`InventoryRemoveItem` for the correct inventory type. The latter ordering is
retained from the existing emulator and is compatible with the inspected
personal/home removal path; it is not claimed as a recovered retail ordering.

The exhausted `items` row remains with stack zero and no inventory link, as in
the existing atomic reload implementation. Retaining this unowned record is an
internal storage choice, not a claim about original server retention. It is not
loaded into a later character inventory. No schema migration or live-data
rewrite is needed.

The two request codecs now require the two-field tuple and a 64-bit entity ID,
and accept nonnegative integer quantities represented as Python `Int` or
`Long`. Negative, non-integer, or greater-than-`uint.MaxValue` quantities are
rejected through `InvalidClientMessageException`, which the existing receive
path handles. Zero is decoded exactly and ignored by the mutation path. This
eliminates unchecked narrowing such as `4294967297` becoming `1`.

## Validation and limits

`ItemConsumptionTests` exercises real SQLite transactions and both actual
destruction handlers, reopening database contexts for persisted-state checks.
It distinguishes character 101 from roster slot 2 and includes personal/home
items at the same slot, another character, and another account. Cases cover
partial/full quantities, stale owner/location/count, missing rows, invalid
quantities and instance identities, exact full removal, unchanged unrelated
rows and fields, and repeated requests for a destroyed entity.

SQLite triggers deliberately suppress or abort the link deletion after the
stack update. These verify transaction rollback and the absence of memory or
client updates when the second step fails. `ItemDestructionPacketTests` covers
both integer encodings, tuple shape, zero, negative values, excess width, and
non-integer input. Focused logs and TRX results are retained in the research
directory, alongside the isolated source copy. **45 test cases passed, zero
failed**, using cached SDK image `rasa_net:retail-attack-candidate`
(`ba9950bab955`) with networking disabled. Exact artifacts are
`focused-tests-02.log`, `test-results/item-consumption-02.trx`, and
`validation-manifest.json`. The first build log is retained and records a
corrected test-property name. The source copy used here precedes the final
combined equip/swap changes; the full candidate requires its own validation.

These checks establish internal ownership and conservation behavior. They do
not reconstruct clan destruction, consumption through crafting/mission/usable
items, all trade and inventory-move transactions, original failure messages,
original item restrictions, or the original database commit strategy. Home
access and tab permission rules remain separate preservation work. No live
database writes, deployments, commits, or original-client execution were
performed for this validation.
