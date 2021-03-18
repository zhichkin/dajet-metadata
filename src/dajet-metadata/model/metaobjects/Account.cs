namespace DaJet.Metadata.Model
{
    public sealed class Account : MetadataObject
    {
    }
    public sealed class AccountPropertyFactory : MetadataPropertyFactory
    {
        protected override void InitializePropertyNameLookup()
        {
            PropertyNameLookup.Add("idrref", "Ссылка");
            PropertyNameLookup.Add("version", "ВерсияДанных");
            PropertyNameLookup.Add("marked", "ПометкаУдаления");
            PropertyNameLookup.Add("predefinedid", "Предопределённый");
            PropertyNameLookup.Add("parentidrref", "Родитель");
            PropertyNameLookup.Add("code", "Код");
            PropertyNameLookup.Add("description", "Наименование");
            PropertyNameLookup.Add("orderfield", "Порядок");
            PropertyNameLookup.Add("kind", "Тип");
            PropertyNameLookup.Add("offbalance", "Забалансовый");
        }
    }
}