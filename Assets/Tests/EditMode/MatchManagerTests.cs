using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Match;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Tests
{
    // Item 2 (Victory Conditions, docs/PARTIAL_ELEMENTS_FIX_PLAN.md). Exercises
    // MatchManager.EvaluateSkirmishOutcome/ResolveTimeLimitOutcome directly -
    // the testable seam extracted from the Update()-driven Evaluate() (same
    // convention as ConstructionSite.internal Tick/CommandBus.internal
    // EnqueueAt) - rather than trying to drive SimClock/Time.unscaledTime,
    // neither of which behaves usefully in EditMode. Conquest elimination
    // itself (MatchManager.FactionHasForces) predates this item and had zero
    // coverage before now either.
    public class MatchManagerTests
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

                // Unit.All registration/deregistration normally happens via
                // OnEnable/OnDisable, but Unity's Editor doesn't guarantee
                // those fire synchronously within a single test method (same
                // gotcha CommandBusDeterminismTests/BuildingAttackerTests
                // document) - removed directly here instead.
                if (go.TryGetComponent(out Unit unit))
                {
                    Unit.All.Remove(unit);
                }

                Object.DestroyImmediate(go);
            }

            _spawned.Clear();
            DiplomacyRegistry.Reset();
        }

        private GameObject CreateUnit(string name, FactionId faction)
        {
            var go = new GameObject(name);
            Unit unit = go.AddComponent<Unit>();
            if (!Unit.All.Contains(unit))
            {
                Unit.All.Add(unit);
            }

            go.AddComponent<FactionMember>().Configure(faction);
            _spawned.Add(go);
            return go;
        }

        [Test]
        public void EvaluateSkirmishOutcome_PlayerHasNoForces_IsDefeat()
        {
            CreateUnit("Enemy", FactionId.Enemy);

            Assert.AreEqual(MatchOutcome.Defeat, MatchManager.EvaluateSkirmishOutcome(timeLimitReached: false),
                "A Player with zero units/buildings must lose, regardless of the time limit.");
            Assert.AreEqual(MatchOutcome.Defeat, MatchManager.EvaluateSkirmishOutcome(timeLimitReached: true),
                "Defeat-by-elimination must take priority over (or coincide correctly with) a reached time limit.");
        }

        [Test]
        public void EvaluateSkirmishOutcome_AllHostilesEliminated_IsVictory()
        {
            CreateUnit("Player", FactionId.Player);

            Assert.AreEqual(MatchOutcome.Victory, MatchManager.EvaluateSkirmishOutcome(timeLimitReached: false),
                "Every hostile faction (Enemy, Enemy2) already has zero units/buildings - this must be Victory even before any time limit.");
        }

        [Test]
        public void EvaluateSkirmishOutcome_AlliedFactionSurviving_DoesNotBlockVictory()
        {
            CreateUnit("Player", FactionId.Player);
            CreateUnit("Enemy", FactionId.Enemy);
            DiplomacyRegistry.SetAllied(FactionId.Player, FactionId.Enemy, true);

            Assert.AreEqual(MatchOutcome.Victory, MatchManager.EvaluateSkirmishOutcome(timeLimitReached: false),
                "An allied faction surviving must not block Victory - only non-allied (hostile) factions need eliminating.");
        }

        [Test]
        public void EvaluateSkirmishOutcome_BothSidesAlive_TimeLimitNotReached_IsOngoing()
        {
            CreateUnit("Player", FactionId.Player);
            CreateUnit("Enemy", FactionId.Enemy);

            Assert.AreEqual(MatchOutcome.Ongoing, MatchManager.EvaluateSkirmishOutcome(timeLimitReached: false),
                "With both sides alive and no time limit reached, the match must stay Ongoing.");
        }

        [Test]
        public void EvaluateSkirmishOutcome_TimeLimitReached_FallsThroughToTiebreaker()
        {
            CreateUnit("Player", FactionId.Player);
            CreateUnit("Player2", FactionId.Player);
            CreateUnit("Enemy", FactionId.Enemy);

            Assert.AreEqual(MatchOutcome.Victory, MatchManager.EvaluateSkirmishOutcome(timeLimitReached: true),
                "Neither side is eliminated, but the time limit is reached - the tiebreaker (population) must resolve it: Player 2 vs Enemy 1.");
        }

        [Test]
        public void ResolveTimeLimitOutcome_PlayerSideAhead_IsVictory()
        {
            CreateUnit("Player1", FactionId.Player);
            CreateUnit("Player2", FactionId.Player);
            CreateUnit("Enemy", FactionId.Enemy);

            Assert.AreEqual(MatchOutcome.Victory, MatchManager.ResolveTimeLimitOutcome());
        }

        [Test]
        public void ResolveTimeLimitOutcome_HostileSideAhead_IsDefeat()
        {
            CreateUnit("Player", FactionId.Player);
            CreateUnit("Enemy1", FactionId.Enemy);
            CreateUnit("Enemy2", FactionId.Enemy);

            Assert.AreEqual(MatchOutcome.Defeat, MatchManager.ResolveTimeLimitOutcome());
        }

        [Test]
        public void ResolveTimeLimitOutcome_TiedPopulation_IsDraw()
        {
            CreateUnit("Player", FactionId.Player);
            CreateUnit("Enemy", FactionId.Enemy);

            Assert.AreEqual(MatchOutcome.Draw, MatchManager.ResolveTimeLimitOutcome(),
                "Equal population on both sides must be a Draw, not an arbitrary Victory/Defeat.");
        }

        [Test]
        public void ResolveTimeLimitOutcome_AlliedFactionPopulation_CountsTowardPlayerSide()
        {
            // Player(1) + allied Enemy(1) = 2 vs. hostile Enemy2(1) = 1:
            // the alliance must tip this to Victory, not a false Draw from
            // comparing Player's 1 against Enemy's 1 in isolation.
            CreateUnit("Player", FactionId.Player);
            CreateUnit("AlliedEnemy", FactionId.Enemy);
            CreateUnit("HostileEnemy2", FactionId.Enemy2);
            DiplomacyRegistry.SetAllied(FactionId.Player, FactionId.Enemy, true);

            Assert.AreEqual(MatchOutcome.Victory, MatchManager.ResolveTimeLimitOutcome());
        }
    }
}
