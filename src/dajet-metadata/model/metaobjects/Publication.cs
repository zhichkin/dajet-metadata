using System;
using System.Collections.Generic;

namespace DaJet.Metadata.Model
{
    public sealed class PublicationPropertyFactory : MetadataPropertyFactory
    {
        protected override void InitializePropertyNameLookup()
        {
            PropertyNameLookup.Add("idrref", "Ссылка");
            PropertyNameLookup.Add("marked", "ПометкаУдаления");
            PropertyNameLookup.Add("version", "ВерсияДанных");
            PropertyNameLookup.Add("predefinedid", "Предопределённый");
            PropertyNameLookup.Add("code", "Код");
            PropertyNameLookup.Add("description", "Наименование");
            PropertyNameLookup.Add("sentno", "НомерОтправленного");
            PropertyNameLookup.Add("receivedno", "НомерПринятого");
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