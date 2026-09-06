using UnityEngine;
using UnityEngine.AI;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.Buildings;

namespace KingdomsOfBharat.Combat
{
    // Wave 4 item 20 (docs/IMPLEMENTATION_ROADMAP.md): the Battering Ram -
    // an anti-building specialist distinct from the existing Mangonel-like
    // Siege unit (SiegeFactory) in 3 ways the roadmap's own spec calls out:
    //  - Anti-building ONLY: MeleeAttacker.SetBuildingOnly(true) means an
    //    AttackMove order against anything but a Building-class target is
    //    flatly refused, not just weak against units (unlike Siege, which
    //    can still fight units at a flat unremarkable 1x).
    //  - No splash: SetSplashRadius is never called (stays at the default
    //    0/disabled), unlike Siege's own 2.25 - single-target only.
    //  - Garrisonable: unlike every combat-unit factory, this one adds a
    //    GarrisonPoint to ITSELF (not a GarrisonSeeker - a Battering Ram
    //    doesn't walk into buildings, it hosts friendly land units inside
    //    it for protection while it closes on a wall, the real AoE II
    //    mechanic). GarrisonPoint/GarrisonSeeker already generalize to a
    //    non-Building host with zero changes needed - GarrisonSeeker's own
    //    ComputeApproachPoint already falls back to the target's raw
    //    transform.position when it has no BuildingFootprintTag, exactly
    //    this case, and GarrisonPoint.OnDestroy already ungarrisons
    //    everyone if the Ram dies with occupants aboard.
    // Deliberately does NOT add a StanceController (see ScoutFactory's own
    // "utility unit, not a combat unit" precedent for the shape of this
    // choice, though the reasoning here is different): Aggressive/Defensive
    // auto-engage scans for any nearby hostile Attackable regardless of
    // class, and a buildingOnly attacker parked chasing a phantom "target"
    // it can structurally never hit is worse than simply requiring an
    // explicit player order onto a specific building every time, matching
    // real AoE II's own ram (never auto-engages).
    // No dedicated Battering Ram model exists yet
    // (docs/YOUR_ACTION_ITEMS.md has no entry for it) - reuses the same
    // Human Character Dummy body + Kanabo weapon as SiegeFactory (whose own
    // comment already flags "a real siege engine, e.g. a battering ram,
    // would be a better fit long-term" for that exact prop) since no
    // dedicated ram/cart model exists yet - **flagging directly per the
    // flag-asset-needs convention: a Battering Ram currently looks
    // identical to a Siege unit in the field.**
    public static class BatteringRamFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));
            // Phase 2 migration: see WorkerFactory's identical note.
            UnitDefinition def = DataRegistry.GetUnit("battering_ram");
            if (def == null)
            {
                Debug.LogWarning("BatteringRamFactory: no generated UnitDefinition for 'battering_ram' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            }

            // Wave 4 item 20: Battering Ram tier ladder - baked in at
            // spawn, not retroactive, same convention as every other
            // combat-unit tier line.
            BatteringRamTierData tier = BatteringRamLineProgress.Current(faction);

            GameObject go = HumanModelFactory.Spawn(HumanModelFactory.Gender.Male, position, civilization, faction: faction);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} {tier.Name}"
                : $"Enemy {profile.DisplayName} {tier.Name}";

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.5f;
            agent.height = 2f;
            agent.speed = def != null ? def.moveSpeed : 1.3f;

            var unit = go.AddComponent<Unit>();
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure(((def != null ? def.maxHP : 80f) + tier.HpBonus) * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureArmor(
                meleeArmor: def != null ? def.meleeArmor : 3f,
                pierceArmor: def != null ? def.pierceArmor : 0f);
            attackable.ConfigureClass(UnitClass.BatteringRam);
            attackable.EnableUpgradeArmorScaling();
            go.AddComponent<Repairable>();
            go.AddComponent<HealthBar>();

            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage((def != null ? def.attackDamage : 18f) + tier.DamageBonus);
            attacker.SetDamageMultiplier(profile.SoldierDamageMultiplier);
            attacker.SetRange(def != null ? def.attackRange : 1.5f);
            attacker.SetUnitClass(UnitClass.BatteringRam);
            attacker.SetBuildingOnly(true);
            attacker.EnableUpgradeDamageScaling();

            // Garrisonable (the roadmap's own 3rd distinguishing trait):
            // hosts up to 4 friendly land units for protection - same
            // capacity as Tower, a reasonable "small escort" size, not
            // independently balanced.
            go.AddComponent<GarrisonPoint>().Configure(4);

            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<AnimationDriver>().Configure(HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Male), agent, unit);

            // Purely cosmetic - see SoldierFactory's identical note. Same
            // Kanabo as Siege (see this class's own header comment on the
            // missing dedicated model).
            WeaponAttachment.AttachToBone(
                go, HumanBodyBones.RightHand, "Weapons/Kanabo/scene",
                targetSize: 1.5f, localPositionOffset: new Vector3(0.05f, 0.1f, 0f), localEulerOffset: new Vector3(0f, 0f, 100f));

            // See WorkerFactory: only Player vision feeds FogOfWarManager.
            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(6f);
            }

            return go;
        }
    }
}
