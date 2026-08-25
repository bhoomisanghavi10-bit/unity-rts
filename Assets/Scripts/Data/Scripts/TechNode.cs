using System.Collections.Generic;
using UnityEngine;

// AoE2-style tech tree node: age-gated, prerequisite-chained, applies StatModifiers
// when researched. One asset per technology.
[CreateAssetMenu(fileName = "NewTech", menuName = "BharatRTS/Tech Node")]
public class TechNode : ScriptableObject
{
    public string techId;
    public string displayName;
    [TextArea] public string description;

    [Tooltip("1 = Age I ... 4 = Age IV, extend if BHARAT RTS uses more ages")]
    public int age = 1;

    public List<TechNode> prerequisites = new List<TechNode>();
    public ResourceCost cost;
    public float researchTimeSeconds;

    [Tooltip("What this tech does when researched")]
    public List<StatModifier> effects = new List<StatModifier>();

    public Sprite icon;
}

[System.Serializable]
public struct ResourceCost
{
    public int food;
    public int wood;
    public int gold;
    public int stone;
}

[System.Serializable]
public struct StatModifier
{
    [Tooltip("Which unit category this applies to, or \"All\"")]
    public UnitCategory targetCategory;
    public bool applyToAllCategories;
    public StatType stat;
    public ModifierOp operation;
    public float value;
}

// ResourceRate (gather speed, e.g. Double-Bit Axe/Horticulture) added
// alongside the original ResourceCost (spend-side changes, e.g. Market
// spread/Wheelbarrow carry cap) - the CSV data schema (tech_tree_
// template.csv's EffectType column) distinguishes the two, so the enum
// needs to as well.
public enum StatType { HP, Attack, MeleeArmor, PierceArmor, MoveSpeed, AttackSpeed, Range, TrainTime, ResourceCost, ResourceRate }
public enum ModifierOp { Add, Multiply }
