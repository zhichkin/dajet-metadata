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
        
        private static readonly MetadataProvider ms_unf_provider = new(DataSourceType.SqlServer, in MS_UNF);
        private static readonly MetadataProvider ms_erp_provider = new(DataSourceType.SqlServer, in MS_ERP);
        private static readonly MetadataProvider pg_unf_provider = new(DataSourceType.PostgreSql, in PG_UNF);
        private static readonly MetadataProvider pg_erp_provider = new(DataSourceType.PostgreSql, in PG_ERP);

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
        public bool InitializeSqlServerUnf()
        {
            try
            {
                ms_unf_provider.Initialize();
            }
            catch
            {
                return false;
            }

            return true;
        }
        [Benchmark(Description = "PG UNF")]
        public bool InitializePostgreSqlUnf()
        {
            try
            {
                pg_unf_provider.Initialize();
            }
            catch
            {
                return false;
            }

            return true;
        }
        [Benchmark(Description = "MS ERP")]
        public bool InitializeSqlServerErp()
        {
            try
            {
                ms_erp_provider.Initialize();
            }
            catch
            {
                return false;
            }

            return true;
        }
        [Benchmark(Description = "PG ERP")]
        public bool InitializePostgreSqlErp()
        {
            try
            {
                pg_erp_provider.Initialize();
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}