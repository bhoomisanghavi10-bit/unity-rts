using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Tests
{
    // Wave 2 item 7: covers the Durg building itself (component-level -
    // DurgFactory.Place is deliberately not exercised here, same reason
    // BarracksFactory.Place/TownCenterFactory.Place aren't in
    // UniqueUnitsTests.cs - building factories NRE outside Play mode; see
    // this project's own documented EditMode-only limitation). The two
    // resource-deduction tests that used to live here were relocated
    // in-place within UniqueUnitsTests.cs (RequestTrainUniqueUnit moved off
    // Barracks onto Durg this session) rather than duplicated.
    public class DurgTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _spawned.Clear();

            // Same restore convention UniqueUnitsTests.cs documents - avoid
            // contaminating other tests' Player-faction assumptions in
            // static registries shared across the same test run.
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Chola);
        }

        private GameObject CreateGameObject(string name)
        {
            GameObject go = new GameObject(name);
            _spawned.Add(go);
            return go;
        }

        private Durg CreateDurg(FactionId faction, CivilizationId civ)
        {
            CivilizationRegistry.Assign(faction, civ);
            GameObject go = CreateGameObject("Durg");
            go.AddComponent<FactionMember>().Configure(faction);
            return go.AddComponent<Durg>();
        }

        private ResourceStockpile CreateStockpile(float food = 1000f, float gold = 1000f)
        {
            ResourceStockpile stockpile = CreateGameObject("Stockpile").AddComponent<ResourceStockpile>();
            stockpile.SetTotal(ResourceType.Food, food);
            stockpile.SetTotal(ResourceType.Gold, gold);
            return stockpile;
        }

        [Test]
        public void RequestTrainUniqueUnit_WhileAlreadyTraining_DoesNotDeductTwice()
        {
            ResourceStockpile stockpile = CreateStockpile();
            Durg durg = CreateDurg(FactionId.Player, CivilizationId.Maurya);

            durg.RequestTrainUniqueUnit(0);
            float foodAfterFirst = stockpile.GetTotal(ResourceType.Food);
            durg.RequestTrainUniqueUnit(0);

            Assert.AreEqual(foodAfterFirst, stockpile.GetTotal(ResourceType.Food));
        }

        [Test]
        public void UniqueUnitCount_DelegatesToUniqueUnitDefinition()
        {
            Durg mauryaDurg = CreateDurg(FactionId.Player, CivilizationId.Maurya);
            Assert.AreEqual(UniqueUnitDefinition.CountFor(CivilizationId.Maurya), mauryaDurg.UniqueUnitCount);

            Durg cholaDurg = CreateDurg(FactionId.Player, CivilizationId.Chola);
            Assert.AreEqual(UniqueUnitDefinition.CountFor(CivilizationId.Chola), cholaDurg.UniqueUnitCount);
        }

        [Test]
        public void IsComplete_TrueWithoutConstructionSite()
        {
            Durg durg = CreateDurg(FactionId.Player, CivilizationId.Chola);
            Assert.IsTrue(durg.IsComplete);
        }

        [Test]
        public void IsTraining_FalseBeforeAnyRequest()
        {
            Durg durg = CreateDurg(FactionId.Player, CivilizationId.Chola);
            Assert.IsFalse(durg.IsTraining);
        }
    }
}
