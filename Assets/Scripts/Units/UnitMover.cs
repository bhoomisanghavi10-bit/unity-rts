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

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        // For callers (e.g. Gatherer's drop-off approach point) that need
        // to keep a computed target outside this agent's own body, not
        // just outside a building's carved obstacle.
        public float Radius => _agent.radius;

        public void MoveTo(Vector3 destination)
        {
            _agent.SetDestination(destination);
        }
    }
}
