using BenchmarkDotNet.Attributes;
using DaJet.Data;
using DaJet.Metadata;
using DaJet.TypeSystem;

namespace Benchmark
{
    [MemoryDiagnoser]
    [MinColumn]
    [MaxColumn]
    public class GetEntityDefinitionBenchmarks
    {
        private static readonly string MS_UNF = "Data Source=ZHICHKIN;Initial Catalog=unf;Integrated Security=True;Encrypt=False;";
        private static readonly string MS_ERP = "Data Source=ZHICHKIN;Initial Catalog=erp_uh;Integrated Security=True;Encrypt=False;";
        private static readonly string PG_UNF = "Host=localhost;Port=5432;Database=unf;Username=postgres;Password=postgres;";
        private static readonly string PG_ERP = "Host=localhost;Port=5432;Database=erp_uh;Username=postgres;Password=postgres;";

        private static readonly MetadataProvider ms_unf_provider = MetadataProvider.Create(DataSourceType.SqlServer, in MS_UNF);
        private static readonly MetadataProvider ms_erp_provider = MetadataProvider.Create(DataSourceType.SqlServer, in MS_ERP);
        private static readonly MetadataProvider pg_unf_provider = MetadataProvider.Create(DataSourceType.PostgreSql, in PG_UNF);
        private static readonly MetadataProvider pg_erp_provider = MetadataProvider.Create(DataSourceType.PostgreSql, in PG_ERP);

        [Benchmark(Description = "MS UNF")]
        public EntityDefinition SqlServerUnf()
        {
            return ms_unf_provider.GetMetadataObject("Справочник.Номенклатура");
        }
        [Benchmark(Description = "PG UNF")]
        public EntityDefinition PostgreSqlUnf()
        {
            return pg_unf_provider.GetMetadataObject("Справочник.Номенклатура");
        }
        [Benchmark(Description = "MS ERP")]
        public EntityDefinition SqlServerErp()
        {
            return ms_erp_provider.GetMetadataObject("Справочник.Номенклатура");
        }
        [Benchmark(Description = "PG ERP")]
        public EntityDefinition PostgreSqlErp()
        {
            return pg_erp_provider.GetMetadataObject("Справочник.Номенклатура");
        }
    }
}