using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Combat
{
    // Milestone-6 placeholder: places a stationary target dummy to test
    // combat against. Tagged Enemy faction (milestone 9) so it doubles as a
    // way to test fog-of-war hide/reveal before milestone 10's real AI
    // opponent exists - this doesn't change its attackability, since the
    // same-faction exclusion only blocks Player-vs-Player targeting.
    public class TargetDummySpawner : MonoBehaviour
    {
        [SerializeField] private Vector3 position = new Vector3(10f, 1f, -6f);
        [SerializeField] private float health = 40f;
        [SerializeField] private Color color = new Color(0.6f, 0.15f, 0.15f);

        private void Awake()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "TargetDummy";
            go.transform.position = position;
            go.transform.localScale = new Vector3(1f, 2f, 1f);

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(FindShader()) { color = color };

            go.AddComponent<TargetDummy>();
            var attackable = go.AddComponent<Attackable>();
            attackable.Configure(health);
            go.AddComponent<FactionMember>().Configure(FactionId.Enemy);
        }

        private static Shader FindShader()
        {
            return Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Diffuse");
        }
    }
}
