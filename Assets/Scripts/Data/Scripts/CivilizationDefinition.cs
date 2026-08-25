using System.Collections.Generic;
using UnityEngine;

// AoE2-style civilization: a bundle of unique units/techs plus always-on
// passive bonuses. Passive bonuses are what make civs feel structurally
// different rather than just re-skinned with different unit art.
[CreateAssetMenu(fileName = "NewCivilization", menuName = "BharatRTS/Civilization")]
public class CivilizationDefinition : ScriptableObject
{
    public string civId;
    public string displayName;
    [TextArea] public string flavorText; // historical/cultural basis, dynasty/region

    public List<UnitDefinition> uniqueUnits = new List<UnitDefinition>();
    public List<TechNode> uniqueTechs = new List<TechNode>();

    [Tooltip("Always-on bonuses, AoE2-style (e.g. +work rate, cheaper docks)")]
    public List<StatModifier> passiveBonuses = new List<StatModifier>();

    [Tooltip("Techs auto-granted for free when this civ reaches the given age")]
    public List<TechNode> freeTechsOnAgeUp = new List<TechNode>();

    [Tooltip("Bonus shared with allied players, AoE2 team-bonus style")]
    public StatModifier teamBonus;

    public Sprite emblem;
}
