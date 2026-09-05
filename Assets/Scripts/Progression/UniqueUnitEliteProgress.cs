using System.Collections.Generic;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Progression
{
    // Wave 3 item 16 (docs/IMPLEMENTATION_ROADMAP.md): the Unique-unit Elite
    // tier - every existing unique unit EXCEPT MauryaWarElephantFactory/
    // VijayanagaraWarElephantFactory (those already got their one
    // Durg->Imperial elite step from item 13's ElephantLineProgress -
    // user-confirmed resolution, see that file's own comment) gets a single
    // Durg->Imperial elite upgrade, matching AoE's rule that every unique
    // unit gets exactly one elite tier.
    //
    // Shape mirrors ElephantLineProgress exactly (single Imperial-gated
    // step, same +30 HP/+6 dmg/200 Gold/100 Wood/40s growth every other
    // line's own first Imperial-gate step already uses, not independently
    // balanced) - the only real difference is this tracks 5 DIFFERENT
    // unitIds (one per civ's own unique unit), not one shared table read by
    // whichever faction trains a common unit type, since each of these 5
    // units is genuinely civ-exclusive. Keyed by unitId (not CivilizationId)
    // so Durg's own per-slot UniqueUnitDefinition.UnitId lookup (already
    // established by ElephantLineProgress.IsElephantUnitId) can drive
    // eligibility/tier lookups without a civ switch.
    //
    // Research lives on Durg, not Barracks - same "research where the unit
    // trains" deviation item 13 already established, since every one of
    // these 5 units trains from Durg (UniqueUnitDefinition.Spawn via
    // Durg.RequestTrainUniqueUnit), not Barracks.
    public readonly struct EliteTierData
    {
        public readonly string Name;
        public readonly AgeId RequiredAge;
        public readonly float HpBonus;
        public readonly float DamageBonus;
        public readonly float GoldCost;
        public readonly float WoodCost;
        public readonly float ResearchTime;

        public EliteTierData(string name, AgeId requiredAge, float hpBonus, float damageBonus, float goldCost, float woodCost, float researchTime)
        {
            Name = name;
            RequiredAge = requiredAge;
            HpBonus = hpBonus;
            DamageBonus = damageBonus;
            GoldCost = goldCost;
            WoodCost = woodCost;
            ResearchTime = researchTime;
        }
    }

    public static class UniqueUnitEliteProgress
    {
        private static readonly Dictionary<string, EliteTierData> Elites = new Dictionary<string, EliteTierData>
        {
            { "chola_naval_raider", new EliteTierData("Maha Naval Raider", AgeId.Imperial, hpBonus: 30f, damageBonus: 6f, goldCost: 200f, woodCost: 100f, researchTime: 40f) },
            { "rajput_royal_guard", new EliteTierData("Maha Royal Guard", AgeId.Imperial, hpBonus: 30f, damageBonus: 6f, goldCost: 200f, woodCost: 100f, researchTime: 40f) },
            { "pillar_edict_scholar", new EliteTierData("Maha Pillar Edict Scholar", AgeId.Imperial, hpBonus: 30f, damageBonus: 6f, goldCost: 200f, woodCost: 100f, researchTime: 40f) },
            { "maratha_mavla_raider", new EliteTierData("Maha Mavla Raider", AgeId.Imperial, hpBonus: 30f, damageBonus: 6f, goldCost: 200f, woodCost: 100f, researchTime: 40f) },
            { "maratha_durg_garrison", new EliteTierData("Maha Durg Garrison", AgeId.Imperial, hpBonus: 30f, damageBonus: 6f, goldCost: 200f, woodCost: 100f, researchTime: 40f) },
        };

        private static readonly Dictionary<(FactionId, string), bool> FactionElite = new Dictionary<(FactionId, string), bool>();

        // Data-driven check, same "no hardcoded civ switch" convention as
        // ElephantLineProgress.IsElephantUnitId - lets Durg gate its own
        // per-slot elite button without knowing which civ it belongs to.
        public static bool IsEligible(string unitId) => unitId != null && Elites.ContainsKey(unitId);

        public static EliteTierData DataFor(string unitId) => Elites[unitId];

        public static bool IsElite(FactionId faction, string unitId) =>
            FactionElite.TryGetValue((faction, unitId), out bool elite) && elite;

        public static bool HasNextTier(FactionId faction, string unitId) =>
            IsEligible(unitId) && !IsElite(faction, unitId);

        // Ordinal age comparison relies on AgeId's own declared order
        // (Ancient < Classical < Durg < Imperial), same convention every
        // other line's NextTierAgeRequirementMet already relies on.
        public static bool NextTierAgeRequirementMet(FactionId faction, string unitId) =>
            HasNextTier(faction, unitId) && AgeProgress.CurrentAge(faction) >= DataFor(unitId).RequiredAge;

        public static void AdvanceTier(FactionId faction, string unitId)
        {
            FactionElite[(faction, unitId)] = true;
        }

        // Convenience readers for the 5 factories - fold naturally into
        // each factory's own existing "def != null ? def.X : fallback"
        // spawn-time expression without a separate branch.
        public static string DisplayName(FactionId faction, string unitId, string baseName) =>
            IsElite(faction, unitId) ? DataFor(unitId).Name : baseName;

        public static float HpBonus(FactionId faction, string unitId) =>
            IsElite(faction, unitId) ? DataFor(unitId).HpBonus : 0f;

        public static float DamageBonus(FactionId faction, string unitId) =>
            IsElite(faction, unitId) ? DataFor(unitId).DamageBonus : 0f;

        // Test-only: static state persists for the whole Editor/Test-Runner
        // domain, same isolation need as ElephantLineProgress.ResetForTests.
        internal static void ResetForTests()
        {
            FactionElite.Clear();
        }
    }
}
