using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Tests
{
    // Covers Roadmap Section 5 item 3: the 2-slot unique-unit support
    // Maurya/Maratha needed (UniqueUnitDefinition/Barracks), Pillar Edict
    // Scholar's gather aura, and Maratha Durg Garrison's Garrison
    // component. Uses FactionId.Enemy (not Player), same reasoning as
    // CivPassiveBonusTests - avoids contaminating TrainingAndTradeTests'
    // Player-faction assumptions in static registries shared across the
    // same test run.
    public class UniqueUnitsTests
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

        [Test]
        public void CountFor_MauryaAndMaratha_HaveTwoUniqueUnits()
        {
            Assert.AreEqual(2, UniqueUnitDefinition.CountFor(CivilizationId.Maurya));
            Assert.AreEqual(2, UniqueUnitDefinition.CountFor(CivilizationId.Maratha));
        }

        [Test]
        public void CountFor_ChoIaVijayanagaraRajput_HaveOneUniqueUnitEach()
        {
            Assert.AreEqual(1, UniqueUnitDefinition.CountFor(CivilizationId.Chola));
            Assert.AreEqual(1, UniqueUnitDefinition.CountFor(CivilizationId.Vijayanagara));
            Assert.AreEqual(1, UniqueUnitDefinition.CountFor(CivilizationId.Rajput));
        }

        [Test]
        public void For_MauryaSlots_ReturnDistinctUnitsMatchingCsv()
        {
            UniqueUnitDefinition slot0 = UniqueUnitDefinition.For(CivilizationId.Maurya, 0);
            UniqueUnitDefinition slot1 = UniqueUnitDefinition.For(CivilizationId.Maurya, 1);

            Assert.AreEqual(130f, slot0.FoodCost);
            Assert.AreEqual(100f, slot0.GoldCost);
            Assert.AreEqual(40f, slot1.FoodCost);
            Assert.AreEqual(10f, slot1.GoldCost);
            Assert.AreNotEqual(slot0.Name, slot1.Name);
        }

        private ResourceStockpile CreateStockpile(FactionId faction, float food = 1000f, float gold = 1000f)
        {
            ResourceStockpile stockpile = CreateGameObject("Stockpile").AddComponent<ResourceStockpile>();
            stockpile.SetTotal(ResourceType.Food, food);
            stockpile.SetTotal(ResourceType.Gold, gold);
            return stockpile;
        }

        private Barracks CreateBarracks(FactionId faction, CivilizationId civ)
        {
            CivilizationRegistry.Assign(faction, civ);
            GameObject go = CreateGameObject("Barracks");
            go.AddComponent<FactionMember>().Configure(faction);
            return go.AddComponent<Barracks>();
        }

        // ResourceStockpile's own faction field ([SerializeField], no
        // Configure method - see ResourceStockpile.cs) always defaults to
        // Player with no way to construct one for Enemy in a test, so
        // these use FactionId.Player like TrainingAndTradeTests does, and
        // restore CivilizationRegistry's Player entry back to Chola
        // afterward - same "explicitly restore Player to Chola" convention
        // CivPassiveBonusTests documents for its own Player-faction tests.
        [TearDown]
        public void RestorePlayerCivilization()
        {
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Chola);
        }

        [Test]
        public void RequestTrainUniqueUnit_Slot0_DeductsWarElephantCost()
        {
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);
            Barracks barracks = CreateBarracks(FactionId.Player, CivilizationId.Maurya);

            barracks.RequestTrainUniqueUnit(0);

            Assert.IsTrue(barracks.IsTraining);
            Assert.AreEqual(1000f - 130f, stockpile.GetTotal(ResourceType.Food));
            Assert.AreEqual(1000f - 100f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void RequestTrainUniqueUnit_Slot1_DeductsPillarEdictScholarCost()
        {
            ResourceStockpile stockpile = CreateStockpile(FactionId.Player);
            Barracks barracks = CreateBarracks(FactionId.Player, CivilizationId.Maurya);

            barracks.RequestTrainUniqueUnit(1);

            Assert.IsTrue(barracks.IsTraining);
            Assert.AreEqual(1000f - 40f, stockpile.GetTotal(ResourceType.Food));
            Assert.AreEqual(1000f - 10f, stockpile.GetTotal(ResourceType.Gold));
        }

        [Test]
        public void Gatherer_WithinPillarEdictAuraRange_GetsBoostedRate()
        {
            GameObject auraGo = CreateGameObject("Scholar");
            auraGo.AddComponent<PillarEdictAura>().Configure(FactionId.Enemy);
            auraGo.transform.position = Vector3.zero;

            GameObject workerGo = CreateGameObject("Worker");
            workerGo.AddComponent<FactionMember>().Configure(FactionId.Enemy);
            Gatherer gatherer = workerGo.AddComponent<Gatherer>();
            workerGo.transform.position = new Vector3(3f, 0f, 0f);

            gatherer.RefreshAuraMultiplier();

            Assert.AreEqual(PillarEdictAura.Multiplier, gatherer.AuraMultiplier, 0.001f);
        }

        [Test]
        public void Gatherer_OutsidePillarEdictAuraRange_StaysUnboosted()
        {
            GameObject auraGo = CreateGameObject("Scholar");
            auraGo.AddComponent<PillarEdictAura>().Configure(FactionId.Enemy);
            auraGo.transform.position = Vector3.zero;

            GameObject workerGo = CreateGameObject("Worker");
            workerGo.AddComponent<FactionMember>().Configure(FactionId.Enemy);
            Gatherer gatherer = workerGo.AddComponent<Gatherer>();
            workerGo.transform.position = new Vector3(50f, 0f, 0f);

            gatherer.RefreshAuraMultiplier();

            Assert.AreEqual(1f, gatherer.AuraMultiplier, 0.001f);
        }

        // General garrisoning system (2026-09-01): Garrison was generalized
        // into GarrisonPoint (see GarrisonPointTests.cs for the fuller,
        // dedicated coverage of the new pooled-capacity/durgOnly behavior).
        // These 3 tests stay here, updated to the new API, since they
        // specifically cover the Maratha Durg Garrison unique unit's own
        // siege-immunity mechanic via a durgOnly:true GarrisonPoint - the
        // exact configuration WallFactory now uses.
        [Test]
        public void GarrisonPoint_TryGarrison_DeactivatesUnitAndSetsBuildingSiegeImmune()
        {
            GameObject wallGo = CreateGameObject("Wall");
            Attackable wallAttackable = wallGo.AddComponent<Attackable>();
            wallAttackable.ConfigureClass(UnitClass.Building);
            GarrisonPoint garrisonPoint = wallGo.AddComponent<GarrisonPoint>();
            garrisonPoint.Configure(1, durgOnly: true);

            GameObject unitGo = CreateGameObject("DurgGarrisonUnit");
            unitGo.AddComponent<GarrisonSeeker>().Configure(true);

            bool result = garrisonPoint.TryGarrison(unitGo);

            Assert.IsTrue(result);
            Assert.AreEqual(1, garrisonPoint.Count);
            Assert.IsFalse(unitGo.activeSelf);
            Assert.IsTrue(wallAttackable.SiegeImmune);
        }

        [Test]
        public void GarrisonPoint_TryGarrison_FailsWhenAlreadyOccupied()
        {
            GameObject wallGo = CreateGameObject("Wall");
            wallGo.AddComponent<Attackable>().ConfigureClass(UnitClass.Building);
            GarrisonPoint garrisonPoint = wallGo.AddComponent<GarrisonPoint>();
            garrisonPoint.Configure(1, durgOnly: true);
            GameObject firstUnit = CreateGameObject("FirstUnit");
            firstUnit.AddComponent<GarrisonSeeker>().Configure(true);
            GameObject secondUnit = CreateGameObject("SecondUnit");
            secondUnit.AddComponent<GarrisonSeeker>().Configure(true);
            garrisonPoint.TryGarrison(firstUnit);

            bool result = garrisonPoint.TryGarrison(secondUnit);

            Assert.IsFalse(result);
            Assert.IsTrue(secondUnit.activeSelf);
        }

        [Test]
        public void GarrisonPoint_UngarrisonAll_ReactivatesUnitAndClearsSiegeImmune()
        {
            GameObject wallGo = CreateGameObject("Wall");
            Attackable wallAttackable = wallGo.AddComponent<Attackable>();
            wallAttackable.ConfigureClass(UnitClass.Building);
            GarrisonPoint garrisonPoint = wallGo.AddComponent<GarrisonPoint>();
            garrisonPoint.Configure(1, durgOnly: true);
            GameObject unitGo = CreateGameObject("DurgGarrisonUnit");
            unitGo.AddComponent<GarrisonSeeker>().Configure(true);
            garrisonPoint.TryGarrison(unitGo);

            garrisonPoint.UngarrisonAll();

            Assert.AreEqual(0, garrisonPoint.Count);
            Assert.IsTrue(unitGo.activeSelf);
            Assert.IsFalse(wallAttackable.SiegeImmune);
        }
    }
}
