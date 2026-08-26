using UnityEngine;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Buildings
{
    // Roadmap Section 5 item 3: Durg Garrison-unit-side half of Garrison
    // staffing - walk to an owned Wall/Tower and enter it on arrival. Same
    // shape as FarmWorker, except entering is a one-shot transition (the
    // unit disables itself inside the building) rather than a continuous
    // in-range state, so there's no StopWorking-equivalent here - leaving
    // is Garrison.Ungarrison(), triggered from the building's side (see
    // BuildMenu's Ungarrison button), not from this component.
    [RequireComponent(typeof(UnitMover))]
    public class DurgGarrisonWorker : MonoBehaviour
    {
        [SerializeField] private float interactionRange = 2.5f;

        private UnitMover _mover;
        private Garrison _garrison;

        private void Awake()
        {
            _mover = GetComponent<UnitMover>();
        }

        public void GarrisonAt(Garrison garrison)
        {
            _garrison = garrison;
            _mover.MoveTo(garrison.transform.position);
        }

        public void CancelGarrison()
        {
            _garrison = null;
        }

        private void Update()
        {
            if (_garrison == null)
            {
                return;
            }

            if (Vector3.Distance(transform.position, _garrison.transform.position) <= interactionRange)
            {
                _garrison.TryGarrison(gameObject);
                _garrison = null;
            }
        }
    }
}
