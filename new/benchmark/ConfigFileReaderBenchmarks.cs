using BenchmarkDotNet.Attributes;
using DaJet;
using Microsoft.Data.SqlClient;
using Microsoft.IO;
using System.Buffers;
using System.Data;
using System.IO.Compression;

namespace Benchmark
{
    [MemoryDiagnoser]
    [MinColumn]
    [MaxColumn]
    public class ConfigFileReaderBenchmarks
    {
        private static readonly RecyclableMemoryStreamManager _memory = new();
        private const string MS_CONNECTION = "Data Source=ZHICHKIN;Initial Catalog=unf;Integrated Security=True;Encrypt=False;";
        private const string MS_PARAMS_SCRIPT = "SELECT (CASE WHEN SUBSTRING(BinaryData, 1, 3) = 0xEFBBBF THEN 1 ELSE 0 END) AS UTF8, CAST(DataSize AS int) AS DataSize, BinaryData FROM Params WHERE FileName = @FileName;";
        private const string MS_CONFIG_SCRIPT = "SELECT (CASE WHEN SUBSTRING(BinaryData, 1, 3) = 0xEFBBBF THEN 1 ELSE 0 END) AS UTF8, CAST(DataSize AS int) AS DataSize, BinaryData FROM Config WHERE FileName = @FileName;";

        private static bool _utf8;
        private static int _size;
        private static byte[] _data; // database data
        private static byte[] _buffer; // decompressed

        private static Guid metadataUuid = new Guid("bea7f781-5f18-4219-997c-9a767fb284be");
        private static readonly OneDbMetadataProvider ms_unf_provider = new(DataSourceType.SqlServer, MS_CONNECTION);

        [GlobalSetup]
        public void GlobalSetup()
        {
            GetConfigFileData("bea7f781-5f18-4219-997c-9a767fb284be"); // Справочник "Номенклатура"
            DecompressConfigFile();

            ms_unf_provider.Initialize();
        }
        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _buffer = null;
            ArrayPool<byte>.Shared.Return(_data);
        }

        private static SqlConnection CreateDbConnection()
        {
            return new SqlConnection(MS_CONNECTION);
        }
        private static void GetConfigFileData(string fileName)
        {
            using (SqlConnection connection = CreateDbConnection())
            {
                connection.Open();

                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = MS_CONFIG_SCRIPT;
                    command.CommandType = CommandType.Text;
                    command.CommandTimeout = 10; // seconds

                    command.Parameters.AddWithValue("FileName", fileName);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            bool utf8 = (reader.GetInt32(0) == 1);
                            _size = reader.GetInt32(1);
                            _data = ArrayPool<byte>.Shared.Rent(_size);
                            long loaded = reader.GetBytes(2, 0, _data, 0, _size);
                            Console.WriteLine($"Data loaded = {loaded}");
                        }
                    }
                }
            }
        }
        private static void DecompressConfigFile()
        {
            if (_utf8)
            {
                _buffer = _data; return;
            }

            Span<byte> buffer = stackalloc byte[1024];

            using (MemoryStream source = new(_data, 0, _size, false, true))
            {
                using (DeflateStream deflate = new(source, CompressionMode.Decompress))
                {
                    using (RecyclableMemoryStream memory = _memory.GetStream())
                    {
                        int decompressed = 0;
                        do
                        {
                            decompressed = deflate.Read(buffer);
                            memory.Write(buffer[..decompressed]);
                        }
                        while (decompressed > 0);

                        _buffer = memory.GetReadOnlySequence().ToArray();

                        Console.WriteLine($"Decompressed = {memory.Length}");
                    }
                }
            }
        }

        [Benchmark(Description = "Parser")]
        public TableDefinition ParseConfigFile()
        {
            TableDefinition table = ms_unf_provider.ParseConfigFile(metadataUuid, _buffer);

            //Console.WriteLine($"[{table.DbName}] {table.Name} {{{table.Properties.Count}}} = {_buffer.Length} bytes");

            return table;
        }

        #region "Старые тесты allocation-free stack-based парсера"

        //        [Benchmark(Description = "Seek (old)")]
        //        public int SeekConfigFile()
        //        {
        //            int result = 0;

        //            ConfigFileReader reader = new(_buffer);

        //            uint[] vector = ArrayPool<uint>.Shared.Rent(16);

        //            vector[0] = 2;
        //            vector[1] = 10;
        //            vector[2] = 2;
        //            vector[3] = 3;

        //            if (reader.Seek(vector.AsSpan(0, 4)))
        //            {
        //                result++;
        //            }

        //            vector[0] = 2;
        //            vector[1] = 10;
        //            vector[2] = 2;
        //            vector[3] = 4;
        //            vector[4] = 3;

        //            if (reader.Seek(vector.AsSpan(0, 5)))
        //            {
        //                result++;
        //            }

        //            vector[0] = 7;
        //            vector[1] = 17;
        //            vector[2] = 1;
        //            vector[3] = 2;
        //            vector[4] = 2;
        //            vector[5] = 2;
        //            vector[6] = 3;

        //            if (reader.Seek(vector.AsSpan(0, 7)))
        //            {
        //                result++;
        //            }

        //            vector[0] = ConfigFileToken.EndObject;

        //            if (reader.Seek(vector.AsSpan(0, 1)))
        //            {
        //                result++;
        //            }

        //            ArrayPool<uint>.Shared.Return(vector);

        //            return result;
        //        }

        //        [Benchmark(Description = "Seek (new)")]
        //        public int FindConfigFile()
        //        {
        //            int result = 0;

        //            ConfigFileReader reader = new(_buffer);

        //            if (reader[2][10][2][3].Seek())
        //            {
        //                result++;
        //            }

        //            if (reader[2][10][2][4][3].Seek())
        //            {
        //                result++;
        //            }

        //            if (reader[7][17][1][2][2][2][3].Seek())
        //            {
        //                result++;
        //            }

        //            if (reader[7][17][1][2][5][3].Seek())
        //            {
        //                result++;
        //            }

        //            if (reader[ConfigFileToken.EndObject].Seek())
        //            {
        //                result++;
        //            }

        //            return result;
        //        }

        //        [Benchmark(Description = "Vectorized")]
        //        public int VectorizedSeekConfigFile()
        //        {
        //#pragma warning disable CFREADER_VECTORIZED

        //            int result = 0;

        //            ConfigFileReader reader = new(_buffer);

        //            if (reader[2][10][2][3].SeekVectorized())
        //            {
        //                result++;
        //            }

        //            if (reader[2][10][2][4][3].SeekVectorized())
        //            {
        //                result++;
        //            }

        //            if (reader[7][17][1][2][2][2][3].SeekVectorized())
        //            {
        //                result++;
        //            }

        //            if (reader[ConfigFileToken.EndObject].SeekVectorized())
        //            {
        //                result++;
        //            }

        //            //Console.WriteLine($"Result = {result}");

        //#pragma warning restore CFREADER_VECTORIZED

        //            return result;
        //        }

        #endregion
    }
}