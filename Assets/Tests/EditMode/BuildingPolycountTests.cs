using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;

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

        // 2026-09-15: found the Imperial-tier check above didn't cover the
        // age-tiered variants (TownCenter/Tower/Wall's Ancient/Classical/
        // Durg models - a wholly separate system from the Imperial-tier
        // prefabs, resolved via AgeTieredBuildingVisual/BuildingModelFactory's
        // age-suffixed Resources paths) - those shipped at ~1.9-3.0M
        // un-decimated triangles with zero coverage, even after the Imperial
        // pass above already had a regression test. BuildingMeshDecimator's
        // DecimateAllAgeTiered now caps these too; this test exists so a
        // future regression back to multi-million-triangle age-tiered models
        // fails loudly the same way the Imperial-tier one already does.
        //
        // Wall/Durg was previously excluded here: its old source FBX
        // (Buildings/_Source/Wall_Durg/Wall_Durg_model.fbx) appeared to have
        // a corrupted mesh - vertexCount/bounds both read 0 while
        // triangles.Length still reported ~3.08M stale indices. Re-diagnosed
        // 2026-09-16 after the user supplied a fresh source delivery that
        // showed the exact same symptom: manually parsing the raw FBX binary
        // (walking the node tree, zlib-inflating every Geometry data block -
        // Vertices/PolygonVertexIndex/Normals/UV) proved the file itself was
        // never corrupted - every block decompresses cleanly to its declared
        // size with sane, finite vertex data. The real cause was Unity's own
        // FBX importer: with the default isReadable=false/indexFormat=Auto/
        // weldVertices=true/optimizeMesh*=true settings, this particular
        // ~1.5M-vertex mesh imported with a populated triangle index buffer
        // but a 0-length vertex buffer. Forcing isReadable=true,
        // indexFormat=UInt32, and disabling weldVertices/optimizeMeshPolygons/
        // optimizeMeshVertices on the ModelImporter fixed it completely (real
        // geometry, correct world-space bounds, confirmed live). The prior
        // session's "confirmed via a fresh-GUID copy" conclusion only ever
        // re-tested the corrupted import result, not this importer-setting
        // combination together. BuildingMeshDecimator.cs itself was also
        // hardened (see its own comment) since MeshSimplifier.ToMesh() can
        // separately produce degenerate output with no thrown exception -
        // a second, independent failure mode hit on the very first re-run of
        // Wall_Ancient/Wall_Classical this same session, before Wall_Durg's
        // source was ever touched.
        [Test]
        public void AgeTieredBuildingModels_StayUnderTriangleCeiling_ForEveryCivAndAge()
        {
            LogAssert.ignoreFailingMessages = true;
            try
            {
                foreach (CivilizationId civ in System.Enum.GetValues(typeof(CivilizationId)))
                {
                    AssertAgeTieredUnderCeiling(civ, "TownCenter", AgeId.Ancient);
                    AssertAgeTieredUnderCeiling(civ, "TownCenter", AgeId.Classical);
                    AssertAgeTieredUnderCeiling(civ, "TownCenter", AgeId.Durg);
                    AssertAgeTieredUnderCeiling(civ, "Tower", AgeId.Ancient);
                    AssertAgeTieredUnderCeiling(civ, "Tower", AgeId.Classical);
                    AssertAgeTieredUnderCeiling(civ, "Tower", AgeId.Durg);
                    AssertAgeTieredUnderCeiling(civ, "Wall", AgeId.Ancient);
                    AssertAgeTieredUnderCeiling(civ, "Wall", AgeId.Classical);
                    AssertAgeTieredUnderCeiling(civ, "Wall", AgeId.Durg);
                }
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }
        }

        private void AssertAgeTieredUnderCeiling(CivilizationId civ, string name, AgeId age)
        {
            GameObject root = BuildingModelFactory.Spawn(
                name, civ, Vector3.zero, new Vector3(3f, 2f, 3f), Color.white, age);
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
                $"{civ}/{name}_{age} has {triangleCount} triangles - exceeds the decimation ceiling, " +
                "the exact multi-million-triangle regression this test exists to catch");

            Object.DestroyImmediate(root);
            _spawnedRoot = null;
        }
    }
}
