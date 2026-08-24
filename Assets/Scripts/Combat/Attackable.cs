using UnityEngine;
using KingdomsOfBharat.Vfx;
using KingdomsOfBharat.Audio;

namespace KingdomsOfBharat.Combat
{
    // AoE-style: melee attackers (Soldiers) deal Melee damage, ranged
    // attackers (Archers) deal Pierce damage - two separate armor stats let
    // a unit/building resist one better than the other instead of a single
    // flat damage reduction.
    public enum DamageType
    {
        Melee,
        Pierce,
    }

    // Anything that can take damage and die: soldiers, archers, buildings,
    // and the target dummy.
    public class Attackable : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 30f;
        [SerializeField] private float meleeArmor;
        [SerializeField] private float pierceArmor;
        [SerializeField] private UnitClass unitClass = UnitClass.Infantry;

        public float Health { get; private set; }
        public float MaxHealth => maxHealth;
        public bool IsDead => Health <= 0f;
        public UnitClass Class => unitClass;

        public void Configure(float newMaxHealth)
        {
            maxHealth = newMaxHealth;
            Health = maxHealth;
        }

        // Save/load only - sets current Health directly without touching
        // maxHealth or re-triggering Awake()'s full-heal reset, unlike
        // Configure(). Called after Configure() during a load's rebuild
        // (which always spawns a fresh, full-health unit/building via the
        // normal Factory first), so a damaged save restores as damaged
        // instead of silently healing back to full on load.
        public void RestoreHealth(float savedHealth)
        {
            Health = Mathf.Clamp(savedHealth, 0f, maxHealth);
        }

        // Armor is additive on top of whatever Configure(maxHealth) already
        // set - factories call this second, after Configure, so a caller
        // that skips it just gets 0/0 armor (today's pre-armor behavior).
        public void ConfigureArmor(float meleeArmor, float pierceArmor)
        {
            this.meleeArmor = meleeArmor;
            this.pierceArmor = pierceArmor;
        }

        // Defaults to Infantry (the field's own default) so a factory that
        // skips this call - same convention as ConfigureArmor - still gets
        // a reasonable CombatBonus lookup instead of an unset/zero value.
        public void ConfigureClass(UnitClass newUnitClass)
        {
            unitClass = newUnitClass;
        }

        private void Awake()
        {
            Health = maxHealth;
        }

        // AoE's own floor: armor can blunt a hit a long way but never to
        // zero - every attack that lands does at least 1 damage.
        public void TakeDamage(float amount, DamageType damageType = DamageType.Melee)
        {
            if (IsDead)
            {
                return;
            }

            float armor = damageType == DamageType.Melee ? meleeArmor : pierceArmor;
            float effective = Mathf.Max(1f, amount - armor);

            Health -= effective;
            VfxFactory.SpawnBurst(HitPoint(), new Color(1f, 0.9f, 0.5f), size: 0.08f, count: 4, speed: 1f, lifetime: 0.2f);
            SfxPlayer.PlayAttackHit(HitPoint());

            if (Health <= 0f)
            {
                Health = 0f;
                VfxFactory.SpawnBurst(transform.position, new Color(0.5f, 0.45f, 0.4f), size: 0.25f, count: 14, speed: 2f, lifetime: 0.5f);
                if (unitClass == UnitClass.Building)
                {
                    SfxPlayer.PlayBuildingDestroyed(transform.position);
                }
                else
                {
                    SfxPlayer.PlayUnitDeath(transform.position);
                }
                Destroy(gameObject);
            }
        }

        private Vector3 HitPoint()
        {
            return transform.position + Vector3.up * 1f;
        }
    }
}
