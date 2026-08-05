using UnityEngine;

namespace KingdomsOfBharat.Camera
{
    // Drives an RTS-style camera rig: WASD/arrow pan, screen-edge pan, and
    // scroll-wheel zoom (dolly along the camera's own height). Movement is
    // applied in world space so panning stays level regardless of the
    // camera's downward tilt.
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class RTSCameraController : MonoBehaviour
    {
        [Header("Pan")]
        [SerializeField] private float panSpeed = 20f;
        [SerializeField] private bool edgeScrollEnabled = true;
        [SerializeField] private float edgeScrollBorder = 12f;

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 400f;
        [SerializeField] private float minHeight = 8f;
        [SerializeField] private float maxHeight = 35f;

        [Header("Map Bounds")]
        [SerializeField] private Vector2 mapMin = new Vector2(-20f, -20f);
        [SerializeField] private Vector2 mapMax = new Vector2(20f, 20f);

        private void Update()
        {
            Vector3 move = GetKeyboardInput() + GetEdgeScrollInput();
            transform.Translate(move * panSpeed * Time.deltaTime, Space.World);
            ClampPosition();

            HandleZoom();
        }

        private static Vector3 GetKeyboardInput()
        {
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");
            return new Vector3(x, 0f, z);
        }

        private Vector3 GetEdgeScrollInput()
        {
            if (!edgeScrollEnabled)
            {
                return Vector3.zero;
            }

            Vector3 mousePos = Input.mousePosition;
            Vector3 move = Vector3.zero;

            if (mousePos.x <= edgeScrollBorder)
            {
                move.x -= 1f;
            }
            else if (mousePos.x >= Screen.width - edgeScrollBorder)
            {
                move.x += 1f;
            }

            if (mousePos.y <= edgeScrollBorder)
            {
                move.z -= 1f;
            }
            else if (mousePos.y >= Screen.height - edgeScrollBorder)
            {
                move.z += 1f;
            }

            return move;
        }

        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Approximately(scroll, 0f))
            {
                return;
            }

            Vector3 position = transform.position;
            position.y = Mathf.Clamp(position.y - scroll * zoomSpeed * Time.deltaTime, minHeight, maxHeight);
            transform.position = position;
        }

        private void ClampPosition()
        {
            Vector3 position = transform.position;
            position.x = Mathf.Clamp(position.x, mapMin.x, mapMax.x);
            position.z = Mathf.Clamp(position.z, mapMin.y, mapMax.y);
            transform.position = position;
        }
    }
}
