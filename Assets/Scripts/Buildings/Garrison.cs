using UnityEngine;
using KingdomsOfBharat.Combat;

namespace KingdomsOfBharat.Buildings
{
    // Roadmap Section 5 item 3: lets a Maratha Durg Garrison unit (see
    // DurgGarrisonWorker) enter a Wall/Tower, granting that building
    // immunity to Siege's anti-building bonus for as long as the unit
    // stays inside (see Attackable.SiegeImmune / MeleeAttacker). Single-
    // slot capacity - "a fortress-defense specialist," not a barracks-in-
    // a-wall. Added by WallFactory/TowerFactory, same one-line-per-
    // component convention every other building component there follows.
    public class Garrison : MonoBehaviour
    {
        private GameObject _garrisonedUnit;

        public bool HasDurgGarrison => _garrisonedUnit != null;

        // Resolved lazily, not in Awake() - see Barracks' identical
        // comment on Site/FactionMember: AddComponent fires Awake()
        // immediately, before a factory has necessarily added every
        // sibling component yet, so an eager Awake()-time GetComponent
        // risks finding nothing.
        private Attackable Attackable => GetComponent<Attackable>();

        public bool TryGarrison(GameObject unit)
        {
            if (HasDurgGarrison || unit == null)
            {
                return false;
            }

            _garrisonedUnit = unit;
            unit.SetActive(false);
            Attackable?.SetSiegeImmune(true);
            return true;
        }

        public void Ungarrison()
        {
            if (!HasDurgGarrison)
            {
                return;
            }

            _garrisonedUnit.transform.position = transform.position;
            _garrisonedUnit.SetActive(true);
            _garrisonedUnit = null;
            Attackable?.SetSiegeImmune(false);
        }

        private void OnDestroy()
        {
            // A garrisoned unit shouldn't quietly disappear with the
            // building it was defending - hand it back on destruction,
            // same "release what you're holding" convention as
            // FarmWorker.OnDisable's CancelWork.
            if (HasDurgGarrison)
            {
                Ungarrison();
            }
        }
    }
}
