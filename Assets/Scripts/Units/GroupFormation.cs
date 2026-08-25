using UnityEngine;

namespace KingdomsOfBharat.Units
{
    // Phase 6 gap-close: which formation type SelectionManager's move
    // orders currently spread a multi-unit selection into. Grid is the
    // original item-41 behavior (an anti-clump grid, not a real formation
    // choice - kept as the default so existing move-order feel is
    // unchanged unless the player actively picks something else). Line and
    // Box are real, player-selectable formation types: Line for a single
    // firing rank facing the destination (good for ranged fire massed
    // behind a front, or just presenting a wide front rather than a
    // clump), Box for a hollow all-around perimeter (good for escorting
    // something vulnerable in the middle, or not exposing a flank).
    public enum FormationType
    {
        Grid,
        Line,
        Box,
    }

    // Spreads a multi-unit move order across real space instead of every
    // unit in the group pathing to the exact same point - without this,
    // a 10-unit army all target one spot and jam up fighting for the same
    // few square meters of NavMesh, relying entirely on NavMeshAgent's own
    // local avoidance to sort itself out.
    public static class GroupFormation
    {
        // Index/total identify this unit's slot within the group (order
        // doesn't matter - purely which formation cell it lands in);
        // spacing is the gap between adjacent units in world units.
        // moveDirection only matters for Line (which way the rank faces) -
        // Grid/Box are deliberately axis-aligned regardless of travel
        // direction, same "simple over clever" call the original Grid-only
        // version already made.
        public static Vector3 GetOffset(FormationType type, int index, int total, float spacing, Vector3 moveDirection)
        {
            if (total <= 1)
            {
                return Vector3.zero;
            }

            return type switch
            {
                FormationType.Line => LineOffset(index, total, spacing, moveDirection),
                FormationType.Box => BoxOffset(index, total, spacing),
                _ => GridOffset(index, total, spacing),
            };
        }

        // Grid-only overload kept for any caller that doesn't have/need a
        // formation choice or travel direction (none currently in this
        // project, but this is the same signature GetOffset always had -
        // no reason to force every existing call site through the new
        // 5-arg version just to keep using the default behavior).
        public static Vector3 GetOffset(int index, int total, float spacing)
        {
            return total <= 1 ? Vector3.zero : GridOffset(index, total, spacing);
        }

        private static Vector3 GridOffset(int index, int total, float spacing)
        {
            int columns = Mathf.CeilToInt(Mathf.Sqrt(total));
            int rows = Mathf.CeilToInt((float)total / columns);
            int row = index / columns;
            int column = index % columns;

            float x = (column - (columns - 1) * 0.5f) * spacing;
            float z = (row - (rows - 1) * 0.5f) * spacing;
            return new Vector3(x, 0f, z);
        }

        // A single rank, perpendicular to the direction of travel - the
        // whole group arrives shoulder-to-shoulder facing the destination
        // rather than staggered in depth. Falls back to a fixed axis if
        // moveDirection is degenerate (selection already standing on the
        // destination) rather than dividing by ~zero.
        private static Vector3 LineOffset(int index, int total, float spacing, Vector3 moveDirection)
        {
            Vector3 forward = moveDirection.sqrMagnitude > 0.0001f ? moveDirection.normalized : Vector3.forward;
            Vector3 right = new Vector3(forward.z, 0f, -forward.x);
            float offset = (index - (total - 1) * 0.5f) * spacing;
            return right * offset;
        }

        // A hollow square perimeter, walked clockwise from the front-left
        // corner at `spacing` intervals - not a filled block, so nothing
        // stands exposed on a flank the way a Line's single rank does, at
        // the cost of an empty (escortable) center. Side length is derived
        // from total*spacing/4 so the perimeter's total walked distance
        // matches the group's actual footprint regardless of headcount,
        // the same "size follows the group" idea Grid's row/column count
        // already uses.
        private static Vector3 BoxOffset(int index, int total, float spacing)
        {
            float perimeter = total * spacing;
            float side = perimeter / 4f;
            float half = side * 0.5f;
            float walked = (index * spacing) % perimeter;

            if (walked < side)
            {
                return new Vector3(-half + walked, 0f, half);
            }

            walked -= side;
            if (walked < side)
            {
                return new Vector3(half, 0f, half - walked);
            }

            walked -= side;
            if (walked < side)
            {
                return new Vector3(half - walked, 0f, -half);
            }

            walked -= side;
            return new Vector3(-half, 0f, -half + walked);
        }
    }
}
