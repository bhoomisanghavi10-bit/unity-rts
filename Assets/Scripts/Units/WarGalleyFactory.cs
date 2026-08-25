using UnityEngine;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.FogOfWar;

namespace KingdomsOfBharat.Units
{
    // Item 49: the naval combat unit - a ranged attacker (BoatAttacker,
    // Pierce damage) that can fight other boats or bombard shore targets.
    // No CombatBonus entries exist for UnitClass.Naval yet (see UnitClass.cs)
    // - flat 1x against everything, a real but simple first pass rather
    // than a fully balanced naval/land interaction matrix.
    public static class WarGalleyFactory
    {
        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));
            // Phase 2 migration: see WorkerFactory's identical note (and
            // FishingBoatFactory's on move speed - same reasoning here).
            UnitDefinition def = DataRegistry.GetUnit("war_galley");
            if (def == null)
            {
                Debug.LogWarning("WarGalleyFactory: no generated UnitDefinition for 'war_galley' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            }

            // "CombatShip" is the sourced model's actual name (Ships/CombatShip.prefab) -
            // this class/method stays "WarGalley" as the internal gameplay term.
            GameObject go = BoatModelFactory.Spawn("CombatShip", position, profile.PrimaryColor, isWarGalley: true);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} War Galley"
                : $"Enemy {profile.DisplayName} War Galley";

            go.AddComponent<Unit>();
            go.AddComponent<WaterMover>();
            go.AddComponent<SelectionIndicator>();
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure((def != null ? def.maxHP : 45f) * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureArmor(
                meleeArmor: (def != null ? def.meleeArmor : 0f) + UpgradeProgress.ArmorBonus(faction),
                pierceArmor: (def != null ? def.pierceArmor : 0f) + UpgradeProgress.ArmorBonus(faction));
            attackable.ConfigureClass(UnitClass.Naval);
            go.AddComponent<HealthBar>();

            var attacker = go.AddComponent<BoatAttacker>();
            attacker.SetBaseDamage(def != null ? def.attackDamage : 8f);
            attacker.SetDamageMultiplier(profile.SoldierDamageMultiplier);
            attacker.SetDamageBonus(UpgradeProgress.DamageBonus(faction));
            attacker.SetRange(def != null ? def.attackRange : 4f);

            go.AddComponent<FactionMember>().Configure(faction);

            var collider = go.AddComponent<CapsuleCollider>();
            collider.radius = 0.6f;
            collider.height = 3f;

            if (faction == FactionId.Player)
            {
                go.AddComponent<VisionSource>().Configure(9f);
            }

            return go;
        }
    }
}
