using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Tests
{
    // Wave 1 item 6 (docs/IMPLEMENTATION_ROADMAP.md): age-up building-count
    // gate. User-confirmed design: 2 completed, non-TownCenter buildings
    // owned by the faction, and the gate only applies from Classical
    // onward (Ancient->Classical stays cost-only). Uses FactionId.Enemy2 to
    // stay isolated from Building.All entries any other test in the same
    // run might leave behind for Player/Enemy.
    public class AgeUpRequirementTests
    {
        private const FactionId TestFaction = FactionId.Enemy2;
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go == null)
                {
                    continue;
                }
                // Building.All registration normally happens via OnEnable,
                // but Unity's Editor doesn't guarantee that fires
                // synchronously within a single test method - same gotcha
                // BuildingFootprintTests already documents. Managed
                // directly here instead.
                if (go.TryGetComponent(out Building building))
                {
                    Building.All.Remove(building);
                }
                Object.DestroyImmediate(go);
            }
            _spawned.Clear();
            AgeProgress.Initialize(TestFaction, AgeId.Ancient);
        }

        private GameObject CreateGameObject(string name)
        {
            var go = new GameObject(name);
            _spawned.Add(go);
            return go;
        }

        // Registers directly in Building.All rather than relying on
        // Building.OnEnable to have fired by the time AgeUpRequirement.IsMet
        // reads it - see the TearDown comment above for why.
        private void CreateCompletedBuilding(FactionId faction)
        {
            GameObject go = CreateGameObject("Building");
            go.AddComponent<FactionMember>().Configure(faction);
            Building building = go.AddComponent<Building>();
            if (!Building.All.Contains(building))
            {
                Building.All.Add(building);
            }
        }

        private void CreateUnderConstructionBuilding(FactionId faction)
        {
            GameObject go = CreateGameObject("Foundation");
            go.AddComponent<FactionMember>().Configure(faction);
            Building building = go.AddComponent<Building>();
            if (!Building.All.Contains(building))
            {
                Building.All.Add(building);
            }
            go.AddComponent<ConstructionSite>().EnsureInitialized();
        }

        [TestCase(AgeId.Ancient, false)]
        [TestCase(AgeId.Classical, true)]
        [TestCase(AgeId.Durg, true)]
        public void AppliesTo_ExemptsOnlyAncient(AgeId currentAge, bool expectedApplies)
        {
            Assert.AreEqual(expectedApplies, AgeUpRequirement.AppliesTo(currentAge));
        }

        [Test]
        public void IsMet_WithNoBuildings_IsFalse()
        {
            Assert.IsFalse(AgeUpRequirement.IsMet(TestFaction));
        }

        [Test]
        public void IsMet_WithTwoCompletedBuildings_IsTrue()
        {
            CreateCompletedBuilding(TestFaction);
            CreateCompletedBuilding(TestFaction);

            Assert.IsTrue(AgeUpRequirement.IsMet(TestFaction));
        }

        [Test]
        public void IsMet_TownCenterDoesNotCountTowardRequirement()
        {
            GameObject go = CreateGameObject("TC");
            go.AddComponent<FactionMember>().Configure(TestFaction);
            TownCenter tc = go.AddComponent<TownCenter>();
            if (!Building.All.Contains(tc))
            {
                Building.All.Add(tc);
            }
            CreateCompletedBuilding(TestFaction);

            Assert.IsFalse(AgeUpRequirement.IsMet(TestFaction),
                "TownCenter + 1 other building should not satisfy a 2-building requirement");
        }

        [Test]
        public void IsMet_UnderConstructionBuildingsDoNotCount()
        {
            CreateUnderConstructionBuilding(TestFaction);
            CreateUnderConstructionBuilding(TestFaction);

            Assert.IsFalse(AgeUpRequirement.IsMet(TestFaction));
        }

        [Test]
        public void IsMet_OtherFactionBuildingsDoNotCount()
        {
            CreateCompletedBuilding(FactionId.Player);
            CreateCompletedBuilding(FactionId.Player);

            Assert.IsFalse(AgeUpRequirement.IsMet(TestFaction));
        }

        [Test]
        public void RequestAgeUp_FromClassicalWithoutBuildings_DoesNotAdvance()
        {
            AgeProgress.Initialize(TestFaction, AgeId.Classical);
            GameObject tcGo = CreateGameObject("TC");
            tcGo.AddComponent<FactionMember>().Configure(TestFaction);
            TownCenter tc = tcGo.AddComponent<TownCenter>();
            ResourceStockpile stockpile = CreateGameObject("Stockpile").AddComponent<ResourceStockpile>();
            stockpile.Configure(TestFaction);
            stockpile.SetTotal(ResourceType.Wood, 1000f);
            stockpile.SetTotal(ResourceType.Stone, 1000f);

            tc.RequestAgeUp();

            Assert.IsFalse(tc.IsAgingUp);
            Assert.AreEqual(AgeId.Classical, AgeProgress.CurrentAge(TestFaction));
            Assert.AreEqual(1000f, stockpile.GetTotal(ResourceType.Wood), 0.001f,
                "resources must not be spent when the building-count gate blocks the request");
        }

        [Test]
        public void RequestAgeUp_FromClassicalWithTwoBuildings_Advances()
        {
            AgeProgress.Initialize(TestFaction, AgeId.Classical);
            GameObject tcGo = CreateGameObject("TC");
            tcGo.AddComponent<FactionMember>().Configure(TestFaction);
            TownCenter tc = tcGo.AddComponent<TownCenter>();
            CreateCompletedBuilding(TestFaction);
            CreateCompletedBuilding(TestFaction);
            ResourceStockpile stockpile = CreateGameObject("Stockpile").AddComponent<ResourceStockpile>();
            stockpile.Configure(TestFaction);
            stockpile.SetTotal(ResourceType.Wood, 1000f);
            stockpile.SetTotal(ResourceType.Stone, 1000f);

            tc.RequestAgeUp();

            Assert.IsTrue(tc.IsAgingUp);
        }

        [Test]
        public void RequestAgeUp_FromAncientWithoutBuildings_StillAdvances()
        {
            AgeProgress.Initialize(TestFaction, AgeId.Ancient);
            GameObject tcGo = CreateGameObject("TC");
            tcGo.AddComponent<FactionMember>().Configure(TestFaction);
            TownCenter tc = tcGo.AddComponent<TownCenter>();
            ResourceStockpile stockpile = CreateGameObject("Stockpile").AddComponent<ResourceStockpile>();
            stockpile.Configure(TestFaction);
            stockpile.SetTotal(ResourceType.Wood, 1000f);
            stockpile.SetTotal(ResourceType.Stone, 1000f);

            tc.RequestAgeUp();

            Assert.IsTrue(tc.IsAgingUp,
                "Ancient->Classical is exempt from the building-count gate by design");
        }
    }
}
