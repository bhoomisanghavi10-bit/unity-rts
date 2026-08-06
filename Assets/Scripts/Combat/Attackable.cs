using UnityEngine;
using KingdomsOfBharat.Vfx;

namespace KingdomsOfBharat.Combat
{
    // Anything that can take damage and die: soldiers and the target dummy.
    public class Attackable : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 30f;

        public float Health { get; private set; }
        public bool IsDead => Health <= 0f;

        public void Configure(float newMaxHealth)
        {
            maxHealth = newMaxHealth;
            Health = maxHealth;
        }

        private void Awake()
        {
            Health = maxHealth;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead)
            {
                return;
            }

            Health -= amount;
            VfxFactory.SpawnBurst(HitPoint(), new Color(1f, 0.9f, 0.5f), size: 0.08f, count: 4, speed: 1f, lifetime: 0.2f);

            if (Health <= 0f)
            {
                Health = 0f;
                VfxFactory.SpawnBurst(transform.position, new Color(0.5f, 0.45f, 0.4f), size: 0.25f, count: 14, speed: 2f, lifetime: 0.5f);
                Destroy(gameObject);
            }
        }

        private Vector3 HitPoint()
        {
            return transform.position + Vector3.up * 1f;
        }
    }
}
