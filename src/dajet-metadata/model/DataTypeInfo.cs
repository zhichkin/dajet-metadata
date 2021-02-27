// В целях оптимизации в свойстве ReferenceTypeCode класса DataTypeInfo
// не хранятся все допустимые для данного описания коды ссылочных типов.
// В случае составного типа код типа конкретного значения можно получить в базе данных в поле {имя поля}_TRef.
// В случае же сохранения кода типа в базу данных код типа можно получить из свойства MetaObject.TypeCode.
// У не составных типов такого поля в базе данных нет, поэтому необходимо сохранить код типа в DataTypeInfo,
// именно по этому значение свойства ReferenceTypeCode класса DataTypeInfo может быть больше ноля.

namespace DaJet.Metadata.Model
{
    ///<summary>Класс для описания типов данных свойства объекта метаданных (реквизита, измерения или ресурса)</summary>
    public sealed class DataTypeInfo
    {
        ///<summary>Типом значения свойства может быть "Строка" (поддерживает составной тип данных)</summary>
        public bool CanBeString { get; set; } = false;
        ///<summary>Типом значения свойства может быть "Булево" (поддерживает составной тип данных)</summary>
        public bool CanBeBoolean { get; set; } = false;
        ///<summary>Типом значения свойства может быть "Число" (поддерживает составной тип данных)</summary>
        public bool CanBeNumeric { get; set; } = false;
        ///<summary>Типом значения свойства может быть "Дата" (поддерживает составной тип данных)</summary>
        public bool CanBeDateTime { get; set; } = false;
        ///<summary>Типом значения свойства может быть "Ссылка" (поддерживает составной тип данных)</summary>
        public bool CanBeReference { get; set; } = false;
        ///<summary>Код ссылочного типа значения. По умолчанию равен 0 - многозначный ссылочный тип (составной тип данных).</summary>
        public int ReferenceTypeCode { get; set; } = 0;  // 0 = multiple type by default
        ///<summary>Типом значения свойства является byte[8] - версия данных, timestamp, rowversion.Не поддерживает составной тип данных.</summary>
        public bool IsBinary { get; set; } = false;
        ///<summary>Тип значения свойства "УникальныйИдентификатор", binary(16). Не поддерживает составной тип данных.</summary>
        public bool IsUuid { get; set; } = false;
        ///<summary>Тип значения свойства "ХранилищеЗначения", varbinary(max). Не поддерживает составной тип данных.</summary>
        public bool IsValueStorage { get; set; } = false;
        ///<summary>Проверяет имеет ли свойство составной тип данных</summary>
        public bool IsMultipleType
        {
            get
            {
                if (IsUuid || IsValueStorage || IsBinary) return false;

                int count = 0;
                if (CanBeString) count++;
                if (CanBeBoolean) count++;
                if (CanBeNumeric) count++;
                if (CanBeDateTime) count++;
                if (CanBeReference) count++;
                if (count > 1) return true;

                if (CanBeReference && ReferenceTypeCode == 0) return true;

                return false;
            }
        }
    }
}