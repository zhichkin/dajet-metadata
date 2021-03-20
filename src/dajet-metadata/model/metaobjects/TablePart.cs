namespace DaJet.Metadata.Model
{
    public sealed class TablePart : MetadataObject
    {
        public MetadataObject Owner { get; set; }
    }
    public sealed class TablePartPropertyFactory : MetadataPropertyFactory
    {
        protected override void InitializePropertyNameLookup()
        {
            // все реквизиты обязательные
            PropertyNameLookup.Add("_idrref", "Ссылка"); // _Reference31_IDRRef binary(16)
            PropertyNameLookup.Add("_keyfield", "Ключ"); // binary(4)
            PropertyNameLookup.Add("_lineno", "НомерСтроки"); // _LineNo49 numeric(5,0) - DBNames
        }
    }
}