namespace DaJet
{
    internal sealed class Property : DatabaseObject
    {
        internal static Property Create(Guid uuid, int code, string name)
        {
            return new Property(uuid, code, name);
        }
        internal Property(Guid uuid, int code, string name) : base(uuid, code, name) { }

        internal static PropertyDefinition[] Parse(ref ConfigFileReader reader, ReadOnlySpan<uint> root, in MetadataRegistry registry)
        {
            Guid type = reader[root][1].SeekUuid(); // идентификатор типа коллекции
            int count = reader[root][2].SeekNumber(); // количество элементов коллекции

            if (count == 0) { return null; }

            PropertyDefinition[] array = new PropertyDefinition[count];

            int N = root.Length;
            uint[] vector = new uint[N + 1]; // Адрес конкретного свойства [root][N]

            root.CopyTo(vector);

            for (uint i = 0; i < array.Length; i++)
            {
                vector[N] = i + 3; // Смещение от корневого узла [root][0]

                if (reader[vector][ConfigFileToken.StartObject].Seek())
                {
                    array[i] = Parse(ref reader, vector, type, in registry);
                }
            }

            return array;
        }
        private static PropertyDefinition Parse(ref ConfigFileReader reader, ReadOnlySpan<uint> root, Guid type, in MetadataRegistry registry)
        {
            // Свойство объекта:
            // -----------------
            // [root][offset] 0.1.1.1.8 = 0 если заимствование отстутствует
            // [root][offset] 0.1.1.1.11 - uuid расширяемого объекта метаданных
            // [root][offset] 0.1.1.1.15 - Объект описания дополнительных типов данных свойства
            // [root][offset] 0.1.1.1.15.0 = #
            // [root][offset] 0.1.1.1.15.1 = f5c65050-3bbb-11d5-b988-0050bae0a95d (константа)
            // [root][offset] 0.1.1.1.15.2 = {объект описания типов данных - Pattern} [0][1][1][2] += PropertyType

            PropertyDefinition property = new();

            //if (_cache != null && _cache.Extension != null) // 0.1.1.1.8 = 0 если заимствование отстутствует
            //{
            //    _converter[0][1][1][1][11] += Parent; // uuid расширяемого объекта метаданных
            //    _converter[0][1][1][1][15][2] += ExtensionPropertyType;
            //}

            property.Purpose = PropertyTypes.GetPropertyPurpose(type);

            Guid uuid = reader[root][1][2][2][2][2][3].SeekUuid();

            if (registry.TryGetEntry(uuid, out DatabaseObject entry))
            {
                property.Columns.Add(new ColumnDefinition()
                {
                    Name = entry.GetMainDbName()
                });
            }
            else
            {
                //TODO: Зафиксировать ошибку.
                //TODO: Свойство не найдено в реестре объектов метаданных, а это значит,
                //TODO: что у него нет поля/полей в соответствующей таблице базы данных
            }

            property.Name = reader[root][1][2][2][2][3].SeekString();

            //_converter[0][1][1][1][3][2] += PropertyAlias;

            if (reader[root][1][2][2][3][ConfigFileToken.StartObject].Seek())
            {
                property.Type = DataType.Parse(ref reader, root, out List<Guid> references);
            }

            if (type == PropertyTypes.Catalog_Properties ||
                type == PropertyTypes.Characteristic_Properties)
            {
                property.PropertyUsage = (PropertyUsage)reader[root][1][4].SeekNumber();
            }
            else if (type == PropertyTypes.InformationRegister_Dimension)
            {
                property.CascadeDelete = reader[root][1][3].SeekNumber() == 1;
                property.UseForChangeTracking = reader[root][1][6].SeekNumber() == 1;
            }
            else if (type == PropertyTypes.AccountingRegister_Dimension)
            {
                property.IsBalance = reader[root][1][3].SeekNumber() == 1; // Балансовый
                property.AccountingFlag = reader[root][1][4].SeekUuid(); // Признак учёта
            }
            else if (type == PropertyTypes.AccountingRegister_Measure)
            {
                property.IsBalance = reader[root][1][3].SeekNumber() == 1; // Балансовый
                property.AccountingFlag = reader[root][1][4].SeekUuid(); // Признак учёта
                property.AccountingDimensionFlag = reader[root][1][5].SeekUuid(); // Признак учёта субконто
            }
            else if (type == PropertyTypes.BusinessTask_Routing_Property)
            {
                property.RoutingDimension = reader[root][1][4].SeekUuid(); // Измерение адресации
            }

            return property;
        }
    }
}