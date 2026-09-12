using System.IO;

namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;

    // Original charactercreation.OnCreateCharacter sends this six-field form
    // before the account has a family name. No destination slot is transmitted.
    public class CreateCharacterPacket : RequestCreateCharacterInSlotPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CreateCharacter;

        public CreateCharacterPacket() => SlotNum = 1;

        public override void Read(PythonReader pr)
        {
            if (pr.ReadTuple() != 6)
                throw new InvalidDataException("Initial character creation requires six fields.");
            ReadCharacterDetails(pr);
        }
    }
}
