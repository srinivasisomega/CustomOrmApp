using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
namespace CustomOrmApp
{
    public class DbSet<T> where T : BaseEntity, new()
    {
        private readonly SqlConnection _connection;

        public DbSet(SqlConnection connection)
        {
            _connection = connection;
        }

        public void Add(T entity)
        {
            var tableName = $"[{typeof(T).Name}]"; // Escape table name with square brackets
            var properties = typeof(T).GetProperties().Where(p => p.Name != "Id");
            var columns = string.Join(", ", properties.Select(p => $"[{p.Name}]")); // Escape column names
            var values = string.Join(", ", properties.Select(p => $"@{p.Name}"));

            var query = $"INSERT INTO {tableName} ({columns}) VALUES ({values}); SELECT SCOPE_IDENTITY();";

            using (var command = new SqlCommand(query, _connection))
            {
                if (_connection.State == System.Data.ConnectionState.Closed)
                {
                    _connection.Open();
                }

                foreach (var prop in properties)
                {
                    command.Parameters.AddWithValue($"@{prop.Name}", prop.GetValue(entity) ?? DBNull.Value);
                }

                var id = command.ExecuteScalar();
                entity.Id = Convert.ToInt32(id);
            }
        }

        public IEnumerable<T> GetAll()
        {
            var tableName = $"[{typeof(T).Name}]"; // Escape table name
            var query = $"SELECT * FROM {tableName}";
            var result = new List<T>();

            using (var command = new SqlCommand(query, _connection))
            {
                if (_connection.State == System.Data.ConnectionState.Closed)
                {
                    _connection.Open();
                }

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var entity = new T();
                        foreach (var prop in typeof(T).GetProperties())
                        {
                            var value = reader[prop.Name];
                            prop.SetValue(entity, value == DBNull.Value ? null : value);
                        }
                        result.Add(entity);
                    }
                }
            }

            return result;
        }

        public void Update(T entity)
        {
            var tableName = $"[{typeof(T).Name}]"; // Escape table name
            var properties = typeof(T).GetProperties().Where(p => p.Name != "Id");
            var setClause = string.Join(", ", properties.Select(p => $"[{p.Name}] = @{p.Name}")); // Escape column names

            var query = $"UPDATE {tableName} SET {setClause} WHERE Id = @Id";

            using (var command = new SqlCommand(query, _connection))
            {
                if (_connection.State == System.Data.ConnectionState.Closed)
                {
                    _connection.Open();
                }

                foreach (var prop in properties)
                {
                    command.Parameters.AddWithValue($"@{prop.Name}", prop.GetValue(entity) ?? DBNull.Value);
                }
                command.Parameters.AddWithValue("@Id", entity.Id);
                command.ExecuteNonQuery();
            }
        }

        public void Delete(T entity)
        {
            var tableName = $"[{typeof(T).Name}]"; // Escape table name
            var query = $"DELETE FROM {tableName} WHERE Id = @Id";

            using (var command = new SqlCommand(query, _connection))
            {
                if (_connection.State == System.Data.ConnectionState.Closed)
                {
                    _connection.Open();
                }

                command.Parameters.AddWithValue("@Id", entity.Id);
                command.ExecuteNonQuery();
            }
        }

    }

}
