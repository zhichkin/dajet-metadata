using System.Text.Json.Serialization;

namespace DaJet.TypeSystem
{
    public sealed class ColumnDefinition
    {
        public string Name { get; set; } = string.Empty;
        public DataType Type { get; set; }
        public ColumnPurpose Purpose { get; set; }
        [JsonIgnore] public bool IsGenerated { get; set; }
        [JsonIgnore] public bool IsPrimaryKey { get; set; }
        public override string ToString()
        {
            return string.Format("[0] {1}", Purpose, Name);
        }
    }
}