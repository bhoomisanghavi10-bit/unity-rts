using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Tests
{
    // Wave 4 item 24 (docs/IMPLEMENTATION_ROADMAP.md): the Trebuchet -
    // covers the new MeleeAttacker.SetMinRange mechanic (this project's
    // first "too close to fire" gate - nothing else uses it),
    // CombatBonus's new Trebuchet pairings, and Barracks.
    // RequestTrainTrebuchet's inline Imperial-only age gate (this
    // project's first RequestTrainX with an age check of its own, since
    // every prior gate lived on a tier ladder's RequiredAge). Drives
    // MeleeAttacker directly via its internal Tick(deltaTime), same
    // convention as SiegeSplashTests/BuildingAttackerTests.
    public class TrebuchetTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();
        private static readonly Regex SetDestinationErrorPattern = new Regex("SetDestination.*NavMesh");
        // Same VFX-burst-logs-once-per-hit gotcha SiegeSplashTests/
        // BuildingAttackerTests already document.
        private static readonly Regex VfxDestroyErrorPattern = new Regex("Destroy may not be called from edit mode");

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            foreach (GameObject go in _spawned)
            {
                if (go == null)
                {
                    continue;
                }
                if (go.TryGetComponent(out Unit unit))
                {
                    Unit.All.Remove(unit);
                }
                Object.DestroyImmediate(go);
            }
            _spawned.Clear();
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Chola);
        }

        private GameObject CreateGameObject(string name)
        {
            var go = new GameObject(name);
            _spawned.Add(go);
            return go;
        }

        private MeleeAttacker CreateTrebuchetAttacker(FactionId faction)
        {
            GameObject go = CreateGameObject("Trebuchet");
            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<Attackable>().ConfigureClass(UnitClass.Trebuchet);
            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage(20f);
            attacker.SetRange(10f);
            attacker.SetMinRange(4f);
            attacker.SetUnitClass(UnitClass.Trebuchet);
            return attacker;
        }

        private Attackable CreateHostileUnit(FactionId faction, Vector3 position)
        {
            GameObject go = CreateGameObject("HostileUnit");
            go.transform.position = position;
            go.AddComponent<FactionMember>().Configure(faction);
            Unit unit = go.AddComponent<Unit>();
            if (!Unit.All.Contains(unit))
            {
                Unit.All.Add(unit);
            }
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure(1000f);
            attackable.ConfigureClass(UnitClass.Infantry);
            return attackable;
        }

        [Test]
        public void Tick_TargetInsideMinRange_DealsNoDamage()
        {
            MeleeAttacker attacker = CreateTrebuchetAttacker(FactionId.Player);
            Attackable target = CreateHostileUnit(FactionId.Enemy, new Vector3(2f, 0f, 0f));

            LogAssert.Expect(LogType.Error, SetDestinationErrorPattern);
            attacker.AttackMove(target);
            attacker.Tick(1f);

            Assert.AreEqual(1000f, target.Health, "A target closer than the minimum range must take no damage - the Trebuchet refuses to fire, it doesn't back away.");
        }

        [Test]
        public void Tick_TargetBetweenMinAndMaxRange_DealsDamage()
        {
            MeleeAttacker attacker = CreateTrebuchetAttacker(FactionId.Player);
            Attackable target = CreateHostileUnit(FactionId.Enemy, new Vector3(6f, 0f, 0f));

            LogAssert.Expect(LogType.Error, SetDestinationErrorPattern);
            attacker.AttackMove(target);
            LogAssert.Expect(LogType.Error, VfxDestroyErrorPattern);
            attacker.Tick(1f);

            Assert.Less(target.Health, 1000f, "A target between the minimum and maximum range must take damage.");
        }

        [Test]
        public void Tick_TargetBeyondMaxRange_MovesButDealsNoDamage()
        {
            MeleeAttacker attacker = CreateTrebuchetAttacker(FactionId.Player);
            Attackable target = CreateHostileUnit(FactionId.Enemy, new Vector3(15f, 0f, 0f));

            LogAssert.Expect(LogType.Error, SetDestinationErrorPattern);
            attacker.AttackMove(target);
            LogAssert.Expect(LogType.Error, SetDestinationErrorPattern);
            attacker.Tick(1f);

            Assert.AreEqual(1000f, target.Health, "A target beyond max range must take no damage - the Trebuchet should be moving toward it, not firing.");
        }

        [Test]
        public void CombatBonus_TrebuchetVsBuilding_Is5x()
        {
            Assert.AreEqual(5f, CombatBonus.Multiplier(UnitClass.Trebuchet, UnitClass.Building));
        }

        [Test]
        public void CombatBonus_CavalryVsTrebuchet_Is1_5x()
        {
            Assert.AreEqual(1.5f, CombatBonus.Multiplier(UnitClass.Cavalry, UnitClass.Trebuchet));
        }

        // --- Barracks.RequestTrainTrebuchet's Imperial-only age gate ---

        private Barracks CreateBarracks(FactionId faction)
        {
            GameObject go = CreateGameObject("Barracks");
            go.AddComponent<FactionMember>().Configure(faction);
            return go.AddComponent<Barracks>();
        }

        private ResourceStockpile CreateStockpile(float wood = 1000f, float gold = 1000f)
        {
            ResourceStockpile stockpile = CreateGameObject("Stockpile").AddComponent<ResourceStockpile>();
            stockpile.SetTotal(ResourceType.Wood, wood);
            stockpile.SetTotal(ResourceType.Gold, gold);
            return stockpile;
        }

        [Test]
        public void RequestTrainTrebuchet_BelowImperialAge_IsRefusedEvenWithFullResources()
        {
            ResourceStockpile stockpile = CreateStockpile();
            Barracks barracks = CreateBarracks(FactionId.Player);
            AgeProgress.Initialize(FactionId.Player, AgeId.Durg);

            barracks.RequestTrainTrebuchet();

            Assert.IsFalse(barracks.IsTraining, "Trebuchet must not be trainable before Imperial age.");
            Assert.AreEqual(1000f, stockpile.GetTotal(ResourceType.Wood), "A refused request must not deduct resources.");
        }

        [Test]
        public void RequestTrainTrebuchet_AtImperialAge_DeductsCostAndStartsTraining()
        {
            ResourceStockpile stockpile = CreateStockpile();
            Barracks barracks = CreateBarracks(FactionId.Player);
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);

            barracks.RequestTrainTrebuchet();

            Assert.IsTrue(barracks.IsTraining);
            Assert.Less(stockpile.GetTotal(ResourceType.Wood), 1000f);
            Assert.Less(stockpile.GetTotal(ResourceType.Gold), 1000f);
        }

        [Test]
        public void RequestTrainTrebuchet_WhileAlreadyTraining_DoesNotDeductTwice()
        {
            ResourceStockpile stockpile = CreateStockpile();
            Barracks barracks = CreateBarracks(FactionId.Player);
            AgeProgress.Initialize(FactionId.Player, AgeId.Imperial);

            barracks.RequestTrainTrebuchet();
            float woodAfterFirst = stockpile.GetTotal(ResourceType.Wood);
            barracks.RequestTrainTrebuchet();

            Assert.AreEqual(woodAfterFirst, stockpile.GetTotal(ResourceType.Wood));
        }
    }
}
