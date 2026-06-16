namespace DaJet.TypeSystem
{
    public enum ColumnPurpose : byte
    {
        ///<summary>Single type value of the property. _Fld
        ///<br>Value purpose - single value storage.</br>
        ///</summary>
        Value,
        ///<summary><b>Discriminated (tagged) union</b>
        ///<br>Составной тип данных _Fld + _TYPE binary(1)</br>
        ///<br><b>0x01</b> - Неопределено = null</br>
        ///<br><b>0x02</b> - Булево = boolean</br>
        ///<br><b>0x03</b> - Число = decimal</br>
        ///<br><b>0x04</b> - Дата = DateTime</br>
        ///<br><b>0x05</b> - Строка = string</br>
        ///<br><b>0x06</b> - byte[] = binary</br>
        ///<br><b>0x07</b> - ?</br>
        ///<br><b>0x08</b> - Ссылка = Entity</br>
        ///</summary>
        Tag,
        ///<summary>0x02 Boolean value (bool) _Fld + _L binary(1)</summary>
        Boolean,
        ///<summary>0x03 Numeric value (decimal | int) _Fld + _N numeric(p,s)</summary>
        Numeric,
        ///<summary>0x04 Date and time value (DateTime) _Fld + _T datetime2</summary>
        DateTime,
        ///<summary>0x05 String value (string) _Fld + _S nvarchar | nchar</summary>
        String,
        ///<summary>0x06 Binary value (byte[]) _Fld + _B varbinary(max)</summary>
        Binary,
        ///<summary>Type code of the reference type (int) _Fld + _RTRef binary(4)</summary>
        TypeCode,
        ///<summary>0x08 Reference type primary key value (Guid) _Fld + _RRRef binary(16)</summary>
        Identity
    }
    public static class ColumnPurposeExtensions
    {
        public static string GetSuffix(this ColumnPurpose purpose)
        {
            if (purpose == ColumnPurpose.Tag) { return "TYPE"; }
            else if (purpose == ColumnPurpose.Boolean) { return "L"; }
            else if (purpose == ColumnPurpose.Numeric) { return "N"; }
            else if (purpose == ColumnPurpose.DateTime) { return "T"; }
            else if (purpose == ColumnPurpose.String) { return "S"; }
            else if (purpose == ColumnPurpose.Binary) { return "B"; }
            else if (purpose == ColumnPurpose.TypeCode) { return "TRef"; }
            else if (purpose == ColumnPurpose.Identity) { return "RRef"; }
            else
            {
                return string.Empty;
            }
        }
    }
}