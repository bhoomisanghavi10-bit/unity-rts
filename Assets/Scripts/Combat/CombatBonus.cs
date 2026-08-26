namespace KingdomsOfBharat.Combat
{
    // AoE-style counter matrix: a damage multiplier keyed by (attacker's
    // class, target's class), applied on top of the base damage/armor math
    // in MeleeAttacker/Attackable rather than replacing it - armor still
    // blunts a hit, this just scales how hard that hit lands first.
    // Matches the actual roster (Worker/Soldier are Infantry, Archer and
    // Cavalry are their own classes, every building is Building) with a
    // complete rock-paper-scissors triangle plus one flavor rule:
    //  - Infantry vs Archer: archers are fast to kill once something
    //    actually reaches melee range with them, so closing the gap is
    //    real counterplay, not just "attack it."
    //  - Archer vs Cavalry: ranged fire punishes a charging, lightly-
    //    armored horse and rider before they close the distance.
    //  - Cavalry vs Archer (the other half of that same matchup, item 43
    //    balance pass): without this, the 1.5x Archer bonus above doesn't
    //    actually hold up - Cavalry's speed (6.5) outruns Archer's (3.8),
    //    so Archer can never kite, and Cavalry's raw stats alone
    //    (6 dmg/40 HP vs 4 dmg/18 HP, no armor either side) already kill
    //    an Archer in 3 hits versus the 7 Archer needs unboosted - the
    //    bonus above wasn't enough to flip that. A flat penalty on
    //    Cavalry's own hit closes the gap the other way instead.
    //  - Cavalry vs Infantry: a mounted charge overwhelms footmen who
    //    can't outrun or out-position it, closing the triangle
    //    (Infantry > Archer > Cavalry > Infantry).
    //  - Archer vs Building: arrows are a poor tool against wood/stone,
    //    same reasoning AoE2 gives its own archer line.
    //  - Siege vs Building: the entire reason a Siege unit exists - built
    //    to crack Walls/Towers a normal army would grind against for
    //    ages, at the cost of being slow and unremarkable (flat 1x, no
    //    special bonus or penalty) against every unit class.
    //  - Naval vs Archer (Phase 5 gap-close, 2026-08-25): the one land/
    //    naval pairing with real evidence of imbalance, found via the same
    //    numeric-audit approach item 43 used for Cavalry vs Archer. War
    //    Galley's 8 base damage against Archer's 18 HP/0 pierce armor
    //    killed an Archer in 3 hits - the exact same number Cavalry's
    //    unboosted stats hit before item 43's fix, and arguably worse
    //    here: a Galley parked off a beach has no melee reach to punish,
    //    so a shore-standing Archer gets none of the counterplay a land
    //    unit at least has against Cavalry. Softened, not eliminated - a
    //    boat should still threaten the coastline, just not for free.
    // Every other pairing (including anything not listed, and every other
    // Naval matchup - no land/naval CombatBonus pass beyond this one
    // evidenced pairing) is a flat 1x - no bonus, no penalty.
    public static class CombatBonus
    {
        public static float Multiplier(UnitClass attacker, UnitClass target)
        {
            if (attacker == UnitClass.Infantry && target == UnitClass.Archer)
            {
                return 1.5f;
            }

            if (attacker == UnitClass.Archer && target == UnitClass.Cavalry)
            {
                return 1.5f;
            }

            if (attacker == UnitClass.Cavalry && target == UnitClass.Archer)
            {
                return 0.4f;
            }

            if (attacker == UnitClass.Cavalry && target == UnitClass.Infantry)
            {
                return 1.5f;
            }

            if (attacker == UnitClass.Archer && target == UnitClass.Building)
            {
                return 0.5f;
            }

            if (attacker == UnitClass.Siege && target == UnitClass.Building)
            {
                return 3f;
            }

            if (attacker == UnitClass.Naval && target == UnitClass.Archer)
            {
                return 0.5f;
            }

            // Spearman (Phase 2 content addition, unit_roster_template.csv/
            // counter_matrix_template.csv): the anti-cavalry specialist -
            // hard-counters Cavalry, but a Spearman blob without support
            // dies fast to plain Infantry. Values match CounterMatrix's own
            // Spearman->Cavalry/Infantry->Spearman entries exactly (see
            // CsvToScriptableObject's CombatBonus/CounterMatrix resolution
            // note) - this is the "add its pairs once Spearman actually
            // exists" case that resolution predicted, not a new design.
            if (attacker == UnitClass.Spearman && target == UnitClass.Cavalry)
            {
                return 2f;
            }

            if (attacker == UnitClass.Infantry && target == UnitClass.Spearman)
            {
                return 1.25f;
            }

            return 1f;
        }
    }
}
