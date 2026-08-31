using UnityEngine;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Combat;

namespace KingdomsOfBharat.Buildings
{
    // Worker-side half of repair: walk to a damaged Repairable and count as
    // an active repairer for as long as it stays in range. Same shape as
    // Builder/ConstructionSite and FarmWorker/Farm - a Repairable with zero
    // active repairers makes no progress.
    [RequireComponent(typeof(UnitMover))]
    public class Repairer : MonoBehaviour
    {
        [SerializeField] private float interactionRange = 2.5f;

        private UnitMover _mover;
        private Repairable _target;
        private bool _repairing;

        // For SelectedUnitPanel/HoverTooltip (via UnitStatus) to show a
        // status line. True only while actually in range and contributing,
        // not while walking over.
        public bool IsRepairing => _repairing;

        private void Awake()
        {
            _mover = GetComponent<UnitMover>();
        }

        public void RepairAt(Repairable target)
        {
            CancelRepair();
            _target = target;
            _mover.MoveTo(target.transform.position);
        }

        public void CancelRepair()
        {
            if (_repairing && _target != null)
            {
                _target.StopRepair();
            }
            _repairing = false;
            _target = null;
        }

        private void Update()
        {
            if (_target == null)
            {
                return;
            }

            if (!_target.IsRepairable)
            {
                CancelRepair();
                return;
            }

            bool inRange = Vector3.Distance(transform.position, _target.transform.position) <= interactionRange;

            if (inRange && !_repairing)
            {
                _target.BeginRepair();
                _repairing = true;
            }
            else if (!inRange && _repairing)
            {
                _target.StopRepair();
                _repairing = false;
            }
        }

        private void OnDisable()
        {
            CancelRepair();
        }
    }
}
