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
    }
}
