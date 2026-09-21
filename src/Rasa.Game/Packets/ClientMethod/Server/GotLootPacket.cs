namespace Rasa.Packets.ClientMethod.Server
{
    using Data;
    using Memory;
    using Structures;

    public class GotLootPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.GotLoot;

        public LootDispenser Loot { get; set; }

        private readonly ulong _creatureEntityId;
        private readonly int _credits;
        private readonly uint _itemClassId;
        private readonly uint _itemQuantity;
        private readonly ulong _itemEntityId;

        public GotLootPacket(LootDispenser loot)
        {
            Loot = loot;
        }

        /// <summary>Credits only, for a creature's kill payout.</summary>
        public GotLootPacket(ulong creatureEntityId, int credits)
        {
            _creatureEntityId = creatureEntityId;
            _credits = credits;
        }

        /// <summary>
        /// One item, for a vendor purchase. The client's Recv_GotLoot reads the entity class id out of the list
        /// and asks its own language manager for the name (GetEntityClassName), which is why the item has to
        /// travel as a class id and not as the text of one.
        /// </summary>
        public GotLootPacket(ulong sourceEntityId, uint itemClassId, uint quantity, ulong itemEntityId)
        {
            _creatureEntityId = sourceEntityId;
            _itemClassId = itemClassId;
            _itemQuantity = quantity;
            _itemEntityId = itemEntityId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            if (Loot == null)
            {
                pw.WriteULong(_creatureEntityId);

                if (_itemClassId == 0)
                {
                    pw.WriteList(0);
                }
                else
                {
                    pw.WriteList(1);
                    pw.WriteTuple(3);
                    pw.WriteUInt(_itemClassId);
                    pw.WriteUInt(_itemQuantity);
                    pw.WriteULong(_itemEntityId);
                }

                pw.WriteInt(_credits);
                return;
            }

            pw.WriteULong(Loot.AttachedTo);
            pw.WriteList(Loot.LootItems.Count);
            foreach (var item in Loot.LootItems)
            {
                pw.WriteTuple(3);
                pw.WriteUInt(item.ItemClassId);
                pw.WriteUInt(item.ItemQuantity);
                pw.WriteULong(item.EntityId);
            }
            pw.WriteInt(Loot.Credits);
        }
    }
}
