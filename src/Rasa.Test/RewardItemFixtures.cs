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
        }

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
            Execute(worldConnection, "INSERT INTO itemtemplate_requirement_skill (id, skill_id, skill_level) VALUES (116929, 1, 1), (116930, 1, 1)");
            Execute(worldConnection, "INSERT INTO itemtemplate_requirement (id, req_type, req_value) VALUES (27121, 1, 5), (27100, 1, 5)");

            foreach (var template in Gear)
            {
                Execute(worldConnection, $"INSERT INTO itemtemplate_itemclass (itemTemplateId, itemClassId) VALUES ({template.Template}, {(uint)template.Class})");
                Execute(worldConnection, $"INSERT INTO itemtemplate_requirement_skill (id, skill_id, skill_level) VALUES ({template.Template}, {template.Skill}, 1)");
                Execute(worldConnection, $"INSERT INTO itemtemplate_requirement (id, req_type, req_value) VALUES ({(uint)template.Class}, 1, 5)");
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

            foreach (var template in Gear)
                RegisterClass(template.Class, template.MaxHitPoints, template.WeaponClass);

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

        private void RegisterClass(EntityClasses classId, int maxHitPoints, WeaponClassEntry weapon)
        {
            _previousClasses[classId] = EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(classId, out var previous) ? previous : null;
            var augmentations = weapon == null
                ? new List<AugmentationType> { AugmentationType.Armor, AugmentationType.Equipable, AugmentationType.Item }
                : new List<AugmentationType> { AugmentationType.Weapon, AugmentationType.Equipable, AugmentationType.Item };
            EntityClassManager.Instance.LoadedEntityClasses[classId] = new EntityClass((uint)classId, "stand-in for the world-seed class", 0, 0,
                augmentations, false)
            {
                ItemClassInfo = new ItemClassInfo(new ItemClassEntry { Id = (uint)classId, MaxHitPoints = maxHitPoints, StackSize = 1, LootValue = 500 }),
                WeaponClassInfo = weapon == null ? null : new WeaponClassInfo(weapon)
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
