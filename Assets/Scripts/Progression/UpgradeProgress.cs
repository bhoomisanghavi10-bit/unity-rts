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

        // Wave 3 item 17: each of the 3 tiers now requires its own age,
        // Blacksmith-style (AoE's own Attack/Armor lines gate tier 2 on
        // Feudal and tier 3 on Castle) - previously ungated, any tier was
        // researchable back-to-back the instant a Karmashala existed.
        // Index 0 is the requirement to advance FROM tier 0 TO tier 1, etc -
        // same "array indexed by current tier" shape every other tier
        // line's own RequiredAge field already uses. Tier 1 requires
        // Classical (matching Karmashala's own build-age gate, so this is
        // never a stricter requirement than "the building exists at all"),
        // tier 2 Durg, tier 3 Imperial.
        private static readonly AgeId[] TierRequiredAges = { AgeId.Classical, AgeId.Durg, AgeId.Imperial };

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

        // Wave 3 item 17: the age gate for whichever tier is next - same
        // "HasNextTier && CurrentAge >= RequiredAge" ordinal comparison
        // every other tier line's own NextTierAgeRequirementMet already
        // relies on (AgeId's declared order is Ancient < Classical < Durg <
        // Imperial).
        // Callers (BuildMenu's UpdateUpgradeButton) evaluate this
        // unconditionally alongside HasNextTier, even once maxed - clamp
        // rather than index out of TierRequiredAges' bounds; the returned
        // value is simply unused once HasNextTier is false.
        public static AgeId NextAttackTierRequiredAge(FactionId faction) =>
            TierRequiredAges[System.Math.Min(AttackTier(faction), TierRequiredAges.Length - 1)];
        public static AgeId NextArmorTierRequiredAge(FactionId faction) =>
            TierRequiredAges[System.Math.Min(ArmorTier(faction), TierRequiredAges.Length - 1)];

        public static bool NextAttackTierAgeRequirementMet(FactionId faction) =>
            HasNextAttackTier(faction) && AgeProgress.CurrentAge(faction) >= NextAttackTierRequiredAge(faction);

        public static bool NextArmorTierAgeRequirementMet(FactionId faction) =>
            HasNextArmorTier(faction) && AgeProgress.CurrentAge(faction) >= NextArmorTierRequiredAge(faction);

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

        // Test-only: this class's state is static (persists for the whole
        // Editor/Test-Runner domain, not per-match), so EditMode tests that
        // call AdvanceAttack/AdvanceArmor/etc need a way to isolate
        // themselves from tiers a previous test left behind - same
        // InternalsVisibleTo grant as MeleeAttacker/BoatAttacker's own
        // internal Tick(deltaTime) (see AssemblyInfo.cs).
        internal static void ResetForTests()
        {
            AttackTiers.Clear();
            ArmorTiers.Clear();
            ClassAttackTiers.Clear();
            ClassArmorTiers.Clear();
        }
    }
}
