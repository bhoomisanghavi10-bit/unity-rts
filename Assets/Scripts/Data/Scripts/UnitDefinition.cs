using UnityEngine;

// One asset per unit type. Combat stats here feed directly into CounterMatrix
// lookups at damage-resolution time (see CounterMatrix.cs).
[CreateAssetMenu(fileName = "NewUnit", menuName = "BharatRTS/Unit Definition")]
public class UnitDefinition : ScriptableObject
{
    public string unitId;
    public string displayName;
    public UnitCategory category;
    public int ageRequirement = 1;
    public ResourceCost cost;
    public float trainTimeSeconds;

    [Header("Combat Stats")]
    public float maxHP;
    public float attackDamage;
    public DamageType attackType;
    public float meleeArmor;
    public float pierceArmor;
    public float attackRange;
    public float attackSpeed; // attacks per second
    public float moveSpeed;

    [Header("Design Metadata")]
    public bool isUniqueUnit;
    [Tooltip("Empty if this unit is available to all civs")]
    public string civOwnerId;
    [TextArea]
    [Tooltip("Free-text design intent, e.g. \"Strong vs Cavalry, weak vs Spearmen\" — cross-check against CounterMatrix entries")]
    public string counterNotes;
    [TextArea]
    public string historicalBasis;

    public GameObject prefab;
}

public enum UnitCategory { Infantry, Spearman, Cavalry, Archer, Siege, Naval, Support, Hero }
public enum DamageType { Melee, Pierce, Siege, Fire, Trample }
