using DaJet.Metadata.Model;
using System;
using System.Collections.Generic;
using System.IO;

namespace DaJet.Metadata
{
    public sealed class DBNamesCash
    {
        public Dictionary<Guid, MetaObject> ValueTypes { get; } = new Dictionary<Guid, MetaObject>();
        public Dictionary<Guid, MetaObject> ReferenceTypes { get; } = new Dictionary<Guid, MetaObject>();
        public Dictionary<Guid, MetaObject> TableParts { get; } = new Dictionary<Guid, MetaObject>();
        public Dictionary<Guid, MetaProperty> Properties { get; } = new Dictionary<Guid, MetaProperty>();
    }
    public interface IDBNamesFileParser
    {
        DBNamesCash Parse(StreamReader stream);
    }
    public sealed class DBNamesFileParser : IDBNamesFileParser
    {
        public DBNamesCash Parse(StreamReader stream)
        {
            DBNamesCash cash = new DBNamesCash();
            string line = stream.ReadLine(); // 1. line
            if (line != null)
            {
                int capacity = ParseCapacity(line);
                while ((line = stream.ReadLine()) != null)
                {
                    ParseEntry(line, cash);
                }
            }
            return cash;
        }
        private bool IsValueType(string token)
        {
            return token == MetadataTokens.Const
                || token == MetadataTokens.AccRg
                || token == MetadataTokens.InfoRg
                || token == MetadataTokens.AccumRg;
        }
        private bool IsReferenceType(string token)
        {
            return token == MetadataTokens.Acc
                || token == MetadataTokens.Enum
                || token == MetadataTokens.Chrc
                || token == MetadataTokens.Node
                || token == MetadataTokens.Document
                || token == MetadataTokens.Reference;
        }
        private string MapTokenToTypeName(string token)
        {
            if (token == MetadataTokens.VT) return MetaObjectTypes.TablePart;
            else if (token == MetadataTokens.Acc) return MetaObjectTypes.Account;
            else if (token == MetadataTokens.Enum) return MetaObjectTypes.Enumeration;
            else if (token == MetadataTokens.Node) return MetaObjectTypes.Publication;
            else if (token == MetadataTokens.Chrc) return MetaObjectTypes.Characteristic;
            else if (token == MetadataTokens.Const) return MetaObjectTypes.Constant;
            else if (token == MetadataTokens.AccRg) return MetaObjectTypes.AccountingRegister;
            else if (token == MetadataTokens.InfoRg) return MetaObjectTypes.InformationRegister;
            else if (token == MetadataTokens.AccumRg) return MetaObjectTypes.AccumulationRegister;
            else if (token == MetadataTokens.Document) return MetaObjectTypes.Document;
            else if (token == MetadataTokens.Reference) return MetaObjectTypes.Catalog;
            else return MetaObjectTypes.Unknown;
        }
        private string CreateDBName(string token, string code)
        {
            return $"_{token}{code}";
        }
        private MetaObject CreateMetaObject(Guid uuid, string token, string code)
        {
            return new MetaObject()
            {
                UUID = uuid,
                TypeCode = int.Parse(code),
                TypeName = MapTokenToTypeName(token),
                TableName = CreateDBName(token, code)
            };
        }
        private MetaProperty CreateMetaProperty(Guid uuid, string token, string code)
        {
            return new MetaProperty()
            {
                UUID = uuid,
                Field = CreateDBName(token, code)
            };
        }

        private int ParseCapacity(string line)
        {
            return int.Parse(line.Replace("{", string.Empty).Replace(",", string.Empty));
        }
        private void ParseEntry(string line, DBNamesCash cash)
        {
            string[] items = line.Split(',');
            if (items.Length < 3) return;

            Guid uuid = new Guid(items[0].Substring(1));
            if (uuid == Guid.Empty) return; // Системный объект метаданных

            string token = items[1].Trim('\"');
            string code = items[2].TrimEnd('}');

            if (token == MetadataTokens.Fld)
            {
                _ = cash.Properties.TryAdd(uuid, CreateMetaProperty(uuid, token, code));
            }
            else if (IsValueType(token))
            {
                _ = cash.ValueTypes.TryAdd(uuid, CreateMetaObject(uuid, token, code));
            }
            else if (IsReferenceType(token))
            {
                _ = cash.ReferenceTypes.TryAdd(uuid, CreateMetaObject(uuid, token, code));
            }
            else if (token == MetadataTokens.VT)
            {
                _ = cash.TableParts.TryAdd(uuid, CreateMetaObject(uuid, token, code));
            }
            else if (token.EndsWith(MetadataTokens.ChngR) && !token.StartsWith(MetadataTokens.Config))
            {
                MetaObject owner;
                if (cash.ValueTypes.TryGetValue(uuid, out owner))
                {
                    owner.MetaObjects.Add(CreateMetaObject(uuid, token, code));
                }
                else if (cash.ReferenceTypes.TryGetValue(uuid, out owner))
                {
                    owner.MetaObjects.Add(CreateMetaObject(uuid, token, code));
                }
            }
            else
            {
                //TODO: другие объекты метаданных, в том числе различные зависимые значимые типы (таблицы итогов и т.п.)
            }
        }
    }
}