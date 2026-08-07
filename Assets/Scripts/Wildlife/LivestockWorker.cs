using UnityEngine;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Wildlife
{
    // Worker-side "milking" component: walk to a Livestock animal and
    // produce Food for as long as it stays in range. The target itself no
    // longer wanders while actively being milked (see Livestock.
    // IsBeingMilked), so once contact is made this no longer needs to
    // re-chase - only the initial approach can require movement.
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
            if (_target != null)
            {
                _target.IsBeingMilked = false;
            }

            _target = target;
            IsMilking = false;
            _mover.MoveTo(target.transform.position);
        }

        public void CancelWork()
        {
            if (_target != null)
            {
                _target.IsBeingMilked = false;
            }

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
                _target.IsBeingMilked = false;
                _mover.MoveTo(_target.transform.position);
                return;
            }

            IsMilking = true;
            _target.IsBeingMilked = true;
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
