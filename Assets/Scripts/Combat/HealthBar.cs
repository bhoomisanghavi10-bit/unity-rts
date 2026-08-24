using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Combat
{
    // World-space HP bar above anything with Attackable: a billboarded pair
    // of quads (dark backing + a green-to-red fill) that scales with current
    // health. AoE-style visibility rule - hidden at full health unless
    // force-shown (selection drives this via SelectionIndicator.SetSelected),
    // so damaged units/buildings always read at a glance without cluttering
    // full-health ones.
    [RequireComponent(typeof(Attackable))]
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private float width = 1f;
        [SerializeField] private float heightAboveModel = 0.35f;

        private Attackable _attackable;
        private Transform _root;
        private Transform _fill;
        private MeshRenderer _fillRenderer;
        private UnityEngine.Camera _camera;
        private bool _forceShow;

        public void SetForceShow(bool show)
        {
            _forceShow = show;
        }

        private void Awake()
        {
            _attackable = GetComponent<Attackable>();
            _camera = UnityEngine.Camera.main;
            BuildBar();
        }

        private void BuildBar()
        {
            _root = new GameObject("HealthBar").transform;
            _root.SetParent(transform, false);
            _root.localPosition = new Vector3(0f, TopY() + heightAboveModel, 0f);

            CreateQuad(new Color(0.1f, 0.05f, 0.05f), 0f);
            _fill = CreateQuad(Color.green, -0.005f);
            _fillRenderer = _fill.GetComponent<MeshRenderer>();
        }

        private float TopY()
        {
            Renderer rend = GetComponentInChildren<Renderer>();
            return rend != null ? rend.bounds.extents.y : 1f;
        }

        private Transform CreateQuad(Color color, float zOffset)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(_root, false);
            quad.transform.localPosition = new Vector3(0f, 0f, zOffset);
            quad.transform.localScale = new Vector3(width, 0.12f, 1f);
            quad.GetComponent<MeshRenderer>().sharedMaterial = GameplayMaterial.CreateOpaque(color);
            return quad.transform;
        }

        private void LateUpdate()
        {
            if (_attackable.IsDead)
            {
                return;
            }

            float fraction = Mathf.Clamp01(_attackable.Health / _attackable.MaxHealth);
            bool show = _forceShow || fraction < 0.999f;
            _root.gameObject.SetActive(show);
            if (!show)
            {
                return;
            }

            _fill.localScale = new Vector3(width * fraction, 0.12f, 1f);
            _fill.localPosition = new Vector3(-width * (1f - fraction) * 0.5f, 0f, -0.005f);
            // Item 46 colorblind mode: red-green is the pairing deuteranopia/
            // protanopia confuse most easily - blue/orange stays readable
            // across all common types of color blindness.
            _fillRenderer.sharedMaterial.color = GameSettings.ColorblindMode
                ? Color.Lerp(new Color(0.9f, 0.45f, 0.05f), new Color(0.15f, 0.55f, 1f), fraction)
                : Color.Lerp(Color.red, Color.green, fraction);

            if (_camera != null)
            {
                _root.rotation = _camera.transform.rotation;
            }
        }
    }
}
