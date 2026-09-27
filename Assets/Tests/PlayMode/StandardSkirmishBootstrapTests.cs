using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Camera;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.FogOfWar;

namespace KingdomsOfBharat.PlayModeTests
{
    // The project's first meaningful PlayMode suite (previously the
    // PlayMode runner discovered zero tests at all): a single, reliable
    // end-to-end verification that a standard skirmish actually bootstraps
    // through the real production path - map/civs applied, both factions'
    // starting forces present, the camera correctly focused (see
    // SkirmishCameraSmokeTests.cs for that fix's own narrower coverage),
    // and the player's own start actually revealed through fog, not just
    // framed by the camera. Every check below reads observable state via
    // real production APIs (CivilizationSetup, MapRegistry,
    // CivilizationRegistry, Population, Building.All/Unit.All,
    // RTSCameraController, FogOfWarManager) - nothing here re-derives or
    // hardcodes a value production code already owns (e.g. starting worker
    // counts, which map/factory logic decides).
    public class StandardSkirmishBootstrapTests
    {
        [UnityTest]
        public IEnumerator StandardRiverValleySkirmish_BootstrapsCorrectly()
        {
            // Requirement 7 ("no unexpected Error or Exception log"): this
            // is deliberately just the Unity Test Framework's own default
            // behavior, not something asserted explicitly - a [UnityTest]
            // already fails automatically the instant any unhandled
            // LogType.Error/Exception/Assert message is logged during it,
            // unless explicitly swallowed via LogAssert.Expect (which this
            // test never calls). Confirmed false here rather than left
            // implicit, since a previous test file in this same suite
            // could otherwise have left it true.
            LogAssert.ignoreFailingMessages = false;

            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;

            CivilizationSetup civilizationSetup = Object.FindFirstObjectByType<CivilizationSetup>();
            Assert.IsNotNull(civilizationSetup, "Main scene must contain a CivilizationSetup component.");

            civilizationSetup.SetMap(MapId.RiverValley);
            const CivilizationId playerCivilization = CivilizationId.Chola;

            // Same entry point CivPicker's Confirm button calls - the real
            // production initialization path (requirement 1), not a
            // reimplementation of what it does.
            civilizationSetup.BeginMatch(playerCivilization);

            // Two frames, not a real-time wait: a newly-activated
            // GameObject's Start() (TownCenterSpawner/UnitSpawner/
            // AiController) only runs on the frame after activation, so
            // one frame is the minimum; a second is the same proven-
            // reliable margin SkirmishCameraSmokeTests.cs already uses.
            // Deliberately kept this short rather than a WaitForSeconds
            // here: this MCP-driven headless Editor has no real mouse, so
            // Input.mousePosition reads a synthetic off-screen value
            // (confirmed live: y far below 0) that RTSCameraController's
            // own pre-existing edge-scroll logic reads as "pinned at the
            // screen edge," panning the camera every frame it's given to
            // run - a longer real-time wait before checking requirement 5
            // would make this test flaky in exactly this environment for
            // a reason that has nothing to do with the match bootstrap
            // itself. Checking camera framing within a couple of frames of
            // BeginMatch keeps that drift negligible.
            yield return null;
            yield return null;

            // 1. A match can start through the production initialization path.
            Assert.IsTrue(CivilizationSetup.HasMatchStarted, "BeginMatch should mark the match as started.");

            // 2. The selected map and civilizations are applied.
            Assert.AreEqual(MapId.RiverValley, MapRegistry.CurrentId, "The map picked via SetMap should be the active one.");
            Assert.IsTrue(CivilizationRegistry.IsAssigned(FactionId.Player));
            Assert.AreEqual(playerCivilization, CivilizationRegistry.For(FactionId.Player));
            Assert.IsTrue(CivilizationRegistry.IsAssigned(FactionId.Enemy));
            Assert.AreNotEqual(
                CivilizationRegistry.For(FactionId.Player), CivilizationRegistry.For(FactionId.Enemy),
                "Player and Enemy must resolve to distinct civilizations (ResolveDistinctCivilization's own contract).");

            // 3. The local player receives the expected Town Center and starting workers.
            TownCenter playerTownCenter = FindTownCenterFor(FactionId.Player);
            Assert.IsNotNull(playerTownCenter, "Player should have exactly one Town Center after BeginMatch.");
            Assert.Greater(Population.Current(FactionId.Player), 0, "Player should have starting population.");
            Assert.IsTrue(HasWorker(FactionId.Player), "Player should have at least one starting Worker.");

            // 4. The enemy receives its expected starting forces.
            TownCenter enemyTownCenter = FindTownCenterFor(FactionId.Enemy);
            Assert.IsNotNull(enemyTownCenter, "Enemy should have its own Town Center after BeginMatch.");
            Assert.Greater(Population.Current(FactionId.Enemy), 0, "Enemy should have starting population.");
            Assert.IsTrue(HasWorker(FactionId.Enemy), "Enemy should have at least one starting Worker.");

            // 5. The camera focuses near the local player's actual start.
            RTSCameraController cameraController = Object.FindFirstObjectByType<RTSCameraController>();
            Assert.IsNotNull(cameraController, "Main scene must contain the RTS camera rig.");
            UnityEngine.Camera cam = cameraController.GetComponent<UnityEngine.Camera>();
            Assert.IsNotNull(cam);

            Vector3 localStart = MapRegistry.Current.PlayerTownCenter;
            Vector3 viewportPoint = cam.WorldToViewportPoint(localStart);
            Assert.Greater(viewportPoint.z, 0f, "The local player's start should be in front of the camera.");
            Assert.GreaterOrEqual(viewportPoint.x, 0f, "The local player's start should be within the horizontal viewport bounds.");
            Assert.LessOrEqual(viewportPoint.x, 1f, "The local player's start should be within the horizontal viewport bounds.");
            Assert.GreaterOrEqual(viewportPoint.y, 0f, "The local player's start should be within the vertical viewport bounds.");
            Assert.LessOrEqual(viewportPoint.y, 1f, "The local player's start should be within the vertical viewport bounds.");

            // 6. The local starting area is visible through fog - a
            // separate concern from #5 (camera framing): the player's own
            // Town Center must actually read as revealed (not fogged
            // black), which is what FogOfWarManager itself is responsible
            // for, not the camera. Checked last and after its own real-
            // time wait (its recomputeInterval is 0.25s, so a frame count
            // alone isn't enough) since fog state doesn't depend on the
            // camera at all - deferring this wait to here, after camera
            // framing is already captured, keeps requirement 5's check
            // clear of the edge-scroll drift window explained above.
            yield return new WaitForSeconds(0.3f);

            FogOfWarManager fog = Object.FindFirstObjectByType<FogOfWarManager>();
            Assert.IsNotNull(fog, "Main scene must contain a FogOfWarManager.");
            Assert.IsTrue(fog.IsVisible(playerTownCenter.transform.position), "The player's own starting area should be revealed through fog, not hidden.");

            // 7. No unexpected Error or Exception log is emitted - covered
            // by the framework's own default fail-on-unexpected-log
            // behavior (see the LogAssert.ignoreFailingMessages comment
            // above); reaching this line with the test still running means
            // it held.

            // 8. Cleanup: see TearDown below.
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Requirement 8 ("cleanup leaves static registries and scene
            // state ready for the next test"). Reloading the scene
            // destroys every GameObject this test's match spawned -
            // Unit.All/Building.All empty themselves via their own
            // OnDisable (see Unit.cs/Building.cs), and the old
            // CivilizationSetup instance's OnDestroy fires, which already
            // resets HasMatchStarted/ScenarioManager/CustomScenarioContext/
            // NetworkMatch on its own. MapRegistry.Select restores this
            // class's own documented default (RiverValley) so a later
            // reader that inspects MapRegistry.Current before calling
            // BeginMatch itself sees the conventional baseline rather than
            // whatever map this test picked.
            //
            // Deliberately NOT resetting CivilizationRegistry/AgeProgress/
            // ProgressionRegistry's per-faction state here: every one of
            // them is already unconditionally overwritten by the very next
            // real BeginMatch call (ProgressionRegistry.ResetAllForNewMatch
            // exists precisely so two matches in one process can never leak
            // progression between them) - every test in this suite starts
            // by calling BeginMatch itself, so none of them depend on this
            // TearDown to leave those particular registries pre-cleared.
            //
            // What this TearDown can't guarantee: an EditMode test run
            // immediately after this PlayMode session, with no domain
            // reload in between, could still observe these same statics'
            // leftover values if it reads them directly without driving its
            // own BeginMatch-equivalent setup first - a pre-existing,
            // documented Unity/Editor behavior (exiting Play Mode does not
            // itself force a domain reload), not something any single
            // test's teardown can control from inside the Play session.
            SceneManager.LoadScene("Main");
            yield return null;

            MapRegistry.Select(MapId.RiverValley);
        }

        private static TownCenter FindTownCenterFor(FactionId faction)
        {
            foreach (Building building in Building.All)
            {
                if (building is TownCenter townCenter
                    && building.TryGetComponent(out FactionMember member)
                    && member.Faction == faction)
                {
                    return townCenter;
                }
            }

            return null;
        }

        // Same "Worker == a Unit with a Gatherer component" marker this
        // project already establishes elsewhere (TownBell/
        // IdleWorkerFinder/ScoreProgress all identify workers this way),
        // not a re-derivation of it.
        private static bool HasWorker(FactionId faction)
        {
            foreach (Unit unit in Unit.All)
            {
                if (unit.TryGetComponent(out FactionMember member) && member.Faction == faction
                    && unit.TryGetComponent(out Gatherer _))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
