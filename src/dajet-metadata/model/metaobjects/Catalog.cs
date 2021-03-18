namespace DaJet.Metadata.Model
{
    public sealed class Catalog : MetadataObject
    {

    }
    public sealed class CatalogPropertyFactory : MetadataPropertyFactory
    {
        protected override void InitializePropertyNameLookup()
        {
            PropertyNameLookup.Add("idrref", "Ссылка");
            PropertyNameLookup.Add("version", "ВерсияДанных");
            PropertyNameLookup.Add("marked", "ПометкаУдаления");
            PropertyNameLookup.Add("predefinedid", "Предопределённый");
            PropertyNameLookup.Add("code", "Код");
            PropertyNameLookup.Add("description", "Наименование");
            PropertyNameLookup.Add("folder", "ЭтоГруппа");
            PropertyNameLookup.Add("parentidrref", "Родитель");
            // TODO: учесть свойство "Владелец" для PostgreSQL
        }
    }
}