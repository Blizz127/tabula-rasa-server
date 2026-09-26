# Persistent item modules and restriction overrides

This change supplies generic item-instance infrastructure. It assigns no
modules to boot-camp rewards and implements no module combat effects. The
filmed Shinobi configuration remains a separate reconstruction question; see
[its research record](bootcamp-shinobi-rifle.md) and subsequent module research.

The original compatibility client's `Item.Recv_ItemInfo` receives separate
ordered class-module and loot-module ID lists. `Item.GetName` passes the loot
list to `GetItemName`, whose strict priority comparison preserves the first
module when priorities tie. Consequently, an unordered set or a template-wide
module field cannot represent independent items faithfully. The same original
packet receives `hasSellableFlag` and inverse `notTradable` per item.
Exact source member hashes and offsets are recorded in
[the machine-readable record](evidence/item-instance-metadata.json).

Migration `20260922233000_ItemInstanceMetadata` adds three nullable columns to
`items` for both character-database providers: `loot_modules` as TEXT containing
an ordered JSON array, and `tradable_override` / `sellable_override` as nullable
booleans. JSON entries contain `ModuleId` and nullable `Level`. Null level
preserves an unknown server value; it is not a claim about original module
strength, which the client also derives from its crafting tables. JSON is an
emulator storage choice, not a recovered original database format.

Old rows receive null values. Null overrides inherit template flags, while
explicit true and false override them. No existing item is assigned a module,
and no global template is modified. Downgrading removes these three columns
and their contents while retaining the pre-existing item fields.

Creation copies supplied metadata before persistence. Character and shared
home inventory reconstruction restore it; clan inventory reconstruction also
restores it when materializing a stored item. Stack splitting copies the
persisted metadata, and personal/clan inventory merging refuses stacks whose
ordered modules or overrides differ. Partial vendor-sale copies retain the
same metadata. Vendor sales, auction creation, and player trade consult the
effective restrictions; trade checks them again before completion.

`ItemInfoPacket` snapshots effective flags and the ordered loot-module IDs.
It sends IDs alone in the original list field, without adding level tuples or
changing the original 15-field packet shape. Class modules remain a separate,
currently empty list. Module definition responses and combat application are
outside this change.

New tests cover queued-packet stability, same-template instance isolation,
creation and metadata updates, character reconnect, shared-storage splitting,
merge isolation, vendor-sale and final-trade restriction checks, and an actual
SQLite migrator upgrade/rollback retaining legacy item rows. Both provider
migration operations and frozen models are checked, but that does not claim
execution against a MySQL server.

The parent’s serial .NET 5 integration run passed **1,300/1,300 tests**, none
skipped, in **5.9650 minutes**. All nine new metadata test cases passed. The
actual SQLite upgrade/rollback test took 0.2593761 seconds in that full run.
The preceding targeted run passed 200 of 201 cases; its sole failure was SQL
formatting in the migration test fixture, where literal JSON braces were
interpreted as format syntax. After parameterizing that test value, the single
migration regression passed in 2.4635307 seconds (3.9671 seconds for the test
run), followed by the successful full suite. The earlier boss baseline is also
retained: one pass and two expected regression failures. It is separate from
the item-metadata validation.

All four logs, their TRX files, and the tested source snapshot are archived at
`/home/blizz/backups/rasa-net/research/20260922-client-playthrough/verification-modules-final/`.
The archive manifest SHA-256 is
`8e2a98c143e9d35540c210b66c3b290e3eac423e4300c786eae46b7a53f15b7f`;
the full-run log SHA-256 is
`6f3842e44ef5d84495f4aa50f12fd79f543f3ac0ba9a68f3ca347c0b3088df83`.
All **2,071 source files** match the tested snapshot. Two subsequent research
document changes were recorded separately during archiving, and this
verification update changes documentation only. MSTest’s raw TRX counters also
include aggregate data-driven parent results; the reported totals count leaf
results, matching the console summaries. Automated emulator tests do not prove
original-live fidelity. No production runtime or database was changed.
