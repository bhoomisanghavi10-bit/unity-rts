using UnityEngine;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Camera;

namespace KingdomsOfBharat.UI
{
    // Follows the cursor with the name (and a couple of relevant
    // properties - HP, status, resource amount, build progress...) of
    // whatever's directly under it, for anything in the game: units,
    // buildings, resource nodes, wildlife. Same unfiltered raycast
    // SelectionManager/BuildingPlacer already use, so it always agrees
    // with what a click there would actually hit.
    public class HoverTooltip : MonoBehaviour
    {
        [SerializeField] private float maxDistance = 500f;

        private UnityEngine.Camera _camera;
        private GUIStyle _style;
        private string _line1;
        private string _line2;
        private string _line3;

        private void Awake()
        {
            _camera = UnityEngine.Camera.main;
        }

        private void Update()
        {
            _line1 = null;
            _line2 = null;
            _line3 = null;

            if (BuildingPlacer.IsPlacing || MinimapController.IsPointerOverMinimap)
            {
                return;
            }

            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            {
                return;
            }

            GameObject go = hit.collider.gameObject;
            _line1 = go.name;

            if (go.TryGetComponent(out FactionMember factionMember))
            {
                _line1 += factionMember.Faction == FactionId.Enemy ? " (Enemy)" : " (Player)";
            }

            if (go.TryGetComponent(out Unit unit))
            {
                _line2 = UnitStatus.Describe(unit);
            }
            else if (go.TryGetComponent(out ConstructionSite site) && !site.IsComplete)
            {
                _line2 = $"Building: {(int)(site.Progress * 100f)}%";
            }
            else if (go.TryGetComponent(out ResourceNode node))
            {
                _line2 = node.ResourceType.ToString();
            }

            if (go.TryGetComponent(out Attackable attackable))
            {
                _line3 = $"HP: {(int)attackable.Health}/{(int)attackable.MaxHealth}";
            }
        }

        private void OnGUI()
        {
            if (_line1 == null)
            {
                return;
            }

            EnsureStyle();

            Vector2 mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            float width = 160f;
            float height = 20f + (_line2 != null ? 18f : 0f) + (_line3 != null ? 18f : 0f);
            Rect rect = new Rect(mouse.x + 16f, mouse.y + 16f, width, height);

            GUI.Box(rect, GUIContent.none);

            float y = rect.y + 2f;
            GUI.Label(new Rect(rect.x + 6f, y, width - 12f, 18f), _line1, _style);
            y += 18f;

            if (_line2 != null)
            {
                GUI.Label(new Rect(rect.x + 6f, y, width - 12f, 18f), _line2, _style);
                y += 18f;
            }

            if (_line3 != null)
            {
                GUI.Label(new Rect(rect.x + 6f, y, width - 12f, 18f), _line3, _style);
            }
        }

        private void EnsureStyle()
        {
            if (_style != null)
            {
                return;
            }

            _style = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            _style.normal.textColor = Color.white;
        }
    }
}
