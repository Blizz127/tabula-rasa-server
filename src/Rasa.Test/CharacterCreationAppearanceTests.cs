using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Memory;
using Rasa.Structures;
using Rasa.Test.Reconstruction;

namespace Rasa.Test
{
    [TestClass]
    public class CharacterCreationAppearanceTests
    {
        [TestMethod]
        public void EveryOriginalChoiceMatchesItsSlotClassAndRaceEvidence()
        {
            using var document = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("character-creation-appearance.json")));
            var rows = document.RootElement.GetProperty("choices");
            Assert.AreEqual(92, rows.GetArrayLength());
            foreach (var row in rows.EnumerateArray())
            {
                var slot = (EquipmentData)row.GetProperty("slot_id").GetUInt32();
                var template = row.GetProperty("item_template_id").GetUInt32();
                var requiredRace = row.GetProperty("required_race");
                foreach (var race in new[] { Race.Human, Race.Forean, Race.Brann, Race.Thrax })
                {
                    var allowed = requiredRace.ValueKind == JsonValueKind.Null || requiredRace.GetInt32() == (int)race;
                    Assert.AreEqual(allowed, CharacterCreationAppearance.TryResolve(race, slot, template, out var entityClass));
                    if (allowed) Assert.AreEqual(row.GetProperty("entity_class_id").GetUInt32(), entityClass);
                    Assert.IsFalse(CharacterCreationAppearance.TryResolve(race, EquipmentData.Weapon, template, out _));
                }
            }
            Assert.IsFalse(CharacterCreationAppearance.TryResolve(Race.Human, EquipmentData.Hair, uint.MaxValue, out _));
            Assert.IsFalse(CharacterCreationAppearance.TryResolve(Race.Human, EquipmentData.Shoes, 122854, out _));
        }

        [TestMethod]
        public void MandatoryBaldHairAndFaceAllowOptionalAccessories()
        {
            var appearance = new Dictionary<EquipmentData, AppearanceData>
            {
                [EquipmentData.Hair] = new AppearanceData { SlotId = EquipmentData.Hair, Class = 60, Color = new Color(82, 52, 32) },
                [EquipmentData.Face] = new AppearanceData { SlotId = EquipmentData.Face, Class = 50312, Color = new Color(99, 113, 90) }
            };
            Assert.IsTrue(CharacterCreationAppearance.IsValid(Race.Forean, appearance));
            appearance[EquipmentData.Beard] = new AppearanceData { SlotId = EquipmentData.Beard, Class = 97438, Color = new Color(82, 52, 32) };
            appearance[EquipmentData.Eyewear] = new AppearanceData { SlotId = EquipmentData.Eyewear, Class = 97426, Color = new Color(168, 140, 66) };
            Assert.IsFalse(CharacterCreationAppearance.IsValid(Race.Forean, appearance), "Hybrid beard/accessory picker is disabled and reset to None.");
            appearance.Remove(EquipmentData.Beard);
            Assert.IsTrue(CharacterCreationAppearance.IsValid(Race.Forean, appearance));
            appearance[EquipmentData.Hair].Class = 36;
            Assert.IsFalse(CharacterCreationAppearance.IsValid(Race.Forean, appearance), "Human hair is not offered to a hybrid.");
        }

        [DataTestMethod]
        [DataRow(Race.Human, 39u, true)]
        [DataRow(Race.Forean, 50312u, false)]
        [DataRow(Race.Brann, 50311u, false)]
        [DataRow(Race.Thrax, 50313u, false)]
        public void BeardPickerIsAvailableOnlyToHumans(Race race, uint face, bool allowed)
        {
            var appearance = new Dictionary<EquipmentData, AppearanceData>
            {
                [EquipmentData.Hair] = new AppearanceData { SlotId = EquipmentData.Hair, Class = 60, Color = new Color(82, 52, 32) },
                [EquipmentData.Face] = new AppearanceData { SlotId = EquipmentData.Face, Class = face, Color = new Color(214, 178, 132) },
                [EquipmentData.Beard] = new AppearanceData { SlotId = EquipmentData.Beard, Class = 97438, Color = new Color(82, 52, 32) }
            };
            Assert.AreEqual(allowed, CharacterCreationAppearance.IsValid(race, appearance));
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void AppearanceColorAcceptsFullByteRangeInBothIntegerEncodings(bool longValues)
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            writer.WriteUInt(14); writer.WriteTuple(2); writer.WriteUInt(60); writer.WriteTuple(4);
            foreach (var channel in new[] { 0, 128, 254, 255 })
                if (longValues) writer.WriteLong(channel); else writer.WriteUInt((uint)channel);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            var value = new AppearanceData(); value.Read(reader);
            Assert.AreEqual(0xfffe8000u, value.Color.Hue);
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [DataTestMethod]
        [DataRow(-1L, false)]
        [DataRow(256L, false)]
        [DataRow(-1L, true)]
        [DataRow(256L, true)]
        [DataRow(4294967551L, true)]
        public void AppearanceColorCannotWrapMalformedChannels(long channel, bool longValue)
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            writer.WriteUInt(14); writer.WriteTuple(2); writer.WriteUInt(60); writer.WriteTuple(4);
            if (longValue) writer.WriteLong(channel); else writer.WriteInt((int)channel);
            writer.WriteInt(0); writer.WriteInt(0); writer.WriteInt(255);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.ThrowsException<OverflowException>(() => new AppearanceData().Read(reader));
        }

        [DataTestMethod]
        [DataRow(1, 4)]
        [DataRow(3, 4)]
        [DataRow(2, 3)]
        [DataRow(2, 5)]
        public void AppearanceRejectsMalformedTupleShapes(int appearanceFields, int colorFields)
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            writer.WriteUInt(14); writer.WriteTuple(appearanceFields); writer.WriteUInt(60); writer.WriteTuple(colorFields);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.ThrowsException<InvalidDataException>(() => new AppearanceData().Read(reader));
        }
    }
}
