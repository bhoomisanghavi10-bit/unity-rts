namespace KingdomsOfBharat.Combat
{
    // Broad category an Attackable belongs to, for CombatBonus's counter
    // matrix - separate from DamageType (what an attack deals) since this
    // is about what a target IS, not what it hits with. Deliberately just
    // three values today (this project's actual roster: Worker/Soldier are
    // both Infantry, Archer is its own class, every building is Building)-
    // Cavalry and Siege (roadmap items 36-37) slot in here later without
    // touching anything that already exists.
    public enum UnitClass
    {
        Infantry,
        Archer,
        Building,
    }
}
