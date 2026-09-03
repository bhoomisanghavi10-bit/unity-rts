using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Tests
{
    // Wave 2 item 8: covers the Karmashala building itself (component-level -
    // KarmashalaFactory.Place is deliberately not exercised here, same
    // reason BarracksFactory.Place/DurgFactory.Place aren't in their own
    // component tests - building factories NRE outside Play mode; see this
    // project's own documented EditMode-only limitation).
    public class KarmashalaTests
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

            // UpgradeProgress is static (persists for the whole Editor/
            // Test-Runner domain, not per-match) - reset so tier state one
            // test advances doesn't leak into the next, same convention
            // UpgradeProgressTests already documents.
            UpgradeProgress.ResetForTests();
        }

        private GameObject CreateGameObject(string name)
        {
            GameObject go = new GameObject(name);
            _spawned.Add(go);
            return go;
        }

        private Karmashala CreateKarmashala(FactionId faction)
        {
            GameObject go = CreateGameObject("Karmashala");
            go.AddComponent<FactionMember>().Configure(faction);
            return go.AddComponent<Karmashala>();
        }

        private ResourceStockpile CreateStockpile(float gold = 1000f)
        {
            ResourceStockpile stockpile = CreateGameObject("Stockpile").AddComponent<ResourceStockpile>();
            stockpile.SetTotal(ResourceType.Gold, gold);
            return stockpile;
        }

        [Test]
        public void IsComplete_TrueWithoutConstructionSite()
        {
            Karmashala karmashala = CreateKarmashala(FactionId.Player);
            Assert.IsTrue(karmashala.IsComplete);
        }

        [Test]
        public void IsResearchingAttack_FalseBeforeAnyRequest()
        {
            Karmashala karmashala = CreateKarmashala(FactionId.Player);
            Assert.IsFalse(karmashala.IsResearchingAttack);
        }

        [Test]
        public void IsResearchingArmor_FalseBeforeAnyRequest()
        {
            Karmashala karmashala = CreateKarmashala(FactionId.Player);
            Assert.IsFalse(karmashala.IsResearchingArmor);
        }

        [Test]
        public void RequestResearchAttack_DeductsGoldOnceAtTierZeroCost()
        {
            ResourceStockpile stockpile = CreateStockpile();
            Karmashala karmashala = CreateKarmashala(FactionId.Player);
            float expectedCost = karmashala.NextAttackUpgradeCost;

            karmashala.RequestResearchAttack();

            Assert.AreEqual(1000f - expectedCost, stockpile.GetTotal(ResourceType.Gold));
            Assert.IsTrue(karmashala.IsResearchingAttack);
        }

        [Test]
        public void RequestResearchAttack_WhileAlreadyResearching_DoesNotDeductTwice()
        {
            ResourceStockpile stockpile = CreateStockpile();
            Karmashala karmashala = CreateKarmashala(FactionId.Player);

            karmashala.RequestResearchAttack();
            float goldAfterFirst = stockpile.GetTotal(ResourceType.Gold);
            karmashala.RequestResearchAttack();

            Assert.AreEqual(goldAfterFirst, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchArmor_DeductsGoldOnceAtTierZeroCost()
        {
            ResourceStockpile stockpile = CreateStockpile();
            Karmashala karmashala = CreateKarmashala(FactionId.Player);
            float expectedCost = karmashala.NextArmorUpgradeCost;

            karmashala.RequestResearchArmor();

            Assert.AreEqual(1000f - expectedCost, stockpile.GetTotal(ResourceType.Gold));
            Assert.IsTrue(karmashala.IsResearchingArmor);
        }

        [Test]
        public void RequestResearchArmor_WhileAlreadyResearching_DoesNotDeductTwice()
        {
            ResourceStockpile stockpile = CreateStockpile();
            Karmashala karmashala = CreateKarmashala(FactionId.Player);

            karmashala.RequestResearchArmor();
            float goldAfterFirst = stockpile.GetTotal(ResourceType.Gold);
            karmashala.RequestResearchArmor();

            Assert.AreEqual(goldAfterFirst, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchAttack_AtMaxTier_DoesNotDeduct()
        {
            ResourceStockpile stockpile = CreateStockpile();
            Karmashala karmashala = CreateKarmashala(FactionId.Player);

            for (int i = 0; i < UpgradeProgress.MaxTier; i++)
            {
                UpgradeProgress.AdvanceAttack(FactionId.Player);
            }

            karmashala.RequestResearchAttack();

            Assert.AreEqual(1000f, stockpile.GetTotal(ResourceType.Gold));
            Assert.IsFalse(karmashala.IsResearchingAttack);
        }

        [Test]
        public void RequestResearchAttack_InsufficientGold_DoesNotDeductOrStart()
        {
            ResourceStockpile stockpile = CreateStockpile(gold: 0f);
            Karmashala karmashala = CreateKarmashala(FactionId.Player);

            karmashala.RequestResearchAttack();

            Assert.AreEqual(0f, stockpile.GetTotal(ResourceType.Gold));
            Assert.IsFalse(karmashala.IsResearchingAttack);
        }
    }
}
