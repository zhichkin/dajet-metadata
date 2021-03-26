namespace DaJet.Metadata.Model
{
    public sealed class Catalog : MetadataObject
    {

    }
    public sealed class CatalogPropertyFactory : MetadataPropertyFactory
    {
        protected override void InitializePropertyNameLookup()
        {
            PropertyNameLookup.Add("_idrref", "Ссылка");
            PropertyNameLookup.Add("_version", "ВерсияДанных");
            PropertyNameLookup.Add("_marked", "ПометкаУдаления");
            PropertyNameLookup.Add("_predefinedid", "Предопределённый");
            PropertyNameLookup.Add("_code", "Код"); // необязательный 1.17 - длина кода, 1.18 - тип кода (0 - строка, 1 - число)
            PropertyNameLookup.Add("_description", "Наименование"); // необязательный
            PropertyNameLookup.Add("_folder", "ЭтоГруппа"); // необязательный
            PropertyNameLookup.Add("_parentidrref", "Родитель"); // необязательный
            PropertyNameLookup.Add("_owneridrref", "Владелец"); // необязательный
            PropertyNameLookup.Add("_ownerid_type", "Владелец"); // необязательный
            PropertyNameLookup.Add("_ownerid_rtref", "Владелец"); // необязательный
            PropertyNameLookup.Add("_ownerid_rrref", "Владелец"); // необязательный
            // TODO: свойство "Владелец" (необязательный)
            // _OwnerIDRRef binary(16)
            // _OwnerID_TYPE binary(1)
            // _OwnerID_RTRef binary(4)
            // _OwnerID_RRRef binary(16)
        }
    }
}