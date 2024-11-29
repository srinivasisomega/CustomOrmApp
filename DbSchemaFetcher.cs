using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

namespace CustomOrmApp
{
    

    public class DbSchemaFetcher
    {
        private readonly string _connectionString;

        public DbSchemaFetcher(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<TableSchema> GetTables()
        {
            var tables = new List<TableSchema>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Get all tables
                var tablesQuery = @"
                SELECT TABLE_NAME 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_TYPE = 'BASE TABLE'";

                using (var command = new SqlCommand(tablesQuery, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tables.Add(new TableSchema { TableName = reader["TABLE_NAME"].ToString() });
                    }
                }

                // Get all columns
                foreach (var table in tables)
                {
                    var columnsQuery = $@"
                    SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = '{table.TableName}'";

                    using (var command = new SqlCommand(columnsQuery, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            table.Columns.Add(new ColumnSchema
                            {
                                ColumnName = reader["COLUMN_NAME"].ToString(),
                                DataType = reader["DATA_TYPE"].ToString(),
                                IsNullable = reader["IS_NULLABLE"].ToString() == "YES"
                            });
                        }
                    }
                }
            }

            return tables;
        }
    }

}
