using UnityEngine;

namespace KingdomsOfBharat.Core
{
    // Replaces ProceduralGround (a hand-built mesh) with a real
    // UnityEngine.Terrain, per the user-directed "Option B" terrain
    // migration (docs/MAP_VISUAL_UPGRADE_PLAN.md). Same public contract as
    // ProceduralGround - a Rebuild() that reads MapRegistry.Current and
    // (re)builds ground for the current map - so CivilizationSetup's single
    // call site only needed a type-name swap.
    //
    // Every ground-height/collision consumer in this project (GroundReference,
    // NavMeshBaker, BuildingPlacer) keys off the GameObject's *name* ("Ground")
    // and a generic PhysicsCollider/raycast, not the component type - so a
    // Terrain + TerrainCollider on the same "Ground"-named object is a
    // functional drop-in for all of them with zero other code changes. The
    // scene's existing "Ground" GameObject keeps that name; only its
    // component set changes (ProceduralGround -> ProceduralTerrain).
    public class ProceduralTerrain : MonoBehaviour
    {
        // Component-level defaults (not per-map) - mirror ProceduralGround's
        // own Inspector defaults, used only for a scene/test context that
        // never calls CivilizationSetup.BeginMatch (which always selects a
        // real MapId first).
        [SerializeField] private float mapSize = 40f;
        [SerializeField] private float noiseHeight = 0.6f;
        [SerializeField] private float noiseScale = 0.15f;
        [SerializeField] private Color waterColor = new Color(0.15f, 0.35f, 0.55f, 0.85f);
        [SerializeField] private float waterSurfaceY = 0.25f;
        // Placeholder flat colors for the Grass/Dirt terrain layers when no
        // sourced texture exists yet under Resources/Terrain/<Layer>/Albedo -
        // see docs/MAP_VISUAL_UPGRADE_PLAN.md's sourcing checklist. Dropping
        // real files in later upgrades the look with no code change, since
        // BuildLayer() always tries Resources.Load first.
        [SerializeField] private Color grassColor = new Color(0.36f, 0.5f, 0.28f);
        [SerializeField] private Color dirtColor = new Color(0.5f, 0.4f, 0.26f);

        private Vector3 _waterCenter;
        private Vector3 _waterHalfExtents;
        private bool HasWater => _waterHalfExtents.x > 0f && _waterHalfExtents.z > 0f;

        // Standard power-of-two-plus-one heightmap resolution, decoupled from
        // MapDefinitionData.GroundResolution (which only ever controlled the
        // old mesh's vertex density, not noise frequency - HeightAt samples
        // raw world coordinates, so hill *shape* is unaffected by this
        // decoupling). GroundResolution is left unused on MapDefinitionData
        // rather than ripped out - a flagged, harmless dormant field.
        private const int HeightmapResolution = 257;
        private const int AlphamapResolution = 256;
        private const int HolesResolution = HeightmapResolution - 1;

        // Cached across Rebuild() calls (map switch/match restart) so
        // TerrainLayer ScriptableObjects aren't recreated - and leaked -
        // every time; only their per-call-independent texture content ever
        // needs building, once.
        private TerrainLayer[] _layers;

        private void Awake()
        {
            Rebuild();
        }

        // See ProceduralGround's own header note (now historical) on why
        // this is split from Awake(): CivilizationSetup.BeginMatchCore calls
        // this directly and synchronously right after MapRegistry.Select(),
        // before activating any gated content that assumes ground already
        // has its final size/shape.
        public void Rebuild()
        {
            ApplyMapDefinition();

            Terrain terrain = GetComponent<Terrain>();
            if (terrain == null)
            {
                terrain = gameObject.AddComponent<Terrain>();
            }

            if (terrain.materialTemplate == null)
            {
                // Terrain has no default material under URP unless one is
                // assigned; without it the terrain renders magenta.
                Shader terrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
                if (terrainShader != null)
                {
                    terrain.materialTemplate = new Material(terrainShader);
                }
            }

            TerrainCollider collider = GetComponent<TerrainCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<TerrainCollider>();
            }

            TerrainData data = terrain.terrainData;
            if (data == null)
            {
                data = new TerrainData { name = "ProceduralTerrainData" };
            }

