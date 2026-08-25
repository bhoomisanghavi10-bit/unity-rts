using System.Collections.Generic;
using UnityEngine;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Units;
using KingdomsOfBharat.Buildings;
using KingdomsOfBharat.Wildlife;

namespace KingdomsOfBharat.FogOfWar
{
    // Grid-based fog of war over the Player's vision only. Not every-frame:
    // recomputes on an interval, fully re-deriving cell state from scratch
    // each time (never an incremental diff - see the Gatherer._dropOff bug
    // for why that class of "stale cached state" bug is worth avoiding here
    // deliberately). Renders via a hand-built quad mesh with NO collider
    // (never CreatePrimitive) since SelectionManager/BuildingPlacer raycast
    // completely unfiltered and a stray collider would break every click in
    // the game.
    public class FogOfWarManager : MonoBehaviour
    {
        private enum CellState : byte { Unexplored, Explored, Visible }

        [SerializeField] private int gridSize = 40;
        [SerializeField] private float worldSize = 40f;
        [SerializeField] private float recomputeInterval = 0.25f;
        [SerializeField] private float quadSize = 48f;
        [SerializeField] private float quadHeight = 2.5f;

        private CellState[] _cells;
        private Texture2D _texture;
        private float _timer;
        private bool _wasMatchStarted;

        // Phase 5 map-awareness fix: this component is always-active from
        // scene load (not one of CivilizationSetup's gatedMatchContent, the
        // way NavMeshBaker/ResourceNodeSpawner/etc. are), so an Awake()-time
        // read of MapRegistry.Current - like NavMeshBaker's own fix - would
        // still see the stale RiverValley default, since the player hasn't
        // picked a map through CivPicker yet at that point. Grid/texture/
        // quad are built lazily instead, on the first Update() after
        // HasMatchStarted flips true, by which point MapRegistry.Select has
        // already run (CivilizationSetup.BeginMatchCore calls it before
        // activating any gated content).
        private void Update()
        {
            if (!CivilizationSetup.HasMatchStarted)
            {
                _wasMatchStarted = false;
                return;
            }

            if (!_wasMatchStarted)
            {
                _wasMatchStarted = true;
                InitializeForCurrentMap();
            }

            _timer += Time.deltaTime;
            if (_timer < recomputeInterval)
            {
                return;
            }

            _timer = 0f;
            Recompute();
        }

        // worldSize/gridSize/quadSize were previously fixed Inspector
        // defaults sized for RiverValley (40) only - correct there, but
        // wrong on Highlands (52) and Coastal (50): the world->grid
        // conversion used the wrong scale, and the fog quad (48) was
        // literally smaller than either map's actual ground, leaving a
        // real strip near the edge with no fog overlay at all. gridSize
        // keeps the same 1-cell-per-world-unit ratio the 40/40f defaults
        // already implied; quadSize keeps the same +8 margin over ground
        // size the defaults implied (48 - 40 = 8), so the quad still
        // extends a bit past the ground edge on every map instead of just
        // barely covering RiverValley's.
        private void InitializeForCurrentMap()
        {
            worldSize = MapRegistry.Current.GroundSize;
            gridSize = Mathf.RoundToInt(worldSize);
            quadSize = worldSize + 8f;

            _cells = new CellState[gridSize * gridSize];
            BuildTexture();
            BuildQuad();
            Recompute();
        }

        private void Recompute()
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                if (_cells[i] == CellState.Visible)
                {
                    _cells[i] = CellState.Explored;
                }
            }

            foreach (VisionSource source in VisionSource.All)
            {
                RevealAround(source.transform.position, source.VisionRadius);
            }

