using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Tests
{
    // Item 3 (Area of Effect / Trample, docs/PARTIAL_ELEMENTS_FIX_PLAN.md). Cavalry
    // trample reuses MeleeAttacker.SetSplashRadius (the same mechanism
    // SiegeSplashTests.cs already covers for Siege) but with the new
    // damageMultiplier parameter this item added, so splash victims take
    // reduced (35%) damage instead of a second full hit - a deliberately
    // "minor" secondary effect, not Siege-tier splash. Same test structure/
    // helpers as SiegeSplashTests.cs (drives MeleeAttacker.internal
    // Tick(deltaTime) directly, same EditMode-only SetDestination/VFX-destroy
    // log handling).
    public class CavalryTrampleTests
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

        // Mirrors CavalryFactory's own wiring: base damage 10, Cavalry
        // class, splash radius 1.25 / 35% secondary damage.
        private MeleeAttacker CreateCavalryAttacker(FactionId faction)
        {
            GameObject go = CreateGameObject("Cavalry");
            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<Attackable>().ConfigureClass(UnitClass.Cavalry);
            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage(10f);
            attacker.SetRange(1f);
            attacker.SetUnitClass(UnitClass.Cavalry);
            attacker.SetSplashRadius(1.25f, 0.35f);
            return attacker;
        }

        private Attackable CreateHostileInfantry(FactionId faction, Vector3 position)
        {
            GameObject go = CreateGameObject("HostileInfantry");
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

        private void AttackAndTick(MeleeAttacker attacker, Attackable primaryTarget, int expectedHitCount = 1)
        {
            ExpectSetDestinationError();
            attacker.AttackMove(primaryTarget);
            ExpectVfxDestroyErrors(expectedHitCount);
            attacker.Tick(1f);
        }

        [Test]
        public void Tick_PrimaryTarget_TakesFullDamage()
        {
            MeleeAttacker attacker = CreateCavalryAttacker(FactionId.Player);
            Attackable primaryTarget = CreateHostileInfantry(FactionId.Enemy, Vector3.zero);

            AttackAndTick(attacker, primaryTarget);

            // 10 base damage * 1.5x Cavalry->Infantry CombatBonus = 15 (no
            // armor configured on the test target, so full damage lands).
            Assert.AreEqual(1000f - 15f, primaryTarget.Health, 0.01f,
                "The primary target must take the normal, unreduced hit - only splash victims get the trample multiplier.");
        }

        [Test]
        public void Tick_TrampleDamage_HitsHostileInRadius_AtReducedDamage()
        {
            MeleeAttacker attacker = CreateCavalryAttacker(FactionId.Player);
            Attackable primaryTarget = CreateHostileInfantry(FactionId.Enemy, Vector3.zero);
            Attackable trampled = CreateHostileInfantry(FactionId.Enemy, new Vector3(1f, 0f, 0f));

            AttackAndTick(attacker, primaryTarget, expectedHitCount: 2);

            // 10 base damage * 1.5x CombatBonus * 0.35 trample multiplier = 5.25.
            Assert.AreEqual(1000f - 5.25f, trampled.Health, 0.01f,
                "A hostile clumped within the trample radius should take 35% of a full hit, not a second full hit.");
        }

        [Test]
        public void Tick_TrampleDamage_MissesHostileOutsideRadius()
        {
            MeleeAttacker attacker = CreateCavalryAttacker(FactionId.Player);
            Attackable primaryTarget = CreateHostileInfantry(FactionId.Enemy, Vector3.zero);
            Attackable outOfRadius = CreateHostileInfantry(FactionId.Enemy, new Vector3(3f, 0f, 0f));

            AttackAndTick(attacker, primaryTarget);

            Assert.AreEqual(1000f, outOfRadius.Health,
                "A hostile standing well clear of the impact point (beyond the 1.25-unit trample radius) must be untouched.");
        }

        [Test]
        public void Tick_TrampleDamage_DoesNotDamageFriendlyInRadius()
        {
            MeleeAttacker attacker = CreateCavalryAttacker(FactionId.Player);
            Attackable primaryTarget = CreateHostileInfantry(FactionId.Enemy, Vector3.zero);
            Attackable friendly = CreateHostileInfantry(FactionId.Player, new Vector3(1f, 0f, 0f));

            AttackAndTick(attacker, primaryTarget);

            Assert.AreEqual(1000f, friendly.Health, "A friendly (same-faction) unit must never take trample damage.");
        }
    }
}
