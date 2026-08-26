using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Tests
{
    // Covers the backend train/trade paths BuildMenu's new Cavalry/Siege/
    // Spearman/Dock/Market buttons call into - RequestTrainX and Sell/Buy
    // were already fully implemented before those buttons existed (only
    // AiController or nothing at all could reach them), so this is the
    // first automated coverage of methods that were previously only
    // exercised manually or by the AI.
    public class TrainingAndTradeTests
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
        }

        private GameObject CreateGameObject(string name)
        {
            GameObject go = new GameObject(name);
            _spawned.Add(go);
            return go;
        }

        private ResourceStockpile CreateStockpile(FactionId faction, float food = 1000f, float wood = 1000f, float gold = 1000f, float stone = 1000f)
        {
            ResourceStockpile stockpile = CreateGameObject("Stockpile").AddComponent<ResourceStockpile>();
            stockpile.SetTotal(ResourceType.Food, food);
            stockpile.SetTotal(ResourceType.Wood, wood);
            stockpile.SetTotal(ResourceType.Gold, gold);
            stockpile.SetTotal(ResourceType.Stone, stone);
            return stockpile;
        }

        private Barracks CreateBarracks(FactionId faction)
        {
            GameObject go = CreateGameObject("Barracks");
            go.AddComponent<FactionMember>().Configure(faction);
            return go.AddComponent<Barracks>();
        }

        private Dock CreateDock(FactionId faction)
        {
            GameObject go = CreateGameObject("Dock");
            go.AddComponent<FactionMember>().Configure(faction);
            return go.AddComponent<Dock>();
        }

        private Market CreateMarket(FactionId faction)
        {
            GameObject go = CreateGameObject("Market");
            go.AddComponent<FactionMember>().Configure(faction);
            return go.AddComponent<Market>();
        }

        [Test]
        public void RequestTrainCavalry_DeductsCostAndStartsTraining()
        {
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);
            Barracks barracks = CreateBarracks(FactionId.Player);

            barracks.RequestTrainCavalry();

            Assert.IsTrue(barracks.IsTraining);
            Assert.AreEqual(1000f - 70f, stockpile.GetTotal(ResourceType.Food));
            Assert.AreEqual(1000f - 50f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestTrainSiege_DeductsCostAndStartsTraining()
        {
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);
            Barracks barracks = CreateBarracks(FactionId.Player);

            barracks.RequestTrainSiege();

            Assert.IsTrue(barracks.IsTraining);
            Assert.AreEqual(1000f - 90f, stockpile.GetTotal(ResourceType.Food));
            Assert.AreEqual(1000f - 75f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestTrainSpearman_DeductsCostAndStartsTraining()
        {
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);
            Barracks barracks = CreateBarracks(FactionId.Player);

            barracks.RequestTrainSpearman();

            Assert.IsTrue(barracks.IsTraining);
            Assert.Less(stockpile.GetTotal(ResourceType.Food), 1000f);
            Assert.Less(stockpile.GetTotal(ResourceType.Wood), 1000f);
        }

        [Test]
        public void Barracks_WhileTraining_RejectsAnotherTrainRequest()
        {
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);
            Barracks barracks = CreateBarracks(FactionId.Player);

            barracks.RequestTrainCavalry();
            float foodAfterFirst = stockpile.GetTotal(ResourceType.Food);
            barracks.RequestTrainSiege();

            Assert.AreEqual(foodAfterFirst, stockpile.GetTotal(ResourceType.Food));
        }

        [Test]
        public void Dock_RequestTrainFishingBoat_DeductsCostAndStartsTraining()
        {
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);
            Dock dock = CreateDock(FactionId.Player);

            dock.RequestTrainFishingBoat();

            Assert.IsTrue(dock.IsTraining);
            Assert.AreEqual(1000f - 40f, stockpile.GetTotal(ResourceType.Food));
            Assert.AreEqual(1000f - 30f, stockpile.GetTotal(ResourceType.Wood));
        }

        [Test]
        public void Dock_RequestTrainWarGalley_DeductsCostAndStartsTraining()
        {
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);
            Dock dock = CreateDock(FactionId.Player);

            dock.RequestTrainWarGalley();

            Assert.IsTrue(dock.IsTraining);
            Assert.AreEqual(1000f - 60f, stockpile.GetTotal(ResourceType.Food));
            Assert.AreEqual(1000f - 60f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void Market_Sell_ConvertsResourceToGoldAtSellRate()
        {
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player, wood: 200f, gold: 0f);
            Market market = CreateMarket(FactionId.Player);

            bool result = market.Sell(ResourceType.Wood, 50f);

            Assert.IsTrue(result);
            Assert.AreEqual(200f - 50f, stockpile.GetTotal(ResourceType.Wood));
            Assert.AreEqual(50f * market.EffectiveSellRate, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void Market_Buy_ConvertsGoldToResourceAtBuyRate()
        {
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player, food: 0f, gold: 1000f);
            Market market = CreateMarket(FactionId.Player);

            bool result = market.Buy(ResourceType.Food, 50f);

            Assert.IsTrue(result);
            Assert.AreEqual(50f, stockpile.GetTotal(ResourceType.Food));
            Assert.AreEqual(1000f - (50f * market.EffectiveBuyRate), stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void Market_Sell_FailsAndNoOpsWhenInsufficientResource()
        {
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player, stone: 10f);
            Market market = CreateMarket(FactionId.Player);

            bool result = market.Sell(ResourceType.Stone, 50f);

            Assert.IsFalse(result);
            Assert.AreEqual(10f, stockpile.GetTotal(ResourceType.Stone));
        }
    }
}
