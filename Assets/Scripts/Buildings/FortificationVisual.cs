using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Progression;

namespace KingdomsOfBharat.Buildings
{
    // Shared age-branching visual builder for Wall (every WallPieceKind
    // variant) and Gate (2026-09-28, alongside the "classic age wall and
    // gate kit" delivery). Ancient and Imperial keep using
    // BuildingModelFactory's real 3D mesh pipeline (via its own Refresh,
    // given an already-created root) exactly as before this delivery -
    // Refresh's own age-suffixed lookup chain already degrades correctly
    // for ages with no dedicated art (Imperial resolves to the per-civ
    // model, an Ancient-only junction piece resolves to its literal
    // resourceName with no suffix). Classical and Durg use the new flat
    // isometric sprite-billboard kit (WallSpriteVisual) instead - Durg
    // reuses Classical's exact same source images with a plain material
    // darken tint, not a second baked-darker delivery (see
    // WallSpriteVisual.MaterialFor).
    internal static class FortificationVisual
    {
        public static void Build(GameObject root, string meshResourceName, string classicalClosedPath, string classicalOpenPath, AgeId age, CivilizationId civ, Color civColor, FactionId? faction, Vector3 size)
        {
            DestroyExistingVisualAndColliders(root);

            if (age == AgeId.Classical || age == AgeId.Durg)
            {
                bool darken = age == AgeId.Durg;
                WallSpriteVisual.Build(root, classicalClosedPath, classicalOpenPath, size.x, size.y, darken);
                AddBoxCollider(root, size);
                return;
            }

            BuildingModelFactory.Refresh(root, meshResourceName, civ, age, size, civColor, faction);
        }

        private static void DestroyExistingVisualAndColliders(GameObject root)
        {
            Transform existing = root.transform.Find("Visual");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            foreach (BoxCollider collider in root.GetComponents<BoxCollider>())
            {
                Object.DestroyImmediate(collider);
            }
        }

        // BuildingModelFactory's own mesh path derives a tight collider from
        // the real rendered bounds instead - this is the sprite path's
        // equivalent, sized to the same gameplay Size box every other part
        // of this piece (NavMeshObstacle/BuildingFootprint) already uses,
        // so clicking/raycasting against a flat card hits a real volume.
        private static void AddBoxCollider(GameObject root, Vector3 size)
        {
            var collider = root.AddComponent<BoxCollider>();
            collider.size = size;
            collider.center = Vector3.zero;
        }
    }
}
