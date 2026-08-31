using UnityEngine;
using UnityEngine.AI;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Combat
{
    // Phase 6 unique unit (Vijayanagara): war elephants were a real,
    // significant part of Vijayanagara Empire warfare - historically used
    // both as shock cavalry and to batter fortifications, which is why
    // this is kept as UnitClass.Siege (gets CombatBonus's existing 3x
    // Building bonus for free, zero new balance entries) rather than a new
    // class, extending the same "Hampi fortifications" identity their
    // unique tech already leans into. Distinct from SiegeFactory's own
    // Siege unit in shape, not just numbers: much higher HP (a durable
    // battering unit, not SiegeFactory's glass-cannon), lower per-hit
    // damage (SiegeFactory is still the better pure anti-building
    // specialist), and slower - reads as "a different kind of siege unit"
    // rather than a strictly-better SiegeFactory. No elephant model
    // exists, so this reuses the Male Human Character Dummy body/
    // animations like every other unit pending a real pack, same
    // established convention.
    public static class VijayanagaraWarElephantFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));
            // Phase 2 migration: see WorkerFactory's identical note.
            UnitDefinition def = DataRegistry.GetUnit("vijayanagara_war_elephant");
            if (def == null)
            {
                Debug.LogWarning("VijayanagaraWarElephantFactory: no generated UnitDefinition for 'vijayanagara_war_elephant' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            }

            GameObject go = HumanModelFactory.Spawn(HumanModelFactory.Gender.Male, position, civilization);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} War Elephant"
                : $"Enemy {profile.DisplayName} War Elephant";

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.6f;
            agent.height = 2.4f;
            agent.speed = def != null ? def.moveSpeed : 2.2f;

            var unit = go.AddComponent<Unit>();
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            // General garrisoning system (2026-09-01): lets this unit
            // be ordered to walk to and enter a friendly GarrisonPoint -
            // see GarrisonSeeker.
            go.AddComponent<GarrisonSeeker>();
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure((def != null ? def.maxHP : 90f) * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureArmor(
                meleeArmor: (def != null ? def.meleeArmor : 2f) + UpgradeProgress.ArmorBonus(faction) + UpgradeProgress.ClassArmorBonus(faction, UnitClass.Siege),
                pierceArmor: (def != null ? def.pierceArmor : 2f) + UpgradeProgress.ArmorBonus(faction) + UpgradeProgress.ClassArmorBonus(faction, UnitClass.Siege));
            attackable.ConfigureClass(UnitClass.Siege);
            go.AddComponent<HealthBar>();

            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage(def != null ? def.attackDamage : 10f);
            attacker.SetDamageMultiplier(profile.SoldierDamageMultiplier);
            attacker.SetDamageBonus(UpgradeProgress.DamageBonus(faction) + UpgradeProgress.ClassDamageBonus(faction, UnitClass.Siege));
            attacker.SetRange(def != null ? def.attackRange : 2.5f);
            attacker.SetUnitClass(UnitClass.Siege);
            go.AddComponent<StanceController>();

            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<AnimationDriver>().Configure(HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Male), agent, unit);

            // Larger scale than Siege's own Kanabo - reads as a heavier,
            // slower-swung weapon on this bulkier, tankier unit.
            WeaponAttachment.AttachToBone(
                go, HumanBodyBones.RightHand, "Weapons/Kanabo/scene",
                targetSize: 2f, localPositionOffset: new Vector3(0.05f, 0.1f, 0f), localEulerOffset: new Vector3(0f, 0f, 100f));

            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(7f);
            }

            return go;
        }
    }
}
