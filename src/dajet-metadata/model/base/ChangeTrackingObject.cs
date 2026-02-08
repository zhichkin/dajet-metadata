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

                //NOTE: Собственные объекты расширений всегда получают постфикс x1
                //NOTE: при наличии DbName токена _ChngR > 0.

                //NOTE: Основные объекты конфигурации получают постфикс x1
                //NOTE: по наличию заимствованного объекта и настройкам расширения.

                //NOTE: Если заимствованный объект входит в состав любого плана обмена своего расширения,
                //NOTE: то к имени его таблицы регистрации изменений DbName добавляется постфикс x1.

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