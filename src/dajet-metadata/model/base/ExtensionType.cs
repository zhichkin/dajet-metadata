namespace DaJet.Metadata
{
    [Flags] internal enum ExtensionType : byte
    {
        ///<summary>Объект основной конфигурации</summary>
        None = 0x00,
        ///<summary>Собственный объект расширения</summary>
        Extension = 0x01,
        ///<summary>Заимствованный расширением объект основной конфигурации</summary>
        Borrowed = 0x02
    }
}