using UnityEngine;

namespace KingdomsOfBharat.Units
{
    // Keeps the visual model's local Y offset continuously re-aligned to
    // the actual terrain height beneath wherever the ROOT currently is,
    // instead of only computing that alignment once at spawn time (see
    // HumanModelFactory.AlignFeetToGround). NavMeshAgent independently
    // moves the root every frame and normally tracks the baked NavMesh's
    // own height, but that can drift out of sync with the real ground
    // mesh under some conditions - seen in testing: a Worker correctly
    // grounded at spawn started visibly floating after a couple of
    // gather/drop-off round trips near a building. Recomputing this every
    // frame rather than trusting a one-time value matches this project's
    // established "recompute, don't incrementally track" convention (see
    // FogOfWarManager/Population) and fixes the drift regardless of
    // exactly what caused the root/ground mismatch to grow.
    public class GroundFollower : MonoBehaviour
    {
        private const float GroundClearance = 0.08f;

        private Transform _model;

        public void Configure(Transform model)
        {
            _model = model;
        }

        private void LateUpdate()
        {
            if (_model == null)
            {
                return;
            }

            Renderer[] renderers = _model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            Vector3 rootPosition = transform.position;
            Vector3 origin = new Vector3(rootPosition.x, rootPosition.y + 20f, rootPosition.z);
            int mask = LayerMask.GetMask("Ground");
            if (mask == 0)
            {
                mask = ~0;
            }

            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 100f, mask))
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            float correction = hit.point.y + GroundClearance - bounds.min.y;
            _model.position += new Vector3(0f, correction, 0f);
        }
    }
}
