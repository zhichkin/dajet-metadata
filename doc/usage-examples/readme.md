## DaJet Metadata - примеры использования



### Чтение свойств основной конфигурации

```csharp
using DaJet.Data;
using DaJet.Metadata;
using DaJet.TypeSystem;

namespace DaJet.Metadata.Examples
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string MS_UNF = "Data Source=server;Initial Catalog=unf;Integrated Security=True;Encrypt=False;";

            MetadataProvider provider = MetadataProvider.Create(DataSourceType.SqlServer, in MS_UNF);

            Configuration configuration = provider.GetConfiguration();

            Console.WriteLine($"Имя конфигурации     : {configuration.Name}");
            Console.WriteLine($"Синоним конфигурации : {configuration.Alias}");
            Console.WriteLine($"Версия платформы     : {configuration.CompatibilityVersion}");
            Console.WriteLine($"Версия Конфигурации  : {configuration.AppConfigVersion}");
        }
    }
}

> Имя конфигурации     : УправлениеНебольшойФирмой
> Синоним конфигурации : Управление нашей фирмой, редакция 3.0
> Версия платформы     : 80321
> Версия конфигурации  : 3.0.4.45
```

### Чтение свойств основной конфигурации и расширений

```csharp
using DaJet.Data;
using DaJet.Metadata;
using DaJet.TypeSystem;

namespace DaJet.Metadata.Examples
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string MS_TEST = "Data Source=server;Initial Catalog=dajet-metadata;Integrated Security=True;Encrypt=False;";

            MetadataProvider provider = MetadataProvider.Create(DataSourceType.SqlServer, in MS_TEST);

            foreach (Configuration configuration in provider.GetConfigurations())
            {
                Console.WriteLine($"Имя конфигурации     : {configuration.Name}");
                Console.WriteLine($"Синоним конфигурации : {configuration.Alias}");
                Console.WriteLine($"Версия платформы     : {configuration.CompatibilityVersion}");
                Console.WriteLine($"Версия конфигурации  : {configuration.AppConfigVersion}");
                Console.WriteLine();
            }
        }
    }
}

> Имя конфигурации     : DaJet_Metadata
> Синоним конфигурации : DaJet Metadata
> Версия платформы     : 80327
> Версия конфигурации  : 1.0.0
>
> Имя конфигурации     : Расширение1
> Синоним конфигурации : Расширение1
> Версия платформы     : 80327
> Версия конфигурации  : 1.0.0
```

### Чтение специальных свойств расширений

```csharp
using DaJet.Data;
using DaJet.Metadata;
using DaJet.TypeSystem;

namespace DaJet.Metadata.Examples
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string MS_TEST = "Data Source=server;Initial Catalog=dajet-metadata;Integrated Security=True;Encrypt=False;";

            MetadataProvider provider = MetadataProvider.Create(DataSourceType.SqlServer, in MS_TEST);

            foreach (ExtensionInfo extension in provider.GetExtensions())
            {
                Console.WriteLine($"Имя расширения : {extension.Name}");
                Console.WriteLine($"Активно        : {extension.IsActive}");
                Console.WriteLine($"Версия         : {extension.Version}");
                Console.WriteLine($"Назначение     : {extension.Purpose}");
                Console.WriteLine();
            }
        }
    }
}

> Имя расширения : Расширение1
> Активно        : True
> Версия         : 1.0.0
> Назначение     : Customization
```

### Получение списка справочников основной конфигурации или расширения

```csharp
using DaJet.Data;
using DaJet.Metadata;
using DaJet.TypeSystem;

namespace DaJet.Metadata.Examples
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string MS_TEST = "Data Source=server;Initial Catalog=dajet-metadata;Integrated Security=True;Encrypt=False;";

            MetadataProvider provider = MetadataProvider.Create(DataSourceType.SqlServer, in MS_TEST);

            List<string> names = provider.GetMetadataNames("DaJet_Metadata", MetadataNames.Catalog);

            foreach (string name in names)
            {
                Console.WriteLine(name);
            }

            Console.WriteLine();

            names = provider.GetMetadataNames("Расширение1", MetadataNames.Catalog);

            foreach (string name in names)
            {
                Console.WriteLine(name);
            }
        }
    }
}

> Справочник1
> Справочник2
> Заимствованный

> Заимствованный
> Расш1_Справочник1
> Расш1_Справочник2
```

### Просмотр справочника и его свойств

```csharp
using DaJet.Data;
using DaJet.Metadata;
using DaJet.TypeSystem;

namespace DaJet.Metadata.Examples
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string MS_TEST = "Data Source=server;Initial Catalog=dajet-metadata;Integrated Security=True;Encrypt=False;";

            MetadataProvider provider = MetadataProvider.Create(DataSourceType.SqlServer, in MS_TEST);

            EntityDefinition entity = provider.GetMetadataObject("Справочник.Справочник1");

            Console.WriteLine($"{entity.Name} [{entity.DbName}]");

            foreach (PropertyDefinition property in entity.Properties)
            {
                Console.WriteLine($"{property.Name}");

                foreach (ColumnDefinition column in property.Columns)
                {
                    Console.WriteLine($" {column.Name} {column.Type}");
                }
            }
        }
    }
}

> Справочник1 [_Reference702]
> Ссылка
>  _IDRRef binary(16)
> ВерсияДанных
>  _Version binary(8)
> ПометкаУдаления
>  _Marked binary(1)
> Код
>  _Code string(9)
> Наименование
>  _Description string(25)
> Предопределенный
>  _PredefinedID binary(16)
```
