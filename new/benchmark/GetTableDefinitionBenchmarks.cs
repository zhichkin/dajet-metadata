using BenchmarkDotNet.Attributes;
using DaJet;

namespace Benchmark
{
    [MemoryDiagnoser]
    [MinColumn]
    [MaxColumn]
    public class GetTableDefinitionBenchmarks
    {
        private static readonly string MS_UNF = "Data Source=ZHICHKIN;Initial Catalog=unf;Integrated Security=True;Encrypt=False;";
        private static readonly string MS_ERP = "Data Source=ZHICHKIN;Initial Catalog=erp_uh;Integrated Security=True;Encrypt=False;";
        private static readonly string PG_UNF = "Host=localhost;Port=5432;Database=unf;Username=postgres;Password=postgres;";
        private static readonly string PG_ERP = "Host=localhost;Port=5432;Database=erp_uh;Username=postgres;Password=postgres;";

        private static readonly OneDbMetadataProvider ms_unf_provider = new(DataSourceType.SqlServer, in MS_UNF);
        private static readonly OneDbMetadataProvider ms_erp_provider = new(DataSourceType.SqlServer, in MS_ERP);
        private static readonly OneDbMetadataProvider pg_unf_provider = new(DataSourceType.PostgreSql, in PG_UNF);
        private static readonly OneDbMetadataProvider pg_erp_provider = new(DataSourceType.PostgreSql, in PG_ERP);

        [GlobalSetup]
        public void GlobalSetup()
        {
            ms_unf_provider.Initialize();
            pg_unf_provider.Initialize();
            ms_erp_provider.Initialize();
            pg_erp_provider.Initialize();
        }

        [Benchmark(Description = "MS UNF")]
        public TableDefinition SqlServerUnf()
        {
            return ms_unf_provider.GetMetadataObject("Справочник.Номенклатура");
        }
        [Benchmark(Description = "PG UNF")]
        public TableDefinition PostgreSqlUnf()
        {
            return pg_unf_provider.GetMetadataObject("Справочник.Номенклатура");
        }
        [Benchmark(Description = "MS ERP")]
        public TableDefinition SqlServerErp()
        {
            return ms_erp_provider.GetMetadataObject("Справочник.Номенклатура");
        }
        [Benchmark(Description = "PG ERP")]
        public TableDefinition PostgreSqlErp()
        {
            return pg_erp_provider.GetMetadataObject("Справочник.Номенклатура");
        }
    }
}