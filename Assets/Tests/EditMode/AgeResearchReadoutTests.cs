using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Tests
{
    // Wave 5 item 32 (docs/IMPLEMENTATION_ROADMAP.md): the always-visible
    // Age/research readout. Civilization/Population/Age labels were already
    // always-on (folded into ResourceHUD during the 2026-09-12 HUD pass);
    // this item adds a research-in-progress meter, scoped to Age-up
    // specifically (matches AoE's own top-center "torch" readout - other
    // concurrent research tracks this project supports, e.g. Karmashala's
    // Attack/Armor or a Barracks tier line, stay visible on their own
    // building's selection UI, unchanged). TownCenter.FindAgingUp is the
    // one piece of new logic with real branching, so it's what's covered
    // here - ResourceHUD.Update() itself is thin UI glue, verified live via
    // UnityMCP instead, same convention this project's other pure-UI-wiring
    // sessions already follow.
    //
    // Uses FactionId.Enemy2 to stay isolated from Building.All entries any
    // other test in the same run might leave behind for Player/Enemy - same
    // convention AgeUpRequirementTests already established.
    public class AgeResearchReadoutTests
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
                // AgeUpRequirementTests already documents.
                if (go.TryGetComponent(out Building building))
                {
                    Building.All.Remove(building);
                }
                Object.DestroyImmediate(go);
            }
            _spawned.Clear();
            AgeProgress.Initialize(TestFaction, AgeId.Ancient);
        }

        private TownCenter CreateTownCenter(FactionId faction)
        {
            var go = new GameObject("TC");
            _spawned.Add(go);
            go.AddComponent<FactionMember>().Configure(faction);
            TownCenter tc = go.AddComponent<TownCenter>();
            if (!Building.All.Contains(tc))
            {
                Building.All.Add(tc);
            }
            return tc;
        }

        private ResourceStockpile CreateStockpile(FactionId faction)
        {
            var go = new GameObject("Stockpile");
            _spawned.Add(go);
            ResourceStockpile stockpile = go.AddComponent<ResourceStockpile>();
            stockpile.Configure(faction);
            stockpile.SetTotal(ResourceType.Wood, 1000f);
            stockpile.SetTotal(ResourceType.Stone, 1000f);
            return stockpile;
        }

        [Test]
        public void FindAgingUp_WithNoTownCenters_ReturnsNull()
        {
            Assert.IsNull(TownCenter.FindAgingUp(TestFaction));
        }

        [Test]
        public void FindAgingUp_WithTownCenterNotAgingUp_ReturnsNull()
        {
            CreateTownCenter(TestFaction);

            Assert.IsNull(TownCenter.FindAgingUp(TestFaction));
        }

        [Test]
        public void FindAgingUp_WhileAgingUp_ReturnsThatTownCenter()
        {
            AgeProgress.Initialize(TestFaction, AgeId.Ancient);
            TownCenter tc = CreateTownCenter(TestFaction);
            CreateStockpile(TestFaction);

            tc.RequestAgeUp();

            Assert.IsTrue(tc.IsAgingUp);
            Assert.AreSame(tc, TownCenter.FindAgingUp(TestFaction));
            Assert.AreEqual(AgeId.Classical, tc.AgeUpTarget);
        }

        [Test]
        public void FindAgingUp_IgnoresOtherFactions()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Ancient);
            TownCenter playerTc = CreateTownCenter(FactionId.Player);
            CreateStockpile(FactionId.Player);
            playerTc.RequestAgeUp();

            Assert.IsTrue(playerTc.IsAgingUp);
            Assert.IsNull(TownCenter.FindAgingUp(TestFaction),
                "a different faction's in-progress Age-up must not leak into this faction's readout");

            // TearDown only resets TestFaction's AgeProgress - reset Player's
            // own state directly so this test doesn't bleed into others.
            AgeProgress.Initialize(FactionId.Player, AgeId.Ancient);
        }
    }
}
