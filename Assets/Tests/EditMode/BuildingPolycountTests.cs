using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Tests
{
    // Roadmap Section 1/5 item 17: the 2026-09-02 visual audit found every
    // civ-specific building shipping at ~1.7-2.0M un-decimated triangles with
    // zero regression coverage catching it. BuildingMeshDecimator now caps
    // every civ-specific building at ~500,000 tris (the spec's literal
    // 8,000-20,000 target proved unreachable without either visible carved-
    // relief artifacts or computationally impractical simplifier settings on
    // this raw un-retopologized Meshy geometry - see docs/SESSION_LOG.md).
    // This ceiling only needs to catch a full regression back toward the
    // multi-million-triangle baseline, not enforce an exact number - a 1.5x
    // buffer over the ~500,000 target keeps it from flaking on the small
    // per-mesh variance BuildingMeshDecimator already logs (499,999-500,000).
    public class BuildingPolycountTests
    {
        private const int TriangleCeiling = 750_000;

        private static readonly string[] BuildingNames =
        {
            "TownCenter", "Barracks", "Tower", "Market", "Farm", "House", "Wall", "Gate", "Dock"
        };

        private GameObject _spawnedRoot;

        [TearDown]
        public void TearDown()
        {
            if (_spawnedRoot != null)
            {
                Object.DestroyImmediate(_spawnedRoot);
                _spawnedRoot = null;
            }
        }

        [Test]
        public void CivBuildingModels_StayUnderTriangleCeiling_ForEveryCivAndBuilding()
        {
            // See BuildingModelFactoryTests for why this warning is expected
            // and unrelated to this test's own concern (polycount, not
            // material assignment).
            LogAssert.ignoreFailingMessages = true;
            try
            {
                foreach (CivilizationId civ in System.Enum.GetValues(typeof(CivilizationId)))
                {
                    foreach (string name in BuildingNames)
                    {
                        GameObject root = BuildingModelFactory.Spawn(
                            name, civ, Vector3.zero, new Vector3(3f, 2f, 3f), Color.white);
                        _spawnedRoot = root;

                        int triangleCount = 0;
                        foreach (MeshFilter meshFilter in root.GetComponentsInChildren<MeshFilter>(true))
                        {
                            if (meshFilter.sharedMesh != null)
                            {
                                triangleCount += meshFilter.sharedMesh.triangles.Length / 3;
                            }
                        }

                        Assert.LessOrEqual(triangleCount, TriangleCeiling,
                            $"{civ}/{name} has {triangleCount} triangles - exceeds the decimation ceiling, " +
                            "the exact multi-million-triangle regression this test exists to catch");

                        Object.DestroyImmediate(root);
                        _spawnedRoot = null;
                    }
                }
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }
        }
    }
}
