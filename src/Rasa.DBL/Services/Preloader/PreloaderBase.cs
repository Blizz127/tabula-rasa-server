using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    public abstract class PreloaderBase
    {
        protected void Insert(MigrationBuilder migrationBuilder, string tableName, Type entityType)
        {
            var columns = GetColumnNames(entityType);
            var rows = GetRows().ToList();
            var innerLength = rows.First().Length;
            // SQLite prepares each statement from the remaining command text. One
            // 30,225-row operation causes repeated conversion of a huge SQL tail.
            // Bound each command while retaining the migration's transaction,
            // original row order, columns and values for both providers.
            const int batchSize = 256;
            for (var start = 0; start < rows.Count; start += batchSize)
            {
                var count = Math.Min(batchSize, rows.Count - start);
                var values = new object[count, innerLength];
                for (var row = 0; row < count; row++)
                    for (var column = 0; column < innerLength; column++)
                        values[row, column] = rows[start + row][column];
                migrationBuilder.InsertData(tableName, columns, values);
            }
        }
        private string[] GetColumnNames(Type entityType)
        {
            var publicProperties = entityType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var columns = publicProperties.SelectMany(p => p.GetCustomAttributes<ColumnAttribute>());
            var columnNames = columns.Select(c => c.Name)
                .Where(name => !string.IsNullOrEmpty(name));
            return columnNames.ToArray();
        }

        protected abstract IEnumerable<object[]> GetRows();
    }
}
