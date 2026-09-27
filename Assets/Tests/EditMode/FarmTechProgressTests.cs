using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Tests
{
    // Farm mechanics correction against the pasted AoE II reference:
    // FarmTechProgress (Horse Collar/Heavy Plow/Crop Rotation) - a 3-tier,
    // age-gated, sequential track researched at TownCenter, reaching +375
    // Food (550 total with Farm's own 175 base) at Imperial. Mirrors
    // UpgradeProgress's/the various *LineProgress classes' own age-gate
    // test shape.
    public class FarmTechProgressTests
    {
        [TearDown]
        public void TearDown()
        {
            FarmTechProgress.ResetForTests();
        }

        [Test]
        public void Tier_DefaultsToZero()
        {
            Assert.AreEqual(0, FarmTechProgress.Tier(FactionId.Player));
            Assert.AreEqual(0f, FarmTechProgress.MaxFoodBonus(FactionId.Player));
        }

        [Test]
        public void HasNextTier_TrueBelowMax_FalseAtMax()
        {
            Assert.IsTrue(FarmTechProgress.HasNextTier(FactionId.Player));
            for (int i = 0; i < FarmTechProgress.MaxTier; i++)
            {
                FarmTechProgress.Advance(FactionId.Player);
            }
            Assert.IsFalse(FarmTechProgress.HasNextTier(FactionId.Player));
        }

        [Test]
        public void NextTierAgeRequirementMet_FalseBelowRequiredAge_TrueAtOrAbove()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Ancient);
            Assert.IsFalse(FarmTechProgress.NextTierAgeRequirementMet(FactionId.Player));

            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            Assert.IsTrue(FarmTechProgress.NextTierAgeRequirementMet(FactionId.Player));
        }

        [Test]
        public void NextTierName_IsSequential_HorseCollarThenHeavyPlowThenCropRotation()
        {
            Assert.AreEqual("Horse Collar", FarmTechProgress.NextTierName(FactionId.Player));
            FarmTechProgress.Advance(FactionId.Player);
            Assert.AreEqual("Heavy Plow", FarmTechProgress.NextTierName(FactionId.Player));
            FarmTechProgress.Advance(FactionId.Player);
            Assert.AreEqual("Crop Rotation", FarmTechProgress.NextTierName(FactionId.Player));
            FarmTechProgress.Advance(FactionId.Player);
            Assert.IsNull(FarmTechProgress.NextTierName(FactionId.Player));
        }

        [Test]
        public void MaxFoodBonus_Reaches375AtMaxTier_TotalingFarmsAoEAccurate550()
        {
            for (int i = 0; i < FarmTechProgress.MaxTier; i++)
            {
                FarmTechProgress.Advance(FactionId.Player);
            }

            Assert.AreEqual(375f, FarmTechProgress.MaxFoodBonus(FactionId.Player));
        }

        [Test]
        public void MaxFoodBonus_PerFaction_Independent()
        {
            FarmTechProgress.Advance(FactionId.Player);
            Assert.AreEqual(125f, FarmTechProgress.MaxFoodBonus(FactionId.Player));
            Assert.AreEqual(0f, FarmTechProgress.MaxFoodBonus(FactionId.Enemy));
        }

        // --- TownCenter.RequestResearchFarmTech ---

        private readonly System.Collections.Generic.List<GameObject> _spawned =
            new System.Collections.Generic.List<GameObject>();

        [TearDown]
        public void TearDownGameObjects()
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

        private TownCenter CreateTownCenter(FactionId faction)
        {
            var go = new GameObject("TownCenter");
            _spawned.Add(go);
            go.AddComponent<FactionMember>().Configure(faction);
            return go.AddComponent<TownCenter>();
        }

        // ResourceStockpile's faction field is Inspector-only (defaults to
        // Player) - matches FarmTests'/RepairableTests' own CreateStockpile
        // convention.
        private ResourceStockpile CreateStockpile(float gold = 1000f, float wood = 1000f)
        {
            var go = new GameObject("Stockpile");
            _spawned.Add(go);
            ResourceStockpile stockpile = go.AddComponent<ResourceStockpile>();
            stockpile.SetTotal(ResourceType.Gold, gold);
            stockpile.SetTotal(ResourceType.Wood, wood);
            return stockpile;
        }

        [Test]
        public void RequestResearchFarmTech_DeductsCostAndStarts_WhenAgeMet()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            TownCenter townCenter = CreateTownCenter(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile();

            townCenter.RequestResearchFarmTech();

            Assert.IsTrue(townCenter.IsResearchingFarmTech);
            Assert.AreEqual(1000f - FarmTechProgress.NextTierGoldCost(FactionId.Player), stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchFarmTech_BlockedByAgeGate_EvenWithFunds()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Ancient);
            TownCenter townCenter = CreateTownCenter(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile();

            townCenter.RequestResearchFarmTech();

            Assert.IsFalse(townCenter.IsResearchingFarmTech);
            Assert.AreEqual(1000f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchFarmTech_AtMaxTier_DoesNotStart()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            for (int i = 0; i < FarmTechProgress.MaxTier; i++)
            {
                FarmTechProgress.Advance(FactionId.Player);
            }
            TownCenter townCenter = CreateTownCenter(FactionId.Player);
            CreateStockpile();

            townCenter.RequestResearchFarmTech();

            Assert.IsFalse(townCenter.IsResearchingFarmTech);
        }
    }
}
