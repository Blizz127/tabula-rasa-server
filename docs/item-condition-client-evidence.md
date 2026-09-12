# Item condition, race and item-state packets

This 12 September 2026 audit targets the original final-live game. It proves
the client condition calculation, race/requirement identifiers and several
packet consumers. It corrects the trade-flag interpretation across the loader
and packets and supplies the missing race-state packet. Item wear, repair
costs and server durability formulas remain separate evidence gaps.

## Provenance and reproducibility

The package identifies itself as client **1.16.5.0**; provenance and the missing
independent official checksum are in [client-artifacts.md](client-artifacts.md).
The modules used here were compiled on **10 February 2009 UTC**. Decompiled
comments display the previous evening in the host timezone. All inspection
was static; acquired modules were not executed or imported.

Raw originals, method disassembly, manifests, scripts and test logs are in:

`/home/blizz/backups/rasa-net/research/20260912-client-artifacts/item-condition-selected/`

`source-manifest.json` and `method-manifest.json` identify exact source paths,
hashes, UTC compilation times, original method lines and code flags.
`extract-methods.py` regenerates selected disassembly using xdis.
`audit-types.py` compares generated literals against the read-only world
snapshot from [world-equipment-client-audit.md](world-equipment-client-audit.md).
Its hash is `a70c12eebea8675955e01c8e5ed2d09a137db3f929c28bae608152e92685514a`.

| Original archive member | SHA-256 |
| --- | --- |
| `trpython.zip:client/augmentations/item.pyo` | `1b5995e918b08c7ca2872d07c80ce55686f4f9057da94af636db4c6f27eb8691` |
| `trpython.zip:client/augmentations/armor.pyo` | `f5913c6a6599ee87c7693cd608e630cd09d3f2d36ec20aca5e6b1c7e75865ed6` |
| `trpython.zip:client/augmentations/equipable.pyo` | `0f3fec34caf3da49b9364110a729b0bb66f762568cab881636f3b0b954abe3b6` |
| `trpython.zip:client/augmentations/manifestation.pyo` | `724f68eb70e5a47338db82977c55abfa09f1b187f86a0b9a560c012a8588886d` |
| `data/game.zip:generated/client/constant/race.pyo` | `15c6905c1c9159cc712ef5cbf1641318ac154986e4199fff5e851ec578121d1a` |
| `data/game.zip:generated/client/constant/reqtype.pyo` | `11f4a7c3f5bb8c0c21b0cfefc2b4ce00af3c77d776f9da70a8209a677b3aeec2` |
| `data/game.zip:generated/client/methodid.pyo` | `cc75aa76099de1bf4ea924bf68a4d8277fb3ac1e497ae5127b4e7ee7147412d8` |

Generated constants were decoded with the literal-only reader retained in
`river-recon-selected/decode-literal-tables.py`. `enum-literals.json` and
`method-id-literal-proof.json` retain the exact rows and bytecode assignment
offsets. Original method IDs are `ArmorInfo=406`, `ItemInfo=469`,
`ItemStatus=470`, and `RaceId=760`; these match the repository enum.

## Condition uses received item hit points

`Item.__init__` initializes maximum hit points from `itemclass.lookup[classId]`
slot 4 and current hit points to `None`. `Recv_ItemInfo`, original line 187,
and `Recv_ItemStatus`, line 212, replace both fields directly with their
received arguments. They do not clamp or convert the values.

`Item.GetCondition`, original line 254, bytecode offsets 18–76, is exactly:

```text
if maximum is not None and current is not None and maximum > 0:
    return 100 * current / maximum
return 100
```

