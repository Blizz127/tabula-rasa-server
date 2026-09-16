using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Data
{
    /// <summary>
    /// The auction house's own data, carried the way the client carries it (client/auctionhouse.pyo, decoded to
    /// generated_client_auctionhouse.pyo.json).
    ///
    /// <c>categorydata</c> is the category tree the browse window shows: 83 rows of (bit, name, parentId, tooltipId),
    /// where the bit is what the client puts in a query's category field, and the name is the item class prefix that
    /// belongs to it ("Weapon_Pistol" for a pistol). <c>durationdata</c> is the four listing durations the create
    /// window offers, each with the deposit percentage it costs: 5%, 10%, 20% and 25%.
    ///
    /// Both are the client's own tables rather than authored data, which is why they live here next to the other
    /// client-derived tables (ExpPerLevel, HospitalCatalog) instead of in a world table of their own.
    /// </summary>
    public static class AuctionHouseData
    {
        /// <summary>One duration option: its tooltip id and the deposit it costs as a percentage of the price.</summary>
        public readonly struct Duration
        {
            public Duration(uint tooltipId, uint depositPercent)
            {
                TooltipId = tooltipId;
                DepositPercent = depositPercent;
            }

            public uint TooltipId { get; }
            public uint DepositPercent { get; }
        }

        /// <summary>One catalogue category: the bit the client queries with, its name and its parent.</summary>
        public readonly struct Category
        {
            public Category(uint id, uint bit, string name, uint parentId, uint tooltipId)
            {
                Id = id;
                Bit = bit;
                Name = name;
                ParentId = parentId;
                TooltipId = tooltipId;
            }

            public uint Id { get; }
            public uint Bit { get; }
            public string Name { get; }
            public uint ParentId { get; }
            public uint TooltipId { get; }
        }

        /// <summary>
        /// The four listing durations the create window offers, in the order the client sends them (0-3).
        /// </summary>
        public static readonly Duration[] Durations =
        {
            new Duration(4948u, 5u),   // duration 0
            new Duration(4949u, 10u),   // duration 1
            new Duration(4950u, 20u),   // duration 2
            new Duration(4951u, 25u),   // duration 3
        };

        /// <summary>
        /// The catalogue tree, in the client's own order. <see cref="CategoryOf"/> matches an item to one of these by
        /// the item's class name, which is how the client's own category names are written ("Weapon_Pistol").
        /// </summary>
        public static readonly Category[] Categories =
        {
            new Category(1u, 16777216u, "Weapon", 1u, 4907u),
            new Category(2u, 16842752u, "Weapon_Pistol", 2u, 4913u),
            new Category(3u, 16908288u, "Weapon_Rifle", 2u, 4914u),
            new Category(4u, 16973824u, "Weapon_Shotgun", 2u, 4915u),
            new Category(5u, 17039360u, "Weapon_MachineGun", 2u, 4916u),
            new Category(6u, 17104896u, "Weapon_LeechGun", 2u, 4917u),
            new Category(7u, 17170432u, "Weapon_RPG", 2u, 4918u),
            new Category(8u, 17235968u, "Weapon_RocketLauncher", 2u, 4919u),
            new Category(9u, 17301504u, "Weapon_NetGun", 2u, 4920u),
            new Category(10u, 17367040u, "Weapon_PolarityGun", 2u, 4921u),
            new Category(11u, 17432576u, "Weapon_InjectionGun", 2u, 4922u),
            new Category(12u, 17498112u, "Weapon_PropellantGun", 2u, 4923u),
            new Category(13u, 17563648u, "Weapon_Staff", 2u, 4924u),
            new Category(14u, 17629184u, "Weapon_SniperRifle", 2u, 4925u),
            new Category(15u, 17694720u, "Weapon_Blade", 2u, 4926u),
            new Category(16u, 33554432u, "Armor", 1u, 4908u),
            new Category(17u, 33619968u, "Armor_MotorAssist", 2u, 4927u),
            new Category(18u, 33620224u, "Armor_MotorAssist_Head", 3u, 1511u),
            new Category(21u, 33620992u, "Armor_MotorAssist_Chest", 3u, 1512u),
            new Category(22u, 33621248u, "Armor_MotorAssist_Hands", 3u, 1513u),
            new Category(23u, 33621504u, "Armor_MotorAssist_Legs", 3u, 1514u),
            new Category(24u, 33621760u, "Armor_MotorAssist_Feet", 3u, 1515u),
            new Category(25u, 33685504u, "Armor_Reflective", 2u, 4928u),
            new Category(26u, 33685760u, "Armor_Reflective_Head", 3u, 1511u),
            new Category(29u, 33686528u, "Armor_Reflective_Chest", 3u, 1512u),
            new Category(30u, 33686784u, "Armor_Reflective_Hands", 3u, 1513u),
            new Category(31u, 33687040u, "Armor_Reflective_Legs", 3u, 1514u),
            new Category(32u, 33687296u, "Armor_Reflective_Feet", 3u, 1515u),
            new Category(33u, 33751040u, "Armor_Hazmat", 2u, 4929u),
            new Category(34u, 33751296u, "Armor_Hazmat_Head", 3u, 1511u),
            new Category(37u, 33752064u, "Armor_Hazmat_Chest", 3u, 1512u),
            new Category(38u, 33752320u, "Armor_Hazmat_Hands", 3u, 1513u),
            new Category(39u, 33752576u, "Armor_Hazmat_Legs", 3u, 1514u),
            new Category(40u, 33752832u, "Armor_Hazmat_Feet", 3u, 1515u),
            new Category(41u, 33816576u, "Armor_Graviton", 2u, 4930u),
            new Category(42u, 33816832u, "Armor_Graviton_Head", 3u, 1511u),
            new Category(43u, 33817088u, "Armor_Graviton_UpperFace", 3u, 3800u),
            new Category(44u, 33817344u, "Armor_Graviton_LowerFace", 3u, 3801u),
            new Category(45u, 33817600u, "Armor_Graviton_Chest", 3u, 1512u),
            new Category(46u, 33817856u, "Armor_Graviton_Hands", 3u, 1513u),
            new Category(47u, 33818112u, "Armor_Graviton_Legs", 3u, 1514u),
            new Category(48u, 33818368u, "Armor_Graviton_Feet", 3u, 1515u),
            new Category(49u, 33882112u, "Armor_Stealth", 2u, 4931u),
            new Category(50u, 33882368u, "Armor_Stealth_Head", 3u, 1511u),
            new Category(53u, 33883136u, "Armor_Stealth_Chest", 3u, 1512u),
            new Category(54u, 33883392u, "Armor_Stealth_Hands", 3u, 1513u),
            new Category(55u, 33883648u, "Armor_Stealth_Legs", 3u, 1514u),
            new Category(56u, 33883904u, "Armor_Stealth_Feet", 3u, 1515u),
            new Category(57u, 33947648u, "Armor_MechSuit", 2u, 4932u),
            new Category(58u, 33947904u, "Armor_MechSuit_Head", 3u, 1511u),
            new Category(61u, 33948672u, "Armor_MechSuit_Chest", 3u, 1512u),
            new Category(62u, 33948928u, "Armor_MechSuit_Hands", 3u, 1513u),
            new Category(63u, 33949184u, "Armor_MechSuit_Legs", 3u, 1514u),
            new Category(64u, 33949440u, "Armor_MechSuit_Feet", 3u, 1515u),
            new Category(65u, 34013184u, "Armor_BioSuit", 2u, 4933u),
            new Category(66u, 34013440u, "Armor_BioSuit_Head", 3u, 1511u),
            new Category(69u, 34014208u, "Armor_BioSuit_Chest", 3u, 1512u),
            new Category(70u, 34014464u, "Armor_BioSuit_Hands", 3u, 1513u),
            new Category(71u, 34014720u, "Armor_BioSuit_Legs", 3u, 1514u),
            new Category(72u, 34014976u, "Armor_BioSuit_Feet", 3u, 1515u),
            new Category(73u, 50331648u, "Crafting", 1u, 4909u),
            new Category(74u, 50397184u, "Crafting_Fabrication", 2u, 4934u),
            new Category(75u, 50397440u, "Crafting_Fabrication_Ammunition", 3u, 5008u),
            new Category(76u, 50397696u, "Crafting_Fabrication_Paint", 3u, 5012u),
            new Category(77u, 50397952u, "Crafting_Fabrication_Consumable", 3u, 50u),
            new Category(78u, 50398208u, "Crafting_Fabrication_Resource", 3u, 5009u),
            new Category(104u, 50593792u, "Crafting_Salvage", 2u, 5936u),
            new Category(105u, 67108864u, "Consumables", 1u, 4910u),
            new Category(106u, 17760256u, "Weapon_Tool", 2u, 5005u),
            new Category(107u, 67174400u, "Consumables_Ammunition", 2u, 5008u),
            new Category(108u, 67239936u, "Consumables_Resources", 2u, 5009u),
            new Category(109u, 67305472u, "Consumables_Explosives", 2u, 5010u),
            new Category(110u, 67371008u, "Consumables_Medical", 2u, 5011u),
            new Category(111u, 67436544u, "Consumables_Dyes", 2u, 5012u),
            new Category(112u, 67502080u, "Consumables_Miscellaneous", 2u, 5013u),
            new Category(113u, 34078720u, "Armor_Accessory", 2u, 5935u),
            new Category(114u, 34078976u, "Armor_Accessory_UpperFace", 3u, 3800u),
            new Category(115u, 34079232u, "Armor_Accessory_LowerFace", 3u, 3801u),
            new Category(116u, 50659328u, "Crafting_Modules", 2u, 5937u),
            new Category(117u, 50659584u, "Crafting_Modules_Armor", 3u, 5938u),
            new Category(118u, 50659840u, "Crafting_Modules_Tools", 3u, 5939u),
            new Category(119u, 50660096u, "Crafting_Modules_Weapons", 3u, 5940u),
            new Category(120u, 50724864u, "Crafting_Mimeogel", 2u, 5941u),
        };

        /// <summary>Longest category name first, so an exact prefix match picks the most specific category.</summary>
        private static readonly Category[] ByNameLength =
            Categories.OrderByDescending(category => category.Name.Length).ToArray();

        /// <summary>
        /// The category an item belongs to, from its class name. The client's category names are pairs
        /// ("Weapon_Pistol"), and the class names spell the same thing out with extra words between them
        /// ("Weapon_Avatar_Pistol_Physical_UNC_01_to_04"), so the match is on the category's **last** word appearing as
        /// one of the class name's words - longest first, so Pistol beats a bare Weapon. Categories whose last word is
        /// a container word ("Any", "Other") only match by full name.
        /// </summary>
        public static uint CategoryOf(string className)
        {
            if (string.IsNullOrEmpty(className))
                return 0;

            var words = className.Split('_');

            // The client's categories are parent/leaf pairs: "Weapon" contains "Weapon_Pistol", "Armor" contains
            // "Armor_MotorAssist". An item class spells the leaf out with extra words between its own
            // ("Weapon_Avatar_Pistol_Physical_UNC_01_to_04"), so a leaf matches when all of its words appear among the
            // class name's words - and the longest such leaf wins, so an armour class carrying both a family word and a
            // slot word lands in the family the item actually belongs to.
            uint best = 0;
            var bestLength = 0;
            foreach (var category in Categories)
            {
                if (!category.Name.Contains('_'))
                    continue;

                var parts = category.Name.Split('_');
                if (!parts.All(part => words.Any(word => string.Equals(word, part, StringComparison.OrdinalIgnoreCase))))
                    continue;

                if (category.Name.Length > bestLength)
                {
                    best = category.Bit;
                    bestLength = category.Name.Length;
                }
            }

            if (best != 0)
                return best;

            // Nothing specific matched: the class name may still start with a parent category's own name.
            foreach (var category in ByNameLength)
                if (className.StartsWith(category.Name, StringComparison.OrdinalIgnoreCase))
                    return category.Bit;

            return 0;
        }


        /// <summary>The category id of a bit the client queried with, or 0.</summary>
        public static uint IdOfBit(uint bit) => Categories.FirstOrDefault(category => category.Bit == bit).Id;
    }
}
