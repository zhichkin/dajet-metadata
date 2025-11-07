namespace DaJet
{
    public sealed class PropertyDefinition
    {
        public string Name { get; set; } = string.Empty;
        public TypeDefinition Type { get; set; } = new();
        public PropertyPurpose Purpose { get; set; } = PropertyPurpose.System;
        public List<ColumnDefinition> Columns { get; set; }
    }
}