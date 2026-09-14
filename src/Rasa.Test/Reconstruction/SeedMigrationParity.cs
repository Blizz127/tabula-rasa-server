using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Test.Reconstruction
{
    /// <summary>
    /// Compares the SQLite and MySQL classes of each boot-camp data migration by running
    /// their protected Up/Down against provider-specific <see cref="MigrationBuilder"/>s.
    /// Data operations must match table, columns and values exactly; raw SQL and any
    /// non-data operation are rejected (build plan section 1.7, SeedMigrationParityTests).
    /// </summary>
    public static class SeedMigrationParity
    {
        public const string SqliteNamespace = "Rasa.Migrations.SqliteWorld";
        public const string MySqlNamespace = "Rasa.Migrations.MySqlWorld";
        public const string SqliteProvider = "Microsoft.EntityFrameworkCore.Sqlite";
        public const string MySqlProvider = "Pomelo.EntityFrameworkCore.MySql";
        public const string BootcampPrefix = "Bootcamp";
        public const string WildernessPrefix = "Wilderness";

        public sealed class MigrationPair
        {
            public MigrationPair(string name, Type sqlite, Type mySql)
            {
                Name = name;
                Sqlite = sqlite;
                MySql = mySql;
            }

            public string Name { get; }
            public Type Sqlite { get; }
            public Type MySql { get; }
        }

        public sealed class Discovery
        {
            public List<MigrationPair> Pairs { get; } = new List<MigrationPair>();
            public List<string> Errors { get; } = new List<string>();
        }

        public static Discovery Discover(Assembly assembly, string sqliteNamespace, string mySqlNamespace, string prefix)
        {
            var migrations = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(Migration).IsAssignableFrom(t)
                            && t.Name.StartsWith(prefix, StringComparison.Ordinal))
                .ToList();
            var sqlite = migrations.Where(t => t.Namespace == sqliteNamespace).ToDictionary(t => t.Name, StringComparer.Ordinal);
            var mySql = migrations.Where(t => t.Namespace == mySqlNamespace).ToDictionary(t => t.Name, StringComparer.Ordinal);

            var discovery = new Discovery();
            foreach (var name in sqlite.Keys.Union(mySql.Keys).OrderBy(n => n, StringComparer.Ordinal))
            {
                if (!mySql.ContainsKey(name))
                    discovery.Errors.Add($"{name}: {sqliteNamespace}.{name} has no {mySqlNamespace} counterpart");
                else if (!sqlite.ContainsKey(name))
                    discovery.Errors.Add($"{name}: {mySqlNamespace}.{name} has no {sqliteNamespace} counterpart");
                else
                    discovery.Pairs.Add(new MigrationPair(name, sqlite[name], mySql[name]));
            }
            return discovery;
        }

        public static List<string> Compare(MigrationPair pair)
        {
            var errors = new List<string>();
            foreach (var direction in new[] { "Up", "Down" })
            {
                var sqlite = Operations(pair.Sqlite, direction, SqliteProvider, errors);
                var mySql = Operations(pair.MySql, direction, MySqlProvider, errors);
                if (sqlite == null || mySql == null)
                    continue;

                var label = $"{pair.Name}.{direction}";
                RejectNonDataOperations(label, "SQLite", sqlite, errors);
                RejectNonDataOperations(label, "MySQL", mySql, errors);

                if (sqlite.Count != mySql.Count)
                    errors.Add($"{label}: SQLite has {sqlite.Count} operations, MySQL has {mySql.Count}");
                for (var i = 0; i < Math.Min(sqlite.Count, mySql.Count); i++)
                    CompareOperation($"{label} operation {i}", sqlite[i], mySql[i], errors);
            }
            return errors;
        }

        public static IReadOnlyList<MigrationOperation> Operations(Type migrationType, string direction, string provider, List<string> errors)
        {
            var method = typeof(Migration).GetMethod(direction, BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(MigrationBuilder) }, null)
                         ?? throw new MissingMethodException(typeof(Migration).FullName, direction);
            var builder = new MigrationBuilder(provider);
            try
            {
                var migration = (Migration)Activator.CreateInstance(migrationType);
                method.Invoke(migration, new object[] { builder });
            }
            catch (TargetInvocationException e)
            {
                errors.Add($"{migrationType.FullName}.{direction} threw {e.InnerException?.GetType().Name}: {e.InnerException?.Message}");
                return null;
            }
            return builder.Operations;
        }

        private static void RejectNonDataOperations(string label, string provider, IReadOnlyList<MigrationOperation> operations, List<string> errors)
        {
            for (var i = 0; i < operations.Count; i++)
            {
                switch (operations[i])
                {
                    case SqlOperation _:
                        errors.Add($"{label} operation {i}: raw Sql is forbidden in boot-camp data migrations ({provider})");
                        break;
                    case InsertDataOperation _:
                    case UpdateDataOperation _:
                    case DeleteDataOperation _:
                        break;
                    default:
                        errors.Add($"{label} operation {i}: {operations[i].GetType().Name} is not a data operation ({provider}); boot-camp data migrations use InsertData, UpdateData and DeleteData only");
                        break;
                }
            }
        }

        private static void CompareOperation(string label, MigrationOperation sqlite, MigrationOperation mySql, List<string> errors)
        {
            if (sqlite.GetType() != mySql.GetType())
            {
                errors.Add($"{label}: SQLite {sqlite.GetType().Name} != MySQL {mySql.GetType().Name}");
                return;
            }

            switch (sqlite)
            {
                case InsertDataOperation s:
                {
                    var m = (InsertDataOperation)mySql;
                    CompareTable(label, s.Schema, s.Table, m.Schema, m.Table, errors);
                    CompareColumns(label, "columns", s.Columns, m.Columns, errors);
                    CompareValues(label, "values", s.Columns, s.Values, m.Values, errors);
                    break;
                }
                case UpdateDataOperation s:
                {
                    var m = (UpdateDataOperation)mySql;
                    CompareTable(label, s.Schema, s.Table, m.Schema, m.Table, errors);
                    CompareColumns(label, "key columns", s.KeyColumns, m.KeyColumns, errors);
                    CompareValues(label, "key values", s.KeyColumns, s.KeyValues, m.KeyValues, errors);
                    CompareColumns(label, "columns", s.Columns, m.Columns, errors);
                    CompareValues(label, "values", s.Columns, s.Values, m.Values, errors);
                    break;
                }
                case DeleteDataOperation s:
                {
                    var m = (DeleteDataOperation)mySql;
                    CompareTable(label, s.Schema, s.Table, m.Schema, m.Table, errors);
                    CompareColumns(label, "key columns", s.KeyColumns, m.KeyColumns, errors);
                    CompareValues(label, "key values", s.KeyColumns, s.KeyValues, m.KeyValues, errors);
                    break;
                }
            }
        }

        private static void CompareTable(string label, string sqliteSchema, string sqliteTable, string mySqlSchema, string mySqlTable, List<string> errors)
        {
            if (sqliteSchema != mySqlSchema || sqliteTable != mySqlTable)
                errors.Add($"{label}: table SQLite '{Qualified(sqliteSchema, sqliteTable)}' != MySQL '{Qualified(mySqlSchema, mySqlTable)}'");
        }

        private static string Qualified(string schema, string table) => schema == null ? table : $"{schema}.{table}";

        private static void CompareColumns(string label, string what, string[] sqlite, string[] mySql, List<string> errors)
        {
            if (!(sqlite ?? Array.Empty<string>()).SequenceEqual(mySql ?? Array.Empty<string>(), StringComparer.Ordinal))
                errors.Add($"{label}: {what} SQLite [{string.Join(", ", sqlite ?? Array.Empty<string>())}] != MySQL [{string.Join(", ", mySql ?? Array.Empty<string>())}]");
        }

        private static void CompareValues(string label, string what, string[] columns, object[,] sqlite, object[,] mySql, List<string> errors)
        {
            if (sqlite == null || mySql == null)
            {
                if (sqlite != mySql)
                    errors.Add($"{label}: {what} present for only one provider");
                return;
            }
            if (sqlite.GetLength(0) != mySql.GetLength(0) || sqlite.GetLength(1) != mySql.GetLength(1))
            {
                errors.Add($"{label}: {what} shape SQLite {sqlite.GetLength(0)}x{sqlite.GetLength(1)} != MySQL {mySql.GetLength(0)}x{mySql.GetLength(1)}");
                return;
            }
            for (var row = 0; row < sqlite.GetLength(0); row++)
            {
                for (var column = 0; column < sqlite.GetLength(1); column++)
                {
                    var s = sqlite[row, column];
                    var m = mySql[row, column];
                    if (ValuesEqual(s, m))
                        continue;
                    var name = columns != null && column < columns.Length ? columns[column] : column.ToString(CultureInfo.InvariantCulture);
                    errors.Add($"{label}: {what} row {row} column '{name}' SQLite {Describe(s)} != MySQL {Describe(m)}");
                }
            }
        }

        private static bool ValuesEqual(object sqlite, object mySql)
        {
            if (sqlite == null || mySql == null)
                return sqlite == null && mySql == null;
            if (sqlite.GetType() != mySql.GetType())
                return false;
            if (sqlite is IStructuralEquatable structural)
                return structural.Equals(mySql, StructuralComparisons.StructuralEqualityComparer);
            return sqlite.Equals(mySql);
        }

        private static string Describe(object value)
        {
            if (value == null)
                return "null";
            var text = value switch
            {
                double d => d.ToString("R", CultureInfo.InvariantCulture),
                float f => f.ToString("R", CultureInfo.InvariantCulture),
                string s => $"\"{s}\"",
                _ => Convert.ToString(value, CultureInfo.InvariantCulture)
            };
            return $"{text} ({value.GetType().Name})";
        }
    }
}
