using System;
using UnityEngine;
using KingdomsOfBharat.Vfx;
using KingdomsOfBharat.Audio;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;

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

        [SerializeField] private bool siegeImmune;

        // Wave 0 item 2 (retroactive upgrade rule): whether this Attackable
        // should live-read UpgradeProgress's flat + per-class armor bonus
        // in TakeDamage below, split per damage type since ArcherFactory/
        // CholaNavalRaiderFactory only ever applied the bonus to
        // pierceArmor (matching AoE's own pierce-specific "Archer Armor"
        // line) while every other combat-unit factory applied it to both.
        // Opt-in, not automatic - see EnableUpgradeArmorScaling. Buildings
        // and Workers never called this before the fix and must not start
        // silently benefiting from Blacksmith-style research now.
        private bool _upgradeArmorAppliesMelee;
        private bool _upgradeArmorAppliesPierce;
        private FactionMember _faction;
        private bool _factionResolved;

        public float Health { get; private set; }
        public float MaxHealth => maxHealth;
        public bool IsDead => Health <= 0f;
        public UnitClass Class => unitClass;

        // Resolved lazily, not cached in Awake - same convention as
        // MeleeAttacker.Faction (a factory adds FactionMember well after
        // Attackable in every spawn sequence).
        private FactionMember Faction
        {
            get
            {
                if (!_factionResolved)
                {
                    TryGetComponent(out _faction);
                    _factionResolved = true;
                }
                return _faction;
            }
        }

        // Called only by the combat-unit factories that already baked
        // UpgradeProgress's bonus into ConfigureArmor's arguments before
        // this fix - see each factory's own call site. Default true/true
        // matches every factory except Archer/CholaNavalRaider, which pass
        // melee: false.
        public void EnableUpgradeArmorScaling(bool melee = true, bool pierce = true)
        {
            _upgradeArmorAppliesMelee = melee;
            _upgradeArmorAppliesPierce = pierce;
        }
        // Roadmap Section 5 item 3 (Maratha Durg Garrison): a building
        // with a Durg Garrison unit inside is immune to Siege's normal 3x
        // anti-building bonus - see GarrisonPoint.TryGarrison/UngarrisonAll,
        // which set this, and MeleeAttacker's bonus computation, which
        // reads it.
        public bool SiegeImmune => siegeImmune;

        public void SetSiegeImmune(bool value)
        {
            siegeImmune = value;
        }

        // Roadmap Section 1 (worker self-defense/cross-awareness, AoE-parity
        // Phase 4.2): fires whenever a hit lands and this Attackable survives
        // it, carrying the attacker's own Attackable so a listener (e.g.
        // Gatherer) can fight back or flee toward/away from a real target -
        // not just "something hit me." Deliberately doesn't fire on a lethal
        // hit (nothing left to react). Attackable stays decoupled from any
        // specific reactor (Gatherer, a future auto-defense system) - it just
        // announces the hit, same "generic event, specific listener" shape as
        // every other cross-system hook in this project.
        public event Action<Attackable> OnDamaged;

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

        // Live/incremental healing (Repairable) - adds to current Health,
        // unlike RestoreHealth's absolute save/load snap.
        public void Heal(float amount)
        {
            Health = Mathf.Min(maxHealth, Health + amount);
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
        public void TakeDamage(float amount, DamageType damageType = DamageType.Melee, Attackable attacker = null)
        {
            if (IsDead)
            {
                return;
            }

            float armor = damageType == DamageType.Melee ? meleeArmor : pierceArmor;
            bool appliesToThisType = damageType == DamageType.Melee ? _upgradeArmorAppliesMelee : _upgradeArmorAppliesPierce;
            if (appliesToThisType && Faction != null)
            {
                armor += UpgradeProgress.ArmorBonus(Faction.Faction) + UpgradeProgress.ClassArmorBonus(Faction.Faction, unitClass);
            }
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
                    RajputDefianceHook.TrySpawnSurvivor(this);
                }
                Destroy(gameObject);
                return;
            }

            if (attacker != null)
            {
                OnDamaged?.Invoke(attacker);
            }
        }

        private Vector3 HitPoint()
        {
            return transform.position + Vector3.up * 1f;
        }
    }
}
