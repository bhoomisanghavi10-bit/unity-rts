using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomsOfBharat.Combat;

namespace KingdomsOfBharat.Tests
{
    // Wave 0 item 3 (docs/IMPLEMENTATION_ROADMAP.md): confirm Attackable's
    // "every hit does at least 1 damage" floor (Attackable.cs:160,
    // Mathf.Max(1f, amount - armor)) actually holds once a caller has
    // already applied a CounterMatrix/CombatBonus multiplier below 1.0 to
    // the raw damage before it reaches TakeDamage - every real call site
    // (MeleeAttacker.ResolveHit, BoatAttacker.Tick, BuildingAttacker.Tick)
    // passes baseDamage * bonus, already-multiplied, into TakeDamage, so
    // this drives TakeDamage the same way: with a final post-multiplier
    // amount, not a raw pre-multiplier one.
    public class DamageClampTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();
        private static readonly Regex VfxDestroyErrorPattern = new Regex("Destroy may not be called from edit mode");

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
        }

        // Every non-lethal TakeDamage call fires a hit-burst VFX whose
        // particle-system cleanup logs an Editor-only "Destroy may not be
        // called from edit mode" error - LogAssert.ignoreFailingMessages
        // alone doesn't suppress it in this Unity Test Framework version
        // (same gotcha RetroactiveUpgradeTests/SiegeSplashTests already
        // document), so each hit needs its own explicit Expect.
        private void ExpectVfxDestroyError()
        {
            LogAssert.Expect(LogType.Error, VfxDestroyErrorPattern);
        }

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
        }

        private GameObject CreateGameObject(string name)
        {
            var go = new GameObject(name);
            _spawned.Add(go);
            return go;
        }

        private Attackable CreateTarget(float meleeArmor, float pierceArmor)
        {
            GameObject go = CreateGameObject("Target");
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure(1000f);
            attackable.ConfigureArmor(meleeArmor, pierceArmor);
            attackable.ConfigureClass(UnitClass.Infantry);
            return attackable;
        }

        [Test]
        public void TakeDamage_ArmorExceedsPostMultiplierDamage_StillDealsExactlyOne()
        {
            // Simulates a hard-countered attacker (e.g. a 0.5x CombatBonus
            // matchup) hitting a heavily-armored target: raw damage 10,
            // multiplier 0.5 -> 5 arrives at TakeDamage, armor 20 would
            // otherwise floor this at/below zero.
            Attackable target = CreateTarget(meleeArmor: 20f, pierceArmor: 0f);

            float postMultiplierDamage = 10f * 0.5f;
            ExpectVfxDestroyError();
            target.TakeDamage(postMultiplierDamage, KingdomsOfBharat.Combat.DamageType.Melee);

            Assert.AreEqual(999f, target.Health, 0.001f,
                "armor stacked with a sub-1.0 counter multiplier must still floor at 1 damage, never 0");
        }

        [Test]
        public void TakeDamage_ArmorFarExceedsDamage_NeverGoesNegativeOrZero()
        {
            // Extreme case: armor an order of magnitude above the incoming
            // hit (e.g. a maxed-out Blacksmith-style armor stack). The
            // clamp must still guarantee a nonzero, non-negative result.
            Attackable target = CreateTarget(meleeArmor: 500f, pierceArmor: 500f);

            ExpectVfxDestroyError();
            target.TakeDamage(1f, KingdomsOfBharat.Combat.DamageType.Melee);
            ExpectVfxDestroyError();
            target.TakeDamage(1f, KingdomsOfBharat.Combat.DamageType.Pierce);

            Assert.AreEqual(998f, target.Health, 0.001f,
                "each hit must still deal exactly the 1-damage floor regardless of how far armor exceeds it");
        }

        [Test]
        public void TakeDamage_DamageExceedsArmor_ClampDoesNotDistortNormalDamage()
        {
            // Regression guard: the floor must only kick in when
            // (amount - armor) would fall below 1 - normal hits where
            // armor is legitimately overcome should be unaffected.
            Attackable target = CreateTarget(meleeArmor: 3f, pierceArmor: 0f);

            ExpectVfxDestroyError();
            target.TakeDamage(10f, KingdomsOfBharat.Combat.DamageType.Melee);

            Assert.AreEqual(993f, target.Health, 0.001f,
                "a hit that legitimately beats armor should deal amount - armor, not the 1-damage floor");
        }

        [Test]
        public void TakeDamage_ClampedHits_AccumulateTowardZero_NeverStall()
        {
            // The floor exists specifically so a unit can never become
            // mathematically unkillable - even with armor massively
            // exceeding every incoming hit, repeated clamped 1-damage hits
            // must still whittle health down instead of stalling forever.
            Attackable target = CreateTarget(meleeArmor: 999f, pierceArmor: 999f);
            target.Configure(3f);

            ExpectVfxDestroyError();
            target.TakeDamage(1f, KingdomsOfBharat.Combat.DamageType.Melee);
            Assert.AreEqual(2f, target.Health, 0.001f);

            ExpectVfxDestroyError();
            target.TakeDamage(1f, KingdomsOfBharat.Combat.DamageType.Pierce);
            Assert.AreEqual(1f, target.Health, 0.001f);
        }
    }
}
