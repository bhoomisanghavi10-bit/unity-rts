using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Tests
{
    // Wave 4 item 23 (Scorpion): MeleeAttacker.SetPierceThrough - a hit
    // that continues past the primary target in a straight line, damaging
    // anything else hostile standing behind it at full damage. Mirrors
    // SiegeSplashTests' own structure/gotchas exactly (drives MeleeAttacker
    // directly via its internal Tick(deltaTime), expects the same
    // SetDestination/VFX-destroy Editor-only log noise per hit) - the
    // geometry under test is a line, not a radius, so the fixture positions
    // hostiles ahead-in-line, behind-in-line-but-past-depth, and off to the
    // side instead of near/far from a single impact point.
    public class ScorpionPierceThroughTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();
        private static readonly Regex SetDestinationErrorPattern = new Regex("SetDestination.*NavMesh");
        private static readonly Regex VfxDestroyErrorPattern = new Regex("Destroy may not be called from edit mode");

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
        }

        private static void ExpectSetDestinationError()
        {
            LogAssert.Expect(LogType.Error, SetDestinationErrorPattern);
        }

        private static void ExpectVfxDestroyErrors(int hitCount)
        {
            for (int i = 0; i < hitCount; i++)
            {
                LogAssert.Expect(LogType.Error, VfxDestroyErrorPattern);
            }
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
                if (go.TryGetComponent(out Building building))
                {
                    Building.All.Remove(building);
                }
                Object.DestroyImmediate(go);
            }
            _spawned.Clear();
        }

        private GameObject CreateGameObject(string name)
        {
            var go = new GameObject(name);
            _spawned.Add(go);
            return go;
        }

        private MeleeAttacker CreateScorpionAttacker(FactionId faction, float pierceDepth)
        {
            GameObject go = CreateGameObject("Scorpion");
            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<Attackable>().ConfigureClass(UnitClass.Scorpion);
            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage(10f);
            attacker.SetRange(6f);
            attacker.SetUnitClass(UnitClass.Scorpion);
            attacker.SetPierceThrough(pierceDepth);
            return attacker;
        }

        private Attackable CreateHostileUnit(FactionId faction, Vector3 position, UnitClass unitClass = UnitClass.Infantry)
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
            attackable.ConfigureClass(unitClass);
            return attackable;
        }

        private Attackable CreateHostileBuilding(FactionId faction, Vector3 position)
        {
            GameObject go = CreateGameObject("HostileBuilding");
            go.transform.position = position;
            go.AddComponent<FactionMember>().Configure(faction);
            Building building = go.AddComponent<Building>();
            if (!Building.All.Contains(building))
            {
                Building.All.Add(building);
            }
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure(1000f);
            attackable.ConfigureClass(UnitClass.Building);
            return attackable;
        }

        private void AttackAndTick(MeleeAttacker attacker, Attackable primaryTarget, int expectedHitCount = 1)
        {
            ExpectSetDestinationError();
            attacker.AttackMove(primaryTarget);
            ExpectVfxDestroyErrors(expectedHitCount);
            attacker.Tick(1f);
        }

        [Test]
        public void Tick_PierceThrough_HitsHostileBehindTarget_MissesHostilePastDepth()
        {
            // Attacker at origin, primary target 3 units down +Z, so the
            // line of fire runs along +Z.
            MeleeAttacker attacker = CreateScorpionAttacker(FactionId.Player, pierceDepth: 3f);
            Attackable primaryTarget = CreateHostileUnit(FactionId.Enemy, new Vector3(0f, 0f, 3f));
            Attackable behindInDepth = CreateHostileUnit(FactionId.Enemy, new Vector3(0f, 0f, 5f));
            Attackable behindPastDepth = CreateHostileUnit(FactionId.Enemy, new Vector3(0f, 0f, 8f));

            AttackAndTick(attacker, primaryTarget, expectedHitCount: 2);

            Assert.Less(primaryTarget.Health, 1000f, "Primary target should take damage.");
            Assert.Less(behindInDepth.Health, 1000f, "Hostile within pierce-through depth, in line, should take damage.");
            Assert.AreEqual(1000f, behindPastDepth.Health, "Hostile beyond pierce-through depth should be untouched.");
        }

        [Test]
        public void Tick_PierceThrough_MissesHostileOffToTheSide()
        {
            MeleeAttacker attacker = CreateScorpionAttacker(FactionId.Player, pierceDepth: 3f);
            Attackable primaryTarget = CreateHostileUnit(FactionId.Enemy, new Vector3(0f, 0f, 3f));
            Attackable offToSide = CreateHostileUnit(FactionId.Enemy, new Vector3(3f, 0f, 3f));

            AttackAndTick(attacker, primaryTarget);

            Assert.AreEqual(1000f, offToSide.Health, "A hostile off to the side of the bolt's line, not behind the target, must be untouched.");
        }

        [Test]
        public void Tick_PierceThrough_MissesHostileInFrontOfTarget()
        {
            // A hostile standing between the attacker and the primary
            // target (i.e. closer than the target, not behind it) should
            // not be hit - only the primary target and whatever is behind
            // it in the bolt's continued path.
            MeleeAttacker attacker = CreateScorpionAttacker(FactionId.Player, pierceDepth: 3f);
            Attackable inFront = CreateHostileUnit(FactionId.Enemy, new Vector3(0f, 0f, 1f));
            Attackable primaryTarget = CreateHostileUnit(FactionId.Enemy, new Vector3(0f, 0f, 3f));

            AttackAndTick(attacker, primaryTarget);

            Assert.AreEqual(1000f, inFront.Health, "A hostile standing in front of (closer than) the primary target should not be hit by the pierce-through.");
        }

        [Test]
        public void Tick_PierceThrough_DoesNotDamageFriendlyBehindTarget()
        {
            MeleeAttacker attacker = CreateScorpionAttacker(FactionId.Player, pierceDepth: 3f);
            Attackable primaryTarget = CreateHostileUnit(FactionId.Enemy, new Vector3(0f, 0f, 3f));
            Attackable friendly = CreateHostileUnit(FactionId.Player, new Vector3(0f, 0f, 5f));

            AttackAndTick(attacker, primaryTarget);

            Assert.AreEqual(1000f, friendly.Health, "A friendly (same-faction) unit must never take pierce-through damage.");
        }

        [Test]
        public void Tick_PierceThrough_OnBuildingVictim_AppliesScorpionCombatBonus()
        {
            MeleeAttacker attacker = CreateScorpionAttacker(FactionId.Player, pierceDepth: 3f);
            Attackable primaryTarget = CreateHostileUnit(FactionId.Enemy, new Vector3(0f, 0f, 3f));
            Attackable building = CreateHostileBuilding(FactionId.Enemy, new Vector3(0f, 0f, 5f));

            AttackAndTick(attacker, primaryTarget, expectedHitCount: 2);

            // 10 base damage * 1x (Scorpion carries no CombatBonus entry vs
            // Building, so the default applies) = 10 (no armor configured
            // on the test building, so full damage lands). Confirms the
            // pierced victim still goes through the same ResolveHit/
            // CombatBonus path a primary-target hit would, just at full
            // (not splash-reduced) multiplier.
            Assert.AreEqual(1000f - 10f, building.Health, 0.01f,
                "Pierce-through onto a Building-class victim should still resolve through CombatBonus, at full damage.");
        }

        [Test]
        public void Tick_PierceThrough_FullDamage_NotReducedLikeSplash()
        {
            MeleeAttacker attacker = CreateScorpionAttacker(FactionId.Player, pierceDepth: 3f);
            Attackable primaryTarget = CreateHostileUnit(FactionId.Enemy, new Vector3(0f, 0f, 3f));
            Attackable pierced = CreateHostileUnit(FactionId.Enemy, new Vector3(0f, 0f, 5f));

            AttackAndTick(attacker, primaryTarget, expectedHitCount: 2);

            // 10 base damage * 2x Scorpion->Infantry CombatBonus = 20 (no
            // armor configured on the test unit, so full damage lands) -
            // the same multiplier a primary-target hit would get, at full
            // (not splash-reduced) strength.
            Assert.AreEqual(1000f - 20f, pierced.Health, 0.01f,
                "A pierced Infantry-class victim should take the same full damage (base * CombatBonus) a primary hit would, not a reduced splash-style amount.");
        }

        [Test]
        public void Tick_WithZeroPierceDepth_DoesNotDamageSecondHostile()
        {
            // Default pierceThroughDepth (0/disabled) - every non-Scorpion
            // MeleeAttacker user (Soldier/Archer/Cavalry/Spearman/Siege/
            // Worker) must be completely unaffected by this feature.
            GameObject go = CreateGameObject("Soldier");
            go.AddComponent<FactionMember>().Configure(FactionId.Player);
            go.AddComponent<Attackable>().ConfigureClass(UnitClass.Infantry);
            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage(10f);
            attacker.SetRange(6f);

            Attackable primaryTarget = CreateHostileUnit(FactionId.Enemy, new Vector3(0f, 0f, 3f));
            Attackable behind = CreateHostileUnit(FactionId.Enemy, new Vector3(0f, 0f, 5f));

            AttackAndTick(attacker, primaryTarget);

            Assert.Less(primaryTarget.Health, 1000f, "Primary target should still take damage.");
            Assert.AreEqual(1000f, behind.Health, "With pierce-through disabled (default), only the primary target should be hit.");
        }
    }
}
