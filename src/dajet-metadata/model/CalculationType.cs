using DaJet.TypeSystem;

namespace DaJet.Metadata
{
    ///<summary>
    ///План видов расчёта. Загружается только как ссылочный тип: код CKinds из файла DBNames
    ///и идентификатор ссылочного типа "ПланВидовРасчетаСсылка.<Имя>". Этого достаточно,
    ///чтобы реквизит такого типа получил верный набор колонок и верный код типа.
    ///Сам план видов расчёта и его таблицы не описываются (нет в MetadataNames и MetadataLookup).
    ///</summary>
    internal sealed class CalculationType : MetadataObject
    {
        ///<summary>Идентификатор типа объекта метаданных "План видов расчёта" (компонента "Расчёт")</summary>
        internal static readonly Guid MetadataType = new("30b100d6-b29f-47ac-aec7-cb8ca8a54767");

        internal static CalculationType Create(Guid uuid)
        {
            return new CalculationType(uuid);
        }
        internal CalculationType(Guid uuid) : base(uuid) { }

        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.CKinds)
            {
                Code = code;
            }
        }
        internal override string GetMainDbName()
        {
            return string.Format("_{0}{1}", MetadataToken.CKinds, Code);
        }
        internal override string GetTableNameИзменения()
        {
            return string.Empty;
        }
        public override string ToString()
        {
            return string.Format("{0}.{1}", "ПланВидовРасчета", Name);
        }

        internal sealed class Parser : ConfigFileParser
        {
            internal override void Initialize(ReadOnlySpan<byte> file, in MetadataRegistry registry)
            {
                // План видов расчёта нужен только как ссылочный тип: ошибка разбора его файла
                // не должна останавливать загрузку метаданных (тип реквизита определится, как раньше)

                try
                {
                    RegisterReference(file, in registry);
                }
                catch (FormatException)
                {
                    // Значение по ожидаемому адресу не является UUID: ссылка не регистрируется
                }
            }
            private static void RegisterReference(ReadOnlySpan<byte> file, in MetadataRegistry registry)
            {
                ConfigFileReader reader = new(file);

                // Идентификатор объекта метаданных - значение поля FileName в таблице Config
                Guid uuid = reader[2][2][2][2][3].SeekUuid();

                if (uuid == Guid.Empty || !registry.TryGetEntry(uuid, out CalculationType metadata))
                {
                    return;
                }

                // Имя объекта метаданных конфигурации
                metadata.Name = reader[2][2][2][3].SeekString();

                // Идентификатор ссылочного типа данных, например, "ПланВидовРасчетаСсылка.Начисления"
                Guid reference = reader[2][5].SeekUuid();

                // Заимствованный объект расширения не имеет собственного кода в файле DBNames.
                // Без кода ссылка не регистрируется: тип такого реквизита определяется, как и раньше.

                if (reference != Guid.Empty && metadata.Code > 0)
                {
                    registry.AddReference(uuid, reference);
                }
            }
            internal override EntityDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
            {
                throw new NotSupportedException("План видов расчёта загружается только как ссылочный тип");
            }
        }
    }
}
