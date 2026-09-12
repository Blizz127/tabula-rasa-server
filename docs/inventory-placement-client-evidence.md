# Personal and Home inventory placement

Inspected 2026-09-12 against the archived 1.16.5.0 compatibility client. The
preservation target remains the original service immediately before shutdown;
identity of this artifact with the exact final build still needs an authentic
manifest. See `final-retail-target.md` and `client-artifacts.md`.

## Original evidence

Static-only originals, extraction script, hash manifest, structured instructions
and raw disassembly are retained under
`/home/blizz/backups/rasa-net/research/20260912-inventory-placement/`.
No acquired module was imported or executed. The archive is
`final-client-selected/Tabula Rasa 1.16.5.0/trpython.zip` in the retained client
research directory; its modules contain Python 2.4 bytecode dated 2009-02-10 UTC.

| Original member and function | Source line / instruction offsets | Evidence |
| --- | --- | --- |
| `client/inventory.pyo`, `_SendServerRequest` | 1638; 711–726, 757–772, 912–927, 958–973 | All four personal/Home move calls carry `(srcSlot, destSlot, quantity)`. |
| Same, `AddItemToPersonalInventory` | 621; 46–135, 316–400 | Requires a living avatar. Missing quantity or a nonstackable item becomes quantity 1; oversized requests are clamped to the visible stack count in the client. |
| Same, `_AddItemToInventory` | 842; 27–116, 250–334 | The Home/Clan helper likewise requires a living avatar and retains the selected stack quantity. |
| `client/ui/inventorywindow.pyo`, `OnQuantityOk` | 951; 38–98 | Zero cancels; other selected quantities start the drag with that quantity. |
| `client/ui/lockboxwindow.pyo`, `OnQuantityOk` | 767; 38–98 | The same quantity-selection behavior exists in the lockbox. |
| `client/augmentations/item.pyo`, `GetMaxStackCount` | 158; 0–23 | Stack capacity comes from the original item-class table. |
| `client/ui/inventorywindow.pyo`, `OnDNDDrop` | 665; 190–248 | Dropping into a different personal category discards that destination and lets the inventory helper select a slot in the item's own category. |
| Same, `OnIconRightClicked` | 804; 627–712 | A Home deposit checks `IsPlaceableInLockbox`; a disallowed item produces local error audio. |
| `client/inventory.pyo`, `AddItemToHomeInventoryTab` / `_GetSlotRangeForTab` | 801 / 1021 | Converts a tab-local slot to the cumulative range from the original tab table. |

The personal inventory's five 50-slot categories come from
`data/game.zip::generated/client/inventorydata.pyo`; the five 96-slot Home tabs
come from `generated/client/lockboxtabdata.pyo`. The prior static literal records
are in `20260912-client-artifacts/equipment-eligibility/` and
`20260912-lockbox-tabs/` respectively. Locked tabs are displayed through the
server's availability dictionary; `LockboxTabIsLocked` and the window's tab
setup distinguish available storage from tabs still offered for purchase.

Member hashes, also retained in the extraction manifest:

- `client/inventory.pyo`: `fea5b3bc09103b261cdccd8b9396b0cef0e93c0b484fcb1039655da57990bd70`
- `client/ui/inventorywindow.pyo`: `dc0ec8d20babe216848b2b64c087d9a0a49e53ca2b0d0cf8dede5bb9abc17830`
- `client/ui/lockboxwindow.pyo`: `f4209434382cd297d70e2c31090ebff69869e3a99b56d65a41d188d35ce0e5ae`

Confidence is high for call shapes, quantity selection, original table sizes and
client admission predicates. Database transaction design is an emulator repair,
not a claim about the original server's implementation.

## Corrections

The four move handlers previously ignored quantity and separately persisted
items in a swap. Home withdrawals also discarded a long-encoded quantity during
decoding, and some bounds checks admitted index 250/480 into shorter lists.

All four requests now decode the same strict three-field contract. Whole-item
moves and swaps compare ownership, locations and expected stack counts, commit
both locations, then publish ordered remove/add notifications. Home access also
compares the account's persisted tab count, including equipment swaps through
Home storage. Personal destinations stay in the item's original category, and
items flagged as not placeable in the lockbox cannot enter it through a swap or
equipment removal.

Selecting less than the source count now splits into an empty destination. One
transaction decreases the original count, copies the persisted item attributes
into a separate item row and inserts its destination location. Failure at either
insert rolls back every change. The runtime registers/publishes that new item
only after commit and sends its entity data before the inventory addition.
The source keeps its identity; both rows retain their combined quantity.
Persisted HP, color, ammunition, crafter and creation metadata are copied rather
than reset using a template constructor. Creation metadata is a local storage
choice, not independently established original client-visible behavior.

## Verification and fidelity gaps

Integration tests exercise all four directions for splits and nonstackable
swaps, reloaded instance data, tab edges at 95/96, 191/192 and 479/480, category
and dead-avatar admission, stale stack/template/location/tab state, and failures
at the second swap write or either split insert. They verify rollback and absence
of partial client publication. Packet tests cover long quantities, invalid
arity/types/signs/widths and the handled message exception. Equipment tests cover
locked Home sources and a no-lockbox item entering Home through unequip.

**Occupied-stack combining remains incomplete.** The client forwards quantities
but does not supply the original server's complete combining/equality/overflow
rules. A partial move into an occupied slot remains unsupported; whole occupied
stack swaps retain the existing behavior, which is not certified as original
stack combining. Web searches on 2026-09-12 did not recover usable original
combining evidence. This work does not establish full inventory fidelity.

Other gaps: original proximity/access rules and rejection messages, complete
instance binding/module representation and final template flags, Clan storage,
automatic stack combining, and broadcasts that refresh other sessions sharing
an account's Home inventory. Existing invalid stored placements are not silently
relocated or deleted. Native-client comparison with original-service captures
remains necessary, including publication behavior during split/merge operations.
