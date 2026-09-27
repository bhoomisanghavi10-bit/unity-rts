using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Camera;

namespace KingdomsOfBharat.Tests
{
    // Repository-audit reproduction: at match start the camera stayed
    // wherever the scene/CivPicker screen last left it, nowhere near the
    // local player's actual starting base - with fog enabled that reads as
    // an almost entirely black Game view. Covers RTSCameraController's new
    // match-start focus API: the pure ground-point math
    // (ComputeGroundFocusPosition) directly, and the instance-level
    // FocusOnMatchStart behavior (snap, viewport centering, velocity
    // reset, repeated-match correctness) against a real Camera component.
    public class RTSCameraControllerTests
    {
        // The scene's own Main Camera pitch (Assets/Scenes/Main.unity's
        // Main Camera transform: m_LocalEulerAnglesHint {x: 56.6, y: 0,
        // z: 0}) - used so these tests exercise the actual rig angle, not
        // an arbitrary one.
        private const float ScenePitchDegrees = 56.6f;

        private GameObject _cameraGo;
        private RTSCameraController _controller;

        [TearDown]
        public void TearDown()
        {
            if (_cameraGo != null)
            {
                Object.DestroyImmediate(_cameraGo);
            }
        }

        private RTSCameraController CreateController(float pitchDegrees, Vector3 startPosition)
        {
            _cameraGo = new GameObject("TestCamera", typeof(UnityEngine.Camera));
            _cameraGo.transform.position = startPosition;
            _cameraGo.transform.rotation = Quaternion.Euler(pitchDegrees, 0f, 0f);
            _controller = _cameraGo.AddComponent<RTSCameraController>();
            return _controller;
        }

        private static Vector3 GetVelocity(RTSCameraController controller)
        {
            FieldInfo field = typeof(RTSCameraController).GetField("_velocity", BindingFlags.NonPublic | BindingFlags.Instance);
            return (Vector3)field.GetValue(controller);
        }

