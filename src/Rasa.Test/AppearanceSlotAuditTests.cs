using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test
{
    /// <summary>
    /// Every outfit row is filed under the slot the client puts that class in.
    ///
    /// The client's own <c>equipmentdata.equipableClassEquipmentSlot</c> gives each equipable class exactly one
    /// slot (<c>clientdata/equipment-slots.csv</c>), and the slot key is what the client reads to find a face, a
    /// weapon and an accessory effect - only the mesh placement survives a wrong one. 421 rows over 159 NPCs
    /// were wrong before <c>AppearanceSlotRepair</c>, all of them copies of three shipped donor bodies taken as
    /// the analogue outfit for a created NPC and carried forward batch after batch.
    ///
    /// This is the check that stops the next batch doing it again: it reads the world, not a migration, so it
    /// covers the preloaders and every migration at once.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AppearanceSlotAuditTests
    {
        /// <summary>
        /// Rows the client's table cannot judge, with the reason. Creature 8 is a Bane war machine wearing a
        /// human face swap: the class is a real one and FACE is the right key for it, but
        /// <c>equipableClassMeshSwapset</c> is keyed (class, mesh) and carries only the six human and Thrax
        /// avatar bodies, so nothing can render on mesh 15868 whatever the key says.
        /// </summary>
        private static readonly Dictionary<string, string> Allowed = new();

        [TestMethod]
        public void EveryOutfitRowIsInTheSlotTheClientGivesItsClass()
        {
            var root = RepositoryRoot();
            var slots = ReadSlots(Path.Combine(root, "clientdata", "equipment-slots.csv"));
            Assert.IsTrue(slots.Count > 6000, $"the client's slot table should be whole, read {slots.Count}");

            using var connection = OpenWorld(root);
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT a.id, a.slot_id, a.Class_id, COALESCE(c.comment, '') " +
                "FROM creature_appearance a LEFT JOIN creature c ON c.id = a.id ORDER BY a.id, a.slot_id";
            using var reader = command.ExecuteReader();

            var wrong = new List<string>();
            var unknown = new List<string>();
            var rows = 0;

            while (reader.Read())
            {
                var creature = reader.GetInt64(0);
                var slot = reader.GetInt64(1);
                var entityClass = reader.GetInt64(2);
                rows++;

                if (!slots.TryGetValue(entityClass, out var expected))
                {
                    unknown.Add($"creature {creature} slot {slot} wears class {entityClass}, which the client has no slot for - {reader.GetString(3)}");
                    continue;
                }

                if (slot == expected || Allowed.ContainsKey($"{creature}:{slot}")) continue;

                wrong.Add(string.Format(CultureInfo.InvariantCulture,
                    "creature {0} has class {1} in slot {2}; the client puts it in {3} - {4}",
                    creature, entityClass, slot, expected, reader.GetString(3)));
            }

            Assert.IsTrue(rows > 3000, $"the world should have its outfits, read {rows}");
            Assert.AreEqual(0, unknown.Count, "outfit rows whose class the client does not know:\n" + string.Join("\n", unknown));
            Assert.AreEqual(0, wrong.Count,
                $"{wrong.Count} outfit rows are in the wrong slot:\n" + string.Join("\n", wrong.Take(40)));
        }

        /// <summary>No creature may carry the same slot twice: the client keeps one class per key.</summary>
        [TestMethod]
        public void NoCreatureCarriesTheSameSlotTwice()
        {
            using var connection = OpenWorld(RepositoryRoot());
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT id, slot_id, COUNT(*) FROM creature_appearance GROUP BY id, slot_id HAVING COUNT(*) > 1";
            using var reader = command.ExecuteReader();
            var duplicates = new List<string>();
            while (reader.Read())
                duplicates.Add($"creature {reader.GetInt64(0)} has {reader.GetInt64(2)} rows in slot {reader.GetInt64(1)}");
            Assert.AreEqual(0, duplicates.Count, string.Join("\n", duplicates));
        }

        private static Dictionary<long, long> ReadSlots(string path)
        {
            var slots = new Dictionary<long, long>();
            foreach (var line in File.ReadLines(path).Skip(1))
            {
                var parts = line.Split(',');
                if (parts.Length < 2) continue;
                slots[long.Parse(parts[0], CultureInfo.InvariantCulture)] = long.Parse(parts[1], CultureInfo.InvariantCulture);
            }
            return slots;
        }

        private static string RepositoryRoot()
        {
            var directory = AppContext.BaseDirectory;
            while (directory != null && !Directory.Exists(Path.Combine(directory, "navmesh")))
                directory = Path.GetDirectoryName(directory.TrimEnd(Path.DirectorySeparatorChar));
            Assert.IsNotNull(directory, "the repository root carries the navmesh folder");
            return directory;
        }

        private static SqliteConnection OpenWorld(string root)
        {
            var path = Path.Combine(root, "rasaworld.db");
            if (!File.Exists(path) || new FileInfo(path).Length == 0)
                Assert.Inconclusive("rasaworld.db is not in the repository root; this audit reads the world database");
            var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly");
            connection.Open();
            return connection;
        }
    }
}
