using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Tests
{
    // Roadmap ad hoc request (2026-08-28): the foundation squash-to-grow
    // animation must scale only the visual child, never the root - a
    // building's NavMeshObstacle/BuildingFootprintTag live on the root, and
    // AoE-style footprint blocking needs to hold from the instant a
    // foundation is placed, not only once the visual actually starts
    // growing (see ConstructionSite.cs's class doc comment).
    public class ConstructionSiteTests
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

        private (GameObject root, Transform visual) NewFoundation(Vector3 finalScale)
        {
            var root = new GameObject("Root");
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = finalScale;
            _root = root;
            return (root, visual.transform);
        }

        // Calls EnsureInitialized() directly (internal, exposed for exactly
        // this) instead of relying on Awake having already run by the next
        // line - Unity's Editor doesn't guarantee that timing synchronously
        // within a test method, even across a yielded frame (confirmed
        // empirically, not just in theory: converting these to [UnityTest]
        // + yield return null was tried first and still failed the same
        // way). Real gameplay doesn't have this problem - Awake always
        // fires before anything else can run for a given object - so this
        // is a test-authoring workaround only, not a change to production
        // timing.
        [Test]
        public void Awake_LeavesRootScaleAtIdentity_RegardlessOfVisualScale()
        {
            (GameObject root, Transform visual) = NewFoundation(new Vector3(4f, 4f, 4f));

            root.AddComponent<ConstructionSite>().EnsureInitialized();

            Assert.AreEqual(Vector3.one, root.transform.localScale,
                "Root scale must stay (1,1,1) throughout construction so a NavMeshObstacle on root carves a stable footprint from the instant the foundation is placed");
        }

        [Test]
        public void Awake_SquashesVisualChildOnly_NotRoot()
        {
            (GameObject root, Transform visual) = NewFoundation(new Vector3(4f, 4f, 4f));

            root.AddComponent<ConstructionSite>().EnsureInitialized();

            Assert.AreEqual(0.01f, visual.localScale.y, 0.0001f,
                "Foundation should start squashed to near-zero height on the visual child");
            Assert.AreEqual(4f, visual.localScale.x, 0.0001f);
            Assert.AreEqual(4f, visual.localScale.z, 0.0001f);
        }

        [Test]
        public void CompleteImmediately_RestoresVisualChildToFinalScale_RootStaysIdentity()
        {
            (GameObject root, Transform visual) = NewFoundation(new Vector3(3f, 5f, 3f));

            var site = root.AddComponent<ConstructionSite>();
            site.CompleteImmediately();

            Assert.AreEqual(5f, visual.localScale.y, 0.0001f);
            Assert.AreEqual(Vector3.one, root.transform.localScale);
            Assert.IsTrue(site.IsComplete);
        }

        [Test]
        public void Awake_WithNoVisualChild_FallsBackToOwnTransform_DoesNotThrow()
        {
            var root = new GameObject("BareRoot");
            root.transform.localScale = new Vector3(2f, 2f, 2f);
            _root = root;

            Assert.DoesNotThrow(() => root.AddComponent<ConstructionSite>().EnsureInitialized());
        }

        // Worker mechanics audit (2026-08-29): confirms the AoE II
        // diminishing-returns multi-builder formula's exact predicted
        // ratios, not just "more workers is faster". Pure function test -
        // no Time.deltaTime dependency, same "pure/testable helper"
        // convention as HoverTooltip.ResolveCursorState.
        [TestCase(1, 1f)]
        [TestCase(2, 1.6f)]
        [TestCase(3, 1.9f)]
        [TestCase(4, 2.2f)]
        [TestCase(0, 0f)]
        public void SpeedMultiplier_MatchesAoeIIDiminishingReturnsFormula(int activeBuilders, float expectedMultiplier)
        {
            Assert.AreEqual(expectedMultiplier, ConstructionSite.SpeedMultiplier(activeBuilders), 0.0001f);
        }

        // AoE-style cancel-construction rule: "resources can be fully
        // refunded if the player cancels the building at any point during
        // its construction, as long as it hasn't taken damage... if a
        // building does take damage before it is complete and then is
        // canceled, only a portion of the resources are returned depending
        // on how much damage the building took." Enemy2 isolates this
        // faction's stockpile from other fixtures, same convention as
        // WorkerOrderTests.
        private const FactionId CancelTestFaction = FactionId.Enemy2;
        private GameObject _stockpileGo;

        [TearDown]
        public void TearDownStockpile()
        {
            if (_stockpileGo != null)
            {
                Object.DestroyImmediate(_stockpileGo);
                _stockpileGo = null;
            }
        }

        private ResourceStockpile NewStockpile()
        {
            _stockpileGo = new GameObject("Stockpile");
            var stockpile = _stockpileGo.AddComponent<ResourceStockpile>();
            stockpile.Configure(CancelTestFaction);
            return stockpile;
        }

        private GameObject NewFoundationWithCost(float maxHealth, float woodCost)
        {
            var root = new GameObject("Foundation");
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one;
            _root = root;

            root.AddComponent<FactionMember>().Configure(CancelTestFaction);
            var attackable = root.AddComponent<Attackable>();
            attackable.Configure(maxHealth);
            root.AddComponent<BuildingCost>().Record(ResourceType.Wood, woodCost);
            root.AddComponent<ConstructionSite>().EnsureInitialized();

            return root;
        }

        [Test]
        public void CancelAndRefund_UndamagedFoundation_RefundsFullCost()
        {
            ResourceStockpile stockpile = NewStockpile();
            GameObject foundation = NewFoundationWithCost(maxHealth: 300f, woodCost: 100f);

            foundation.GetComponent<ConstructionSite>().CancelAndRefund();

            Assert.AreEqual(100f, stockpile.GetTotal(ResourceType.Wood), 0.001f);
        }

        [Test]
        public void CancelAndRefund_DamagedFoundation_RefundsProportionally()
        {
            ResourceStockpile stockpile = NewStockpile();
            GameObject foundation = NewFoundationWithCost(maxHealth: 300f, woodCost: 100f);

            // Attackable.TakeDamage unconditionally spawns a VfxFactory
            // burst (stopAction: Destroy), which logs an Editor-only
            // "Destroy may not be called from edit mode!" once that
            // particle system's own cleanup fires outside Play mode - same
            // documented, expected situation BuildingAttackerTests'
            // TickIgnoringVfxLogs works around, not a real bug here.
            LogAssert.ignoreFailingMessages = true;
            foundation.GetComponent<Attackable>().TakeDamage(150f);
            LogAssert.ignoreFailingMessages = false;

            foundation.GetComponent<ConstructionSite>().CancelAndRefund();

            // Health drops from 300 to 150 (half remaining) - refund should
            // be half of the recorded 100 Wood.
            Assert.AreEqual(50f, stockpile.GetTotal(ResourceType.Wood), 0.001f);
        }

        [Test]
        public void CancelAndRefund_DestroysTheFoundation()
        {
            NewStockpile();
            GameObject foundation = NewFoundationWithCost(maxHealth: 300f, woodCost: 100f);

            foundation.GetComponent<ConstructionSite>().CancelAndRefund();

            Assert.IsTrue(foundation == null, "Canceling should destroy the foundation GameObject");
            _root = null;
        }

        [Test]
        public void CancelAndRefund_OnAlreadyCompleteSite_DoesNothing()
        {
            ResourceStockpile stockpile = NewStockpile();
            GameObject foundation = NewFoundationWithCost(maxHealth: 300f, woodCost: 100f);
            foundation.GetComponent<ConstructionSite>().CompleteImmediately();

            foundation.GetComponent<ConstructionSite>().CancelAndRefund();

            Assert.AreEqual(0f, stockpile.GetTotal(ResourceType.Wood), 0.001f,
                "A completed building must not be refunded through the cancel path");
            Assert.IsFalse(foundation == null, "A completed building should not be destroyed by CancelAndRefund");
        }
    }
}
