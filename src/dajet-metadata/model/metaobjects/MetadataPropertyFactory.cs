using System;
using System.Collections.Generic;

namespace DaJet.Metadata.Model
{
    public interface IMetadataPropertyFactory
    {
        MetadataProperty CreateProperty(MetadataObject owner, SqlFieldInfo field);
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
            if (PropertyNameLookup.TryGetValue(fieldName, out string propertyName))
            {
                return propertyName;
            }
            return string.Empty;
        }
        public MetadataProperty CreateProperty(MetadataObject owner, SqlFieldInfo field)
        {
            string fieldName = field.COLUMN_NAME.ToLowerInvariant().TrimStart('_');

            string propertyName = LookupPropertyName(fieldName);
            if (string.IsNullOrEmpty(propertyName))
            {
                propertyName = field.COLUMN_NAME;
            }

            MetadataProperty property = new MetadataProperty()
            {
                Name = propertyName,
                DbName = field.COLUMN_NAME,
                FileName = Guid.Empty,
                Purpose = PropertyPurpose.System
            };

            SetupPropertyType(owner, property, field);
            
            property.Fields.Add(new DatabaseField()
            {
                Name = field.COLUMN_NAME,
                TypeName = field.DATA_TYPE,
                Length = field.CHARACTER_MAXIMUM_LENGTH,
                Scale = field.NUMERIC_SCALE,
                Precision = field.NUMERIC_PRECISION,
                IsNullable = field.IS_NULLABLE,
                Purpose = (field.DATA_TYPE == "timestamp" || fieldName == "version") ? FieldPurpose.Version : FieldPurpose.Value
            });

            return property;
        }
        private void SetupPropertyType(MetadataObject owner, MetadataProperty property, SqlFieldInfo field)
        {
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
    }
}