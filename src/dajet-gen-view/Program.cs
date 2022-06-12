using DaJet.CodeGenerator.SqlServer;
using DaJet.Metadata;
using DaJet.Metadata.Model;
using System.CommandLine;
using System.Diagnostics;

namespace DaJet.CodeGenerator
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            //args = new string[] { "--help" };
            //args = new string[] { "--version" };

            //string ms = "Data Source=zhichkin;Initial Catalog=dajet-metadata-ms;Integrated Security=True;Encrypt=False;";
            //args = new string[] { "create", "--ms", ms };
            //args = new string[] { "delete", "--ms", ms };

            RootCommand command = new()
            {
                Description = "DaJet database view generator 1.0.0"
            };

            command.Add(CreateCommandFactory());
            
            command.Add(DeleteCommandFactory());

            return command.Invoke(args);
        }
        private static Command CreateCommandFactory()
        {
            var ms = new Option<string>("--ms", "SQL Server connection string");
            var pg = new Option<string>("--pg", "PostgreSQL connection string");

            Command create = new("create") { ms, pg };

            create.Description = "Create database views"
                + Environment.NewLine + "Options:";

            foreach (Option option in create.Options)
            {
                create.Description += Environment.NewLine + "  --" + option.Name.PadRight(4) + option.Description;
            }

            create.SetHandler<string, string>(ExecuteCreateCommand, ms, pg);

            return create;
        }
        private static Command DeleteCommandFactory()
        {
            var ms = new Option<string>("--ms", "SQL Server connection string");
            var pg = new Option<string>("--pg", "PostgreSQL connection string");

            Command create = new("delete") { ms, pg };

            create.Description = "Delete database views"
                + Environment.NewLine + "Options:";

            foreach (Option option in create.Options)
            {
                create.Description += Environment.NewLine + "  --" + option.Name.PadRight(4) + option.Description;
            }

            create.SetHandler<string, string>(ExecuteDeleteCommand, ms, pg);

            return create;
        }
        private static void ExecuteCreateCommand(string ms, string pg)
        {
            if (!string.IsNullOrWhiteSpace(pg))
            {
                Console.WriteLine("Sorry, PostgreSql support is under construction ... ");
                return;
            }

            IMetadataService metadataService = new MetadataService();

            if (!metadataService
                .UseDatabaseProvider(DatabaseProvider.SQLServer)
                .UseConnectionString(ms)
                .TryOpenInfoBase(out InfoBase infoBase, out string message))
            {
                Console.WriteLine("Error: " + message);
                return;
            }

            SqlGeneratorOptions options = new SqlGeneratorOptions()
            {
                DatabaseProvider = "SqlServer",
                ConnectionString = metadataService.ConnectionString
            };

            ISqlGenerator generator = new SqlGenerator(options);

            Stopwatch watch = new();

            watch.Start();

            if (!generator.TryCreateViews(in infoBase, out int result, out List<string> errors))
            {
                foreach (string error in errors)
                {
                    Console.WriteLine(error);
                }
            }

            watch.Stop();

            Console.WriteLine($"Created {result} views in {watch.ElapsedMilliseconds} ms");
        }
        private static void ExecuteDeleteCommand(string ms, string pg)
        {
            if (!string.IsNullOrWhiteSpace(pg))
            {
                Console.WriteLine("Sorry, PostgreSql support is under construction ... ");
                return;
            }

            SqlGeneratorOptions options = new SqlGeneratorOptions()
            {
                DatabaseProvider = "SqlServer",
                ConnectionString = ms
            };

            ISqlGenerator generator = new SqlGenerator(options);

            Stopwatch watch = new();

            watch.Start();

            int result = generator.DropViews();

            watch.Stop();

            Console.WriteLine($"Dropped {result} views in {watch.ElapsedMilliseconds} ms");
        }
    }
}