using UnityEngine;

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
            if (Health <= 0f)
            {
                Health = 0f;
                Destroy(gameObject);
            }
        }
    }
}
