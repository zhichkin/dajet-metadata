namespace DaJet
{
    internal abstract class MetadataObject
    {
        protected MetadataObject(Guid uuid) { Uuid = uuid; }
        ///<summary>Идентификатор объекта метаданных</summary>
        internal Guid Uuid { get; private set; }
        ///<summary>Имя объекта метаданных (регистронезависимое)</summary>
        internal string Name { get; set; } = string.Empty;
        internal virtual void AddDbName(int code, string name)
        {
            throw new NotImplementedException();
        }
        public override string ToString()
        {
            return string.Format("{0}.{1}", GetType().Name, Name);
        }

        ///<summary>Тип объекта метаданных, например, "Справочник"
        ///<br>Смотри также: <see cref="MetadataType"/></br>
        ///</summary>
        //public Guid Type { get; set; } = Guid.Empty;

        ///<summary>Идентификатор расширяемого объекта метаданных основной конфигурации
        ///<br>Используется при синхронизации объектов расширения по внутренним идентификаторам</br>
        ///<br>Смотри также: <see cref="InfoBase.MapMetadataByUuid"/></br>
        ///</summary>
        //public Guid Parent { get; set; } = Guid.Empty;
    }
}