using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Buildings
{
    // Hand-assembled multi-part building silhouettes, built entirely out of
    // primitives (+ a couple of hand-generated pyramid meshes for
    // roofs/spires, since CreatePrimitive has no cone/pyramid shape) -
    // BuildingModelFactory's fallback whenever no real imported model
    // exists yet under Resources/Buildings/. Deliberately more than a
    // single flat cube: a stepped, spired silhouette for TownCenter
    // (temple/gopuram-like), a keep with corner towers for Barracks
    // (fort-like), a farmhouse-plus-granary for Farm, a simple hut for
    // House - readable at RTS camera distance without needing any asset
    // import, and ready to be re-skinned later with generated textures
    // (Canva/Claude design) via each part's own material.
    //
    // Every child part is created with its own GameplayMaterial.CreateOpaque
    // (never left on Unity's default material - the same URP-default-magenta
    // trap that bit the fog quad and placement ghost earlier this project),
    // colored directly with the civilization's color - unlike
    // BuildingModelFactory.TintMaterials' partial Lerp (meant to let a real
    // imported pack's own diffuse texture/color show through), a fully
    // procedural building has no other visual identity to preserve, so it
    // uses the civ color outright, matching the flat-cube look this
    // replaces.
    //
    // Every primitive's auto-added Collider is destroyed immediately: an
    // extra collider sitting on a CHILD GameObject (instead of the
    // building's root, where Barracks/ConstructionSite/FactionMember
    // actually live) would silently break HoverTooltip/SelectionManager -
    // both resolve components straight off hit.collider.gameObject with no
    // GetComponentInParent fallback. BuildingModelFactory adds the one
    // real interactive collider afterward, sized to the whole assembly's
    // bounds.
    public static class ProceduralBuildingFactory
    {
        public static GameObject Build(string resourceName, Color civColor, Transform parent)
        {
            GameObject container = new GameObject("Procedural_" + resourceName);
            container.transform.SetParent(parent, false);

            switch (resourceName)
            {
                case "TownCenter":
                    BuildTemple(container.transform, civColor);
                    break;
                case "Barracks":
                    BuildFort(container.transform, civColor);
                    break;
                case "Farm":
                    BuildFarmstead(container.transform, civColor);
                    break;
                case "House":
                    BuildHut(container.transform, civColor);
                    break;
                default:
                    BuildHut(container.transform, civColor);
                    break;
            }

            return container;
        }

        // Stepped-pyramid gopuram silhouette: three shrinking tiers plus a
        // spire, evoking a Chola-style temple tower far more than a single
        // box does, without needing any imported geometry.
        private static void BuildTemple(Transform parent, Color color)
        {
            float y = 0f;
            y = AddTierCube(parent, new Vector3(3f, 1f, 3f), y, color);
            y = AddTierCube(parent, new Vector3(2.2f, 0.8f, 2.2f), y, color);
            y = AddTierCube(parent, new Vector3(1.5f, 0.6f, 1.5f), y, color);
            AddPyramid(parent, "Spire", new Vector3(0f, y, 0f), 1f, 1f, 1.3f, color);
        }

        // A main keep with a squat corner tower at each corner, each capped
        // with its own small pyramid roof - the fort-like read Barracks is
        // meant to have.
        private static void BuildFort(Transform parent, Color color)
        {
            AddTierCube(parent, new Vector3(3f, 1.6f, 3f), 0f, color);

            const float towerHeight = 2.2f;
            const float towerRadius = 0.4f;
            Vector3[] corners =
            {
                new Vector3(1.3f, 0f, 1.3f),
                new Vector3(-1.3f, 0f, 1.3f),
                new Vector3(1.3f, 0f, -1.3f),
                new Vector3(-1.3f, 0f, -1.3f),
            };

            foreach (Vector3 corner in corners)
            {
                AddCylinder(parent, corner, towerRadius, towerHeight, color);
                AddPyramid(parent, "TowerCap", corner + Vector3.up * towerHeight, towerRadius * 2.2f, towerRadius * 2.2f, 0.6f, color);
            }
        }

        // A low farmhouse body with its own pitched roof, plus a small
        // grain-silo (cylinder + conical roof) off to one side.
        private static void BuildFarmstead(Transform parent, Color color)
        {
            AddTierCube(parent, new Vector3(2.4f, 0.7f, 1.6f), 0f, color);
            AddPyramid(parent, "FarmRoof", new Vector3(0f, 0.7f, 0f), 2.6f, 1.8f, 0.5f, color);

            Vector3 siloXz = new Vector3(1.1f, 0f, 0.6f);
            AddCylinder(parent, siloXz, 0.35f, 1f, color);
            AddPyramid(parent, "SiloRoof", siloXz + Vector3.up * 1f, 0.8f, 0.8f, 0.5f, color);
        }

        // A simple single-room hut - base plus pitched roof.
        private static void BuildHut(Transform parent, Color color)
        {
            AddTierCube(parent, new Vector3(1.6f, 1f, 1.6f), 0f, color);
            AddPyramid(parent, "Roof", new Vector3(0f, 1f, 0f), 2f, 2f, 0.9f, color);
        }

        // Places a cube so its BOTTOM sits at baseY (matching the
        // "stack upward from the ground" convention every Build* method
        // above assumes), returns the new top Y for the next tier/part to
        // stack on.
        private static float AddTierCube(Transform parent, Vector3 size, float baseY, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, baseY + size.y * 0.5f, 0f);
            go.transform.localScale = size;
            Object.Destroy(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = GameplayMaterial.CreateOpaque(color);

            return baseY + size.y;
        }

        private static void AddCylinder(Transform parent, Vector3 localBaseXz, float radius, float height, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(localBaseXz.x, height * 0.5f, localBaseXz.z);
            // Unity's built-in cylinder mesh is 2 units tall and 1 unit
            // wide at scale (1,1,1) - halve the desired height/diameter
            // back into that mesh's own units.
            go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            Object.Destroy(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = GameplayMaterial.CreateOpaque(color);
        }

        private static void AddPyramid(Transform parent, string name, Vector3 localBasePosition, float baseWidth, float baseDepth, float height, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localBasePosition;

            go.AddComponent<MeshFilter>().mesh = BuildPyramidMesh(baseWidth, baseDepth, height);
            // Double-sided: this hand-built mesh's triangle winding can't
            // be verified without an Editor to look at it in, and a wrong
            // winding silently back-face-culls a whole side, leaving a
            // gap in the roof. Disabling culling makes that impossible
            // regardless of winding, at negligible cost for a few small
            // roof/spire meshes.
            go.AddComponent<MeshRenderer>().sharedMaterial = CreateDoubleSidedMaterial(color);
        }

        private static Material CreateDoubleSidedMaterial(Color color)
        {
            Material material = GameplayMaterial.CreateOpaque(color);
            if (material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            }

            return material;
        }

        // No built-in PrimitiveType covers a cone/pyramid, so this hand-
        // builds the smallest mesh that reads as one: four base corners
        // plus an apex, four sloped side faces (the underside is never
        // visible from an RTS camera angle sitting flush on a roof/tower,
        // so it's left open to save geometry).
        private static Mesh BuildPyramidMesh(float baseWidth, float baseDepth, float height)
        {
            float halfWidth = baseWidth * 0.5f;
            float halfDepth = baseDepth * 0.5f;

            Vector3[] vertices =
            {
                new Vector3(-halfWidth, 0f, -halfDepth),
                new Vector3(halfWidth, 0f, -halfDepth),
                new Vector3(halfWidth, 0f, halfDepth),
                new Vector3(-halfWidth, 0f, halfDepth),
                new Vector3(0f, height, 0f),
            };

            int[] triangles =
            {
                0, 1, 4,
                1, 2, 4,
                2, 3, 4,
                3, 0, 4,
            };

            var mesh = new Mesh { name = "ProceduralPyramid" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
