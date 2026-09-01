using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomsOfBharat.Combat;

namespace KingdomsOfBharat.Tests
{
    // AoE-parity Phase 2 combat-calibration audit (2026-09-01): a numeric
    // audit found the Archer->Cavalry hard-counter bonus resolved as a
    // near coin-flip at its old 1.5x value (Archer wins with only 7% of
    // its own HP left) rather than a real decisive counter - see
    // CombatBonus.cs's own comment on the raised 2.0x value for the full
    // rationale. This drives a full simultaneous round-by-round duel
    // through the real production formula (CombatBonus.Multiplier +
    // Attackable.TakeDamage's flat-armor-subtraction math), using the
    // exact base stats from unit_roster_template.csv (Archer 18 HP/4 dmg
    // Pierce/0 armor, Cavalry 40 HP/6 dmg Melee/1 melee armor/0 pierce
    // armor), rather than re-deriving the expected numbers independently -
    // this is the same duel this session hand-computed in chat before the
    // value was approved, now pinned as a regression test.
    public class CombatBonusTests
    {
        private GameObject _archerGo;
        private GameObject _cavalryGo;
        private Attackable _archer;
        private Attackable _cavalry;

        [SetUp]
        public void SetUp()
        {
            _archerGo = new GameObject("Archer");
            _archer = _archerGo.AddComponent<Attackable>();
            _archer.Configure(18f);
            _archer.ConfigureArmor(meleeArmor: 0f, pierceArmor: 0f);
            _archer.ConfigureClass(UnitClass.Archer);

            _cavalryGo = new GameObject("Cavalry");
            _cavalry = _cavalryGo.AddComponent<Attackable>();
            _cavalry.Configure(40f);
            _cavalry.ConfigureArmor(meleeArmor: 1f, pierceArmor: 0f);
            _cavalry.ConfigureClass(UnitClass.Cavalry);
        }

        [TearDown]
        public void TearDown()
        {
            if (_archerGo != null) Object.DestroyImmediate(_archerGo);
            if (_cavalryGo != null) Object.DestroyImmediate(_cavalryGo);
        }

        [Test]
        public void ArcherVsCavalryMultiplier_Is2x_NotOldValueOrOvershoot()
        {
            Assert.AreEqual(2f, CombatBonus.Multiplier(UnitClass.Archer, UnitClass.Cavalry));
        }

        [Test]
        public void ArcherVsCavalryDuel_ResolvesAsDecisiveArcherWin_NotACoinFlip()
        {
            // Attackable.TakeDamage spawns a VFX burst and Destroy()s on
            // death - the VFX particle system's stop-action logs an
            // Editor-only "Destroy may not be called from edit mode"
            // warning outside Play mode, same as BuildingAttackerTests
            // already documents for this exact call.
            LogAssert.ignoreFailingMessages = true;

            const float archerDamage = 4f;
            const float cavalryDamage = 6f;
            float archerBonus = CombatBonus.Multiplier(UnitClass.Archer, UnitClass.Cavalry);
            float cavalryBonus = CombatBonus.Multiplier(UnitClass.Cavalry, UnitClass.Archer);

            int rounds = 0;
            while (!_archer.IsDead && !_cavalry.IsDead)
            {
                rounds++;
                // Both units act simultaneously each second (both use
                // MeleeAttacker's shared 1s attackInterval in real play) -
                // the while condition above already guarantees both are
                // alive entering this round, so both hits land
                // unconditionally even if one side's hit would kill the
                // other within this same round.
                _archer.TakeDamage(cavalryDamage * cavalryBonus, KingdomsOfBharat.Combat.DamageType.Melee);
                _cavalry.TakeDamage(archerDamage * archerBonus, KingdomsOfBharat.Combat.DamageType.Pierce);

                Assert.Less(rounds, 50, "Duel should resolve well within 50 rounds - runaway loop indicates a formula regression.");
            }

            Assert.IsTrue(_cavalry.IsDead, "Archer should win this matchup outright, not just survive.");
            Assert.AreEqual(5, rounds, "Cavalry (40 HP) should die in exactly 5 rounds at Archer's 2x bonus (8 effective dmg/hit).");
            Assert.AreEqual(6f, _archer.Health, 0.01f, "Archer should still hold roughly a third of its 18 HP (6 HP) when Cavalry dies - a clear win, not the old 1.5x value's ~7%-HP coin-flip.");
        }
    }
}
