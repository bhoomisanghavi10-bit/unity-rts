using UnityEngine;

namespace KingdomsOfBharat.Units
{
    // Item 49: the naval equivalent of UnitMover - same public MoveTo(Vector3)
    // shape so BoatGatherer/AttackMove-style code can drive a boat the same
    // way land code drives a NavMeshAgent, but boats don't have a NavMesh to
    // path on (see ProceduralGround - water is a literal hole in the land
    // mesh, not a walkable surface for any agent type). Moves in a straight
    // line toward the destination instead - no obstacle avoidance, no
    // pathfinding around other boats. A real limitation, not hidden: fine
    // for a first pass on a simple rectangular water body, would need actual
    // water-surface pathfinding (a second NavMesh agent type baked over the
    // water region) to hold up on a complex coastline.
    public class WaterMover : MonoBehaviour
    {
        [SerializeField] private float speed = 3f;
        [SerializeField] private float turnSpeed = 180f;
        [SerializeField] private float arrivalDistance = 0.3f;

        private Vector3 _destination;
        private bool _hasDestination;

        public void MoveTo(Vector3 destination)
        {
            _destination = destination;
            _hasDestination = true;
        }

        public void Stop()
        {
            _hasDestination = false;
        }

        public bool HasArrived => !_hasDestination;

        private void Update()
        {
            if (!_hasDestination)
            {
                return;
            }

            Vector3 toTarget = _destination - transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude <= arrivalDistance)
            {
                _hasDestination = false;
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            transform.position += toTarget.normalized * speed * Time.deltaTime;
        }
    }
}
