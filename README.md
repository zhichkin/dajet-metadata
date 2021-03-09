# dajet-metadata

Library to read 1C:Enterprise 8 metadata directly from Microsoft SQL Server or PostgreSQL database.

Библиотека для чтения метаданных 1С:Предприятие 8 напрямую из базы данных СУБД.

Поддерживаются Microsoft SQL Server и PostgreSQL.

Кроме метаданных 1С дополнительно выполняется чтение метаданных СУБД.

Использование:
```C#
using DaJet.Metadata;
using DaJet.Metadata.Model;

static void Main(string[] args)
{
    IMetadataProvider metadata = new MetadataProvider();
    metadata.UseConnectionString("Data Source=MY_DATABASE_SERVER;Initial Catalog=MY_1C_DATABASE;Integrated Security=True");

    // 1. Прочитать всю конфигурацию информационной базы 1С
    InfoBase infoBase = metadata.LoadInfoBase();

    // 2. Прочитать метаданные одного объекта по его типу и имени
    MetaObject metaObject = metadata.LoadMetaObject("Catalog", "Номенклатура");
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