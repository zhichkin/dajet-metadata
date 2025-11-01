namespace DaJet
{
    public interface IMetadataProvider
    {
        void Initialize();
        int GetYearOffset();
        InfoBase GetInfoBase();
        MetadataObject GetMetadataObject(string metadataName);
        T GetMetadataObject<T>(string metadataName) where T : MetadataObject;
    }
}