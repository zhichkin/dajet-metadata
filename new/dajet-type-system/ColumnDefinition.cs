using DaJet.TypeSystem;

namespace DaJet.TypeSystem
{
    public sealed class ColumnDefinition
    {
        public string Name { get; set; } = string.Empty;
        public DataType Type { get; set; }
        public ColumnPurpose Purpose { get; set; }
        public bool IsGenerated { get; set; }
        public bool IsPrimaryKey { get; set; }
        public override string ToString()
        {
            return string.Format("[0] {1}", Purpose, Name);
        }
    }
}