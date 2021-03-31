using System;
using System.Linq;

namespace DaJet.Metadata.Model
{
    public sealed class Document : ApplicationObject
    {
    }
    public sealed class DocumentPropertyFactory : MetadataPropertyFactory
    {
        protected override void InitializePropertyNameLookup()
        {
            PropertyNameLookup.Add("_idrref", "Ссылка");
            PropertyNameLookup.Add("_version", "ВерсияДанных");
            PropertyNameLookup.Add("_marked", "ПометкаУдаления");
            PropertyNameLookup.Add("_date_time", "Дата");
            PropertyNameLookup.Add("_number", "Номер"); // необязательный
            PropertyNameLookup.Add("_posted", "Проведён");
        }
        private MetadataProperty CreateProperty_Дата()
        {
            MetadataProperty property = new MetadataProperty()
            {
                Name = "Дата",
                FileName = Guid.Empty,
                Purpose = PropertyPurpose.System
            };
            property.PropertyType.CanBeDateTime = true;
            property.Fields.Add(new DatabaseField() // TODO: учесть тип СУБД
            {
                Name = "_Date_Time",
                Length = 6,
                Precision = 19,
                TypeName = "datetime2"
            });
            return property;
        }
    }
}