The arithmetic is `LOAD_CONST 100`, `BINARY_MULTIPLY`, `BINARY_DIVIDE`
(offsets 57, 63, 67). Its code flags are 67, with no future-division flag.
For integer arguments, Python 2 classic division rounds toward negative
infinity; floating arguments use true division. This distinction is specified
by [Python PEP 238](https://peps.python.org/pep-0238/). Current server packets
write integer hit points, so the integer branch is the relevant comparison
for those emitted values. An original server capture would still be needed
to establish every historical field's actual wire type.

| Current / maximum, integer inputs | Condition |
| --- | ---: |
| 0 / 160 | 0 |
| 1 / 200 | 0 |
| 2 / 200 | 1 |
| -1 / 200 | -1 |
| 240 / 160 | 150 |
| Any current / maximum 0 or negative | 100 |
| Either argument `None` | 100 |

`Equipable.CanActorEquip`, original line 88, rejects when `GetCondition() == 0`
(bytecode 175–213). It does not use `<= 0` or simply test current hit points.
The negative and above-maximum cases are recorded faithfully without adding
a clamp. The original weapon icon warning uses a different `<= 0` comparison;
an icon rule must not replace the equipment predicate. Full use/equip
predicates are recorded in
[equipment-eligibility-client-evidence.md](equipment-eligibility-client-evidence.md).

`GetCurrentHitPoints`, original line 234, returns current hit points if maximum
is `None` or current is already known; otherwise it returns zero.
`GetCondition` uses the private fields directly, so its missing-current
fallback remains 100 even when `GetCurrentHitPoints` would report zero.

`Recv_ItemStatus` also notifies the weapon drawer when hit points cross zero,
and posts the item-repaired event when hit points increase to exactly the
maximum. `Recv_ItemInfo` posts the general item-info event instead. The current
vendor repair path sends full item data; it does not emit the dedicated
`ItemStatus` update. This audit does not invent the original repair cost,
server update ordering, or wear schedule.

`Armor.Recv_ArmorInfo`, original line 50, accepts `(currentHitPoints,
maxHitPoints)` but consists only of `LOAD_CONST None; RETURN_VALUE`.
The existing two-field packet has the right argument shape, but this final
client consumer does not update item condition from it. Armor damage
absorption fields are read from `armorclass`; their relationship to actor
armor pools, wear and item condition must be established separately.

## All-row numeric and absence checks

`persisted-type-audit.json` records every mismatch and aggregate type count.
The original 9,115 item classes have 6,852 positive integer maximum-HP values
and 2,263 `None` values. The database matches every positive number and
imports those 2,263 absences as zero; the original table has no numeric zero.
For `GetCondition`, both maximum zero and maximum `None` return 100, but they
remain distinct in other consumers and must not be conflated generally.
No nullable-model migration or guessed server hit-point values were added.

All 5,293 generic requirement rows use integer class IDs, integer requirement
type **1**, and integer values. The original enum also defines Body=2,
Mind=3, Spirit=4 and maximum XP level=5. Those enum numbers match the current
repository, although the retained generic table contains no rows for 2–5.
The class-keyed loader correction is covered by the world equipment audit;
numeric constants alone do not imply that missing requirement rows should be
invented.

## Race identifiers and the missing initialization packet

The original race constants are Human=1, Forean hybrid=2, Brann hybrid=3 and
Thrax hybrid=4, matching the repository `Race` values. The 70 original
template race requirements are integers: 61 require Human and three require
each hybrid. The database matches every row. The separate read-only aggregate
`active-character-race-counts.json` confirms the current stored race value is
also within that original mapping; it includes no character or account names.

`Manifestation.__init__`, original line 136, initializes race to `None`.
`Recv_RaceId`, method line 1731, assigns its single argument at offsets 0–6;
`GetRaceId`, line 1739, returns that field. `Item.CanActorUse` compares the
template requirement to that returned race. Before this correction the
server declared opcode 760 but never sent the race-state method, leaving the
client's race-specific equipment check comparing a valid requirement to
`None`.

`RaceIdPacket` now writes the original one-argument integer tuple, and initial
actor data publishes the character's stored race. This establishes the
client's state for the existing check; it does not change race unlocks,
character-creation availability, or any race requirement rows.

## Trade flag correction and queued item state

`Item.Recv_ItemInfo` receives 15 fields, with the **negative** `notTradable`
flag at position 13 (one-based). At original line 205, offsets 108–115,
`UNARY_NOT` sets the positive internal tradable state. The item tooltip's
item augmentation likewise uses a negative not-tradable field.

The old loader set the positively named `ItemInfo.Tradable` directly from
`NotTradableFlag != 0`. The old `ItemInfoPacket` then wrote it directly, which
accidentally preserved the database's negative flag on that wire path.
The tooltip negated the already inverted property and therefore disagreed.
The repair changes both ends together: the loader maps
`Tradable = NotTradableFlag == 0`, and the packet writes `!Tradable`.
This preserves the correct negative wire flag for database-loaded item state,
corrects the tooltip and in-memory semantics, and changes no stored flags.
Only 4,985 item template flag rows exist for 30,225 mapped templates. For
templates without a flag row, positive `Tradable` now defaults to true so
`ItemInfo` retains its prior negative wire default of false. This preserves
an existing unverified placeholder; it does not prove those templates were
tradable in the original game. The missing-row loader regression guards this
distinction from the corrected semantics of explicitly populated flags.

`ItemInfoPacket` also snapshots its current/maximum HP, crafter, template ID,
flags, quality and inventory category when queued. A later repair or template
mutation cannot rewrite an earlier item-state message. Class/loot module
lists remain the existing empty lists; this does not claim original module
instances have been recovered. Original `IsTradable` additionally rejects
equipped items and open hands, so the packet flag alone is not the full
runtime trade predicate.

## Validation and remaining scope

Focused tests cover both trade flag values through the actual template
loader, including a missing template flag row, and through both item-state/
tooltip serializers, all 15 queued item
fields after source mutation, and the four original race IDs and opcode.
All **11** focused tests pass. The final focused log is
`item-condition-selected/final-focused-tests-03.log`. The filter is
`FullyQualifiedName~ItemInfoPacketTests|FullyQualifiedName~ItemRequirementLoadingTests`,
run against a copied workspace in `rasa_net:latest` with networking disabled.
Source verification comes from the original consumers; passing tests only
verify the reconstruction and its integration.

No original game code was run. No database rows, schema, deployment or commit
were changed by this audit. Original durability loss, repair economics,
module-modified maximum HP, field absence on the server wire, and full
trade/binding/uniqueness behavior remain evidence gaps.
