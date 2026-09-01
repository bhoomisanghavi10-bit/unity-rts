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
    // AoE-parity Phase 2.3 (Roadmap Section 1): Siege previously dealt
    // single-target damage only, so Line vs. Staggered/Flank/Skirmish
    // formations were cosmetic against it - nothing in combat resolution
    // read unit spacing. MeleeAttacker.SetSplashRadius (Siege-only; every
    // other unit keeps the default 0/disabled) makes a hit also damage
    // nearby hostiles around the primary target's position. Drives
    // MeleeAttacker directly via its internal Tick(deltaTime), same
    // convention as BuildingAttackerTests - Update() itself depends on
    // Time.deltaTime, which EditMode tests don't naturally advance.
    public class SiegeSplashTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();
        private static readonly Regex SetDestinationErrorPattern = new Regex("SetDestination.*NavMesh");
        // Attackable.TakeDamage unconditionally spawns a VfxFactory particle
        // burst (stopAction: Destroy) - harmless at real runtime, but Unity's
        // Editor logs this once per hit outside Play mode (same situation
        // BuildingAttackerTests documents). Unlike that test, this file also
        // uses LogAssert.Expect for the SetDestination error below, and once
        // a test uses Expect at all, ignoreFailingMessages alone no longer
        // suppresses other unexpected errors in this Unity Test Framework
        // version - each VFX destroy log needs its own explicit Expect too,
        // once per hit the test causes.
        private static readonly Regex VfxDestroyErrorPattern = new Regex("Destroy may not be called from edit mode");

        [SetUp]
        public void SetUp()
        {
            // AttackMove -> UnitMover.MoveTo -> NavMeshAgent.SetDestination
            // logs an Editor error here (no baked NavMesh in an EditMode
            // test scene) even though the call itself doesn't throw - same
            // situation GathererCombatResponseTests documents for its own
            // SetDestination calls. Each test that calls AttackMove expects
            // it explicitly via ExpectSetDestinationError().
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
                // Unit.All/Building.All registration normally happens via
                // OnEnable/OnDisable, but Unity's Editor doesn't guarantee
                // those fire synchronously within a single test method -
                // same gotcha BuildingAttackerTests documents. Removed
                // directly here instead.
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

        private MeleeAttacker CreateSiegeAttacker(FactionId faction, float splashRadius)
        {
            GameObject go = CreateGameObject("Siege");
            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<Attackable>().ConfigureClass(UnitClass.Siege);
            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage(10f);
            attacker.SetRange(3f);
            attacker.SetUnitClass(UnitClass.Siege);
            attacker.SetSplashRadius(splashRadius);
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
        public void Tick_SplashDamage_HitsHostileInRadius_MissesHostileOutsideRadius()
        {
            MeleeAttacker attacker = CreateSiegeAttacker(FactionId.Player, splashRadius: 2.25f);
            Attackable primaryTarget = CreateHostileUnit(FactionId.Enemy, Vector3.zero);
            Attackable inRadius = CreateHostileUnit(FactionId.Enemy, new Vector3(2f, 0f, 0f));
            Attackable outOfRadius = CreateHostileUnit(FactionId.Enemy, new Vector3(5f, 0f, 0f));

            AttackAndTick(attacker, primaryTarget, expectedHitCount: 2);

            Assert.Less(primaryTarget.Health, 1000f, "Primary target should take damage.");
            Assert.Less(inRadius.Health, 1000f, "Hostile inside splash radius should take damage.");
            Assert.AreEqual(1000f, outOfRadius.Health, "Hostile outside splash radius should be untouched.");
        }

        [Test]
        public void Tick_SplashDamage_DoesNotDamageFriendlyInRadius()
        {
            MeleeAttacker attacker = CreateSiegeAttacker(FactionId.Player, splashRadius: 2.25f);
            Attackable primaryTarget = CreateHostileUnit(FactionId.Enemy, Vector3.zero);
            Attackable friendly = CreateHostileUnit(FactionId.Player, new Vector3(1f, 0f, 0f));

            AttackAndTick(attacker, primaryTarget);

            Assert.AreEqual(1000f, friendly.Health, "A friendly (same-faction) unit must never take splash damage.");
        }

        [Test]
        public void Tick_SplashDamage_OnBuildingVictim_AppliesSiegeCombatBonus()
        {
            MeleeAttacker attacker = CreateSiegeAttacker(FactionId.Player, splashRadius: 2.25f);
            Attackable primaryTarget = CreateHostileUnit(FactionId.Enemy, Vector3.zero);
            Attackable building = CreateHostileBuilding(FactionId.Enemy, new Vector3(1.5f, 0f, 0f));

            AttackAndTick(attacker, primaryTarget, expectedHitCount: 2);

            // 10 base damage * 3x Siege->Building CombatBonus = 30 (no
            // armor configured on the test building, so full damage lands).
            Assert.AreEqual(1000f - 30f, building.Health, 0.01f,
                "Splash onto a Building-class victim should get the same CombatBonus a primary-target hit would.");
        }

        [Test]
        public void Tick_WithZeroSplashRadius_DoesNotDamageSecondHostile()
        {
            // Default splashRadius (0/disabled) - every non-Siege
            // MeleeAttacker user (Soldier/Archer/Cavalry/Spearman/Worker)
            // must be completely unaffected by this feature.
            GameObject go = CreateGameObject("Soldier");
            go.AddComponent<FactionMember>().Configure(FactionId.Player);
            go.AddComponent<Attackable>().ConfigureClass(UnitClass.Infantry);
            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage(10f);
            attacker.SetRange(3f);

            Attackable primaryTarget = CreateHostileUnit(FactionId.Enemy, Vector3.zero);
            Attackable nearby = CreateHostileUnit(FactionId.Enemy, new Vector3(0.5f, 0f, 0f));

            AttackAndTick(attacker, primaryTarget);

            Assert.Less(primaryTarget.Health, 1000f, "Primary target should still take damage.");
            Assert.AreEqual(1000f, nearby.Health, "With splash disabled (default), only the primary target should be hit.");
        }
    }
}
