# Equipment eligibility in the February client

Audit date: 12 September 2026. Target: 1:1 preservation of the final live
service. The recovered original client supplies exact equipment predicates,
class-to-slot checks, and equip request arguments. Original server validation,
binding persistence, and failure-message ordering still require separate
evidence.

## Provenance and reproduction

The source is the recovered package with embedded version **1.16.5.0** and
Python 2.4 modules compiled **10 February 2009 UTC**. Acquisition, hashes, and
the missing independent official checksum are recorded in
[client-artifacts.md](client-artifacts.md). This is versioned client evidence;
it does not establish every final server hotfix.

Static research is retained outside the repository at:

`/home/blizz/backups/rasa-net/research/20260912-client-artifacts/equipment-eligibility/`

`extract-evidence.py` reads selected members from the original `trpython.zip`
and uses `xdis` to disassemble code objects. `source-manifest.json` records each
member hash, timestamp, original source filename, method first line, arguments,
and disassembly filename. No acquired module was executed or imported.
`read-literal-data.py` uses the locally authored strict literal decoder to read
generated inventory/equipment tables from `data/game.zip`; it rejects imports,
calls, and branches. Its manifest retains original collection-assignment
offsets. Reproduce with:

```sh
/tmp/rasa-bytecode-tools/bin/python /home/blizz/backups/rasa-net/research/20260912-client-artifacts/equipment-eligibility/extract-evidence.py
/tmp/rasa-bytecode-tools/bin/python /home/blizz/backups/rasa-net/research/20260912-client-artifacts/equipment-eligibility/read-literal-data.py
```

| Original member | SHA-256 |
| --- | --- |
| `trpython.zip:client/augmentations/item.pyo` | `1b5995e918b08c7ca2872d07c80ce55686f4f9057da94af636db4c6f27eb8691` |
| `trpython.zip:client/augmentations/equipable.pyo` | `0f3fec34caf3da49b9364110a729b0bb66f762568cab881636f3b0b954abe3b6` |
| `trpython.zip:client/augmentations/actor.pyo` | `2ecb129d5071ac588e076415857ea4e5b3ebae08a964e2828b885089ac316c4f` |
| `trpython.zip:client/inventory.pyo` | `fea5b3bc09103b261cdccd8b9396b0cef0e93c0b484fcb1039655da57990bd70` |
| `trpython.zip:client/ui/attributeswindow.pyo` | `23610f22dd8fafeab21f8b490f2897dd5b2a0186027a942e00d44f5a7166fde6` |
| `trpython.zip:client/ui/weapondrawerwindow.pyo` | `0c05631490865de259ae9b43cb1723e7faf7b83ee50ed720e0b31e7054616933` |
| `data/game.zip:generated/client/equipmentdata.pyo` | `75c2caf08e42fea264dbf521e11350dee44faf369bfdeae035fbb3bc3d5afed8` |
| `data/game.zip:generated/client/inventorydata.pyo` | `43d60915efa08ee62ee826b3cea3fb7d123b878ed4f65854b0cdb7ca869ec8db` |

The adjacent `item-condition-selected/source-manifest.json` records the strict
enum audit. Original `constant/reqtype.pyo` hash is
`11f4a7c3f5bb8c0c21b0cfefc2b4ce00af3c77d776f9da70a8209a677b3aeec2`;
`constant/race.pyo` hash is
`15c6905c1c9159cc712ef5cbf1641318ac154986e4199fff5e851ec578121d1a`.
Both come from `data/game.zip:generated/client/constant/`. The requirements
enum is minimum level 1, Body 2, Mind 3, Spirit 4, maximum level 5. Races are
Human 1, Forean Hybrid 2, Brann Hybrid 3, Thrax Hybrid 4.

## Exact predicates

Offsets below are local to the named original method, not file byte offsets.
They were checked against raw instructions, including branch destinations.

