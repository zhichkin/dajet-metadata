namespace DaJet.Metadata.Parsers
{
    class ModuleFileParser
    {
        //TODO: чтение исходного кода 1С общих модулей конфигурации
        //private const string COMMON_MODULES_COLLECTION_UUID = "0fe48980-252d-11d6-a3c7-0050bae0a776";
        //public List<CommonModuleInfo> GetCommonModules()
        //{
        //    List<CommonModuleInfo> list = new List<CommonModuleInfo>();
        //    string fileName = GetRootConfigFileName();
        //    string metadata = ReadConfigFile(fileName);
        //    using (StringReader reader = new StringReader(metadata))
        //    {
        //        string line = reader.ReadLine();
        //        while (!string.IsNullOrEmpty(line))
        //        {
        //            if (line.Substring(1, 36) == COMMON_MODULES_COLLECTION_UUID)
        //            {
        //                list = ParseCommonModules(line); break;
        //            }
        //            line = reader.ReadLine();
        //        }
        //    }
        //    return list;
        //}
        //private List<CommonModuleInfo> ParseCommonModules(string line)
        //{
        //    List<CommonModuleInfo> list = new List<CommonModuleInfo>();
        //    string[] fileNames = line.TrimStart('{').TrimEnd('}').Split(',');
        //    if (int.TryParse(fileNames[1], out int count) && count == 0)
        //    {
        //        return list;
        //    }
        //    int offset = 2;
        //    for (int i = 0; i < count; i++)
        //    {
        //        CommonModuleInfo moduleInfo = ReadCommonModuleMetadata(fileNames[i + offset]);
        //        list.Add(moduleInfo);
        //    }
        //    return list;
        //}
        //private CommonModuleInfo ReadCommonModuleMetadata(string fileName)
        //{
        //    string metadata = ReadConfigFile(fileName);
        //    string uuid = string.Empty;
        //    string name = string.Empty;
        //    using (StringReader reader = new StringReader(metadata))
        //    {
        //        _ = reader.ReadLine(); // 1. line
        //        _ = reader.ReadLine(); // 2. line
        //        _ = reader.ReadLine(); // 3. line
        //        string line = reader.ReadLine(); // 4. line
        //        string[] lines = line.Split(',');
        //        uuid = lines[2].TrimEnd('}');
        //        name = lines[3].Trim('"');
        //    }
        //    return new CommonModuleInfo(uuid, name);
        //}
        //public string ReadCommonModuleSourceCode(CommonModuleInfo module)
        //{
        //    return ReadConfigFile(module.UUID + ".0");
        //}
    }
}