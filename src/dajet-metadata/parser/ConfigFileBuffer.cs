using System.Buffers;
using System.Data.Common;
using System.IO.Compression;

namespace DaJet.Metadata
{
    internal struct ConfigFileBuffer : IDisposable
    {
        private const int DECOMPRESS_BUFFER_SIZE_INIT = 64 * 1024;
        private const int DECOMPRESS_BUFFER_SIZE_GROW = 32 * 1024; // Это значение должно быть больше, чем 1024 байт, которые выделяются в стеке в качестве буфера для потоковой распаковки данных файла

        private int _length;
        private byte[] _buffer;
        private string _fileName;
        internal ConfigFileBuffer(int length)
        {
            _length = length;
            _buffer = ArrayPool<byte>.Shared.Rent(length);
        }
        internal ConfigFileBuffer(in DbDataReader reader)
        {
            Load(in reader);
        }
        internal readonly int Length { get { return _length; } }
        internal readonly string FileName { get { return _fileName; } }
        internal readonly ReadOnlySpan<byte> AsReadOnlySpan()
        {
            if (_length == 0)
            {
                return ReadOnlySpan<byte>.Empty;
            }

            return _buffer.AsSpan(0, _length);
        }
        internal void Load(in DbDataReader reader)
        {
            ArgumentNullException.ThrowIfNull(reader);

            bool utf8 = (reader.GetInt32(0) == 1);
            
            _length = reader.GetInt32(1);
            _fileName = reader.GetString(2);

            _buffer = ArrayPool<byte>.Shared.Rent(_length);

            try
            {
                _length = (int)reader.GetBytes(3, 0L, _buffer, 0, _length);
            }
            catch
            {
                Dispose(); throw;
            }

            if (utf8) { return; }

            byte[] decompressed = ArrayPool<byte>.Shared.Rent(DECOMPRESS_BUFFER_SIZE_INIT);

            try
            {
                _length = Decompress(ref decompressed);
            }
            catch
            {
                Dispose();

                if (decompressed is not null)
                {
                    ArrayPool<byte>.Shared.Return(decompressed);
                }

                throw;
            }

            byte[] return_me = _buffer;

            _buffer = decompressed;

            if (return_me is not null)
            {
                ArrayPool<byte>.Shared.Return(return_me);
            }
        }
        private readonly int Decompress(ref byte[] output)
        {
            int position = 0;
            int capacity = output.Length;

            Span<byte> buffer = stackalloc byte[1024]; // Буфер для потоковой распаковки данных файла

            using (MemoryStream memory = new(_buffer, 0, _length))
            {
                using (DeflateStream deflate = new(memory, CompressionMode.Decompress))
                {
                    int decompressed = deflate.Read(buffer);

                    while (decompressed > 0)
                    {
                        if (decompressed > capacity - position)
                        {
                            capacity += DECOMPRESS_BUFFER_SIZE_GROW; // Запрашиваем больше памяти для новых данных

                            GrowBuffer(capacity, ref output, position);

                            capacity = output.Length; // Функция GrowBuffer может выделить больше памяти
                        }

                        Span<byte> target = output.AsSpan(position, decompressed);

                        buffer[..decompressed].CopyTo(target);

                        position += decompressed;

                        decompressed = deflate.Read(buffer);
                    }
                }
            }

            return position;
        }
        private static void GrowBuffer(int capacity, ref byte[] buffer, int length)
        {
            if (capacity <= buffer.Length) { return; }

            byte[] new_buffer = ArrayPool<byte>.Shared.Rent(capacity);

            buffer.AsSpan(0, length).CopyTo(new_buffer);

            byte[] return_me = buffer;

            buffer = new_buffer;

            if (return_me is not null)
            {
                ArrayPool<byte>.Shared.Return(return_me);
            }
        }
        public void Dispose()
        {
            _length = 0;

            if (_buffer is not null)
            {
                ArrayPool<byte>.Shared.Return(_buffer);
            }

            _buffer = null;
            _fileName = null;
        }
    }
}