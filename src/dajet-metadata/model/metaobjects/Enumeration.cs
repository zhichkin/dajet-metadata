namespace DaJet.Metadata.Model
{
    public sealed class Enumeration : MetadataObject
    {

    }
    public sealed class EnumerationPropertyFactory : MetadataPropertyFactory
    {
        protected override void InitializePropertyNameLookup()
        {
            PropertyNameLookup.Add("idrref", "Ссылка");
            PropertyNameLookup.Add("enumorder", "Порядок");
        }
    }
}