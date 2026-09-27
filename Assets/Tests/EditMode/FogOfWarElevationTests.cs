using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.FogOfWar;

namespace KingdomsOfBharat.Tests
{
    // AoE II fog-of-war spec (docs handed in 2026-09-28): elevation sight
    // rules and the hard 20-tile vision cap. HasElevationLineOfSight can't
    // be exercised through the real BuildElevationGrid pass in EditMode (no
    // live Physics scene/terrain to raycast against), so these drive it
    // directly against a synthetic elevation grid via the
    // ConfigureForTest/HasElevationLineOfSightForTest seam.
    public class FogOfWarElevationTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                Object.DestroyImmediate(go);
            }
            _spawned.Clear();
        }

        private FogOfWarManager NewManager(int gridSize, float[] elevationGrid)
        {
            var go = new GameObject("FogOfWarManagerTest");
            _spawned.Add(go);
            var manager = go.AddComponent<FogOfWarManager>();
            manager.ConfigureForTest(gridSize, elevationGrid);
            return manager;
        }

        [Test]
        public void FlatGround_HasLineOfSight_Everywhere()
        {
            const int size = 10;
            var elevation = new float[size * size]; // all zero
            FogOfWarManager manager = NewManager(size, elevation);

            bool sees = manager.HasElevationLineOfSightForTest(1, 1, 0f, 8, 8);

            Assert.IsTrue(sees);
        }

        [Test]
        public void LowerGround_LookingUp_IsBlockedByACliffAhead()
        {
            const int size = 10;
            var elevation = new float[size * size];
            // A cliff wall at x=4 for every z, well above the threshold.
            for (int z = 0; z < size; z++)
            {
                elevation[z * size + 4] = 10f;
            }
            FogOfWarManager manager = NewManager(size, elevation);

            // Source at x=1 (elevation 0) looking past the cliff at x=4
            // toward a target at x=8 on the far side.
            bool seesBeyondCliff = manager.HasElevationLineOfSightForTest(1, 5, 0f, 8, 5);

            Assert.IsFalse(seesBeyondCliff);
        }

        [Test]
        public void LowerGround_CanStillSeeTheCliffEdgeTileItself()
        {
            const int size = 10;
            var elevation = new float[size * size];
            elevation[5 * size + 4] = 10f; // cliff tile itself is the target
            FogOfWarManager manager = NewManager(size, elevation);

            bool seesTheCliffTile = manager.HasElevationLineOfSightForTest(1, 5, 0f, 4, 5);

            Assert.IsTrue(seesTheCliffTile);
        }

        [Test]
        public void HigherGround_SeesDownIntoLowerBasin_Unobstructed()
        {
            const int size = 10;
            var elevation = new float[size * size];
            // Source sits on a raised plateau; everything ahead is lower
            // ground, including a dip in the middle - none of it should
            // block sight from up top.
            elevation[5 * size + 2] = 10f;
            FogOfWarManager manager = NewManager(size, elevation);

            bool seesBasin = manager.HasElevationLineOfSightForTest(2, 5, 10f, 8, 5);

            Assert.IsTrue(seesBasin);
        }

        [Test]
        public void SmallElevationNoise_DoesNotFalselyBlockVision()
        {
            const int size = 10;
            var elevation = new float[size * size];
            // Gentle rolling terrain, nowhere near the cliff threshold.
            for (int i = 0; i < elevation.Length; i++)
            {
                elevation[i] = 0.4f;
            }
            FogOfWarManager manager = NewManager(size, elevation);

            bool sees = manager.HasElevationLineOfSightForTest(1, 1, 0.4f, 8, 8);

            Assert.IsTrue(sees);
        }

        [Test]
        public void VisionSource_ClampsConfiguredRadius_ToTheHardCap()
        {
            var go = new GameObject("VisionSourceTest");
            _spawned.Add(go);
            var source = go.AddComponent<VisionSource>();

            source.Configure(999f);

            Assert.AreEqual(VisionSource.HardCap, source.VisionRadius);
        }

        [Test]
        public void VisionSource_ClampsConfiguredRadius_ToAtLeastOne()
        {
            var go = new GameObject("VisionSourceTest");
            _spawned.Add(go);
            var source = go.AddComponent<VisionSource>();

            source.Configure(-5f);

            Assert.AreEqual(1f, source.VisionRadius);
        }
    }
}
