using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.ResourceGathering;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Camera;
using KingdomsOfBharat.Selection;

namespace KingdomsOfBharat.UI
{
    // Follows the cursor with the name (and a couple of relevant
    // properties - HP, status, resource amount, build progress...) of
    // whatever's directly under it, for anything in the game: units,
    // buildings, resource nodes, wildlife. Same unfiltered raycast
    // SelectionManager/BuildingPlacer already use, so it always agrees
    // with what a click there would actually hit. uGUI/TMP replacement
    // for the original OnGUI version - panelRoot is a Canvas child
    // repositioned to the cursor and toggled active/inactive instead of
    // being conditionally drawn every frame.
    public class HoverTooltip : MonoBehaviour
    {
        [SerializeField] private float maxDistance = 500f;
        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private TMP_Text line1Label;
        [SerializeField] private TMP_Text line2Label;
        [SerializeField] private TMP_Text line3Label;

        private UnityEngine.Camera _camera;
        private SelectionManager _selectionManager;

        // Cursor textures, not sprites - Cursor.SetCursor takes a Texture2D
        // directly. Loaded once; hotspot is the texture center for the
        // symmetric gather/attack-move icons, top-left for the arrow. Only 4
        // of the spec'd 5 states exist (no build-placement cursor was ever
        // generated - see docs/UI_ART_BRIEF.md) - BuildingPlacer.IsPlacing is
        // checked below and deliberately left on the default cursor rather
        // than silently doing nothing.
        private Texture2D _cursorDefault;
        private Texture2D _cursorGather;
        private Texture2D _cursorAttackMove;

        private void Awake()
        {
            _camera = UnityEngine.Camera.main;
            _selectionManager = FindFirstObjectByType<SelectionManager>();

            // Dedicated art, not the shared UIStyleTheme.PanelFrameSprite -
            // see SelectedUnitPanel.cs for why these panels each get their
            // own purpose-made frame instead of the generic modal one.
            Sprite frame = Resources.Load<Sprite>("UI/Panels/panel_tooltip");
            if (panelRoot.TryGetComponent(out Image background))
            {
                background.color = Color.white;
                if (frame != null)
                {
                    background.sprite = frame;
                    background.type = Image.Type.Sliced;
                    background.pixelsPerUnitMultiplier = 44f;
                }
            }

            _cursorDefault = Resources.Load<Texture2D>("UI/Cursors/default");
            _cursorGather = Resources.Load<Texture2D>("UI/Cursors/gather");
            _cursorAttackMove = Resources.Load<Texture2D>("UI/Cursors/attack_move");
        }

        private void SetCursor(Texture2D texture)
        {
            if (texture == null)
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                return;
            }

            Vector2 hotspot = texture == _cursorDefault ? Vector2.zero : new Vector2(texture.width / 2f, texture.height / 2f);
            Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
        }

        private bool SelectionCanGather()
        {
            if (_selectionManager == null)
            {
                return false;
            }

            foreach (Unit unit in _selectionManager.Selected)
            {
                if (unit.TryGetComponent(out Gatherer _))
                {
                    return true;
                }
            }

            return false;
        }

        private void Update()
        {
            string line1 = null;
            string line2 = null;
            string line3 = null;
            Texture2D cursor = _cursorDefault;

            // BuildingPlacer.IsPlacing is exactly where a build-placement
            // cursor belongs - no asset exists for it (see field comments
            // above), so this deliberately falls through to the default
            // cursor rather than a dedicated one.
            if (!BuildingPlacer.IsPlacing && !MinimapController.IsPointerOverMinimap)
            {
                Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
                {
                    GameObject go = hit.collider.gameObject;
                    line1 = go.name;

                    if (go.TryGetComponent(out FactionMember factionMember))
                    {
                        // Item 48: real faction name instead of a binary
                        // Player/Enemy ternary (which mislabeled a 3rd
                        // faction as "Player"), plus an (Allied) suffix
                        // when relevant so allies read differently from
                        // hostiles at a glance.
                        line1 += factionMember.Faction == FactionId.Player
                            ? " (Player)"
                            : DiplomacyRegistry.AreAllied(FactionId.Player, factionMember.Faction)
                                ? $" ({factionMember.Faction} - Allied)"
                                : $" ({factionMember.Faction})";

                        if (factionMember.Faction != FactionId.Player
                            && !DiplomacyRegistry.AreAllied(FactionId.Player, factionMember.Faction))
                        {
                            cursor = _cursorAttackMove;
                        }
                    }

                    if (go.TryGetComponent(out Unit unit))
                    {
                        line2 = UnitStatus.Describe(unit);
                    }
                    else if (go.TryGetComponent(out ConstructionSite site) && !site.IsComplete)
                    {
                        line2 = $"Building: {(int)(site.Progress * 100f)}%";
                    }
                    else if (go.TryGetComponent(out ResourceNode node))
                    {
                        line2 = node.ResourceType.ToString();
                        if (SelectionCanGather())
                        {
                            cursor = _cursorGather;
                        }
                    }

                    if (go.TryGetComponent(out Attackable attackable))
                    {
                        line3 = $"HP: {(int)attackable.Health}/{(int)attackable.MaxHealth}";
                    }
                }
            }

            SetCursor(cursor);

            panelRoot.gameObject.SetActive(line1 != null);
            if (line1 == null)
            {
                return;
            }

            panelRoot.position = new Vector3(Input.mousePosition.x + 16f, Input.mousePosition.y - 16f, 0f);

            line1Label.text = line1;
            line2Label.gameObject.SetActive(line2 != null);
            if (line2 != null)
            {
                line2Label.text = line2;
            }

            line3Label.gameObject.SetActive(line3 != null);
            if (line3 != null)
            {
                line3Label.text = line3;
            }
        }
    }
}
