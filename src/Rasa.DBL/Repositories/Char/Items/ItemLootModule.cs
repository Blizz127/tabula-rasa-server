using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Rasa.Repositories.Char.Items
{
    // Module order determines the original client's name prefix when priorities tie.
    // Level is nullable because an unrecovered server level is not the same as zero.
    public sealed record ItemLootModule(int ModuleId, int? Level);

    public static class ItemLootModules
    {
        public static string Serialize(IReadOnlyList<ItemLootModule> modules)
            => modules == null || modules.Count == 0 ? null : JsonSerializer.Serialize(modules);

        public static IReadOnlyList<ItemLootModule> Deserialize(string json)
            => json == null ? Array.Empty<ItemLootModule>() :
                JsonSerializer.Deserialize<ItemLootModule[]>(json) ?? throw new JsonException("Item modules must be an array.");

        public static IReadOnlyList<ItemLootModule> Copy(IReadOnlyList<ItemLootModule> modules)
        {
            if (modules == null)
                return Array.Empty<ItemLootModule>();
            if (modules.Any(module => module == null))
                throw new ArgumentException("Item module entries cannot be null.", nameof(modules));
            return Array.AsReadOnly(modules.ToArray());
        }
    }
}
