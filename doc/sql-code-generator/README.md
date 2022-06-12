## DaJet SQL views generator

Требуется установка [.NET 6.0](https://dotnet.microsoft.com/download/).

Утилита **dajet-gen-view** создаёт представления СУБД (view)
для объектов конфигруации 1С:Предприятие 8.

На данный момент времени поддерживается только Microsoft SQL Server.

Представления создаются в той же самой базе данных,
в которой расположены сами объекты.

Это позволяет обращаться к данным объектов непосредственно
при помощи языка запросов СУБД по именам этих объектов,
которые заданы в конфигураторе 1С:Предприятие 8.

```SQL
SELECT Порядок, Имя, Синоним, Значение FROM [Перечисление.СтавкиНДС];
```

Результат запроса:

| Порядок | Имя       | Синоним | Значение                           |
|---------|-----------|---------|------------------------------------|
| 0       | НДС18     | 18%     | 0xAAF7E893CEE0CD1F48A876B826B5EF6B |
| 1       | НДС18_118 | 18/118  | 0xA25A110797C154784B9D6E30ACA7B2A3 |
| 2       | НДС10     | 10%     | 0xA292D1D0AEE8F07245D062C1B99522A7 |
| 3       | НДС10_110 | 10/110  | 0xBAA362A36797835044C643D2AD5C7ACE |
| 4       | НДС0      | 0%      | 0xAD96B43FD894A64146F4C1B29A7EEB40 |
| 5       | БезНДС    | Без НДС | 0xAF78C65D1C17AD414E8846212489ABF1 |

### Поддерживаемые типы объектов:
- Документы
- Справочники
- Перечисления
- Планы обмена
- Регистры сведений
- Регистры накопления
- Планы видов характеристик
- Табличные части

### Создание представлений СУБД

**dajet-gen-view.exe** create --ms "Data Source=one_c_sql_server;Initial Catalog=one_c_database;Integrated Security=True;Encrypt=False;"

### Удаление представлений СУБД

**dajet-gen-view.exe** delete --ms "Data Source=one_c_sql_server;Initial Catalog=one_c_database;Integrated Security=True;Encrypt=False;"

[NuGet package](https://www.nuget.org/packages/DaJet.Metadata/) & [Telegram support channel](https://t.me/dajet_studio_group)

[Конфигурация 1С для выполнения тестов](https://github.com/zhichkin/dajet-metadata/blob/main/1c/dajet-metadata-test-base.cf)

Кроме метаданных 1С дополнительно можно выполнять чтение метаданных СУБД.

<details>
<summary>Пример загрузки метаданных и свойств конфигурации</summary>

```C#
using DaJet.Metadata;
using DaJet.Metadata.Model;

static void Main(string[] args)
{
    string csPostgres = "Host=127.0.0.1;Port=5432;Database=MY_1C_DATABASE;Username=postgres;Password=postgres;";
    string csSqlServer = "Data Source=MY_DATABASE_SERVER;Initial Catalog=MY_1C_DATABASE;Integrated Security=True";

    IMetadataService metadataService = new MetadataService();

    #region "PostgreSQL"

    // Настройки для подключения к PostgreSQL
    metadataService.
        .UseConnectionString(csPostgres)
        .UseDatabaseProvider(DatabaseProviders.PostgreSQL);

    // 1. Пример чтения метаданных конфигурации 1С
    InfoBase infoBase = metadataService.LoadInfoBase();
    
    // 2. Пример чтения свойств конфигурации 1С
    ConfigInfo config = metadataService.ReadConfigurationProperties();

    // 3. Пример дополнения справочника "Номенклатура" свойствами из базы данных
    MetadataObject catalog = infoBase.Catalogs.Values
                                 .Where(i => i.Name == "Номенклатура")
                                 .FirstOrDefault();

    if (catalog == null)
    {
        Console.WriteLine("Справочник \"Номенклатура\" не найден!");
        return;
    }

    Console.WriteLine("Свойства справочника до обогащения из базы данных.");
    Console.WriteLine(catalog.Name + " (" + catalog.TableName + "):");
    foreach (MetadataProperty property in catalog.Properties)
    {
        Console.WriteLine(" - " + property.Name + " (" + property.DbName + ")");
    }

    // Выполняем сравнение и объединение с метаданными СУБД
    metadataService.EnrichFromDatabase(catalog);

    Console.WriteLine("Свойства справочника после обогащения из базы данных.");
    Console.WriteLine(catalog.Name + " (" + catalog.TableName + "):");
    foreach (MetadataProperty property in catalog.Properties)
    {
        Console.WriteLine(" - " + property.Name + " (" + property.DbName + ")");
    }

    #endregion

    #region "Microsoft SQL Server"

    // Настройки для подключения к Microsoft SQL Server
    metadataService.
        .UseConnectionString(csSqlServer)
        .UseDatabaseProvider(DatabaseProviders.SQLServer);

    // Всё остальное работает ровно также, как и для PostgreSQL

    #endregion
}
```

</details>

<details>
<summary>Пример загрузки свойств и узлов плана обмена</summary>

```C#
using DaJet.Metadata;
using DaJet.Metadata.Model;
using DaJet.Metadata.Mappers;

static void Main(string[] args)
{
    string connectionString = "Data Source=MY_DATABASE_SERVER;Initial Catalog=MY_1C_DATABASE;Integrated Security=True";

    IMetadataService metadataService = new MetadataService();

    metadataService.
        .UseConnectionString(connectionString)
        .UseDatabaseProvider(DatabaseProviders.SQLServer);

    InfoBase infoBase = metadataService.LoadInfoBase();

    // Находим план обмена (публикацию) по его имени, как оно указано в конфигурации 1С
    Publication publication = infoBase.Publications.Values
                                  .Where(i => i.Name == "ТестовыйПланОбмена")
                                  .FirstOrDefault();

    if (publication == null)
    {
        Console.WriteLine("План обмена \"ТестовыйПланОбмена\" не найден!");
        return;
    }

    // Создаём экземпляр класса для загрузки данных плана обмена
    PublicationDataMapper mapper = new PublicationDataMapper();
    mapper.UseDatabaseProvider(metadataService.DatabaseProvider);
    mapper.UseConnectionString(metadataService.ConnectionString);

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

<details>
<summary>Пример сравнения метаданных СУБД для объекта метаданных 1С</summary>

```C#
using DaJet.Metadata;
using DaJet.Metadata.Model;

static void Main(string[] args)
{
    string connectionString = "Data Source=MY_DATABASE_SERVER;Initial Catalog=MY_1C_DATABASE;Integrated Security=True";

    IMetadataService metadataService = new MetadataService();

    metadataService.
        .UseConnectionString(connectionString)
        .UseDatabaseProvider(DatabaseProviders.SQLServer);

    InfoBase infoBase = metadataService.LoadInfoBase();

    // Находим объект метаданных 1С для загрузки его метаданных СУБД
    Publication publication = infoBase.Publications.Values
                                  .Where(i => i.Name == "ТестовыйПланОбмена")
                                  .FirstOrDefault();

    if (publication == null)
    {
        Console.WriteLine("План обмена \"ТестовыйПланОбмена\" не найден!");
        return;
    }

    List<string> delete_list; // Список "лишних" - есть в объекте метаданных 1С, но не найдены в базе данных
    List<string> insert_list; // Список "новых" - есть в базе данных, но нет в объекте метаданных 1С
    bool result = metadataService.CompareWithDatabase(publication, out delete_list, out insert_list);
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
