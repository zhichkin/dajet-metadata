using DaJet.TypeSystem;

namespace DaJet.Metadata
{
    internal abstract class ChangeTrackingObject : MetadataObject
    {
        protected ChangeTrackingObject(Guid uuid) : base(uuid) { }
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
                Configurator.ConfigurePropertyСсылка(in changes, Code);

                foreach (PropertyDefinition property in owner.Properties)
                {
                    if (property.Purpose.IsSharedProperty() && property.Purpose.UseDataSeparation())
                    {
                        changes.Properties.Add(property);
                    }
                }

                owner.Entities.Add(changes);
            }
        }
    }
}