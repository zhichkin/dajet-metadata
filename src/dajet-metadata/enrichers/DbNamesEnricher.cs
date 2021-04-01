using DaJet.Metadata.Model;
using DaJet.Metadata.Services;
using System;
using System.Collections.Generic;

namespace DaJet.Metadata.Enrichers
{
    public sealed class DbNamesEnricher : IContentEnricher
    {
        private const string DBNAMES_FILE_NAME = "DBNames"; // Params
        private Configurator Configurator { get; }
        public DbNamesEnricher(Configurator configurator)
        {
            Configurator = configurator;
        }
        public void Enrich(MetadataObject metadataObject)
        {
            if (!(metadataObject is InfoBase infoBase)) throw new ArgumentOutOfRangeException();

            ConfigObject configObject = Configurator.FileReader.ReadConfigObject(DBNAMES_FILE_NAME);

            int entryCount = configObject.GetInt32(new int[] { 1, 0 });

            for (int i = 1; i < entryCount; i++)
            {
                Guid uuid = configObject.GetUuid(new int[] { 1, i, 0 });
                if (uuid == Guid.Empty) continue;
                
                string token = configObject.GetString(new int[] { 1, i, 1 });
                int code = configObject.GetInt32(new int[] { 1, i, 2 });

                ProcessEntry(infoBase, uuid, token, code);
            }
        }
        private void ProcessEntry(InfoBase infoBase, Guid uuid, string token, int code)
        {
            if (token == MetadataTokens.Fld || token == MetadataTokens.LineNo)
            {
                _ = infoBase.Properties.TryAdd(uuid, Configurator.CreateProperty(uuid, token, code));
                return;
            }

            Type type = Configurator.GetTypeByToken(token);
            if (type == null) return; // unsupported type of metadata object

            ApplicationObject metaObject = Configurator.CreateObject(uuid, token, code);
            if (metaObject == null) return; // unsupported type of metadata object

            if (token == MetadataTokens.VT)
            {
                _ = infoBase.TableParts.TryAdd(uuid, metaObject);
                return;
            }

            if (!infoBase.AllTypes.TryGetValue(type, out Dictionary<Guid, ApplicationObject> collection))
            {
                return; // unsupported collection of metadata objects
            }
            _ = collection.TryAdd(uuid, metaObject);
        }
    }
}