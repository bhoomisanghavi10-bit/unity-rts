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
    //
    // Unified with Data/Scripts/FormationDefinition.cs's own (previously
    // separate, incompatible) FormationType enum - that file originally
    // declared its own global {Line, Box, Staggered, Flank, Column,
    // Skirmish} under the same name, the same silent-collision trap
    // already hit once with SelectionManager's _currentFormation (fixed
    // via full qualification, see that file's comment). Rather than fix
    // the collision again, the two enums are now genuinely one: this is
    // the sole FormationType in the project, FormationDefinition.type
    // references this directly, and FormationController (the "runtime
    // FormationController - not included here, belongs in your existing
    // unit-control code" that comment asked for) drives its front/back-row
    // composition on top of these same shapes rather than a parallel set.
    // Column/Staggered/Flank/Skirmish are real shapes (see below), not
    // placeholders - SelectionManager's R-hotkey cycle deliberately still
    // only reaches Grid/Line/Box (the 3 original move-order-spread
    // choices); the other 4 are reachable via a FormationDefinition
    // asset + FormationController, not the hotkey, since they're aimed at
    // pre-composed squads rather than an ad-hoc move-order spread.
    public enum FormationType
    {
        Grid,
        Line,
        Box,
        Column,
        Staggered,
        Flank,
        Skirmish,
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
                FormationType.Line => RankOffset(index, total, spacing, moveDirection, 0f),
                FormationType.Box => BoxOffset(index, total, spacing),
                FormationType.Column => ColumnOffset(index, total, spacing, moveDirection),
                FormationType.Staggered => StaggeredOffset(index, total, spacing, moveDirection),
                FormationType.Flank => FlankOffset(index, total, spacing, moveDirection),
                FormationType.Skirmish => SkirmishOffset(index, total, spacing, moveDirection),
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
        //
        // Public (not the private LineOffset it used to be) so
        // FormationController can lay out several ranks at different
        // depths - one call per row, each with its own depthOffset - to
        // build genuine front/back-row composition (a front rank at
        // depthOffset 0, a back rank at depthOffset spacing, etc.)
        // directly on top of this same "one horizontal rank" primitive,
        // rather than duplicating the perpendicular-axis math.
        public static Vector3 RankOffset(int index, int total, float spacing, Vector3 moveDirection, float depthOffset)
        {
            Vector3 forward = moveDirection.sqrMagnitude > 0.0001f ? moveDirection.normalized : Vector3.forward;
            Vector3 right = new Vector3(forward.z, 0f, -forward.x);
            float offset = total <= 1 ? 0f : (index - (total - 1) * 0.5f) * spacing;
            return right * offset - forward * depthOffset;
        }

        // Single file along the direction of travel - the classic
        // narrow-column marching order, one unit deep per row.
        private static Vector3 ColumnOffset(int index, int total, float spacing, Vector3 moveDirection)
        {
            Vector3 forward = moveDirection.sqrMagnitude > 0.0001f ? moveDirection.normalized : Vector3.forward;
            float depth = (index - (total - 1) * 0.5f) * spacing;
            return -forward * depth;
        }

        // Two interleaved ranks, alternating units between a near row and
        // a row one half-spacing further back - avoids every unit sharing
        // one exact firing line the way a plain Line does (real staggered-
        // rank formations exist so a single volley/AoE hit can't rake an
        // entire rank at once).
        private static Vector3 StaggeredOffset(int index, int total, float spacing, Vector3 moveDirection)
        {
            int lane = index % 2;
            int laneIndex = index / 2;
            int laneTotal = Mathf.CeilToInt(total / 2f);
            Vector3 rank = RankOffset(laneIndex, laneTotal, spacing, moveDirection, lane * spacing * 0.5f);
            return rank;
        }

        // A shallow forward-facing wedge/chevron - the two flanks angle
        // forward ahead of the center, presenting an arrowhead rather than
        // a flat rank. Reuses Box's "size follows the group" side length.
        private static Vector3 FlankOffset(int index, int total, float spacing, Vector3 moveDirection)
        {
            Vector3 forward = moveDirection.sqrMagnitude > 0.0001f ? moveDirection.normalized : Vector3.forward;
            Vector3 right = new Vector3(forward.z, 0f, -forward.x);
            float lateral = total <= 1 ? 0f : (index - (total - 1) * 0.5f) * spacing;
            float depth = Mathf.Abs(lateral) * 0.5f;
            return right * lateral + forward * depth;
        }

        // Loose, wide spread rather than a tight rank - deliberately more
        // spacing than requested (1.5x) so a Skirmish-formation group
        // isn't standing shoulder-to-shoulder for area attacks to punish,
        // the actual point of a skirmish line historically.
        private static Vector3 SkirmishOffset(int index, int total, float spacing, Vector3 moveDirection)
        {
            return RankOffset(index, total, spacing * 1.5f, moveDirection, 0f);
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
