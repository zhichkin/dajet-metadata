namespace DaJet.Metadata
{
    internal abstract class MetadataObject
    {
        protected MetadataObject(Guid uuid) { Uuid = uuid; }
        ///<summary>Идентификатор объекта метаданных</summary>
        internal Guid Uuid { get; private set; }
        ///<summary>Код объекта метаданных</summary>
        internal int Code { get; set; }
        ///<summary>Идентификатор конфигурации (основной или расширения)</summary>
        internal byte Cfid { get; set; }
        ///<summary>Имя объекта метаданных</summary>
        internal string Name { get; set; } = string.Empty;
        internal virtual void AddDbName(int code, string name)
        {
            throw new NotImplementedException(); // DefinedType, Enumeration, Property
        }
        internal abstract string GetMainDbName(); // return string.Format("_{0}{1}", DbName, TypeCode);
        internal abstract string GetTableNameИзменения(); // return string.Format("_{0}{1}", MetadataToken.ReferenceChngR, _ChngR);
        internal virtual bool IsChangeTrackingEnabled { get { return false; } }
        public override string ToString()
        {
            return string.Format("{0}.{1}", GetType().Name, Name);
        }

        private ExtensionType _extension;

        // Собственный объект расширения
        internal void MarkAsExtension() { _extension |= ExtensionType.Extension; }
        internal bool IsExtension { get { return (_extension & ExtensionType.Extension) == ExtensionType.Extension; } }

        // Заимствованный расширением объект основной конфигурации
        internal void MarkAsBorrowed() { _extension |= ExtensionType.Borrowed; }
        internal bool IsBorrowed { get { return (_extension & ExtensionType.Borrowed) == ExtensionType.Borrowed; } }

        ///<summary>Тип объекта метаданных, например, "Справочник"
        ///<br>Смотри также: <see cref="MetadataTypes"/></br>
        ///</summary>
        //public Guid Type { get; set; } = Guid.Empty;

        ///<summary>Идентификатор расширяемого объекта метаданных основной конфигурации
        ///<br>Используется при синхронизации объектов расширения по внутренним идентификаторам</br>
        ///<br>Смотри также: <see cref="Configuration.MapMetadataByUuid"/></br>
        ///</summary>
        //public Guid Parent { get; set; } = Guid.Empty;
    }
}