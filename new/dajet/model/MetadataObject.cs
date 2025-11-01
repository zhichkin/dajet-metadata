namespace DaJet
{
    public abstract class MetadataObject
    {
        public MetadataObject(Guid uuid) { Uuid = uuid; }
        ///<summary>Тип объекта метаданных, например, "Справочник"
        ///<br>Смотри также: <see cref="MetadataType"/></br>
        ///</summary>
        //public Guid Type { get; set; } = Guid.Empty;
        ///<summary>Идентификатор объекта метаданных</summary>
        public Guid Uuid { get; private set; }
        ///<summary>Идентификатор расширяемого объекта метаданных основной конфигурации
        ///<br>Используется при синхронизации объектов расширения по внутренним идентификаторам</br>
        ///<br>Смотри также: <see cref="InfoBase.MapMetadataByUuid"/></br>
        ///</summary>
        //public Guid Parent { get; set; } = Guid.Empty;
        ///<summary>Имя объекта метаданных (регистронезависимое)</summary>
        public string Name { get; set; } = string.Empty;
        ///<summary>Коллекция идентификаторов СУБД <see cref="DbName"/> объекта метаданных</summary>
        internal List<DbName> DbNames { get; set; }
        public override string ToString()
        {
            return string.Format("{0}.{1}", GetType().Name, Name);
        }
    }
}