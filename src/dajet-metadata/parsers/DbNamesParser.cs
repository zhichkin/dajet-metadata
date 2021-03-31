using DaJet.Metadata.Model;
using System;
using System.Collections.Generic;
using System.IO;

namespace DaJet.Metadata.Parsers
{
    public sealed class DbNamesParser
    {
        private InfoBase InfoBase { get; set; }
        private IMetadataManager MetadataManager { get; set; }
        private readonly ConfigFileParser ConfigFileParser = new ConfigFileParser();
        public void Parse(StreamReader stream, InfoBase infoBase, IMetadataManager metadataManager)
        {
            InfoBase = infoBase;
            MetadataManager = metadataManager;

            ConfigObject mdo = ConfigFileParser.Parse(stream);

            int entryCount = mdo.GetInt32(new int[] { 1, 0 });

            for (int i = 1; i < entryCount; i++)
            {
                Guid uuid = mdo.GetUuid(new int[] { 1, i, 0 });
                if (uuid == Guid.Empty) continue;
                
                string token = mdo.GetString(new int[] { 1, i, 1 });
                int code = mdo.GetInt32(new int[] { 1, i, 2 });
                
                ParseEntry(uuid, token, code);
            }
        }
        private void ParseEntry(Guid uuid, string token, int code)
        {
            if (token == MetadataTokens.Fld || token == MetadataTokens.LineNo)
            {
                _ = InfoBase.Properties.TryAdd(uuid, MetadataManager.CreateProperty(uuid, token, code));
                return;
            }

            Type type = MetadataManager.GetTypeByToken(token);
            if (type == null) return; // unsupported type of metadata object

            ApplicationObject metaObject = MetadataManager.CreateObject(uuid, token, code);
            if (metaObject == null) return; // unsupported type of metadata object

            if (token == MetadataTokens.VT)
            {
                _ = InfoBase.TableParts.TryAdd(uuid, metaObject);
                return;
            }

            if (!InfoBase.AllTypes.TryGetValue(type, out Dictionary<Guid, ApplicationObject> collection))
            {
                return; // unsupported collection of metadata objects
            }

            _ = collection.TryAdd(uuid, metaObject);
        }
    }
}