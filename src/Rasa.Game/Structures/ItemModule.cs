using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Structures
{
    /// <summary>A module tooltip definition, distinct from an item's persisted module IDs.</summary>
    public sealed class ItemModule
    {
        public int ModuleId { get; }
        public int? ModuleLevel { get; }
        public IReadOnlyList<ModuleInfo> Effects { get; }

        public ItemModule(int moduleId, int? moduleLevel, IEnumerable<ModuleInfo> effects)
        {
            if (effects == null)
                throw new ArgumentNullException(nameof(effects));
            var rows = effects.ToArray();
            if (rows.Any(row => row == null))
                throw new ArgumentException("A module effect row cannot be null.", nameof(effects));
            ModuleId = moduleId;
            ModuleLevel = moduleLevel;
            Effects = Array.AsReadOnly(rows);
        }

        public ItemModule(int moduleId, int? moduleLevel, ModuleInfo effect)
            : this(moduleId, moduleLevel, new[] { effect })
        {
        }
    }
}
