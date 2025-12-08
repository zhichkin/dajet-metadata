using DaJet.TypeSystem;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace DaJet.Metadata
{
    internal abstract class ConfigFileParser
    {
        internal abstract void Initialize(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry);
        internal abstract EntityDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry, bool relations);

        private static readonly FrozenDictionary<Guid, ConfigFileParser> _parsers = CreateConfigFileParsersLookup();
        private static FrozenDictionary<Guid, ConfigFileParser> CreateConfigFileParsersLookup()
        {
            List<KeyValuePair<Guid, ConfigFileParser>> list =
            [
                new KeyValuePair<Guid, ConfigFileParser>(MetadataTypes.SharedProperty, new SharedProperty.Parser()),
                new KeyValuePair<Guid, ConfigFileParser>(MetadataTypes.DefinedType, new DefinedType.Parser()),
                new KeyValuePair<Guid, ConfigFileParser>(MetadataTypes.Catalog, new Catalog.Parser()),
                new KeyValuePair<Guid, ConfigFileParser>(MetadataTypes.Constant, new Constant.Parser()),
                new KeyValuePair<Guid, ConfigFileParser>(MetadataTypes.Document, new Document.Parser()),
                new KeyValuePair<Guid, ConfigFileParser>(MetadataTypes.Enumeration, new Enumeration.Parser()),
                new KeyValuePair<Guid, ConfigFileParser>(MetadataTypes.Publication, new Publication.Parser()),
                new KeyValuePair<Guid, ConfigFileParser>(MetadataTypes.Characteristic, new Characteristic.Parser()),
                new KeyValuePair<Guid, ConfigFileParser>(MetadataTypes.InformationRegister, new InformationRegister.Parser()),
                new KeyValuePair<Guid, ConfigFileParser>(MetadataTypes.AccumulationRegister, new AccumulationRegister.Parser()),
                new KeyValuePair<Guid, ConfigFileParser>(MetadataTypes.Account, new Account.Parser()),
                new KeyValuePair<Guid, ConfigFileParser>(MetadataTypes.AccountingRegister, new AccountingRegister.Parser()),
                new KeyValuePair<Guid, ConfigFileParser>(MetadataTypes.BusinessTask, new BusinessTask.Parser()),
                new KeyValuePair<Guid, ConfigFileParser>(MetadataTypes.BusinessProcess, new BusinessProcess.Parser())
            ];
            return list.ToFrozenDictionary();
        }
        internal static bool TryGetParser(Guid type, [MaybeNullWhen(false)] out ConfigFileParser parser)
        {
            return _parsers.TryGetValue(type, out parser);
        }
        internal static bool TryGetParser(in string type, [MaybeNullWhen(false)] out ConfigFileParser parser)
        {
            Guid uuid = MetadataLookup.GetMetadataType(in type);

            if (uuid == Guid.Empty)
            {
                parser = null;
                return false;
            }

            return TryGetParser(uuid, out parser);
        }
    }
}