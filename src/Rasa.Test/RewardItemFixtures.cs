using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Rasa.Configuration;
using Rasa.Configuration.ContextSetup;
using Rasa.Context.World;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Repositories.Char;
using Rasa.Repositories.UnitOfWork;
using Rasa.Repositories.World;
using Rasa.Services.DbContext;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Test
{
    /// <summary>
    /// Loads the reconstructed mission-reward item templates through the real <see cref="ItemManager.LoadItemTemplates"/>
    /// from a world database migrated through <c>WildernessClassGear</c>: the Training Day pistols (116929 Vextronics
    /// Pistol, 116930 Vextronics Pulse Pistol) and the twelve D11 class-gear templates (122859..122871). Unit tests do
    /// not replay the world seed, so the original seed rows those templates hang on are stood in for with their
    /// deployed values: the template-to-class map (itemtemplate_itemclass), the skill requirements
    /// (itemtemplate_requirement_skill), the class-keyed level requirement (itemtemplate_requirement), and the item,
    /// weapon, armor and equipable classes, which the entity-class seed would load. Dispose restores the previous
    /// ItemManager and entity classes.
    ///
    /// Since MissionRewardItems (2026-09-26) it also carries the 25 reward templates of missions 1541, 1673, 1040, 983,
    /// 970, 1068 and 1863 with their deployed world-seed values: class, item-class max_hp and stack size, class level
    /// requirement, template skill requirement and, for the seven weapons, their weaponclass rows.
    /// </summary>
    public sealed class RewardItemFixtures : IDisposable
    {
        public const uint Pistol = 116929;
        public const uint PulsePistol = 116930;
        public const EntityClasses PistolClass = (EntityClasses)27121;
        public const EntityClasses PulsePistolClass = (EntityClasses)27100;

        /// <summary>One reconstructed reward template: its item class and the world-seed rows it hangs on.</summary>
        private sealed class RewardTemplate
        {
            public uint Template;
            public EntityClasses Class;
            public int MaxHitPoints;
            public int Skill;
            public bool Weapon;
            public WeaponClassEntry WeaponClass;
            public int Level = 5;
            public uint StackSize = 1;
            public bool Consumable;
            public ArmorClassEntry ArmorClass;
        }

        private static ArmorClassEntry Armor(uint id, uint absorbed, int regen) =>
            new ArmorClassEntry { Id = id, MinDamageAbsorbed = absorbed, MaxDamageAbsorbed = absorbed, RegenRate = regen };

        private static WeaponClassEntry Weapon(uint id, uint template, uint action, uint arg, byte draw, byte reload, uint ammo, uint clip,
            int damage, byte damageType, uint anim) => new WeaponClassEntry
        {
            Id = id, WeaponTemplatId = template, AttackActionId = action, AttackActionArgId = arg, DrawActionId = draw,
            StowActionId = draw, ReloadActionId = reload, AmmoClassId = ammo, ClipSize = clip, MinDamage = damage,
            MaxDamage = damage, DamageType = damageType, WeaponAnimConditionCode = anim
        };

        private static WeaponClassEntry MachineGun() => new WeaponClassEntry
        {
            Id = 27059, WeaponTemplatId = 7, AttackActionId = 149, AttackActionArgId = 1, DrawActionId = 7,
            StowActionId = 7, ReloadActionId = 6, AmmoClassId = 3147, ClipSize = 100, MinDamage = 37,
            MaxDamage = 37, DamageType = 1, WeaponAnimConditionCode = 7
        };

        private static WeaponClassEntry RepairTool() => new WeaponClassEntry
        {
            Id = 12797, WeaponTemplatId = 195, AttackActionId = 198, AttackActionArgId = 1, DrawActionId = 10,
            StowActionId = 16, ReloadActionId = 89, AmmoClassId = 3807, ClipSize = 10, MinDamage = 190,
            MaxDamage = 190, DamageType = 1, WeaponAnimConditionCode = 14
        };

        // The D11 class-gear block: Soldier (2010) Reflective helmet/vest/gloves/legs/boots and the Rage-O-Matic
        // machine gun; Specialist (2011) Hazmat helmet/vest/gloves/legs/boots and the Repair-O-Matic repair tool.
        private static readonly RewardTemplate[] Gear =
        {
            new RewardTemplate { Template = 122859, Class = (EntityClasses)18504, MaxHitPoints = 126, Skill = 21 },
            new RewardTemplate { Template = 122860, Class = (EntityClasses)18596, MaxHitPoints = 189, Skill = 21 },
            new RewardTemplate { Template = 122862, Class = (EntityClasses)18458, MaxHitPoints = 63, Skill = 21 },
            new RewardTemplate { Template = 122863, Class = (EntityClasses)18550, MaxHitPoints = 158, Skill = 21 },
            new RewardTemplate { Template = 122864, Class = (EntityClasses)18412, MaxHitPoints = 95, Skill = 21 },
            new RewardTemplate { Template = 122865, Class = (EntityClasses)27059, MaxHitPoints = 120, Skill = 22, Weapon = true, WeaponClass = MachineGun() },
            new RewardTemplate { Template = 122866, Class = (EntityClasses)13710, MaxHitPoints = 95, Skill = 30 },
            new RewardTemplate { Template = 122867, Class = (EntityClasses)13802, MaxHitPoints = 142, Skill = 30 },
            new RewardTemplate { Template = 122868, Class = (EntityClasses)13664, MaxHitPoints = 47, Skill = 30 },
            new RewardTemplate { Template = 122869, Class = (EntityClasses)13756, MaxHitPoints = 118, Skill = 30 },
            new RewardTemplate { Template = 122870, Class = (EntityClasses)13618, MaxHitPoints = 71, Skill = 30 },
            new RewardTemplate { Template = 122871, Class = (EntityClasses)12797, MaxHitPoints = 100, Skill = 14, Weapon = true, WeaponClass = RepairTool() }
        };

        // MissionRewardItems: the reward templates of 1541, 1673, 1040, 983 (equipment) and 970, 1068, 1863 (consumables),
        // each with its deployed itemtemplate_itemclass, itemclass (max_hp, stack), itemtemplate_requirement level and
        // itemtemplate_requirement_skill rows, and the weapons' weaponclass rows.
        private static readonly RewardTemplate[] MissionRewards =
        {
            new RewardTemplate { Template = 120418, Class = (EntityClasses)13924, MaxHitPoints = 636, Skill = 30, Level = 34, ArmorClass = Armor(13924, 6364, 35) },
            new RewardTemplate { Template = 120419, Class = (EntityClasses)16776, MaxHitPoints = 584, Skill = 19, Level = 33, ArmorClass = Armor(16776, 5836, 16) },
            new RewardTemplate { Template = 120420, Class = (EntityClasses)18718, MaxHitPoints = 849, Skill = 21, Level = 34, ArmorClass = Armor(18718, 8485, 27) },
            new RewardTemplate { Template = 120421, Class = (EntityClasses)19930, MaxHitPoints = 2313, Skill = 48, Level = 35, ArmorClass = Armor(19930, 23133, 24) },
            new RewardTemplate { Template = 120101, Class = (EntityClasses)14067, MaxHitPoints = 2944, Skill = 30, Level = 39, ArmorClass = Armor(14067, 29444, 164) },
            new RewardTemplate { Template = 120102, Class = (EntityClasses)18073, MaxHitPoints = 4164, Skill = 57, Level = 35, ArmorClass = Armor(18073, 41640, 28) },
            new RewardTemplate { Template = 120103, Class = (EntityClasses)16931, MaxHitPoints = 2700, Skill = 19, Level = 38, ArmorClass = Armor(16931, 27000, 75) },
            new RewardTemplate { Template = 120104, Class = (EntityClasses)18861, MaxHitPoints = 3926, Skill = 21, Level = 39, ArmorClass = Armor(18861, 39258, 123) },
            new RewardTemplate { Template = 120382, Class = (EntityClasses)27054, MaxHitPoints = 120, Skill = 22, Level = 33, Weapon = true, WeaponClass = Weapon(27054, 118, 149, 4, 7, 42, 3807, 100, 389, 6, 7) },
            new RewardTemplate { Template = 120383, Class = (EntityClasses)27074, MaxHitPoints = 120, Skill = 55, Level = 31, Weapon = true, WeaponClass = Weapon(27074, 172, 398, 1, 3, 44, 25938, 3, 460, 13, 3) },
            new RewardTemplate { Template = 120384, Class = (EntityClasses)27115, MaxHitPoints = 120, Skill = 1, Level = 30, Weapon = true, WeaponClass = Weapon(27115, 75, 1, 67, 1, 51, 25920, 10, 569, 6, 1) },
            new RewardTemplate { Template = 120385, Class = (EntityClasses)27157, MaxHitPoints = 120, Skill = 58, Level = 33, Weapon = true, WeaponClass = Weapon(27157, 185, 249, 3, 7, 22, 3301, 50, 497, 3, 7) },
            new RewardTemplate { Template = 120804, Class = (EntityClasses)27094, MaxHitPoints = 120, Skill = 1, Level = 25, Weapon = true, WeaponClass = Weapon(27094, 65, 1, 170, 1, 47, 25920, 10, 654, 13, 1) },
            new RewardTemplate { Template = 120805, Class = (EntityClasses)27156, MaxHitPoints = 120, Skill = 58, Level = 28, Weapon = true, WeaponClass = Weapon(27156, 185, 249, 3, 7, 22, 3301, 50, 329, 3, 7) },
            new RewardTemplate { Template = 120806, Class = (EntityClasses)27087, MaxHitPoints = 120, Skill = 55, Level = 26, Weapon = true, WeaponClass = Weapon(27087, 175, 398, 3, 3, 46, 25938, 3, 282, 3, 3) },
            new RewardTemplate { Template = 45059, Class = (EntityClasses)22542, MaxHitPoints = 100, Level = 30, StackSize = 100, Consumable = true },
            new RewardTemplate { Template = 118897, Class = (EntityClasses)28488, MaxHitPoints = 100, Level = 30, StackSize = 5000, Consumable = true },
            new RewardTemplate { Template = 111037, Class = (EntityClasses)26287, MaxHitPoints = 100, Level = 26, StackSize = 5000, Consumable = true },
            new RewardTemplate { Template = 111027, Class = (EntityClasses)26277, MaxHitPoints = 100, Level = 26, StackSize = 5000, Consumable = true },
            new RewardTemplate { Template = 118906, Class = (EntityClasses)28497, MaxHitPoints = 100, Level = 30, StackSize = 5000, Consumable = true },
            new RewardTemplate { Template = 111017, Class = (EntityClasses)26267, MaxHitPoints = 100, Level = 26, StackSize = 5000, Consumable = true },
            new RewardTemplate { Template = 45062, Class = (EntityClasses)22545, MaxHitPoints = 100, Level = 35, StackSize = 100, Consumable = true },
            new RewardTemplate { Template = 118898, Class = (EntityClasses)28489, MaxHitPoints = 100, Level = 35, StackSize = 5000, Consumable = true },
            new RewardTemplate { Template = 111048, Class = (EntityClasses)26298, MaxHitPoints = 100, Level = 31, StackSize = 5000, Consumable = true },
            new RewardTemplate { Template = 45446, Class = (EntityClasses)22963, MaxHitPoints = 100, Level = 31, StackSize = 5000, Consumable = true }
        };

        private readonly object _previousItemManager;
        private readonly Dictionary<EntityClasses, EntityClass> _previousClasses = new();

        public ItemManager Items { get; }

        private sealed class TestConfiguration : IDbContextConfigurationService
        {
            private readonly SqliteConnection _connection;
            public TestConfiguration(SqliteConnection connection) => _connection = connection;
            public void Configure(DbContextOptionsBuilder builder, DatabaseConnectionConfiguration configuration)
                => builder.UseSqlite(_connection);
        }

        public class EquipmentWorldProxy : DispatchProxy
        {
            public SqliteWorldContext Context;
            protected override object Invoke(MethodInfo method, object[] args)
            {
                switch (method.Name)
                {
                    case "get_Equipment": return new EquipmentRepository(Context);
                    case "Dispose": Context.Dispose(); return null;
                    default: throw new NotSupportedException(method.Name);
                }
            }
        }

        private sealed class Factory : IGameUnitOfWorkFactory
        {
            private readonly SqliteConnection _world;
            public Factory(SqliteConnection world) => _world = world;
            public ICharUnitOfWork CreateChar() => throw new NotSupportedException();
            public IWorldUnitOfWork CreateWorld()
            {
                var unit = DispatchProxy.Create<IWorldUnitOfWork, EquipmentWorldProxy>();
                ((EquipmentWorldProxy)(object)unit).Context = new SqliteWorldContext(Options.Create(new DatabaseConfiguration()),
                    new TestConfiguration(_world), new SqliteDbContextPropertyModifier());
                return unit;
            }
        }

        private static void Execute(SqliteConnection connection, string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        /// <summary>Inserts the original world-seed rows of the reward templates that the migrations do not own.</summary>
        public static void SeedOriginalTemplateRows(SqliteConnection worldConnection)
        {
            Execute(worldConnection, "INSERT INTO itemtemplate_itemclass (itemTemplateId, itemClassId) VALUES (116929, 27121), (116930, 27100)");
            // Original client mappings used by Forming Alliances: Thrax Heart and the
            // reconstructed level-range Motor Assist Vest counterpart.
            Execute(worldConnection, "INSERT INTO itemtemplate_itemclass (itemTemplateId, itemClassId) VALUES (2285, 10346), (13738, 16396)");
            // The Wilderness doctors' sample items (WildernessXenobiologySamples): Thrax Blood Sample, Shield Drone Scraps,
            // Xanx Pincers and Fithik Spleen, each the lower of its two client templates.
            Execute(worldConnection, "INSERT INTO itemtemplate_itemclass (itemTemplateId, itemClassId) VALUES (2524, 11150), (2527, 11153), (2532, 11160), (2533, 11161)");
            Execute(worldConnection, "INSERT INTO itemtemplate_requirement_skill (id, skill_id, skill_level) VALUES (116929, 1, 1), (116930, 1, 1)");
            Execute(worldConnection, "INSERT INTO itemtemplate_requirement (id, req_type, req_value) VALUES (27121, 1, 5), (27100, 1, 5)");

            foreach (var template in Gear)
            {
                Execute(worldConnection, $"INSERT INTO itemtemplate_itemclass (itemTemplateId, itemClassId) VALUES ({template.Template}, {(uint)template.Class})");
                Execute(worldConnection, $"INSERT INTO itemtemplate_requirement_skill (id, skill_id, skill_level) VALUES ({template.Template}, {template.Skill}, 1)");
                Execute(worldConnection, $"INSERT INTO itemtemplate_requirement (id, req_type, req_value) VALUES ({(uint)template.Class}, 1, 5)");
            }

            foreach (var template in MissionRewards)
            {
                Execute(worldConnection, $"INSERT INTO itemtemplate_itemclass (itemTemplateId, itemClassId) VALUES ({template.Template}, {(uint)template.Class})");
                if (template.Skill > 0)
                    Execute(worldConnection, $"INSERT INTO itemtemplate_requirement_skill (id, skill_id, skill_level) VALUES ({template.Template}, {template.Skill}, 1)");
                Execute(worldConnection, $"INSERT INTO itemtemplate_requirement (id, req_type, req_value) VALUES ({(uint)template.Class}, 1, {template.Level})");
            }
        }

        public RewardItemFixtures(SqliteConnection worldConnection)
        {
            var field = typeof(ItemManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            _previousItemManager = field.GetValue(null);

            // itemclass / weaponclass rows of the reward classes as deployed. The pistols: max_hp 120, ammo 3147 clip 20
            // 91 physical (attack action 1, arg 133) and ammo 3807 clip 10 106 EMP (arg 135). The gear: each piece's
            // client itemclass max_hp and stack 1, the machine gun and repair tool with their weaponclass values.
            RegisterClass(PistolClass, 120, new WeaponClassEntry { Id = 27121, WeaponTemplatId = 1, AttackActionId = 1, AttackActionArgId = 133, DrawActionId = 1, StowActionId = 1, ReloadActionId = 1, AmmoClassId = 3147, ClipSize = 20, MinDamage = 91, MaxDamage = 91, DamageType = 1, WeaponAnimConditionCode = 1 });
            RegisterClass(PulsePistolClass, 120, new WeaponClassEntry { Id = 27100, WeaponTemplatId = 77, AttackActionId = 1, AttackActionArgId = 135, DrawActionId = 1, StowActionId = 1, ReloadActionId = 49, AmmoClassId = 3807, ClipSize = 10, MinDamage = 106, MaxDamage = 106, DamageType = 5, WeaponAnimConditionCode = 1 });
            RegisterClass((EntityClasses)10346, 0, null, 12);
            foreach (var sampleClass in new uint[] { 11150, 11153, 11160, 11161 })
                RegisterClass((EntityClasses)sampleClass, 0, null, 1000);
            RegisterClass((EntityClasses)16396, 130, null);

            foreach (var template in Gear)
                RegisterClass(template.Class, template.MaxHitPoints, template.WeaponClass);
            foreach (var template in MissionRewards)
                RegisterClass(template.Class, template.MaxHitPoints, template.WeaponClass, template.StackSize, template.Consumable, template.ArmorClass);

            Items = (ItemManager)Activator.CreateInstance(typeof(ItemManager), BindingFlags.NonPublic | BindingFlags.Instance, null,
                new object[] { new Factory(worldConnection) }, null);
            field.SetValue(null, Items);
            try
            {
                Items.LoadItemTemplates();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void RegisterClass(EntityClasses classId, int maxHitPoints, WeaponClassEntry weapon, uint stackSize = 1, bool consumable = false,
            ArmorClassEntry armor = null)
        {
            _previousClasses[classId] = EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(classId, out var previous) ? previous : null;
            var augmentations = consumable
                ? new List<AugmentationType> { AugmentationType.Item }
                : weapon == null
                ? new List<AugmentationType> { AugmentationType.Armor, AugmentationType.Equipable, AugmentationType.Item }
                : new List<AugmentationType> { AugmentationType.Weapon, AugmentationType.Equipable, AugmentationType.Item };
            EntityClassManager.Instance.LoadedEntityClasses[classId] = new EntityClass((uint)classId, "stand-in for the world-seed class", 0, 0,
                augmentations, false)
            {
                ItemClassInfo = new ItemClassInfo(new ItemClassEntry { Id = (uint)classId, MaxHitPoints = maxHitPoints, StackSize = stackSize, LootValue = 500 }),
                WeaponClassInfo = weapon == null ? null : new WeaponClassInfo(weapon),
                ArmorClassInfo = armor == null ? null : new ArmorClassInfo(armor)
            };
        }

        public void Dispose()
        {
            typeof(ItemManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, _previousItemManager);
            foreach (var pair in _previousClasses)
                if (pair.Value == null)
                    EntityClassManager.Instance.LoadedEntityClasses.Remove(pair.Key);
                else
                    EntityClassManager.Instance.LoadedEntityClasses[pair.Key] = pair.Value;
        }
    }
}
