namespace DaJet.Metadata.Model
{
    public sealed class Account : ApplicationObject
    {
    }
    public sealed class AccountPropertyFactory : MetadataPropertyFactory
    {
        protected override void InitializePropertyNameLookup()
        {
            PropertyNameLookup.Add("_idrref", "Ссылка");
            PropertyNameLookup.Add("_version", "ВерсияДанных");
            PropertyNameLookup.Add("_marked", "ПометкаУдаления");
            PropertyNameLookup.Add("_predefinedid", "Предопределённый");
            PropertyNameLookup.Add("_parentidrref", "Родитель");
            PropertyNameLookup.Add("_code", "Код");
            PropertyNameLookup.Add("_description", "Наименование");
            PropertyNameLookup.Add("_orderfield", "Порядок");
            PropertyNameLookup.Add("_kind", "Тип");
            PropertyNameLookup.Add("_offbalance", "Забалансовый");
        }
    }
}