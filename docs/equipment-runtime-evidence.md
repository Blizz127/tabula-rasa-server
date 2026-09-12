# Equipment admission and conserved swaps

Research/implementation date: 2026-09-12. Baseline: `21d7d40`.
The target is the original final live game. This integration follows the
[original eligibility consumers](equipment-eligibility-client-evidence.md),
[item condition and race audit](item-condition-client-evidence.md), and
[original class-keyed requirement data](world-equipment-client-audit.md).
Their source hashes, raw bytecode locations and reproduction scripts establish
the client contract. Original shutdown manifest and server-side policies remain
separate fidelity requirements.

## Rules implemented from original consumers

`EquipmentRequirements.Check` uses inclusive minimum/maximum level boundaries,
current Body/Mind/Spirit values, template race equality, and learned skill rank
only when the template minimum is positive. Missing skill or attribute values
follow the original zero-valued getters. A zero skill minimum does not require
an otherwise absent learned skill. No additional requirement rows are invented.

Condition follows the integer fields this server sends in ItemInfo. It is
`floor(100 * currentHP / maximumHP)` for positive maximum and specified current;
missing values or nonpositive maximum yield 100. The equipment predicate rejects
exactly zero, preserving the original negative and above-maximum quirks. For
example, 1/200 HP is zero condition, 2/200 is one, and -1/200 is minus one.
Wide integer arithmetic avoids host overflow without clamping original values.
This does not establish original wear, repair costs, or every historical field's
wire type; original floating fields would use true division instead.

The two equipment decoders enforce the original three-field tuple:
`(non-equipped slot, non-equipped inventory type, equipment/drawer slot)`.
The same request handles equipping, unequipping and swapping. The packet retains
Personal, Home and Clan type identifiers; the implemented routes below cover
Personal and Home. Both handlers require a living avatar, consistent with the
original client submission checks. Slots are bounded before indexing.

An incoming item must match its generated equipment slot. The normal equipment
endpoint cannot write weapon slot 13, which belongs to the selected drawer
reference. Drawer weapons are identified by generated slot 13 itself, rather
than the presence of a particular server template-stat record. `InventoryUse`
dispatches other generated slots to equipment, so the seven visible armor icons
are not imposed as a hard slot whitelist. The existing manifestation list still
has 22 positions; complete mech equipment requires its own representation and
lifecycle. An empty incoming slot permits removal without checking the outgoing
item's condition, level or skill requirements.

## Personal and home persistence

Previously equipment swaps published removals and moved items one at a time
with unconditional database writes. A failure during the second move could
leave duplicated locations or memory inconsistent with saved inventory.

The handlers now capture both slot instances, validate exact registration,
ownership, stack existence and expected source slots, then call
`CharacterInventoryRepository.TrySwapItems`. The repository checks both expected
locations under a serializable transaction, rejects duplicate/changed occupancy,
and verifies the linked item rows exist with positive stacks. Each update
rechecks its item/account/owner/type/slot. Both rows commit before any in-memory
move or successful inventory notification. Failure of the second write rolls
back the first. Empty source or destination slots are checked as expected empty,
not treated as permission to overwrite another stored item.

Personal/equipped/drawer ownership uses the character ID, never its roster slot.
Home ownership uses the authenticated account and character owner zero. A direct
Home-to-drawer swap therefore moves the incoming item's owner from zero to the
character and the outgoing item's owner back to zero in the same transaction.
Empty Home slots also accept unequipped items. Equipment eligibility uses the
original generated class slot, independent of the home slot's numeric index.

After commit, ordered inventory remove/add notifications update both slots.
Only a change to the active weapon refreshes equipped slot 13, appearance,
readiness and magazine state and cancels work captured against the old weapon.
A failed swap preserves the prior selected weapon and pending reload. Initial
actor data now includes the original `RaceId` packet before control/equipment,
so the client can apply its own race predicate instead of comparing against
its initial unknown race.

The existing system-message transport reports server eligibility failures;
its English text is not claimed as original localized retail messaging.
Original `InventoryMoveFailed` is a no-op in the recovered client and supplies
no pending-action cleanup contract to reproduce here. Inventory location
transactions are emulator consistency repairs, not claims about the original
server's SQL schema or exact transaction boundaries.

## Verification and remaining scope

The controlled SQLite session tests drive the actual armor and weapon handlers,
reopen database contexts, and inspect queued notifications. They cover failed
requirements, dead-avatar admission, mismatched class slots, index boundaries,
full weapon swaps, removal of broken armor, Home owner transitions, shared-home
visibility to another character, changed account/owner/location, duplicate
occupancy, and a trigger that aborts the second database move. That forced
failure proves no first move, slot mutation, cancellation or success packet
survives a failed transaction. Pure tests verify condition quirks, inclusive
bounds, current attributes, skill minima, and request arity/field preservation.
Initial actor-data tests verify race precedes control and equipment.

An initial isolated combined run passed 466 tests; subsequent final-review
regressions cover malformed field types and failed appearance persistence. The final built-image validation,
backup and deployment record is in [retail-accuracy.md](retail-accuracy.md).
These tests establish implementation properties on SQLite; original-client
sessions, final-state configuration and full gameplay remain unverified.

Clan equipment routes are present in the original protocol, but the current
clan session/storage path and server permissions are incomplete. The original
client declares withdrawal rank at least 2, which is recorded for reconstruction.
Home proximity/access rules, cross-session home update broadcasts, lockbox item
placement policies, binding and uniqueness persistence, inventory operations
outside these equipment/destruction paths, and complete mech slots remain gaps.
The original no-lockbox flag is shown in general placement UI; no explicit
outgoing-item check was recovered in the equipment swap branch, and a new server
swap policy is not guessed from that absence. Appearance persistence follows the
inventory transaction separately. A failed appearance query/save is logged but
does not prevent committed equipment and stats from refreshing; a forced
appearance-table failure is covered by regression. Retrying the failed
cosmetic save, reconciliation of non-weapon appearance on later login, and its
full original second-hue contract remain necessary work.
