using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Reflection;
namespace CustomOrmApp
{
    public class CustomDbContext : IDisposable
    {
        private readonly SqlConnection _connection;
        private readonly Dictionary<string, List<(string ColumnName, string DataType, bool IsPrimaryKey)>> _entityMappings;

        public CustomDbContext(string connectionString)
        {
            _connection = new SqlConnection(connectionString);
            _entityMappings = new Dictionary<string, List<(string ColumnName, string DataType, bool IsPrimaryKey)>>();
            InitializeMappings();
        }

        private void InitializeMappings()
        {
            // Use reflection to gather metadata for all entity types
            var entityTypes = Assembly.GetExecutingAssembly()
                                      .GetTypes()
                                      .Where(t => t.IsClass && t.BaseType == typeof(BaseEntity));

            foreach (var entityType in entityTypes)
            {
                var tableName = entityType.Name;
                var columns = new List<(string ColumnName, string DataType, bool IsPrimaryKey)>();

                foreach (var property in entityType.GetProperties())
                {
                    var columnName = property.Name;
                    var dataType = MapToSqlType(property.PropertyType);
                    var isPrimaryKey = property.Name == "Id"; // Assumes "Id" is the primary key by convention

                    columns.Add((columnName, dataType, isPrimaryKey));
                }

                _entityMappings[tableName] = columns;
            }
        }

        private string MapToSqlType(Type type)
        {
            // Map C# types to SQL Server types
            return type.Name switch
            {
                "Int32" => "INT",
                "String" => "NVARCHAR(MAX)",
                "Decimal" => "DECIMAL(18,2)",
                "DateTime" => "DATETIME",
                "Boolean" => "BIT",
                _ => "NVARCHAR(MAX)" // Default to NVARCHAR(MAX) for unsupported types
            };
        }
        private void UpdateTableSchema(SqlConnection connection, string tableName, List<(string ColumnName, string DataType, bool IsPrimaryKey)> modelColumns)
        {
            // Get existing columns in the database
            var existingColumnsQuery = $@"
    SELECT COLUMN_NAME, DATA_TYPE 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = '{tableName}'";

            var existingColumns = new Dictionary<string, string>();

            using (var command = new SqlCommand(existingColumnsQuery, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var columnName = reader["COLUMN_NAME"].ToString();
                    var dataType = reader["DATA_TYPE"].ToString();
                    existingColumns[columnName] = dataType;
                }
            }

            // Collect model column names for comparison
            var modelColumnNames = modelColumns.Select(c => c.ColumnName).ToHashSet();

            // Drop columns that exist in the database but not in the model
            foreach (var existingColumn in existingColumns.Keys)
            {
                if (!modelColumnNames.Contains(existingColumn))
                {
                    var dropColumnQuery = $"ALTER TABLE [{tableName}] DROP COLUMN [{existingColumn}]";
                    using (var dropColumnCommand = new SqlCommand(dropColumnQuery, connection))
                    {
                        dropColumnCommand.ExecuteNonQuery();
                    }
                }
            }

            // Add or update columns to match the model
            foreach (var column in modelColumns)
            {
                if (!existingColumns.ContainsKey(column.ColumnName))
                {
                    // Add new column
                    var addColumnQuery = $"ALTER TABLE [{tableName}] ADD [{column.ColumnName}] {column.DataType}";
                    using (var addColumnCommand = new SqlCommand(addColumnQuery, connection))
                    {
                        addColumnCommand.ExecuteNonQuery();
                    }
                }
                else if (existingColumns[column.ColumnName] != column.DataType)
                {
                    // Update column type (if needed)
                    var alterColumnQuery = $"ALTER TABLE [{tableName}] ALTER COLUMN [{column.ColumnName}] {column.DataType}";
                    using (var alterColumnCommand = new SqlCommand(alterColumnQuery, connection))
                    {
                        alterColumnCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        public void Migrate()
        {
            if (_connection.State != System.Data.ConnectionState.Open)
                _connection.Open();

            foreach (var table in _entityMappings)
            {
                var tableName = table.Key;

                // Check if the table exists
                var tableExistsQuery = $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}'";
                using (var command = new SqlCommand(tableExistsQuery, _connection))
                {
                    var tableExists = (int)command.ExecuteScalar() > 0;

                    if (!tableExists)
                    {
                        // Create the table if it doesn't exist
                        var createTableCommand = $@"
                    CREATE TABLE [{tableName}] (
                        {GenerateColumnsSql(table.Value)}
                    )";
                        using (var createCommand = new SqlCommand(createTableCommand, _connection))
                        {
                            createCommand.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // Update the table schema to match the model
                        UpdateTableSchema(_connection, tableName, table.Value);
                    }
                }
            }
        }

        private string GenerateColumnsSql(List<(string ColumnName, string DataType, bool IsPrimaryKey)> columns)
        {
            var columnDefinitions = new List<string>();

            foreach (var column in columns)
            {
                string definition = $"[{column.ColumnName}] {column.DataType}";

                if (column.IsPrimaryKey)
                    definition += " PRIMARY KEY IDENTITY(1,1)"; // Add IDENTITY for primary key

                columnDefinitions.Add(definition);
            }

            return string.Join(", ", columnDefinitions);
        }

        public DbSet<T> Set<T>() where T : BaseEntity, new()
        {
            return new DbSet<T>(_connection);
        }

        public void Dispose()
        {
            if (_connection.State == System.Data.ConnectionState.Open)
            {
                _connection.Close();
            }
            _connection.Dispose();
        }
    }

}
