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
        private bool _harvesting;
        private bool _reseeding;

        // For SelectedUnitPanel (UI) to show a status line. IsFarming keeps
        // its pre-existing meaning (actively harvesting) for UnitStatus/
        // AnimationDriver compatibility.
        public bool IsFarming => _harvesting;

        // Item 5 (Renewable Resource): true while actively restoring a
        // depleted Farm's Food. See Update()'s autonomous switching.
        public bool IsReseeding => _reseeding;

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
            if (_harvesting && _farm != null)
            {
                _farm.StopWorking();
            }
            if (_reseeding && _farm != null)
            {
                _farm.StopReseed();
            }
            _harvesting = false;
            _reseeding = false;
            _farm = null;
        }

        // Item 5 (Renewable Resource): re-evaluates the farm's live
        // IsDepleted state every tick and switches between harvesting and
        // reseeding on its own - so the existing StaffAt() order (right-
        // click a Farm) naturally reseeds a depleted one and resumes
        // harvesting the moment it's full again, with no separate order
        // and no SelectionManager change needed.
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
            bool wantHarvest = inRange && !_farm.IsDepleted;
            bool wantReseed = inRange && _farm.IsDepleted;

            if (wantHarvest && !_harvesting)
            {
                _farm.BeginWorking();
                _harvesting = true;
            }
            else if (!wantHarvest && _harvesting)
            {
                _farm.StopWorking();
                _harvesting = false;
            }

            if (wantReseed && !_reseeding)
            {
                _farm.BeginReseed();
                _reseeding = true;
            }
            else if (!wantReseed && _reseeding)
            {
                _farm.StopReseed();
                _reseeding = false;
            }
        }

        private void OnDisable()
        {
            CancelWork();
        }
    }
}
