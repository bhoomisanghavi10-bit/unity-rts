using UnityEngine;
using UnityEngine.AI;

namespace KingdomsOfBharat.Units
{
    // Thin wrapper over NavMeshAgent so command-issuing code (SelectionManager)
    // doesn't talk to the agent API directly.
    [RequireComponent(typeof(NavMeshAgent))]
    public class UnitMover : MonoBehaviour
    {
        private NavMeshAgent _agent;

        internal static bool SkipMovesOffNavMesh;

        // Resolved lazily, not cached in Awake - same "sibling component may
        // not exist yet" gotcha documented on GarrisonPoint/Repairable/
        // Gatherer.Mover: an EditMode test's AddComponent<UnitMover>() (or
        // the RequireComponent-driven add on Gatherer/MeleeAttacker owners)
        // doesn't guarantee Awake has run before MoveTo is called
        // synchronously right after.
        private NavMeshAgent Agent => _agent != null ? _agent : (_agent = GetComponent<NavMeshAgent>());

        // For callers (e.g. Gatherer's drop-off approach point) that need
        // to keep a computed target outside this agent's own body, not
        // just outside a building's carved obstacle.
        public float Radius => Agent.radius;

        public void MoveTo(Vector3 destination)
        {
            // Test fixtures with no NavMesh opt out of Unity's "not placed on
            // a NavMesh" error (existing tests still expect it by default).
            if (SkipMovesOffNavMesh && !Agent.isOnNavMesh)
            {
                return;
            }

            Agent.SetDestination(destination);
        }

        // Path state for order recovery (worker stuck detection).
        public bool IsPathInvalid => Agent.isOnNavMesh && !Agent.pathPending
            && Agent.pathStatus == NavMeshPathStatus.PathInvalid;

        public void Stop()
        {
            if (Agent.isOnNavMesh)
            {
                Agent.ResetPath();
            }
        }

        // Nearest walkable point to p (false when none within radius).
        public bool TrySnap(Vector3 p, float radius, out Vector3 snapped)
        {
            if (NavMesh.SamplePosition(p, out NavMeshHit hit, radius, Agent.areaMask))
            {
                snapped = hit.position;
                return true;
            }

            snapped = p;
            return false;
        }

        // True only when a COMPLETE path from here to p exists.
        public bool CanReach(Vector3 p)
        {
            // An agent that is not on a NavMesh (EditMode fixtures, or not yet
            // placed) cannot judge reachability, so it must not veto targets.
            if (!Agent.isOnNavMesh)
            {
                return true;
            }

            if (!TrySnap(p, 3f, out Vector3 target))
            {
                return false;
            }

            var path = new NavMeshPath();
            return NavMesh.CalculatePath(transform.position, target, Agent.areaMask, path)
                && path.status == NavMeshPathStatus.PathComplete;
        }
    }
}
