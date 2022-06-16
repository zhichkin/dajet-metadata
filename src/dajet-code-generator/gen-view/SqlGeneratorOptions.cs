using DaJet.Metadata.Model;

namespace DaJet.CodeGenerator
{
    public interface ISqlGenerator
    {
        bool SchemaExists(string name);
        void CreateSchema(string name);
        void DropSchema(string name);
        string GenerateViewScript(in ApplicationObject metadata);
        string GenerateEnumViewScript(in Enumeration enumeration);
        bool TryScriptView(in StreamWriter writer, in ApplicationObject metadata, out string error);
        bool TryScriptViews(in InfoBase infoBase, out int result, out List<string> errors);
        bool TryCreateView(in ApplicationObject metadata, out string error);
        bool TryCreateViews(in InfoBase infoBase, out int result, out List<string> errors);
        int DropViews();
        void DropView(in ApplicationObject metadata);
    }
    public sealed class SqlGeneratorOptions
    {
        public string Schema { get; set; } = "dbo";
        public string OutputFile { get; set; } = string.Empty;
        public string DatabaseProvider { get; set; } = string.Empty; // { SqlServer, PostgreSql }
        public string ConnectionString { get; set; } = string.Empty;
        public List<string> Namespaces { get; set; } = new()
        {
            "Catalogs",
            "Documents",
            "Enumerations",
            "Publications",
            "Characteristics",
            "InformationRegisters",
            "AccumulationRegisters"
            //TODO: not supported "Constants" (Константа)
            //TODO: not supported "Accounts" (ПланСчетов)
            //TODO: not supported "AccountingRegisters" (РегистрБухгалтерии)
        };
        public static void Configure(in SqlGeneratorOptions options, Dictionary<string, string> values)
        {
            if (values.TryGetValue(nameof(SqlGeneratorOptions.DatabaseProvider), out string? DatabaseProvider))
            {
                options.DatabaseProvider = DatabaseProvider ?? string.Empty;
            }

            if (values.TryGetValue(nameof(SqlGeneratorOptions.ConnectionString), out string? ConnectionString))
            {
                options.ConnectionString = ConnectionString ?? string.Empty;
            }

            if (values.TryGetValue(nameof(SqlGeneratorOptions.Schema), out string? Schema))
            {
                options.Schema = Schema ?? string.Empty;
            }

            if (values.TryGetValue(nameof(SqlGeneratorOptions.OutputFile), out string? OutputFile))
            {
                options.OutputFile = OutputFile ?? string.Empty;
            }
        }
    }
}