using DaJet.Metadata.Model;
using System;
using System.Collections.Generic;
using System.IO;

namespace DaJet.Metadata
{
    public interface IDBNamesFileParser
    {
        void Parse(StreamReader stream, InfoBase infoBase);
    }
    public sealed class DBNamesFileParser : IDBNamesFileParser
    {
        public void Parse(StreamReader stream, InfoBase infoBase)
        {
            string line = stream.ReadLine(); // 1. line
            if (line != null)
            {
                int capacity = ParseCapacity(line);
                while ((line = stream.ReadLine()) != null)
                {
                    ParseEntry(line, infoBase);
                }
            }
        }
        private string MapTokenToTypeName(string token)
        {
            if (token == MetadataTokens.VT) return MetadataObjectTypes.TablePart;
            else if (token == MetadataTokens.Acc) return MetadataObjectTypes.Account;
            else if (token == MetadataTokens.Enum) return MetadataObjectTypes.Enumeration;
            else if (token == MetadataTokens.Node) return MetadataObjectTypes.Publication;
            else if (token == MetadataTokens.Chrc) return MetadataObjectTypes.Characteristic;
            else if (token == MetadataTokens.Const) return MetadataObjectTypes.Constant;
            else if (token == MetadataTokens.AccRg) return MetadataObjectTypes.AccountingRegister;
            else if (token == MetadataTokens.InfoRg) return MetadataObjectTypes.InformationRegister;
            else if (token == MetadataTokens.AccumRg) return MetadataObjectTypes.AccumulationRegister;
            else if (token == MetadataTokens.Document) return MetadataObjectTypes.Document;
            else if (token == MetadataTokens.Reference) return MetadataObjectTypes.Catalog;
            else return MetadataObjectTypes.Unknown;
        }
        private string CreateDBName(string token, string code)
        {
            return $"_{token}{code}";
        }
        private MetadataObject CreateMetadataObject(Guid uuid, string token, string code)
        {
            if (token == MetadataTokens.Node)
            {
                return new Publication()
                {
                    FileName = uuid,
                    TypeCode = int.Parse(code),
                    TypeName = MapTokenToTypeName(token),
                    TableName = CreateDBName(token, code)
                };
            }
            return new MetadataObject()
            {
                FileName = uuid,
                TypeCode = int.Parse(code),
                TypeName = MapTokenToTypeName(token),
                TableName = CreateDBName(token, code)
            };
        }
        private MetadataProperty CreateMetadataProperty(Guid uuid, string token, string code)
        {
            return new MetadataProperty()
            {
                FileName = uuid,
                Field = CreateDBName(token, code)
            };
        }

        private int ParseCapacity(string line)
        {
            return int.Parse(line.Replace("{", string.Empty).Replace(",", string.Empty));
        }
        private void ParseEntry(string line, InfoBase infoBase)
        {
            string[] items = line.Split(',');
            if (items.Length < 3) return;

            Guid uuid = new Guid(items[0].Substring(1));
            if (uuid == Guid.Empty) return; // Системный объект метаданных

            string token = items[1].Trim('\"');
            string code = items[2].TrimEnd('}');

            if (token == MetadataTokens.Fld)
            {
                _ = infoBase.Properties.TryAdd(uuid, CreateMetadataProperty(uuid, token, code));
            }
            else if (token == MetadataTokens.VT)
            {
                _ = infoBase.TableParts.TryAdd(uuid, CreateMetadataObject(uuid, token, code));
            }
            else if (token == MetadataTokens.Acc)
            {
                _ = infoBase.Accounts.TryAdd(uuid, CreateMetadataObject(uuid, token, code));
            }
            else if (token == MetadataTokens.Reference)
            {
                _ = infoBase.Catalogs.TryAdd(uuid, CreateMetadataObject(uuid, token, code));
            }
            else if (token == MetadataTokens.Document)
            {
                _ = infoBase.Documents.TryAdd(uuid, CreateMetadataObject(uuid, token, code));
            }
            else if (token == MetadataTokens.Enum)
            {
                _ = infoBase.Enumerations.TryAdd(uuid, CreateMetadataObject(uuid, token, code));
            }
            else if (token == MetadataTokens.Node)
            {
                _ = infoBase.Publications.TryAdd(uuid, CreateMetadataObject(uuid, token, code));
            }
            else if (token == MetadataTokens.Chrc)
            {
                _ = infoBase.Characteristics.TryAdd(uuid, CreateMetadataObject(uuid, token, code));
            }
            else if (token == MetadataTokens.Const)
            {
                _ = infoBase.Constants.TryAdd(uuid, CreateMetadataObject(uuid, token, code));
            }
            else if (token == MetadataTokens.AccRg)
            {
                _ = infoBase.AccountingRegisters.TryAdd(uuid, CreateMetadataObject(uuid, token, code));
            }
            else if (token == MetadataTokens.InfoRg)
            {
                _ = infoBase.InformationRegisters.TryAdd(uuid, CreateMetadataObject(uuid, token, code));
            }
            else if (token == MetadataTokens.AccumRg)
            {
                _ = infoBase.AccumulationRegisters.TryAdd(uuid, CreateMetadataObject(uuid, token, code));
            }
            else if (token.EndsWith(MetadataTokens.ChngR) && !token.StartsWith(MetadataTokens.Config))
            {
                AttachChangeTrackingTable(infoBase, uuid, token, code);
            }
            else
            {
                //TODO: другие объекты метаданных, в том числе различные зависимые значимые типы (таблицы итогов и т.п.)
            }
        }
        private void AttachChangeTrackingTable(InfoBase infoBase, Guid uuid, string token, string code)
        {
            List<Dictionary<Guid, MetadataObject>> list = new List<Dictionary<Guid, MetadataObject>>()
            {
                infoBase.Accounts,
                infoBase.Catalogs,
                infoBase.Documents,
                infoBase.Enumerations,
                infoBase.Characteristics,
                infoBase.Constants,
                infoBase.AccountingRegisters,
                infoBase.InformationRegisters,
                infoBase.AccumulationRegisters
            };
            MetadataObject owner;
            foreach (var item in list)
            {
                if (item.TryGetValue(uuid, out owner))
                {
                    owner.MetadataObjects.Add(CreateMetadataObject(uuid, token, code));
                    break;
                }
            }
        }
    }
}