using System.Collections.Generic;
using UnityEngine;

namespace KingdomsOfBharat.Buildings
{
    // Wall mechanics audit (2026-09-29): AoE2 auto-selects a wall tile's
    // shape (Straight/Corner/Endcap/T/X) from its 4 cardinal neighbors -
    // a bitmask that assumes a strict tile grid. This project's walls are
    // free-angle (Session A's own deliberate choice, not axis-snapped), so
    // there's no fixed "cardinal" frame to bitmask against. Classified
    // instead by the ANGLE BETWEEN neighbor directions, which is
    // rotation-invariant and works at any drag angle: two neighbors
    // roughly opposite each other means the piece continues a straight
    // line; any other angle between two neighbors means a turn. Pure
    // function, no scene dependency - see WallConnectivityTests.cs.
    public static class WallConnectivity
    {
        public static WallFactory.WallPieceKind ClassifyPieceKind(
            Vector3 position, IReadOnlyList<Vector3> neighborPositions, float oppositeToleranceDegrees = 30f)
        {
            int count = neighborPositions.Count;

            if (count <= 1)
            {
                return WallFactory.WallPieceKind.EndPost;
            }

            if (count == 2)
            {
                Vector2 dirA = FlatDirection(position, neighborPositions[0]);
                Vector2 dirB = FlatDirection(position, neighborPositions[1]);
                float angle = Vector2.Angle(dirA, dirB);
                bool roughlyOpposite = Mathf.Abs(angle - 180f) <= oppositeToleranceDegrees;
                return roughlyOpposite ? WallFactory.WallPieceKind.Straight : WallFactory.WallPieceKind.Corner;
            }

            // AoE II reference (2026-09-29, user's explicit call): no
            // dedicated T/X-junction art - a 3-way or 4+-way meeting point
            // just reuses the Corner piece, same as the real game.
            return WallFactory.WallPieceKind.Corner;
        }

        private static Vector2 FlatDirection(Vector3 from, Vector3 to)
        {
            Vector2 direction = new Vector2(to.x - from.x, to.z - from.z);
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        }
    }
}
