namespace DaJet
{
    public interface IMetadataProvider
    {
        void Initialize();
        int GetYearOffset();
        InfoBase GetInfoBase();
        TableDefinition GetMetadataObject(string metadataName);
    }
}