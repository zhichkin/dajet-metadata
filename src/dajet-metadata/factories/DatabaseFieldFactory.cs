namespace DaJet.Metadata.Model
{
    public interface IDatabaseFieldFactory
    {
        DatabaseField CreateField();
    }
    public sealed class DatabaseFieldFactory : IDatabaseFieldFactory
    {
        public DatabaseField CreateField()
        {
            throw new System.NotImplementedException();
        }
    }
}