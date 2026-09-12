# Inventory session and selected weapon evidence

Research date: 2026-09-12. Emulator baseline: `6db42a9`. The target remains
the original service immediately before shutdown, including its content and
mechanics. These fixes repair inventory ownership, instance loading, and ordered
equipment notifications; they do not establish complete inventory fidelity.

## Original sources and reproducibility

The source is the statically recovered `trpython.zip` and `data/game.zip` under
`/home/blizz/backups/rasa-net/research/20260912-client-artifacts/final-client-selected/Tabula Rasa 1.16.5.0/`.
That archive label agrees with the compatibility requirement in `docs/setup.md`;
it does not independently prove the shutdown client's exact revision. See
[final-retail-target.md](final-retail-target.md).

All extraction, raw bytecode, manifests, and validation artifacts for this pass
are retained outside Git in
`/home/blizz/backups/rasa-net/research/20260912-inventory-session/`.
`extract-client-evidence.py` uses xdis 6.1.7 to inspect Python 2.4 code objects;
`extract-capacity-constants.py` requires an integer `LOAD_CONST` immediately
before each selected `STORE_NAME`. Neither imports or executes acquired game
code. Uncompyle6 3.9.3 output is a reading aid; the claims below were checked
against raw instructions. Compilation times below are UTC. The decompiler's
February 9 evening header is America/Chicago local time, not a conflicting
build date.

| Archive member | Compiled UTC, 2009-02-10 | SHA-256 |
| --- | --- | --- |
| `trpython.zip:client/inventory.pyo` | 03:55:09 | `fea5b3bc09103b261cdccd8b9396b0cef0e93c0b484fcb1039655da57990bd70` |
| `trpython.zip:client/augmentations/actor.pyo` | 03:55:14 | `2ecb129d5071ac588e076415857ea4e5b3ebae08a964e2828b885089ac316c4f` |
| `trpython.zip:client/augmentations/manifestation.pyo` | 03:55:15 | `724f68eb70e5a47338db82977c55abfa09f1b187f86a0b9a560c012a8588886d` |
| `trpython.zip:shared/gameconstants.pyo` | 03:55:26 | `e0fc260a81d20fe01023ad79e5460773d46aba3d7feeba7291373cba0026ed40` |
| `game.zip:generated/client/inventorydata.pyo` | 03:55:34 | `43d60915efa08ee62ee826b3cea3fb7d123b878ed4f65854b0cdb7ca869ec8db` |
| `game.zip:generated/client/equipmentdata.pyo` | 03:55:34 | `75c2caf08e42fea264dbf521e11350dee44faf369bfdeae035fbb3bc3d5afed8` |

`original-manifest.json` also records byte lengths, embedded original source
paths, and the retained shared inventory/UI drawer modules. Exact method
locations and instructions are in `inventory-consumers-bytecode.json` and
`inventory-consumers.raw-dis.txt`. `inventory-capacity-constants.json` records
the following original integer assignments:

- Home capacity 480: `shared/gameconstants.pyo`, load/store offsets 549/552.
- Clan capacity 500: the same module, offsets 573/576.
- Weapon drawer slots 5: the same module, offsets 652/655.
- Weapon equipment slot 13: `generated/client/equipmentdata.pyo`, offsets 18/21.

The original `inventorydata` category table gives five categories of 50 slots.
`inventorydata-literals.json` preserves its strict literal-decoder output,
including every dictionary construction offset.
`client/inventory.pyo` computes their consecutive offsets and the total personal
capacity from that table. Inventory identifiers are personal 1, home 2,
equipped 8, weapon drawer 9, and clan 15. These support the existing capacities
and type identifiers; this pass changes no capacity or storage price.

## Client contract versus server persistence

The following client facts have high confidence for the recovered build:

- `client/inventory.pyo::Recv_InventoryAddItem`, original line 316, accepts
  `(type, entityId, slot)` and delegates to `_AddItem`, line 549. The latter
  inserts the entity into the selected inventory's mappings. Drawer membership
  is distinct from the actor's selected weapon.
- `GetWeaponItemBySlot`, line 1495, reads the weapon drawer mapping.
  `_AddItemToWeaponDrawer`, line 1781, and `_SendServerRequest`, line 1638,
  distinguish personal-to-drawer equipment changes from moves within the drawer.
- `Manifestation.Recv_WeaponDrawerSlot`, original line 483, stores the armed
  slot. When `bRequested` is false it also initializes the requested slot;
  then it posts an armed-weapon UI update. It does not choose a weapon by
  inventory arrival order.
- `Actor.Recv_EquipmentInfo`, original line 2287, resets its equipped dictionary
  at bytecode offsets 48–54, then consumes `(slotId, entityId)` pairs. It checks
  that each entity exists before accepting it (offsets 76–114). The weapon
  begins as `None` and is assigned only for the weapon slot (128–147); an absent
  weapon therefore clears it. The receiver publishes equipment changes and an
  armed-weapon UI event after processing the replacement set.

