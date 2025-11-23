using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DaJet
{
    [InlineArray(16)] internal struct ConfigFileVector { private uint _element; }
    internal static class ConfigFileVectorExtensions
    {
        internal static Span<uint> AsSpan(this ConfigFileVector vector, int size)
        {
            return MemoryMarshal.CreateSpan(ref Unsafe.As<ConfigFileVector, uint>(ref vector), size);
        }
        internal static ReadOnlySpan<uint> AsReadOnlySpan(this ConfigFileVector vector, int size)
        {
            return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ConfigFileVector, uint>(ref Unsafe.AsRef(in vector)), size);
        }
    }
}