            float half = mapSize * 0.5f;
            // Terrain's local origin is its min corner, not its center like
            // the old mesh's (-half..+half) convention - offset the
            // transform so world-space still spans -half..+half on X/Z,
            // matching every existing town-center/resource-position constant
            // in MapDefinitionData exactly.
            transform.position = new Vector3(-half, 0f, -half);

            // Two-octave sum's theoretical max is noiseHeight*1.3 - give a
            // little headroom so a full-amplitude peak never clips against
            // TerrainData.size.y.
            float terrainHeightScale = Mathf.Max(noiseHeight * 1.3f, 0.01f) * 1.25f;

            data.heightmapResolution = HeightmapResolution;
            data.size = new Vector3(mapSize, terrainHeightScale, mapSize);
            data.alphamapResolution = AlphamapResolution;

            ApplyHeights(data, terrainHeightScale, half);
            ApplyHoles(data, half);
            ApplyLayers(data);
            ApplyAlphamaps(data);

            terrain.terrainData = data;
            collider.terrainData = data;

            int groundLayer = LayerMask.NameToLayer("Ground");
            gameObject.layer = groundLayer >= 0 ? groundLayer : 0;
            gameObject.isStatic = true;

            Transform existingWater = transform.Find("Water");
            if (existingWater != null)
            {
                Destroy(existingWater.gameObject);
            }

            if (HasWater)
            {
                BuildWaterPlane();
            }
        }

        private void ApplyMapDefinition()
        {
            MapDefinitionData map = MapRegistry.Current;
            mapSize = map.GroundSize;
            noiseHeight = map.NoiseHeight;
            noiseScale = map.NoiseScale;
            _waterCenter = map.WaterCenter;
            _waterHalfExtents = map.WaterHalfExtents;
        }

        // Identical formula to ProceduralGround's own HeightAt, so slope
        // profile - and therefore NavMesh walkability under
        // ProjectSettings/NavMeshAreas.asset's existing agentSlope/agentClimb
        // limits - matches the proven-safe values exactly.
        private float HeightAt(float worldX, float worldZ)
        {
            float h = Mathf.PerlinNoise(worldX * noiseScale, worldZ * noiseScale) * noiseHeight;
            h += Mathf.PerlinNoise(worldX * noiseScale * 3.7f, worldZ * noiseScale * 3.7f) * noiseHeight * 0.3f;
            return h;
        }

        private void ApplyHeights(TerrainData data, float terrainHeightScale, float half)
        {
            var heights = new float[HeightmapResolution, HeightmapResolution];
            for (int z = 0; z < HeightmapResolution; z++)
            {
                float worldZ = -half + (float)z / (HeightmapResolution - 1) * mapSize;
                for (int x = 0; x < HeightmapResolution; x++)
                {
                    float worldX = -half + (float)x / (HeightmapResolution - 1) * mapSize;
                    heights[z, x] = HeightAt(worldX, worldZ) / terrainHeightScale;
                }
            }

            data.SetHeights(0, 0, heights);
        }

        // True in the holes[,] array means "solid ground" (Unity's own
        // convention); false is a hole - same "no ground geometry here" as
        // ProceduralGround's own triangle-skipping IsWaterCell check, so
        // collision/NavMesh naturally exclude the water rectangle exactly as
        // before.
        private void ApplyHoles(TerrainData data, float half)
        {
            var holes = new bool[HolesResolution, HolesResolution];
            for (int z = 0; z < HolesResolution; z++)
            {
                float cellCenterZ = -half + (z + 0.5f) / HolesResolution * mapSize;
                for (int x = 0; x < HolesResolution; x++)
                {
                    float cellCenterX = -half + (x + 0.5f) / HolesResolution * mapSize;
                    holes[z, x] = !IsWaterCell(cellCenterX, cellCenterZ);
                }
            }

            data.SetHoles(0, 0, holes);
        }

        private bool IsWaterCell(float worldX, float worldZ)
        {
            if (!HasWater)
            {
                return false;
            }

            return Mathf.Abs(worldX - _waterCenter.x) <= _waterHalfExtents.x
                && Mathf.Abs(worldZ - _waterCenter.z) <= _waterHalfExtents.z;
        }

        private void ApplyLayers(TerrainData data)
        {
            if (_layers == null)
            {
                _layers = new[]
                {
                    BuildLayer("Terrain/Grass", grassColor),
                    BuildLayer("Terrain/Dirt", dirtColor),
                    BuildLayer("Terrain/Rock", Color.gray),
                };
            }

            data.terrainLayers = _layers;
        }

