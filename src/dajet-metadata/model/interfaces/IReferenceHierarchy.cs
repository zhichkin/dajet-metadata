namespace DaJet.Metadata.Model
{
    public interface IReferenceHierarchy
    {
        bool IsHierarchical { get; set; }
        HierarchyType HierarchyType { get; set; }
    }
}