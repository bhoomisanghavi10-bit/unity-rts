using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.FogOfWar;

namespace KingdomsOfBharat.Buildings
{
    // Milestone-4 placeholder: places a single Town Center so gathered
    // resources have somewhere to be dropped off. Milestone 5 replaces this
    // with player-placed construction.
    public class TownCenterSpawner : MonoBehaviour
    {
        [SerializeField] private Vector3 position = new Vector3(0f, 1f, 8f);
        [SerializeField] private Color color = new Color(0.55f, 0.5f, 0.45f);

        private void Awake()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "TownCenter";
            go.transform.position = position;
            go.transform.localScale = new Vector3(3f, 2f, 3f);

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(FindShader()) { color = color };

            go.AddComponent<TownCenter>();
            go.AddComponent<FactionMember>().Configure(FactionId.Player);
            go.AddComponent<VisionSource>().Configure(10f);
        }

        private static Shader FindShader()
        {
            return Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Diffuse");
        }
    }
}
