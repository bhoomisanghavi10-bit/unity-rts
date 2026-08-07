using UnityEngine;
using UnityEngine.AI;

namespace KingdomsOfBharat.Wildlife
{
    // NavMeshAgent's own built-in rotation (agent.updateRotation, the
    // default) can wobble/oscillate at low speed and while the path is
    // being reset frequently (WildBoar.Engage() used to clear it every
    // tick while holding ground) - reported as the boar spinning,
    // jittering, and facing the wrong way while walking. Takes rotation
    // over manually instead: smoothly turns the root to face the agent's
    // actual movement direction, and only turns at all while really
    // moving, so it can never wobble while stationary.
    public class AnimalFacing : MonoBehaviour
    {
        [SerializeField] private float turnSpeedDegrees = 360f;
        [SerializeField] private float minSpeedToTurn = 0.05f;

        private NavMeshAgent _agent;

        public void Configure(NavMeshAgent agent)
        {
            _agent = agent;
            _agent.updateRotation = false;
        }

        private void LateUpdate()
        {
            if (_agent == null)
            {
                return;
            }

            Vector3 velocity = _agent.velocity;
            velocity.y = 0f;
            if (velocity.sqrMagnitude < minSpeedToTurn * minSpeedToTurn)
            {
                return;
            }

            Quaternion target = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeedDegrees * Time.deltaTime);
        }
    }
}
