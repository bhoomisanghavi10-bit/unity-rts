using System.Collections.Generic;
using UnityEngine;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Wildlife;
using KingdomsOfBharat.Camera;
using KingdomsOfBharat.Audio;

namespace KingdomsOfBharat.Selection
{
    // Owns all selection/command input: left-click selects a single unit,
    // left-click-drag box-selects multiple, right-click issues a move order
    // (or a gather/build/attack-move order, depending what the click landed
    // on) to whatever is currently selected. Disarmed entirely while
    // BuildingPlacer is mid-placement so a single click doesn't double up as
    // both a placement action and a unit command.
    public class SelectionManager : MonoBehaviour
    {
        [SerializeField] private float dragThreshold = 6f;
        [SerializeField] private KeyCode cycleStanceKey = KeyCode.V;
        [SerializeField] private float formationSpacing = 1.5f;

        private readonly List<Unit> _selected = new List<Unit>();
        private Building _selectedBuilding;
        private UnityEngine.Camera _camera;
        private Vector2 _dragStart;
        private bool _dragging;

        // Control groups (Ctrl+1-9 to assign the current selection, 1-9 to
        // reselect it) - 9 fixed slots rather than a Dictionary, since AoE's
        // own convention is exactly the digit row, no more.
        private readonly List<Unit>[] _controlGroups = new List<Unit>[9];

        // For SelectedUnitPanel / BuildMenu (UI) to read current selection.
        public IReadOnlyList<Unit> Selected => _selected;

        // Buildings are single-select only and mutually exclusive with unit
        // selection (AoE-style) - selecting one clears the other. Null when
        // nothing/a unit is selected instead.
        public Building SelectedBuilding => _selectedBuilding;

        private void Awake()
        {
            cycleStanceKey = GameSettings.GetKey("CycleStance", cycleStanceKey);

            _camera = UnityEngine.Camera.main;

            for (int i = 0; i < _controlGroups.Length; i++)
            {
                _controlGroups[i] = new List<Unit>();
            }
        }

        private void Update()
        {
            // A selected unit can now die mid-selection (workers are
            // killable since milestone 11 - wild boars, enemy soldiers).
            // Prune before anything this frame reads _selected: HandleMoveInput
            // below, and SelectedUnitPanel/BuildMenu's OnGUI, which would
            // otherwise throw MissingReferenceException touching a
            // destroyed Unit every frame. The == null check here is
            // deliberate - it invokes UnityEngine.Object's overridden
            // equality, which is what actually detects "destroyed but not
            // yet real C# null" Unity objects; a plain reference/is-null
            // check would not catch this.
            _selected.RemoveAll(unit => unit == null);

            // Same destroyed-but-not-yet-null concern as above, now that
            // buildings are attackable and can die mid-selection too.
            if (!_selectedBuilding)
            {
                _selectedBuilding = null;
            }

            if (BuildingPlacer.IsPlacing || MinimapController.IsPointerOverMinimap)
            {
                return;
            }

            HandleSelectionInput();
            HandleMoveInput();
            HandleRallyInput();
            HandleStanceHotkey();
            HandleControlGroupInput();
        }

        // Ctrl+[1-9] assigns the current unit selection to that group,
        // replacing whatever was in it before; plain [1-9] reselects it.
        // Buildings never join a control group (AoE-style - groups are for
        // maneuvering an army, not a base), matching how SelectedBuilding
        // is already mutually exclusive with unit selection elsewhere here.
        private void HandleControlGroupInput()
        {
            bool assigning = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            for (int i = 0; i < _controlGroups.Length; i++)
            {
                if (!Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    continue;
                }

                if (assigning)
                {
                    AssignControlGroup(i);
                }
                else
                {
                    SelectControlGroup(i);
                }
            }
        }

        private void AssignControlGroup(int index)
        {
            if (_selected.Count == 0)
            {
                return;
            }

            _controlGroups[index].Clear();
            _controlGroups[index].AddRange(_selected);
        }

