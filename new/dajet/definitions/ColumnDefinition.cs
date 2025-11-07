namespace DaJet
{
    public sealed class ColumnDefinition
    {
        public string Name { get; set; } = string.Empty;
        public ColumnPurpose Purpose { get; set; } = ColumnPurpose.Default;
        public string Type { get; set; } = string.Empty;
        public int Length { get; set; }
        public int Scale { get; set; }
        public int Precision { get; set; }
        public bool IsGenerated { get; set; }
        public bool IsPrimaryKey { get; set; }
    }
}