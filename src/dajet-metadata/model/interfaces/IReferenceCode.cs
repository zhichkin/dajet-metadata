namespace DaJet.Metadata.Model
{
    public interface IReferenceCode
    {
        int CodeLength { get; set; }
        CodeType CodeType { get; set; }
    }
}