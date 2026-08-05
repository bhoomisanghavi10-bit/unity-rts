using UnityEngine;

namespace KingdomsOfBharat.Buildings
{
    // Drives a building's build-time progress: visually grows the building
    // from ground level up to full height, and flips IsComplete once done.
    // Progress only advances while at least one Builder is actively working
    // the site (AoE-style — a placed foundation sits idle until a worker is
    // sent to build it; multiple workers build proportionally faster). A
    // building with no ConstructionSite (e.g. the milestone-4 Town Center)
    // should be treated as always complete.
    public class ConstructionSite : MonoBehaviour
    {
        [SerializeField] private float buildTime = 8f;

        private float _progress; // 0..1
        private Vector3 _finalScale;
        private float _baseY;
        private int _activeBuilders;

        public bool IsComplete { get; private set; }

        public void Configure(float duration)
        {
            buildTime = duration;
        }

        public void BeginBuilding()
        {
            _activeBuilders++;
        }

        public void StopBuilding()
        {
            _activeBuilders = Mathf.Max(0, _activeBuilders - 1);
        }

        private void Awake()
        {
            _finalScale = transform.localScale;
            _baseY = transform.position.y - _finalScale.y * 0.5f;
            ApplyHeight(0.01f);
        }

        private void Update()
        {
            if (IsComplete || _activeBuilders <= 0)
            {
                return;
            }

            _progress += (Time.deltaTime / buildTime) * _activeBuilders;
            _progress = Mathf.Clamp01(_progress);
            ApplyHeight(Mathf.Lerp(0.01f, _finalScale.y, _progress));

            if (_progress >= 1f)
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
