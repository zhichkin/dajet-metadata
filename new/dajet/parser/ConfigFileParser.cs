using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace DaJet
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
                new KeyValuePair<Guid, ConfigFileParser>(MetadataType.SharedProperty, new SharedProperty.Parser()),
                new KeyValuePair<Guid, ConfigFileParser>(MetadataType.DefinedType, new DefinedType.Parser()),
                new KeyValuePair<Guid, ConfigFileParser>(MetadataType.Catalog, new Catalog.Parser()),
                new KeyValuePair<Guid, ConfigFileParser>(MetadataType.Constant, new Constant.Parser()),
                new KeyValuePair<Guid, ConfigFileParser>(MetadataType.Document, new Document.Parser()),
                new KeyValuePair<Guid, ConfigFileParser>(MetadataType.Enumeration, new Enumeration.Parser()),
                new KeyValuePair<Guid, ConfigFileParser>(MetadataType.Publication, new Publication.Parser()),
                new KeyValuePair<Guid, ConfigFileParser>(MetadataType.Characteristic, new Characteristic.Parser()),
                new KeyValuePair<Guid, ConfigFileParser>(MetadataType.InformationRegister, new InformationRegister.Parser()),
                new KeyValuePair<Guid, ConfigFileParser>(MetadataType.AccumulationRegister, new AccumulationRegister.Parser()),
                new KeyValuePair<Guid, ConfigFileParser>(MetadataType.Account, new Account.Parser()),
                new KeyValuePair<Guid, ConfigFileParser>(MetadataType.AccountingRegister, new AccountingRegister.Parser()),
                new KeyValuePair<Guid, ConfigFileParser>(MetadataType.BusinessTask, new BusinessTask.Parser()),
                new KeyValuePair<Guid, ConfigFileParser>(MetadataType.BusinessProcess, new BusinessProcess.Parser())
            ];
            return list.ToFrozenDictionary();
        }
        internal static bool TryGetParser(Guid type, [MaybeNullWhen(false)] out ConfigFileParser parser)
        {
            return _parsers.TryGetValue(type, out parser);
        }
        internal static bool TryGetParser(in string type, [MaybeNullWhen(false)] out ConfigFileParser parser)
        {
            Guid uuid = MetadataName.GetMetadataType(in type);

            if (uuid == Guid.Empty)
            {
                parser = null;
                return false;
            }

            return TryGetParser(uuid, out parser);
        }
    }
}