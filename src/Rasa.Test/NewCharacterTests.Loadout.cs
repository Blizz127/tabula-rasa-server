using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Context.Char;
using Rasa.Data;
using Rasa.Test.Reconstruction;

namespace Rasa.Test
{
    public partial class NewCharacterTests
    {
        private static void AssertStarterLoadout(SqliteCharContext context, uint characterId)
        {
            using var evidence = JsonDocument.Parse(File.ReadAllText(
                EvidenceLocator.EvidenceFile("new-character-loadout.json")));
            var locations = context.CharacterInventoryEntries.Where(i => i.CharacterId == characterId).ToArray();
            Assert.AreEqual(evidence.RootElement.GetProperty("rows").GetArrayLength(), locations.Length);
            Assert.IsFalse(locations.Any(i => i.InventoryType == (uint)InventoryType.Personal && i.SlotId < 50),
                "Final-week footage A3-019 shows an empty Equipment backpack before the crate.");
            foreach (var row in evidence.RootElement.GetProperty("rows").EnumerateArray())
            {
                var fields = row.GetProperty("fields");
                uint Value(string name) => fields.GetProperty(name).GetProperty("value").GetUInt32();
                var templateId = Value("template_id");
                var item = context.ItemEntries.Where(i => i.ItemTemplateId == templateId).ToArray()
                    .Single(i => locations.Any(l => l.ItemId == i.ItemId));
                var location = locations.Single(l => l.ItemId == item.ItemId);
                Assert.AreEqual(Value("inventory_type"), location.InventoryType, row.GetProperty("name").GetString());
                Assert.AreEqual(Value("slot_id"), location.SlotId);
                Assert.AreEqual(Value("quantity"), item.StackSize);
                Assert.AreEqual(Value("ammo_count"), item.AmmoCount, "Loaded magazine must survive creation persistence.");
                Assert.AreEqual((int)Value("current_hit_points"), item.CurrentHitPoints);
                Assert.AreEqual(Value("color"), item.Color);
            }
        }
    }
}
