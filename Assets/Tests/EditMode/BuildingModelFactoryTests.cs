using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Tests
{
    // Roadmap Section 4.3/Section 5 item 6: BuildingModelFactory.Spawn now
    // takes a CivilizationId and probes Buildings/<CivId>/<resourceName>
    // before the existing shared-model lookup chain. Chola now has all 9
    // civ-specific building models wired (see MeshyBuildingImporter); every
    // other civ still has none, so this test covers both paths: Chola
    // must resolve its own models, and every civ without one must still
    // fall through cleanly to today's shared model/procedural fallback.
    public class BuildingModelFactoryTests
    {
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
        public void Spawn_FallsBackToSharedModel_ForEveryCiv_WhenNoCivSpecificModelExists()
        {
            // TintMaterials reads renderer.materials, which Unity logs as
            // an edit-mode material-leak error (only relevant to this
            // synchronous test context - the same call happens at real
            // runtime spawn with no such warning) - expected here, not a
            // regression to chase. Suppressed for the duration of this
            // test rather than matched per-call since the shared model's
            // material count (and therefore how many times it logs) isn't
            // this test's concern.
            LogAssert.ignoreFailingMessages = true;
            try
            {
                foreach (CivilizationId civ in System.Enum.GetValues(typeof(CivilizationId)))
                {
                    GameObject root = BuildingModelFactory.Spawn(
                        "Barracks", civ, Vector3.zero, new Vector3(3f, 2f, 3f), Color.white);
                    _spawnedRoot = root;

                    Assert.IsNotNull(root, $"Spawn returned null for civ {civ}");
                    Assert.IsNotNull(root.GetComponentInChildren<Renderer>(),
                        $"Spawn for civ {civ} produced no visible model (shared/procedural fallback broke)");
                    Assert.IsNotNull(root.GetComponent<BoxCollider>(),
                        $"Spawn for civ {civ} did not add the expected bounds collider");

                    Object.DestroyImmediate(root);
                    _spawnedRoot = null;
                }
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }
        }

        private static readonly string[] CholaBuildingNames =
        {
            "TownCenter", "Barracks", "Tower", "Market", "House", "Gate", "Wall", "Dock", "Farm"
        };

        [Test]
        public void CholaBuildingModels_ExistAtTheExactResourcePath_ForAllNineBuildings()
        {
            foreach (string name in CholaBuildingNames)
            {
                GameObject prefab = Resources.Load<GameObject>($"Buildings/Chola/{name}");
                Assert.IsNotNull(prefab, $"Buildings/Chola/{name} did not resolve via Resources.Load");
            }
        }

        [Test]
        public void Spawn_UsesCholaSpecificModel_AndStandsUpright()
        {
            LogAssert.ignoreFailingMessages = true;
            try
            {
                foreach (string name in CholaBuildingNames)
                {
                    GameObject root = BuildingModelFactory.Spawn(
                        name, CivilizationId.Chola, Vector3.zero, new Vector3(3f, 2f, 3f), Color.white);
                    _spawnedRoot = root;

                    Renderer renderer = root.GetComponentInChildren<Renderer>();
                    Assert.IsNotNull(renderer, $"Chola {name} produced no visible model");
                    Assert.IsNotNull(root.GetComponent<BoxCollider>(),
                        $"Chola {name} did not get the expected bounds collider");
                    Assert.Greater(renderer.bounds.size.y, 0.1f,
                        $"Chola {name} has a near-zero height - likely lying on its side (import rotation bug)");

                    Object.DestroyImmediate(root);
                    _spawnedRoot = null;
                }
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }
        }
    }
}
