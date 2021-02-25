using DaJet.Metadata.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DaJet.Metadata
{
    /// <summary>
    /// Интерфейс для чтения метаданных прикладных объектов конфигурации 1С
    /// </summary>
    public interface IMetadataReader
    {
        ///<summary>Загружает метаданные прикладных объектов конфигурации 1С из SQL Server</summary>
        ///<returns>Возвращает объект, содержащий метаданные прикладных объектов конфигурации 1С</returns>
        InfoBase LoadInfoBase();
    }
    /// <summary>
    /// Класс, реализующий интерфейс <see cref="IMetadataReader"/>, для чтения метаданных из SQL Server
    /// </summary>
    public sealed class MetadataReader : IMetadataReader
    {
        private const string DBNAMES_FILE_NAME = "DBNames";

        private readonly IMetadataFileReader MetadataFileReader;
        private readonly IDBNamesFileParser DBNamesFileParser = new DBNamesFileParser();
        private readonly IMetaObjectFileParser MetaObjectFileParser = new MetaObjectFileParser();

        private DBNamesCash DBNamesCash;

        public MetadataReader(IMetadataFileReader metadataFileReader)
        {
            MetadataFileReader = metadataFileReader;
        }

        public InfoBase LoadInfoBase()
        {
            InfoBase infoBase = new InfoBase();

            ReadDBNames();
            MetaObjectFileParser.UseCash(DBNamesCash);
            ReadMetaUuids();
            ReadMetaObjects();

            return infoBase;
        }

        private void ReadDBNames()
        {
            byte[] fileData = MetadataFileReader.ReadBytes(DBNAMES_FILE_NAME);
            using (StreamReader reader = MetadataFileReader.CreateReader(fileData))
            {
                DBNamesCash = DBNamesFileParser.Parse(reader);
            }
        }
        private void ReadMetaUuids()
        {
            int i = 0;
            Task[] tasks = new Task[DBNamesCash.ReferenceTypes.Count];
            foreach (var item in DBNamesCash.ReferenceTypes)
            {
                tasks[i] = Task.Factory.StartNew(
                    ReadMetaUuid,
                    item.Value,
                    CancellationToken.None,
                    TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Default);
                ++i;
            }

            try
            {
                Task.WaitAll(tasks);
            }
            catch (AggregateException ex)
            {
                foreach (Exception ie in ex.InnerExceptions)
                {
                    if (ie is OperationCanceledException)
                    {
                        //TODO: log exception
                        //break;
                    }
                    else
                    {
                        //TODO: log exception
                    }
                }
            }
        }
        private void ReadMetaUuid(object metaObject)
        {
            MetaObject obj = (MetaObject)metaObject;
            byte[] fileData = MetadataFileReader.ReadBytes(obj.UUID.ToString());
            using (StreamReader stream = MetadataFileReader.CreateReader(fileData))
            {
                MetaObjectFileParser.ParseMetaUuid(stream, obj);
            }
            DBNamesCash.MetaReferenceTypes.TryAdd(obj.MetaUuid, (MetaObject)metaObject);
        }
        private void ReadMetaObjects()
        {
            ReadValueTypes();
            ReadReferenceTypes();
        }
        private void ReadValueTypes()
        {
            int i = 0;
            Task[] tasks = new Task[DBNamesCash.ValueTypes.Count];
            foreach (var item in DBNamesCash.ValueTypes)
            {
                tasks[i] = Task.Factory.StartNew(
                    ReadMetaObject,
                    item.Value,
                    CancellationToken.None,
                    TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Default);
                ++i;
            }

            try
            {
                Task.WaitAll(tasks);
            }
            catch (AggregateException ex)
            {
                foreach (Exception ie in ex.InnerExceptions)
                {
                    if (ie is OperationCanceledException)
                    {
                        //TODO: log exception
                        //break;
                    }
                    else
                    {
                        //TODO: log exception
                    }
                }
            }
        }
        private void ReadReferenceTypes()
        {
            int i = 0;
            Task[] tasks = new Task[DBNamesCash.ReferenceTypes.Count];
            foreach (var item in DBNamesCash.ReferenceTypes)
            {
                tasks[i] = Task.Factory.StartNew(
                    ReadMetaObject,
                    item.Value,
                    CancellationToken.None,
                    TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Default);
                ++i;
            }

            try
            {
                Task.WaitAll(tasks);
            }
            catch (AggregateException ex)
            {
                foreach (Exception ie in ex.InnerExceptions)
                {
                    if (ie is OperationCanceledException)
                    {
                        //TODO: log exception
                        //break;
                    }
                    else
                    {
                        //TODO: log exception
                    }
                }
            }
        }
        private void ReadMetaObject(object metaObject)
        {
            MetaObject obj = (MetaObject)metaObject;
            byte[] fileData = MetadataFileReader.ReadBytes(obj.UUID.ToString());
            if (fileData == null)
            {
                return; // TODO: log error "Metadata file is not found"
            }
            using (StreamReader stream = MetadataFileReader.CreateReader(fileData))
            {
                MetaObjectFileParser.ParseMetaObject(stream, obj);
            }
        }
    }
}