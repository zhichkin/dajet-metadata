namespace DaJet.Metadata.Model
{
    public sealed class MetaField
    {
        public string Name { get; set; }
        public FieldPurpose Purpose { get; set; } = FieldPurpose.Value;
        public string TypeName { get; set; }
        public int Length { get; set; }
        public int Precision { get; set; }
        public int Scale { get; set; }
        public bool IsNullable { get; set; }
        public int KeyOrdinal { get; set; }
        public bool IsPrimaryKey { get; set; }
        public override string ToString() { return Name; }
    }
}