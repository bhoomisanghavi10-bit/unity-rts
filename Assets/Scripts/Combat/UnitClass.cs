namespace KingdomsOfBharat.Combat
{
    // Broad category an Attackable belongs to, for CombatBonus's counter
    // matrix - separate from DamageType (what an attack deals) since this
    // is about what a target IS, not what it hits with. This project's
    // actual roster: Worker/Soldier are both Infantry, Archer and Cavalry
    // are each their own class, every building is Building. Siege
    // (roadmap item 37) slots in here later without touching anything
    // that already exists.
    public enum UnitClass
    {
        Infantry,
        Archer,
        Cavalry,
        Building,
    }
}
