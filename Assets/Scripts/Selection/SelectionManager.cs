using System.Collections.Generic;
using UnityEngine;
using KingdomsOfBharat.Units;

namespace KingdomsOfBharat.Selection
{
    // Owns all selection/command input: left-click selects a single unit,
    // left-click-drag box-selects multiple, right-click issues a move order
    // to whatever is currently selected.
    public class SelectionManager : MonoBehaviour
    {
        [SerializeField] private float dragThreshold = 6f;

        private readonly List<Unit> _selected = new List<Unit>();
        private UnityEngine.Camera _camera;
        private Vector2 _dragStart;
        private bool _dragging;

        private void Awake()
        {
            _camera = UnityEngine.Camera.main;
        }

        private void Update()
        {
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

            foreach (Unit unit in _selected)
            {
                if (unit.TryGetComponent(out UnitMover mover))
                {
                    mover.MoveTo(hit.point);
                }
            }
        }

        private void SelectSingle(Vector2 screenPos)
        {
            ClearSelection();

            Ray ray = _camera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 500f) && hit.collider.TryGetComponent(out Unit unit))
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
                Vector3 screenPoint = _camera.WorldToScreenPoint(unit.transform.position);
                if (screenPoint.z > 0f && box.Contains(screenPoint))
                {
                    Select(unit);
                }
            }
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
