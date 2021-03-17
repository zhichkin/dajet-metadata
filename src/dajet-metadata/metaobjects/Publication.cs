using System;
using System.Collections.Generic;

namespace DaJet.Metadata.Model
{
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