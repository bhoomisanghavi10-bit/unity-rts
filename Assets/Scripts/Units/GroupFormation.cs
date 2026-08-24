using UnityEngine;

namespace KingdomsOfBharat.Units
{
    // Spreads a multi-unit move order across a rough grid instead of every
    // unit in the group pathing to the exact same point - without this,
    // a 10-unit army all target one spot and jam up fighting for the same
    // few square meters of NavMesh, relying entirely on NavMeshAgent's own
    // local avoidance to sort itself out. Deliberately simple (an
    // axis-aligned grid, not rotated to face the destination or shaped
    // like a wedge/line) - the goal is "don't stack on one point," not a
    // full formation-facing/spacing system.
    public static class GroupFormation
    {
        // Index/total identify this unit's slot within the group (order
        // doesn't matter - purely which grid cell it lands in); spacing
        // is the gap between adjacent units in world units.
        public static Vector3 GetOffset(int index, int total, float spacing)
        {
            if (total <= 1)
            {
                return Vector3.zero;
            }

            int columns = Mathf.CeilToInt(Mathf.Sqrt(total));
            int rows = Mathf.CeilToInt((float)total / columns);
            int row = index / columns;
            int column = index % columns;

            float x = (column - (columns - 1) * 0.5f) * spacing;
            float z = (row - (rows - 1) * 0.5f) * spacing;
            return new Vector3(x, 0f, z);
        }
    }
}
