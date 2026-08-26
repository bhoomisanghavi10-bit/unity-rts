using System.Collections.Generic;
using UnityEngine;
using KingdomsOfBharat.Units;

// Squad/group movement + facing rules. Paired with a runtime
// FormationController - Units/FormationController.cs, driving
// Units/GroupFormation.cs's shape functions with this asset's per-category
// row assignment.
//
// `type` was originally its own global FormationType enum here, separate
// from and incompatible with Units/GroupFormation.cs's own FormationType
// (same name, different values) - the same silent-collision trap already
// hit once with SelectionManager's _currentFormation. The two are now
// unified into GroupFormation's single FormationType (see that file's
// comment) rather than fixed via qualification again.
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
