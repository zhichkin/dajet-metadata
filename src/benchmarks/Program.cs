using BenchmarkDotNet.Running;

namespace Benchmark
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args is not null && args.Length > 0)
            {
                if (args[0] == "table")
                {
                    BenchmarkRunner.Run<GetEntityDefinitionBenchmarks>();
                }
                else if (args[0] == "cache")
                {
                    BenchmarkRunner.Run<GetOrCreateProviderBenchmarks>();
                }
            }
            else
            {
                BenchmarkRunner.Run<InitializeMetadataBenchmarks>();
            }
        }
    }
}