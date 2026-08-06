using UnityEngine;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Wildlife
{
    // Worker-side "milking" component: walk to a Livestock animal and
    // produce Food for as long as it stays in range. Same "must be
    // present" shape as Builder/FarmWorker, except the target itself can
    // wander off, so staying in range may take re-chasing periodically.
    [RequireComponent(typeof(UnitMover))]
    public class LivestockWorker : MonoBehaviour
    {
        [SerializeField] private float interactionRange = 2f;
        [SerializeField] private float foodPerSecond = 0.4f;

        private UnitMover _mover;
        private Livestock _target;

        // For SelectedUnitPanel (UI) to show a status line.
        public bool IsMilking { get; private set; }

        private void Awake()
        {
            _mover = GetComponent<UnitMover>();
        }

        public void StaffAt(Livestock target)
        {
            _target = target;
            IsMilking = false;
            _mover.MoveTo(target.transform.position);
        }

        public void CancelWork()
        {
            _target = null;
            IsMilking = false;
        }

        private void Update()
        {
            if (_target == null)
            {
                return;
            }

            bool inRange = Vector3.Distance(transform.position, _target.transform.position) <= interactionRange;
            if (!inRange)
            {
                IsMilking = false;
                _mover.MoveTo(_target.transform.position);
                return;
            }

            IsMilking = true;
            FactionId faction = TryGetComponent(out FactionMember factionMember)
                ? factionMember.Faction
                : FactionId.Player;
            ResourceStockpile.For(faction).Add(ResourceType.Food, foodPerSecond * Time.deltaTime);
        }

        private void OnDisable()
        {
            CancelWork();
        }
    }
}
