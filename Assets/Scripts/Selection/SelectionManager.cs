using System.Collections.Generic;
using UnityEngine;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Wildlife;

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

        private readonly List<Unit> _selected = new List<Unit>();
        private UnityEngine.Camera _camera;
        private Vector2 _dragStart;
        private bool _dragging;

        // For SelectedUnitPanel / BuildMenu (UI) to read current selection.
        public IReadOnlyList<Unit> Selected => _selected;

        private void Awake()
        {
            _camera = UnityEngine.Camera.main;
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

            if (BuildingPlacer.IsPlacing)
            {
                return;
            }

            HandleSelectionInput();
            HandleMoveInput();
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

            bool hitNode = hit.collider.TryGetComponent(out ResourceNode node);
            ConstructionSite site = null;
            bool hitSite = !hitNode
                && hit.collider.TryGetComponent(out site)
                && !site.IsComplete;
            Farm farm = null;
            bool hitFarm = !hitNode && !hitSite
                && hit.collider.TryGetComponent(out farm)
                && farm.IsComplete;
            Livestock livestock = null;
            bool hitLivestock = !hitNode && !hitSite && !hitFarm
                && hit.collider.TryGetComponent(out livestock);
            Attackable attackable = null;
            bool hitAttackable = !hitNode && !hitSite && !hitFarm && !hitLivestock
                && hit.collider.TryGetComponent(out attackable)
                && !attackable.IsDead;

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
                        mover.MoveTo(hit.point);
                    }
                }
            }
        }

        private void SelectSingle(Vector2 screenPos)
        {
            ClearSelection();

            Ray ray = _camera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 500f)
                && hit.collider.TryGetComponent(out Unit unit)
                && IsPlayerControllable(unit))
            {
                Select(unit);
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
