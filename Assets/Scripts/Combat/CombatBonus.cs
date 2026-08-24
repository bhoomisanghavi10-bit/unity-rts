namespace KingdomsOfBharat.Combat
{
    // AoE-style counter matrix: a damage multiplier keyed by (attacker's
    // class, target's class), applied on top of the base damage/armor math
    // in MeleeAttacker/Attackable rather than replacing it - armor still
    // blunts a hit, this just scales how hard that hit lands first.
    // Deliberately sparse today, matching the actual roster (Worker/Soldier
    // are Infantry, Archer is its own class, every building is Building):
    //  - Infantry vs Archer: archers are fast to kill once something
    //    actually reaches melee range with them, so closing the gap is
    //    real counterplay, not just "attack it."
    //  - Archer vs Building: arrows are a poor tool against wood/stone,
    //    same reasoning AoE2 gives its own archer line.
    // Every other pairing (including anything not listed) is a flat 1x -
    // no bonus, no penalty. Cavalry/Siege (roadmap items 36-37) each add
    // one or two more entries here when they land, not a redesign.
    public static class CombatBonus
    {
        public static float Multiplier(UnitClass attacker, UnitClass target)
        {
            if (attacker == UnitClass.Infantry && target == UnitClass.Archer)
            {
                return 1.5f;
            }

            if (attacker == UnitClass.Archer && target == UnitClass.Building)
            {
                return 0.5f;
            }

            return 1f;
        }
    }
}
