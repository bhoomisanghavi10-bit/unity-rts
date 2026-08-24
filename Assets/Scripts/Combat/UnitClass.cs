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
        // Item 49: War Galley. Deliberately no CombatBonus entries yet -
        // flat 1x against everything, same as any unlisted pairing -
        // land/naval balance is a separate concern from just having ships
        // exist and fight.
        Naval,
    }
}
