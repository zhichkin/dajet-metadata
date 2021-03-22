namespace DaJet.Metadata.Model
{
    public sealed class Constant : MetadataObject
    {

    }
    public sealed class ConstantPropertyFactory : MetadataPropertyFactory
    {
        protected override void InitializePropertyNameLookup()
        {
            PropertyNameLookup.Add("_recordkey", "КлючЗаписи"); // binary(1)
        }
    }
}