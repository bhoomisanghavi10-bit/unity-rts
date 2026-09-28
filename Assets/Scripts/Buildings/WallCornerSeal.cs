using UnityEngine;

namespace KingdomsOfBharat.Buildings
{
    // Wall mechanics audit (2026-09-29): AoE2's diagonal-block rule ("if
    // two wall tiles touch only diagonally, the gap between them is
    // impassable") assumes a strict 1x1 tile grid, which this project
    // doesn't have (continuous NavMesh, free-angle wall placement - see
    // WallFactory's own Session A comment). Reproduced geometrically
    // instead: two AABB footprints (rotation ignored, same simplification
    // BuildingFootprint.IsClear already uses everywhere) are "diagonal
    // neighbors" when neither their X-ranges nor their Z-ranges overlap
    // (a genuine corner-to-corner offset, not an orthogonal adjacency) and
    // both gaps are small enough that a unit could squeeze through. Pure
    // function, no scene dependency - see WallCornerSealTests.cs.
    public static class WallCornerSeal
    {
        public static bool TryComputeSeal(
            Vector3 centerA, Vector2 sizeA, Vector3 centerB, Vector2 sizeB,
            float maxGap, out Vector3 sealCenter, out Vector2 sealSize)
        {
            sealCenter = Vector3.zero;
            sealSize = Vector2.zero;

            float aMinX = centerA.x - sizeA.x * 0.5f;
            float aMaxX = centerA.x + sizeA.x * 0.5f;
            float aMinZ = centerA.z - sizeA.y * 0.5f;
            float aMaxZ = centerA.z + sizeA.y * 0.5f;

            float bMinX = centerB.x - sizeB.x * 0.5f;
            float bMaxX = centerB.x + sizeB.x * 0.5f;
            float bMinZ = centerB.z - sizeB.y * 0.5f;
            float bMaxZ = centerB.z + sizeB.y * 0.5f;

            // Positive gap along an axis = the two ranges don't overlap on
            // that axis at all. Zero or negative = they overlap/touch
            // along that axis - an orthogonal adjacency (or an outright
            // overlap), never the diagonal-corner case this seals.
            float gapX = Mathf.Max(aMinX, bMinX) > Mathf.Min(aMaxX, bMaxX)
                ? Mathf.Max(aMinX, bMinX) - Mathf.Min(aMaxX, bMaxX)
                : 0f;
            float gapZ = Mathf.Max(aMinZ, bMinZ) > Mathf.Min(aMaxZ, bMaxZ)
                ? Mathf.Max(aMinZ, bMinZ) - Mathf.Min(aMaxZ, bMaxZ)
                : 0f;

            if (gapX <= 0f || gapZ <= 0f || gapX > maxGap || gapZ > maxGap)
            {
                return false;
            }

            // The seal exactly fills the open rectangle between the two
            // footprints' closest corners.
            float sealMinX = Mathf.Min(aMaxX, bMaxX);
            float sealMaxX = Mathf.Max(aMinX, bMinX);
            float sealMinZ = Mathf.Min(aMaxZ, bMaxZ);
            float sealMaxZ = Mathf.Max(aMinZ, bMinZ);

            sealCenter = new Vector3((sealMinX + sealMaxX) * 0.5f, (centerA.y + centerB.y) * 0.5f, (sealMinZ + sealMaxZ) * 0.5f);
            sealSize = new Vector2(gapX, gapZ);
            return true;
        }
    }
}
