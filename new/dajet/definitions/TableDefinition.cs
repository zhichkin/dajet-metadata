namespace DaJet
{
    public sealed class TableDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string DbName { get; set; } = string.Empty;
        public List<PropertyDefinition> Properties { get; set; } = new();
        public List<TableDefinition> Tables { get; set; } = new();
        public override string ToString()
        {
            return string.Format("[{0}] {1}", DbName, Name);
        }
    }
}