        private void SelectControlGroup(int index)
        {
            // Same destroyed-but-not-yet-null pruning as _selected/
            // _selectedBuilding above - a grouped unit can die long after
            // the group was assigned, well before it's ever reselected.
            _controlGroups[index].RemoveAll(unit => unit == null);
            if (_controlGroups[index].Count == 0)
            {
                return;
            }

            ClearSelection();
            foreach (Unit unit in _controlGroups[index])
            {
                Select(unit);
            }
        }

        // Right-click while a production building (one with a RallyPoint -
        // see TownCenter/Barracks) is selected sets its rally point instead
        // of issuing a unit-move order; unit and building selection are
        // already mutually exclusive (see SelectBuilding), so this never
        // fires on the same click as HandleMoveInput. Mirrors
        // HandleMoveInput's own hit-priority shape (resource node -> hostile
        // target -> plain point) but without the friendly-build-assist/
        // staff-farm cases, which don't apply to a rally point.
        private void HandleRallyInput()
        {
            if (_selectedBuilding == null
                || !_selectedBuilding.TryGetComponent(out RallyPoint rally)
                || !Input.GetMouseButtonDown(1))
            {
                return;
            }

            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 500f))
            {
                return;
            }

            if (hit.collider.TryGetComponent(out ResourceNode node))
            {
                rally.SetGatherTarget(node);
                return;
            }

            if (hit.collider.TryGetComponent(out Attackable attackable)
                && !attackable.IsDead
                && !IsFriendlyToPlayer(attackable))
            {
                rally.SetAttackTarget(attackable);
                return;
            }

