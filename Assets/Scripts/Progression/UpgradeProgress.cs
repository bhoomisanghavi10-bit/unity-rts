using System.Collections.Generic;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Progression
{
    // Which military upgrade tier each faction has researched - same bare
    // static-registry shape as AgeProgress/CivilizationRegistry. AoE's
    // Blacksmith-style Attack/Armor lines, simplified to a flat tier count
    // per track (each tier = +damage or +armor) rather than separate named
    // techs, since there's no dedicated Blacksmith building yet - Barracks
    // researches both (see Barracks.RequestResearchAttack/Armor).
    public static class UpgradeProgress
    {
        public const int MaxTier = 3;
        private const float DamagePerTier = 2f;
        private const float ArmorPerTier = 1f;

        private static readonly Dictionary<FactionId, int> AttackTiers = new Dictionary<FactionId, int>();
        private static readonly Dictionary<FactionId, int> ArmorTiers = new Dictionary<FactionId, int>();

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

        // Read by SoldierFactory/ArcherFactory at spawn time and baked in,
        // same "not retroactive" convention as CivilizationProfile/AgeProfile
        // bonuses - researching a tier only benefits units trained after.
        public static float DamageBonus(FactionId faction)
        {
            return AttackTier(faction) * DamagePerTier;
        }

        public static float ArmorBonus(FactionId faction)
        {
            return ArmorTier(faction) * ArmorPerTier;
        }
    }
}
