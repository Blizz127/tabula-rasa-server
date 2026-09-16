using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// Creature loot, in the shape the original server's <c>creature_type_loot</c> held it.
    ///
    /// These seven rows are the only loot data that survived, and they belong to one creature type: the original's
    /// type 20, "(Raiding) Trainee Thrax Footsoldier" (class 25580, Bane_Thrax_Soldier_Pistol_NO_XP). That class is
    /// not in this world, so the rows go on its counterpart - this world's Thrax soldiers, the generic Thrax Soldier
    /// (3) and the boot camp's Thrax Infantry Initiates (198507, 198513) - which is an inference, recorded per row.
    /// The items are the original templates: standard-grade cartridges (28), the five Motor Assist armour pieces
    /// (13066, 13096, 13126, 13156, 13186) at half a percent each, and Class I Basic Med Packs (44917).
    ///
    /// Everything else a creature might drop was in the lost server data, so creatures without rows keep the
    /// emulator's stand-in drop (GAP-CREATURE-LOOT).
    /// </summary>
    public class CreatureLootPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureLootEntry.TableName, typeof(CreatureLootEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            // id, creature_id, item_template_id, chance, stacksize_min, stacksize_max, comment
            var soldiers = new uint[] { 3u, 198507u, 198513u };
            var rows = new (uint ItemTemplateId, double Chance, uint Min, uint Max, string Label)[]
            {
                (28u, 12.0, 1u, 35u, "Standard Grade Cartridges"),
                (13066u, 0.5, 1u, 1u, "Motor Assist Armor Boots"),
                (13096u, 0.5, 1u, 1u, "Motor Assist Armor Gloves"),
                (13126u, 0.5, 1u, 1u, "Motor Assist Armor Helmet"),
                (13156u, 0.5, 1u, 1u, "Motor Assist Armor Legs"),
                (13186u, 0.5, 1u, 1u, "Motor Assist Armor Vest"),
                (44917u, 5.0, 1u, 3u, "Class I Basic Med Pack")
            };

            uint id = 1;
            foreach (var creature in soldiers)
                foreach (var row in rows)
                    yield return new object[]
                    {
                        id++, creature, row.ItemTemplateId, row.Chance, row.Min, row.Max,
                        $"creature_type_loot type 20 -> creature {creature}: {row.Label} ({row.Chance}%, {row.Min}-{row.Max})"
                    };
        }
    }
}
