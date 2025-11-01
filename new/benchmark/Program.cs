using BenchmarkDotNet.Running;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Benchmark
{
    public static class Program
    {
        private static int size = 16;
        private static byte left, right;
        //private static ConfigFileVector span2 = new();
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<InitializeMetadataBenchmarks>();
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