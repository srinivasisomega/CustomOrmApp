using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace CustomOrmApp
{
    public class User : BaseEntity
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    public class Product : BaseEntity
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "Server=COGNINE-L105;Database=bb2;Trusted_Connection=True;Trust Server Certificate=True;";

            using (var context = new CustomDbContext(connectionString))
            {
                // Migrate models to tables
                context.Migrate();

                // Perform CRUD operations
                //var users = context.Set<User>();

                //// Add a new user
                //var newUser = new User { Name = "John Doe", Age = 30 };
                //users.Add(newUser);

                //// Retrieve all users
                //var allUsers = users.GetAll();
                //foreach (var user in allUsers)
                //{
                //    Console.WriteLine($"User: {user.Name}, Age: {user.Age}");
                //}

                //// Update a user
                //newUser.Age = 31;
                //users.Update(newUser);

                // Delete the user
                //users.Delete(newUser);
            }
        }
    }

    //class Program
    //    {
    //        static void Main(string[] args)
    //        {
    //            string connectionString = "Server=COGNINE-L105;Database=bb2;Trusted_Connection=True;Trust Server Certificate=True";
    //            GenerateDatabaseModelsAndContext(connectionString);
    //        }

    //        static void GenerateDatabaseModelsAndContext(string connectionString)
    //        {
    //            // Get the base directory where the application is running
    //            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;

    //            // Traverse upwards to locate the project directory (where the .csproj is located)
    //            while (!Directory.EnumerateFiles(appDirectory, "*.csproj", SearchOption.TopDirectoryOnly).Any())
    //            {
    //                string parentDirectory = Path.GetDirectoryName(appDirectory);
    //                if (string.IsNullOrEmpty(parentDirectory))
    //                    throw new InvalidOperationException("Could not locate the project directory.");
    //                appDirectory = parentDirectory;
    //            }

    //            // Once found, use the appDirectory to define the output folder for models
    //            string outputFolder = Path.Combine(appDirectory, "Models");

    //            // Ensure the folder exists
    //            if (!Directory.Exists(outputFolder))
    //                Directory.CreateDirectory(outputFolder);

    //            string namespaceName = "CustomOrmApp";

    //            // Fetch the schema and generate code
    //            var schemaFetcher = new DbSchemaFetcher(connectionString);
    //            var tables = schemaFetcher.GetTables();

    //            var codeGenerator = new CodeGenerator(outputFolder);
    //            codeGenerator.GenerateModels(tables);
    //            codeGenerator.GenerateDbContext(tables, namespaceName);

    //            Console.WriteLine($"Models and DbContext generated in {outputFolder}");
    //        }


    //    }


}
