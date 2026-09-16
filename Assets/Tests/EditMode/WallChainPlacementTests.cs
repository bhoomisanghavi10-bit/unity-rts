using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Buildings;

namespace KingdomsOfBharat.Tests
{
    // Wall system Session A: BuildingPlacer.ComputeWallChain is the one
    // pure function driving free-angle drag-placement - see the class's
    // own comment for the exact math. Pure/static, no MonoBehaviour/scene
    // dependency, so it's directly testable rather than needing a live
    // Play-mode drag simulation.
    public class WallChainPlacementTests
    {
        private const float SegmentSpacing = 2.4f;
        private const int MaxSegments = 30;

        [Test]
        public void ZeroDistanceDrag_ReturnsSingleSegmentAtAnchor()
        {
            Vector3 anchor = new Vector3(5f, 0f, 5f);

            List<(Vector3 position, Quaternion rotation)> chain =
                BuildingPlacer.ComputeWallChain(anchor, anchor, SegmentSpacing, MaxSegments);

            Assert.AreEqual(1, chain.Count);
            Assert.AreEqual(anchor, chain[0].position);
            Assert.AreEqual(Quaternion.identity, chain[0].rotation);
        }

        [Test]
        public void ClickWithoutDrag_MatchesOldSingleClickBehavior()
        {
            // Before the mouse is pressed, UpdateWallDrag calls this with
            // anchor == current (the live cursor point) every frame - this
            // test locks in that the degenerate case is exactly one
            // segment at the cursor, so a plain click is unaffected by
            // this session's changes.
            Vector3 cursor = new Vector3(-3f, 0f, 12f);

            List<(Vector3 position, Quaternion rotation)> chain =
                BuildingPlacer.ComputeWallChain(cursor, cursor, SegmentSpacing, MaxSegments);

            Assert.AreEqual(1, chain.Count);
            Assert.AreEqual(cursor, chain[0].position);
        }

        [Test]
        public void DragOneSegmentSpacing_ReturnsTwoSegments()
        {
            Vector3 anchor = Vector3.zero;
            Vector3 current = new Vector3(SegmentSpacing, 0f, 0f);

            List<(Vector3 position, Quaternion rotation)> chain =
                BuildingPlacer.ComputeWallChain(anchor, current, SegmentSpacing, MaxSegments);

            Assert.AreEqual(2, chain.Count);
            Assert.AreEqual(anchor, chain[0].position);
            Assert.AreEqual(new Vector3(SegmentSpacing, 0f, 0f), chain[1].position);
        }

        [Test]
        public void DragAlongPositiveX_KeepsIdentityRotation()
        {
            Vector3 anchor = Vector3.zero;
            Vector3 current = new Vector3(SegmentSpacing * 3f, 0f, 0f);

            List<(Vector3 position, Quaternion rotation)> chain =
                BuildingPlacer.ComputeWallChain(anchor, current, SegmentSpacing, MaxSegments);

            Assert.Greater(chain.Count, 1);
            // Dragging straight along +X means the wall's own long axis
            // (local +X) already points along the drag direction, so
            // FromToRotation(right, right) is identity.
            Assert.AreEqual(Quaternion.identity, chain[0].rotation);
        }

        [Test]
        public void DiagonalDrag_RotatesLocalRightAxisToFaceDragDirection()
        {
            Vector3 anchor = Vector3.zero;
            Vector3 current = new Vector3(10f, 0f, 10f); // 45 degrees in the XZ plane

            List<(Vector3 position, Quaternion rotation)> chain =
                BuildingPlacer.ComputeWallChain(anchor, current, SegmentSpacing, MaxSegments);

            Vector3 expectedDir = new Vector3(1f, 0f, 1f).normalized;
            Vector3 actualDir = chain[0].rotation * Vector3.right;

            Assert.Less(Vector3.Distance(expectedDir, actualDir), 0.001f,
                $"expected rotated local +X to align with {expectedDir}, was {actualDir}");
        }

        [Test]
        public void EverySegmentInADiagonalChain_SharesTheSameRotation()
        {
            Vector3 anchor = Vector3.zero;
            Vector3 current = new Vector3(0f, 0f, SegmentSpacing * 4f);

            List<(Vector3 position, Quaternion rotation)> chain =
                BuildingPlacer.ComputeWallChain(anchor, current, SegmentSpacing, MaxSegments);

            Assert.Greater(chain.Count, 1);
            for (int i = 1; i < chain.Count; i++)
            {
                Assert.AreEqual(chain[0].rotation, chain[i].rotation);
            }
        }

        [Test]
        public void SegmentsAreEvenlySpacedAlongTheDragLine()
        {
            Vector3 anchor = Vector3.zero;
            Vector3 current = new Vector3(SegmentSpacing * 5f, 0f, 0f);

            List<(Vector3 position, Quaternion rotation)> chain =
                BuildingPlacer.ComputeWallChain(anchor, current, SegmentSpacing, MaxSegments);

            for (int i = 1; i < chain.Count; i++)
            {
                float gap = Vector3.Distance(chain[i].position, chain[i - 1].position);
                Assert.AreEqual(SegmentSpacing, gap, 0.001f);
            }
        }

        [Test]
        public void VeryLongDrag_ClampsToMaxSegments()
        {
            Vector3 anchor = Vector3.zero;
            Vector3 current = new Vector3(SegmentSpacing * 1000f, 0f, 0f);

            List<(Vector3 position, Quaternion rotation)> chain =
                BuildingPlacer.ComputeWallChain(anchor, current, SegmentSpacing, MaxSegments);

            Assert.AreEqual(MaxSegments, chain.Count);
        }

        [Test]
        public void NegativeDirectionDrag_StillProducesEvenlySpacedChain()
        {
            Vector3 anchor = new Vector3(10f, 0f, 10f);
            Vector3 current = new Vector3(10f - SegmentSpacing * 2f, 0f, 10f);

            List<(Vector3 position, Quaternion rotation)> chain =
                BuildingPlacer.ComputeWallChain(anchor, current, SegmentSpacing, MaxSegments);

            Assert.AreEqual(3, chain.Count);
            Assert.AreEqual(anchor, chain[0].position);
            Assert.AreEqual(new Vector3(10f - SegmentSpacing, 0f, 10f), chain[1].position);
        }
    }
}
