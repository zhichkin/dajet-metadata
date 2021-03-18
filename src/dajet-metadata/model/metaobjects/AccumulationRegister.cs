namespace DaJet.Metadata.Model
{
    public sealed class AccumulationRegister : MetadataObject
    {
    }
    public sealed class AccumulationRegisterPropertyFactory : MetadataPropertyFactory
    {
        protected override void InitializePropertyNameLookup()
        {
            PropertyNameLookup.Add("period", "Период");
            PropertyNameLookup.Add("recorder", "Регистратор");
            // TODO: добавить остальные свойства
        }
    }
}