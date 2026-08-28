using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Buildings;

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
    }
}
