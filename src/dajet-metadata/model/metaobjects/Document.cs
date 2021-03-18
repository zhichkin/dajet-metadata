namespace DaJet.Metadata.Model
{
    public sealed class Document : MetadataObject
    {
    }
    public sealed class DocumentPropertyFactory : MetadataPropertyFactory
    {
        protected override void InitializePropertyNameLookup()
        {
            PropertyNameLookup.Add("idrref", "Ссылка");
            PropertyNameLookup.Add("version", "ВерсияДанных");
            PropertyNameLookup.Add("marked", "ПометкаУдаления");
            PropertyNameLookup.Add("date_time", "Дата");
            PropertyNameLookup.Add("number", "Номер");
            PropertyNameLookup.Add("posted", "Проведён");
        }
    }
}