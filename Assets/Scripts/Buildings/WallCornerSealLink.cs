using UnityEngine;

namespace KingdomsOfBharat.Buildings
{
    // Wall mechanics audit (2026-09-29): keeps a WallCornerSeal obstacle
    // (see BuildingPlacer.SealDiagonalGaps) alive only as long as BOTH the
    // Wall/Gate pieces it plugs the corner between still exist - polling
    // (same 0.5s-interval convention as Gate.checkInterval) rather than an
    // event, since neither Building nor Attackable fires a generic
    // "I was destroyed" event today. Relies on Unity's fake-null on a
    // destroyed UnityEngine.Object.
    public class WallCornerSealLink : MonoBehaviour
    {
        private const float CheckInterval = 0.5f;

        private Building _a;
        private Building _b;
        private float _timer;

        public void Configure(Building a, Building b)
        {
            _a = a;
            _b = b;
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f)
            {
                return;
            }
            _timer = CheckInterval;

            if (_a == null || _b == null)
            {
                Destroy(gameObject);
            }
        }
    }
}
