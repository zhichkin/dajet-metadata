using DaJet.TypeSystem;

namespace DaJet.TypeSystem
{
    public sealed class PropertyDefinition
    {
        public string Name { get; set; } = string.Empty;
        public DataType Type { get; set; }
        public PropertyPurpose Purpose { get; set; }
        public List<ColumnDefinition> Columns { get; set; } = new();
        public List<Guid> References { get; set; }
        public override string ToString()
        {
            return string.Format("[{0}] {1}", Purpose, Name);
        }
    }
}