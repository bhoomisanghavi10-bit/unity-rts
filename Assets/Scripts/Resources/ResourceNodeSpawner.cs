using UnityEngine;

namespace KingdomsOfBharat.ResourceGathering
{
    // Milestone-4 placeholder: scatters tree (Wood) and farmland (Food)
    // nodes around the map so gathering is testable. A fixed random seed
    // keeps layout reproducible between runs.
    public class ResourceNodeSpawner : MonoBehaviour
    {
        [SerializeField] private int treeCount = 6;
        [SerializeField] private int farmCount = 3;
        [SerializeField] private float minRadius = 6f;
        [SerializeField] private float maxRadius = 16f;
        [SerializeField] private float startingAmount = 40f;
        [SerializeField] private int randomSeed = 12345;

        private void Start()
        {
            Random.InitState(randomSeed);

            for (int i = 0; i < treeCount; i++)
            {
                SpawnTree(RandomPointInRing());
            }

            for (int i = 0; i < farmCount; i++)
            {
                SpawnFarmland(RandomPointInRing());
            }
        }

        private Vector3 RandomPointInRing()
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            float radius = Random.Range(minRadius, maxRadius);
            return new Vector3(direction.x * radius, 0f, direction.y * radius);
        }

        private void SpawnTree(Vector3 position)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Tree";
            go.transform.position = position + Vector3.up;
            go.transform.localScale = new Vector3(0.6f, 1f, 0.6f);
            Colorize(go, new Color(0.25f, 0.45f, 0.2f));

            var node = go.AddComponent<ResourceNode>();
            node.Configure(ResourceType.Wood, startingAmount);
        }

        private void SpawnFarmland(Vector3 position)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Farmland";
            go.transform.position = position + Vector3.up * 0.1f;
            go.transform.localScale = new Vector3(2.5f, 0.2f, 2.5f);
            Colorize(go, new Color(0.75f, 0.65f, 0.25f));

            var node = go.AddComponent<ResourceNode>();
            node.Configure(ResourceType.Food, startingAmount);
        }

        private static void Colorize(GameObject go, Color color)
        {
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(FindShader()) { color = color };
        }

        private static Shader FindShader()
        {
            return Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Diffuse");
        }
    }
}