        private static void SetVelocity(RTSCameraController controller, Vector3 value)
        {
            FieldInfo field = typeof(RTSCameraController).GetField("_velocity", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(controller, value);
        }

        private static Vector3 GetTargetPosition(RTSCameraController controller)
        {
            FieldInfo field = typeof(RTSCameraController).GetField("_targetPosition", BindingFlags.NonPublic | BindingFlags.Instance);
            return (Vector3)field.GetValue(controller);
        }

        // --- ComputeGroundFocusPosition (pure math) ---

        [Test]
        public void ComputeGroundFocusPosition_WithDownwardPitch_ForwardRayFromResultHitsGroundTarget()
        {
            Vector3 groundTarget = new Vector3(0f, 1f, 20f);
            float height = 18f;
            Vector3 forward = Quaternion.Euler(ScenePitchDegrees, 0f, 0f) * Vector3.forward;

            Vector3 result = RTSCameraController.ComputeGroundFocusPosition(groundTarget, height, forward);

            Assert.AreEqual(height, result.y, 1e-4f);

            // Re-derive the ray/ground intersection from the returned
            // position and confirm it lands exactly on groundTarget - the
            // actual contract this method exists to satisfy (requirement:
            // the ground target must be centered in the viewport, not
            // merely under the camera's own XZ position).
            float t = (result.y - groundTarget.y) / -forward.y;
            Vector3 hitPoint = result + t * forward;
            Assert.AreEqual(groundTarget.x, hitPoint.x, 1e-3f);
            Assert.AreEqual(groundTarget.y, hitPoint.y, 1e-3f);
            Assert.AreEqual(groundTarget.z, hitPoint.z, 1e-3f);
        }

        [Test]
        public void ComputeGroundFocusPosition_IsNotTheGroundTargetItself_ForADownwardPitchedCamera()
        {
            // Directly guards against the naive fix the requirements
            // explicitly forbid: "do not merely assign the camera
            // transform to the Town Center position." For a pure X-axis
            // pitch (this project's actual rig has zero yaw - see the
            // scene's own Main Camera transform), the along-forward axis
            // (Z here) must shift away from the raw ground target; X is
            // correctly unaffected since a zero-yaw pitch never moves
            // sideways relative to the target.
            Vector3 groundTarget = new Vector3(5f, 1f, -12f);
            Vector3 forward = Quaternion.Euler(ScenePitchDegrees, 0f, 0f) * Vector3.forward;

            Vector3 result = RTSCameraController.ComputeGroundFocusPosition(groundTarget, 18f, forward);

            Assert.AreEqual(groundTarget.x, result.x, 1e-4f);
            Assert.AreNotEqual(groundTarget.z, result.z);
        }

        [Test]
        public void ComputeGroundFocusPosition_WithYawAndPitch_ForwardRayFromResultStillHitsGroundTarget()
        {
            // General case: the derivation isn't specific to this
            // project's currently-zero-yaw rig. With yaw added, both X and
            // Z of the naive "assign transform to target" fix would be
            // wrong - this confirms the general ray/plane solve still
            // holds instead of just the zero-yaw special case above.
            Vector3 groundTarget = new Vector3(5f, 1f, -12f);
            Vector3 forward = Quaternion.Euler(ScenePitchDegrees, 40f, 0f) * Vector3.forward;
            float height = 25f;

            Vector3 result = RTSCameraController.ComputeGroundFocusPosition(groundTarget, height, forward);

            Assert.AreNotEqual(groundTarget.x, result.x);
            Assert.AreNotEqual(groundTarget.z, result.z);

            float t = (result.y - groundTarget.y) / -forward.y;
            Vector3 hitPoint = result + t * forward;
            Assert.AreEqual(groundTarget.x, hitPoint.x, 1e-3f);
            Assert.AreEqual(groundTarget.y, hitPoint.y, 1e-3f);
            Assert.AreEqual(groundTarget.z, hitPoint.z, 1e-3f);
        }

        [Test]
        public void ComputeGroundFocusPosition_ScalesWithHeight_TallerCameraSitsFartherFromTarget()
        {
            Vector3 groundTarget = Vector3.zero;
            Vector3 forward = Quaternion.Euler(ScenePitchDegrees, 0f, 0f) * Vector3.forward;

            Vector3 low = RTSCameraController.ComputeGroundFocusPosition(groundTarget, 10f, forward);
            Vector3 high = RTSCameraController.ComputeGroundFocusPosition(groundTarget, 40f, forward);

            // A taller camera at the same pitch has to sit further back
            // (more negative Z here, since forward points toward +Z) to
            // keep the same ground point centered.
            Assert.Less(high.z, low.z);
        }

        [Test]
        public void ComputeGroundFocusPosition_DegenerateZeroPitch_FallsBackToDirectGroundXZ()
        {
            // A camera with forward.y == 0 (looking perfectly along the
            // ground plane) can never intersect the ground ahead of it -
            // the division would be undefined/NaN. The guarded fallback is
            // the best-effort "old" behavior (direct XZ assignment) rather
            // than propagating a NaN into the transform.
            Vector3 groundTarget = new Vector3(3f, 1f, 7f);
            Vector3 forward = Vector3.forward; // no pitch at all

            Vector3 result = RTSCameraController.ComputeGroundFocusPosition(groundTarget, 18f, forward);

            Assert.AreEqual(groundTarget.x, result.x, 1e-4f);
            Assert.AreEqual(groundTarget.z, result.z, 1e-4f);
            Assert.AreEqual(18f, result.y, 1e-4f);
        }

        // --- FocusOnMatchStart (instance behavior) ---

        [Test]
        public void FocusOnMatchStart_SnapsTransformImmediately_NotJustTheEasedTarget()
        {
            RTSCameraController controller = CreateController(ScenePitchDegrees, new Vector3(-50f, 18f, -50f));
            Vector3 groundTarget = new Vector3(0f, 1f, 20f);

            controller.FocusOnMatchStart(groundTarget);

            Vector3 expected = RTSCameraController.ComputeGroundFocusPosition(groundTarget, 18f, controller.transform.forward);
            Assert.AreEqual(expected, controller.transform.position);
            Assert.AreEqual(expected, GetTargetPosition(controller));
        }

        [Test]
        public void FocusOnMatchStart_CentersTheGroundTarget_InTheCameraViewport()
        {
            RTSCameraController controller = CreateController(ScenePitchDegrees, new Vector3(-50f, 18f, -50f));
            Vector3 groundTarget = new Vector3(0f, 1f, 20f);

            controller.FocusOnMatchStart(groundTarget);

            UnityEngine.Camera cam = controller.GetComponent<UnityEngine.Camera>();
            Vector3 viewportPoint = cam.WorldToViewportPoint(groundTarget);

            Assert.Greater(viewportPoint.z, 0f, "Ground target should be in front of the camera.");
            Assert.AreEqual(0.5f, viewportPoint.x, 0.01f, "Ground target should be horizontally centered.");
            Assert.AreEqual(0.5f, viewportPoint.y, 0.01f, "Ground target should be vertically centered.");
        }

        [Test]
        public void FocusOnMatchStart_ResetsVelocity_SoNoOvershootFollowsTheSnap()
        {
            RTSCameraController controller = CreateController(ScenePitchDegrees, Vector3.zero);
            SetVelocity(controller, new Vector3(37f, 0f, -19f));

            controller.FocusOnMatchStart(new Vector3(0f, 1f, 20f));

            Assert.AreEqual(Vector3.zero, GetVelocity(controller));
        }

        [Test]
        public void FocusOnMatchStart_HeightIsClampedToConfiguredRange()
        {
            // minHeight/maxHeight default to 8/60 (RTSCameraController's own
            // Inspector defaults) - starting far outside that range (as a
            // stale pre-match camera position could be) must not produce a
            // focus height outside the configured zoom bounds.
            RTSCameraController controller = CreateController(ScenePitchDegrees, new Vector3(0f, 500f, 0f));

            controller.FocusOnMatchStart(new Vector3(0f, 1f, 20f));

            Assert.LessOrEqual(controller.transform.position.y, 60f);
            Assert.GreaterOrEqual(controller.transform.position.y, 8f);
        }

        [Test]
        public void FocusOnMatchStart_CalledAgainForASecondMatch_DiscardsThePriorFocus_NoLeakedState()
        {
            // Simulates repeated matches in one process (e.g. Play Again):
            // a fresh FocusOnMatchStart call must land exactly on the new
            // target regardless of where the previous match's call left
            // the camera, including leftover pan/zoom velocity from a
            // player moving the camera during the first match.
            RTSCameraController controller = CreateController(ScenePitchDegrees, new Vector3(-50f, 18f, -50f));

            Vector3 firstMatchTarget = new Vector3(0f, 1f, 20f); // e.g. River Valley
            controller.FocusOnMatchStart(firstMatchTarget);
            Vector3 afterFirstMatch = controller.transform.position;

            // Simulate the player panning/zooming during the first match.
            SetVelocity(controller, new Vector3(12f, 0f, 8f));
            controller.transform.position += new Vector3(30f, 5f, -10f);

            Vector3 secondMatchTarget = new Vector3(0f, 1f, 57f); // e.g. a Skirmish map
            controller.FocusOnMatchStart(secondMatchTarget);

            Assert.AreEqual(Vector3.zero, GetVelocity(controller), "Velocity from the first match must not survive into the second.");
            Assert.AreNotEqual(afterFirstMatch, controller.transform.position, "The camera must actually move for the second match's different target.");

            UnityEngine.Camera cam = controller.GetComponent<UnityEngine.Camera>();
            Vector3 viewportPoint = cam.WorldToViewportPoint(secondMatchTarget);
            Assert.AreEqual(0.5f, viewportPoint.x, 0.01f);
            Assert.AreEqual(0.5f, viewportPoint.y, 0.01f);
        }
    }
}
