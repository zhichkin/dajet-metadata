using DaJet.Metadata.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace DaJet.Metadata.Tests
{
    [TestClass] public class CatalogHierarchyOfFolders : TestClassBase
    {
        private MetaObject Catalog { get; set; }
        public CatalogHierarchyOfFolders() : base() { }
        private void SetupMetaObject()
        {
            if (Catalog != null) return;
            SetupInfoBase();
            Catalog = InfoBase.Catalogs.Values.Where(i => i.Name == "СправочникИерархическийГруппы").FirstOrDefault();
            Assert.IsNotNull(Catalog);
        }

        // ЭтоГруппа (наличие)
        // Родитель
    }
}