| Consumer | Proven behavior and raw offsets |
| --- | --- |
| `Item.__init__`, first line 54 | Instructions 79–100 load generic `reqData` by **entity class ID**. Template skill and race requirements use item template IDs instead. The complete database mapping comparison is in [world-equipment-client-audit.md](world-equipment-client-audit.md). |
| `Item.CanActorUse`, first line 267 | Minimum level rejects `GetExperienceLevel() < requirement` at 48–70; maximum rejects `> requirement` at 95–117. Both bounds are inclusive. |
| Same method | Body at 142–167, Mind at 192–217, and Spirit at 242–267 compare **GetAttributeCurrent**, rejecting only when current value is below the requirement. Unknown requirement types fall through the loop. |
| `Actor.GetAttributeCurrent`, first line 915 | Returns the attribute's `.current` at 31–37, or zero if absent at 42–45. Neither normal maximum nor current maximum is substituted. |
| `Item.CanActorUse` | At 284–305, reads `itemTemplateRaceRequirement.get(itemTemplateId, None)`. At 308–343, a non-`None` requirement rejects a different `actor.GetRaceId()`. |
| `Equipable.CanActorEquip`, first line 88 | Calls `CanActorUse` at 3–22. It then reads `itemTemplateSkillRequirement.get(GetItemTemplateId(), (None,None))` at 69–99. The skill gate applies only if both values are non-`None` and the minimum is **greater than zero**, at 102–137. A lower actor rank rejects at 141–166; equality passes. |
| `Actor.GetSkillLevel`, first line 1801 | Reads `__skillDict.get(skillId, 0)` at 0–18. Absence means rank zero. The mere absence of a learned skill is not a rejection when no positive requirement applies. |
| `Equipable.CanActorEquip` | At 175–213, if `GetCondition` exists, rejects **condition == 0**. It does not reject all nonpositive values. |
| `Item.GetCondition`, first line 254 | If both hit-point values are non-`None` and maximum is positive, returns `100 * current / maximum` at 58–68. `BINARY_DIVIDE` at 67 uses Python 2 integer floor division when inputs are integers. Otherwise returns 100 at 73–76. There is no clamp. |

Consequently, current durability 1 out of maximum 200 produces condition zero
and fails this client predicate. A negative condition is not equal to zero;
changing the rejection to `<= 0` would alter the recovered rule. Zero or
missing maximum follows the fallback to 100. The server's item hit-point
state and packet fields must match this consumer; eligibility alone does not
reconstruct durability loss or repair.

The original `CanActorEquip` catches `AttributeError` around `CanActorUse` and
warns before continuing (31–68). That composition fallback is recorded as a
client quirk; it is not evidence that malformed server item definitions should
be accepted.

## Slots, requests, and client state

`attributeswindow.OnDNDEquipEnter`, first line 681, reads
`equipableClassEquipmentSlot.get(entity.classId, None)` at 160–181. It accepts
the hovered slot only if it equals that slot and `CanActorEquip` succeeds at
199–224. `Init`, first line 52, creates equipment icons at 1556–1814 for these
seven slots:

| Slot | ID |
| --- | ---: |
| Helmet | 1 |
| Shoes | 2 |
| Gloves | 3 |
| Torso | 15 |
| Legs | 16 |
| Eyewear | 19 |
| Mask | 21 |

Character-view drops also require the class's slot to occur among those icons
(`OnDNDCharacterViewDrop`, first line 834, offsets 145–222).
`weapondrawerwindow._IsEntityWeapon`, first line 749, instead requires the
class's equipment slot to equal `WEAPON` at 31–67. The original generated
constant is **13**, at module offsets 18–21. This UI predicate does not inspect
a separate weapon-info augmentation. There is no recovered normal armor-UI
route to slot 13. Other generated slots include appearance and mech slots;
their complete presentation and capacities remain separate. The generic
`Equipable.InventoryUse`, first line 48, routes slot 13 through the requested
weapon drawer slot at 30–94, and all other non-`None` generated slots to
`AddItemToEquipmentInventory` at 98–136. Thus the seven visible icons are not
a complete server slot whitelist; matching the item's generated slot remains
the common rule.

`inventory.AddItemToWeaponDrawer` checks `CanActorEquip` at 185–245 before
calling `_AddItemToWeaponDrawer`. Both `_AddItemToWeaponDrawer`, first line
1781, and `AddItemToEquipmentInventory`, first line 767, reject an absent or
dead avatar at 15–89. Both reject moving an item onto its current same-inventory
slot, and find the entity's existing inventory location before sending.
The UI drop methods also reject `gameui.IsUILockedDown`, displaying the
shapeshift message. Exact server transformation permissions remain separate.

`inventory._SendServerRequest`, first line 1638, proves both equip calls have
exactly this **three-element tuple**, in both movement directions:

```text
RequestEquipArmor(non-equipped slot, non-equipped inventory type, equipment slot)
RequestEquipWeapon(non-equipped slot, non-equipped inventory type, drawer slot)
```

