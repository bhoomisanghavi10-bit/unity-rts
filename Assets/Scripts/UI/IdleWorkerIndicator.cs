using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.Multiplayer;

namespace KingdomsOfBharat.UI
{
    // Wave 6 item 34: "a small UI addition near the minimap" (the item's
    // own note). Built entirely in code and self-attached via
    // MinimapController.Awake() (AddComponent, not a scene-authored
    // [SerializeField] hookup) - same "avoid the recurring new-field-null-
    // in-the-scene gotcha" convention ResourceHUD's own Age/research row
    // used. Positioned via a snapshot of the minimap panel's own bottom-
    // right anchor/position/size that MinimapController passes into
    // Configure() - NOT by reading `transform` directly, since
    // MinimapController.ApplyDiamondFrame() reparents/re-stretches that
    // same RectTransform as part of building the diamond mask (see
    // Configure's own comment).
    //
    // Shows a live count of the local faction's idle Workers (Gatherer
    // presence, same marker TownBell/IdleWorkerFinder use) and, on click or
    // its own hotkey, selects the next one and pans the camera to it -
    // cycling round-robin through the current idle set the same way AoE's
    // own idle-villager button does, rather than always jumping back to
    // the first.
    public class IdleWorkerIndicator : MonoBehaviour
    {
        private const float RowHeight = 40f;
        private const float Gap = 10f;
        private const float IconSize = 28f;

        private SelectionManager _selectionManager;
        private Camera.RTSCameraController _cameraController;
        private Button _button;
        private TMP_Text _countLabel;
        private Unit _lastSelected;
        private KeyCode _hotkey;

        private void Awake()
        {
            _selectionManager = FindFirstObjectByType<SelectionManager>();
            UnityEngine.Camera mainCamera = UnityEngine.Camera.main;
            if (mainCamera != null)
            {
                _cameraController = mainCamera.GetComponent<Camera.RTSCameraController>();
            }
        }

        // MinimapController.ApplyDiamondFrame() reparents and re-stretches
        // its OWN RectTransform (the same GameObject this component gets
        // attached to) as part of building the diamond mask/border - so by
        // the time Awake() runs here, `transform` no longer describes the
        // minimap's real screen position/size (it reads
        // anchorMin=(0,0)/anchorMax=(1,1)/sizeDelta=(0,0), stretched to
        // fill the new mask parent instead). MinimapController snapshots
        // its own bottom-right anchor/position/size BEFORE calling
        // ApplyDiamondFrame() and passes that snapshot here explicitly,
        // rather than this component trying to read `transform` itself.
        public void Configure(Transform referenceParent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            BuildUi(referenceParent, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
        }

        private void BuildUi(Transform referenceParent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject panelGo = new GameObject("IdleWorkerIndicator", typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.SetParent(referenceParent, false);
            panelRect.anchorMin = anchorMin;
            panelRect.anchorMax = anchorMax;
            panelRect.pivot = pivot;
            panelRect.anchoredPosition = anchoredPosition + new Vector2(0f, sizeDelta.y + Gap);
            panelRect.sizeDelta = new Vector2(sizeDelta.x, RowHeight);

            Image panelImage = panelGo.GetComponent<Image>();
            Sprite frameSprite = Resources.Load<Sprite>("UI/Panels/panel_resource_bar");
            if (frameSprite != null)
            {
                panelImage.sprite = frameSprite;
                panelImage.type = Image.Type.Sliced;
                panelImage.pixelsPerUnitMultiplier = frameSprite.rect.height / RowHeight;
            }
            else
            {
                panelImage.color = new Color(0f, 0f, 0f, 0.6f);
            }

            _button = panelGo.GetComponent<Button>();
            _button.targetGraphic = panelImage;
            _button.onClick.AddListener(CycleToNextIdle);

            Sprite iconSprite = Resources.Load<Sprite>("UI/Icons/train_worker");
            if (iconSprite != null)
            {
                GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                RectTransform iconRect = iconGo.GetComponent<RectTransform>();
                iconRect.SetParent(panelRect, false);
                iconRect.anchorMin = new Vector2(0f, 0.5f);
                iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.pivot = new Vector2(0f, 0.5f);
                iconRect.anchoredPosition = new Vector2(6f, 0f);
                iconRect.sizeDelta = new Vector2(IconSize, IconSize);
                Image iconImage = iconGo.GetComponent<Image>();
                iconImage.sprite = iconSprite;
                iconImage.type = Image.Type.Simple;
                iconImage.raycastTarget = false;
            }

            GameObject labelGo = new GameObject("CountLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.SetParent(panelRect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(IconSize + 12f, 0f);
            labelRect.offsetMax = new Vector2(-6f, 0f);
            _countLabel = labelGo.GetComponent<TextMeshProUGUI>();
            _countLabel.fontSize = 16f;
            _countLabel.color = Color.white;
            _countLabel.raycastTarget = false;
            _countLabel.enableWordWrapping = false;
            _countLabel.verticalAlignment = VerticalAlignmentOptions.Middle;
        }

        private void Update()
        {
            _hotkey = GameSettings.GetKey("SelectIdleWorker", KeyCode.F6);

            List<Unit> idle = IdleWorkerFinder.FindAll(NetworkMatch.LocalFaction);
            _countLabel.text = $"Idle Workers: {idle.Count}";
            _button.interactable = idle.Count > 0;

            if (Input.GetKeyDown(_hotkey))
            {
                CycleToNextIdle();
            }
        }

        // Round-robins through the CURRENT idle set on every call, rather
        // than always the first entry - a repeated click/hotkey press
        // walks through every idle worker in turn, same as AoE's own
        // idle-villager button. If the last-selected unit is no longer
        // idle (it started a task, died, or got garrisoned), restarts from
        // the front of the list instead of guessing an index.
        private void CycleToNextIdle()
        {
            List<Unit> idle = IdleWorkerFinder.FindAll(NetworkMatch.LocalFaction);
            if (idle.Count == 0)
            {
                return;
            }

            int index = _lastSelected != null ? idle.IndexOf(_lastSelected) : -1;
            Unit next = idle[(index + 1) % idle.Count];
            _lastSelected = next;

            _selectionManager?.SelectOnly(next);
            _cameraController?.JumpTo(next.transform.position.x, next.transform.position.z);
        }
    }
}
