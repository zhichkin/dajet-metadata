namespace DaJet
{
    internal abstract class ConfigFileParser
    {
        internal abstract Type Type { get; }
        internal abstract void Parse(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry);
    }
    internal abstract class ConfigFileParser<T> : ConfigFileParser where T : MetadataObject
    {
        internal override Type Type => typeof(T);
        internal abstract T Parse(ReadOnlySpan<byte> file);
    }
}