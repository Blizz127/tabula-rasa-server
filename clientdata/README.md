# Client data

Small tables lifted verbatim from the client's own decoded data, for audits that need to check our world
against what the client believes.

## equipment-slots.csv

`generated/client/equipmentdata.equipableClassEquipmentSlot` from client 1.16.5.0 (`data/game.zip`, zip mtime
2009-02-09): the one equipment slot each of the 6,935 equipable classes belongs in. Columns are the class id and
the slot id, and the slot numbers are the client's own module constants, which `src/Rasa.Game/Data/EquipmentData.cs`
already reproduces (HELMET 1, SHOES 2, GLOVES 3, WEAPON 13, HAIR 14, TORSO 15, LEGS 16, FACE 17, WING 18,
EYEWEAR 19).

`AppearanceSlotAuditTests` reads it. The slot key is not bookkeeping: the client's `Actor._GetSkinColor` reads
the FACE key directly, `GetWeaponClassIdFromAppearanceData` reads WEAPON then HANDTOHAND, `_ProcessAppearanceData`
decides on accessory effects by key, and `Recv_AppearanceData` diffs by key. Only mesh placement survives a wrong
key, because `_ProcessSwapsets` re-derives the slot from this same table.
