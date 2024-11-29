using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomOrmApp
{
    public class CodeGenerator
    {
        private readonly string _outputFolder;

        public CodeGenerator(string outputFolder)
        {
            _outputFolder = outputFolder;
            if (!Directory.Exists(_outputFolder))
            {
                Directory.CreateDirectory(_outputFolder);
            }
        }

        public void GenerateModels(List<TableSchema> tables)
        {
            foreach (var table in tables)
            {
                var className = table.TableName;
                var filePath = Path.Combine(_outputFolder, $"{className}.cs");

                var code = new StringBuilder();
                code.AppendLine("using System;");
                code.AppendLine("using System.ComponentModel.DataAnnotations;");
                code.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
                code.AppendLine();
                code.AppendLine($"[Table(\"{table.TableName}\")]");
                code.AppendLine($"public class {className}");
                code.AppendLine("{");

                foreach (var column in table.Columns)
                {
                    if (column.ColumnName == "Id")
                    {
                        code.AppendLine("    [Key]");
                    }

                    if (column.IsNullable)
                    {
                        code.AppendLine($"    public {MapToCSharpType(column.DataType)}? {column.ColumnName} {{ get; set; }}");
                    }
                    else
                    {
                        code.AppendLine($"    public {MapToCSharpType(column.DataType)} {column.ColumnName} {{ get; set; }}");
                    }
                }

                code.AppendLine("}");
                File.WriteAllText(filePath, code.ToString());
            }
        }

        public void GenerateDbContext(List<TableSchema> tables, string namespaceName)
        {
            var dbContextPath = Path.Combine(_outputFolder, "AppDbContext.cs");

            var code = new StringBuilder();
            code.AppendLine("using Microsoft.EntityFrameworkCore;");
            code.AppendLine();
            code.AppendLine($"namespace {namespaceName}");
            code.AppendLine("{");
            code.AppendLine("    public class AppDbContext : DbContext");
            code.AppendLine("    {");
            code.AppendLine("        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}");
            code.AppendLine();

            foreach (var table in tables)
            {
                code.AppendLine($"        public DbSet<{table.TableName}> {table.TableName}s {{ get; set; }}");
            }

            code.AppendLine("    }");
            code.AppendLine("}");
            File.WriteAllText(dbContextPath, code.ToString());
        }

        private string MapToCSharpType(string sqlType)
        {
            return sqlType switch
            {
                "int" => "int",
                "bigint" => "long",
                "smallint" => "short",
                "tinyint" => "byte",
                "bit" => "bool",
                "decimal" => "decimal",
                "numeric" => "decimal",
                "float" => "double",
                "real" => "float",
                "date" => "DateTime",
                "datetime" => "DateTime",
                "datetime2" => "DateTime",
                "char" => "string",
                "varchar" => "string",
                "text" => "string",
                "nvarchar" => "string",
                "nchar" => "string",
                "ntext" => "string",
                "binary" => "byte[]",
                "varbinary" => "byte[]",
                _ => "object"
            };
        }
    }

}
