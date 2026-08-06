using UnityEngine;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Wildlife
{
    // A hunted boar's carcass - a Food ResourceNode, same mechanic as any
    // other resource node (farmland, trees), just spawned dynamically at
    // the kill location instead of scattered at map start.
    public static class CarcassFactory
    {
        public static void Spawn(Vector3 position, float foodAmount)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Carcass";
            go.transform.position = position + Vector3.up * 0.2f;
            go.transform.localScale = new Vector3(1.2f, 0.4f, 1.2f);

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(FindShader()) { color = new Color(0.4f, 0.2f, 0.15f) };

            var node = go.AddComponent<ResourceNode>();
            node.Configure(ResourceType.Food, foodAmount);
        }

        private static Shader FindShader()
        {
            return Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Diffuse");
        }
    }
}
