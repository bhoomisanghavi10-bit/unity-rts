using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using KingdomsOfBharat.Buildings;

namespace KingdomsOfBharat.Tests
{
    // Roadmap ad hoc request (2026-08-28): AoE-style building footprints -
    // every building except Wall/Gate occupies a square tile footprint and
    // carves a NavMeshObstacle shrunk by Margin on every side, leaving a
    // thin walkable edge. See BuildingFootprint.cs.
    public class BuildingFootprintTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go == null)
                {
                    continue;
                }
                // Building.All registration/deregistration normally happens
                // via OnEnable/OnDisable, but Unity's Editor doesn't
                // guarantee those fire synchronously within a single test
                // method (confirmed empirically - even a yielded frame
                // wasn't enough). Managed directly here instead, so cleanup
                // doesn't depend on that timing either.
                if (go.TryGetComponent(out Building building))
                {
                    Building.All.Remove(building);
                }
                Object.DestroyImmediate(go);
            }
            _spawned.Clear();
        }

        // Registers directly in Building.All rather than relying on
        // Building.OnEnable to have fired by the time IsClear() (below)
        // reads it - see the TearDown comment above for why.
        private GameObject NewBuilding(Vector3 position)
        {
            var go = new GameObject("TestBuilding");
            go.transform.position = position;
            Building building = go.AddComponent<Building>();
            if (!Building.All.Contains(building))
            {
                Building.All.Add(building);
            }
            _spawned.Add(go);
            return go;
        }

        [Test]
        public void World_MultipliesTilesByTileWorldSize()
        {
            Assert.AreEqual(BuildingFootprint.TileWorldSize * 4f, BuildingFootprint.World(4));
        }

        [Test]
        public void Square_ReturnsEqualWidthAndDepth()
        {
            Vector2 square = BuildingFootprint.Square(BuildingFootprint.BarracksTiles);
            Assert.AreEqual(square.x, square.y);
            Assert.AreEqual(BuildingFootprint.World(BuildingFootprint.BarracksTiles), square.x);
        }

        [Test]
        public void Attach_WithCarveObstacle_AddsTagAndMarginShrunkObstacle()
        {
            GameObject go = NewBuilding(Vector3.zero);
            Vector2 footprint = BuildingFootprint.Square(BuildingFootprint.HouseTiles);

            BuildingFootprint.Attach(go, footprint, carveObstacle: true);

            var tag = go.GetComponent<BuildingFootprintTag>();
            Assert.IsNotNull(tag, "Attach did not add a BuildingFootprintTag");
            Assert.AreEqual(footprint, tag.Size);

            var obstacle = go.GetComponent<NavMeshObstacle>();
            Assert.IsNotNull(obstacle, "Attach(carveObstacle: true) did not add a NavMeshObstacle");
            Assert.IsTrue(obstacle.carving);
            Assert.AreEqual(footprint.x - BuildingFootprint.Margin * 2f, obstacle.size.x, 0.001f);
            Assert.AreEqual(footprint.y - BuildingFootprint.Margin * 2f, obstacle.size.z, 0.001f);
        }

        [Test]
        public void Attach_WithoutCarveObstacle_TagsOnly_NoObstacleAdded()
        {
            GameObject go = NewBuilding(Vector3.zero);
            Vector2 footprint = new Vector2(2.4f, 0.4f);

            BuildingFootprint.Attach(go, footprint, carveObstacle: false);

            Assert.IsNotNull(go.GetComponent<BuildingFootprintTag>());
            Assert.IsNull(go.GetComponent<NavMeshObstacle>(),
                "Attach(carveObstacle: false) should leave Wall/Gate's own obstacle setup untouched, not add its own");
        }

        [Test]
        public void IsClear_ReturnsFalse_WhenCandidateFootprintOverlapsExistingBuilding()
        {
            GameObject existing = NewBuilding(Vector3.zero);
            BuildingFootprint.Attach(existing, BuildingFootprint.Square(BuildingFootprint.BarracksTiles), carveObstacle: true);

            // Barracks is 4x4 - a candidate centered 1 unit away still
            // overlaps (half-widths sum to 4, well over 1).
            bool clear = BuildingFootprint.IsClear(new Vector3(1f, 0f, 0f), BuildingFootprint.Square(BuildingFootprint.HouseTiles));

            Assert.IsFalse(clear);
        }

        [Test]
        public void IsClear_ReturnsTrue_WhenCandidateFootprintDoesNotOverlapAnyExistingBuilding()
        {
            GameObject existing = NewBuilding(Vector3.zero);
            BuildingFootprint.Attach(existing, BuildingFootprint.Square(BuildingFootprint.HouseTiles), carveObstacle: true);

            // House (2x2) at origin plus House (2x2) candidate: half-widths
            // sum to 2 - well clear at 20 units away.
            bool clear = BuildingFootprint.IsClear(new Vector3(20f, 0f, 20f), BuildingFootprint.Square(BuildingFootprint.HouseTiles));

            Assert.IsTrue(clear);
        }

        [Test]
        public void IsClear_TreatsUntaggedBuilding_WithFallbackClearance()
        {
            // A Building with no BuildingFootprintTag at all (shouldn't
            // happen via a real Factory, but IsClear must fail safe rather
            // than silently ignoring it).
            NewBuilding(Vector3.zero);

            bool clearFarAway = BuildingFootprint.IsClear(new Vector3(20f, 0f, 20f), BuildingFootprint.Square(BuildingFootprint.HouseTiles));
            bool clearNearby = BuildingFootprint.IsClear(new Vector3(0.5f, 0f, 0.5f), BuildingFootprint.Square(BuildingFootprint.HouseTiles));

            Assert.IsTrue(clearFarAway);
            Assert.IsFalse(clearNearby);
        }

        // Bug fix (2026-08-29): Gatherer used to walk workers to the
        // TownCenter's raw transform.position, which sits inside the
        // NavMeshObstacle carved by Attach(carveObstacle: true) above - a
        // NavMeshAgent can never physically reach it, so deposits never
        // fired. GetNearestApproachPoint returns the nearest point on the
        // real walkable boundary instead, from whichever direction the
        // caller approaches from - tested from 5 different angles here so
        // it isn't only verified from whichever side it happened to be
        // built against.
        // Expected distance from center is the AABB exit distance along
        // `direction` (obstacle half-extent 2.5 on both axes) plus buffer -
        // axis-aligned approaches exit through a face (2.5 + buffer =
        // 3.0), the diagonal approach exits through the corner, which is
        // farther out (2.5 / cos(45 deg) + buffer =~ 4.0355) - this is why
        // each case needs its own expected value rather than one shared
        // constant.
        [TestCase(1f, 0f, 3.0f, TestName = "FromPositiveX")]
        [TestCase(-1f, 0f, 3.0f, TestName = "FromNegativeX")]
        [TestCase(0f, 1f, 3.0f, TestName = "FromPositiveZ")]
        [TestCase(0f, -1f, 3.0f, TestName = "FromNegativeZ")]
        [TestCase(1f, 1f, 4.0355f, TestName = "FromDiagonal")]
        public void GetNearestApproachPoint_CarvingBuilding_LandsJustOutsideObstacleBoundary(float dirX, float dirZ, float expectedDistanceFromCenter)
        {
            GameObject go = NewBuilding(new Vector3(10f, 0f, 10f));
            Vector2 footprint = BuildingFootprint.Square(BuildingFootprint.TownCenterTiles); // 6x6
            BuildingFootprint.Attach(go, footprint, carveObstacle: true);
            var tag = go.GetComponent<BuildingFootprintTag>();

            Vector3 direction = new Vector3(dirX, 0f, dirZ).normalized;
            Vector3 farAway = go.transform.position + direction * 50f;
            const float buffer = 0.5f;

            Vector3 approach = tag.GetNearestApproachPoint(farAway, buffer);

            // Obstacle half-extent is footprint/2 - Margin = 3 - 0.5 = 2.5
            // on both axes; the approach point should sit `buffer` beyond
            // that boundary along the same direction, and stay on the
            // correct (facing) side rather than the opposite one.
            float actualDistanceFromCenter = Vector3.Distance(go.transform.position, approach);
            Assert.AreEqual(expectedDistanceFromCenter, actualDistanceFromCenter, 0.01f);

            Vector3 actualDirection = (approach - go.transform.position).normalized;
            Assert.AreEqual(direction.x, actualDirection.x, 0.01f);
            Assert.AreEqual(direction.z, actualDirection.z, 0.01f);
        }

        [Test]
        public void GetNearestApproachPoint_NonCarvingBuilding_UsesRawFootprintEdge_NotMarginShrunk()
        {
            GameObject go = NewBuilding(Vector3.zero);
            Vector2 footprint = new Vector2(4f, 4f);
            BuildingFootprint.Attach(go, footprint, carveObstacle: false);
            var tag = go.GetComponent<BuildingFootprintTag>();

            Vector3 approach = tag.GetNearestApproachPoint(new Vector3(50f, 0f, 0f), buffer: 0f);

            // No obstacle carved, so the edge is the raw footprint half
            // (2), not footprint/2 - Margin (1.5) - confirms CarvesObstacle
            // actually gates the Margin subtraction rather than always
            // applying it.
            Assert.AreEqual(2f, Vector3.Distance(go.transform.position, approach), 0.01f);
        }
    }
}
