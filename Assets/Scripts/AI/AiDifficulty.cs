namespace KingdomsOfBharat.AI
{
    // How aggressively/quickly the AI plays - scales AiController's
    // existing timer/threshold fields (see AiController.ApplyDifficulty)
    // rather than changing its decision logic. No UI to pick this yet
    // (same "Inspector for now" state CivilizationSetup was in before
    // CivPicker existed) - set on the AiController GameObject directly.
    public enum AiDifficulty
    {
        Easy,
        Normal,
        Hard,
    }

    // Which opening priorities the AI follows - real variety, not just a
    // label: EconomyFirst actually delays Barracks construction (see
    // AiController.TryBuildBarracks) until a worker-count threshold is
    // met, instead of building it the instant resources allow like every
    // style did before this existed.
    public enum BuildOrderStyle
    {
        RushMilitary,
        Balanced,
        EconomyFirst,
    }
}
