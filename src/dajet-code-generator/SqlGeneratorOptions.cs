using DaJet.Metadata.Model;

namespace DaJet.CodeGenerator
{
    public interface ISqlGenerator
    {
        string GenerateViewScript(in ApplicationObject metadata);
        string GenerateEnumViewScript(in Enumeration enumeration);
        bool TryCreateView(in ApplicationObject metadata, out string error);
        bool TryCreateViews(in InfoBase infoBase, out int result, out List<string> errors);
        int DropViews();
        void DropView(in ApplicationObject metadata);
    }
    public sealed class SqlGeneratorOptions
    {
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
        }
    }
}