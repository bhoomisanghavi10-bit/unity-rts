using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Match;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Tests
{
    // Wave 4 item 28: Hero unit (Maharaja) + Regicide victory condition.
    // Covers HeroProgress's live-scan/persistent-flag split, Durg.
    // RequestTrainHero's independent training track and population-cap-of-1
    // gate, and MatchManager.EvaluateSkirmishOutcome's new Regicide branch
    // (the same internal static testable seam MatchManagerTests.cs already
    // exercises for elimination/Time Limit).
    public class HeroTests
    {
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

                // Same Unit.All-doesn't-deregister-synchronously-in-EditMode
                // gotcha MatchManagerTests.cs/BuildingAttackerTests already
                // document.
                if (go.TryGetComponent(out Unit unit))
                {
                    Unit.All.Remove(unit);
                }

                Object.DestroyImmediate(go);
            }

            _spawned.Clear();
            HeroProgress.ResetForTests();
            GameSettings.RegicideEnabled = false;
            DiplomacyRegistry.Reset();
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Chola);
        }

        private GameObject CreateGameObject(string name)
        {
            var go = new GameObject(name);
            _spawned.Add(go);
            return go;
        }

        private GameObject CreateHero(FactionId faction)
        {
            GameObject go = CreateGameObject("Hero");
            Unit unit = go.AddComponent<Unit>();
            if (!Unit.All.Contains(unit))
            {
                Unit.All.Add(unit);
            }

            go.AddComponent<Attackable>().ConfigureClass(UnitClass.Hero);
            go.AddComponent<FactionMember>().Configure(faction);
            return go;
        }

        private GameObject CreateUnit(FactionId faction)
        {
            GameObject go = CreateGameObject("Unit");
            Unit unit = go.AddComponent<Unit>();
            if (!Unit.All.Contains(unit))
            {
                Unit.All.Add(unit);
            }

            go.AddComponent<Attackable>().ConfigureClass(UnitClass.Infantry);
            go.AddComponent<FactionMember>().Configure(faction);
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

        // --- HeroProgress ---

        [Test]
        public void HeroProgress_IsAlive_FalseWithNoUnits()
        {
            Assert.IsFalse(HeroProgress.IsAlive(FactionId.Player));
        }

        [Test]
        public void HeroProgress_IsAlive_TrueOnceMatchingHeroRegistered()
        {
            CreateHero(FactionId.Player);
            Assert.IsTrue(HeroProgress.IsAlive(FactionId.Player));
            Assert.IsFalse(HeroProgress.IsAlive(FactionId.Enemy), "A hero for Player must not read as alive for Enemy.");
        }

        [Test]
        public void HeroProgress_IsAlive_IgnoresNonHeroUnits()
        {
            CreateUnit(FactionId.Player);
            Assert.IsFalse(HeroProgress.IsAlive(FactionId.Player), "A plain Infantry unit must not count as a living hero.");
        }

        [Test]
        public void HeroProgress_HasTrainedHero_FollowsMarkTrained()
        {
            Assert.IsFalse(HeroProgress.HasTrainedHero(FactionId.Player));
            HeroProgress.MarkTrained(FactionId.Player);
            Assert.IsTrue(HeroProgress.HasTrainedHero(FactionId.Player));
            Assert.IsFalse(HeroProgress.HasTrainedHero(FactionId.Enemy));
        }

        // --- Durg.RequestTrainHero ---

        [Test]
        public void RequestTrainHero_DeductsCostAndStartsTraining()
        {
            ResourceStockpile stockpile = CreateStockpile();
            Durg durg = CreateDurg(FactionId.Player, CivilizationId.Maurya);

            durg.RequestTrainHero();

            Assert.IsTrue(durg.IsTrainingHero);
            Assert.Less(stockpile.GetTotal(ResourceType.Food), 1000f);
            Assert.Less(stockpile.GetTotal(ResourceType.Gold), 1000f);
        }

        [Test]
        public void RequestTrainHero_WhileAlreadyTraining_DoesNotDeductTwice()
        {
            ResourceStockpile stockpile = CreateStockpile();
            Durg durg = CreateDurg(FactionId.Player, CivilizationId.Maurya);

            durg.RequestTrainHero();
            float foodAfterFirst = stockpile.GetTotal(ResourceType.Food);
            durg.RequestTrainHero();

            Assert.AreEqual(foodAfterFirst, stockpile.GetTotal(ResourceType.Food));
        }

        [Test]
        public void RequestTrainHero_RefusedWhileALivingHeroAlreadyExists()
        {
            ResourceStockpile stockpile = CreateStockpile();
            Durg durg = CreateDurg(FactionId.Player, CivilizationId.Maurya);
            CreateHero(FactionId.Player);

            durg.RequestTrainHero();

            Assert.IsFalse(durg.IsTrainingHero, "The population-cap-of-1 check must refuse training a 2nd Maharaja while one is alive.");
            Assert.AreEqual(1000f, stockpile.GetTotal(ResourceType.Food), "A refused request must not deduct resources.");
        }

        [Test]
        public void RequestTrainHero_DoesNotBlockOrGetBlockedByUniqueUnitTraining()
        {
            CreateStockpile();
            Durg durg = CreateDurg(FactionId.Player, CivilizationId.Maurya);

            durg.RequestTrainUniqueUnit();
            durg.RequestTrainHero();

            Assert.IsTrue(durg.IsTraining, "The unique-unit queue must still be running.");
            Assert.IsTrue(durg.IsTrainingHero, "The hero track must start independently, not be blocked by the unique-unit queue.");
        }

        // --- MatchManager Regicide ---

        [Test]
        public void EvaluateSkirmishOutcome_RegicideDisabled_DeadTrainedHeroDoesNotAffectOutcome()
        {
            GameSettings.RegicideEnabled = false;
            CreateUnit(FactionId.Player);
            CreateUnit(FactionId.Enemy);
            HeroProgress.MarkTrained(FactionId.Player); // trained, but not alive - Regicide is off, so this must not matter

            Assert.AreEqual(MatchOutcome.Ongoing, MatchManager.EvaluateSkirmishOutcome(timeLimitReached: false));
        }

        [Test]
        public void EvaluateSkirmishOutcome_RegicideEnabled_PlayerHeroDead_IsDefeat()
        {
            GameSettings.RegicideEnabled = true;
            CreateUnit(FactionId.Player); // Player still has forces - Regicide must still end the match
            CreateUnit(FactionId.Enemy);
            HeroProgress.MarkTrained(FactionId.Player);

            Assert.AreEqual(MatchOutcome.Defeat, MatchManager.EvaluateSkirmishOutcome(timeLimitReached: false),
                "A trained-but-no-longer-alive Player hero must end the match even while the Player's army is still standing.");
        }

        [Test]
        public void EvaluateSkirmishOutcome_RegicideEnabled_AllTrainedHostileHeroesDead_IsVictory()
        {
            GameSettings.RegicideEnabled = true;
            CreateUnit(FactionId.Player);
            CreateUnit(FactionId.Enemy); // Enemy still has forces - Regicide must still end the match
            HeroProgress.MarkTrained(FactionId.Enemy);

            Assert.AreEqual(MatchOutcome.Victory, MatchManager.EvaluateSkirmishOutcome(timeLimitReached: false));
        }

        [Test]
        public void EvaluateSkirmishOutcome_RegicideEnabled_NoFactionEverTrainedHero_FallsThroughToElimination()
        {
            GameSettings.RegicideEnabled = true;
            CreateUnit(FactionId.Player);
            CreateUnit(FactionId.Enemy);

            Assert.AreEqual(MatchOutcome.Ongoing, MatchManager.EvaluateSkirmishOutcome(timeLimitReached: false),
                "Regicide must not spuriously decide the match for a faction that never trained a hero.");
        }

        [Test]
        public void EvaluateSkirmishOutcome_RegicideEnabled_HostileHeroAliveWithForces_StaysOngoing()
        {
            GameSettings.RegicideEnabled = true;
            CreateUnit(FactionId.Player);
            CreateUnit(FactionId.Enemy); // non-hero forces, keeps plain elimination from also granting Victory
            HeroProgress.MarkTrained(FactionId.Enemy);
            CreateHero(FactionId.Enemy);

            Assert.AreEqual(MatchOutcome.Ongoing, MatchManager.EvaluateSkirmishOutcome(timeLimitReached: false),
                "A hostile faction's still-alive trained hero must not trigger Regicide Victory.");
        }
    }
}
