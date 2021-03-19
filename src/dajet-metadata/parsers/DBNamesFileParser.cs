﻿using DaJet.Metadata.Model;
using System;
using System.Collections.Generic;
using System.IO;

namespace DaJet.Metadata
{
    public interface IDBNamesFileParser
    {
        void Parse(StreamReader stream, InfoBase infoBase);
        DatabaseProviders DatabaseProvider { get; }
        void UseDatabaseProvider(DatabaseProviders databaseProvider);
    }
    public sealed class DBNamesFileParser : IDBNamesFileParser
    {
        private readonly IMetadataObjectsManager MetadataManager = new MetadataObjectsManager();
        public DatabaseProviders DatabaseProvider { get; private set; } = DatabaseProviders.SQLServer;
        public void UseDatabaseProvider(DatabaseProviders databaseProvider)
        {
            DatabaseProvider = databaseProvider;
            MetadataManager.UseDatabaseProvider(DatabaseProvider);
        }
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
            int code = int.Parse(items[2].TrimEnd('}'));

            if (token == MetadataTokens.Fld)
            {
                _ = infoBase.Properties.TryAdd(uuid, MetadataManager.CreateProperty(uuid, token, code));
                return;
            }

            Type type = MetadataManager.GetTypeByToken(token);
            if (type == null) return; // unsupported type of metadata object

            MetadataObject metaObject = MetadataManager.CreateObject(uuid, token, code);
            if (metaObject == null) return; // unsupported type of metadata object

            if (token == MetadataTokens.VT)
            {
                _ = infoBase.TableParts.TryAdd(uuid, metaObject);
                return;
            }

            if (!infoBase.AllTypes.TryGetValue(type, out Dictionary<Guid, MetadataObject> collection))
            {
                return; // unsupported collection of metadata objects
            }

            _ = collection.TryAdd(uuid, metaObject);
        }
        private void AttachChangeTrackingTable(InfoBase infoBase, Guid uuid, string token, int code)
        {
            // [ChngR, но не ConfigChngR]
            // Список объектов, которые могут иметь таблицы изменений в планах обмена 1С
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
                    owner.MetadataObjects.Add(MetadataManager.CreateObject(uuid, token, code));
                    break;
                }
            }
        }
    }
}