            rally.SetPoint(hit.point);
        }

        // Cycles stance (Aggressive -> Defensive -> StandGround) for every
        // currently selected unit that has a StanceController - Workers
        // don't get one (see StanceController's own comment), so this is a
        // silent no-op for an all-Worker selection.
        private void HandleStanceHotkey()
        {
            if (_selected.Count == 0 || !Input.GetKeyDown(cycleStanceKey))
            {
                return;
            }

            foreach (Unit unit in _selected)
            {
                if (unit.TryGetComponent(out StanceController stance))
                {
                    stance.CycleStance();
                }
            }
        }

        private void HandleSelectionInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _dragStart = Input.mousePosition;
                _dragging = true;
            }

            if (!Input.GetMouseButtonUp(0) || !_dragging)
            {
                return;
            }

            _dragging = false;
            Vector2 dragEnd = Input.mousePosition;

            if (Vector2.Distance(_dragStart, dragEnd) < dragThreshold)
            {
                SelectSingle(dragEnd);
            }
            else
            {
                SelectInBox(_dragStart, dragEnd);
            }
        }

        private void HandleMoveInput()
        {
            if (_selected.Count == 0 || !Input.GetMouseButtonDown(1))
            {
                return;
            }

            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 500f))
            {
                return;
            }

            // Once per click, not once per unit in the loop below - every
            // branch there (move/gather/build/attack/staff) is some kind
            // of "order issued" acknowledgment, same sound regardless of
            // which.
            SfxPlayer.PlayMove();

            bool hitNode = hit.collider.TryGetComponent(out ResourceNode node);
            // Faction-gated, not just "unfinished"/"finished": an enemy's
            // incomplete building shouldn't offer the "help build" action,
            // and an enemy's finished Farm shouldn't offer "staff it" -
            // both need to fall through to the attack branch below instead
            // (see hitAttackable), now that buildings carry Attackable.
            ConstructionSite site = null;
            bool hitSite = !hitNode
                && hit.collider.TryGetComponent(out site)
                && !site.IsComplete
                && IsFriendlyToPlayer(site);
            Farm farm = null;
            bool hitFarm = !hitNode && !hitSite
                && hit.collider.TryGetComponent(out farm)
                && farm.IsComplete
                && IsFriendlyToPlayer(farm);
            Livestock livestock = null;
            bool hitLivestock = !hitNode && !hitSite && !hitFarm
                && hit.collider.TryGetComponent(out livestock);
            Attackable attackable = null;
            bool hitAttackable = !hitNode && !hitSite && !hitFarm && !hitLivestock
                && hit.collider.TryGetComponent(out attackable)
                && !attackable.IsDead;

            // Only the plain-move (else) branch below uses this - a group
            // ordered onto open ground spreads into a rough grid (see
            // GroupFormation) instead of every unit pathing to the exact
            // same point, but gather/build/attack/staff targets are a
            // single specific thing every selected unit needs to reach,
            // not open ground to spread across.
            int formationIndex = 0;

            foreach (Unit unit in _selected)
            {
                unit.TryGetComponent(out Gatherer gatherer);
                unit.TryGetComponent(out Builder builder);
                unit.TryGetComponent(out MeleeAttacker attacker);
                unit.TryGetComponent(out FarmWorker farmWorker);
                unit.TryGetComponent(out LivestockWorker livestockWorker);

                if (hitNode)
                {
                    builder?.CancelBuild();
                    attacker?.CancelAttack();
                    farmWorker?.CancelWork();
                    livestockWorker?.CancelWork();
                    gatherer?.GatherFrom(node);
                }
                else if (hitSite && IsSameFaction(unit, site))
                {
                    gatherer?.CancelGather();
                    attacker?.CancelAttack();
                    farmWorker?.CancelWork();
                    livestockWorker?.CancelWork();
                    builder?.BuildAt(site);
                }
                else if (hitFarm && farmWorker != null && IsSameFaction(unit, farm))
                {
                    gatherer?.CancelGather();
                    builder?.CancelBuild();
                    attacker?.CancelAttack();
                    livestockWorker?.CancelWork();
                    farmWorker.StaffAt(farm);
                }
                else if (hitLivestock && livestockWorker != null)
                {
                    gatherer?.CancelGather();
                    builder?.CancelBuild();
                    attacker?.CancelAttack();
                    farmWorker?.CancelWork();
                    livestockWorker.StaffAt(livestock);
                }
                else if (hitAttackable && attacker != null && IsHostileTarget(unit, attackable))
                {
                    gatherer?.CancelGather();
                    builder?.CancelBuild();
                    farmWorker?.CancelWork();
                    livestockWorker?.CancelWork();
                    attacker.AttackMove(attackable);
                }
                else
                {
                    gatherer?.CancelGather();
                    builder?.CancelBuild();
                    attacker?.CancelAttack();
                    farmWorker?.CancelWork();
                    livestockWorker?.CancelWork();
                    if (unit.TryGetComponent(out UnitMover mover))
                    {
                        Vector3 offset = GroupFormation.GetOffset(formationIndex, _selected.Count, formationSpacing);
                        mover.MoveTo(hit.point + offset);
                        formationIndex++;
                    }
                }
            }
        }

        private void SelectSingle(Vector2 screenPos)
        {
            ClearSelection();

            // RaycastAll rather than a single Raycast: a building's collider
            // (bigger, and often nearer the camera at typical RTS angles)
            // would otherwise eclipse a smaller unit collider standing right
            // next to it, making units near buildings unselectable. Units
            // take priority over whatever else the ray also passes through.
            Ray ray = _camera.ScreenPointToRay(screenPos);
            RaycastHit[] hits = Physics.RaycastAll(ray, 500f);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.TryGetComponent(out Unit unit) && IsPlayerControllable(unit))
                {
                    Select(unit);
                    SfxPlayer.PlaySelect();
                    return;
                }
            }

            // Buildings select second (any faction, not just Player's own -
            // AoE lets you click an enemy building to see its HP, just not
            // command it), so a unit standing on/near one is still picked
            // first by the loop above.
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.TryGetComponent(out Building building))
                {
                    SelectBuilding(building);
                    SfxPlayer.PlaySelect();
                    return;
                }
            }
        }

        private void SelectInBox(Vector2 start, Vector2 end)
        {
            ClearSelection();

            Rect box = Rect.MinMaxRect(
                Mathf.Min(start.x, end.x), Mathf.Min(start.y, end.y),
                Mathf.Max(start.x, end.x), Mathf.Max(start.y, end.y));

            foreach (Unit unit in Unit.All)
            {
                if (!IsPlayerControllable(unit))
                {
                    continue;
                }

                Vector3 screenPoint = _camera.WorldToScreenPoint(unit.transform.position);
                if (screenPoint.z > 0f && box.Contains(screenPoint))
                {
                    Select(unit);
                }
            }

            // Once for the whole box, not once per unit caught inside it -
            // Select() above is called in a loop, but the sound is one
            // "selection made" acknowledgment, not one per unit.
            if (_selected.Count > 0)
            {
                SfxPlayer.PlaySelect();
            }
        }

        // No FactionMember present is treated as "not player-controllable"
        // here (unlike the fail-open combat/build checks below) - every
        // spawner tags its units, so absence would mean something's wrong
        // rather than "neutral," and defaulting to selectable would let the
        // player command units nothing actually spawned as theirs.
        private static bool IsPlayerControllable(Unit unit)
        {
            return unit.TryGetComponent(out FactionMember factionMember)
                && factionMember.Faction == FactionId.Player;
        }

        // Neutral (no FactionMember) targets/sites are always valid - see
        // TargetDummy, which is deliberately untagged-as-Player so it stays
        // attackable regardless of the attacker's own faction.
        private static bool IsHostileTarget(Unit source, Attackable target)
        {
            if (!target.TryGetComponent(out FactionMember targetFaction))
            {
                return true;
            }

            if (!source.TryGetComponent(out FactionMember sourceFaction))
            {
                return true;
            }

            return targetFaction.Faction != sourceFaction.Faction;
        }

        // No FactionMember present is treated as friendly here (matches
        // IsSameFaction/IsHostileTarget's fail-open convention) - only
        // used to decide whether a click should be a build-assist/staff
        // action (friendly) or an attack (hostile), and every selected
        // unit is always Player's own (see IsPlayerControllable).
        private static bool IsFriendlyToPlayer(Component target)
        {
            return !target.TryGetComponent(out FactionMember targetFaction)
                || targetFaction.Faction == FactionId.Player;
        }

        private static bool IsSameFaction(Unit source, Component target)
        {
            if (!source.TryGetComponent(out FactionMember sourceFaction))
            {
                return true;
            }

            if (!target.TryGetComponent(out FactionMember targetFaction))
            {
                return true;
            }

            return sourceFaction.Faction == targetFaction.Faction;
        }

        private void Select(Unit unit)
        {
            _selected.Add(unit);
            if (unit.TryGetComponent(out SelectionIndicator indicator))
            {
                indicator.SetSelected(true);
            }
        }

        // Single-select, mutually exclusive with unit selection - ClearSelection
        // (called by both SelectSingle and SelectInBox before reselecting)
        // already drops any previously selected building.
        private void SelectBuilding(Building building)
        {
            _selectedBuilding = building;
            if (building.TryGetComponent(out SelectionIndicator indicator))
            {
                indicator.SetSelected(true);
            }
        }

        private void ClearSelection()
        {
            foreach (Unit unit in _selected)
            {
                if (unit != null && unit.TryGetComponent(out SelectionIndicator indicator))
                {
                    indicator.SetSelected(false);
                }
            }
            _selected.Clear();

            if (_selectedBuilding != null && _selectedBuilding.TryGetComponent(out SelectionIndicator buildingIndicator))
            {
                buildingIndicator.SetSelected(false);
            }
            _selectedBuilding = null;
        }

        // Simple immediate-mode box-select overlay. Screen-space input is
        // bottom-left origin; OnGUI is top-left origin, hence the Y flip here
        // only (SelectInBox above deliberately uses raw screen coordinates to
        // match Camera.WorldToScreenPoint).
        private void OnGUI()
        {
            if (!_dragging)
            {
                return;
            }

            Vector2 start = _dragStart;
            Vector2 end = Input.mousePosition;
            start.y = Screen.height - start.y;
            end.y = Screen.height - end.y;

            Rect rect = Rect.MinMaxRect(
                Mathf.Min(start.x, end.x), Mathf.Min(start.y, end.y),
                Mathf.Max(start.x, end.x), Mathf.Max(start.y, end.y));

            Color previous = GUI.color;
            GUI.color = new Color(0.3f, 0.8f, 1f, 0.25f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
