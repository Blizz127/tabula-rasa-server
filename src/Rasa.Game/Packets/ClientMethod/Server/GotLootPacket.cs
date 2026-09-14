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

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            if (Loot == null)
            {
                pw.WriteULong(_creatureEntityId);
                pw.WriteList(0);
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
