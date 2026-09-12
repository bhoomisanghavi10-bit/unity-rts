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
    // Creates a "Scout" (Chara) unit - Wave 4 item 18
    // (docs/IMPLEMENTATION_ROADMAP.md), the first Wave 4 item, closing the
    // roadmap's own "single most conspicuous missing unit" gap now that Fog
    // of War exists (FogOfWarManager). Deliberately built as an exploration
    // unit, not a combat one, which is why it deviates from every other
    // Barracks-trained factory in three ways: no StanceController (matches
    // WorkerFactory, which also carries a weak self-defense MeleeAttacker
    // but isn't meant to auto-engage); no
    // EnableUpgradeArmorScaling/EnableUpgradeDamageScaling opt-in (same
    // "utility unit, not a combat unit" call WorkerFactory already made -
    // the 13 combat factories that DO opt in are all real fighters); and a
    // VisionSource radius well above the standard 8f every other Player
    // unit uses, since a scout's whole purpose is revealing fog faster/
    // farther than the army it's screening for. Uses UnitClass.Support for
    // its Attackable/MeleeAttacker classification - the first live unit to
    // do so for real combat resolution (Worker uses Infantry for that, only
    // Support for its own move-speed multiplier lookup) - safe since
    // CombatBonus/CounterMatrix have zero Support entries yet, matching
    // Wave 0 item 1's own note that Support was added with no live
    // consumer.
    //
    // No dedicated Scout-horse model exists yet (docs/YOUR_ACTION_ITEMS.md
    // item 18 specs a lean, riderless mount, not yet delivered) - reuses
    // the same "Mounts/Horse/scene" placeholder CavalryFactory already
    // uses, same "primitive/placeholder until a real pack lands"
    // convention as everywhere else in this project. Flagging this
    // directly: Scout currently looks identical to Cavalry in the field: a
    // real, visually distinct mount is a genuine future asset need, not
    // just a procedural-shape gap.
    public static class ScoutFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));
            UnitDefinition def = DataRegistry.GetUnit("chara");
            if (def == null)
            {
                Debug.LogWarning("ScoutFactory: no generated UnitDefinition for 'chara' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            }

            // Wave 4 item 18: the Scout tier ladder - baked in at spawn, not
            // retroactive, same convention as every other line.
            ScoutTierData tier = ScoutLineProgress.Current(faction);

            GameObject go = HumanModelFactory.Spawn(HumanModelFactory.Gender.Male, position, civilization, faction: faction);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} {tier.Name}"
                : $"Enemy {profile.DisplayName} {tier.Name}";

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.speed = (def != null ? def.moveSpeed : 7.5f) + tier.SpeedBonus;
            agent.speed *= CivilizationProfile.FindCategoryMultiplier(civilization, StatType.MoveSpeed, UnitClass.Support);

            var unit = go.AddComponent<Unit>();
            unit.IconKey = "train_chara";
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            // General garrisoning system (2026-09-01): lets this unit be
            // ordered to walk to and enter a friendly GarrisonPoint - see
            // GarrisonSeeker.
            go.AddComponent<GarrisonSeeker>();

            var attackable = go.AddComponent<Attackable>();
            attackable.Configure(((def != null ? def.maxHP : 20f) + tier.HpBonus) * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureArmor(
                meleeArmor: def != null ? def.meleeArmor : 0f,
                pierceArmor: def != null ? def.pierceArmor : 0f);
            attackable.ConfigureClass(UnitClass.Support);
            go.AddComponent<HealthBar>();

            // Weak flat self-defense attack, matching WorkerFactory's
            // convention - never scaled by tier or UpgradeProgress research,
            // since this line's own growth axis is vision/speed, not
            // combat (see ScoutLineProgress).
            var attacker = go.AddComponent<MeleeAttacker>();
            attacker.SetBaseDamage(def != null ? def.attackDamage : 2f);
            attacker.SetRange(def != null ? def.attackRange : 1f);
            attacker.SetUnitClass(UnitClass.Support);

            go.AddComponent<FactionMember>().Configure(faction);
            go.AddComponent<AnimationDriver>().Configure(HumanAnimationSet.LoadFor(HumanModelFactory.Gender.Male), agent, unit);

            // Placeholder mount - see the class-level comment above.
            WeaponAttachment.AttachBeside(
                go, "Mounts/Horse/scene",
                targetSize: 2.2f, localPositionOffset: new Vector3(0f, 0f, -0.6f), localEulerOffset: Vector3.zero);

            // See WorkerFactory: only Player vision feeds FogOfWarManager.
            // 12f base (vs. every other unit's 8f) plus this tier's own
            // VisionBonus - the actual point of this unit.
            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(12f + tier.VisionBonus);
            }

            return go;
        }
    }
}
