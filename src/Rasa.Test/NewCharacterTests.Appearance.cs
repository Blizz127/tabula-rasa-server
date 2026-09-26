using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Packets.Game.Client;
using Rasa.Packets.Game.Server;
using Rasa.Structures;

namespace Rasa.Test
{
    public partial class NewCharacterTests
    {
        [DataTestMethod]
        [DataRow(false, "missinghair")]
        [DataRow(false, "missingface")]
        [DataRow(false, "outfit")]
        [DataRow(false, "wrongslot")]
        [DataRow(false, "crossrace")]
        [DataRow(false, "unknown")]
        [DataRow(false, "transparent")]
        [DataRow(false, "nullcolor")]
        [DataRow(false, "nullentry")]
        [DataRow(false, "mismatchkey")]
        [DataRow(true, "missinghair")]
        [DataRow(true, "missingface")]
        [DataRow(true, "outfit")]
        [DataRow(true, "wrongslot")]
        [DataRow(true, "crossrace")]
        [DataRow(true, "unknown")]
        [DataRow(true, "transparent")]
        [DataRow(true, "nullcolor")]
        [DataRow(true, "nullentry")]
        [DataRow(true, "mismatchkey")]
        public void InvalidAppearanceCannotCreateOrSpendCloneCredits(bool clone, string invalid)
        {
            Dictionary<EquipmentData, AppearanceData> appearance;
            var creation = FirstRequest();
            var cloning = new RequestCloneCharacterToSlotPacket
                { CloneSlotNum = 1, SlotNum = 2, CharacterName = "Second", RaceId = Race.Human, Scale = 1 };
            if (clone)
            {
                _manager.RequestCreateCharacterInSlot(_client, creation);
                Drain();
                using (var context = Context())
                {
                    context.CharacterEntries.Single().CloneCredits = 1;
                    context.SaveChanges();
                }
                SetAppearance(cloning.AppearanceData, Race.Human);
                appearance = cloning.AppearanceData;
            }
            else appearance = creation.AppearanceData;

            switch (invalid)
            {
                case "missinghair": appearance.Remove(EquipmentData.Hair); break;
                case "missingface": appearance.Remove(EquipmentData.Face); break;
                case "outfit": appearance[EquipmentData.Shoes] = new AppearanceData
                    { SlotId = EquipmentData.Shoes, Class = 122854, Color = new Color(255, 255, 255) }; break;
                case "wrongslot": appearance[EquipmentData.Hair].Class = 39; break;
                case "crossrace": appearance[EquipmentData.Face].Class = 50312; break;
                case "unknown": appearance[EquipmentData.Hair].Class = uint.MaxValue; break;
                case "transparent": appearance[EquipmentData.Face].Color.Alpha = 0; break;
                case "nullcolor": appearance[EquipmentData.Hair].Color = null; break;
                case "nullentry": appearance[EquipmentData.Hair] = null; break;
                case "mismatchkey": appearance[EquipmentData.Hair].SlotId = EquipmentData.Face; break;
            }
            if (clone) _manager.RequestCloneCharacterToSlot(_client, cloning);
            else _manager.RequestCreateCharacterInSlot(_client, creation);
            Assert.AreEqual(CreateCharacterResult.InvalidEncoding, Drain().OfType<UserCreationFailedPacket>().Single().Result);
            using var saved = Context();
            Assert.AreEqual(clone ? 1 : 0, saved.CharacterEntries.Count());
            Assert.AreEqual(clone ? 5 : 0, saved.CharacterAppearanceEntries.Count());
            if (clone) Assert.AreEqual(1u, saved.CharacterEntries.Single().CloneCredits);
            else Assert.AreEqual("", saved.GameAccountEntries.Single().FamilyName);
        }

        [TestMethod]
        public void AppearanceTemplatesSaveOriginalClassesAndColorsWithoutWorldLookups()
        {
            var request = FirstRequest();
            request.Gender = 1; // Original female picker exposes Beard as Accessory with the same templates.
            request.AppearanceData[EquipmentData.Beard] = new AppearanceData
                { SlotId = EquipmentData.Beard, Class = 97438, Color = new Color(82, 52, 32) };
            _manager.RequestCreateCharacterInSlot(_client, request);
            Assert.AreEqual(1, Drain().OfType<CharacterCreateSuccessPacket>().Count());
            using var saved = Context();
            Assert.AreEqual(3812u, saved.CharacterAppearanceEntries.Single(a => a.Slot == (uint)EquipmentData.Hair).Class);
            Assert.AreEqual(3667u, saved.CharacterAppearanceEntries.Single(a => a.Slot == (uint)EquipmentData.Face).Class);
            Assert.AreEqual(25345u, saved.CharacterAppearanceEntries.Single(a => a.Slot == (uint)EquipmentData.Beard).Class);
            Assert.AreEqual(new Color(214, 178, 132).Hue, saved.CharacterAppearanceEntries.Single(a => a.Slot == (uint)EquipmentData.Face).Color);
        }
    }
}
