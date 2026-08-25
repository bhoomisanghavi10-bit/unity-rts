using UnityEngine;

// Per-age incremental cost/bonus data - Phase 2 migration addition, no
// counterpart in the original Phase 1 CSV design (the peer's content
// design covered civ bonuses/tech tree/units/counter matrix, not age
// advancement). Mirrors Progression/AgeProfile.cs's existing hardcoded
// struct field-for-field so the migration is a pure data-source swap,
// not a redesign.
[CreateAssetMenu(fileName = "NewAgeProfile", menuName = "BharatRTS/Age Profile")]
public class AgeProfileDefinition : ScriptableObject
{
    public string ageId;
    public string displayName;
    public float woodCost;
    public float stoneCost;
    public float researchTime;
    public float gatherRateMultiplier;
    public float maxHealthMultiplier;
    public float trainTimeMultiplier;
}
