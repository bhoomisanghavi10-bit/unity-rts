using UnityEngine;
using UnityEngine.AI;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Buildings
{
    // AoE-style gate: blocks movement like a Wall by default (see
    // GateFactory's NavMeshObstacle), but opens - disables carving - the
    // moment a friendly unit is within openRadius, and closes again once
    // none remain nearby. A known simplification vs real AoE: NavMeshObstacle
    // carving is faction-blind, so an enemy standing right next to a
    // friendly unit when the gate opens could slip through in that same
    // window - accepted, since the engine has no notion of per-faction
    // navmesh visibility to do this exactly right.
    [RequireComponent(typeof(NavMeshObstacle))]
    public class Gate : Building
    {
        // Widened from 3 to 5 alongside the 2026-09-28 Ancient modular kit's
        // real 7.2-wide gate mesh (was tuned for the old 2.4-wide
        // placeholder) - a unit approaching the gate's outer edge, not just
        // its exact center, still needs to fall within this radius to open it.
        [SerializeField] private float openRadius = 5f;
        [SerializeField] private float checkInterval = 0.25f;

        private NavMeshObstacle _obstacle;
        private FactionMember _factionMember;
        private bool _factionResolved;
        private float _timer;

        private FactionId Faction
        {
            get
            {
                if (!_factionResolved)
                {
                    TryGetComponent(out _factionMember);
                    _factionResolved = true;
                }
                return _factionMember != null ? _factionMember.Faction : FactionId.Player;
            }
        }

        private void Awake()
        {
            _obstacle = GetComponent<NavMeshObstacle>();
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f)
            {
                return;
            }
            _timer = checkInterval;

            _obstacle.carving = !AnyFriendlyNearby();
        }

        private bool AnyFriendlyNearby()
        {
            foreach (Unit unit in Unit.All)
            {
                if (!unit.TryGetComponent(out FactionMember member) || member.Faction != Faction)
                {
                    continue;
                }

                if (Vector3.Distance(transform.position, unit.transform.position) <= openRadius)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
