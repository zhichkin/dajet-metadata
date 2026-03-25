namespace DaJet.TypeSystem
{
    public interface ISchemaProvider
    {
        EntityDefinition GetSchema(in string domain, in string identifier);
    }
}