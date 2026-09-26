using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Packets.Game.Client;

namespace Rasa.Test
{
    [TestClass]
    public class CharacterNameTests
    {
        [DataTestMethod]
        [DataRow(null, CreateCharacterResult.InvalidEncoding)]
        [DataRow("", CreateCharacterResult.NameTooShort)]
        [DataRow("Ab", CreateCharacterResult.NameTooShort)]
        [DataRow("Abcdefghijklmnopqrstuv", CreateCharacterResult.NameTooLong)]
        [DataRow("alice", CreateCharacterResult.NameFormatInvalid)]
        [DataRow("Ab1", CreateCharacterResult.NameFormatInvalid)]
        [DataRow("Ab_", CreateCharacterResult.NameFormatInvalid)]
        [DataRow("Ab\n", CreateCharacterResult.NameFormatInvalid)]
        [DataRow("Abc\n", CreateCharacterResult.NameFormatInvalid)]
        [DataRow("Aaa", CreateCharacterResult.NameFormatInvalid)]
        [DataRow("AaaB", CreateCharacterResult.NameFormatInvalid)]
        [DataRow("Abbba", CreateCharacterResult.NameFormatInvalid)]
        [DataRow("AbBb", CreateCharacterResult.NameFormatInvalid)]
        [DataRow("A-b", CreateCharacterResult.NameFormatInvalid)]
        [DataRow("A b", CreateCharacterResult.NameFormatInvalid)]
        [DataRow("A'b", CreateCharacterResult.NameFormatInvalid)]
        [DataRow("Abc", CreateCharacterResult.Success)]
        [DataRow("Anna", CreateCharacterResult.Success)]
        [DataRow("Abcdefghijklmnopqrst", CreateCharacterResult.Success)]
        [DataRow("Élodie", CreateCharacterResult.Success)]
        public void CreationAndCloningUseTheOriginalNameFormatMessages(string name, CreateCharacterResult expected)
        {
            var creation = new RequestCreateCharacterInSlotPacket
                { CharacterName = name, FamilyName = "Family", Scale = 1, RaceId = Race.Human };
            var clone = new RequestCloneCharacterToSlotPacket
                { CharacterName = name, Scale = 1, RaceId = Race.Human };
            Assert.AreEqual(expected, creation.Validate());
            Assert.AreEqual(expected, clone.Validate());
            creation.CharacterName = "First";
            creation.FamilyName = name;
            Assert.AreEqual(expected, creation.Validate());
            Assert.AreEqual(expected == CreateCharacterResult.Success, CharacterManager.IsValidName(name, out _));
        }
    }
}
