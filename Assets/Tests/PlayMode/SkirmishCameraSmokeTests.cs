using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Camera;

namespace KingdomsOfBharat.PlayModeTests
{
    // Repository-audit reproduction: Chola workers spawned around
    // x=-3..3, z=0, the main camera began near (-50, 18, -50), and with
    // fog enabled the Game view was almost entirely black - only revealed
    // by manually recentering the camera. This project has no PlayMode
    // suite at all yet (first one, hence the new
    // KingdomsOfBharat.PlayModeTests assembly alongside this file) - the
    // EditMode tests in RTSCameraControllerTests.cs cover the ground-point
    // math and FocusOnMatchStart's own behavior directly and don't need a
    // live scene, but only a real PlayMode run exercises the actual
    // production path this fix depends on: CivilizationSetup.BeginMatch
    // running Awake/Start/the first Update tick on the real scene's
    // TownCenterSpawner/UnitSpawner/RTSCameraController, in the real
    // order they run in during an actual match.
    //
    // Deliberately checks camera framing only (Camera.WorldToViewportPoint
    // against the map's own defined PlayerTownCenter), not fog-of-war
    // reveal state - fog is a separate rendering concern from where the
    // camera points, and the reported bug was specifically about camera
    // position ("manually centering the camera over the player spawn
    // exposed the starting settlement").
    public class SkirmishCameraSmokeTests
    {
        [UnityTest]
        public IEnumerator BeginningARiverValleySkirmish_MakesTheLocalPlayerStartVisible()
        {
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;

            CivilizationSetup civilizationSetup = Object.FindFirstObjectByType<CivilizationSetup>();
            Assert.IsNotNull(civilizationSetup, "Main scene must contain a CivilizationSetup component.");

            // Same entry point CivPicker's Confirm button calls - bypasses
            // only the UI click itself, not any of the match-start logic.
            civilizationSetup.BeginMatch(CivilizationId.Chola);
            yield return null;
            yield return null;

            RTSCameraController cameraController = Object.FindFirstObjectByType<RTSCameraController>();
            Assert.IsNotNull(cameraController, "Main scene must contain the RTS camera rig.");

            UnityEngine.Camera cam = cameraController.GetComponent<UnityEngine.Camera>();
            Assert.IsNotNull(cam);

            Vector3 localStart = MapRegistry.Current.PlayerTownCenter;
            Vector3 viewportPoint = cam.WorldToViewportPoint(localStart);

            Assert.Greater(viewportPoint.z, 0f, "The local player's start should be in front of the camera, not behind it.");
            Assert.GreaterOrEqual(viewportPoint.x, 0f, "The local player's start should be within the horizontal viewport bounds.");
            Assert.LessOrEqual(viewportPoint.x, 1f, "The local player's start should be within the horizontal viewport bounds.");
            Assert.GreaterOrEqual(viewportPoint.y, 0f, "The local player's start should be within the vertical viewport bounds.");
            Assert.LessOrEqual(viewportPoint.y, 1f, "The local player's start should be within the vertical viewport bounds.");
        }

        // Requirement 6 ("Ensure repeated matches reset the camera
        // correctly") through the real production path - the EditMode
        // tests in RTSCameraControllerTests.cs already cover this at the
        // FocusOnMatchStart level directly, but never through a second
        // real CivilizationSetup.BeginMatch call the way an actual
        // "Play Again in the same process" session would. Simulates a
        // player panning far away during the first match, then confirms a
        // second BeginMatch call - with no scene reload or component
        // teardown in between, exactly like this project's own repeated-
        // match precedent (see ProgressionRegistry.ResetAllForNewMatch) -
        // still lands correctly.
        [UnityTest]
        public IEnumerator BeginningASecondSkirmishInTheSameProcess_RefocusesDespitePriorCameraDrift()
        {
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;

            CivilizationSetup civilizationSetup = Object.FindFirstObjectByType<CivilizationSetup>();
            Assert.IsNotNull(civilizationSetup, "Main scene must contain a CivilizationSetup component.");

            civilizationSetup.BeginMatch(CivilizationId.Chola);
            yield return null;
            yield return null;

            RTSCameraController cameraController = Object.FindFirstObjectByType<RTSCameraController>();
            Assert.IsNotNull(cameraController, "Main scene must contain the RTS camera rig.");
            UnityEngine.Camera cam = cameraController.GetComponent<UnityEngine.Camera>();

            // Simulate the player having panned/zoomed far away by the
            // time a second match starts in this same process.
            cameraController.transform.position += new Vector3(200f, 10f, 200f);
            Vector3 driftedPosition = cameraController.transform.position;

            civilizationSetup.BeginMatch(CivilizationId.Maurya);
            yield return null;
            yield return null;

            Vector3 localStart = MapRegistry.Current.PlayerTownCenter;
            Vector3 viewportPoint = cam.WorldToViewportPoint(localStart);

            Assert.AreNotEqual(driftedPosition, cameraController.transform.position,
                "The second BeginMatch must move the camera off the drifted position, not leave it there.");
            Assert.Greater(viewportPoint.z, 0f, "The local player's start should be in front of the camera after the second match starts.");
            Assert.GreaterOrEqual(viewportPoint.x, 0f);
            Assert.LessOrEqual(viewportPoint.x, 1f);
            Assert.GreaterOrEqual(viewportPoint.y, 0f);
            Assert.LessOrEqual(viewportPoint.y, 1f);
        }
    }
}
