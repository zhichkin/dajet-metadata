using DaJet.TypeSystem;

namespace DaJet.Metadata
{
    internal abstract class ChangeTrackingObject : DatabaseObject
    {
        protected int _ChngR;
        protected ChangeTrackingObject(Guid uuid, int code, string name) : base(uuid, code, name) { }
        internal abstract string GetTableNameИзменения();
        internal bool IsChangeTrackingEnabled { get { return _ChngR > 0; } }
        internal virtual void ConfigureChangeTrackingTable(in EntityDefinition owner)
        {
            //NOTE: Реализация по умолчанию для ссылочных объектов метаданных

            if (IsChangeTrackingEnabled)
            {
                EntityDefinition changes = new() // Таблица регистрации изменений
                {
                    Name = "Изменения",
                    DbName = GetTableNameИзменения() //TODO: (extended ? "x1" : string.Empty)
                };

                Configurator.ConfigurePropertyУзелПланаОбмена(in changes);
                Configurator.ConfigurePropertyНомерСообщения(in changes);
                Configurator.ConfigurePropertyСсылка(in changes, TypeCode);

                //TODO:
                //foreach (MetadataProperty property in entity.Properties)
                //{
                //    if (property is SharedProperty shared && shared.DataSeparationUsage == DataSeparationUsage.Use)
                //    {
                //        table.Properties.Add(shared);
                //    }
                //}

                owner.Entities.Add(changes);
            }
        }
    }
}