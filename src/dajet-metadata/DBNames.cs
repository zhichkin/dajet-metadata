using DaJet.Metadata.Model;
using System.Collections.Generic;

namespace DaJet.Metadata
{
    public sealed class DBNameEntry
    {
        public MetaObject MetaObject = new MetaObject();
        public List<DBName> DBNames = new List<DBName>();
        public override string ToString()
        {
            string metadataType = MetaObject.TypeName;
            if (string.IsNullOrEmpty(metadataType))
            {
                if (DBNames.Count > 0)
                {
                    metadataType = DBNames[0].Token;
                }
            }
            return string.Format("{0} {{{1}:{2}}}", metadataType, MetaObject.TypeCode, MetaObject.UUID);
        }
    }
    public sealed class DBName
    {
        public string Token;
        public int TypeCode;
        public bool IsMainTable;
        public override string ToString()
        {
            return string.Format("{0}({1})", Token, TypeCode);
        }
    }
}