For armor, Personal sends at 387–408 and 546–567, Home at 433–454 and 592–613,
and Clan at 479–500 and 638–659. For weapons, the equivalent ranges are 26–47
and 228–249, 72–93 and 274–295, and 118–139 and 320–341. Thus the call describes
a swap, including unequipping into the specified inventory. Clan is a real
original route, even where the emulator lacks its storage and permission
implementation. Drawer-to-drawer movement is a separate two-element
`WeaponDrawerInventory_MoveItem` call at 164–182.

Original inventory IDs are Personal 1, Home 2, Equipped 8, Weapon Drawer 9,
and Clan 15. Generated `categoryInfo[EQUIPMENT][0]` is 50;
`client.inventory` initializes that category's start to zero at 472–484, so
its personal slots are 0–49. Original `shared.gameconstants` gives Home
capacity 480 at 549–552 and five drawer slots at 652–655. Its
`NUM_EQUIPMENT_SLOTS=9` is not the maximum ID of the sparse equipment slots.
That raw module is retained in `combat-selected/shared-gameconstants.raw-dis.txt`.

Normal armor and drawer icon drops check bind-on-equip and bound state before
showing `DisplayItemBindConfirmation`. Character-view armor drop lacks this
confirmation path. The confirmed action remains the equip request; there is no
additional permission bit in its tuple. Binding the persisted item is still a
server responsibility requiring reconstruction.

`Recv_InventoryMoveFailed`, first line 292, accepts `(type, fromSlot, toSlot)`
but contains only `LOAD_CONST None` at 0 and `RETURN_VALUE` at 3. It does not
release a pending queue or undo a predicted inventory change. The inspected
equip submission paths send calls and play UI sounds; they do not mutate the
inventory mapping. After submitting a drawer swap, `_AddItemToWeaponDrawer`
calls `actor.SendRequestedWeaponDrawerSlot` at 284–290. That helper sends
`RequestArmWeapon(requestedSlot)` only when forced or different from the
current armed slot. Equip rejection and arm-selection failure are therefore
distinct contracts; the latter has a separate `Recv_ArmWeaponFailed` receiver.

For Home placement, `Item.IsPlaceableInLockbox`, first line 387, returns its
received boolean. `inventorywindow.OnSlotEntered` at 619–641 checks this flag
and whether a lockbox is open before displaying the Add Lockbox tooltip/cursor.
A bounded byte-string search across original client Python members found this
flag name only in the item module and this inventory UI consumer; results are
retained in `lockbox-flag-symbol-members.json`. The inspected equip submission
and `_SendServerRequest` methods do not check the outgoing swap item's flag or
bound state. The flag establishes original placement eligibility, but applying
it to the outgoing half of a Home equip swap is an inference about original
server validation. Bound-to-character alone is not proven equivalent to
forbidden Home placement. Exact lockbox-session and remote-access admission
must also be reconstructed independently of the three-element swap tuple.

## Clan prerequisite and remaining limits

The bounded clan trace is also in `source-manifest.json`.
`client/clan.pyo` SHA-256 is
`2046d2fbc9c5056d8b950953fe4683f357584177ae8966bcb3b437aa21fbbf09`.
`CanClanWithdrawFromLockbox`, first line 203, returns false for a missing member
at 0–16; otherwise it returns
`member.rank >= CLAN_RANK_TO_WITHDRAW_FROM_LOCKBOX` at 21–36. Original shared
constants define `CLAN_RANK_3=2` at 1224–1227 and assign that withdrawal
threshold at 1260–1263. The clan window uses this predicate to display full or
restricted access. Its inspected item drag methods do not enforce that
predicate themselves. Opening the UI follows the lockbox's `Recv_Use` for the
current manifestation, not an arbitrary source inventory number.

Exact session ownership, distance checks, clan membership changes, tab access,
and original failure ordering need the full lockbox implementation. They
cannot be inferred solely from the equip tuple. Account/character ownership
and atomic persistence guards in the emulator are necessary integrity checks;
their database transaction design is not a claim about the original server's
internal implementation. Likewise, existing English `SystemMessage` rejection
text is not proven original localization or packet ordering.

This audit establishes the client predicates and request contracts with high
confidence. It does not establish complete armor stat contributions, temporary
attribute/modifier behavior, automatic unequip when requirements later change,
durability loss/repair, binding persistence, all mech/appearance routes, or all
original storage permissions. Those remain preservation gaps.
