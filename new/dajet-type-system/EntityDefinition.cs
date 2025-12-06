namespace DaJet.TypeSystem
{
    public sealed class EntityDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string DbName { get; set; } = string.Empty;
        public List<PropertyDefinition> Properties { get; set; } = new();
        public List<EntityDefinition> Entities { get; set; } = new();
        public override string ToString()
        {
            return string.Format("[{0}] {1}", DbName, Name);
        }

        public PropertyDefinition GetPropertyByColumnName(in string columnName)
        {
            foreach (PropertyDefinition property in Properties)
            {
                foreach (ColumnDefinition column in property.Columns)
                {
                    if (column.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        return property;
                    }
                }
            }

            return null;
        }
    }
}