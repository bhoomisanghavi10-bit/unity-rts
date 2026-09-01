namespace KingdomsOfBharat.ResourceGathering
{
    // Roadmap Section 1 (worker self-defense/cross-awareness, AoE-parity
    // Phase 4.2): how a Gatherer reacts to being attacked mid-task. Per-civ
    // default lives in WorkerCombatResponseDefaults, not here - this enum is
    // just the two possible reactions.
    public enum CombatResponse
    {
        Fight,
        Flee,
    }
}