These receivers support publishing item entities before referencing them and
keeping drawer position, selected slot, and equipped weapon consistent. They
do not reveal the original database keys, original transaction boundaries, or
the original server's complete login packet ordering. The deterministic query
order added here is an emulator implementation choice, not a recovered retail
wire-order claim.

## Proven emulator defects and changes

At the baseline, `InitCharacterInventory` queried every inventory row for an
account. It constructed, registered, and sent each item before checking its
character owner. A second character's private items therefore became client
entities even though they were never placed in the active character's slot
lists. They also escaped cleanup that traverses those lists.

`CharacterInventoryRepository.GetItems(accountId, characterId)` now selects
only that character's personal/equipped/drawer rows, plus account home rows
whose character owner is zero. It orders by inventory type, slot, and item ID.
The account and character filters apply in the database query before entities
are constructed. Account-home ownership here is established by the emulator's
existing storage read/write convention; original server schema equivalence is
not asserted.

The loader checks destination capacity and occupancy before publishing an
entity. Missing item rows or templates no longer abort the remaining valid
inventory. Unusable rows are logged and retained in the database. Duplicate
locations load the first usable row in the stable order and withhold later
collisions; this is containment of corrupt emulator data, not a retail policy
for impossible duplicate locations. Direct equipped-inventory slot 13 rows are
withheld because this emulator stores weapons in inventory 9 and uses equipped
slot 13 as the selected drawer's reference. No database repair or migration is
performed. Loaded instances now also retain `ItemTemplateId` alongside their
resolved template and existing stack, magazine, health, color, and crafter.

Previously, every drawer item added by `AddItemBySlot` overwrote equipped slot
13. For example, selected drawer 1 was replaced by drawer 4 merely because
drawer 4 loaded later. Adding/removing an inactive drawer now preserves the
equipped weapon; changing the active drawer updates its reference. The final
login reference is derived from the persisted active slot. Empty or invalid
persisted selections do not borrow another drawer's weapon or rewrite the
persisted selection.

`RequestArmWeapon` now persists valid empty selections and calls
`RefreshArmedWeapon(client, previousWeaponEntityId)`. That helper refreshes
equipment, appearance, and magazine information, clears readiness when no
weapon remains, and cancels pending weapon work when the actual weapon changes.
The same helper runs after active drawer swaps and equipment changes. An
inactive drawer equip preserves the selected weapon and its pending reload.
The weapon-equip/drawer input bounds are checked before indexing; an item without
weapon data cannot be equipped through the weapon handler.

`EquipmentInfoPacket` now snapshots its occupied slots at construction and
serializes without mutating its state. Previously a second serialization could
throw on duplicate dictionary entries, while queued selections could both read
the final mutable inventory list. `AppearanceDataPacket` also snapshots its
slot, class, and both color values, preventing later appearance edits from
rewriting an already queued selection. The existing wire field layouts are
preserved.

## Validation and remaining limits

`InventorySessionTests` uses isolated SQLite databases, real item/inventory/
character/appearance repositories, actual inventory and selection managers,
and fresh database contexts. Its fixtures distinguish character ID 101 from
account roster slot 2, include another character and account, and share home
storage. It checks entity publication/order, selected-versus-last drawer,
empty selection persistence, instance values after a fresh session, malformed
rows, active/inactive moves, pending reload cancellation, and immutable packet
serialization. The related reload lifecycle/persistence tests are included in
the focused validation run: **44 passed, zero failed**, using the cached
`rasa_net:retail-weapon-candidate` SDK image `c0649d7af720` with networking
disabled. Exact artifacts are `focused-tests-05.log`,
`test-results/inventory-session-05.trx`, and `validation-manifest.json`.
The TRX counter includes seven data-driven parent containers (51 records);
the console and leaf test count are 44.
The earlier logs remain as the record of fixture setup corrections. The source
copy used for this focused run precedes root's final attack-manager integration;
the combined candidate still requires its own validation.

No live database, schema, deployment, or original client execution is involved
in this validation. Passing tests demonstrate these implementation properties,
not complete 1:1 inventory behavior. Original final server login ordering,
home access eligibility and permissions, all item uniqueness/binding rules,
transaction rollback across inventory swaps, overflow/trade/auction/inbox
lifecycles, and item destruction still require separate fidelity work.
Initial weapon appearance now follows the selected inventory item, including
clearing stale weapon appearance/readiness for empty selection. Persisted
appearance records are not rewritten during load, and the original second-hue
persistence contract remains unverified. Unsupported stored
inventory types remain preserved in the database rather than being exposed
as ordinary personal inventory.

