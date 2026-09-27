using System.Linq;
using KingdomsOfBharat.Performance;
using NUnit.Framework;

namespace KingdomsOfBharat.Tests
{
    public class RepresentativeBenchmarkTests
    {
        [Test]
        public void Scenario_IsFixedAndContainsTheDocumentedSettlementAndBattleComposition()
        {
            var scenario = RepresentativeBenchmarkRunner.BuildScenario();

            Assert.AreEqual(RepresentativeBenchmarkRunner.ScenarioId, scenario.id);
            Assert.AreEqual(20260926, RepresentativeBenchmarkRunner.Seed);
            Assert.AreEqual(10, scenario.buildings.Count);
            Assert.AreEqual(24, scenario.units.Count);
            Assert.AreEqual(4, scenario.units.Count(unit => unit.unitType == "unit.common.worker"));
            Assert.AreEqual(6, scenario.units.Count(unit => unit.unitType == "unit.chola.padati.t1"));
            Assert.AreEqual(6, scenario.units.Count(unit => unit.unitType == "unit.chola.dhanurdhara.t1"));
            Assert.AreEqual(4, scenario.units.Count(unit => unit.unitType == "Cavalry"));
            Assert.AreEqual(4, scenario.units.Count(unit => unit.unitType == "Siege"));
            Assert.AreEqual(2, scenario.buildings.Count(building => building.buildingType == "building.common.town_center"));
            Assert.AreEqual(2, scenario.buildings.Count(building => building.buildingType == "building.common.barracks"));
        }
    }
}
