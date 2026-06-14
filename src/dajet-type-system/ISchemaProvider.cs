namespace DaJet.TypeSystem
{
    public interface ISchemaProvider
    {
        MetadataEntry GetEntry(in string domain, int typeCode);
        MetadataEntry GetEntry(in string domain, Guid typeUuid);
        MetadataEntry GetEntry(in string domain, in string identifier);
        EntityDefinition GetSchema(in string domain, in string identifier);
        Entity GetEnumerationEntity(in string domain, in string identifier);
    }
}