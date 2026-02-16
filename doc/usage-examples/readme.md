## DaJet Metadata - примеры использования



#### Чтение свойств основной конфигурации

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

// *********************
// * Консольный вывод: *
// *********************
// Имя конфигурации     : УправлениеНебольшойФирмой
// Синоним конфигурации : Управление нашей фирмой, редакция 3.0
// Версия платформы     : 80321
// Версия конфигурации  : 3.0.4.45
```