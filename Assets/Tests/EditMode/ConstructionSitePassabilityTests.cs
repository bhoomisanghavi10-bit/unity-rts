using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;

namespace KingdomsOfBharat.Tests
{
    // Wall mechanics audit (2026-09-29): AoE2's "quick-wall" foundation
    // states - passable until a builder ever starts work, then a
    // permanent hard obstacle regardless of whether builders later leave.
    // See ConstructionSite.cs's own comments on why the NavMeshObstacle
    // lookup is lazily resolved (never in Awake) rather than cached eagerly.
    public class ConstructionSitePassabilityTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
                _root = null;
            }
        }

        // Mirrors WallFactory/GateFactory's own ordering (obstacle added
        // AFTER ConstructionSite) - the exact ordering the lazy Obstacle
        // property exists to survive.
        private (GameObject root, NavMeshObstacle obstacle) NewFoundationWithObstacleAfterSite()
        {
            var root = new GameObject("Root");
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one;
            _root = root;

            var site = root.AddComponent<ConstructionSite>();
            var obstacle = root.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.carving = true; // Factories always create it carving=true.

            return (root, obstacle);
        }

        [Test]
        public void BeginBuilding_FirstCall_SetsObstacleCarvingTrue()
        {
            (GameObject root, NavMeshObstacle obstacle) = NewFoundationWithObstacleAfterSite();
            ConstructionSite site = root.GetComponent<ConstructionSite>();

            site.BeginBuilding();

            Assert.IsTrue(obstacle.carving);
            Assert.IsTrue(site.HasStarted);
        }

        [Test]
        public void BeginBuilding_ThenStopBuilding_ObstacleStaysCarvingTrue()
        {
            (GameObject root, NavMeshObstacle obstacle) = NewFoundationWithObstacleAfterSite();
            ConstructionSite site = root.GetComponent<ConstructionSite>();

            site.BeginBuilding();
            site.StopBuilding();

            Assert.IsTrue(obstacle.carving,
                "AoE2's Initiated Foundation state never reverts, even once every builder leaves");
            Assert.IsTrue(site.HasStarted);
        }

        [Test]
        public void CompleteImmediately_ForcesObstacleCarvingTrue()
        {
            (GameObject root, NavMeshObstacle obstacle) = NewFoundationWithObstacleAfterSite();
            ConstructionSite site = root.GetComponent<ConstructionSite>();

            site.CompleteImmediately();

            Assert.IsTrue(obstacle.carving);
            Assert.IsTrue(site.HasStarted);
        }

        [Test]
        public void RestoreProgress_ZeroProgress_LeavesObstacleNotCarving()
        {
            (GameObject root, NavMeshObstacle obstacle) = NewFoundationWithObstacleAfterSite();
            ConstructionSite site = root.GetComponent<ConstructionSite>();

            site.RestoreProgress(0f);

            Assert.IsFalse(obstacle.carving);
            Assert.IsFalse(site.HasStarted);
        }

        [Test]
        public void RestoreProgress_PartialProgress_SetsObstacleCarvingTrue()
        {
            (GameObject root, NavMeshObstacle obstacle) = NewFoundationWithObstacleAfterSite();
            ConstructionSite site = root.GetComponent<ConstructionSite>();

            site.RestoreProgress(0.4f);

            Assert.IsTrue(obstacle.carving,
                "Nonzero saved progress implies a builder worked this site before the save");
            Assert.IsTrue(site.HasStarted);
        }

        [Test]
        public void NeverStarted_HasStartedIsFalse()
        {
            (GameObject root, _) = NewFoundationWithObstacleAfterSite();
            ConstructionSite site = root.GetComponent<ConstructionSite>();

            Assert.IsFalse(site.HasStarted);
        }

        // AoE2's "0 Melee Armor while incomplete" rule (see Attackable.
        // TakeDamage). Melee armor is generous (20) so the effect is
        // unmistakable if armor is genuinely ignored vs. genuinely applied.
        private GameObject NewArmoredFoundation()
        {
            var root = new GameObject("Foundation");
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one;
            _root = root;

            var attackable = root.AddComponent<Attackable>();
            attackable.Configure(1000f);
            attackable.ConfigureArmor(meleeArmor: 20f, pierceArmor: 20f);
            root.AddComponent<ConstructionSite>().EnsureInitialized();

            return root;
        }

        [Test]
        public void MeleeHit_AgainstIncompleteFoundation_IgnoresArmor()
        {
            GameObject foundation = NewArmoredFoundation();
            Attackable attackable = foundation.GetComponent<Attackable>();

            LogAssert.ignoreFailingMessages = true;
            attackable.TakeDamage(25f, DamageType.Melee);
            LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(1000f - 25f, attackable.Health, 0.001f,
                "20 melee armor should be ignored while the foundation is incomplete");
        }

        [Test]
        public void MeleeHit_AgainstCompleteBuilding_AppliesArmorNormally()
        {
            GameObject foundation = NewArmoredFoundation();
            Attackable attackable = foundation.GetComponent<Attackable>();
            foundation.GetComponent<ConstructionSite>().CompleteImmediately();

            LogAssert.ignoreFailingMessages = true;
            attackable.TakeDamage(25f, DamageType.Melee);
            LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(1000f - (25f - 20f), attackable.Health, 0.001f,
                "20 melee armor should apply normally once construction is complete");
        }

        [Test]
        public void PierceHit_AgainstIncompleteFoundation_StillAppliesPierceArmor()
        {
            GameObject foundation = NewArmoredFoundation();
            Attackable attackable = foundation.GetComponent<Attackable>();

            LogAssert.ignoreFailingMessages = true;
            attackable.TakeDamage(25f, DamageType.Pierce);
            LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(1000f - (25f - 20f), attackable.Health, 0.001f,
                "the 0-armor rule is melee-specific, per AoE2's own \"0 Melee Armor\" wording");
        }
    }
}
