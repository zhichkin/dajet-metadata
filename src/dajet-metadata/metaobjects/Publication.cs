using System;
using System.Collections.Generic;

namespace DaJet.Metadata.Model
{
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
    public sealed class Publication : MetaObject
    {
        public bool IsDistributed { get; set; }
        public Publisher Publisher { get; set; }
        public List<MetaObject> Articles { get; set; } = new List<MetaObject>();
        public List<Subscriber> Subscribers { get; set; } = new List<Subscriber>();
    }
}