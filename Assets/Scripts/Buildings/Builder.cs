using UnityEngine;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Buildings
{
    // Worker-side half of construction: walk to an in-progress ConstructionSite
    // and count as an active builder for as long as it stays in range. A site
    // with zero active builders makes no progress (AoE-style: placing a
    // foundation doesn't build it, a worker actually has to be there).
    [RequireComponent(typeof(UnitMover))]
    public class Builder : MonoBehaviour
    {
        [SerializeField] private float interactionRange = 2.5f;

        private UnitMover _mover;
        private ConstructionSite _site;
        private bool _building;

        // For SelectedUnitPanel (UI) to show a status line. True only while
        // actually in range and contributing progress, not while walking over.
        public bool IsBuilding => _building;

        private void Awake()
        {
            _mover = GetComponent<UnitMover>();
        }

        public void BuildAt(ConstructionSite site)
        {
            CancelBuild();
            _site = site;
            _mover.MoveTo(site.transform.position);
        }

        public void CancelBuild()
        {
            if (_building && _site != null)
            {
                _site.StopBuilding();
            }
            _building = false;
            _site = null;
        }

        private void Update()
        {
            if (_site == null)
            {
                return;
            }

            if (_site.IsComplete)
            {
                CancelBuild();
                return;
            }

            bool inRange = Vector3.Distance(transform.position, _site.transform.position) <= interactionRange;

            if (inRange && !_building)
            {
                _site.BeginBuilding();
                _building = true;
            }
            else if (!inRange && _building)
            {
                _site.StopBuilding();
                _building = false;
            }
        }

        private void OnDisable()
        {
            CancelBuild();
        }
    }
}
