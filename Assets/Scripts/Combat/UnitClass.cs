namespace KingdomsOfBharat.Combat
{
    // Broad category an Attackable belongs to, for CombatBonus's counter
    // matrix - separate from DamageType (what an attack deals) since this
    // is about what a target IS, not what it hits with. This project's
    // actual roster: Worker/Soldier are both Infantry, Archer/Cavalry/
    // Siege are each their own class, every building is Building.
    public enum UnitClass
    {
        Infantry,
        Archer,
        Cavalry,
        Siege,
        Building,
        // Item 49: War Galley. One evidenced CombatBonus entry (Naval vs
        // Archer, 0.5x) as of the naval balance pass - every other pairing
        // is still flat 1x, same as any unlisted pairing, pending further
        // audit (see CombatBonus and docs/SESSION_LOG.md).
        Naval,
        // Phase 2 content addition: the anti-cavalry specialist
        // unit_roster_template.csv/counter_matrix_template.csv designed
        // but which had no live factory - see CombatBonus for its 2
        // matchups, sourced from CounterMatrix's own data.
        Spearman,
    }
}
