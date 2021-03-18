namespace DaJet.Metadata.Model
{
    public sealed class AccountingRegister : MetadataObject
    {

    }
    public sealed class AccountingRegisterPropertyFactory : MetadataPropertyFactory
    {
        protected override void InitializePropertyNameLookup()
        {
            PropertyNameLookup.Add("period", "Период");
            PropertyNameLookup.Add("recorder", "Регистратор");
            // TODO: добавить остальные свойства
        }
    }
}