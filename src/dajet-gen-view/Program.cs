namespace DaJet.CodeGenerator
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            //args = new string[] { "--help" };

            //string ms = "Data Source=zhichkin;Initial Catalog=cerberus;Integrated Security=True";
            //string pg = "Host=127.0.0.1;Port=5432;Database=test_node_2;Username=postgres;Password=postgres;";
            //string rmq = "amqp://guest:guest@localhost:5672";
            //string json = "C:\\temp\\export-data.json";
            //args = new string[] { "export",
            //    "--ms", ms,
            //    "--rmq", rmq,
            //    "--data", "Справочник.Клиенты",
            //    "--page", "1-1",
            //    "--route", "dajet-queue"
            //};

            RootCommand command = new RootCommand()
            {
                Description = "DaJet Console 1.0"
            };

            Command export = new Command("export")
            {
                new Option<string>("--ms",   "SQL Server connection string"),
                new Option<string>("--pg",   "PostgreSQL connection string"),
                new Option<string>("--rmq",  "RabbitMQ URI (amqp://guest:guest@localhost:5672/{vhost}/{exchange})"),
                new Option<string>("--json", "JSON file full path"),
                new Option<string>("--data", "Full name of 1С metadata object (Справочник.Номенклатура)"),
                new Option<string>("--page", "Range of data pages to export (page size is 1000 rows)"),
                new Option<string>("--route", "Routing key to use with RabbitMQ")
            };
            export.Description = "Export data from 1C database to RabbitMQ exchange or JSON file"
                + Environment.NewLine + "Options:";
            foreach (Option option in export.Options)
            {
                export.Description += Environment.NewLine + "  --" + option.Name.PadRight(6) + option.Description;
            }
            export.Handler = CommandHandler.Create<string, string, string, string, string, string, FileInfo>(ExecuteExportCommand);

            command.Add(export);

            return command.Invoke(args);
        }
        private static ApplicationObject GetApplicationObjectByName(InfoBase infoBase, string metadataName)
        {
            string[] names = metadataName.Split('.');
            if (names.Length != 2)
            {
                return null;
            }

            string typeName = names[0];
            string objectName = names[1];

            ApplicationObject metaObject = null;
            Dictionary<Guid, ApplicationObject> collection = null;

            if (typeName == "Справочник") collection = infoBase.Catalogs;
            else if (typeName == "Документ") collection = infoBase.Documents;
            else if (typeName == "ПланОбмена") collection = infoBase.Publications;
            else if (typeName == "РегистрСведений") collection = infoBase.InformationRegisters;
            else if (typeName == "РегистрНакопления") collection = infoBase.AccumulationRegisters;
            if (collection == null)
            {
                return null;
            }

            metaObject = collection.Values.Where(o => o.Name == objectName).FirstOrDefault();
            if (metaObject == null)
            {
                return null;
            }

            return metaObject;
        }
        private static void ExecuteExportCommand(string ms, string pg, string rmq, string data, string page, string route, FileInfo json)
        {
            //Console.WriteLine("Export command parameters:");
            //Console.WriteLine("- ms = " + (string.IsNullOrWhiteSpace(ms) ? string.Empty : ms));
            //Console.WriteLine("- pg = " + (string.IsNullOrWhiteSpace(pg) ? string.Empty : pg));
            //Console.WriteLine("- rmq = " + (string.IsNullOrWhiteSpace(rmq) ? string.Empty : rmq));
            //Console.WriteLine("- json = " + (json == null ? string.Empty : json.FullName));
            //Console.WriteLine("- data = " + (string.IsNullOrWhiteSpace(data) ? string.Empty : data));
            //Console.WriteLine("- page = " + (string.IsNullOrWhiteSpace(page) ? string.Empty : page));
            //Console.WriteLine("- route = " + (string.IsNullOrWhiteSpace(route) ? string.Empty : route));
            //Console.WriteLine();

            IMetadataService metadata = new MetadataService()
                .UseConnectionString(ms)
                .UseDatabaseProvider(DatabaseProvider.SQLServer);

            InfoBase infoBase = metadata.OpenInfoBase();

            ApplicationObject metaObject = GetApplicationObjectByName(infoBase, data);
            if (metaObject == null)
            {
                Console.WriteLine($"Object \"{data}\" is not found.");
                return;
            }

            ParsePageParameter(page, out int firstPage, out int lastPage);

            int pageNumber = firstPage;
            if (lastPage == 0)
            {
                lastPage = pageNumber;
            }

            if (data == "Справочник.Клиенты")
            {
                ExportCatalog(infoBase, metaObject, metadata, pageNumber, lastPage, rmq, route);
            }
            else if (data == "РегистрСведений.ОтветственныеЛицаАптек")
            {
                ExportRegister(infoBase, metaObject, metadata, pageNumber, lastPage, rmq, route);
            }
            else
            {
                Console.WriteLine($"Unknown object: \"{data}\".");
            }

            //foreach (int i in GetNumbers())
            //{
            //    Console.SetCursorPosition(0, Console.CursorTop);
            //    Console.Write("i = " + i.ToString());
            //}
        }
        //public static IEnumerable<int> GetNumbers()
        //{
        //    for (int i = 0; i < 10; i++)
        //    {
        //        yield return i;
        //    }
        //}
        private static void ParsePageParameter(string page, out int firstPage, out int lastPage)
        {
            if (page.Contains('-'))
            {
                string[] pages = page.Split('-', StringSplitOptions.RemoveEmptyEntries);
                lastPage = int.Parse(pages[1]);
                firstPage = int.Parse(pages[0]);
            }
            else
            {
                lastPage = 0;
                firstPage = int.Parse(page);
            }
        }
        private static void ExportCatalog(InfoBase infoBase, ApplicationObject metaObject, IMetadataService metadata, int pageNumber, int lastPage, string rmq, string route)
        {
            string messageType = "Справочник.Клиенты";

            CatalogJsonSerializer serializer = new CatalogJsonSerializer();
            CatalogDataMapper mapper = new CatalogDataMapper(infoBase, metaObject);

            int count = mapper.GetTotalRowCount(metadata.ConnectionString);
            Console.WriteLine($"Total rows count = {count}");

            count = 0;
            for (; pageNumber <= lastPage; pageNumber++)
            {
                List<Клиенты> list = mapper.SelectEntities(metadata.ConnectionString, pageNumber);

                foreach (Клиенты entity in list)
                {
                    if (entity.КонтактнаяИнформация == null)
                    {
                        entity.КонтактнаяИнформация = mapper.SelectTablePart(metadata.ConnectionString, entity);
                    }
                }

                using (RabbitMQProducer producer = new RabbitMQProducer())
                {
                    producer.MessageType = messageType;
                    producer.Publish(rmq, route, list, serializer);

                    Console.WriteLine($"Page [{pageNumber}] = {list.Count}");
                }

                count += list.Count;
            }

            Console.WriteLine($"A total of {count} messages has been sent");
        }
        private static void ExportRegister(InfoBase infoBase, ApplicationObject metaObject, IMetadataService metadata, int pageNumber, int lastPage, string rmq, string route)
        {
            string messageType = "РегистрСведений.ОтветственныеЛицаАптек";

            ClusteredIndexInfo indexInfo = SQLHelper.GetClusteredIndexInfo(metadata.ConnectionString, metaObject.TableName);

            RegisterJsonSerializer serializer = new RegisterJsonSerializer();
            RegisterDataMapper mapper = new RegisterDataMapper(infoBase, metaObject, indexInfo);

            int count = mapper.GetTotalRowCount(metadata.ConnectionString);
            Console.WriteLine($"Total rows count = {count}");

            count = 0;
            for (; pageNumber <= lastPage; pageNumber++)
            {
                List<ОтветственныеЛицаАптек> list = mapper.SelectRecords(metadata.ConnectionString, pageNumber);

                using (RabbitMQProducer producer = new RabbitMQProducer())
                {
                    producer.MessageType = messageType;
                    producer.Publish(rmq, route, list, serializer);

                    Console.WriteLine($"Page [{pageNumber}] = {list.Count}");
                }

                count += list.Count;
            }

            Console.WriteLine($"A total of {count} messages has been sent");
        }
    }
}