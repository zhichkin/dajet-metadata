using DaJet.Metadata.Model;
using System;
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
        private sealed class ReadMetaUuidParameters
        {
            internal InfoBase InfoBase { get; set; }
            internal MetadataObject MetadataObject { get; set; }
        }

        private const string DBNAMES_FILE_NAME = "DBNames";

        private readonly IMetadataFileReader MetadataFileReader;
        private readonly IDBNamesFileParser DBNamesFileParser = new DBNamesFileParser();
        private readonly IMetadataObjectFileParser MetadataObjectFileParser = new MetadataObjectFileParser();

        public MetadataReader(IMetadataFileReader metadataFileReader)
        {
            MetadataFileReader = metadataFileReader;
        }

        public InfoBase LoadInfoBase()
        {
            InfoBase infoBase = new InfoBase();
            ReadDBNames(infoBase);
            MetadataObjectFileParser.UseInfoBase(infoBase);
            ReadMetaUuids(infoBase);
            ReadMetadataObjects(infoBase);
            return infoBase;
        }

        private void ReadDBNames(InfoBase infoBase)
        {
            byte[] fileData = MetadataFileReader.ReadBytes(DBNAMES_FILE_NAME);
            using (StreamReader reader = MetadataFileReader.CreateReader(fileData))
            {
                DBNamesFileParser.Parse(reader, infoBase);
            }
        }
        private void ReadMetaUuids(InfoBase infoBase)
        {
            foreach (var collection in infoBase.ReferenceTypes)
            {
                int i = 0;
                Task[] tasks = new Task[collection.Count];
                foreach (var item in collection)
                {
                    ReadMetaUuidParameters parameters = new ReadMetaUuidParameters()
                    {
                        InfoBase = infoBase,
                        MetadataObject = item.Value
                    };
                    tasks[i] = Task.Factory.StartNew(
                        ReadMetaUuid,
                        parameters,
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
        }
        private void ReadMetaUuid(object parameters)
        {
            if (!(parameters is ReadMetaUuidParameters input)) return;

            byte[] fileData = MetadataFileReader.ReadBytes(input.MetadataObject.FileName.ToString());
            using (StreamReader stream = MetadataFileReader.CreateReader(fileData))
            {
                MetadataObjectFileParser.ParseMetaUuid(stream, input.MetadataObject);
            }
            input.InfoBase.MetaReferenceTypes.TryAdd(input.MetadataObject.Uuid, input.MetadataObject);
        }
        private void ReadMetadataObjects(InfoBase infoBase)
        {
            ReadValueTypes(infoBase);
            ReadReferenceTypes(infoBase);
        }
        private void ReadValueTypes(InfoBase infoBase)
        {
            foreach (var collection in infoBase.ValueTypes)
            {
                int i = 0;
                Task[] tasks = new Task[collection.Count];
                foreach (var item in collection)
                {
                    tasks[i] = Task.Factory.StartNew(
                        ReadMetadataObject,
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
        }
        private void ReadReferenceTypes(InfoBase infoBase)
        {
            foreach (var collection in infoBase.ReferenceTypes)
            {
                int i = 0;
                Task[] tasks = new Task[collection.Count];
                foreach (var item in collection)
                {
                    tasks[i] = Task.Factory.StartNew(
                        ReadMetadataObject,
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
        }
        private void ReadMetadataObject(object metaObject)
        {
            MetadataObject obj = (MetadataObject)metaObject;
            byte[] fileData = MetadataFileReader.ReadBytes(obj.FileName.ToString());
            if (fileData == null)
            {
                return; // TODO: log error "Metadata file is not found"
            }
            using (StreamReader stream = MetadataFileReader.CreateReader(fileData))
            {
                MetadataObjectFileParser.ParseMetadataObject(stream, obj);
            }
        }
    }
}