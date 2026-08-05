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

        public void MoveTo(Vector3 destination)
        {
            _agent.SetDestination(destination);
        }
    }
}
