namespace DaJet.Metadata.Model
{
    ///<summary>
    ///Типы данных 1С
    ///</summary>
    public enum DataTypes
    {
        ///<summary>[0x01] Неопределено</summary>
        NULL = 1,
        ///<summary>[0x02] Булево (L|B)</summary>
        Boolean = 2,
        ///<summary>[0x03] Число (N)</summary>
        Numeric = 3,
        ///<summary>[0x04] Дата (T|D)</summary>
        DateTime = 4,
        ///<summary>[0x05] Строка (S)</summary>
        String = 5,
        ///<summary>[0x08] Ссылка (#)</summary>
        Object = 8,
        ///<summary>Составной тип данных</summary>
        Multiple = 9,
        ///<summary>УникальныйИдентификатор</summary>
        UUID = 10,
        ///<summary>ХранилищеЗначения</summary>
        ValueStorage = 11,
        ///<summary>Бинарное значение</summary>
        Binary = 12
    }
}