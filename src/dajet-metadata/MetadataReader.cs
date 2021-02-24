using DaJet.Metadata.Model;
using System;
using System.Collections.Generic;
using System.IO;

namespace DaJet.Metadata
{
    /// <summary>
    /// Интерфейс для чтения метаданных прикладных объектов конфигурации 1С
    /// </summary>
    public interface IMetadataReader
    {

    }
    /// <summary>
    /// Класс, реализующий интерфейс <see cref="IMetadataReader"/>, для чтения метаданных из SQL Server
    /// </summary>
    public sealed class MetadataReader : IMetadataReader
    {
        private const string DBNAMES_FILE_NAME = "DBNames";

        private readonly IMetadataFileReader MetadataFileReader;
        private readonly IDBNamesFileParser DBNamesFileParser = new DBNamesFileParser();

        private DBNamesCash DBNamesCash;

        public MetadataReader(IMetadataFileReader metadataFileReader)
        {
            MetadataFileReader = metadataFileReader;
        }

        #region "DBNames"

        private void ReadDBNames()
        {
            byte[] fileData = MetadataFileReader.ReadBytes(DBNAMES_FILE_NAME);
            using (StreamReader reader = MetadataFileReader.CreateReader(fileData))
            {
                DBNamesCash = DBNamesFileParser.Parse(reader);
            }

        }

        #endregion
    }
}