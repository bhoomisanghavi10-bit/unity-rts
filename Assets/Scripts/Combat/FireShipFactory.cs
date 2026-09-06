using UnityEngine;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.FogOfWar;

namespace KingdomsOfBharat.Combat
{
    // Wave 4 item 25 (docs/IMPLEMENTATION_ROADMAP.md): the Fire Ship - a
    // dedicated anti-naval specialist and the first real consumer of
    // DamageType.Fire (declared in Wave 0 item 4, unused by any live
    // attacker until this item - Attackable.TakeDamage already resolves
    // Fire against pierceArmor, the same "hits from range" bucket Pierce
    // itself uses). Distinct from War Galley in role: a fast, fragile
    // glass cannon (CombatBonus FireShip->Naval 2x) that dies fast if a
    // regular War Galley reaches it first (Naval->FireShip 1.5x, reusing
    // Cavalry->Scorpion's own "counter-specialist is vulnerable to the
    // class it counters" precedent) - see UnitClass.FireShip/CombatBonus.
    // Base shape mirrors WarGalleyFactory's own (BoatModelFactory/
    // WaterMover/BoatAttacker), not MeleeAttacker - a boat, like War
    // Galley, not a land unit. No dedicated Fire Ship model exists yet -
    // reuses the same "CombatShip" hull WarGalleyFactory uses, since no
    // other ship model is sourced - flagging directly per the
    // flag-asset-needs convention: a Fire Ship currently looks identical
    // to a War Galley in the field, distinguished only by a deliberate
    // fire-orange tint instead of the civ's own primary color (a partial,
    // cheap visual differentiator, not a substitute for real art).
    public static class FireShipFactory
    {
        // Blended into the hull material at BoatModelFactory's own fixed
        // 0.35 lerp weight, same as every other boat's civ-color tint -
        // used here in place of profile.PrimaryColor so a Fire Ship reads
        // as visually distinct from a War Galley of the same civ despite
        // sharing the identical "CombatShip" mesh.
        private static readonly Color FireTint = new Color(0.85f, 0.32f, 0.05f);

        public static GameObject Spawn(Vector3 position, FactionId faction)
        {
            CivilizationId civilization = CivilizationRegistry.For(faction);
            CivilizationProfile profile = CivilizationProfile.For(civilization);
            AgeProfile age = AgeProfile.For(AgeProgress.CurrentAge(faction));
            UnitDefinition def = DataRegistry.GetUnit("fire_ship");
            if (def == null)
            {
                Debug.LogWarning("FireShipFactory: no generated UnitDefinition for 'fire_ship' - using fallback stats. Run BharatRTS/Generate Data Assets From CSV.");
            }

            // Fire Ship tier ladder - baked in at spawn, not retroactive,
            // same convention as every other combat-unit tier line.
            FireShipTierData tier = FireShipLineProgress.Current(faction);

            GameObject go = BoatModelFactory.Spawn("CombatShip", position, FireTint, isWarGalley: true, faction: faction);
            go.name = faction == FactionId.Player
                ? $"{profile.DisplayName} {tier.Name}"
                : $"Enemy {profile.DisplayName} {tier.Name}";

            go.AddComponent<Unit>();
            go.AddComponent<WaterMover>();
            go.AddComponent<SelectionIndicator>();
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure(((def != null ? def.maxHP : 30f) + tier.HpBonus) * profile.MaxHealthMultiplier * age.MaxHealthMultiplier);
            attackable.ConfigureArmor(
                meleeArmor: def != null ? def.meleeArmor : 0f,
                pierceArmor: def != null ? def.pierceArmor : 0f);
            attackable.ConfigureClass(UnitClass.FireShip);
            attackable.EnableUpgradeArmorScaling();
            go.AddComponent<Repairable>();
            go.AddComponent<HealthBar>();

            var attacker = go.AddComponent<BoatAttacker>();
            attacker.SetBaseDamage((def != null ? def.attackDamage : 14f) + tier.DamageBonus);
            attacker.SetDamageMultiplier(profile.SoldierDamageMultiplier);
            attacker.SetRange(def != null ? def.attackRange : 3f);
            attacker.SetDamageType(DamageType.Fire);
            attacker.SetUnitClass(UnitClass.FireShip);
            attacker.EnableUpgradeDamageScaling();

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
