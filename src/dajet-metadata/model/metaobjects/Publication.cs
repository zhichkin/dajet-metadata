using System;
using System.Collections.Generic;
using System.Linq;

namespace DaJet.Metadata.Model
{
    public sealed class PublicationPropertyFactory : MetadataPropertyFactory
    {
        protected override void InitializePropertyNameLookup()
        {
            // все реквизиты обязательные
            PropertyNameLookup.Add("_idrref", "Ссылка");
            PropertyNameLookup.Add("_marked", "ПометкаУдаления");
            PropertyNameLookup.Add("_version", "ВерсияДанных");
            PropertyNameLookup.Add("_predefinedid", "Предопределённый");
            PropertyNameLookup.Add("_code", "Код");
            PropertyNameLookup.Add("_description", "Наименование");
            PropertyNameLookup.Add("_sentno", "НомерОтправленного");
            PropertyNameLookup.Add("_receivedno", "НомерПринятого");
        }
        private void PublicationAddPropertyНомерПринятого(MetadataObject metaObject)
        {
            MetadataProperty property = metaObject.Properties.Where(p => p.Name == "НомерПринятого").FirstOrDefault();
            if (property != null) return;
            property = new MetadataProperty()
            {
                Name = "НомерПринятого",
                FileName = Guid.Empty,
                Purpose = PropertyPurpose.System
            };
            property.PropertyType.IsUuid = true;
            property.Fields.Add(new DatabaseField()
            {
                Name = "_ReceivedNo",
                Length = 9,
                Scale = 0,
                Precision = 10,
                TypeName = "numeric",
                IsNullable = false
            });
            metaObject.Properties.Add(property);
        }
        private void PublicationAddPropertyНомерОтправленного(MetadataObject metaObject)
        {
            MetadataProperty property = metaObject.Properties.Where(p => p.Name == "НомерОтправленного").FirstOrDefault();
            if (property != null) return;
            property = new MetadataProperty()
            {
                Name = "НомерОтправленного",
                FileName = Guid.Empty,
                Purpose = PropertyPurpose.System
            };
            property.PropertyType.IsUuid = true;
            property.Fields.Add(new DatabaseField()
            {
                Name = "_SentNo",
                Length = 9,
                Scale = 0,
                Precision = 10,
                TypeName = "numeric",
                IsNullable = false
            });
            metaObject.Properties.Add(property);
        }
    }
    public sealed class Publication : MetadataObject
    {
        public bool IsDistributed { get; set; }
        public Publisher Publisher { get; set; }
        public List<Subscriber> Subscribers { get; set; } = new List<Subscriber>();
        public List<MetadataObject> Articles { get; set; } = new List<MetadataObject>();
    }
    public sealed class Publisher
    {
        public Guid Uuid { get; set; } = Guid.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
    public sealed class Subscriber
    {
        public Guid Uuid { get; set; } = Guid.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsMarkedForDeletion { get; set; }
    }
}