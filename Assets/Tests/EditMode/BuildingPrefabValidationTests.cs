using System.Linq;
using NUnit.Framework;
using KingdomsOfBharat.Editor;

namespace KingdomsOfBharat.Tests
{
    // Runs the shipped-asset validator over every real building prefab and the definition catalog.
    // This is the guard that keeps a converted building from losing its LODs or materials.
    public class BuildingPrefabValidationTests
    {
        [Test]
        public void ShippedBuildingPrefabsAndCatalog_HaveNoValidationErrors()
        {
            var errors = BuildingPrefabValidator.ValidateAll().Where(i => i.IsError).Select(i => i.ToString()).ToList();
            Assert.IsEmpty(errors, string.Join("\n", errors));
        }
    }
}
