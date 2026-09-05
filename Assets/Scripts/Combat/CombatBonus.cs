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
    //    armored horse and rider before they close the distance. AoE-parity
    //    Phase 2 audit (2026-09-01): raised from 1.5x to 2.0x after a
    //    numeric audit found 1.5x resolved this matchup as a near coin-flip
    //    in practice, not a real hard counter - a stationary duel against
    //    base-stat Cavalry (40 HP, 0.4x-penalized 6 dmg swing, see below)
    //    killed Cavalry in 7 hits at 1.5x, but Cavalry's own return hits
    //    landed the Archer at just 1.2/18 HP (7%) remaining when it died -
    //    tactically indistinguishable from a coin-flip once any pathing/
    //    positioning noise is added. 2.0x kills Cavalry in 5 hits with the
    //    Archer still at 6/18 HP (33%) remaining - a clear, not-a-coin-flip
    //    win. 2.5x was also modeled (4 hits, 47% HP remaining) and rejected
    //    as stronger than needed; 1.5x/Cavalry->Infantry and Infantry->
    //    Archer were audited in the same pass and left untouched - both
    //    already resolve decisively (loser retains only ~20-30% max HP),
    //    so raising them further only compresses already-fast fights
    //    without changing the outcome (a "hits to kill" ceiling effect from
    //    this project's flat-armor-subtraction damage model). User-approved
    //    change, not a unilateral rebalance - see docs/SESSION_LOG.md.
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
                return 2f;
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

            // Skirmisher (Wave 4 item 19, unit_roster_template.csv/
            // docs/IMPLEMENTATION_ROADMAP.md): the dedicated anti-archer
            // specialist, closing the last gap in the counter web (Infantry
            // > Archer, Archer > Cavalry, Cavalry > Infantry/Spearman >
            // Cavalry all already existed; nothing hard-countered Archer's
            // own counter-pick before this). Values reuse Spearman's own
            // pairing exactly (2x hard-counter vs its target, 1.25x
            // received from Infantry closing the gap on a lightly-armored
            // specialist) - the closest existing precedent for "a unit
            // built specifically to counter one other class," not
            // independently balanced.
            if (attacker == UnitClass.Skirmisher && target == UnitClass.Archer)
            {
                return 2f;
            }

            if (attacker == UnitClass.Infantry && target == UnitClass.Skirmisher)
            {
                return 1.25f;
            }

            // Battering Ram (Wave 4 item 20, unit_roster_template.csv
            // "battering_ram"): the dedicated anti-building specialist -
            // steeper than Siege's own 3x since a Ram's entire kit is
            // "hit buildings" (MeleeAttacker.SetBuildingOnly enforces it
            // literally cannot target a unit at all, unlike Siege which
            // still can, just unremarkably) - no splash either
            // (BatteringRamFactory never calls SetSplashRadius), so this
            // single-target bonus is the whole point of building one.
            // Not independently balanced - picked to sit clearly above
            // Siege's 3x while still resolvable against a Wall/Tower's
            // real HP/armor in a reasonable number of hits.
            if (attacker == UnitClass.BatteringRam && target == UnitClass.Building)
            {
                return 4f;
            }

            // Camel Rider (Wave 4 item 22, unit_roster_template.csv
            // "camel_rider"): a second anti-cavalry specialist alongside
            // Spearman, this one mounted (Cavalry's own move speed) rather
            // than a footman - see CamelRiderFactory. Values reuse
            // Spearman's own pairing exactly (2x hard-counter vs its
            // target, 1.25x received from Infantry), the closest existing
            // precedent for "a unit built specifically to counter Cavalry,"
            // not independently balanced.
            if (attacker == UnitClass.Camel && target == UnitClass.Cavalry)
            {
                return 2f;
            }

            if (attacker == UnitClass.Infantry && target == UnitClass.Camel)
            {
                return 1.25f;
            }

            // Scorpion (Wave 4 item 23, unit_roster_template.csv
            // "scorpion"): the dedicated anti-infantry siege weapon - its
            // pierce-through bolt (see MeleeAttacker.SetPierceThrough)
            // shreds massed infantry lined up behind its primary target,
            // matching AoE's own Scorpion identity. Reuses the closest
            // existing "hard counter vs one class" precedent value (2x,
            // same as Archer->Cavalry/Skirmisher->Archer/Camel->Cavalry),
            // not independently balanced. The matching weakness reuses
            // Cavalry->Infantry's own 1.5x - a fast unit closes the gap on
            // an unarmored, immobile siege engine before it can fire twice,
            // the same vulnerability real AoE Scorpions have to cavalry
            // raids.
            if (attacker == UnitClass.Scorpion && target == UnitClass.Infantry)
            {
                return 2f;
            }

            if (attacker == UnitClass.Cavalry && target == UnitClass.Scorpion)
            {
                return 1.5f;
            }

            // Fire Ship (Wave 4 item 25, unit_roster_template.csv
            // "fire_ship"): the dedicated anti-naval specialist - a fast,
            // fragile ship whose Fire damage (see FireShipFactory,
            // DamageType.Fire's first real consumer) is built to burn out
            // an enemy hull quickly. Reuses the closest existing "hard
            // counter vs one class" precedent value (2x, same as
            // Archer->Cavalry/Skirmisher->Archer/Camel->Cavalry/
            // Scorpion->Infantry), not independently balanced. The
            // matching weakness reuses Cavalry->Scorpion's own 1.5x - a
            // regular War Galley punishes this glass-cannon specialist if
            // it closes the distance first, the same vulnerability real
            // AoE Fire Ships have to a defended battle line.
            if (attacker == UnitClass.FireShip && target == UnitClass.Naval)
            {
                return 2f;
            }

            if (attacker == UnitClass.Naval && target == UnitClass.FireShip)
            {
                return 1.5f;
            }

            return 1f;
        }
    }
}
