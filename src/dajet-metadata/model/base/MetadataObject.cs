namespace DaJet.Metadata
{
    internal abstract class MetadataObject
    {
        protected MetadataObject(Guid uuid) { Uuid = uuid; }
        ///<summary>Идентификатор конфигурации (основной или расширения)</summary>
        internal byte Cfid { get; set; }
        ///<summary>Идентификатор объекта метаданных</summary>
        internal Guid Uuid { get; private set; }
        ///<summary>Код объекта метаданных</summary>
        internal int Code { get; set; }
        ///<summary>Имя объекта метаданных</summary>
        internal string Name { get; set; } = string.Empty;
        ///<summary>Это объект основной конфигурации</summary>
        internal bool IsMain { get { return Cfid == 0; } }
        ///<summary>Это объект расширения (собственный или заимствованный)</summary>
        internal bool IsExtension { get { return Cfid > 0; } }
        ///<summary>Это заимствованный объект расширения</summary>
        internal bool IsBorrowed { get; set; }
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