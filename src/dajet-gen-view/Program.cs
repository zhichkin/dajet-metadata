using DaJet.Metadata;
using DaJet.Metadata.Model;
using System.CommandLine;
using System.Diagnostics;

namespace DaJet.CodeGenerator
{
    public static class Program
    {
        private const string CONST_SqlServer = "SqlServer";
        private const string CONST_PostgreSql = "PostgreSql";
        public static int Main(string[] args)
        {
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

        private static ISqlGenerator GetSqlGenerator(SqlGeneratorOptions options)
        {
            if (options.DatabaseProvider == CONST_SqlServer)
            {
                return new SqlServer.SqlGenerator(options);
            }
            else if (options.DatabaseProvider == CONST_PostgreSql)
            {
                return new PostgreSql.SqlGenerator(options);
            }

            throw new InvalidOperationException($"Unsupported database provider: [{options.DatabaseProvider}]");
        }
        private static IMetadataService GetMetadataService(SqlGeneratorOptions options)
        {
            if (options.DatabaseProvider == CONST_SqlServer)
            {
                return new MetadataService()
                    .UseConnectionString(options.ConnectionString)
                    .UseDatabaseProvider(DatabaseProvider.SQLServer);
            }
            else if (options.DatabaseProvider == CONST_PostgreSql)
            {
                return new MetadataService()
                    .UseConnectionString(options.ConnectionString)
                    .UseDatabaseProvider(DatabaseProvider.PostgreSQL);
            }

            throw new InvalidOperationException($"Unsupported database provider: [{options.DatabaseProvider}]");
        }

        private static void ExecuteCreateCommand(string ms, string pg, string schema)
        {
            SqlGeneratorOptions options = new();

            if (!string.IsNullOrWhiteSpace(ms))
            {
                options.ConnectionString = ms;
                options.DatabaseProvider = CONST_SqlServer;
            }
            else if (!string.IsNullOrWhiteSpace(pg))
            {
                options.ConnectionString = pg;
                options.DatabaseProvider = CONST_PostgreSql;
            }
            else
            {
                Console.WriteLine("ERROR: Database connection string is not provided.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(schema))
            {
                options.Schema = schema;
            }

            IMetadataService metadataService = GetMetadataService(options);

            if (!metadataService.TryOpenInfoBase(out InfoBase infoBase, out string message))
            {
                Console.WriteLine("Error: " + message);
                return;
            }
            
            ISqlGenerator generator = GetSqlGenerator(options);

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
            SqlGeneratorOptions options = new();

            if (!string.IsNullOrWhiteSpace(ms))
            {
                options.ConnectionString = ms;
                options.DatabaseProvider = CONST_SqlServer;
            }
            else if (!string.IsNullOrWhiteSpace(pg))
            {
                options.ConnectionString = pg;
                options.DatabaseProvider = CONST_PostgreSql;
            }
            else
            {
                Console.WriteLine("ERROR: Database connection string is not provided.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(schema))
            {
                options.Schema = schema;
            }

            ISqlGenerator generator = GetSqlGenerator(options);

            Stopwatch watch = new();

            watch.Start();

            int result = generator.DropViews();

            watch.Stop();

            Console.WriteLine($"Dropped {result} views in {watch.ElapsedMilliseconds} ms");
        }
        private static void ExecuteScriptCommand(string ms, string pg, string schema, string outFile)
        {
            SqlGeneratorOptions options = new();

            if (!string.IsNullOrWhiteSpace(ms))
            {
                options.ConnectionString = ms;
                options.DatabaseProvider = CONST_SqlServer;
            }
            else if (!string.IsNullOrWhiteSpace(pg))
            {
                options.ConnectionString = pg;
                options.DatabaseProvider = CONST_PostgreSql;
            }
            else
            {
                Console.WriteLine("ERROR: Database connection string is not provided.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(schema))
            {
                options.Schema = schema;
            }

            if (!string.IsNullOrWhiteSpace(outFile))
            {
                options.OutputFile = outFile;
            }

            IMetadataService metadataService = GetMetadataService(options);

            if (!metadataService.TryOpenInfoBase(out InfoBase infoBase, out string message))
            {
                Console.WriteLine("Error: " + message);
                return;
            }
            
            ISqlGenerator generator = GetSqlGenerator(options);

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