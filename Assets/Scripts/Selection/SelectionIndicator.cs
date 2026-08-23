using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Combat;

namespace KingdomsOfBharat.Selection
{
    // Placeholder selected-state visual: a flattened disc under the unit,
    // toggled on/off by SelectionManager. Swap for real ring art later.
    // Also force-shows a sibling HealthBar (if any) while selected, so a
    // full-health selected unit/building still displays its bar - AoE-style.
    public class SelectionIndicator : MonoBehaviour
    {
        [SerializeField] private Color ringColor = Color.yellow;
        [SerializeField] private float radius = 1.2f;
        [SerializeField] private float yOffset = -0.9f;

        private GameObject _ring;

        // Called right after AddComponent<SelectionIndicator>() by building
        // factories, before anything reads the ring - AddComponent runs
        // Awake() synchronously, so _ring already exists by the time this
        // resizes it (same lazy-resolution shape ConstructionSite/Barracks
        // use, just resolved eagerly here instead). Units keep the smaller
        // default sizing by simply never calling this.
        public void Configure(float newRadius, float newYOffset)
        {
            radius = newRadius;
            yOffset = newYOffset;
            _ring.transform.localPosition = new Vector3(0f, yOffset, 0f);
            _ring.transform.localScale = new Vector3(radius, 0.02f, radius);
        }

        private void Awake()
        {
            _ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _ring.name = "SelectionRing";
            Destroy(_ring.GetComponent<Collider>());

            _ring.transform.SetParent(transform, false);
            _ring.transform.localPosition = new Vector3(0f, yOffset, 0f);
            _ring.transform.localScale = new Vector3(radius, 0.02f, radius);

            var renderer = _ring.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = GameplayMaterial.CreateOpaque(ringColor);

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            _ring.SetActive(selected);
            if (TryGetComponent(out HealthBar healthBar))
            {
                healthBar.SetForceShow(selected);
            }
        }
    }
}
