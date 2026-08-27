using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Tests
{
    // Roadmap Section 4.3/Section 5 item 6: BuildingModelFactory.Spawn now
    // takes a CivilizationId and probes Buildings/<CivId>/<resourceName>
    // before the existing shared-model lookup chain. No civ-specific
    // building models are sourced yet for any civ, so every civ must still
    // fall through cleanly to today's shared model/procedural fallback -
    // that's the regression this covers, not the (not-yet-existing)
    // civ-specific asset path itself.
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
    }
}
