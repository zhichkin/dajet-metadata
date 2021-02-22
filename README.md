# dajet-metadata

Library to read 1C:Enterprise 8 metadata directly from SQL Server database.

Библиотека для чтения метаданных 1С:Предприятие 8 напрямую из базы данных SQL Server.

Кроме метаданных 1С дополнительно выполняется чтение метаданных SQL Server.

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

![Ссылка для скачивания](https://github.com/zhichkin/dajet-metadata/releases/tag/v0.1)

Помощь по использованию:

![Помощь по использованию](https://github.com/zhichkin/dajet-metadata/blob/main/doc/dajet-usage.png)

Пример использования:

![Пример использования dajet cli](https://github.com/zhichkin/dajet-metadata/blob/main/doc/dajet-usage.png)
