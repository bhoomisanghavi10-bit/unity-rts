using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Tests
{
    // Chola Dhanurdhara ranged-presentation ticket: covers (1) the Archer
    // gets its own distinct presentation binding rather than resolving to
    // the same body/attack clip as every melee unit, (2) MeleeAttacker's
    // opt-in projectile mode fires exactly once per swing and only applies
    // the hit on the projectile's simulated arrival (not at release), and
    // (3) AnimationDriver's PlayableGraph stays bounded across repeated
    // clip switches instead of leaking a playable per switch.
    public class ArcherPresentationTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();
        private static readonly Regex SetDestinationErrorPattern = new Regex("SetDestination.*NavMesh");
        // Object.Destroy (as opposed to DestroyImmediate) logs this
        // Editor-only warning outside Play mode - fired by Projectile's own
        // Collider cleanup at release, Attackable.TakeDamage's hit-VFX
        // burst at arrival, and Projectile's own self-destruct at arrival.
        // Same situation SiegeSplashTests/BuildingAttackerTests document.
        private static readonly Regex DestroyErrorPattern = new Regex("Destroy may not be called from edit mode");

        [SetUp]
        public void SetUp()
        {
            // Any test that calls MeleeAttacker.AttackMove (-> UnitMover.
            // MoveTo -> NavMeshAgent.SetDestination, which logs an Editor
            // error here since there's no baked NavMesh in an EditMode
            // test scene) must expect that error explicitly - per
            // SiegeSplashTests' own documented finding, ignoreFailingMessages
            // alone does not suppress it in this Unity Test Framework
            // version. Tests that never call AttackMove don't need any
            // Expect calls at all.
            LogAssert.ignoreFailingMessages = true;
        }

        private static void ExpectSetDestinationError()
        {
            LogAssert.Expect(LogType.Error, SetDestinationErrorPattern);
        }

        private static void ExpectDestroyErrors(int count)
        {
            for (int i = 0; i < count; i++)
            {
                LogAssert.Expect(LogType.Error, DestroyErrorPattern);
            }
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            foreach (Projectile projectile in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
            {
                if (projectile != null)
                {
                    Object.DestroyImmediate(projectile.gameObject);
                }
            }
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
            ArcherLineProgress.ResetForTests();
        }

        private GameObject CreateGameObject(string name)
        {
            var go = new GameObject(name);
            _spawned.Add(go);
            return go;
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

        // --- Requirement 1/3: distinct archer presentation binding ---

        [Test]
        public void LoadFor_Ranged_UsesADifferentAttackClip_ThanMelee()
        {
            HumanAnimationSet.Clips melee = HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Male, HumanAnimationSet.AttackStyle.Melee);
            HumanAnimationSet.Clips ranged = HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Male, HumanAnimationSet.AttackStyle.Ranged);

            Assert.IsNotNull(melee.Attack, "Melee Attack clip should resolve.");
            Assert.IsNotNull(ranged.Attack, "Ranged Attack clip should resolve.");
            Assert.AreNotEqual(melee.Attack, ranged.Attack,
                "An archer's ranged attack must not resolve to the same sword-swing clip as a melee unit.");
            // Idle/Walk are deliberately shared (a person walking reads
            // fine whether or not they're carrying a bow) - only the
            // attack differs.
            Assert.AreEqual(melee.Idle, ranged.Idle);
            Assert.AreEqual(melee.Walk, ranged.Walk);
        }

        [Test]
        public void ArcherFactory_Spawn_OptsIntoProjectilePresentation()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Maurya);

            GameObject archer = ArcherFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(archer);

            var attacker = archer.GetComponent<MeleeAttacker>();
            Assert.IsTrue(attacker.UsesProjectileForTest,
                "Archer should be configured for projectile presentation, not an instant hitscan resolve.");
            Assert.IsNotNull(archer.GetComponent<AnimationDriver>());
        }

        // --- Requirement 4/5: release synced to the animation marker, damage synced to arrival ---

        [Test]
        public void RangedAttack_AppliesDamage_OnProjectileArrival_NotAtRelease()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Maurya);

            GameObject archerGo = ArcherFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(archerGo);
            var attacker = archerGo.GetComponent<MeleeAttacker>();
            var driver = archerGo.GetComponent<AnimationDriver>();
            HumanAnimationSet.Clips ranged = HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Male, HumanAnimationSet.AttackStyle.Ranged);
            AnimationClip attackClip = ranged.Attack;

            Attackable target = CreateHostileUnit(FactionId.Enemy, new Vector3(3f, 0f, 0f));
            ExpectSetDestinationError();
            attacker.AttackMove(target);

            // Drive the model onto the attack clip and park it just before
            // the release marker (0.55) - deterministic, since the graph
            // isn't guaranteed to auto-advance outside Play mode.
            driver.ForceSetClipForTest(attackClip);
            driver.SetPlayableTimeForTest(attackClip.length * 0.1);
            attacker.Tick(0f); // establishes the "not yet crossed" baseline sample

            Assert.AreEqual(1000f, target.Health, "No hit should resolve before the release marker is reached.");

            // Advance past the marker - this is the "release" frame: a
            // Projectile should now exist in flight, but the hit itself
            // must not have landed yet. Releasing destroys the fresh
            // primitive's auto-added Collider (1 Destroy error).
            driver.SetPlayableTimeForTest(attackClip.length * 0.6);
            ExpectDestroyErrors(1);
            attacker.Tick(0f);

            Assert.AreEqual(1000f, target.Health,
                "Crossing the release marker should spawn a projectile, not apply damage instantly.");
            Projectile[] inFlight = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None);
            Assert.AreEqual(1, inFlight.Length, "Exactly one arrow should be in flight after release.");

            // Let the arrow complete its flight (any deltaTime at least as
            // large as its travel time resolves it in one Tick). Arrival
            // fires the hit's own VFX burst (destroyed synchronously in
            // EditMode) and the arrow's own self-destruct - 2 more errors.
            ExpectDestroyErrors(2);
            inFlight[0].Tick(5f);

            Assert.Less(target.Health, 1000f, "Damage should apply once the projectile arrives.");
        }

        [Test]
        public void RangedAttack_DoesNotDoubleFire_WithinTheSameSwingCycle()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Maurya);

            GameObject archerGo = ArcherFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(archerGo);
            var attacker = archerGo.GetComponent<MeleeAttacker>();
            var driver = archerGo.GetComponent<AnimationDriver>();
            HumanAnimationSet.Clips ranged = HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Male, HumanAnimationSet.AttackStyle.Ranged);
            AnimationClip attackClip = ranged.Attack;

            Attackable target = CreateHostileUnit(FactionId.Enemy, new Vector3(3f, 0f, 0f));
            ExpectSetDestinationError();
            attacker.AttackMove(target);

            driver.ForceSetClipForTest(attackClip);
            driver.SetPlayableTimeForTest(attackClip.length * 0.1);
            attacker.Tick(0f);

            driver.SetPlayableTimeForTest(attackClip.length * 0.6);
            ExpectDestroyErrors(1); // the release itself, not counted twice below
            attacker.Tick(0f); // fires - resets cooldown to attackInterval

            // Cooldown is now > 0 (mid-recovery) - sampling more of the
            // same loop must not spawn a second arrow until it recovers.
            driver.SetPlayableTimeForTest(attackClip.length * 0.9);
            attacker.Tick(0f);
            driver.SetPlayableTimeForTest(attackClip.length * 0.2);
            attacker.Tick(0f);

            Projectile[] inFlight = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None);
            Assert.AreEqual(1, inFlight.Length, "Only one arrow should be released per swing/recovery cycle.");
        }

        // --- Requirement 4: pure crossing-detection logic, every case ---

        [Test]
        public void CrossedReleaseMarker_NoPriorSample_NeverCrosses()
        {
            Assert.IsFalse(MeleeAttacker.CrossedReleaseMarker(-1f, 0.9f, 0.5f));
        }

        [Test]
        public void CrossedReleaseMarker_ForwardProgressPastMarker_Crosses()
        {
            Assert.IsTrue(MeleeAttacker.CrossedReleaseMarker(0.4f, 0.6f, 0.5f));
        }

        [Test]
        public void CrossedReleaseMarker_ForwardProgressBeforeMarker_DoesNotCross()
        {
            Assert.IsFalse(MeleeAttacker.CrossedReleaseMarker(0.1f, 0.2f, 0.5f));
        }

        [Test]
        public void CrossedReleaseMarker_WrapsWithoutReachingMarkerYet_DoesNotCross()
        {
            // Loop restarted (0.95 -> 0.05); the marker (0.5) is beyond
            // both samples in the wrapped-over span (0.95..1.0 union
            // 0.0..0.05), so it hasn't actually been reached yet.
            Assert.IsFalse(MeleeAttacker.CrossedReleaseMarker(0.95f, 0.05f, 0.5f));
        }

        [Test]
        public void CrossedReleaseMarker_WrapsThroughMarker_Crosses()
        {
            // Loop restarted (0.9 -> 0.05); the marker (0.95) sits between
            // 0.9 and the loop boundary (1.0), so playback passed through
            // it during the wrap even though neither raw sample is >= 0.95.
            Assert.IsTrue(MeleeAttacker.CrossedReleaseMarker(0.9f, 0.05f, 0.95f));
        }

        [Test]
        public void CrossedReleaseMarker_WrapsAfterMarkerAlreadyPassed_DoesNotDoubleCount()
        {
            // previousNormalized (0.6) is already past the marker (0.5) -
            // the crossing would have fired on an earlier sample, before
            // this wrap; it must not fire again here.
            Assert.IsFalse(MeleeAttacker.CrossedReleaseMarker(0.6f, 0.1f, 0.5f));
        }

        // --- Requirement 8: bounded animation lifecycle ---

        [Test]
        public void RepeatedClipSwitches_DoNotGrowThePlayableGraphUnbounded()
        {
            AgeProgress.Initialize(FactionId.Player, AgeId.Classical);
            CivilizationRegistry.Assign(FactionId.Player, CivilizationId.Maurya);

            GameObject archerGo = ArcherFactory.Spawn(Vector3.zero, FactionId.Player);
            _spawned.Add(archerGo);
            var driver = archerGo.GetComponent<AnimationDriver>();
            HumanAnimationSet.Clips ranged = HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Male, HumanAnimationSet.AttackStyle.Ranged);

            int baseline = driver.PlayableCountForTest;
            for (int i = 0; i < 25; i++)
            {
                driver.ForceSetClipForTest(i % 2 == 0 ? ranged.Idle : ranged.Attack);

                // Checked after every single switch, not just at the end -
                // a leak that only orphans every other switch (or one that
                // happens to net back down to baseline by the 25th switch
                // through sheer parity) would still slip past an
                // end-of-loop-only assertion. Exactly one live playable
                // should feed the output at any given moment, never more.
                Assert.AreEqual(baseline, driver.PlayableCountForTest,
                    $"Switch #{i}: switching clips must not leave an orphaned AnimationClipPlayable in the graph.");
            }
        }

        // --- Requirement 6: simulation timing independent of frame rate ---

        [Test]
        public void Projectile_DoesNotArrive_WhileAccumulatedElapsedIsBelowTravelTime()
        {
            // Fire() itself destroys the fresh primitive's auto-added
            // Collider (see Projectile.BuildVisual) - 1 Destroy error,
            // regardless of whether the shot ever arrives.
            ExpectDestroyErrors(1);
            bool arrived = false;
            Projectile projectile = Projectile.Fire(Vector3.zero, new Vector3(10f, 0f, 0f), travelTime: 1f, () => arrived = true);

            // Split into many small steps (as a high, variable frame rate
            // would deliver) that sum to just under the travel time - must
            // not have arrived yet regardless of how many steps that took.
            for (int i = 0; i < 37; i++)
            {
                projectile.Tick(0.9f / 37f);
            }

            Assert.IsFalse(arrived, "A projectile must not resolve before its simulated travel time has actually elapsed.");
        }

        [Test]
        public void Projectile_ArrivesAtSameOutcome_RegardlessOfStepGranularity()
        {
            // Two projectiles covering the identical distance/travel time,
            // one driven by a single large step (a low/stuttering frame
            // rate) and one by many small steps (a high frame rate) whose
            // total comfortably exceeds the travel time. Deliberately NOT
            // summed to exactly the travel time - float32 accumulation
            // error from 100 additions can land a hair on either side of
            // an exact boundary, which would make this test flaky for a
            // reason that has nothing to do with frame-rate independence.
            // Both must resolve regardless of step count.
            //
            // Each Fire() destroys its own primitive's Collider (2 errors)
            // and each arrival self-destructs the arrow (2 more) - 4 total.
            ExpectDestroyErrors(4);

            bool arrivedCoarse = false;
            Projectile coarse = Projectile.Fire(Vector3.zero, new Vector3(10f, 0f, 0f), travelTime: 1f, () => arrivedCoarse = true);
            coarse.Tick(1f);

            bool arrivedFine = false;
            Projectile fine = Projectile.Fire(Vector3.zero, new Vector3(10f, 0f, 0f), travelTime: 1f, () => arrivedFine = true);
            for (int i = 0; i < 100; i++)
            {
                fine.Tick(0.02f); // sums to 2.0 - well past the 1s travel time
            }

            Assert.IsTrue(arrivedCoarse, "One large step covering the full travel time should resolve arrival.");
            Assert.IsTrue(arrivedFine, "Many small steps summing well past the travel time should resolve arrival identically.");
        }

        [Test]
        public void Projectile_ArrivalCallback_FiresExactlyOnce_EvenIfTickedAgainAfterArrival()
        {
            // 1 Collider-destroy from Fire() + 1 self-destruct from the
            // first Tick that actually crosses the arrival threshold. The
            // second Tick call is a true no-op once resolved (see
            // Projectile._resolved) - it must not log a second Destroy.
            ExpectDestroyErrors(2);

            int arrivedCount = 0;
            Projectile projectile = Projectile.Fire(Vector3.zero, new Vector3(10f, 0f, 0f), travelTime: 1f, () => arrivedCount++);

            // A stray extra Tick (e.g. a lingering frame before Destroy
            // actually takes effect) must not re-fire the arrival hit.
            projectile.Tick(1f);
            projectile.Tick(1f);

            Assert.AreEqual(1, arrivedCount, "Arrival must apply the hit exactly once, never once per subsequent Tick.");
        }
    }
}
