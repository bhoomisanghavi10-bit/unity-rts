using UnityEngine;

namespace KingdomsOfBharat.Core
{
    // Builds the prototype's ground mesh at runtime: a flat grid with mild
    // Perlin-noise height variation, so the map has visible terrain without
    // needing an authored heightmap or Terrain asset yet.
    public class ProceduralGround : MonoBehaviour
    {
        [SerializeField] private float mapSize = 40f;
        [SerializeField] private int resolution = 40;
        [SerializeField] private float noiseHeight = 0.6f;
        [SerializeField] private float noiseScale = 0.15f;
        [SerializeField] private Color groundColor = new Color(0.36f, 0.5f, 0.28f);

        private void Awake()
        {
            Mesh mesh = BuildMesh();

            var meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = new Material(FindGroundShader()) { color = groundColor };

            var meshCollider = gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;

            gameObject.isStatic = true;
        }

        // Projects don't ship a render pipeline choice at this stage, so try
        // URP's lit shader first and fall back to the built-in pipeline's.
        private static Shader FindGroundShader()
        {
            return Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Diffuse");
        }

        private Mesh BuildMesh()
        {
            int vertsPerSide = resolution + 1;
            var vertices = new Vector3[vertsPerSide * vertsPerSide];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[resolution * resolution * 6];

            float half = mapSize * 0.5f;
            float step = mapSize / resolution;

            for (int z = 0; z <= resolution; z++)
            {
                for (int x = 0; x <= resolution; x++)
                {
                    int i = z * vertsPerSide + x;
                    float worldX = -half + x * step;
                    float worldZ = -half + z * step;
                    float height = Mathf.PerlinNoise(worldX * noiseScale, worldZ * noiseScale) * noiseHeight;

                    vertices[i] = new Vector3(worldX, height, worldZ);
                    uvs[i] = new Vector2((float)x / resolution, (float)z / resolution);
                }
            }

            int t = 0;
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int i = z * vertsPerSide + x;

                    triangles[t++] = i;
                    triangles[t++] = i + vertsPerSide;
                    triangles[t++] = i + 1;

                    triangles[t++] = i + 1;
                    triangles[t++] = i + vertsPerSide;
                    triangles[t++] = i + vertsPerSide + 1;
                }
            }

            var mesh = new Mesh { name = "ProceduralGround" };
            mesh.indexFormat = vertices.Length > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
