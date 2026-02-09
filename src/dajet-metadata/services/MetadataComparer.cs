using DaJet.TypeSystem;
using System.Text;

namespace DaJet.Metadata.Services
{
    internal sealed class MetadataComparer
    {
        private readonly MetadataLoader _loader;
        private readonly MetadataProvider _provider;
        internal MetadataComparer(in MetadataProvider provider, in MetadataLoader loader)
        {
            _loader = loader;
            _provider = provider;
        }
        internal string CompareMetadataToDatabase(List<string> names = null)
        {
            if (names is null || names.Count == 0)
            {
                names = new List<string>()
                {
                    MetadataNames.Constant,
                    MetadataNames.Publication,
                    MetadataNames.Catalog,
                    MetadataNames.Document,
                    MetadataNames.Characteristic,
                    MetadataNames.InformationRegister,
                    MetadataNames.AccumulationRegister,
                    MetadataNames.BusinessTask,
                    MetadataNames.BusinessProcess,
                    MetadataNames.Account,
                    MetadataNames.AccountingRegister
                };
            }

            StringBuilder logger = new();

            foreach (string name in names)
            {
                foreach (EntityDefinition entity in _provider.GetMetadataObjects(name))
                {
                    CompareMetadataObjectToDatabase(in entity, in logger);

                    foreach (EntityDefinition tablePart in entity.Entities)
                    {
                        CompareMetadataObjectToDatabase(in tablePart, in logger);
                    }

                    if (name == MetadataNames.Catalog)
                    {
                        string fullName = string.Format("{0}.{1}.{2}", MetadataNames.Catalog, entity.Name, "Изменения");

                        EntityDefinition changes = _provider.GetMetadataObject(fullName);

                        if (changes is not null)
                        {
                            CompareMetadataObjectToDatabase(in changes, in logger);
                        }
                    }
                }
            }

            return logger.ToString();
        }
        private void CompareMetadataObjectToDatabase(in EntityDefinition entity, in StringBuilder logger)
        {
            EntityDefinition table = _loader.GetDbTableSchema(entity.DbName);

            List<string> source = GetEntityColumnNames(in entity); // испытуемый на соответствие эталону
            List<string> target = GetDbTableColumnNames(in table); // эталон (как должно быть в базе данных)

            Compare(source, target, out List<string> delete_list, out List<string> insert_list);

            if (delete_list.Count == 0 && insert_list.Count == 0)
            {
                //logger.AppendLine($"SUCCESS [{entity.DbName}] {entity.Name}");
                return; // success - проверка прошла успешно
            }

            logger.AppendLine($"[{entity.DbName}] {entity.Name}");

            if (delete_list.Count > 0)
            {
                logger.AppendLine($"* delete (лишние поля)");

                foreach (string column in delete_list)
                {
                    PropertyDefinition property = entity.GetPropertyByColumnName(in column);

                    if (property is not null)
                    {
                        logger.AppendLine($"  - {column} [{property.Name}]");
                    }
                    else
                    {
                        logger.AppendLine($"  - {column}");
                    }
                }
            }

            if (insert_list.Count > 0)
            {
                logger.AppendLine($"* insert (отсутствующие поля)");

                foreach (string column in insert_list)
                {
                    PropertyDefinition property = entity.GetPropertyByColumnName(in column);

                    if (property is not null)
                    {
                        logger.AppendLine($"  - {column} [{property.Name}]");
                    }
                    else
                    {
                        logger.AppendLine($"  - {column}");
                    }
                }
            }
        }
        internal static List<string> GetEntityColumnNames(in EntityDefinition entity)
        {
            List<string> list = new();

            foreach (PropertyDefinition property in entity.Properties)
            {
                foreach (ColumnDefinition column in property.Columns)
                {
                    list.Add(column.Name);
                }
            }

            return list;
        }
        internal static List<string> GetDbTableColumnNames(in EntityDefinition table)
        {
            List<string> list = new();

            foreach (PropertyDefinition property in table.Properties)
            {
                list.Add(property.Name);
            }

            return list; // Имена колонок таблицы базы данных уже отсортированы по возрастанию
        }
        internal static void Compare(List<string> source_list, List<string> target_list, out List<string> delete_list, out List<string> insert_list)
        {
            delete_list = new List<string>(); // Эти элементы есть в source (испытуемый), но их нет в target (эталон)
            insert_list = new List<string>(); // Эти элементы есть в target (эталон), но их нет в source (испытуемый)

            int source_count = source_list.Count;
            int target_count = target_list.Count;
            int source_index = 0;
            int target_index = 0;
            int compareResult;

            if (source_count == 0 && target_count == 0) return;

            source_list.Sort(StringComparer.OrdinalIgnoreCase);
            target_list.Sort(StringComparer.OrdinalIgnoreCase);

            while (source_index < source_count) // source список "ведущий"
            {
                if (target_index < target_count) // в target списке есть ещё элементы
                {
                    compareResult = source_list[source_index].CompareTo(target_list[target_index], StringComparison.OrdinalIgnoreCase);

                    if (compareResult < 0) // source меньше target
                    {
                        // Элемент source больше не может встретиться в списке target.
                        delete_list.Add(source_list[source_index]);
                        // Добавлять элемент target в список insert мы пока не можем,
                        // так как элемент target возможно есть ниже в списке source.
                        source_index++; // Берём следующий элемент source
                    }
                    else if (compareResult == 0) // target равен source
                    {
                        source_index++; // Берём следующий элемент source
                        target_index++; // Берём следующий элемент target
                    }
                    else // source больше target
                    {
                        // Добавлять элемент source в список delete мы пока не можем,
                        // так как элемент source возможно есть ниже в списке target.
                        // Элемент target больше не может встретиться в списке source.
                        insert_list.Add(target_list[target_index]);
                        target_index++; // Берём следующий элемент target
                    }
                }
                else // достигли конца source списка
                {
                    delete_list.Add(source_list[source_index]);
                    source_index++; // Берём следующий элемент source
                }
            }

            while (target_index < target_count) // source список оказался короче target списка
            {
                // Добавляем все оставшиеся элементы target
                insert_list.Add(target_list[target_index]);
                target_index++; // Берём следующий элемент target
            }
        }
    }
}