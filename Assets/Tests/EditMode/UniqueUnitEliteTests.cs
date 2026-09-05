using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Tests
{
    // Wave 3 item 16 (docs/IMPLEMENTATION_ROADMAP.md): the Unique-unit Elite
    // tier. Mirrors ElephantLineTests's coverage shape - eligibility/gating
    // on UniqueUnitEliteProgress, Durg.RequestResearchEliteTier's
    // request/cost/gate behavior per slot, Durg.TrainsEliteEligible's
    // per-slot civ detection (including Maurya's slot 0/slot 1 split - the
    // War Elephant slot is NOT eligible, only Pillar Edict Scholar is), and
    // 2 of the 5 factories actually baking the current tier's bonus/name in
    // at spawn - not retroactively.
    public class UniqueUnitEliteTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            foreach (GameObject go in _spawned)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _spawned.Clear();
            UniqueUnitEliteProgress.ResetForTests();
        }

        private GameObject CreateGameObject(string name)
        {
            GameObject go = new GameObject(name);
            _spawned.Add(go);
            return go;
        }

        private Durg CreateDurg(FactionId faction)
        {
            GameObject go = CreateGameObject("Durg");
            go.AddComponent<FactionMember>().Configure(faction);
            return go.AddComponent<Durg>();
        }

        private ResourceStockpile CreateStockpile(FactionId faction, float gold = 1000f, float wood = 1000f)
        {
            ResourceStockpile stockpile = CreateGameObject("Stockpile-" + faction).AddComponent<ResourceStockpile>();
            stockpile.SetTotal(ResourceType.Gold, gold);
            stockpile.SetTotal(ResourceType.Wood, wood);
            return stockpile;
        }

        // --- UniqueUnitEliteProgress ---

        [Test]
        public void IsEligible_TrueForAllFiveEliteUnits_FalseForWarElephants()
        {
            Assert.IsTrue(UniqueUnitEliteProgress.IsEligible("chola_naval_raider"));
            Assert.IsTrue(UniqueUnitEliteProgress.IsEligible("rajput_royal_guard"));
            Assert.IsTrue(UniqueUnitEliteProgress.IsEligible("pillar_edict_scholar"));
            Assert.IsTrue(UniqueUnitEliteProgress.IsEligible("maratha_mavla_raider"));
            Assert.IsTrue(UniqueUnitEliteProgress.IsEligible("maratha_durg_garrison"));

            Assert.IsFalse(UniqueUnitEliteProgress.IsEligible("maurya_war_elephant"));
            Assert.IsFalse(UniqueUnitEliteProgress.IsEligible("vijayanagara_war_elephant"));
        }

        [Test]
        public void HasNextTier_TrueBeforeAdvance_FalseAfter()
        {
            Assert.IsTrue(UniqueUnitEliteProgress.HasNextTier(FactionId.Player, "chola_naval_raider"));
            UniqueUnitEliteProgress.AdvanceTier(FactionId.Player, "chola_naval_raider");
            Assert.IsFalse(UniqueUnitEliteProgress.HasNextTier(FactionId.Player, "chola_naval_raider"));
        }

        [Test]
        public void NextTierAgeRequirementMet_FalseBelowImperial_TrueAtImperial()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            Assert.IsFalse(UniqueUnitEliteProgress.NextTierAgeRequirementMet(FactionId.Player, "rajput_royal_guard"));

            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            Assert.IsTrue(UniqueUnitEliteProgress.NextTierAgeRequirementMet(FactionId.Player, "rajput_royal_guard"));
        }

        [Test]
        public void AdvanceTier_IsPerFactionAndPerUnitId()
        {
            UniqueUnitEliteProgress.AdvanceTier(FactionId.Player, "maratha_mavla_raider");

            Assert.IsTrue(UniqueUnitEliteProgress.IsElite(FactionId.Player, "maratha_mavla_raider"));
            // A different unitId for the same faction is untouched.
            Assert.IsFalse(UniqueUnitEliteProgress.IsElite(FactionId.Player, "maratha_durg_garrison"));
            // A different faction for the same unitId is untouched.
            Assert.IsFalse(UniqueUnitEliteProgress.IsElite(FactionId.Enemy, "maratha_mavla_raider"));
        }

        [Test]
        public void DisplayName_ReturnsMahaPrefixedNameOnceElite()
        {
            Assert.AreEqual("Naval Raider", UniqueUnitEliteProgress.DisplayName(FactionId.Player, "chola_naval_raider", "Naval Raider"));
            UniqueUnitEliteProgress.AdvanceTier(FactionId.Player, "chola_naval_raider");
            Assert.AreEqual("Maha Naval Raider", UniqueUnitEliteProgress.DisplayName(FactionId.Player, "chola_naval_raider", "Naval Raider"));
        }

        // --- Durg.TrainsEliteEligible ---

        [Test]
        public void TrainsEliteEligible_TrueForChola_SingleSlot()
        {
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Chola);
            Durg durg = CreateDurg(FactionId.Player);

            Assert.IsTrue(durg.TrainsEliteEligible(0));
            Assert.IsFalse(durg.TrainsEliteEligible(1));
        }

        [Test]
        public void TrainsEliteEligible_MauryaSlot0False_Slot1True()
        {
            // Maurya slot 0 is the War Elephant - covered by item 13's
            // ElephantLineProgress instead, deliberately excluded here.
            // Slot 1 is the Pillar Edict Scholar - eligible.
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Maurya);
            Durg durg = CreateDurg(FactionId.Player);

            Assert.IsFalse(durg.TrainsEliteEligible(0));
            Assert.IsTrue(durg.TrainsEliteEligible(1));
        }

        [Test]
        public void TrainsEliteEligible_MarathaBothSlotsTrue()
        {
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Maratha);
            Durg durg = CreateDurg(FactionId.Player);

            Assert.IsTrue(durg.TrainsEliteEligible(0));
            Assert.IsTrue(durg.TrainsEliteEligible(1));
        }

        [Test]
        public void TrainsEliteEligible_VijayanagaraFalse_NoElite()
        {
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Vijayanagara);
            Durg durg = CreateDurg(FactionId.Player);

            Assert.IsFalse(durg.TrainsEliteEligible(0));
            Assert.IsFalse(durg.TrainsEliteEligible(1));
        }

        // --- Durg.RequestResearchEliteTier ---

        [Test]
        public void RequestResearchEliteTier_DeductsCostAndStarts_WhenAgeMet()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Rajput);
            Durg durg = CreateDurg(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            durg.RequestResearchEliteTier(0);

            Assert.IsTrue(durg.IsResearchingEliteTier(0));
            EliteTierData tier = UniqueUnitEliteProgress.DataFor("rajput_royal_guard");
            Assert.AreEqual(1000f - tier.GoldCost, stockpile.GetTotal(ResourceType.Gold));
            Assert.AreEqual(1000f - tier.WoodCost, stockpile.GetTotal(ResourceType.Wood));
        }

        [Test]
        public void RequestResearchEliteTier_BlockedByAgeGate_EvenWithFunds()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Rajput);
            Durg durg = CreateDurg(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            durg.RequestResearchEliteTier(0);

            Assert.IsFalse(durg.IsResearchingEliteTier(0));
            Assert.AreEqual(1000f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchEliteTier_NotEligibleSlot_DoesNotStart()
        {
            // Maurya slot 0 (War Elephant) is not elite-eligible.
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Maurya);
            Durg durg = CreateDurg(FactionId.Player);
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);

            durg.RequestResearchEliteTier(0);

            Assert.IsFalse(durg.IsResearchingEliteTier(0));
            Assert.AreEqual(1000f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestResearchEliteTier_TwoSlotsResearchIndependently()
        {
            // Maratha has 2 elite-eligible slots - both should be able to
            // research concurrently without one blocking the other, same
            // "runs alongside, doesn't block" convention as every other
            // independent tier track.
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Maratha);
            Durg durg = CreateDurg(FactionId.Player);
            CreateStockpile(FactionId.Player);

            durg.RequestResearchEliteTier(0);
            durg.RequestResearchEliteTier(1);

            Assert.IsTrue(durg.IsResearchingEliteTier(0));
            Assert.IsTrue(durg.IsResearchingEliteTier(1));
        }

        [Test]
        public void RequestResearchEliteTier_AtMaxTier_DoesNotStart()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Rajput);
            UniqueUnitEliteProgress.AdvanceTier(FactionId.Player, "rajput_royal_guard");
            Durg durg = CreateDurg(FactionId.Player);
            CreateStockpile(FactionId.Player);

            durg.RequestResearchEliteTier(0);

            Assert.IsFalse(durg.IsResearchingEliteTier(0));
        }

        // --- Factories: bake current tier in at spawn, not retroactive ---

        [Test]
        public void CholaNavalRaiderFactory_BakesCurrentTierNameAndBonusAtSpawn()
        {
            LogAssert.ignoreFailingMessages = true;
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Chola);

            GameObject baseline = CholaNavalRaiderFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(baseline);
            float baselineHp = baseline.GetComponent<Attackable>().MaxHealth;

            UniqueUnitEliteProgress.AdvanceTier(FactionId.Player, "chola_naval_raider");
            GameObject upgraded = CholaNavalRaiderFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(upgraded);

            StringAssert.Contains("Maha Naval Raider", upgraded.name);
            StringAssert.DoesNotContain("Maha Naval Raider", baseline.name);
            Assert.Greater(upgraded.GetComponent<Attackable>().MaxHealth, baselineHp);

            // Not retroactive: the already-spawned baseline instance keeps
            // its original HP even though the faction has since advanced.
            Assert.AreEqual(baselineHp, baseline.GetComponent<Attackable>().MaxHealth);
        }

        [Test]
        public void MarathaDurgGarrisonFactory_BakesCurrentTierNameAndBonusAtSpawn()
        {
            LogAssert.ignoreFailingMessages = true;
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Maratha);

            GameObject baseline = MarathaDurgGarrisonFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(baseline);
            float baselineHp = baseline.GetComponent<Attackable>().MaxHealth;

            UniqueUnitEliteProgress.AdvanceTier(FactionId.Player, "maratha_durg_garrison");
            GameObject upgraded = MarathaDurgGarrisonFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(upgraded);

            StringAssert.Contains("Maha Durg Garrison", upgraded.name);
            StringAssert.DoesNotContain("Maha Durg Garrison", baseline.name);
            Assert.Greater(upgraded.GetComponent<Attackable>().MaxHealth, baselineHp);

            Assert.AreEqual(baselineHp, baseline.GetComponent<Attackable>().MaxHealth);
        }
    }
}
