using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Tests
{
    // Wave 0 item 2 (docs/IMPLEMENTATION_ROADMAP.md): UpgradeProgress's own
    // bonuses used to be read once, at spawn, and baked into the unit's
    // components - "researching a tier only benefits units trained after."
    // AoE's real Blacksmith Attack/Armor upgrades apply to units already on
    // the field the moment research completes. These tests drive Attackable/
    // MeleeAttacker directly (same convention as SiegeSplashTests -
    // internal Tick(deltaTime) instead of depending on Unity's Update loop)
    // and assert the SAME already-constructed instance changes behavior
    // after UpgradeProgress advances, with no respawn - that's the actual
    // bug being fixed, not just "does the formula compute the right number."
    public class RetroactiveUpgradeTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();
        private static readonly Regex SetDestinationErrorPattern = new Regex("SetDestination.*NavMesh");
        private static readonly Regex VfxDestroyErrorPattern = new Regex("Destroy may not be called from edit mode");

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            // UpgradeProgress is static (persists for the whole Test Runner
            // domain) - isolate each test from tiers a previous test left
            // behind.
            UpgradeProgress.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            UpgradeProgress.ResetForTests();
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

        private Attackable CreateTarget(FactionId faction, float meleeArmor = 0f, float pierceArmor = 0f)
        {
            GameObject go = CreateGameObject("Target");
            go.AddComponent<FactionMember>().Configure(faction);
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure(1000f);
            attackable.ConfigureArmor(meleeArmor, pierceArmor);
            attackable.ConfigureClass(UnitClass.Infantry);
            return attackable;
        }

        private void ExpectSetDestinationError()
        {
            LogAssert.Expect(LogType.Error, SetDestinationErrorPattern);
        }

        private void ExpectVfxDestroyErrors(int hitCount)
        {
            for (int i = 0; i < hitCount; i++)
            {
                LogAssert.Expect(LogType.Error, VfxDestroyErrorPattern);
            }
        }

        [Test]
        public void TakeDamage_AlreadySpawnedUnit_GetsMoreArmorAfterResearchCompletes()
        {
            // A Soldier-shaped Attackable: base armor 0/0 (matching
            // SoldierFactory's own def-based fallback), upgrade scaling
            // enabled the way SoldierFactory now calls it.
            Attackable target = CreateTarget(FactionId.Player);
            target.EnableUpgradeArmorScaling();

            ExpectVfxDestroyErrors(2);
            target.TakeDamage(10f, KingdomsOfBharat.Combat.DamageType.Melee);
            Assert.AreEqual(990f, target.Health, 0.01f, "Pre-research hit: no armor bonus yet, full 10 damage lands.");

            UpgradeProgress.AdvanceArmor(FactionId.Player);

            target.TakeDamage(10f, KingdomsOfBharat.Combat.DamageType.Melee);
            // ArmorPerTier = 1f, one tier researched -> +1 armor -> 9 damage.
            Assert.AreEqual(990f - 9f, target.Health, 0.01f,
                "The SAME already-spawned Attackable should take less damage once armor research completes - no respawn required.");
        }

        [Test]
        public void Tick_AlreadySpawnedAttacker_DealsMoreDamageAfterResearchCompletes()
        {
            GameObject attackerGo = CreateGameObject("Soldier");
            attackerGo.AddComponent<FactionMember>().Configure(FactionId.Player);
            attackerGo.AddComponent<Attackable>().ConfigureClass(UnitClass.Infantry);
            var attacker = attackerGo.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage(10f);
            attacker.SetRange(3f);
            attacker.SetUnitClass(UnitClass.Infantry);
            attacker.EnableUpgradeDamageScaling();

            Attackable target = CreateTarget(FactionId.Enemy);

            ExpectSetDestinationError();
            attacker.AttackMove(target);
            ExpectVfxDestroyErrors(1);
            attacker.Tick(0.01f);
            Assert.AreEqual(990f, target.Health, 0.01f, "Pre-research hit: no damage bonus yet, full 10 damage lands.");

            UpgradeProgress.AdvanceAttack(FactionId.Player);

            ExpectVfxDestroyErrors(1);
            attacker.Tick(1f);
            // DamagePerTier = 2f, one tier researched -> +2 damage -> 12.
            Assert.AreEqual(990f - 12f, target.Health, 0.01f,
                "The SAME already-spawned MeleeAttacker should deal more damage once attack research completes - no respawn required.");
        }

        [Test]
        public void TakeDamage_UnitWithoutUpgradeScaling_IsUnaffectedByResearch()
        {
            // A Worker/building-shaped Attackable: never opts in, matching
            // WorkerFactory/every building factory today.
            Attackable target = CreateTarget(FactionId.Player);

            UpgradeProgress.AdvanceArmor(FactionId.Player);
            UpgradeProgress.AdvanceArmor(FactionId.Player);

            ExpectVfxDestroyErrors(1);
            target.TakeDamage(10f, KingdomsOfBharat.Combat.DamageType.Melee);
            Assert.AreEqual(990f, target.Health, 0.01f,
                "An Attackable that never called EnableUpgradeArmorScaling must not silently start benefiting from Blacksmith-style research.");
        }

        [Test]
        public void TakeDamage_UnitWithoutUpgradeDamageScaling_DealsUnaffectedDamage()
        {
            GameObject attackerGo = CreateGameObject("Worker");
            attackerGo.AddComponent<FactionMember>().Configure(FactionId.Player);
            attackerGo.AddComponent<Attackable>().ConfigureClass(UnitClass.Infantry);
            var attacker = attackerGo.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage(2f);
            attacker.SetRange(3f);
            attacker.SetUnitClass(UnitClass.Infantry);
            // No EnableUpgradeDamageScaling() - matches WorkerFactory.

            Attackable target = CreateTarget(FactionId.Enemy);

            UpgradeProgress.AdvanceAttack(FactionId.Player);

            ExpectSetDestinationError();
            attacker.AttackMove(target);
            ExpectVfxDestroyErrors(1);
            attacker.Tick(0.01f);

            Assert.AreEqual(998f, target.Health, 0.01f,
                "A MeleeAttacker that never called EnableUpgradeDamageScaling must not silently start benefiting from Blacksmith-style research.");
        }

        [Test]
        public void TakeDamage_ArcherArmorScaling_AppliesOnlyToPierceArmor()
        {
            // Matches ArcherFactory.EnableUpgradeArmorScaling(melee: false, pierce: true).
            Attackable target = CreateTarget(FactionId.Player);
            target.EnableUpgradeArmorScaling(melee: false, pierce: true);

            UpgradeProgress.AdvanceArmor(FactionId.Player);

            ExpectVfxDestroyErrors(2);
            target.TakeDamage(10f, KingdomsOfBharat.Combat.DamageType.Melee);
            Assert.AreEqual(990f, target.Health, 0.01f,
                "Archer's armor-upgrade scaling is pierce-only - a melee hit should be unaffected by the researched tier.");

            target.TakeDamage(10f, KingdomsOfBharat.Combat.DamageType.Pierce);
            Assert.AreEqual(990f - 9f, target.Health, 0.01f,
                "A pierce hit should reflect the researched armor tier (+1 armor -> 9 damage).");
        }
    }
}
