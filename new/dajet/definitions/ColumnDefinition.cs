namespace DaJet
{
    public sealed class ColumnDefinition
    {
        public string Name { get; set; } = string.Empty;
        public ColumnPurpose Purpose { get; set; } = ColumnPurpose.Default;
        public string Type { get; set; } = string.Empty;
        ///<summary>Квалификатор: длина строки в символах. Неограниченная длина равна 0.</summary>
        public int Length { get; set; }
        ///<summary>Квалификатор: определяет допустимое количество знаков после запятой.</summary>
        public int Scale { get; set; }
        ///<summary>Квалификатор: определяет разрядность числа (сумма знаков до и после запятой).</summary>
        public int Precision { get; set; }
        public bool IsGenerated { get; set; }
        public bool IsPrimaryKey { get; set; }
        public override string ToString()
        {
            return string.Format("[0] {1}", Purpose, Name);
        }
    }
}