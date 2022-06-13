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

            //string schema = "test";
            //string outFile = "C:\\temp\\sql-views.sql";
            //string ms = "Data Source=zhichkin;Initial Catalog=dajet-metadata-ms;Integrated Security=True;Encrypt=False;";
            //args = new string[] { "create", "--ms", ms, "--schema", schema };
            //args = new string[] { "delete", "--ms", ms, "--schema", schema };
            //args = new string[] { "script", "--ms", ms, "--out-file", outFile, "--schema", "test" };

            RootCommand command = new()
            {
                Description = "DaJet database view generator"
            };

            command.Add(CreateCommandFactory());
            
            command.Add(DeleteCommandFactory());

            command.Add(ScriptCommandFactory());

            return command.Invoke(args);
        }
        
        private static Command CreateCommandFactory()
        {
            var ms = new Option<string>("--ms", "SQL Server connection string");
            var pg = new Option<string>("--pg", "PostgreSQL connection string");
            var schema = new Option<string>("--schema", "Database schema to use or create");

            Command create = new("create") { ms, pg, schema };

            create.Description = "Create database views"
                + Environment.NewLine + "Options:";

            foreach (Option option in create.Options)
            {
                create.Description += Environment.NewLine + "  --" + option.Name.PadRight(8) + option.Description;
            }

            create.SetHandler(ExecuteCreateCommand, ms, pg, schema);

            return create;
        }
        private static Command DeleteCommandFactory()
        {
            var ms = new Option<string>("--ms", "SQL Server connection string");
            var pg = new Option<string>("--pg", "PostgreSQL connection string");
            var schema = new Option<string>("--schema", "Database schema to use");

            Command create = new("delete") { ms, pg, schema };

            create.Description = "Delete database views"
                + Environment.NewLine + "Options:";

            foreach (Option option in create.Options)
            {
                create.Description += Environment.NewLine + "  --" + option.Name.PadRight(8) + option.Description;
            }

            create.SetHandler(ExecuteDeleteCommand, ms, pg, schema);

            return create;
        }
        private static Command ScriptCommandFactory()
        {
            var ms = new Option<string>("--ms", "SQL Server connection string");
            var pg = new Option<string>("--pg", "PostgreSQL connection string");
            var schema = new Option<string>("--schema", "Database schema to use");
            var outFile = new Option<string>("--out-file", "File path to save SQL script");

            Command create = new("script") { ms, pg, schema, outFile };

            create.Description = "Script database views to file"
                + Environment.NewLine + "Options:";

            foreach (Option option in create.Options)
            {
                create.Description += Environment.NewLine + "  --" + option.Name.PadRight(10) + option.Description;
            }

            create.SetHandler(ExecuteScriptCommand, ms, pg, schema, outFile);

            return create;
        }
        
        private static void ExecuteCreateCommand(string ms, string pg, string schema)
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

            if (!string.IsNullOrWhiteSpace(schema))
            {
                options.Schema = schema;
            }

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
        private static void ExecuteDeleteCommand(string ms, string pg, string schema)
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

            if (!string.IsNullOrWhiteSpace(schema))
            {
                options.Schema = schema;
            }

            ISqlGenerator generator = new SqlGenerator(options);

            Stopwatch watch = new();

            watch.Start();

            int result = generator.DropViews();

            watch.Stop();

            Console.WriteLine($"Dropped {result} views in {watch.ElapsedMilliseconds} ms");
        }
        private static void ExecuteScriptCommand(string ms, string pg, string schema, string outFile)
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

            if (!string.IsNullOrWhiteSpace(schema))
            {
                options.Schema = schema;
            }

            if (!string.IsNullOrWhiteSpace(outFile))
            {
                options.OutputFile = outFile;
            }

            ISqlGenerator generator = new SqlGenerator(options);

            Stopwatch watch = new();

            watch.Start();

            if (!generator.TryScriptViews(in infoBase, out int result, out List<string> errors))
            {
                foreach (string error in errors)
                {
                    Console.WriteLine(error);
                }
            }

            watch.Stop();

            Console.WriteLine($"Scripted {result} views in {watch.ElapsedMilliseconds} ms");
            Console.WriteLine($"Output script file: {outFile}");
        }
    }
}