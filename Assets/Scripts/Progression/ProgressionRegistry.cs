namespace KingdomsOfBharat.Progression
{
    // Repository-audit finding F08 ("Static progression can leak across
    // matches"): every per-faction progression registry in this folder is a
    // static Dictionary that persists for the whole Editor/Player process,
    // not per-match. Most already had an internal ResetForTests() used only
    // by EditMode tests; CivilizationSetup.BeginMatchCore never called any
    // of them, so a faction's researched tiers/unique tech/hero-trained
    // flag could survive into the next match started in the same process
    // (Play Again, or a second BeginMatch in the same Editor session).
    //
    // This is the single production entry point for that reset: one call
    // from CivilizationSetup.BeginMatchCore instead of scattering every
    // registry's own Reset() through a MonoBehaviour method that's awkward
    // to unit-test directly. Deliberately scoped to progression state only
    // - command queues, NetworkId and RNG streams are a separate,
    // already-tracked concern (see Multiplayer.NetworkId.Reset, called
    // independently by BeginMatchCore) and out of scope here.
    internal static class ProgressionRegistry
    {
        internal static void ResetAllForNewMatch()
        {
            ScoreProgress.Reset();
            UpgradeProgress.Reset();
            UniqueTechProgress.Reset();
            EconomyTechProgress.Reset();
            HeroProgress.Reset();
            UniqueUnitEliteProgress.Reset();

            InfantryLineProgress.Reset();
            SpearmanLineProgress.Reset();
            ArcherLineProgress.Reset();
            CavalryLineProgress.Reset();
            SiegeLineProgress.Reset();
            NavalLineProgress.Reset();
            ScoutLineProgress.Reset();
            SkirmisherLineProgress.Reset();
            CavalryArcherLineProgress.Reset();
            CamelRiderLineProgress.Reset();
            ScorpionLineProgress.Reset();
            BatteringRamLineProgress.Reset();
            ElephantLineProgress.Reset();
            FireShipLineProgress.Reset();
            FarmTechProgress.Reset();
        }
    }
}
