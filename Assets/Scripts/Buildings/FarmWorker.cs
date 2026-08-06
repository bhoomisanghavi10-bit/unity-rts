using UnityEngine;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Buildings
{
    // Worker-side half of Farm staffing: walk to a Farm and count as an
    // active worker for as long as it stays in range. Same shape as
    // Builder/ConstructionSite - a Farm with zero staff produces nothing.
    [RequireComponent(typeof(UnitMover))]
    public class FarmWorker : MonoBehaviour
    {
        [SerializeField] private float interactionRange = 2.5f;

        private UnitMover _mover;
        private Farm _farm;
        private bool _working;

        // For SelectedUnitPanel (UI) to show a status line.
        public bool IsFarming => _working;

        private void Awake()
        {
            _mover = GetComponent<UnitMover>();
        }

        public void StaffAt(Farm farm)
        {
            CancelWork();
            _farm = farm;
            _mover.MoveTo(farm.transform.position);
        }

        public void CancelWork()
        {
            if (_working && _farm != null)
            {
                _farm.StopWorking();
            }
            _working = false;
            _farm = null;
        }

        private void Update()
        {
            if (_farm == null)
            {
                return;
            }

            if (!_farm.IsComplete)
            {
                return; // wait for construction to finish (Builder's job)
            }

            bool inRange = Vector3.Distance(transform.position, _farm.transform.position) <= interactionRange;

            if (inRange && !_working)
            {
                _farm.BeginWorking();
                _working = true;
            }
            else if (!inRange && _working)
            {
                _farm.StopWorking();
                _working = false;
            }
        }

        private void OnDisable()
        {
            CancelWork();
        }
    }
}
