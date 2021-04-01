namespace DaJet.Metadata.Model
{
    public sealed class Characteristic : ApplicationObject
    {
        
    }
    public sealed class CharacteristicPropertyFactory : MetadataPropertyFactory
    {
        protected override void InitializePropertyNameLookup()
        {
            PropertyNameLookup.Add("_idrref", "Ссылка");
            PropertyNameLookup.Add("_version", "ВерсияДанных");
            PropertyNameLookup.Add("_marked", "ПометкаУдаления");
            PropertyNameLookup.Add("_predefinedid", "Предопределённый");
            PropertyNameLookup.Add("_parentidrref", "Родитель"); // необязательный
            PropertyNameLookup.Add("_folder", "ЭтоГруппа"); // необязательный
            PropertyNameLookup.Add("_code", "Код"); // необязательный
            PropertyNameLookup.Add("_description", "Наименование"); // необязательный
            PropertyNameLookup.Add("_type", "ТипЗначения");
        }
    }
}