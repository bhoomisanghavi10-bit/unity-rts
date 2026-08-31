using System.Collections.Generic;
using UnityEngine;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Buildings
{
    // General garrisoning system (Roadmap Section 1 "General garrisoning
    // system" / worker mechanics audit, 2026-09-01) - generalizes what was
    // originally a single-slot Maratha Durg Garrison-only mechanic (this
    // class used to be named Garrison, see git history) into pooled
    // capacity any eligible friendly land unit can enter for safety
    // (garrisoned units are deactivated - untargetable/off the field).
    // Two configurations exist: Tower/TownCenter (durgOnly:false, any unit
    // with a GarrisonSeeker, capacity>1, feeds BuildingAttacker's
    // bonus-shot count) and Wall (durgOnly:true, capacity 1 - unchanged
    // Player-facing behavior from before this generalization, only a
    // siege-immunity-granting unit, i.e. the Maratha Durg Garrison unique
    // unit, may enter).
    public class GarrisonPoint : MonoBehaviour
    {
        private readonly List<GameObject> _occupants = new List<GameObject>();
        private int _capacity = 1;
        private bool _durgOnly;
        private int _siegeImmuneOccupantCount;

        public int Count => _occupants.Count;
        public int Capacity => _capacity;

        // Resolved lazily, not in Awake() - see Barracks' identical
        // comment on Site/FactionMember: AddComponent fires Awake()
        // immediately, before a factory has necessarily added every
        // sibling component yet, so an eager Awake()-time GetComponent
        // risks finding nothing.
        private Attackable Attackable => GetComponent<Attackable>();
        private BuildingFootprintTag FootprintTag => GetComponent<BuildingFootprintTag>();
        private RallyPoint Rally => GetComponent<RallyPoint>();

        public void Configure(int capacity, bool durgOnly = false)
        {
            _capacity = capacity;
            _durgOnly = durgOnly;
        }

        // durgOnly gates entry to only units carrying GarrisonSeeker.
        // GrantsSiegeImmunity (today, exclusively the Maratha Durg
        // Garrison unique unit) - Wall configures durgOnly:true so this
        // reproduces the exact single-slot, siege-immunity-only behavior
        // that existed before this class was generalized.
        public bool TryGarrison(GameObject unit)
        {
            if (unit == null || _occupants.Count >= _capacity)
            {
                return false;
            }

            bool grantsSiegeImmunity = unit.TryGetComponent(out GarrisonSeeker seeker) && seeker.GrantsSiegeImmunity;
            if (_durgOnly && !grantsSiegeImmunity)
            {
                return false;
            }

            _occupants.Add(unit);
            unit.SetActive(false);
            if (grantsSiegeImmunity)
            {
                _siegeImmuneOccupantCount++;
                Attackable?.SetSiegeImmune(true);
            }

            return true;
        }

        // Empties the whole garrison at once (the BuildMenu Ungarrison
        // button's action) - each occupant reactivates at the building's
        // nearest walkable edge (BuildingFootprintTag.GetNearestApproachPoint,
        // same helper Gatherer's drop-off approach uses, so a unit never
        // pops back inside carved-obstacle space) and, if the building has
        // a RallyPoint, resumes whatever it currently points at
        // (RallyPoint.ApplyTo - the only existing "what should a unit
        // leaving this building do next" mechanism in the codebase,
        // already used for freshly-trained units). No RallyPoint (Tower,
        // Wall) just means the unit steps outside and goes idle.
        public void UngarrisonAll()
        {
            foreach (GameObject unit in _occupants)
            {
                if (unit == null)
                {
                    continue;
                }

                Vector3 exitPoint = transform.position;
                if (FootprintTag != null && unit.TryGetComponent(out UnitMover mover))
                {
                    exitPoint = FootprintTag.GetNearestApproachPoint(transform.position, mover.Radius + 0.1f);
                }

                unit.transform.position = exitPoint;
                unit.SetActive(true);
                Rally?.ApplyTo(unit);
            }

            _occupants.Clear();
            _siegeImmuneOccupantCount = 0;
            Attackable?.SetSiegeImmune(false);
        }

        private void OnDestroy()
        {
            // A garrisoned unit shouldn't quietly disappear with the
            // building it was defending - hand every occupant back on
            // destruction, same "release what you're holding" convention
            // as FarmWorker.OnDisable's CancelWork.
            if (_occupants.Count > 0)
            {
                UngarrisonAll();
            }
        }
    }
}
