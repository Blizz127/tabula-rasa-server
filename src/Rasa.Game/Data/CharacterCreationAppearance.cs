using System.Collections.Generic;

namespace Rasa.Data
{
    using Structures;

    /// <summary>Original creation picker choices; field provenance: docs/evidence/character-creation-appearance.json.</summary>
    public static class CharacterCreationAppearance
    {
        private static readonly IReadOnlyDictionary<uint, (EquipmentData Slot, uint EntityClass, Race? RequiredRace)> Choices
            = new Dictionary<uint, (EquipmentData, uint, Race?)>
        {
            [36] = (EquipmentData.Hair, 3663, Race.Human),
            [39] = (EquipmentData.Face, 3667, Race.Human),
            [42] = (EquipmentData.Hair, 3672, Race.Human),
            [60] = (EquipmentData.Hair, 3812, null),
            [61] = (EquipmentData.Face, 3813, Race.Human),
            [99] = (EquipmentData.Face, 4083, Race.Human),
            [609] = (EquipmentData.Face, 7506, Race.Human),
            [610] = (EquipmentData.Face, 7507, Race.Human),
            [611] = (EquipmentData.Face, 7508, Race.Human),
            [682] = (EquipmentData.Face, 7693, Race.Human),
            [683] = (EquipmentData.Face, 7694, Race.Human),
            [684] = (EquipmentData.Face, 7695, Race.Human),
            [1775] = (EquipmentData.Hair, 9355, Race.Human),
            [1776] = (EquipmentData.Hair, 9356, Race.Human),
            [1941] = (EquipmentData.Hair, 9781, Race.Human),
            [1942] = (EquipmentData.Hair, 9782, Race.Human),
            [1943] = (EquipmentData.Hair, 9783, Race.Human),
            [1944] = (EquipmentData.Hair, 9784, Race.Human),
            [2245] = (EquipmentData.Face, 10239, Race.Human),
            [42276] = (EquipmentData.Face, 20824, Race.Human),
            [42277] = (EquipmentData.Face, 20825, Race.Human),
            [49668] = (EquipmentData.Face, 24000, Race.Human),
            [49669] = (EquipmentData.Face, 24001, Race.Human),
            [49670] = (EquipmentData.Face, 24002, Race.Human),
            [49671] = (EquipmentData.Face, 24003, Race.Human),
            [49672] = (EquipmentData.Face, 24004, Race.Human),
            [49673] = (EquipmentData.Face, 24005, Race.Human),
            [49674] = (EquipmentData.Face, 24006, Race.Human),
            [49675] = (EquipmentData.Face, 24007, Race.Human),
            [49676] = (EquipmentData.Face, 24008, Race.Human),
            [49677] = (EquipmentData.Face, 24009, Race.Human),
            [49678] = (EquipmentData.Face, 24010, Race.Human),
            [49679] = (EquipmentData.Face, 24011, Race.Human),
            [49680] = (EquipmentData.Face, 24012, Race.Human),
            [49681] = (EquipmentData.Face, 24013, Race.Human),
            [49682] = (EquipmentData.Face, 24014, Race.Human),
            [49683] = (EquipmentData.Face, 24015, Race.Human),
            [49684] = (EquipmentData.Face, 24016, Race.Human),
            [49685] = (EquipmentData.Face, 24017, Race.Human),
            [49686] = (EquipmentData.Face, 24018, Race.Human),
            [49687] = (EquipmentData.Face, 24019, Race.Human),
            [49688] = (EquipmentData.Face, 24020, Race.Human),
            [49689] = (EquipmentData.Face, 24021, Race.Human),
            [49690] = (EquipmentData.Face, 24022, Race.Human),
            [49691] = (EquipmentData.Face, 24023, Race.Human),
            [49692] = (EquipmentData.Face, 24024, Race.Human),
            [49693] = (EquipmentData.Face, 24025, Race.Human),
            [49694] = (EquipmentData.Face, 24026, Race.Human),
            [49695] = (EquipmentData.Face, 24027, Race.Human),
            [49696] = (EquipmentData.Face, 24028, Race.Human),
            [49697] = (EquipmentData.Face, 24029, Race.Human),
            [49698] = (EquipmentData.Face, 24030, Race.Human),
            [49699] = (EquipmentData.Face, 24031, Race.Human),
            [49700] = (EquipmentData.Face, 24032, Race.Human),
            [49701] = (EquipmentData.Face, 24033, Race.Human),
            [49702] = (EquipmentData.Face, 24034, Race.Human),
            [49703] = (EquipmentData.Face, 24035, Race.Human),
            [50311] = (EquipmentData.Face, 24742, Race.Brann),
            [50312] = (EquipmentData.Face, 24743, Race.Forean),
            [50313] = (EquipmentData.Face, 24744, Race.Thrax),
            [50711] = (EquipmentData.Hair, 25255, Race.Human),
            [50712] = (EquipmentData.Hair, 25256, Race.Human),
            [50713] = (EquipmentData.Hair, 25257, Race.Human),
            [97426] = (EquipmentData.Eyewear, 25330, null),
            [97427] = (EquipmentData.Eyewear, 25334, null),
            [97428] = (EquipmentData.Eyewear, 25335, null),
            [97429] = (EquipmentData.Eyewear, 25336, null),
            [97430] = (EquipmentData.Eyewear, 25337, null),
            [97431] = (EquipmentData.Eyewear, 25338, null),
            [97432] = (EquipmentData.Eyewear, 25339, null),
            [97433] = (EquipmentData.Eyewear, 25340, null),
            [97434] = (EquipmentData.Eyewear, 25341, null),
            [97435] = (EquipmentData.Eyewear, 25342, null),
            [97436] = (EquipmentData.Eyewear, 25343, null),
            [97437] = (EquipmentData.Eyewear, 25344, null),
            [97438] = (EquipmentData.Beard, 25345, null),
            [97439] = (EquipmentData.Beard, 25346, null),
            [97440] = (EquipmentData.Beard, 25347, null),
            [97441] = (EquipmentData.Beard, 25348, null),
            [97442] = (EquipmentData.Beard, 25349, null),
            [97443] = (EquipmentData.Beard, 25350, null),
            [97444] = (EquipmentData.Beard, 25351, null),
            [97445] = (EquipmentData.Beard, 25352, null),
            [97446] = (EquipmentData.Beard, 25353, null),
            [118927] = (EquipmentData.Hair, 28536, Race.Human),
            [118928] = (EquipmentData.Hair, 28537, Race.Human),
            [118966] = (EquipmentData.Face, 28606, Race.Brann),
            [118968] = (EquipmentData.Face, 28608, Race.Brann),
            [118969] = (EquipmentData.Face, 28609, Race.Forean),
            [118970] = (EquipmentData.Face, 28610, Race.Forean),
            [118971] = (EquipmentData.Face, 28611, Race.Thrax),
            [118972] = (EquipmentData.Face, 28612, Race.Thrax),
        };

