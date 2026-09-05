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
        // AoE-Parity Wave 0 item 1: merged in from the former standalone
        // UnitCategory enum (Assets/Scripts/Data/Scripts/UnitDefinition.cs),
        // which had these two values with no UnitClass equivalent - the
        // reason Support/Hero units (Vaidya/Purohita, Maharaja - Wave 4)
        // had no way to get a real runtime combat class. No live unit
        // reports either yet; CombatBonus.Multiplier falls through to its
        // 1f default for both until a real pairing is designed.
        Support,
        Hero,
        // Wave 4 item 19: the dedicated anti-archer specialist
        // (unit_roster_template.csv "skirmisher") - see CombatBonus for its
        // 2 matchups (hard-counters Archer, weak vs Infantry), sourced the
        // same way Spearman's own pairing was: reuse the closest existing
        // precedent, not independently balanced.
        Skirmisher,
    }
}
