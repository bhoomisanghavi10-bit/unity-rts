namespace KingdomsOfBharat.Core
{
    // Item 6 (Scenario Editor, heavy path session 1): a MapRegistry.Current-
    // shaped static holder for "which custom scenario (if any) is the
    // current match being started from." Set by
    // CivilizationSetup.BeginCustomScenarioMatch BEFORE gated match content
    // activates, so TownCenterSpawner/AiController can check it in their
    // own Awake()/Start() and skip their own default base spawn when this
    // scenario already supplies their faction's placements. Cleared on
    // teardown the same way CivilizationSetup.HasMatchStarted is, so a
    // normal (non-custom) match started afterward isn't affected by a
    // stale reference from a previous custom match.
    public static class CustomScenarioContext
    {
        public static CustomScenarioData Current { get; private set; }

        public static void Begin(CustomScenarioData data)
        {
            Current = data;
        }

        public static void End()
        {
            Current = null;
        }

        // True when this scenario placed at least one building OR unit for
        // the given faction - TownCenterSpawner/UnitSpawner/AiController
        // all use this to decide whether to skip their own hardcoded
        // default spawn (a scenario might place only units for a faction
        // with no starting building, or vice versa - both count).
        public static bool HasPlacementsFor(FactionId faction)
        {
            if (Current == null)
            {
                return false;
            }

            foreach (BuildingSaveData building in Current.buildings)
            {
                if ((FactionId)building.faction == faction)
                {
                    return true;
                }
            }

            foreach (UnitSaveData unit in Current.units)
            {
                if ((FactionId)unit.faction == faction)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
