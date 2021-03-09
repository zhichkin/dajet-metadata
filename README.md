# dajet-metadata

Library to read 1C:Enterprise 8 metadata directly from Microsoft SQL Server or PostgreSQL database.

Библиотека для чтения метаданных 1С:Предприятие 8 напрямую из базы данных СУБД.

Поддерживаются Microsoft SQL Server и PostgreSQL.

Кроме метаданных 1С дополнительно выполняется чтение метаданных СУБД.

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


**Утилита для чтения метаданных (свойств конфигурации)**

Требуется установка .NET Core 3.1

[Ссылка для скачивания](https://github.com/zhichkin/dajet-metadata/releases/)

Помощь по использованию:

![Помощь по использованию](https://github.com/zhichkin/dajet-metadata/blob/main/doc/dajet-help.png)

Пример использования для Microsoft SQL Server:

![Пример использования dajet cli для Microsoft SQL Server](https://github.com/zhichkin/dajet-metadata/blob/main/doc/dajet-usage-ms.png)

Пример использования для PostgreSQL:

![Пример использования dajet cli для PostgreSQL](https://github.com/zhichkin/dajet-metadata/blob/main/doc/dajet-usage-pg.png)

**Примечание:** в случае необходимости указать порт для **PostgreSQL** адрес сервера можно указать, например, так: **127.0.0.1:5432**