using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Selection
{
    // Placeholder selected-state visual: a flattened disc under the unit,
    // toggled on/off by SelectionManager. Swap for real ring art later.
    public class SelectionIndicator : MonoBehaviour
    {
        [SerializeField] private Color ringColor = Color.yellow;

        private GameObject _ring;

        private void Awake()
        {
            _ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _ring.name = "SelectionRing";
            Destroy(_ring.GetComponent<Collider>());

            _ring.transform.SetParent(transform, false);
            _ring.transform.localPosition = new Vector3(0f, -0.9f, 0f);
            _ring.transform.localScale = new Vector3(1.2f, 0.02f, 1.2f);

            var renderer = _ring.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = GameplayMaterial.CreateOpaque(ringColor);

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            _ring.SetActive(selected);
        }
    }
}
