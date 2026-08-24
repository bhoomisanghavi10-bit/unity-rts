using System.Collections.Generic;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Combat;

namespace KingdomsOfBharat.Progression
{
    // Which military upgrade tier each faction has researched - AoE's
    // Blacksmith-style Attack/Armor lines, simplified to a flat tier count
    // per track (each tier = +damage or +armor) rather than separate named
    // techs, since there's no dedicated Blacksmith building yet - Barracks
    // researches both (see Barracks.RequestResearchAttack/Armor) and every
    // unit class benefits equally.
    //
    // ClassAttackTier/ClassArmorTier below are a SEPARATE, additive layer
    // on top of this - genuine per-unit-class tech tree depth (item 40:
    // "Archery Range/Stable-style upgrades beyond flat Attack/Armor")
    // without requiring dedicated buildings per class yet, and without
    // changing this file's or Barracks' existing public API (BuildMenu's
    // parameterless RequestResearchAttack()/RequestResearchArmor() calls
    // keep working untouched - this project only has one production
    // building today, so a hard split into separate Archery Range/Stable
    // buildings is future work, not this item).
    public static class UpgradeProgress
    {
        public const int MaxTier = 3;
        private const float DamagePerTier = 2f;
        private const float ArmorPerTier = 1f;

        // Per-class tracks use half the flat tracks' per-tier value -
        // additive on top of an already-active flat bonus, not a
        // replacement, so the combined total at max tiers in both stays
        // comparable to what a single doubled-up flat track would give.
        private const float ClassDamagePerTier = 1f;
        private const float ClassArmorPerTier = 0.5f;

        private static readonly Dictionary<FactionId, int> AttackTiers = new Dictionary<FactionId, int>();
        private static readonly Dictionary<FactionId, int> ArmorTiers = new Dictionary<FactionId, int>();
        private static readonly Dictionary<(FactionId, UnitClass), int> ClassAttackTiers = new Dictionary<(FactionId, UnitClass), int>();
        private static readonly Dictionary<(FactionId, UnitClass), int> ClassArmorTiers = new Dictionary<(FactionId, UnitClass), int>();

        public static int AttackTier(FactionId faction)
        {
            return AttackTiers.TryGetValue(faction, out int tier) ? tier : 0;
        }

        public static int ArmorTier(FactionId faction)
        {
            return ArmorTiers.TryGetValue(faction, out int tier) ? tier : 0;
        }

        public static bool HasNextAttackTier(FactionId faction) => AttackTier(faction) < MaxTier;
        public static bool HasNextArmorTier(FactionId faction) => ArmorTier(faction) < MaxTier;

        public static void AdvanceAttack(FactionId faction)
        {
            AttackTiers[faction] = AttackTier(faction) + 1;
        }

        public static void AdvanceArmor(FactionId faction)
        {
            ArmorTiers[faction] = ArmorTier(faction) + 1;
        }

        // Read by SoldierFactory/ArcherFactory/CavalryFactory/SiegeFactory
        // at spawn time and baked in, same "not retroactive" convention as
        // CivilizationProfile/AgeProfile bonuses - researching a tier only
        // benefits units trained after.
        public static float DamageBonus(FactionId faction)
        {
            return AttackTier(faction) * DamagePerTier;
        }

        public static float ArmorBonus(FactionId faction)
        {
            return ArmorTier(faction) * ArmorPerTier;
        }

        // --- Per-class layer (item 40) ---

        public static int ClassAttackTier(FactionId faction, UnitClass unitClass)
        {
            return ClassAttackTiers.TryGetValue((faction, unitClass), out int tier) ? tier : 0;
        }

        public static int ClassArmorTier(FactionId faction, UnitClass unitClass)
        {
            return ClassArmorTiers.TryGetValue((faction, unitClass), out int tier) ? tier : 0;
        }

        public static bool HasNextClassAttackTier(FactionId faction, UnitClass unitClass) => ClassAttackTier(faction, unitClass) < MaxTier;
        public static bool HasNextClassArmorTier(FactionId faction, UnitClass unitClass) => ClassArmorTier(faction, unitClass) < MaxTier;

        public static void AdvanceClassAttack(FactionId faction, UnitClass unitClass)
        {
            ClassAttackTiers[(faction, unitClass)] = ClassAttackTier(faction, unitClass) + 1;
        }

        public static void AdvanceClassArmor(FactionId faction, UnitClass unitClass)
        {
            ClassArmorTiers[(faction, unitClass)] = ClassArmorTier(faction, unitClass) + 1;
        }

        // Added on top of the flat DamageBonus/ArmorBonus above, not in
        // place of it - see each unit factory's UpgradeProgress calls.
        public static float ClassDamageBonus(FactionId faction, UnitClass unitClass)
        {
            return ClassAttackTier(faction, unitClass) * ClassDamagePerTier;
        }

        public static float ClassArmorBonus(FactionId faction, UnitClass unitClass)
        {
            return ClassArmorTier(faction, unitClass) * ClassArmorPerTier;
        }
    }
}
