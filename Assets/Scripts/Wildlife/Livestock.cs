using UnityEngine;
using UnityEngine.AI;

namespace KingdomsOfBharat.Wildlife
{
    // Domesticated livestock (cow) - grazes near its home spot and is
    // milked for Food (LivestockWorker), never hunted. Deliberately has no
    // Attackable component at all: cattle in this setting are a dairy
    // resource, not meat, and cannot be killed by any unit type - reflects
    // the food-culture context of an Indian-kingdoms setting.
    [RequireComponent(typeof(NavMeshAgent))]
    public class Livestock : MonoBehaviour
    {
        [SerializeField] private float wanderRadius = 3f;
        [SerializeField] private float wanderInterval = 5f;

        private NavMeshAgent _agent;
        private Vector3 _homePosition;
        private float _timer;

        // Set by LivestockWorker for as long as it's actively milking this
        // animal - wandering off mid-milk was forcing the worker to keep
        // re-chasing it every few seconds, which read as the cow refusing
        // to stay still. A cow being worked just stands there instead,
        // same as a Worker's own gather/build/farm targets don't wander.
        public bool IsBeingMilked { get; set; }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _homePosition = transform.position;
        }

        private void Update()
        {
            if (IsBeingMilked)
            {
                return;
            }

            _timer -= Time.deltaTime;
            if (_timer > 0f)
            {
                return;
            }

            _timer = wanderInterval + Random.Range(-1f, 1f);
            Vector2 offset = Random.insideUnitCircle * wanderRadius;
            _agent.SetDestination(_homePosition + new Vector3(offset.x, 0f, offset.y));
        }
    }
}
