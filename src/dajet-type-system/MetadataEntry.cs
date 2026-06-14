namespace DaJet.TypeSystem
{
    public sealed class MetadataEntry
    {
        public MetadataEntry(int code, Guid uuid, string type, string name)
        {
            Code = code;
            Uuid = uuid;
            Type = type;
            Name = name;
        }
        ///<summary>Код объекта метаданных</summary>
        public int Code { get; }
        ///<summary>Идентификатор объекта метаданных</summary>
        public Guid Uuid { get; }
        ///<summary>Тип объекта метаданных</summary>
        public string Type { get; } = string.Empty;
        ///<summary>Имя объекта метаданных</summary>
        public string Name { get; } = string.Empty;
        ///<summary>Полное имя объекта метаданных</summary>
        public string FullName { get { return string.Format("{0}.{1}", Type, Name); } }
        public override string ToString()
        {
            return string.Format("[{0}] {1} {{{2}}}", Code, FullName, Uuid);
        }
    }
}