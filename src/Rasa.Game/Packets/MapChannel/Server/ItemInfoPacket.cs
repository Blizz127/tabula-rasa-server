namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class ItemInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ItemInfo;

        private readonly int _currentHitPoints, _maxHitPoints, _qualityId, _inventoryCategory;
        private readonly string _crafter;
        private readonly uint _itemTemplateId;
        private readonly bool _hasSellableFlag, _hasCharacterUniqueFlag, _hasAccountUniqueFlag, _hasBoEFlag;
        private readonly bool _boundToCharacter, _notTradable, _notPlaceableInLockbox;

        public ItemInfoPacket(Item item, EntityClass classInfo)
        {
            var template = item.ItemTemplate;
            _currentHitPoints = item.CurrentHitPoints;
            _maxHitPoints = classInfo.ItemClassInfo.MaxHitPoints;
            _crafter = item.Crafter;
            _itemTemplateId = template.ItemTemplateId;
            _hasSellableFlag = template.HasSellableFlag;
            _hasCharacterUniqueFlag = template.HasCharacterUniqueFlag;
            _hasAccountUniqueFlag = template.HasAccountUniqueFlag;
            _hasBoEFlag = template.HasBoEFlag;
            _qualityId = template.QualityId;
            _boundToCharacter = template.BoundToCharacter;
            // Original Item.Recv_ItemInfo receives notTradable and negates it.
            _notTradable = !template.ItemInfo.Tradable;
            _notPlaceableInLockbox = template.NotPlaceableInLockbox;
            _inventoryCategory = (int)template.InventoryCategory;
        }
                
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(15);
            pw.WriteInt(_currentHitPoints);
            pw.WriteInt(_maxHitPoints);
            if (!string.IsNullOrEmpty(_crafter))
                pw.WriteString(_crafter);
            else
                pw.WriteNoneStruct();
            pw.WriteUInt(_itemTemplateId);
            pw.WriteBool(_hasSellableFlag);
            pw.WriteBool(_hasCharacterUniqueFlag);
            pw.WriteBool(_hasAccountUniqueFlag);
            pw.WriteBool(_hasBoEFlag);
            pw.WriteList(0);                    // 'classModuleIds'         // ToDo
            pw.WriteList(0);                    // 'lootModuleIds'          // ToDo
            pw.WriteInt(_qualityId);
            pw.WriteBool(_boundToCharacter);
            pw.WriteBool(_notTradable);
            pw.WriteBool(_notPlaceableInLockbox);
            pw.WriteInt(_inventoryCategory);
        }
    }
}
