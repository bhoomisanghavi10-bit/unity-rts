using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Selection;

namespace KingdomsOfBharat.Buildings
{
    // AoE-style rally point for a production building (TownCenter, Barracks):
    // where newly trained units walk to after spawning. Defaults to the
    // building's own fixed spawn offset (today's behavior, unchanged until
    // the player sets one) - right-click while this building is selected
    // (see SelectionManager.HandleBuildingRallyInput) moves it: onto a
    // resource node to auto-gather there, a hostile unit/building to
    // auto-attack-move there, or empty ground to just walk there.
    //
    // Self-contained: TownCenter/Barracks call ApplyTo(spawnedUnit) right
    // after spawning instead of computing transform.position + rallyOffset
    // themselves. A small flag marker (procedural primitive, same
    // create-at-runtime convention as SelectionIndicator's ring) shows only
    // while the owning building is selected, so it doesn't clutter the view
    // otherwise.
    [RequireComponent(typeof(Building))]
    public class RallyPoint : MonoBehaviour
    {
        [SerializeField] private Color flagColor = new Color(0.9f, 0.85f, 0.2f);

        private Vector3 _defaultOffset;
        private Vector3? _point;
        private ResourceNode _gatherTarget;
        private Attackable _attackTarget;
        private Building _building;
        private SelectionManager _selectionManager;
        private GameObject _flag;

        public Vector3 Position => _point ?? transform.position + _defaultOffset;

        // Called once by TownCenter/Barracks at spawn time with their own
        // existing rallyOffset field, so the default (no rally point set)
        // behavior is pixel-identical to before this component existed.
        public void Configure(Vector3 defaultOffset)
        {
            _defaultOffset = defaultOffset;
        }

        public void SetPoint(Vector3 point)
        {
            _point = point;
            _gatherTarget = null;
            _attackTarget = null;
        }

        public void SetGatherTarget(ResourceNode node)
        {
            _point = node.transform.position;
            _gatherTarget = node;
            _attackTarget = null;
        }

        public void SetAttackTarget(Attackable target)
        {
            _point = target.transform.position;
            _attackTarget = target;
            _gatherTarget = null;
        }

        private void Awake()
        {
            _building = GetComponent<Building>();
            _selectionManager = FindFirstObjectByType<SelectionManager>();
            BuildFlag();
        }

        private void BuildFlag()
        {
            _flag = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _flag.name = "RallyFlag";
            Destroy(_flag.GetComponent<Collider>());
            _flag.transform.SetParent(transform, worldPositionStays: false);
            _flag.transform.localScale = new Vector3(0.25f, 0.6f, 0.25f);
            _flag.GetComponent<MeshRenderer>().sharedMaterial = GameplayMaterial.CreateOpaque(flagColor);
            _flag.SetActive(false);
        }

        private void Update()
        {
            bool selected = _selectionManager != null && _selectionManager.SelectedBuilding == _building;
            _flag.SetActive(selected);
            if (selected)
            {
                Vector3 pos = Position;
                pos.y += 0.3f;
                _flag.transform.position = pos;
            }
        }

        // Called by TownCenter/Barracks right after spawning a unit at the
        // building's door - walks it to the rally point, and if the point
        // was set on a resource node or hostile target, starts gathering/
        // attacking immediately too (Gatherer.GatherFrom/MeleeAttacker.
        // AttackMove already move the unit themselves, same as a player's
        // own right-click order - only the plain-point fallback needs an
        // explicit MoveTo).
        public void ApplyTo(GameObject unitGo)
        {
            if (_gatherTarget != null && unitGo.TryGetComponent(out Gatherer gatherer))
            {
                gatherer.GatherFrom(_gatherTarget);
                return;
            }

            if (_attackTarget != null && !_attackTarget.IsDead && unitGo.TryGetComponent(out MeleeAttacker attacker))
            {
                attacker.AttackMove(_attackTarget);
                return;
            }

            // Item 49: same two checks as above, for a boat's own naval
            // equivalents (BoatGatherer/BoatAttacker) - a boat has neither
            // Gatherer nor MeleeAttacker, so without these branches it
            // would silently fall through to "no rally behavior at all"
            // rather than the plain-point WaterMover.MoveTo below.
            if (_gatherTarget != null && unitGo.TryGetComponent(out BoatGatherer boatGatherer))
            {
                boatGatherer.GatherFrom(_gatherTarget);
                return;
            }

            if (_attackTarget != null && !_attackTarget.IsDead && unitGo.TryGetComponent(out BoatAttacker boatAttacker))
            {
                boatAttacker.AttackMove(_attackTarget);
                return;
            }

            if (unitGo.TryGetComponent(out UnitMover mover))
            {
                mover.MoveTo(Position);
                return;
            }

            if (unitGo.TryGetComponent(out WaterMover waterMover))
            {
                waterMover.MoveTo(Position);
            }
        }
    }
}
