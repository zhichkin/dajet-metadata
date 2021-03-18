namespace DaJet.Metadata.Model
{
    public sealed class Characteristic : MetadataObject
    {

    }
    public sealed class CharacteristicPropertyFactory : MetadataPropertyFactory
    {
        protected override void InitializePropertyNameLookup()
        {
            PropertyNameLookup.Add("idrref", "Ссылка");
            PropertyNameLookup.Add("version", "ВерсияДанных");
            PropertyNameLookup.Add("marked", "ПометкаУдаления");
            PropertyNameLookup.Add("predefinedid", "Предопределённый");
            PropertyNameLookup.Add("parentidrref", "Родитель");
            PropertyNameLookup.Add("folder", "ЭтоГруппа");
            PropertyNameLookup.Add("code", "Код");
            PropertyNameLookup.Add("description", "Наименование");
            PropertyNameLookup.Add("type", "ТипЗначения");
        }
    }
}