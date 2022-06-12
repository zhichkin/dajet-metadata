## DaJet SQL views generator

Требуется установка [.NET 6.0](https://dotnet.microsoft.com/download/)

[NuGet](https://www.nuget.org/packages/DaJet.CodeGenerator) & [Telegram](https://t.me/dajet_studio_group)

Утилита **dajet-gen-view** создаёт представления СУБД (view)
для объектов конфигруации 1С:Предприятие 8.

На данный момент времени поддерживается только Microsoft SQL Server.

Представления создаются в той же самой базе данных,
в которой расположены объекты 1С:Предприятие 8.
При совпадении имён представления пересоздаются.

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

**dajet-gen-view.exe** **create** --ms "Data Source=one_c_sql_server;Initial Catalog=one_c_database;Integrated Security=True;Encrypt=False;"

### Удаление представлений СУБД

**dajet-gen-view.exe** **delete** --ms "Data Source=one_c_sql_server;Initial Catalog=one_c_database;Integrated Security=True;Encrypt=False;"

### Программное создание представлений СУБД

```C#
using DaJet.CodeGenerator.SqlServer;
using DaJet.Metadata;
using DaJet.Metadata.Model;

static void Main(string[] args)
{
    IMetadataService metadataService = new MetadataService();

    if (!metadataService
        .UseDatabaseProvider(DatabaseProvider.SQLServer)
        .UseConnectionString("Data Source=SQL_SERVER;Initial Catalog=MY_DATABASE;Integrated Security=True;Encrypt=False;")
        .TryOpenInfoBase(out InfoBase infoBase, out string message))
    {
        Console.WriteLine("Error: " + message);
        return;
    }

    SqlGeneratorOptions options = new SqlGeneratorOptions()
    {
        DatabaseProvider = "SqlServer",
        ConnectionString = metadataService.ConnectionString
    };

    ISqlGenerator generator = new SqlGenerator(options);

    if (!generator.TryCreateViews(in infoBase, out int result, out List<string> errors))
    {
        foreach (string error in errors)
        {
            Console.WriteLine(error);
        }
    }

    Console.WriteLine($"Created {result} views");
}
```

### Программное удаление представлений СУБД

```C#
using DaJet.CodeGenerator.SqlServer;
using DaJet.Metadata;
using DaJet.Metadata.Model;

static void Main(string[] args)
{
    SqlGeneratorOptions options = new SqlGeneratorOptions()
    {
        DatabaseProvider = "SqlServer",
        ConnectionString = "Data Source=SQL_SERVER;Initial Catalog=MY_DATABASE;Integrated Security=True;Encrypt=False;"
    };

    ISqlGenerator generator = new SqlGenerator(options);

    int result = generator.DropViews();

    Console.WriteLine($"Dropped {result} views");
}
```
