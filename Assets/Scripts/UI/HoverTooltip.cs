using UnityEngine;
using TMPro;
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

        private void Awake()
        {
            _camera = UnityEngine.Camera.main;
        }

        private void Update()
        {
            string line1 = null;
            string line2 = null;
            string line3 = null;

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
                    }

                    if (go.TryGetComponent(out Attackable attackable))
                    {
                        line3 = $"HP: {(int)attackable.Health}/{(int)attackable.MaxHealth}";
                    }
                }
            }

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
