using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using DaJet.Data;
using DaJet.Metadata;

namespace Benchmark
{
    [Config(typeof(Config))]
    [MemoryDiagnoser]
    public class InitializeMetadataBenchmarks
    {
        private static readonly string MS_UNF = "Data Source=ZHICHKIN;Initial Catalog=unf;Integrated Security=True;Encrypt=False;";
        private static readonly string MS_ERP = "Data Source=ZHICHKIN;Initial Catalog=erp_uh;Integrated Security=True;Encrypt=False;";
        private static readonly string PG_UNF = "Host=localhost;Port=5432;Database=unf;Username=postgres;Password=postgres;";
        private static readonly string PG_ERP = "Host=localhost;Port=5432;Database=erp_uh;Username=postgres;Password=postgres;";
        
        private class Config : ManualConfig
        {
            public Config()
            {
                AddJob(Job.Default.WithGcServer(true).WithGcForce(false).WithId("Server"));
                //AddJob(Job.Default.WithGcServer(false).WithGcForce(false).WithId("Workstation"));

                //AddJob(Job.Default.WithGcServer(true).WithGcForce(true).WithId("ServerForce"));
                //AddJob(Job.Default.WithGcServer(false).WithGcForce(true).WithId("WorkstationForce"));
            }
        }

        [Benchmark(Description = "MS UNF")]
        public MetadataProvider InitializeSqlServerUnf()
        {
            return MetadataProvider.Create(DataSourceType.SqlServer, in MS_UNF);
        }
        [Benchmark(Description = "PG UNF")]
        public MetadataProvider InitializePostgreSqlUnf()
        {
            return MetadataProvider.Create(DataSourceType.PostgreSql, in PG_UNF);
        }
        [Benchmark(Description = "MS ERP")]
        public MetadataProvider InitializeSqlServerErp()
        {
            return MetadataProvider.Create(DataSourceType.SqlServer, in MS_ERP);
        }
        [Benchmark(Description = "PG ERP")]
        public MetadataProvider InitializePostgreSqlErp()
        {
            return MetadataProvider.Create(DataSourceType.PostgreSql, in PG_ERP);
        }
    }
}