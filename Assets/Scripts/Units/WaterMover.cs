using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Units
{
    // Item 49: the naval equivalent of UnitMover - same public MoveTo(Vector3)
    // shape so BoatGatherer/AttackMove-style code can drive a boat the same
    // way land code drives a NavMeshAgent, but boats don't have a NavMesh to
    // path on (see ProceduralGround - water is a literal hole in the land
    // mesh, not a walkable surface for any agent type). Moves in a straight
    // line toward the destination instead - no pathfinding around other
    // boats. Roadmap Section 1 fix: destinations are clamped to the current
    // map's water rectangle (WaterProximity.ClampToWater) before being
    // stored, so a move order/rally point/attack-move that lands on dry
    // land no longer sails the boat onto it - the water region is a single
    // convex rectangle today, so clamping the endpoint is enough to keep
    // the whole straight-line path inside it. Still a real limitation, not
    // hidden: no avoidance between boats, and a genuinely non-convex
    // coastline (islands, bays) would need real water-surface pathfinding -
    // not built here since no map defines a non-convex water shape yet.
    public class WaterMover : MonoBehaviour
    {
        [SerializeField] private float speed = 3f;
        [SerializeField] private float turnSpeed = 180f;
        [SerializeField] private float arrivalDistance = 0.3f;

        private Vector3 _destination;
        private bool _hasDestination;

        public void MoveTo(Vector3 destination)
        {
            _destination = WaterProximity.ClampToWater(destination);
            _hasDestination = true;
        }

        public void Stop()
        {
            _hasDestination = false;
        }

        public bool HasArrived => !_hasDestination;

        public Vector3 Destination => _destination;

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
