using UnityEngine;
using UnityEngine.AI;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Wildlife
{
    // Places a small starting herd of livestock (cows) near the Player's
    // Town Center - a milk-based Food source that's tame and safe (no
    // wildlife danger, unlike wild boars, and cannot be killed) but must
    // be actively worked to produce anything.
    public class LivestockSpawner : MonoBehaviour
    {
        [SerializeField] private Vector3 herdCenter = new Vector3(-6f, 1f, 8f);
        [SerializeField] private int cowCount = 3;
        [SerializeField] private float spacing = 2f;

        private void Start()
        {
            for (int i = 0; i < cowCount; i++)
            {
                float x = i * spacing - (cowCount - 1) * spacing * 0.5f;
                SpawnCow(herdCenter + new Vector3(x, 0f, 0f));
            }
        }

        private void SpawnCow(Vector3 position)
        {
            GameObject go = AnimalModelFactory.TrySpawn("Livestock", ResolveGroundPoint(position));
            if (go == null)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = "Cow";
                go.transform.position = position;
                go.transform.localScale = new Vector3(0.9f, 0.7f, 0.9f);

                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = GameplayMaterial.CreateOpaque(new Color(0.95f, 0.95f, 0.9f));
            }

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 1f;
            agent.speed = 2f;

            go.AddComponent<Livestock>();
        }

        // Only used for the real-model path (AnimalModelFactory): see
        // ResourceNodeSpawner's identical helper for why. The primitive
        // fallback keeps its existing flat-Y position unchanged.
        private static Vector3 ResolveGroundPoint(Vector3 xzPoint)
        {
            return GroundReference.TryGetHeight(xzPoint, out float height)
                ? new Vector3(xzPoint.x, height, xzPoint.z)
                : xzPoint;
        }
    }
}
