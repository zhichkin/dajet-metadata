namespace DaJet.TypeSystem
{
    public sealed class PropertyDefinition
    {
        public string Name { get; set; } = string.Empty;
        public DataType Type { get; set; }
        public PropertyPurpose Purpose { get; set; }
        public List<ColumnDefinition> Columns { get; set; } = new();
        public List<Guid> References { get; set; } = new();
        public ColumnDefinition GetColumnByPurpose(ColumnPurpose purpose)
        {
            if (Columns is null || Columns.Count == 0)
            {
                return null;
            }

            ColumnDefinition column;

            for (int i = 0; i < Columns.Count; i++)
            {
                column = Columns[i];

                if (column.Purpose == purpose)
                {
                    return column;
                }
            }

            return null;
        }
        public override string ToString()
        {
            return string.Format("[{0}] {1}", Purpose, Name);
        }
    }
}