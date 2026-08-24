using UnityEngine;

namespace KingdomsOfBharat.Core
{
    // Builds the prototype's ground mesh at runtime: a grid with layered
    // (fractal) Perlin-noise height variation for a more natural silhouette
    // than a single noise octave, textured by a runtime-painted splat
    // texture (grass/dirt/rock blended by height and slope) rather than a
    // flat color - all still fully procedural, no authored heightmap or
    // Terrain asset needed. Height stays gentle enough (capped octave sum)
    // that NavMeshBaker's existing agentSlope/agentClimb settings keep the
    // whole map walkable.
    public class ProceduralGround : MonoBehaviour
    {
        [SerializeField] private float mapSize = 40f;
        [SerializeField] private int resolution = 40;
        [SerializeField] private float noiseHeight = 0.6f;
        [SerializeField] private float noiseScale = 0.15f;
        [SerializeField] private Color groundColor = new Color(0.36f, 0.5f, 0.28f);
        [SerializeField] private Color dirtColor = new Color(0.5f, 0.4f, 0.26f);
        [SerializeField] private Color rockColor = new Color(0.42f, 0.38f, 0.34f);
        [SerializeField] private int splatTextureSize = 256;

        private void Awake()
        {
            ApplyMapDefinition();

            Mesh mesh = BuildMesh();

            var meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var meshRenderer = gameObject.AddComponent<MeshRenderer>();
            var material = new Material(FindGroundShader());
            material.mainTexture = BuildSplatTexture();
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.05f);
            }
            meshRenderer.sharedMaterial = material;

            var meshCollider = gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;

            // Ground-height raycasts (HumanModelFactory, AiController) are
            // restricted to this layer specifically, so they can't hit a
            // unit's or building's collider standing in the way instead of
            // the actual terrain. Falls back to layer 0 (Default) if the
            // "Ground" layer hasn't been added to this project yet - still
            // works, just loses the raycast-filtering protection.
            int groundLayer = LayerMask.NameToLayer("Ground");
            gameObject.layer = groundLayer >= 0 ? groundLayer : 0;

            gameObject.isStatic = true;
        }

        // Item 44: pulls ground size/shape from whichever map
        // CivilizationSetup.BeginMatch selected, overriding this
        // component's own Inspector defaults. RiverValley's definition
        // matches those defaults exactly, so a scene that never calls
        // BeginMatch (or an older/test scene) behaves exactly as before.
        private void ApplyMapDefinition()
        {
            MapDefinitionData map = MapRegistry.Current;
            mapSize = map.GroundSize;
            resolution = map.GroundResolution;
            noiseHeight = map.NoiseHeight;
            noiseScale = map.NoiseScale;
        }

        // Same worldX/worldZ -> height formula the mesh uses, so the splat
        // texture's blend lines up exactly with the mesh's actual bumps
        // (sampled independently at texture resolution, not read back from
        // vertex data).
        private float HeightAt(float worldX, float worldZ)
        {
            float h = Mathf.PerlinNoise(worldX * noiseScale, worldZ * noiseScale) * noiseHeight;
            h += Mathf.PerlinNoise(worldX * noiseScale * 3.7f, worldZ * noiseScale * 3.7f) * noiseHeight * 0.3f;
            return h;
        }

        private Texture2D BuildSplatTexture()
        {
            var texture = new Texture2D(splatTextureSize, splatTextureSize, TextureFormat.RGB24, true)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            float half = mapSize * 0.5f;
            float sampleStep = mapSize / splatTextureSize;
            var pixels = new Color32[splatTextureSize * splatTextureSize];

            for (int z = 0; z < splatTextureSize; z++)
            {
                for (int x = 0; x < splatTextureSize; x++)
                {
                    float worldX = -half + x * sampleStep;
                    float worldZ = -half + z * sampleStep;
                    float height = HeightAt(worldX, worldZ);

                    // Estimate slope from neighboring samples rather than
                    // reading mesh normals - keeps the texture pass fully
                    // independent of mesh generation order.
                    float dHeightX = HeightAt(worldX + sampleStep, worldZ) - height;
                    float dHeightZ = HeightAt(worldX, worldZ + sampleStep) - height;
                    float slope = Mathf.Abs(dHeightX) + Mathf.Abs(dHeightZ);

                    // Patchy tint variation so grass isn't a flat single
                    // color - a second, unrelated-frequency noise layer.
                    float patch = Mathf.PerlinNoise(worldX * 0.6f + 100f, worldZ * 0.6f + 100f);

                    Color blended = Color.Lerp(groundColor * (0.92f + patch * 0.16f), dirtColor, Mathf.Clamp01(height / Mathf.Max(noiseHeight, 0.001f)));
                    blended = Color.Lerp(blended, rockColor, Mathf.Clamp01(slope * 6f));

                    pixels[z * splatTextureSize + x] = blended;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(true);
            return texture;
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
                    float height = HeightAt(worldX, worldZ);

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
