namespace KingdomsOfBharat.Core
{
    // AoE-parity Phase 3.2: AoE2/4-style team bonuses - each civ shares a
    // diluted version of its own unique-tech identity with every ally,
    // unconditionally (not gated on the ally having researched anything).
    // civ_bonus_template.csv already spells out all 5 (its "TeamBonus"
    // column), and CivilizationDefinition.teamBonus even parses them into a
    // StatModifier at CSV-import time - but that parse is unreliable for
    // most of them (UnitCategory has no "Building" entry, so Houses/
    // fortifications/Markets can't be represented that way) and is never
    // actually read anywhere at runtime. Hand-written hooks instead, same
    // bespoke-per-civ-mechanic convention as UniqueTechDefinition/
    // RajputDefianceHook/BuildingPlacer.WoodMultiplierFor.
    public static class TeamBonus
    {
        private static readonly FactionId[] AllFactions = { FactionId.Player, FactionId.Enemy, FactionId.Enemy2 };

        // Maurya: allied factions' Houses cost 25% less Wood.
        public const float MauryaHouseWoodMultiplier = 0.75f;

        // Vijayanagara: allied Wall/Gate/Tower get +15% max HP.
        public const float VijayanagaraFortificationHealthMultiplier = 1.15f;

        // Rajput: allied Cavalry get +1 flat damage.
        public const float RajputCavalryDamageBonus = 1f;

        // Maratha: allied Cavalry get +10% move speed.
        public const float MarathaCavalryMoveSpeedMultiplier = 1.10f;

        // Chola: allied Markets get a narrowed +/-5-point buy/sell spread.
        public const float CholaMarketRateBonus = 0.05f;

        // True if `faction` has at least one OTHER allied faction whose
        // civilization is `civ`. Never counts a faction's own civ as its
        // own ally - DiplomacyRegistry.AreAllied(a, a) is true, but a
        // civ's own bonus is a separate, already-existing code path from
        // its team bonus, so self is explicitly excluded here. Returns a
        // bool rather than a count: two allied civs of the same kind
        // don't stack the team bonus twice, matching AoE2/4 convention.
        public static bool HasAlly(FactionId faction, CivilizationId civ)
        {
            foreach (FactionId other in AllFactions)
            {
                if (other == faction)
                {
                    continue;
                }

                if (DiplomacyRegistry.AreAllied(faction, other) && CivilizationRegistry.For(other) == civ)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
