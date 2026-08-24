using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Units
{
    // Item 49: real-model-with-procedural-fallback for boats, mirroring
    // BuildingModelFactory/HumanModelFactory's shape - looks for an
    // imported model under Resources/boats/<name>/ first, falls back to a
    // simple hull-plus-mast primitive silhouette (distinct small/large
    // shape for Fishing Boat vs. War Galley) so both ship types already
    // read differently at a glance before any real model exists.
    public static class BoatModelFactory
    {
        public static GameObject Spawn(string resourceName, Vector3 position, Color civColor, bool isWarGalley)
        {
            GameObject prefab = Resources.Load<GameObject>($"boats/{resourceName}")
                ?? Resources.Load<GameObject>($"boats/{resourceName}/{resourceName}/scene");

            GameObject root = new GameObject(resourceName);
            root.transform.position = position;

            if (prefab != null)
            {
                GameObject model = Object.Instantiate(prefab, root.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                TintMaterials(model, civColor);
            }
            else
            {
                BuildProceduralHull(root.transform, civColor, isWarGalley);
            }

            return root;
        }

        private static void BuildProceduralHull(Transform parent, Color color, bool isWarGalley)
        {
            Vector3 hullSize = isWarGalley ? new Vector3(1.2f, 0.5f, 3.2f) : new Vector3(0.8f, 0.35f, 1.6f);
            AddPart(parent, PrimitiveType.Cube, "Hull", hullSize, Vector3.zero, color);

            // A single mast reads as "boat" at RTS camera distance without
            // needing a real hull silhouette yet - taller/thicker on the
            // War Galley so the two ship types are distinguishable even
            // as placeholders.
            float mastHeight = isWarGalley ? 1.6f : 1f;
            AddPart(parent, PrimitiveType.Cylinder, "Mast", new Vector3(0.12f, mastHeight * 0.5f, 0.12f),
                new Vector3(0f, hullSize.y * 0.5f + mastHeight * 0.5f, 0f), new Color(0.4f, 0.3f, 0.2f));
        }

        private static void AddPart(Transform parent, PrimitiveType type, string name, Vector3 size, Vector3 localPosition, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            Object.Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = size;
            part.GetComponent<MeshRenderer>().sharedMaterial = GameplayMaterial.CreateOpaque(color);
        }

        private static void TintMaterials(GameObject go, Color civColor)
        {
            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.materials)
                {
                    material.color = Color.Lerp(material.color, civColor, 0.35f);
                }
            }
        }
    }
}
