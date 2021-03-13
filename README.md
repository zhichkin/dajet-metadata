# dajet-metadata

Library to read 1C:Enterprise 8 metadata from Microsoft SQL Server or PostgreSQL database.

Библиотека для чтения метаданных 1С:Предприятие 8 напрямую из базы данных СУБД.

Поддерживаются Microsoft SQL Server и PostgreSQL.

Требуется установка [.NET Core 3.1](https://dotnet.microsoft.com/download/).

[NuGet package](https://www.nuget.org/packages/DaJet.Metadata/)

Кроме метаданных 1С дополнительно выполняется чтение метаданных СУБД.

<details>
<summary>Пример загрузки свойств и узлов плана обмена</summary>

```C#
using DaJet.Metadata;
using DaJet.Metadata.Model;
using DaJet.Metadata.Mappers;

static void Main(string[] args)
{
    // Для информационной базы на Microsoft SQL Server
    IMetadataFileReader fileReader = new MetadataFileReader();
    fileReader.UseConnectionString("Data Source=MY_DATABASE_SERVER;Initial Catalog=MY_1C_DATABASE;Integrated Security=True");

    // Для информационной базы на PostgreSQL
    // IMetadataFileReader fileReader = new PostgresMetadataFileReader();
    // fileReader.UseConnectionString("Host=127.0.0.1;Port=5432;Database=test_node_2;Username=postgres;Password=postgres;");

    // Загружаем все метаданные конфигурации 1С
    IMetadataReader metadata = new MetadataReader(fileReader);
    InfoBase infoBase = metadata.LoadInfoBase();

    // Находим план обмена (публикацию) по его имени, как оно указано в конфигурации 1С
    Publication publication = infoBase.Publications.Values
        .Where(i => i.Name == "ТестовыйПланОбмена").FirstOrDefault();

    // Создаём экземпляр класса для загрузки данных плана обмена
    PublicationDataMapper mapper = new PublicationDataMapper();
    mapper.UseConnectionString(fileReader.ConnectionString);
    mapper.UseDatabaseProvider(DatabaseProviders.SQLServer);

    // Загружаем узлы плана обмена (подписчиков)
    mapper.SelectSubscribers(publication);

    // Выводим информацию об "этом узле" (издателе)
    // Код 1С: ПланыОбмена.ТестовыйПланОбмена.ЭтотУзел()
    Console.WriteLine(string.Format("Publisher: ({0}) {1}",
        publication.Publisher.Code,
        publication.Publisher.Name));

    // Выводим информацию об узлах плана обмена (подписчиках)
    Console.WriteLine("Subscribers:");
    foreach (Subscriber subscriber in publication.Subscribers)
    {
        Console.WriteLine(string.Format(" - ({0}) {1} [{2}]",
            subscriber.Code,
            subscriber.Name,
            subscriber.IsMarkedForDeletion ? "x" : "+"));
    }
}
```

</details>

**Пример чтения метаданных:**
```C#
using DaJet.Metadata;
using DaJet.Metadata.Model;

static void Main(string[] args)
{
    // Для информационной базы на Microsoft SQL Server
    IMetadataFileReader fileReader = new MetadataFileReader();
    fileReader.UseConnectionString("Data Source=MY_DATABASE_SERVER;Initial Catalog=MY_1C_DATABASE;Integrated Security=True");

    // Для информационной базы на PostgreSQL
    // IMetadataFileReader fileReader = new PostgresMetadataFileReader();
    // fileReader.UseConnectionString("Host=127.0.0.1;Port=5432;Database=test_node_2;Username=postgres;Password=postgres;");

    // 1. Пример чтения метаданных конфигурации 1С (для всех СУБД)
    IMetadataReader metadata = new MetadataReader(fileReader);
    InfoBase infoBase = metadata.LoadInfoBase();

    // 2. Пример чтения свойств конфигурации 1С (для всех СУБД)
    IConfigurationFileParser configReader = new ConfigurationFileParser(fileReader);
    ConfigInfo config = configReader.ReadConfigurationProperties();
}
```

<details>
<summary>Пример загрузки метаданных СУБД для объекта метаданных 1С</summary>

```C#
using DaJet.Metadata;
using DaJet.Metadata.Model;

static void Main(string[] args)
{
    // Для информационной базы на Microsoft SQL Server
    IMetadataFileReader fileReader = new MetadataFileReader();
    fileReader.UseConnectionString("Data Source=MY_DATABASE_SERVER;Initial Catalog=MY_1C_DATABASE;Integrated Security=True");

    // Для информационной базы на PostgreSQL
    // IMetadataFileReader fileReader = new PostgresMetadataFileReader();
    // fileReader.UseConnectionString("Host=127.0.0.1;Port=5432;Database=test_node_2;Username=postgres;Password=postgres;");

    // Загружаем все метаданные конфигурации 1С
    IMetadataReader metadata = new MetadataReader(fileReader);
    InfoBase infoBase = metadata.LoadInfoBase();

    // Находим объект метаданных 1С для загрузки его метаданных СУБД
    Publication publication = infoBase.Publications.Values
        .Where(i => i.Name == "ТестовыйПланОбмена").FirstOrDefault();

    // Получаем метаданные СУБД для полей таблицы объекта метаданных 1С
    List<SqlFieldInfo> sqlFields = sqlReader.GetSqlFieldsOrderedByName(Publication.TableName);
    if (sqlFields.Count == 0)
    {
        Console.WriteLine("SQL fields are not found.");
        return;
    }

    // Дополняем свойства объекта метаданных 1С по метаданным СУБД
    MetadataCompareAndMergeService merger = new MetadataCompareAndMergeService();
    merger.MergeProperties(Publication, sqlFields);

    // Выводим результат
    Console.WriteLine(metaObject.Name + " (" + metaObject.TableName + "):");
    foreach (MetaProperty property in metaObject.Properties)
    {
        Console.WriteLine(" - " + property.Name + " (" + property.Field + ")");
    }
}
```

</details>

**Утилита для чтения метаданных (свойств конфигурации)**

[Ссылка для скачивания](https://github.com/zhichkin/dajet-metadata/releases/)

Помощь по использованию:

![Помощь по использованию](https://github.com/zhichkin/dajet-metadata/blob/main/doc/dajet-help.png)

Пример использования для Microsoft SQL Server:

![Пример использования dajet cli для Microsoft SQL Server](https://github.com/zhichkin/dajet-metadata/blob/main/doc/dajet-usage-ms.png)

Пример использования для PostgreSQL:

![Пример использования dajet cli для PostgreSQL](https://github.com/zhichkin/dajet-metadata/blob/main/doc/dajet-usage-pg.png)

**Примечание:** в случае необходимости указать порт для **PostgreSQL** адрес сервера можно указать, например, так: **127.0.0.1:5432**