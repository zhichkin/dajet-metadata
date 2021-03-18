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
            PropertyNameLookup.Add("idrref", "Ссылка"); // _Reference31_IDRRef binary(16)
            PropertyNameLookup.Add("keyfield", "Ключ"); // binary(4)
            PropertyNameLookup.Add("lineno", "НомерСтроки"); // _LineNo49 numeric(5,0) - DBNames
        }
        protected override string LookupPropertyName(string fieldName)
        {
            if (fieldName.Substring(0, 6) == "lineno")
            {
                return "НомерСтроки";
            }
            if (fieldName.Substring(fieldName.Length - 6, 6) == "idrref")
            {
                return "Ссылка";
            }
            return base.LookupPropertyName(fieldName);
        }
    }
}