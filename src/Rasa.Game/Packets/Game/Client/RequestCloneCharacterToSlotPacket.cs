using System.Collections.Generic;
using System.IO;

namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;
    using Packets;
    using Structures;

    public class RequestCloneCharacterToSlotPacket : ClientPythonPacket
    {
        public const double MinHeight = 0.90000000000000002;
        public const double MaxHeight = 1.0600000000000001;

        public override GameOpcode Opcode { get; } = GameOpcode.RequestCloneCharacterToSlot;
        
        public byte CloneSlotNum { get; set; }
        public byte SlotNum { get; set; }
        public string CharacterName { get; set; }
        public byte Gender { get; set; }
        public double Scale { get; set; }
        public Race RaceId { get; set; }

        public Dictionary<EquipmentData, AppearanceData> AppearanceData { get; } = new Dictionary<EquipmentData, AppearanceData>();

        public override void Read(PythonReader pr)
        {
            // charactercreation.OnCreateCharacter sends exactly these seven fields.
            if (pr.ReadTuple() != 7)
                throw new InvalidDataException("Character cloning requires seven fields.");

            CloneSlotNum = checked((byte)pr.ReadInt());
            SlotNum = checked((byte)pr.ReadInt());
            CharacterName = pr.ReadUnicodeString();
            Gender = checked((byte)pr.ReadInt());
            Scale = pr.ReadDouble();

            var appearanceCount = pr.ReadDictionary();
            for (var i = 0; i < appearanceCount; i++)
            {
                var data = pr.ReadStruct<AppearanceData>();

                AppearanceData.Add(data.SlotId, data);
            }

            RaceId = (Race)pr.ReadInt();
        }

        public CreateCharacterResult Validate()
        {
            var nameResult = CharacterNameRules.Validate(CharacterName);
            if (nameResult != CreateCharacterResult.Success)
                return nameResult;

            if (!RequestCreateCharacterInSlotPacket.IsValidHeight(Scale))
                return CreateCharacterResult.InvalidCharacterHeight;

            if (RaceId < Race.Human || RaceId > Race.Thrax)
                return CreateCharacterResult.CharacterCreationInvalidRace;

            if (Gender > 1)
                return CreateCharacterResult.InvalidEncoding;

            return CreateCharacterResult.Success;
        }
    }
}
