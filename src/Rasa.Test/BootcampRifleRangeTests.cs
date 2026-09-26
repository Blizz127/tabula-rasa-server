using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Memory;
using Rasa.Packets.MapChannel.Server;
using Rasa.Services.Preloader;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Test.Reconstruction;

namespace Rasa.Test
{
    [TestClass]
    public class BootcampRifleRangeTests
    {
        private static T SeedRow<T>(IPreloader preloader, uint id) where T : new()
        {
            var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            preloader.Preload(builder);
            foreach (var operation in builder.Operations.OfType<InsertDataOperation>())
                for (var row = 0; row < operation.Values.GetLength(0); row++)
                {
                    if (Convert.ToUInt32(operation.Values[row, 0]) != id)
                        continue;
                    var entry = new T();
                    foreach (var property in typeof(T).GetProperties())
                    {
                        var column = property.GetCustomAttribute<ColumnAttribute>()?.Name;
                        var index = Array.IndexOf(operation.Columns, column);
                        if (index >= 0)
                            property.SetValue(entry, Convert.ChangeType(operation.Values[row, index], property.PropertyType));
                    }
                    return entry;
                }
            throw new AssertFailedException($"Seed row {id} was not found");
        }

        [TestMethod]
        public void OriginalRifleClassActionAndObservedRangeAgree()
        {
            var mapping = SeedRow<ItemTemplateItemClassEntry>(new ItemTemplateItemClassPreloader(), 13713);
            var weapon = SeedRow<WeaponClassEntry>(new WeaponClassPreloader(), mapping.ItemClass);
            Assert.AreEqual(27220u, mapping.ItemClass);
            Assert.AreEqual((1u, 134u, 106, 106, 20u),
                (weapon.AttackActionId, weapon.AttackActionArgId, weapon.MinDamage, weapon.MaxDamage, weapon.ClipSize));
            var timing = WeaponAttackData.Get((ActionId)weapon.AttackActionId, weapon.AttackActionArgId);
            using var evidence = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("bootcamp-rifle-range.json")));
            Assert.AreEqual(evidence.RootElement.GetProperty("correction").GetProperty("range_meters").GetInt32(), timing.MaxRange);
            Assert.AreEqual(60, timing.MaxRange);
        }

        [TestMethod]
        public void BothProviderMigrationsDescribeOnlyRifleRangeUpAndDown()
        {
            foreach (var migration in new Migration[]
            {
                new Rasa.Migrations.SqliteWorld.BootcampCrateRifleRange(),
                new Rasa.Migrations.MySqlWorld.BootcampCrateRifleRange()
            })
            {
                Assert.IsNotNull(migration.TargetModel.FindEntityType(typeof(ItemTemplateWeaponEntry)));
                void Check(IReadOnlyList<MigrationOperation> operations, uint expected)
                {
                    Assert.AreEqual(1, operations.Count);
                    var operation = (UpdateDataOperation)operations.Single();
                    Assert.AreEqual("itemtemplate_weapon", operation.Table);
                    CollectionAssert.AreEqual(new[] { "id" }, operation.KeyColumns);
                    CollectionAssert.AreEqual(new[] { "range" }, operation.Columns);
                    Assert.AreEqual(1, operation.Values.Length);
                    Assert.AreEqual(1, operation.KeyValues.Length);
                    Assert.AreEqual(13713u, operation.KeyValues[0, 0]);
                    Assert.AreEqual(expected, operation.Values[0, 0]);
                }
                Check(migration.UpOperations, 60);
                Check(migration.DownOperations, 80);
            }
        }

        [TestMethod]
        public void MigratedRifleTooltipWritesSixtyMetresWithoutChangingMeleeDamage()
        {
            var mapping = SeedRow<ItemTemplateItemClassEntry>(new ItemTemplateItemClassPreloader(), 13713);
            var weapon = SeedRow<ItemTemplateWeaponEntry>(new ItemTemplateWeaponPreloader(), 13713);
            Assert.AreEqual((80u, 25u), (weapon.Range, weapon.AltMaxDamage)); // Previous seed reproduced the 80m/25 capture.
            var migration = new Rasa.Migrations.SqliteWorld.BootcampCrateRifleRange();
            weapon.Range = (uint)((UpdateDataOperation)migration.UpOperations.Single()).Values[0, 0];
            var template = new ItemTemplate(mapping) { WeaponInfo = new WeaponInfo(weapon) };
            var entityClass = new EntityClass(mapping.ItemClass, "fixture", 0, 0,
                new List<AugmentationType> { AugmentationType.Weapon }, false)
            {
                WeaponClassInfo = new WeaponClassInfo(SeedRow<WeaponClassEntry>(new WeaponClassPreloader(), mapping.ItemClass))
            };
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            new ItemTemplateTooltipInfoPacket(template, entityClass).Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(13713u, reader.ReadUInt());
            Assert.AreEqual(27220u, reader.ReadUInt());
            Assert.AreEqual(1, reader.ReadDictionary());
            Assert.AreEqual((int)AugmentationType.Weapon, reader.ReadInt());
            Assert.AreEqual(16, reader.ReadTuple());
            Assert.AreEqual(106, reader.ReadInt());
            Assert.AreEqual(106, reader.ReadInt());
            Assert.AreEqual(3147u, reader.ReadUInt()); // Genuine class27220 ammo class, not a no-ammo fixture.
            Assert.AreEqual(20u, reader.ReadUInt());
            Assert.AreEqual(1u, reader.ReadUInt());
            Assert.AreEqual(1, reader.ReadInt());
            for (var index = 6; index < 10; index++) reader.ReadUInt();
            Assert.AreEqual(60u, reader.ReadUInt()); // Original GetWeaponRange reads weapon tuple index10.
            reader.ReadUInt();
            reader.ReadNoneStruct();
            Assert.AreEqual(5, reader.ReadTuple());
            Assert.AreEqual(25u, reader.ReadUInt()); // Observed82 remains a separate reconstruction gap.
            Assert.AreEqual(weapon.AltDamageType, reader.ReadUInt());
            Assert.AreEqual(weapon.AltRange, reader.ReadUInt());
            Assert.AreEqual(weapon.AltAeRadius, reader.ReadUInt());
            Assert.AreEqual(weapon.AltAeType, reader.ReadUInt());
            Assert.AreEqual((int)weapon.AttackType, reader.ReadInt());
            Assert.AreEqual((int)weapon.ToolType, reader.ReadInt());
            Assert.AreEqual(stream.Length, stream.Position);
        }
    }
}
