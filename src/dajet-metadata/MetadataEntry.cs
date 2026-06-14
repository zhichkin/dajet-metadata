namespace DaJet.Metadata
{
    public sealed class MetadataEntry
    {
        ///<summary>Идентификатор объекта метаданных</summary>
        public Guid Uuid { get; internal set; }
        ///<summary>Код объекта метаданных</summary>
        public int Code { get; internal set; }
        ///<summary>Тип объекта метаданных</summary>
        public string Type { get; internal set; } = string.Empty;
        ///<summary>Имя объекта метаданных</summary>
        public string Name { get; internal set; } = string.Empty;
        ///<summary>Полное имя объекта метаданных</summary>
        public string FullName { get { return string.Format("{0}.{1}", Type, Name); } }

        public override string ToString()
        {
            return string.Format("[{0}] {1} {{{2}}}", Code, FullName, Uuid);
        }
    }
}