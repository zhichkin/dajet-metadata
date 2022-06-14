## DaJet SQL views generator

Требуется установка [.NET 6.0](https://dotnet.microsoft.com/download/)

[NuGet](https://www.nuget.org/packages/DaJet.CodeGenerator) & [Telegram](https://t.me/dajet_studio_group) & [Исходный код](https://github.com/zhichkin/dajet-metadata/tree/main/src/dajet-code-generator)

[Скачать дистрибутив](https://github.com/zhichkin/dajet-metadata/releases/tag/gen-view-1.1.0)

Утилита **dajet-gen-view** создаёт представления СУБД (view) для объектов конфигруации 1С:Предприятие 8.

Поддерживаются Microsoft SQL Server и PostgreSQL.

Поддержка PostgreSQL включена, начиная с [версии 1.2.0](https://github.com/zhichkin/dajet-metadata/releases/tag/gen-view-1.2.0).

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

### Описание команд утилиты и их опций

- **create** - создаёт представления СУБД в базе данных
  - **--ms** - строка подключения к базе данных SQL Server
  - **--pg** - строка подключения к базе данных PostgreSQL (в разработке)
  - **--schema** - имя существующей схемы базы данных (в случае отсутствия будет создана)
- **delete** - удаляет представления СУБД из базы данных
  - **--ms** - строка подключения к базе данных SQL Server
  - **--pg** - строка подключения к базе данных PostgreSQL (в разработке)
  - **--schema** - имя существующей схемы базы данных
- **script** - создаёт скрипт SQL для создания представлений и сохраняет его в указанный файл
  - **--ms** - строка подключения к базе данных SQL Server
  - **--pg** - строка подключения к базе данных PostgreSQL (в разработке)
  - **--schema** - имя схемы базы данных для использования
  - **--out-file** - полный путь к файлу для сохранения SQL скрипта

Опция **--schema** не обязательна для указания. В случае её отсутствия используется схема базы данных по умолчанию. Для SQL Server это схема **dbo**.

### Создание представлений СУБД

**dajet-gen-view** **create** --ms "Data Source=SERVER;Initial Catalog=DATABASE;Integrated Security=True;Encrypt=False;"

### Удаление представлений СУБД

**dajet-gen-view** **delete** --ms "Data Source=SERVER;Initial Catalog=DATABASE;Integrated Security=True;Encrypt=False;"

### Сохранение скрипта создания представлений СУБД в файл

**dajet-gen-view** **script** --ms "Data Source=SERVER;Initial Catalog=DATABASE;Integrated Security=True;Encrypt=False;" --out-file "C:\script.sql"

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
        Schema = "test",
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
        Schema = "test",
        DatabaseProvider = "SqlServer",
        ConnectionString = "Data Source=SQL_SERVER;Initial Catalog=MY_DATABASE;Integrated Security=True;Encrypt=False;"
    };

    ISqlGenerator generator = new SqlGenerator(options);

    int result = generator.DropViews();

    Console.WriteLine($"Dropped {result} views");
}
```

### Программное создание файла скрипта для создания представлений СУБД

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
        Schema = "test",
        OutputFile = "C:\\script.sql",
        DatabaseProvider = "SqlServer",
        ConnectionString = metadataService.ConnectionString
    };

    ISqlGenerator generator = new SqlGenerator(options);

    if (!generator.TryScriptViews(in infoBase, out int result, out List<string> errors))
    {
        foreach (string error in errors)
        {
            Console.WriteLine(error);
        }
    }

    Console.WriteLine($"Scripted {result} views");
}
```

### Нюансы использования версии для PostgreSQL

Длина наименований объектов СУБД в PostgreSQL по умолчанию ограничена 63 байтами.

Большая длина может быть установлена только пересборкой СУБД из исходников.

| The system uses no more than NAMEDATALEN-1 bytes of an identifier; longer names can be written in commands, but they will be truncated. By default, NAMEDATALEN is 64 so the maximum identifier length is 63 bytes. If this limit is problematic, it can be raised by changing the NAMEDATALEN constant in src/include/pg_config_manual.h.