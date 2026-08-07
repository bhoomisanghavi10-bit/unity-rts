using UnityEngine;

namespace KingdomsOfBharat.Core
{
    // Shared "find the actual ground height under this point" raycast,
    // used by HumanModelFactory (unit ground alignment, both at spawn and
    // continuously via GroundFollower) and AiController (building
    // placement). Deliberately does NOT rely on a Physics Layer to filter
    // hits: a newly-added custom Layer (see ProjectSettings/TagManager.asset)
    // only takes effect in an already-open Unity Editor session after a
    // full restart, not just a script recompile - relying on that silently
    // degrades to "hit anything" whenever the Editor hasn't been restarted
    // since the layer was added. That's exactly what turned
    // GroundFollower's continuous per-frame correction into a runaway
    // feedback loop between nearby units in testing: each one's raycast
    // hit the OTHER unit's collider instead of the ground, each aligning
    // to stand on top of the other, every frame, launching both skyward.
    //
    // Instead, this raycasts unfiltered with RaycastAll (a plain Raycast
    // only returns the closest hit, which could easily be a unit/building
    // standing between the ray's start and the actual ground) and only
    // accepts a hit whose GameObject is literally named "Ground" -
    // matching NavMeshBaker's own groundObjectName convention. Works
    // immediately after a recompile, no Editor-side setup required, and
    // can never mistake a unit or building for the ground.
    public static class GroundReference
    {
        private const string GroundObjectName = "Ground";

        public static bool TryGetHeight(Vector3 xzPoint, out float height, float rayStartHeight = 50f, float rayDistance = 100f)
        {
            Vector3 origin = new Vector3(xzPoint.x, xzPoint.y + rayStartHeight, xzPoint.z);
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, rayDistance);

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.gameObject.name == GroundObjectName)
                {
                    height = hit.point.y;
                    return true;
                }
            }

            height = 0f;
            return false;
        }
    }
}
