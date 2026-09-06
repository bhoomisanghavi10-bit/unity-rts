using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Multiplayer;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Tests
{
    // Wave 4 item 27 (Vaidya + Purohita, the Support units splitting AoE's
    // Monk): exercises VaidyaHealer/PurohitaConverter's internal Tick
    // directly with an explicit deltaTime - same convention
    // MeleeAttackerTests/SiegeSplashTests already use - and their pure
    // static helpers (CanConvert/ChanceForTick) with no scene dependency at
    // all, same as GathererDropOffTests' own AcceptsDropOff coverage.
    public class SupportUnitTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();
        private static readonly Regex SetDestinationErrorPattern = new Regex("SetDestination.*NavMesh");
        // Attackable.TakeDamage unconditionally spawns a VfxFactory particle
        // burst (stopAction: Destroy) - harmless at real runtime, but Unity's
        // Editor logs this once per hit outside Play mode - same situation
        // SiegeSplashTests/BuildingAttackerTests document. Every
        // NewAttackable call below that pre-damages its target (and the
        // explicit dead-target TakeDamage call) needs one Expect per hit.
        private static readonly Regex VfxDestroyErrorPattern = new Regex("Destroy may not be called from edit mode");

        [SetUp]
        public void SetUp()
        {
            // HealAt/ConvertAt/Tick's own chase branch all call
            // UnitMover.MoveTo -> NavMeshAgent.SetDestination, which logs
            // an Editor error here (no baked NavMesh in an EditMode test
            // scene) - same expected-not-a-regression situation
            // GathererCombatResponseTests documents; each test that
            // triggers it consumes the log via LogAssert.Expect.
            LogAssert.ignoreFailingMessages = true;
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
            DeterministicRandom.ReseedMatch(1);
        }

        private static void ExpectSetDestinationError()
        {
            LogAssert.Expect(LogType.Error, SetDestinationErrorPattern);
        }

        private static void ExpectVfxDestroyError()
        {
            LogAssert.Expect(LogType.Error, VfxDestroyErrorPattern);
        }

        private GameObject NewGameObject(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            _spawned.Add(go);
            return go;
        }

        private VaidyaHealer NewHealer(Vector3 position, FactionId faction)
        {
            GameObject go = NewGameObject("Vaidya", position);
            go.AddComponent<UnitMover>();
            go.AddComponent<FactionMember>().Configure(faction);
            return go.AddComponent<VaidyaHealer>();
        }

        private PurohitaConverter NewConverter(Vector3 position, FactionId faction)
        {
            GameObject go = NewGameObject("Purohita", position);
            go.AddComponent<UnitMover>();
            go.AddComponent<FactionMember>().Configure(faction);
            return go.AddComponent<PurohitaConverter>();
        }

        private Attackable NewAttackable(Vector3 position, FactionId faction, UnitClass unitClass, float maxHealth, float currentHealth)
        {
            GameObject go = NewGameObject("Target", position);
            go.AddComponent<FactionMember>().Configure(faction);
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure(maxHealth);
            attackable.ConfigureClass(unitClass);
            if (currentHealth < maxHealth)
            {
                ExpectVfxDestroyError();
                attackable.TakeDamage(maxHealth - currentHealth);
            }
            return attackable;
        }

        [Test]
        public void VaidyaHealer_Tick_HealsWhenInRange()
        {
            VaidyaHealer healer = NewHealer(Vector3.zero, FactionId.Player);
            Attackable target = NewAttackable(new Vector3(1f, 0f, 0f), FactionId.Player, UnitClass.Infantry, 30f, 10f);

            ExpectSetDestinationError(); // HealAt's own initial MoveTo
            healer.HealAt(target);
            healer.Tick(1f);

            Assert.Greater(target.Health, 10f);
            Assert.IsTrue(healer.IsHealing);
        }

        [Test]
        public void VaidyaHealer_Tick_StopsAtFullHealth()
        {
            VaidyaHealer healer = NewHealer(Vector3.zero, FactionId.Player);
            Attackable target = NewAttackable(new Vector3(1f, 0f, 0f), FactionId.Player, UnitClass.Infantry, 30f, 30f);

            ExpectSetDestinationError();
            healer.HealAt(target);
            healer.Tick(1f);

            Assert.AreEqual(30f, target.Health);
            Assert.IsFalse(healer.IsHealing, "Already-full-health targets should immediately clear the heal order.");
        }

        [Test]
        public void VaidyaHealer_Tick_ChasesWhenOutOfRange()
        {
            VaidyaHealer healer = NewHealer(Vector3.zero, FactionId.Player);
            Attackable target = NewAttackable(new Vector3(50f, 0f, 0f), FactionId.Player, UnitClass.Infantry, 30f, 10f);

            ExpectSetDestinationError(); // HealAt
            healer.HealAt(target);
            ExpectSetDestinationError(); // Tick's own chase MoveTo
            healer.Tick(1f);

            Assert.AreEqual(10f, target.Health, "Out of range - no healing should happen yet.");
            Assert.IsTrue(healer.IsHealing);
        }

        [TestCase(UnitClass.Building, false)]
        [TestCase(UnitClass.Siege, false)]
        [TestCase(UnitClass.Support, false)]
        [TestCase(UnitClass.Hero, false)]
        [TestCase(UnitClass.Infantry, true)]
        [TestCase(UnitClass.Archer, true)]
        [TestCase(UnitClass.Cavalry, true)]
        [TestCase(UnitClass.Naval, true)]
        public void PurohitaConverter_CanConvert_ExcludesBuildingSiegeSupportHero(UnitClass unitClass, bool expected)
        {
            Attackable target = NewAttackable(Vector3.zero, FactionId.Enemy, unitClass, 30f, 30f);

            Assert.AreEqual(expected, PurohitaConverter.CanConvert(target));
        }

        [Test]
        public void PurohitaConverter_CanConvert_ExcludesDeadTargets()
        {
            Attackable target = NewAttackable(Vector3.zero, FactionId.Enemy, UnitClass.Infantry, 30f, 30f);
            // A killing blow logs three Editor-only Destroy errors - the
            // per-hit VFX burst's stop-action, the death VFX burst's
            // stop-action, and Attackable's own Destroy(gameObject) itself.
            ExpectVfxDestroyError();
            ExpectVfxDestroyError();
            ExpectVfxDestroyError();
            target.TakeDamage(1000f);

            Assert.IsFalse(PurohitaConverter.CanConvert(target));
        }

        [Test]
        public void PurohitaConverter_CanConvert_NullTarget_ReturnsFalse()
        {
            Assert.IsFalse(PurohitaConverter.CanConvert(null));
        }

        [Test]
        public void PurohitaConverter_ChanceForTick_HigherMissingHp_GivesHigherChance()
        {
            float lowMissing = PurohitaConverter.ChanceForTick(0.15f, 0f, 1f);
            float highMissing = PurohitaConverter.ChanceForTick(0.15f, 0.9f, 1f);

            Assert.Greater(highMissing, lowMissing);
        }

        [Test]
        public void PurohitaConverter_ChanceForTick_LongerDeltaTime_GivesHigherChance()
        {
            float shortTick = PurohitaConverter.ChanceForTick(0.15f, 0.5f, 0.1f);
            float longTick = PurohitaConverter.ChanceForTick(0.15f, 0.5f, 2f);

            Assert.Greater(longTick, shortTick);
        }

        [Test]
        public void PurohitaConverter_Tick_GuaranteedRoll_ReassignsFactionMember()
        {
            PurohitaConverter converter = NewConverter(Vector3.zero, FactionId.Player);
            // Forces ChanceForTick's result to exactly 1.0 regardless of
            // DeterministicRandom's actual draw (which never returns
            // exactly 1.0 - see DeterministicRandom.NextFloat01) - a
            // deterministic way to prove a successful roll actually flips
            // FactionMember.Faction, without depending on the RNG's real
            // output.
            converter.baseChancePerSecond = 1000f;
            Attackable target = NewAttackable(new Vector3(1f, 0f, 0f), FactionId.Enemy, UnitClass.Infantry, 30f, 1f);

            ExpectSetDestinationError(); // ConvertAt
            converter.ConvertAt(target);
            converter.Tick(1f);

            Assert.AreEqual(FactionId.Player, target.GetComponent<FactionMember>().Faction);
            Assert.IsFalse(converter.IsConverting, "A landed conversion should clear the target, not repeat.");
        }

        [Test]
        public void PurohitaConverter_Tick_ChasesWhenOutOfRange()
        {
            PurohitaConverter converter = NewConverter(Vector3.zero, FactionId.Player);
            converter.baseChancePerSecond = 1000f;
            Attackable target = NewAttackable(new Vector3(50f, 0f, 0f), FactionId.Enemy, UnitClass.Infantry, 30f, 1f);

            ExpectSetDestinationError(); // ConvertAt
            converter.ConvertAt(target);
            ExpectSetDestinationError(); // Tick's own chase MoveTo
            converter.Tick(1f);

            Assert.AreEqual(FactionId.Enemy, target.GetComponent<FactionMember>().Faction, "Out of range - no conversion roll should happen yet.");
            Assert.IsTrue(converter.IsConverting);
        }

        [Test]
        public void PurohitaConverter_Tick_UnconvertibleTarget_ClearsWithoutRolling()
        {
            PurohitaConverter converter = NewConverter(Vector3.zero, FactionId.Player);
            converter.baseChancePerSecond = 1000f;
            Attackable target = NewAttackable(new Vector3(1f, 0f, 0f), FactionId.Enemy, UnitClass.Building, 30f, 1f);

            ExpectSetDestinationError(); // ConvertAt
            converter.ConvertAt(target);
            converter.Tick(1f);

            Assert.AreEqual(FactionId.Enemy, target.GetComponent<FactionMember>().Faction);
            Assert.IsFalse(converter.IsConverting);
        }
    }
}
