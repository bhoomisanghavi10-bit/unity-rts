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
        // Wave 4 item 20: the dedicated anti-building specialist
        // (unit_roster_template.csv "battering_ram") - distinct from Siege
        // (which can still hit units, just unremarkably) in that a
        // Battering Ram literally cannot be ordered to attack a unit at
        // all (see MeleeAttacker.SetBuildingOnly) - it only ever fights
        // buildings, at a steeper CombatBonus than Siege's own 3x since
        // that's its entire job.
        BatteringRam,
        // Wave 4 item 22: the Camel Rider - a mounted anti-cavalry
        // specialist (unit_roster_template.csv "camel_rider"). Design call
        // made explicitly (the roadmap fixes tier names/ages, not what
        // class this counts as for combat purposes): a genuinely new class
        // rather than folded into Cavalry or Spearman, since it needs both
        // Spearman's hard-counter-vs-Cavalry trait AND Cavalry's own move
        // speed at once - see CombatBonus for its 2 matchups, values reused
        // directly from Spearman's own pairing (closest existing
        // precedent for "a unit built to counter Cavalry"), not
        // independently balanced.
        Camel,
        // Wave 4 item 23: the Scorpion - a dedicated anti-infantry siege
        // weapon (unit_roster_template.csv "scorpion"). Its whole point is
        // a pierce-through bolt that hits whatever stands behind its
        // primary target too (see MeleeAttacker.SetPierceThrough), so it
        // gets its own class rather than folding into Siege (which is
        // anti-building via CombatBonus's 3x, not anti-infantry) - see
        // CombatBonus for its 2 matchups.
        Scorpion,
        // Wave 4 item 25: the Fire Ship - a dedicated anti-naval
        // specialist (unit_roster_template.csv "fire_ship"), the first
        // real consumer of DamageType.Fire (declared in Wave 0 item 4,
        // unused by any live attacker until this item). Gets its own class
        // rather than folding into Naval, since it needs a genuinely
        // asymmetric matchup with War Galley: a hard counter when it
        // closes to range, but fragile if a Galley reaches it first - see
        // CombatBonus for its 2 matchups.
        FireShip,
        // Wave 4 item 24: the Trebuchet - a long-range anti-building
        // specialist with a hard minimum range (see
        // MeleeAttacker.SetMinRange), the first single-tier trainable
        // combat unit in the project. Gets its own class rather than
        // folding into Siege (which would collide with Attackable.
        // SiegeImmune's Siege-specific check) - see CombatBonus for its
        // 2 matchups.
        Trebuchet,
    }
}
