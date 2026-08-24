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

            GameObject go = BoatModelFactory.Spawn("WarGalley", position, profile.PrimaryColor, isWarGalley: true);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} War Galley"
                : $"Enemy {profile.DisplayName} War Galley";

            go.AddComponent<Unit>();
            go.AddComponent<WaterMover>();
            go.AddComponent<SelectionIndicator>();
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure(45f * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureArmor(
                meleeArmor: UpgradeProgress.ArmorBonus(faction),
                pierceArmor: UpgradeProgress.ArmorBonus(faction));
            attackable.ConfigureClass(UnitClass.Naval);
            go.AddComponent<HealthBar>();

            var attacker = go.AddComponent<BoatAttacker>();
            attacker.SetBaseDamage(8f);
            attacker.SetDamageMultiplier(profile.SoldierDamageMultiplier);
            attacker.SetDamageBonus(UpgradeProgress.DamageBonus(faction));
            attacker.SetRange(4f);

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
