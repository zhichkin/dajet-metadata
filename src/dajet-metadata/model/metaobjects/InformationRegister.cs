namespace DaJet.Metadata.Model
{
    public sealed class InformationRegister : MetadataObject
    {

    }
    public sealed class InformationRegisterPropertyFactory : MetadataPropertyFactory
    {
        protected override void InitializePropertyNameLookup()
        {
            PropertyNameLookup.Add("period", "Период");
            // TODO: добавить остальные свойства
        }
    }
}