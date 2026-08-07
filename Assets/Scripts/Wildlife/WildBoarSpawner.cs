using UnityEngine;
using UnityEngine.AI;
using KingdomsOfBharat.Combat;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Wildlife
{
    // Scatters a few wild boars around the map - dangerous wildlife that
    // wanders and attacks nearby units on sight, but drops a Food carcass
    // when killed. A fixed random seed keeps layout reproducible.
    public class WildBoarSpawner : MonoBehaviour
    {
        [SerializeField] private int boarCount = 4;
        [SerializeField] private float minRadius = 10f;
        [SerializeField] private float maxRadius = 18f;
        [SerializeField] private float health = 25f;
        [SerializeField] private int randomSeed = 54321;

        private void Start()
        {
            Random.InitState(randomSeed);

            for (int i = 0; i < boarCount; i++)
            {
                SpawnBoar(RandomPoint());
            }
        }

        private Vector3 RandomPoint()
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            float radius = Random.Range(minRadius, maxRadius);
            return new Vector3(direction.x * radius, 1f, direction.y * radius);
        }

        private void SpawnBoar(Vector3 position)
        {
            GameObject go = AnimalModelFactory.TrySpawn("WildBoar", ResolveGroundPoint(position));
            if (go == null)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = "WildBoar";
                go.transform.position = position;
                go.transform.localScale = new Vector3(0.8f, 0.6f, 0.8f);

                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = GameplayMaterial.CreateOpaque(new Color(0.35f, 0.25f, 0.15f));
            }

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 1f;
            agent.speed = 3f;

            var attackable = go.AddComponent<Attackable>();
            attackable.Configure(health);
            var boar = go.AddComponent<WildBoar>();

            // Safe to add unconditionally, including on the primitive
            // fallback with no Animator to drive - Configure() no-ops if
            // it can't find one, same guard AnimationDriver uses.
            go.AddComponent<BoarAnimationDriver>().Configure(BoarAnimationSet.Load(), agent, boar);
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
