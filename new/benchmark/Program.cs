using BenchmarkDotNet.Running;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Benchmark
{
    public static class Program
    {
        private static int size = 16;
        public static void Main(string[] args)
        {
            if (args is not null && args.Length > 0)
            {
                if (args[0] == "table")
                {
                    BenchmarkRunner.Run<GetTableDefinitionBenchmarks>();
                }
                else if (args[0] == "parser")
                {
                    BenchmarkRunner.Run<ConfigFileReaderBenchmarks>();
                }
            }
            else
            {
                BenchmarkRunner.Run<InitializeMetadataBenchmarks>();
            }
            
            //BenchmarkRunner.Run<InitializeMetadataBenchmarks>();
            //BenchmarkRunner.Run<ConfigFileReaderBenchmarks>();

            //ReadOnlySpan<byte> array = [0, 1, 2, 3];
            //DumpAsm(array);
            //Console.WriteLine(GetTypeSize(typeof(ConfigFileReader)));
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void DumpAsm(ReadOnlySpan<byte> array)
        {
            int segment = 0;

            while (segment < array.Length)
            {
                segment++;
                size = segment;
            }
        }
        private static int GetTypeSize(Type type)
        {
            //Type type = typeof(ConfigFileReader);
            var dm = new DynamicMethod("$", typeof(int), Type.EmptyTypes);
            ILGenerator il = dm.GetILGenerator();
            il.Emit(OpCodes.Sizeof, type);
            il.Emit(OpCodes.Ret);
            int size = (int)dm.Invoke(null, null);
            return size;
        }
    }
}