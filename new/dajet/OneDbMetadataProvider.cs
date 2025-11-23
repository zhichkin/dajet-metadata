using System.Collections.Generic;

namespace DaJet
{
    public sealed class OneDbMetadataProvider : MetadataProvider
    {
        private MetadataRegistry _registry;
        private readonly MetadataLoader _loader;
        public OneDbMetadataProvider(DataSourceType dataSource, in string connectionString)
        {
            _loader = MetadataLoader.Create(dataSource, in connectionString);
        }
        public void Dump(in string tableName, in string fileName, in string outputPath)
        {
            _loader.Dump(in tableName, in fileName, in outputPath);
        }
        public void DumpRaw(in string tableName, in string fileName, in string outputPath)
        {
            _loader.DumpRaw(in tableName, in fileName, in outputPath);
        }
        public override void Initialize()
        {
            _registry = _loader.GetMetadataRegistry();
        }
        public override int GetYearOffset()
        {
            return _loader.GetYearOffset();
        }
        public override InfoBase GetInfoBase()
        {
            InfoBase infoBase = _loader.GetInfoBase();
            
            infoBase.YearOffset = GetYearOffset();

            return infoBase;
        }
        
        public override EntityDefinition GetMetadataObject(in string fullName)
        {
            int dot = fullName.IndexOf('.');

            if (dot < 0)
            {
                return null;
            }

            string type = fullName[..dot];
            string name = fullName[(dot + 1)..];

            if (!_registry.TryGetEntry(in type, in name, out MetadataObject entry))
            {
                return null;
            }

            return _loader.Load(in type, entry.Uuid, in _registry);
        }
        public override IEnumerable<EntityDefinition> GetMetadataObjects(string typeName)
        {
            foreach (MetadataObject entry in _registry.GetMetadataObjects(typeName))
            {
                yield return _loader.Load(in typeName, entry.Uuid, in _registry);
            }
        }
        
        public EntityDefinition GetMetadataObjectWithRelations(in string metadataName)
        {
            int dot = metadataName.IndexOf('.');

            if (dot < 0)
            {
                return null;
            }

            string type = metadataName[..dot];
            string name = metadataName[(dot + 1)..];

            if (!_registry.TryGetEntry(in type, in name, out MetadataObject metadata))
            {
                return null;
            }

            return _loader.LoadWithRelations(in type, metadata.Uuid, in _registry);
        }
        public IEnumerable<EntityDefinition> GetMetadataObjectsWithRelations(string type)
        {
            foreach (MetadataObject entry in _registry.GetMetadataObjects(type))
            {
                yield return _loader.LoadWithRelations(in type, entry.Uuid, in _registry);
            }
        }

        public List<string> ResolveReferences(in List<Guid> references)
        {
            return _registry.ResolveReferences(in references);
        }
    }
}