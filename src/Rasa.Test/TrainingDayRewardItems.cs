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
    /// Loads the Training Day reward pistols (116929 Vextronics Pistol, 116930 Vextronics Pulse Pistol) through the real
    /// <see cref="ItemManager.LoadItemTemplates"/> from a world database migrated through WildernessArrivalTrainingDay, which
    /// supplies their itemtemplate and itemtemplate_weapon rows. Unit tests do not replay the world seed, so the original seed
    /// rows those templates hang on are stood in for with their deployed values: the template-to-class map
    /// (itemtemplate_itemclass 116929 -> 27121, 116930 -> 27100), the Firearms 1 skill requirement, the class-keyed level-5
    /// requirement, and the item and weapon classes 27121/27100 (itemclass, weaponclass, equipableclass rows), which the
    /// entity-class seed would load. Dispose restores the previous ItemManager and entity classes.
    /// </summary>
    public sealed class TrainingDayRewardItems : IDisposable
    {
        public const uint Pistol = 116929;
        public const uint PulsePistol = 116930;
        public const EntityClasses PistolClass = (EntityClasses)27121;
        public const EntityClasses PulsePistolClass = (EntityClasses)27100;

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

        /// <summary>Inserts the original world-seed rows of the two templates that the migration does not own.</summary>
        public static void SeedOriginalTemplateRows(SqliteConnection worldConnection)
        {
            Execute(worldConnection, "INSERT INTO itemtemplate_itemclass (itemTemplateId, itemClassId) VALUES (116929, 27121), (116930, 27100)");
            Execute(worldConnection, "INSERT INTO itemtemplate_requirement_skill (id, skill_id, skill_level) VALUES (116929, 1, 1), (116930, 1, 1)");
            Execute(worldConnection, "INSERT INTO itemtemplate_requirement (id, req_type, req_value) VALUES (27121, 1, 5), (27100, 1, 5)");
        }

        public TrainingDayRewardItems(SqliteConnection worldConnection)
        {
            var field = typeof(ItemManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            _previousItemManager = field.GetValue(null);

            // itemclass / weaponclass rows of 27121 and 27100 as deployed (max_hp 120, stack 1; ammo 3147 clip 20 91 physical,
            // ammo 3807 clip 10 106 EMP; both attack action 1 with arguments 133/135).
            RegisterClass(PistolClass, new WeaponClassEntry { Id = 27121, WeaponTemplatId = 1, AttackActionId = 1, AttackActionArgId = 133, DrawActionId = 1, StowActionId = 1, ReloadActionId = 1, AmmoClassId = 3147, ClipSize = 20, MinDamage = 91, MaxDamage = 91, DamageType = 1, WeaponAnimConditionCode = 1 });
            RegisterClass(PulsePistolClass, new WeaponClassEntry { Id = 27100, WeaponTemplatId = 77, AttackActionId = 1, AttackActionArgId = 135, DrawActionId = 1, StowActionId = 1, ReloadActionId = 49, AmmoClassId = 3807, ClipSize = 10, MinDamage = 106, MaxDamage = 106, DamageType = 5, WeaponAnimConditionCode = 1 });

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

        private void RegisterClass(EntityClasses classId, WeaponClassEntry weapon)
        {
            _previousClasses[classId] = EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(classId, out var previous) ? previous : null;
            EntityClassManager.Instance.LoadedEntityClasses[classId] = new EntityClass((uint)classId, "stand-in for the world-seed class", 0, 0,
                new List<AugmentationType> { AugmentationType.Weapon, AugmentationType.Equipable, AugmentationType.Item }, false)
            {
                ItemClassInfo = new ItemClassInfo(new ItemClassEntry { Id = (uint)classId, MaxHitPoints = 120, StackSize = 1, LootValue = 500 }),
                WeaponClassInfo = new WeaponClassInfo(weapon)
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
