namespace DaJet
{
    public sealed class TableDefinition
    {
        public string Name { get; set; }
        public string DbName { get; set; }
        public List<PropertyDefinition> Properties { get; set; } = new();
        public List<TableDefinition> Tables { get; set; } = new();
    }
}