namespace DaJet
{
    internal abstract class ConfigFileParser
    {
        internal abstract void Parse(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry);
        internal abstract TableDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry);
    }
}