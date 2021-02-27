using DaJet.Metadata.Model;
using System.IO;

namespace DaJet.Metadata
{
    /// <summary>
    /// Интерфейс выполняет чтение и разбор файла, содержащего значения свойств конфигурации 1С.
    /// Зависит от интерфейса IMetadataFileReader, обеспечивающего чтение файлов из базы данных SQL Server.
    /// </summary>
    public interface IConfigurationFileParser
    {
        ///<summary>Получает имя основного файла метаданных конфигурации 1С из файла "root"</summary>
        ///<returns>Имя основного файла метаданных конфигурации 1С (UUID)</returns>
        string GetConfigurationFileName();

        ///<summary>Выполняет чтение свойств конфигурации 1С</summary>
        ///<returns>Значения свойств конфигурации ConfigInfo</returns>
        ConfigInfo ReadConfigurationProperties();
    }
    public sealed class ConfigurationFileParser : IConfigurationFileParser
    {
        private const string ROOT_FILE_NAME = "root";

        private readonly IMetadataFileReader MetadataFileReader;

        public ConfigurationFileParser(IMetadataFileReader metadataFileReader)
        {
            MetadataFileReader = metadataFileReader;
        }

        private string ParseRootFileName(StreamReader reader)
        {
            string rootContent = reader.ReadToEnd();
            string[] lines = rootContent.Split(',');
            string uuid = lines[1];
            return uuid;
        }
        public string GetConfigurationFileName()
        {
            string fileName = null;
            byte[] fileData = MetadataFileReader.ReadBytes(ROOT_FILE_NAME);
            using (StreamReader reader = MetadataFileReader.CreateReader(fileData))
            {
                fileName = ParseRootFileName(reader);
            }
            return fileName;
        }
        public ConfigInfo ReadConfigurationProperties()
        {
            ConfigInfo config = new ConfigInfo();

            int version = MetadataFileReader.GetPlatformRequiredVersion();

            string fileName = GetConfigurationFileName();
            byte[] fileData = MetadataFileReader.ReadBytes(fileName);
            using (StreamReader reader = MetadataFileReader.CreateReader(fileData))
            {
                string line = ReadLines(reader, 8); // 8. line
                config.Name = ParseConfigName(line);
                line = ReadLines(reader, 1); // 9. line
                config.Alias = ParseConfigAlias(line);
                config.Comment = ParseConfigComment(line);
                line = ReadLines(reader, 6); // 15. line
                config.ConfigVersion = ParseConfigVersion(line);
                config.DataLockingMode = ParseDataLockingMode(line);
                line = ReadLines(reader, 1); // 16. line
                config.AutoNumberingMode = ParseAutoNumberingMode(line);
                line = ReadLines(reader, 1); // 17. line
                if (version < 80300)
                {
                    config.Version = ParseOldVersion(line);
                }
                else
                {
                    config.Version = ParseVersion(line);
                }
                line = ReadLines(reader, 4); // 21. line
                config.ModalWindowMode = ParseModalWindowMode(line);
                config.UICompatibilityMode = ParseUICompatibilityMode(line);

                SkipBasicRolesBlock(reader); // Основные роли конфигурации (будут использоваться, если не вводятся пользователи)

                //{
                //{"#",e4c53f94-e5f7-4a34-8c10-218bd811cae1,0},
                //{"B",0}
                //}
                SkipUnknownObjectsBlock(reader);

                line = reader.ReadLine();
                config.SyncCallsMode = ParseSyncCallsMode(line);
            }
            return config;
        }
        private string ReadLines(StreamReader reader, int linesToRead)
        {
            for (int i = 1; i < linesToRead; i++)
            {
                _ = reader.ReadLine();
            }
            return reader.ReadLine();
        }
        private string ParseConfigName(string line)
        {
            string[] lines = line.Split(',');
            return lines[3].Trim('"');
        }
        private string ParseConfigAlias(string line)
        {
            string[] lines = line.Split('"');
            return lines[3];
        }
        private string ParseConfigComment(string line)
        {
            string[] lines = line.Split('"');
            return lines[5];
        }
        private DataLockingMode ParseDataLockingMode(string line)
        {
            return (DataLockingMode)int.Parse(line.Substring(line.Length - 2, 1));
        }
        private string ParseConfigVersion(string line)
        {
            string[] lines = line.Split(',');
            return lines[lines.Length - 4].Trim('"');
        }
        private AutoNumberingMode ParseAutoNumberingMode(string line)
        {
            return (AutoNumberingMode)int.Parse(line.Substring(line.Length - 2, 1));
        }
        private string ParseVersion(string line)
        {
            return line.Substring(line.Length - 6, 5);
        }
        private string ParseOldVersion(string line)
        {
            int version = int.Parse(line.Substring(line.Length - 2, 1));
            if (version == 0) return "80216";
            else if (version == 1) return "80100";
            else if (version == 2) return "80213";
            else return version.ToString();
        }
        private ModalWindowMode ParseModalWindowMode(string line)
        {
            string[] lines = line.Split(',');
            return (ModalWindowMode)int.Parse(lines[3]);
        }
        private UICompatibilityMode ParseUICompatibilityMode(string line)
        {
            return (UICompatibilityMode)int.Parse(line.Substring(line.Length - 2, 1));
        }
        private SyncCallsMode ParseSyncCallsMode(string line)
        {
            string[] lines = line.Split(',');
            return (SyncCallsMode)int.Parse(lines[1]);
        }
        private void SkipBasicRolesBlock(StreamReader reader)
        {
            string line = reader.ReadLine(); // 22. line
            string[] lines = line.Split(',');
            int count = int.Parse(lines[1].TrimEnd('}'));
            if (count > 0)
            {
                count = (count * 3) + 1;
                _ = ReadLines(reader, count);
            }
        }
        private void SkipUnknownObjectsBlock(StreamReader reader)
        {
            string line = reader.ReadLine();
            int count = int.Parse(line.TrimStart('{').TrimEnd(','));
            if (count > 0)
            {
                count = count * 4;
                _ = ReadLines(reader, count);
            }
        }
    }
}