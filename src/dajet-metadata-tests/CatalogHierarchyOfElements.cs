using DaJet.Metadata.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace DaJet.Metadata.Tests
{
    [TestClass] public class CatalogHierarchyOfElements : TestClassBase
    {
        private MetadataObject Catalog { get; set; }
        public CatalogHierarchyOfElements() : base() { }
        private void SetupMetadataObject()
        {
            if (Catalog != null) return;
            SetupInfoBase();
            Catalog = InfoBase.Catalogs.Values.Where(i => i.Name == "СправочникИерархическийЭлементы").FirstOrDefault();
            Assert.IsNotNull(Catalog);
        }
        // ЭтоГруппа (отсутствие)
        // Родитель
    }
}