            RepaintTexture();
            UpdateEnemyVisibility();
        }

        private void RevealAround(Vector3 worldPosition, float radius)
        {
            (int centerX, int centerZ) = WorldToCell(worldPosition);
            int cellRadius = Mathf.CeilToInt(radius);

            for (int dz = -cellRadius; dz <= cellRadius; dz++)
            {
                for (int dx = -cellRadius; dx <= cellRadius; dx++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;
                    if (x < 0 || x >= gridSize || z < 0 || z >= gridSize)
                    {
                        continue;
                    }

                    if (dx * dx + dz * dz > cellRadius * cellRadius)
                    {
                        continue;
                    }

                    _cells[z * gridSize + x] = CellState.Visible;
                }
            }
        }

        private (int x, int z) WorldToCell(Vector3 worldPosition)
        {
            float half = worldSize * 0.5f;
            int x = Mathf.FloorToInt((worldPosition.x + half) / worldSize * gridSize);
            int z = Mathf.FloorToInt((worldPosition.z + half) / worldSize * gridSize);
            x = Mathf.Clamp(x, 0, gridSize - 1);
            z = Mathf.Clamp(z, 0, gridSize - 1);
            return (x, z);
        }

        private void RepaintTexture()
        {
            var pixels = new Color32[_cells.Length];
            for (int i = 0; i < _cells.Length; i++)
            {
                if (_cells[i] == CellState.Unexplored)
                {
                    pixels[i] = new Color32(0, 0, 0, 255);
                }
                else if (_cells[i] == CellState.Explored)
                {
                    pixels[i] = new Color32(0, 0, 0, 140);
                }
                else
                {
                    pixels[i] = new Color32(0, 0, 0, 0);
                }
            }

            _texture.SetPixels32(pixels);
            _texture.Apply(false);
        }

        // Hides mobile things the player hasn't currently got vision on:
        // hostile-faction units/buildings, and wild boars (which wander
        // like units, so are treated the same way). Static resource nodes
        // (trees, farmland, carcasses...) are deliberately left alone here
        // and stay visible once explored - matching how AoE treats terrain
        // features versus units, and simpler than retrofitting every
        // resource spawner with fog awareness for a cosmetic difference.
        // Item 48: gated on "not Player and not allied with Player" rather
        // than a literal Enemy check, so an ally's units stay visible (you
        // can see your ally, same as AoE) while any hostile faction -
        // Enemy, Enemy2, or a former ally who declared war - still fogs.
        private void UpdateEnemyVisibility()
        {
            foreach (Unit unit in Unit.All)
            {
                if (unit.TryGetComponent(out FactionMember factionMember) && IsFogged(factionMember.Faction))
                {
                    SetVisibilityByCell(unit.gameObject);
                }
            }

            foreach (Building building in Building.All)
            {
                if (building.TryGetComponent(out FactionMember factionMember) && IsFogged(factionMember.Faction))
                {
                    SetVisibilityByCell(building.gameObject);
                }
            }

            foreach (WildBoar boar in FindObjectsByType<WildBoar>(FindObjectsSortMode.None))
            {
                SetVisibilityByCell(boar.gameObject);
            }
        }

        private static bool IsFogged(FactionId faction)
        {
            return faction != FactionId.Player && !DiplomacyRegistry.AreAllied(FactionId.Player, faction);
        }

        private void SetVisibilityByCell(GameObject go)
        {
            (int x, int z) = WorldToCell(go.transform.position);
            bool visible = _cells[z * gridSize + x] == CellState.Visible;

            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                r.enabled = visible;
            }

            foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
            {
                c.enabled = visible;
            }
        }

        private void BuildTexture()
        {
            _texture = new Texture2D(gridSize, gridSize, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
        }

        private void BuildQuad()
        {
            float half = quadSize * 0.5f;
            var vertices = new Vector3[]
            {
                new Vector3(-half, 0f, -half),
                new Vector3(half, 0f, -half),
                new Vector3(-half, 0f, half),
                new Vector3(half, 0f, half),
            };
            var uvs = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
            };
            // Same winding order as ProceduralGround's terrain mesh (proven
            // to face upward toward the camera after RecalculateNormals).
            var triangles = new[] { 0, 2, 1, 1, 2, 3 };

            var mesh = new Mesh { name = "FogQuad" };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            // No CreatePrimitive: MeshFilter/MeshRenderer only, so no
            // Collider is ever added to intercept SelectionManager's or
            // BuildingPlacer's unfiltered Physics.Raycast calls.
            var meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            Material material = new Material(GameplayMaterial.FindUnlitShader());
            material.mainTexture = _texture;
            GameplayMaterial.ForceTransparent(material);
            meshRenderer.sharedMaterial = material;

            transform.position = new Vector3(0f, quadHeight, 0f);
        }
    }
}
