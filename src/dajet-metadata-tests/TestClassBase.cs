using DaJet.Metadata.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace DaJet.Metadata.Tests
{
    public class TestClassBase
    {
        // dajet-metadata
        // trade_11_2_3_159_demo
        // accounting_3_0_72_72_demo
        public string ConnectionString { get; set; } = "Data Source=ZHICHKIN;Initial Catalog=dajet-metadata;Integrated Security=True";
#pragma warning disable IDE0052 // Remove unread private members
        private readonly IMetadataReader metadata;
#pragma warning restore IDE0052 // Remove unread private members
        private readonly IMetadataFileReader fileReader;
#pragma warning disable IDE0052 // Remove unread private members
        private readonly IConfigurationFileParser configReader;
#pragma warning restore IDE0052 // Remove unread private members

        protected InfoBase InfoBase { get; set; }

        public TestClassBase()
        {
            fileReader = new MetadataFileReader();
            fileReader.UseConnectionString(ConnectionString);
            metadata = new MetadataReader(fileReader);
            configReader = new ConfigurationFileParser(fileReader);
        }
        protected virtual void SetupInfoBase()
        {
            if (InfoBase != null) return;
            InfoBase = metadata.LoadInfoBase();
            Assert.IsNotNull(InfoBase);
        }
        protected MetadataProperty TestPropertyExists(MetadataObject metaObject, string name)
        {
            MetadataProperty property = metaObject.Properties.Where(p => p.Name == name).FirstOrDefault();
            Assert.IsNotNull(property);
            return property;
            //TODO: проверить наличие полей в базе данных
        }
        protected void TestPropertyNotExists(MetadataObject metaObject, string name)
        {
            MetadataProperty property = metaObject.Properties.Where(p => p.Name == name).FirstOrDefault();
            Assert.IsNull(property);
        }
        protected MetadataObject TestTablePartExists(MetadataObject metaObject, string name)
        {
            MetadataObject tablePart = metaObject.MetadataObjects.Where(t => t.Name == name).FirstOrDefault();
            Assert.IsNotNull(tablePart);
            return tablePart;
            //TODO: проверить наличие таблицы в базе данных tablePart.TableName
        }
        protected DatabaseField TestFieldExists(MetadataProperty property, string name)
        {
            DatabaseField field = property.Fields.Where(f => f.Name == name).FirstOrDefault();
            Assert.IsNotNull(field);
            return field;
            //TODO: проверить наличие поля в базе данных - ?
        }
    }
}