An independent review also corrected repeated inventory initialization during
map transfer. `DynamicObjectManager` retains the player object, and
`MapChannelManager.MapLoaded` loads its inventory again. Appending slots left
old occupied slots in place, causing the new duplicate-row protection to skip
every existing item. Initialization now retires each previous inventory item
entity once, clears the four fixed slot lists, and republishes the persisted
items. The selected weapon's duplicate equipped/drawer reference is cleaned
once. Existing in-memory jam and camera-profile values are retained for the
same item; persisted ammo, stack, durability, color and ownership are reloaded.

This re-publication has direct client evidence: original
`client.inputstate.wonkavator.OnEnterState` calls `gamemap.ClearMap`, which
calls `entitymanager.RemoveAllPhysicalEntities`. Raw method disassemblies,
original modules, and their hashes/UTC timestamps are retained under
`/home/blizz/backups/rasa-net/research/20260912-client-artifacts/world-equipment-selected/`,
in `client-inputstate-wonkavator-OnEnterState.raw-dis.txt`,
`client-gamemap-ClearMap.raw-dis.txt`,
`client-entitymanager-RemoveAllPhysicalEntities.raw-dis.txt`, and
`equipment-consumer-raw-manifest.json`. Acquired modules were only inspected
statically.

Before initial actor publication, the selected weapon also supplies its class
and primary color to appearance. An existing second hue is preserved; when
absent the existing appearance loader fallback `2139062144` remains an
explicit unverified value. No appearance row is written during this repair.
An empty or invalid selected slot clears the weapon appearance class and
in-memory readiness; a nonempty selection preserves readiness.

The final independent focused run passes **18 InventorySessionTests**. New
coverage initializes the same session twice and again after removal of a
stored inventory row, checks fixed capacities, unique cleanup, publication
order, absence of orphan item objects, retained instance state and selection,
and checks stale appearance for nonempty, empty and invalid selected slots.
It verifies that reconciliation does not write appearance rows. The log is
`world-equipment-selected/inventory-review-final-tests.log`, using a copied
workspace in `rasa_net:latest` with networking disabled. This complements the
earlier 44-test inventory/reload run; it does not replace final combined
candidate validation.

One pre-existing continuation is `ReduceStackCount`: it compares character
ownership to the account roster slot, and its full-stack removal hardcodes
personal inventory ownership/type instead of the actual home/personal
location. This review did not change that destruction path.

## Generic item requirement key correction

The companion [world equipment audit](world-equipment-client-audit.md) proves
that all 5,293 existing `itemtemplate_requirement` rows exactly match the
original **class-keyed** `itemclass.reqData`. `ItemManager.LoadItemTemplates`
previously treated their IDs as item template IDs. That incorrect join changed
the resulting requirements for 19,576 of 30,225 mapped templates in the
audited database; the retained comparison enumerates every affected template.

The decisive original consumer is `client/augmentations/item.pyo::Item.__init__`
(method begins at original line 54). Original source line 61, bytecode offsets
79–100, calls `_itemclass.reqData.get(self.classId)` and stores the result as
the item's requirements. The raw instructions are retained in
`/home/blizz/backups/rasa-net/research/20260912-client-artifacts/world-equipment-selected/client-augmentations-item-Item.__init__.raw-dis.txt`.
The original `generated/client/itemclass.pyo` SHA-256 is
`cd0e4367ff60ede526ef5429e3fdbf73f99873a15bcb4c426fed28e7bbc13ed9`.
That directory's `requirements-comparison.json`, `audit-requirements.py`, and
`snapshot-provenance.json` record the complete comparison against the immutable
world snapshot, whose SHA-256 is
`a70c12eebea8675955e01c8e5ed2d09a137db3f929c28bae608152e92685514a`.

The loader now groups templates by `template.Class` and applies each generic
requirement to every template mapped to that class. A template whose numeric
ID happens to equal the requirement's class ID does not receive it unless its
class also matches. Skill and race requirements retain their independently
verified **template-keyed** lookup. No requirement values or database rows are
changed, and no migration is required.

`ItemRequirementLoadingTests` exercises the actual complete template loader
with controlled repository records. Original class 6048 maps template 145 to
a level-one requirement; a synthetic second template for that same class tests
the one-to-many join, while a synthetic template numbered 6048 with another
class tests the numeric collision. A second test ensures race and skill
requirements remain specific to their template. Synthetic controls are test
fixtures, not imported game content. The exact original records used by the
regression are retained in this pass's
`requirements-regression-original-records.json`.
Both loader tests pass in an isolated source copy using the same cached SDK
image with networking disabled. The retained log is
`item-requirements-tests.log`; the TRX is
`test-results/item-requirements.trx`. No live database was used or changed.

This corrects the source of generic requirements. It does not establish that
every equipment eligibility rule, durability check, or server-side use check
is already implemented.
