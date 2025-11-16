namespace DaJet
{
    internal enum AutomaticUsage
    {
        Use = 0,
        DoNotUse = 1
    }
    internal enum SharedPropertyUsage
    {
        Auto = 0,
        Use = 1,
        DoNotUse = 2
    }
    ///<summary>Разделение данных между ИБ</summary>
    internal enum DataSeparationUsage
    {
        ///<summary>Разделять</summary>
        Use = 0,
        ///<summary>Не использовать</summary>
        DoNotUse = 1
    }
    ///<summary>Режим использования разделяемых данных</summary>
    internal enum DataSeparationMode
    {
        ///<summary>Независимо</summary>
        Independent = 0,
        ///<summary>Независимо и совместно</summary>
        IndependentAndShared = 1
    }
    internal sealed class SharedProperty : DatabaseObject
    {
        internal SharedProperty(Guid uuid) : base(uuid, 0, MetadataToken.Fld) { }
        internal DataType Type { get; set; }
        public AutomaticUsage AutomaticUsage { get; set; }
        public Dictionary<Guid, SharedPropertyUsage> UsageSettings { get; } = new();
        public DataSeparationUsage DataSeparationUsage { get; set; } = DataSeparationUsage.DoNotUse;
        public DataSeparationMode DataSeparationMode { get; set; } = DataSeparationMode.Independent;
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.Fld)
            {
                TypeCode = code;
            }
        }
    }
}