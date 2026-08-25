using System.Collections.Generic;
using UnityEngine;

// Squad/group movement + facing rules. Pair with a runtime FormationController
// (not included here — that's game-logic, not data, so it belongs in your
// existing unit-control code, not a ScriptableObject).
[CreateAssetMenu(fileName = "NewFormation", menuName = "BharatRTS/Formation")]
public class FormationDefinition : ScriptableObject
{
    public string formationId;
    public string displayName;
    public FormationType type;
    public float unitSpacing = 1.5f;

    [Tooltip("High-armor/melee units placed at the front row facing the enemy")]
    public List<UnitCategory> preferredFrontRow = new List<UnitCategory> { UnitCategory.Infantry, UnitCategory.Spearman };

    [Tooltip("Ranged/fragile units placed at the back, protected by the front row")]
    public List<UnitCategory> preferredBackRow = new List<UnitCategory> { UnitCategory.Archer, UnitCategory.Siege };

    [Tooltip("Max units per row before wrapping to a new row")]
    public int unitsPerRow = 8;
}

public enum FormationType { Line, Box, Staggered, Flank, Column, Skirmish }
