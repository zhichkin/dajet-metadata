using System;
using System.Collections.Generic;
using System.Linq;

namespace DaJet.Metadata.Model
{
    public interface IMetadataPropertyFactory
    {
        string GetPropertyName(SqlFieldInfo field);
        DatabaseField CreateField(SqlFieldInfo field);
        MetadataProperty CreateProperty(ApplicationObject owner, string name, SqlFieldInfo field);

        void AddPropertyРегистратор(ApplicationObject register, ApplicationObject document, DatabaseProvider provider);

        MetadataProperty CreateProperty(string name, PropertyPurpose purpose);
    }
    public abstract class MetadataPropertyFactory : IMetadataPropertyFactory
    {
        public MetadataPropertyFactory()
        {
            InitializePropertyNameLookup();
        }
        protected abstract void InitializePropertyNameLookup();
        protected Dictionary<string, string> PropertyNameLookup { get; } = new Dictionary<string, string>();
        protected virtual string LookupPropertyName(string fieldName)
        {
            if (PropertyNameLookup.TryGetValue(fieldName.ToLowerInvariant(), out string propertyName))
            {
                return propertyName;
            }
            return string.Empty;
        }

        public string GetPropertyName(SqlFieldInfo field)
        {
            return LookupPropertyName(field.COLUMN_NAME);
        }
        public DatabaseField CreateField(SqlFieldInfo field)
        {
            return new DatabaseField()
            {
                Name = field.COLUMN_NAME,
                TypeName = field.DATA_TYPE,
                Length = field.CHARACTER_MAXIMUM_LENGTH,
                Scale = field.NUMERIC_SCALE,
                Precision = field.NUMERIC_PRECISION,
                IsNullable = field.IS_NULLABLE,
                Purpose = (field.DATA_TYPE == "timestamp"
                        || field.COLUMN_NAME == "_version"
                        || field.COLUMN_NAME == "_Version")
                            ? FieldPurpose.Version
                            : FieldPurpose.Value
            };
        }
        public MetadataProperty CreateProperty(ApplicationObject owner, string name, SqlFieldInfo field)
        {
            MetadataProperty property = new MetadataProperty()
            {
                Name = name,
                DbName = field.COLUMN_NAME,
                FileName = Guid.Empty,
                Purpose = PropertyPurpose.System
            };
            SetupPropertyType(owner, property, field);
            return property;
        }
        private void SetupPropertyType(ApplicationObject owner, MetadataProperty property, SqlFieldInfo field)
        {
            // TODO: учесть именования типов PostgreSQL, например (mchar, mvarchar)

            if (field.DATA_TYPE == "nvarchar")
            {
                property.PropertyType.CanBeString = true;
            }
            else if (field.DATA_TYPE == "numeric")
            {
                property.PropertyType.CanBeNumeric = true;
            }
            else if (field.DATA_TYPE == "timestamp")
            {
                property.PropertyType.IsBinary = true;
            }
            else if (field.DATA_TYPE == "binary")
            {
                if (field.CHARACTER_MAXIMUM_LENGTH == 1)
                {
                    property.PropertyType.CanBeBoolean = true;
                }
                else if (field.CHARACTER_MAXIMUM_LENGTH == 16)
                {
                    if (field.COLUMN_NAME.ToLowerInvariant().TrimStart('_') == "idrref")
                    {
                        property.PropertyType.IsUuid = true;
                    }
                    else
                    {
                        property.PropertyType.CanBeReference = true;
                        if (owner is TablePart)
                        {
                            property.PropertyType.ReferenceTypeCode = ((TablePart)owner).Owner.TypeCode;
                        }
                        else
                        {
                            property.PropertyType.ReferenceTypeCode = owner.TypeCode;
                        }
                    }
                }
            }
        }

        public MetadataProperty CreateProperty(string name, PropertyPurpose purpose)
        {
            return new MetadataProperty()
            {
                Name = name,
                Purpose = purpose,
                FileName = Guid.Empty
            };
        }

        // Используется для сихронизации добавления свойства "Регистратор" между документами
        private readonly object syncRegister = new object();
        public void AddPropertyРегистратор(ApplicationObject register, ApplicationObject document, DatabaseProvider provider)
        {
            lock (syncRegister)
            {
                AddPropertyРегистраторSynchronized(register, document, provider);
            }
        }
        private void AddPropertyРегистраторSynchronized(ApplicationObject register, ApplicationObject document, DatabaseProvider provider)
        {
            MetadataProperty property = register.Properties.Where(p => p.Name == "Регистратор").FirstOrDefault();

            if (property == null)
            {
                // добавляем новое свойство
                property = CreateProperty("Регистратор", PropertyPurpose.System);
                property.DbName = (provider == DatabaseProvider.SQLServer ? "_Recorder" : "_recorder");
                property.PropertyType.CanBeReference = true;
                property.PropertyType.ReferenceTypeCode = document.TypeCode; // single type value
                property.Fields.Add(new DatabaseField()
                {
                    Name = (provider == DatabaseProvider.SQLServer ? "_RecorderRRef" : "_recorderrref"),
                    Length = 16,
                    TypeName = "binary",
                    Scale = 0,
                    Precision = 0,
                    IsNullable = false,
                    KeyOrdinal = 0,
                    IsPrimaryKey = true,
                    Purpose = FieldPurpose.Value
                });
                register.Properties.Add(property);
                return;
            }

            // На всякий случай проверям повторное обращение одного и того же документа
            if (property.PropertyType.ReferenceTypeCode == document.TypeCode) return;

            // Проверям необходимость добавления поля для хранения кода типа документа
            if (property.PropertyType.ReferenceTypeCode == 0) return;

            // Добавляем поле для хранения кода типа документа, предварительно убеждаясь в его отсутствии
            if (property.Fields.Where(f => f.Name.ToLowerInvariant() == "_recordertref").FirstOrDefault() == null)
            {
                property.Fields.Add(new DatabaseField()
                {
                    Name = (provider == DatabaseProvider.SQLServer ? "_RecorderTRef" : "_recordertref"),
                    Length = 4,
                    TypeName = "binary",
                    Scale = 0,
                    Precision = 0,
                    IsNullable = false,
                    KeyOrdinal = 0,
                    IsPrimaryKey = true,
                    Purpose = FieldPurpose.TypeCode
                });
            }

            // Устанавливаем признак множественного типа значения (составного типа данных)
            property.PropertyType.ReferenceTypeCode = 0; // multiple type value
        }
    }
}