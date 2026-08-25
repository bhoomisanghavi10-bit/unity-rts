using System;

namespace KingdomsOfBharat.Match
{
    // Item 50: one win/loss condition for a scripted mission. Deliberately
    // plain delegates rather than an enum-plus-data-fields struct - every
    // scenario in this project is authored in code (see ScenarioRegistry,
    // same "code-defined dictionary" convention MapRegistry/
    // CivilizationProfile already use, not a ScriptableObject/Inspector
    // asset), so a closure over live match state is both simpler and more
    // flexible than a generic serializable condition type nothing actually
    // needs to expose to the Inspector.
    public class MissionObjective
    {
        // Shown in the HUD/objective panel.
        public string Description;

        // Re-evaluated every check; true once this objective is satisfied.
        public Func<bool> IsComplete;

        // Optional - null means this objective can never fail the mission
        // on its own (only "not yet complete"). Set for objectives where
        // failure is meaningful independent of the others, e.g. "the
        // target you were escorting died."
        public Func<bool> IsFailed;

        // Item 50 gap-close: optional one-line narrative beat shown as a
        // toast (see MissionToast) the moment this objective's IsComplete()
        // first flips true - null means this objective completes silently
        // (no toast), same as most triggers in ScenarioRegistry not being
        // narrative moments either.
        public string CompleteText;

        public MissionObjective(string description, Func<bool> isComplete, Func<bool> isFailed = null, string completeText = null)
        {
            Description = description;
            IsComplete = isComplete;
            IsFailed = isFailed;
            CompleteText = completeText;
        }
    }
}
