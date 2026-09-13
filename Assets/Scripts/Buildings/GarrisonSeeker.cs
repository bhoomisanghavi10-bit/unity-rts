using UnityEngine;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Buildings
{
    // Worker/soldier-side half of general garrisoning (Roadmap Section 1
    // "General garrisoning system", 2026-09-01) - generalizes what was
    // originally DurgGarrisonWorker (Maratha Durg Garrison unique-unit-only,
    // see git history) into a component every land unit factory adds, so
    // any eligible unit can be ordered to walk to and enter a friendly
    // GarrisonPoint. Same move-then-act shape as Builder/Repairer: walk
    // over, then a continuous Update() range check that fires the
    // one-shot "enter" transition once close enough - unlike
    // Builder/Repairer there's no ongoing in-range state to leave here,
    // since entering deactivates the unit entirely (see
    // GarrisonPoint.UngarrisonAll for the reverse, triggered from the
    // building's side via BuildMenu's Ungarrison button).
    [RequireComponent(typeof(UnitMover))]
    public class GarrisonSeeker : MonoBehaviour
    {
        [SerializeField] private float interactionRange = 2.5f;
        // Set true only by MarathaDurgGarrisonFactory (via Configure below)
        // - the sole in-game effect is satisfying a durgOnly GarrisonPoint
        // (Wall's) and triggering Attackable.SiegeImmune on the building
        // that unit enters (see GarrisonPoint.TryGarrison).
        [SerializeField] private bool grantsSiegeImmunity;

        private UnitMover _mover;
        private GarrisonPoint _target;
        private Vector3 _approachPoint;

        public bool GrantsSiegeImmunity => grantsSiegeImmunity;

        // Resolved lazily, not in Awake() - same "sibling component may not
        // exist yet" gotcha documented on Gatherer.Mover/GarrisonPoint.
        // Attackable/Repairable/MeleeAttacker.Self: an EditMode test's
        // AddComponent<GarrisonSeeker>() doesn't guarantee Awake has run on
        // the RequireComponent-added UnitMover before GarrisonAt is called
        // synchronously right after (Town Bell, Wave 6 item 33, is the
        // first caller to exercise GarrisonAt in exactly that context - no
        // prior test drove it end to end).
        private UnitMover Mover => _mover != null ? _mover : (_mover = GetComponent<UnitMover>());

        public void Configure(bool grantsSiegeImmunityValue)
        {
            grantsSiegeImmunity = grantsSiegeImmunityValue;
        }

        public void GarrisonAt(GarrisonPoint target)
        {
            _target = target;
            _approachPoint = ComputeApproachPoint(target);
            Mover.MoveTo(_approachPoint);
        }

        public void CancelGarrison()
        {
            _target = null;
        }

        private void Update()
        {
            if (_target == null)
            {
                return;
            }

            if (Vector3.Distance(transform.position, _approachPoint) <= interactionRange)
            {
                _target.TryGarrison(gameObject);
                _target = null;
            }
        }

        // The target building's own footprint (BuildingFootprint.Attach)
        // carves a NavMeshObstacle over its footprint - the raw
        // transform.position this used to move to (and range-check
        // against) sits inside that unwalkable space for any building
        // bigger than a couple tiles (TownCenter's 6-tile footprint is
        // wider than interactionRange itself), which a NavMeshAgent can
        // never actually reach and this component could then never
        // detect as "close enough." Same fix, same helper, as Gatherer's
        // drop-off approach (ComputeDropOffApproachPoint) - computed once
        // per order rather than every frame, matching that precedent.
        private Vector3 ComputeApproachPoint(GarrisonPoint target)
        {
            return target.TryGetComponent(out BuildingFootprintTag footprintTag)
                ? footprintTag.GetNearestApproachPoint(transform.position, Mover.Radius + 0.1f)
                : target.transform.position;
        }
    }
}
