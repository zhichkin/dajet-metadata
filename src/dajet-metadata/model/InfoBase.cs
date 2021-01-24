using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DaJet.Metadata.Model
{
    public sealed class InfoBase
    {
        public string Name { get; set; } = string.Empty;
        public string Alias { get; set; } = string.Empty;
        public ConcurrentDictionary<string, MetaObject> Catalogs { get; } = new ConcurrentDictionary<string, MetaObject>();
        public ConcurrentDictionary<string, MetaObject> Accounts { get; } = new ConcurrentDictionary<string, MetaObject>();
        public ConcurrentDictionary<string, MetaObject> Documents { get; } = new ConcurrentDictionary<string, MetaObject>();
        public ConcurrentDictionary<string, MetaObject> Constants { get; } = new ConcurrentDictionary<string, MetaObject>();
        public ConcurrentDictionary<string, MetaObject> Enumerations { get; } = new ConcurrentDictionary<string, MetaObject>();
        public ConcurrentDictionary<string, MetaObject> Publications { get; } = new ConcurrentDictionary<string, MetaObject>();
        public ConcurrentDictionary<string, MetaObject> Characteristics { get; } = new ConcurrentDictionary<string, MetaObject>();
        public ConcurrentDictionary<string, MetaObject> AccountingRegisters { get; } = new ConcurrentDictionary<string, MetaObject>();
        public ConcurrentDictionary<string, MetaObject> InformationRegisters { get; } = new ConcurrentDictionary<string, MetaObject>();
        public ConcurrentDictionary<string, MetaObject> AccumulationRegisters { get; } = new ConcurrentDictionary<string, MetaObject>();
    }
}