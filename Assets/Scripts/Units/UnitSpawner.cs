using UnityEngine;
using UnityEngine.AI;
using KingdomsOfBharat.Selection;
using KingdomsOfBharat.ResourceGathering;

namespace KingdomsOfBharat.Units
{
    // Milestone-3/4 placeholder: drops a handful of capsule "worker" units on
    // the map so selection, movement, and gathering are testable before
    // buildings train units for real (milestone 6).
    public class UnitSpawner : MonoBehaviour
    {
        [SerializeField] private int unitCount = 4;
        [SerializeField] private float spacing = 2f;
        [SerializeField] private Color unitColor = new Color(0.8f, 0.7f, 0.2f);

        private void Start()
        {
            for (int i = 0; i < unitCount; i++)
            {
                float x = i * spacing - (unitCount - 1) * spacing * 0.5f;
                SpawnUnit(new Vector3(x, 1f, 0f));
            }
        }

        private void SpawnUnit(Vector3 position)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Worker";
            go.transform.position = position;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(FindShader()) { color = unitColor };

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.speed = 3.5f;

            go.AddComponent<Unit>();
            go.AddComponent<UnitMover>();
            go.AddComponent<SelectionIndicator>();
            go.AddComponent<Gatherer>();
        }

        private static Shader FindShader()
        {
            return Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Diffuse");
        }
    }
}
