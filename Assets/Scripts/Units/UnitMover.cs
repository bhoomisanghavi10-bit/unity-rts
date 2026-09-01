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
            Agent.SetDestination(destination);
        }
    }
}