        public static bool TryResolve(Race race, EquipmentData slot, uint templateId, out uint entityClass)
        {
            entityClass = 0;
            if (race < Race.Human || race > Race.Thrax || !Choices.TryGetValue(templateId, out var choice) ||
                choice.Slot != slot || (choice.RequiredRace.HasValue && choice.RequiredRace.Value != race))
                return false;
            entityClass = choice.EntityClass;
            return true;
        }

        public static bool IsValid(Race race, IReadOnlyDictionary<EquipmentData, AppearanceData> appearance)
        {
            // Hair includes the original bald template. Only eyewear and beard/accessory offer None.
            if (appearance == null || !appearance.ContainsKey(EquipmentData.Hair) || !appearance.ContainsKey(EquipmentData.Face))
                return false;
            // _UpdateRaceToggles resets the hybrid beard/accessory choice to None and disables its picker.
            if (race != Race.Human && appearance.ContainsKey(EquipmentData.Beard))
                return false;
            foreach (var pair in appearance)
            {
                var value = pair.Value;
                if (value == null || value.SlotId != pair.Key || value.Color == null || value.Color.Alpha != 255 ||
                    !TryResolve(race, pair.Key, value.Class, out _))
                    return false;
            }
            return true;
        }

        public static uint EntityClassFor(EquipmentData slot, uint templateId)
        {
            if (!Choices.TryGetValue(templateId, out var choice) || choice.Slot != slot)
                throw new System.InvalidOperationException("Unknown character creation appearance choice.");
            return choice.EntityClass;
        }
    }
}
