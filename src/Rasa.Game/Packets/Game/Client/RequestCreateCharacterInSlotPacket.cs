using System.Collections.Generic;
using System.IO;

namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;
    using Packets;
    using Structures;

    public class RequestCreateCharacterInSlotPacket : ClientPythonPacket
    {
        public const double MinHeight = 0.90000000000000002;
        public const double MaxHeight = 1.0600000000000001;

        public override GameOpcode Opcode { get; } = GameOpcode.RequestCreateCharacterInSlot;

        public byte SlotNum { get; set; }
        public string FamilyName { get; set; }
        public string CharacterName { get; set; }
        public byte Gender { get; set; }
        public double Scale { get; set; }
        public Race RaceId { get; set; }

        public Dictionary<EquipmentData, AppearanceData> AppearanceData { get; } = new Dictionary<EquipmentData, AppearanceData>();

        public override void Read(PythonReader pr)
        {
            if (pr.ReadTuple() != 7)
                throw new InvalidDataException("Character-slot creation requires seven fields.");

            SlotNum = checked((byte)pr.ReadInt());
            ReadCharacterDetails(pr);
        }

        protected void ReadCharacterDetails(PythonReader pr)
        {
            FamilyName = pr.ReadUnicodeString();
            CharacterName = pr.ReadUnicodeString();
            Gender = checked((byte)pr.ReadInt());
            Scale = pr.ReadDouble();

            var appearanceCount = pr.ReadDictionary();
            for (var i = 0; i < appearanceCount; i++)
            {
                var data = pr.ReadStruct<AppearanceData>();

                AppearanceData.Add(data.SlotId, data);
            }

            RaceId = (Race) pr.ReadInt();
        }

        public CreateCharacterResult Validate()
        {
            // ReadUnicodeString answers a Python None with null; the length checks below used
            // to dereference it and disconnect the client at the character screen.
            if (CharacterName == null || FamilyName == null)
                return CreateCharacterResult.InvalidEncoding;

            var characterName = CharacterNameRules.Validate(CharacterName);

            if (characterName != CreateCharacterResult.Success)
                return characterName;

            // The family name was never checked: empty, over-long or any characters at all
            // went into account.family_name as sent, and it is shown to every other player.
            var familyName = CharacterNameRules.Validate(FamilyName);

            if (familyName != CreateCharacterResult.Success)
                return familyName;

            if (!IsValidHeight(Scale))
                return CreateCharacterResult.InvalidCharacterHeight;

            if (RaceId < Race.Human || RaceId > Race.Thrax)
                return CreateCharacterResult.CharacterCreationInvalidRace;

            // Male or female; nothing the client offers sends anything else.
            if (Gender > 1)
                return CreateCharacterResult.InvalidEncoding;

            return CreateCharacterResult.Success;
        }

        internal static bool IsValidHeight(double scale)
        {
            // The protocol also carries floats (tag 0x3F). Its representation of the
            // client's minimum 0.9 is slightly below the double constant.
            return !double.IsNaN(scale) && (scale >= MinHeight || scale == (double)(float)MinHeight) && scale <= MaxHeight;
        }

    }
}
