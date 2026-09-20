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

        // Shoreline profile (world units). Land blends down to the waterline
        // over BankWidth, then drops to the bed over BedDropWidth; the
        // waterline (terrain height == waterSurfaceY) sits exactly on the
        // gameplay water rectangle's edge so WaterProximity/WaterMover and
        // the NavMesh exclusion all agree with what the player sees.
        private const float BankWidth = 5f;
        private const float BedDropWidth = 14f;
        private const float BedDepth = 1.5f;
        private const float BeachWidth = 3.5f;
        private const float BeachBerm = 0.3f;
        private const int SandLayerIndex = 3;
        private const int PebbleLayerIndex = 4;

        private float _bedBelowZero;
        private Vector4 _waterRect; // xMin, xMax, zMin, zMax (sides at the map edge extended to infinity)
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
            _bedBelowZero = HasWater ? Mathf.Max(0f, BedDepth - waterSurfaceY) : 0f;
            transform.position = new Vector3(-half, -_bedBelowZero, -half);
            ComputeWaterRect(half);

            // Two-octave sum's theoretical max is noiseHeight*1.3 - give a
            // little headroom so a full-amplitude peak never clips against
            // TerrainData.size.y.
            float terrainHeightScale = Mathf.Max(noiseHeight * 1.3f, 0.01f) * 1.25f;

            data.heightmapResolution = HeightmapResolution;
            terrainHeightScale += _bedBelowZero;
            data.size = new Vector3(mapSize, terrainHeightScale, mapSize);
            data.alphamapResolution = AlphamapResolution;

            ApplyHeights(data, terrainHeightScale, half);
            ClearHoles(data);
            ApplyLayers(data);
            float[,,] alphamap = ApplyAlphamaps(data);

            terrain.terrainData = data;
            collider.terrainData = data;
            TerrainClutter.Apply(terrain, data, alphamap, AlphamapResolution);

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
                    heights[z, x] = (ShapedHeightAt(worldX, worldZ) + _bedBelowZero) / terrainHeightScale;
                }
            }

            data.SetHeights(0, 0, heights);
        }

        // The riverbed is now real terrain (sloped banks, sunken bed) instead
        // of a hole, so the shoreline can be a gradient. Water is kept
        // unwalkable by NavMeshBaker (a not-walkable box over the water
        // rectangle), not by missing geometry. TerrainData is reused across
        // Rebuild() calls, so clear any holes an earlier build left behind.
        private void ClearHoles(TerrainData data)
        {
            var holes = new bool[HolesResolution, HolesResolution];
            for (int z = 0; z < HolesResolution; z++)
            {
                for (int x = 0; x < HolesResolution; x++)
                {
                    holes[z, x] = true;
                }
            }

            data.SetHoles(0, 0, holes);
        }

        // Water rectangle with any side that reaches the map edge extended
        // far outward, so the bank only forms along real shorelines and not
        // along the map border.
        private void ComputeWaterRect(float half)
        {
            if (!HasWater)
            {
                _waterRect = Vector4.zero;
                return;
            }

            const float far = 10000f;
            float xMin = _waterCenter.x - _waterHalfExtents.x;
            float xMax = _waterCenter.x + _waterHalfExtents.x;
            float zMin = _waterCenter.z - _waterHalfExtents.z;
            float zMax = _waterCenter.z + _waterHalfExtents.z;
            if (xMin <= -half + 0.01f) xMin = -far;
            if (xMax >= half - 0.01f) xMax = far;
            if (zMin <= -half + 0.01f) zMin = -far;
            if (zMax >= half - 0.01f) zMax = far;
            _waterRect = new Vector4(xMin, xMax, zMin, zMax);
        }

        // Positive inside the water rectangle (distance to the nearest
        // shoreline edge), negative outside (distance to the rectangle).
        private float SignedWaterDistance(float x, float z)
        {
            if (!HasWater)
            {
                return -10000f;
            }

            // Real east/west shorelines wobble inward (see
            // WaterProximity.ShoreInsetAt); sides extended to the map edge don't.
            float wobble = WaterProximity.ShoreInsetAt(z);
            float xMin = _waterRect.x > -1000f ? _waterRect.x + wobble : _waterRect.x;
            float xMax = _waterRect.y < 1000f ? _waterRect.y - wobble : _waterRect.y;
            float inside = Mathf.Min(
                Mathf.Min(x - xMin, xMax - x),
                Mathf.Min(z - _waterRect.z, _waterRect.w - z));
            if (inside >= 0f)
            {
                return inside;
            }

            float dx = Mathf.Max(0f, Mathf.Max(xMin - x, x - xMax));
            float dz = Mathf.Max(0f, Mathf.Max(_waterRect.z - z, z - _waterRect.w));
            return -Mathf.Sqrt(dx * dx + dz * dz);
        }

        // Base rolling noise plus the carved bank/bed around the water.
        private float ShapedHeightAt(float worldX, float worldZ)
        {
            float land = HeightAt(worldX, worldZ);
            if (!HasWater)
            {
                return land;
            }

            float s = SignedWaterDistance(worldX, worldZ);
            if (s <= -BankWidth)
            {
                return land;
            }

            if (s <= 0f)
            {
                // Land eases down to exactly the waterline at the edge.
                // Land eases down to exactly the waterline at the edge, with a
                // small beach berm added so the ground crosses the waterline
                // at a real slope (~13 deg) even where the noise sits right at
                // water level - a flat waterline makes the depth-driven foam
                // and shallow tint flood a wide strip.
                float u = (s + BankWidth) / BankWidth;
                return Mathf.Lerp(land, waterSurfaceY, u) + BeachBerm * 4f * u * (1f - u);
            }

            float bed = waterSurfaceY - BedDepth;
            // Ease-out (t * (2 - t)) rather than smoothstep: the bed
            // must already have real slope at the waterline, otherwise the
            // "almost no water" strip is several flat units wide and the
            // depth-driven foam floods it.
            float tb = Mathf.Clamp01(s / BedDropWidth);
            return Mathf.Lerp(waterSurfaceY, bed, tb * (2f - tb));
        }

        private void ApplyLayers(TerrainData data)
        {
            if (_layers == null)
            {
                _layers = new[]
                {
                    BuildLayer("Terrain/Grass", grassColor, 0.1f, 3f, 1.3f),
                    BuildLayer("Terrain/Dirt", dirtColor, 0.1f, 3f, 1.3f),
                    BuildLayer("Terrain/Rock", Color.gray),
                    BuildLayer("Terrain/Sand", new Color(0.6f, 0.53f, 0.4f)),
                    // Pre-darkened (wet) pebble albedo, glossier than the
                    // other layers, for the waterline band. Small tile so
                    // the individual stones read at RTS camera height.
                    BuildLayer("Terrain/Pebbles", new Color(0.35f, 0.31f, 0.26f), 0.55f, 3f),
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
        private static TerrainLayer BuildLayer(string resourcePrefix, Color placeholderColor, float smoothness = 0.1f, float tileSize = 4f, float normalScale = 1f)
        {
            Texture2D albedo = Resources.Load<Texture2D>(resourcePrefix + "/Albedo") ?? FlatTexture(placeholderColor);
            Texture2D normal = Resources.Load<Texture2D>(resourcePrefix + "/Normal");

            var layer = new TerrainLayer
            {
                name = resourcePrefix,
                diffuseTexture = albedo,
                normalMapTexture = normal,
                tileSize = new Vector2(tileSize, tileSize),
                smoothness = smoothness,
                normalScale = normalScale,
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
        private float[,,] ApplyAlphamaps(TerrainData data)
        {
            var map = new float[AlphamapResolution, AlphamapResolution, 5];
            float half = mapSize * 0.5f;
            float sampleStep = mapSize / AlphamapResolution;

            for (int z = 0; z < AlphamapResolution; z++)
            {
                float worldZ = -half + z * sampleStep;
                for (int x = 0; x < AlphamapResolution; x++)
                {
                    float worldX = -half + x * sampleStep;
                    // Grass/dirt/rock are driven by the *base* noise so the
                    // carved bank's slope doesn't paint itself as rock.
                    float height = HeightAt(worldX, worldZ);

                    float dHeightX = HeightAt(worldX + sampleStep, worldZ) - height;
                    float dHeightZ = HeightAt(worldX, worldZ + sampleStep) - height;
                    float slope = Mathf.Abs(dHeightX) + Mathf.Abs(dHeightZ);

                    // Height drives the broad grass/dirt split; a mid-frequency
                    // noise term breaks up the smooth blobs so patches vary at
                    // several scales instead of one.
                    float macro = (Mathf.PerlinNoise(worldX * 0.4f + 70f, worldZ * 0.4f + 9f) - 0.5f) * 0.5f;
                    float dirtWeight = Mathf.Clamp01(height / Mathf.Max(noiseHeight, 0.001f) + macro);
                    // Rock only on genuinely steep ground. `slope` is a height
                    // delta across one alphamap texel, so convert to a
                    // gradient (rise per world unit) first. The old
                    // `slope * 6` fired on ordinary rolling hills and bled
                    // the slab-like rock texture through the grass as
                    // blocky patches.
                    float gradient = slope / sampleStep;
                    float rockWeight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 0.7f, gradient));
                    float remaining = 1f - rockWeight;

                    float grass = remaining * (1f - dirtWeight);
                    float dirt = remaining * dirtWeight;
                    float rock = rockWeight;

                    // Beach: sand from the waterline out to a noisy width
                    // (so the strip isn't a ruler-straight band), solid sand
                    // under water.
                    float sand = 0f;
                    if (HasWater)
                    {
                        float s = SignedWaterDistance(worldX, worldZ);
                        float width = BeachWidth * (0.6f + 0.8f * Mathf.PerlinNoise(worldX * 0.35f + 91f, worldZ * 0.35f + 17f));
                        sand = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-width, -0.3f, s));
                    }

                    // Wet pebble band: starts a little above the waterline,
                    // runs under the surface so the shallows show a pebbly
                    // bed, patchy (noise) so it isn't a uniform stripe.
                    float pebble = 0f;
                    if (HasWater)
                    {
                        float s = SignedWaterDistance(worldX, worldZ);
                        float patch = 0.55f + 0.45f * Mathf.PerlinNoise(worldX * 0.5f + 5.3f, worldZ * 0.5f + 44.1f);
                        float reach = 1.6f * (0.7f + 0.6f * Mathf.PerlinNoise(worldX * 0.3f + 13f, worldZ * 0.3f + 2f));
                        float rise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-reach, -0.3f, s));
                        float fall = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2.5f, 5f, s));
                        pebble = rise * fall * patch;
                    }

                    float keep = (1f - sand) * (1f - pebble);
                    float sandKeep = sand * (1f - pebble);
                    map[z, x, 0] = grass * keep;
                    map[z, x, 1] = dirt * keep;
                    map[z, x, 2] = rock * keep;
                    map[z, x, SandLayerIndex] = sandKeep;
                    map[z, x, PebbleLayerIndex] = pebble;
                }
            }

            data.SetAlphamaps(0, 0, map);
            return map;
        }

        // Depth-fade water shader + generated tileable ripple normal map
        // (Resources/Terrain/Water/Normal - ambientCG has no water material,
        // so it's a procedurally generated CC0 asset). Falls back to the old
        // flat transparent material if the shader can't be found, so a
        // missing shader degrades to the previous look instead of magenta.
        private Material BuildWaterMaterial()
        {
            Shader shader = Shader.Find("KingdomsOfBharat/Water");
            if (shader == null)
            {
                return GameplayMaterial.CreateTransparent(waterColor);
            }

            var material = new Material(shader) { name = "KobWater" };
            // Reflected-sky gradient follows the scene's fog (horizon) and
            // ambient sky (zenith) colours.
            material.SetColor("_SkyHorizon", RenderSettings.fogColor);
            material.SetColor("_SkyZenith", RenderSettings.ambientSkyColor);
            Texture2D normal = Resources.Load<Texture2D>("Terrain/Water/Normal");
            if (normal != null)
            {
                material.SetTexture("_NormalMap", normal);
            }
            return material;
        }

        // Byte-for-byte the same mechanism ProceduralGround used: a flat,
        // semi-transparent quad over the water rectangle, no collider (boats
        // use WaterMover/WaterProximity, not NavMeshAgent). Untouched by this
        // migration - a real water shader is a separate, later phase.
        private void BuildWaterPlane()
        {
            var waterGo = new GameObject("Water");
            int waterLayer = LayerMask.NameToLayer("Water");
            if (waterLayer >= 0)
            {
                waterGo.layer = waterLayer;
            }
            waterGo.transform.SetParent(transform, false);
            // .position is always a world-space setter regardless of the
            // parent's own offset (this object's transform now sits at the
            // terrain's min corner, not the origin) - Unity computes the
            // correct local offset internally, same as ProceduralGround's
            // original code.
            waterGo.transform.position = new Vector3(_waterCenter.x, waterSurfaceY, _waterCenter.z);
            WaterProximity.SurfaceY = waterSurfaceY;

            // Grid (not a single quad) so the shader's vertex swell has
            // vertices to move. World-XZ drives the ripple UVs, so mesh UVs
            // are unused.
            float hx = _waterHalfExtents.x;
            float hz = _waterHalfExtents.z;
            const float cell = 2.5f;
            int nx = Mathf.Max(1, Mathf.CeilToInt(hx * 2f / cell));
            int nz = Mathf.Max(1, Mathf.CeilToInt(hz * 2f / cell));
            var vertices = new Vector3[(nx + 1) * (nz + 1)];
            for (int iz = 0; iz <= nz; iz++)
            {
                for (int ix = 0; ix <= nx; ix++)
                {
                    vertices[iz * (nx + 1) + ix] = new Vector3(
                        Mathf.Lerp(-hx, hx, (float)ix / nx), 0f, Mathf.Lerp(-hz, hz, (float)iz / nz));
                }
            }

            var triangles = new int[nx * nz * 6];
            int t = 0;
            for (int iz = 0; iz < nz; iz++)
            {
                for (int ix = 0; ix < nx; ix++)
                {
                    int i0 = iz * (nx + 1) + ix;
                    int i1 = i0 + 1;
                    int i2 = i0 + (nx + 1);
                    int i3 = i2 + 1;
                    triangles[t++] = i0; triangles[t++] = i2; triangles[t++] = i1;
                    triangles[t++] = i1; triangles[t++] = i2; triangles[t++] = i3;
                }
            }

            var mesh = new Mesh { name = "WaterPlane", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            waterGo.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = waterGo.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = BuildWaterMaterial();
            waterGo.AddComponent<PlanarReflection>();
        }
    }
}