        // Tries real sourced PBR textures first (Resources.Load returns null
        // if none exist yet), falls back to a flat-color placeholder -
        // dropping real files into Resources/<resourcePrefix>/{Albedo,Normal}
        // later upgrades the look with no code change. Rock's Mask texture
        // (a Metallic map, per PolishedSurfaces' own naming) is deliberately
        // NOT wired into maskMapTexture this session - its channel packing
        // isn't confirmed to match URP Terrain Lit's expected
        // Metallic/AO/Height/Smoothness layout, flagged rather than guessed.
        private static TerrainLayer BuildLayer(string resourcePrefix, Color placeholderColor)
        {
            Texture2D albedo = Resources.Load<Texture2D>(resourcePrefix + "/Albedo") ?? FlatTexture(placeholderColor);
            Texture2D normal = Resources.Load<Texture2D>(resourcePrefix + "/Normal");

            var layer = new TerrainLayer
            {
                name = resourcePrefix,
                diffuseTexture = albedo,
                normalMapTexture = normal,
                tileSize = new Vector2(4f, 4f),
                smoothness = 0.1f,
                metallic = 0f,
            };
            return layer;
        }

        private static Texture2D FlatTexture(Color color)
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGB24, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color32[16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            texture.SetPixels32(pixels);
            texture.Apply(true);
            return texture;
        }

        // Same height/slope blend rule ProceduralGround's own
        // BuildSplatTexture used (height ratio -> dirt, slope -> rock), now
        // painted as real TerrainLayer weights instead of a runtime color
        // texture - URP's built-in terrain shader blends the layers
        // automatically, no custom shader needed. The old per-pixel "patch"
        // noise tint is dropped: it only mattered for a flat placeholder
        // color, and real tiled textures (once sourced) carry their own
        // visual variation already.
        private void ApplyAlphamaps(TerrainData data)
        {
            var map = new float[AlphamapResolution, AlphamapResolution, 3];
            float half = mapSize * 0.5f;
            float sampleStep = mapSize / AlphamapResolution;

            for (int z = 0; z < AlphamapResolution; z++)
            {
                float worldZ = -half + z * sampleStep;
                for (int x = 0; x < AlphamapResolution; x++)
                {
                    float worldX = -half + x * sampleStep;
                    float height = HeightAt(worldX, worldZ);

                    float dHeightX = HeightAt(worldX + sampleStep, worldZ) - height;
                    float dHeightZ = HeightAt(worldX, worldZ + sampleStep) - height;
                    float slope = Mathf.Abs(dHeightX) + Mathf.Abs(dHeightZ);

                    float dirtWeight = Mathf.Clamp01(height / Mathf.Max(noiseHeight, 0.001f));
                    float rockWeight = Mathf.Clamp01(slope * 6f);
                    float remaining = 1f - rockWeight;

                    map[z, x, 0] = remaining * (1f - dirtWeight); // grass
                    map[z, x, 1] = remaining * dirtWeight; // dirt
                    map[z, x, 2] = rockWeight; // rock
                }
            }

            data.SetAlphamaps(0, 0, map);
        }

        // Byte-for-byte the same mechanism ProceduralGround used: a flat,
        // semi-transparent quad over the water rectangle, no collider (boats
        // use WaterMover/WaterProximity, not NavMeshAgent). Untouched by this
        // migration - a real water shader is a separate, later phase.
        private void BuildWaterPlane()
        {
            var waterGo = new GameObject("Water");
            waterGo.transform.SetParent(transform, false);
            // .position is always a world-space setter regardless of the
            // parent's own offset (this object's transform now sits at the
            // terrain's min corner, not the origin) - Unity computes the
            // correct local offset internally, same as ProceduralGround's
            // original code.
            waterGo.transform.position = new Vector3(_waterCenter.x, waterSurfaceY, _waterCenter.z);

            var mesh = new Mesh { name = "WaterPlane" };
            float hx = _waterHalfExtents.x;
            float hz = _waterHalfExtents.z;
            mesh.vertices = new[]
            {
                new Vector3(-hx, 0f, -hz), new Vector3(hx, 0f, -hz),
                new Vector3(-hx, 0f, hz), new Vector3(hx, 0f, hz),
            };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            waterGo.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = waterGo.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = GameplayMaterial.CreateTransparent(waterColor);
        }
    }
}
