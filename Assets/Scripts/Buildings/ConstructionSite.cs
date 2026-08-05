using UnityEngine;

namespace KingdomsOfBharat.Buildings
{
    // Drives a building's build-time progress after placement: visually
    // grows the building from ground level up to full height, and flips
    // IsComplete once done. A building with no ConstructionSite (e.g. the
    // milestone-4 Town Center) should be treated as always complete.
    public class ConstructionSite : MonoBehaviour
    {
        [SerializeField] private float buildTime = 8f;

        private float _elapsed;
        private Vector3 _finalScale;
        private float _baseY;

        public bool IsComplete { get; private set; }

        public void Configure(float duration)
        {
            buildTime = duration;
        }

        private void Awake()
        {
            _finalScale = transform.localScale;
            _baseY = transform.position.y - _finalScale.y * 0.5f;
            ApplyHeight(0.01f);
        }

        private void Update()
        {
            if (IsComplete)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(_elapsed / buildTime);
            ApplyHeight(Mathf.Lerp(0.01f, _finalScale.y, progress));

            if (progress >= 1f)
            {
                IsComplete = true;
            }
        }

        private void ApplyHeight(float height)
        {
            transform.localScale = new Vector3(_finalScale.x, height, _finalScale.z);
            Vector3 position = transform.position;
            position.y = _baseY + height * 0.5f;
            transform.position = position;
        }
    }
}
