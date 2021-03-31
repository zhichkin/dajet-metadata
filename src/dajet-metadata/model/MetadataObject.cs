using System;

namespace DaJet.Metadata.Model
{
    public abstract class MetadataObject
    {
        ///<summary>Идентификатор объекта метаданных из файла объекта метаданных</summary>
        public Guid Uuid { get; set; } = Guid.Empty;
        ///<summary>Идентификатор файла объекта метаданных в таблице Config и DBNames</summary>
        public Guid FileName { get; set; } = Guid.Empty;
        public string Name { get; set; } = string.Empty;
        public string Alias { get; set; } = string.Empty;
    }
}