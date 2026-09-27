using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Buildings
{
    // Farm's visual is a flat, pre-rendered isometric sprite lying on the
    // ground rather than a 3D mesh, unlike every other building in this
    // project - a deliberate fit, not a shortcut: the scene's Main Camera
    // is permanently fixed Orthographic at 30deg pitch/45deg yaw
    // (RTSCameraController's own "AoE-style fixed isometric camera... never
    // rotated by this controller"), so a pre-rendered isometric tile reads
    // correctly from that exact angle with zero perspective distortion -
    // the same technique classic isometric strategy games use for ground
    // tile art.
    //
    // Swaps between 3 real gameplay states live (not baked in at spawn):
    // under construction, full/harvestable, and depleted - driven by the
    // sibling Farm/ConstructionSite components' own live state, the same
    // "recompute, don't cache" convention this project's other live-state
    // UI (e.g. FarmWorker's harvest/reseed switch) already uses.
    public class FarmVisual : MonoBehaviour
    {
        private enum State
        {
            Construction,
            Full,
            Depleted,
        }

        private static Mesh _sharedQuad;
        private static Material _constructionMaterial;
        private static Material _fullMaterial;
        private static Material _depletedMaterial;

        private Farm _farm;
        private bool _farmResolved;
        private ConstructionSite _site;
        private bool _siteResolved;
        private MeshRenderer _renderer;
        private State? _lastState;

        // Lazily resolved rather than cached in Awake - same "AddComponent
        // ordering hazard" this project's own Farm.cs/FarmWorker.cs already
        // guard against: FarmFactory.Place builds this visual BEFORE adding
        // the sibling Farm/ConstructionSite components, and Unity doesn't
        // guarantee Awake has run synchronously by the time those siblings
        // actually exist.
        private Farm ParentFarm
        {
            get
            {
                if (!_farmResolved)
                {
                    _farm = GetComponentInParent<Farm>();
                    _farmResolved = true;
                }
                return _farm;
            }
        }

        private ConstructionSite ParentSite
        {
            get
            {
                if (!_siteResolved)
                {
                    _site = GetComponentInParent<ConstructionSite>();
                    _siteResolved = true;
                }
                return _site;
            }
        }

        // Builds the "Visual" child (same child name every other building's
        // BuildingModelFactory-built visual uses, so anything that walks
        // transform.Find("Visual") elsewhere keeps working) as a flat quad
        // lying on the ground, sized to the given footprint (world units).
        // Lifted a hair above y=0 (local to the parent, which is itself
        // already ground-aligned by the caller) to avoid z-fighting against
        // the terrain directly underneath a coplanar decal.
        public static GameObject Build(Transform parent, float footprintSize)
        {
            var go = new GameObject("Visual");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            go.transform.localScale = new Vector3(footprintSize, 1f, footprintSize);

            var meshFilter = go.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = SharedQuad();

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.sharedMaterial = ConstructionMaterial();

            var visual = go.AddComponent<FarmVisual>();
            visual._renderer = renderer;
            return go;
        }

        private void Awake()
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<MeshRenderer>();
            }
        }

        private void Update()
        {
            RefreshFromState();
        }

        // Internal (not private) so EditMode tests can drive this directly
        // without depending on Unity's Update loop actually ticking - same
        // convention as Farm.Tick/ConstructionSite's EnsureInitialized (see
        // AssemblyInfo.cs's InternalsVisibleTo grant).
        internal void RefreshFromState()
        {
            Farm farm = ParentFarm;
            if (farm == null)
            {
                return;
            }

            ConstructionSite site = ParentSite;
            bool complete = site == null || site.IsComplete;
            State state = !complete
                ? State.Construction
                : (farm.IsDepleted ? State.Depleted : State.Full);

            if (_lastState == state)
            {
                return;
            }

            _lastState = state;
            _renderer.sharedMaterial = state switch
            {
                State.Construction => ConstructionMaterial(),
                State.Depleted => DepletedMaterial(),
                _ => FullMaterial(),
            };
        }

        private static Mesh SharedQuad()
        {
            if (_sharedQuad != null)
            {
                return _sharedQuad;
            }

            // Same vertex/winding convention as FogOfWarManager.BuildQuad -
            // a flat XZ-plane quad proven to face upward toward the camera
            // after RecalculateNormals, reused here rather than re-derived.
            var vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, -0.5f),
                new Vector3(-0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0f, 0.5f),
            };
            var uvs = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
            };
            var triangles = new[] { 0, 2, 1, 1, 2, 3 };

            var mesh = new Mesh { name = "FarmVisualQuad" };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            _sharedQuad = mesh;
            return mesh;
        }

        private static Material ConstructionMaterial() =>
            _constructionMaterial ??= BuildMaterial("buildings/Farm/Construction");

        private static Material FullMaterial() =>
            _fullMaterial ??= BuildMaterial("buildings/Farm/Full");

        private static Material DepletedMaterial() =>
            _depletedMaterial ??= BuildMaterial("buildings/Farm/Depleted");

        // Unlit - the art already carries its own baked isometric shading,
        // a lit shader would double-light it. Alpha-clipped (not blended):
        // this is a coplanar ground decal, and alpha blending disables
        // ZWrite (see GameplayMaterial.ForceTransparent's own comment),
        // which risks z-fighting/sorting artifacts against the terrain
        // directly underneath - alpha test keeps ZWrite on.
        private static Material BuildMaterial(string resourcePath)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            Material material = new Material(GameplayMaterial.FindUnlitShader());
            material.mainTexture = texture;
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }
            if (material.HasProperty("_AlphaClip"))
            {
                material.SetFloat("_AlphaClip", 1f);
                material.SetFloat("_Cutoff", 0.5f);
                material.EnableKeyword("_ALPHATEST_ON");
            }
            return material;
        }
    }
}
