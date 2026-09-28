using System.Collections.Generic;
using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Buildings
{
    // Classical/Durg age wall-kit pieces (2026-09-28) are flat, pre-rendered
    // isometric billboards rather than a 3D mesh - the delivered art
    // ("classic age wall and gate kit") is a set of ChatGPT-generated
    // isometric renders, not glb/fbx models, so a real mesh pipeline isn't
    // possible for this delivery. Same underlying justification as
    // FarmVisual.cs (the scene's Main Camera is permanently fixed
    // Orthographic, confirmed live at Euler(30,45,0), never rotated by
    // RTSCameraController) - a pre-rendered card reads correctly from that
    // exact, unchanging angle. Unlike Farm's flat ground decal (a field lies
    // on the ground), a wall/gate is a genuinely tall vertical structure, so
    // this uses a Y-axis-only "billboard" instead: the card stands upright
    // (never tilted to match the camera's pitch) and is rotated only around
    // world Y to face the camera's fixed 45-degree yaw - the same technique
    // classic isometric games use for a sprite that must always face a
    // fixed camera. The art's own pitch-driven foreshortening is already
    // baked into the image by whatever renderer produced it.
    public class WallSpriteVisual : MonoBehaviour
    {
        // Confirmed empirically via a live UnityMCP screenshot against the
        // scene's real fixed camera, not derived analytically - see
        // docs/SESSION_LOG.md's matching entry for the exact verification.
        private const float BillboardYawDegrees = 225f;

        private static Mesh _sharedQuad;
        private static readonly Dictionary<(string path, bool darken), Material> MaterialCache = new();

        private MeshRenderer _renderer;
        private Gate _gate;
        private bool _gateResolved;
        private string _closedResourcePath;
        private string _openResourcePath;
        private bool _darken;
        private bool? _lastIsOpen;

        private Gate SiblingGate
        {
            get
            {
                if (!_gateResolved)
                {
                    _gate = GetComponentInParent<Gate>();
                    _gateResolved = true;
                }
                return _gate;
            }
        }

        // Builds the "Visual" child (same name every other building's own
        // visual uses) as a vertical camera-facing card. openResourcePath is
        // null for a plain Wall piece (no state to swap); when non-null this
        // becomes a Gate visual that live-swaps texture based on the
        // sibling Gate's own open/closed NavMeshObstacle state every frame.
        // width/height are the piece's real gameplay Size.x/Size.y (the
        // same box already used for the NavMeshObstacle/BuildingFootprint),
        // so the card exactly fills the footprint it represents.
        public static GameObject Build(GameObject root, string closedResourcePath, string openResourcePath, float width, float height, bool darken)
        {
            var go = new GameObject("Visual");
            go.transform.SetParent(root.transform, false);
            // Setting world .rotation directly (not .localRotation) makes
            // Unity compute whatever local rotation is needed given the
            // parent's current rotation - so this billboard always ends up
            // facing the fixed camera in WORLD space regardless of the
            // root's own placement rotation (a Straight segment chain-
            // dragged at an angle rotates its whole root; junction pieces
            // and Gate are always placed at identity, so this is a no-op
            // for them either way).
            go.transform.rotation = Quaternion.Euler(0f, BillboardYawDegrees, 0f);
            go.transform.localScale = new Vector3(width, height, 1f);

            var meshFilter = go.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = SharedQuad();

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var visual = go.AddComponent<WallSpriteVisual>();
            visual._renderer = renderer;
            visual._closedResourcePath = closedResourcePath;
            visual._openResourcePath = openResourcePath;
            visual._darken = darken;
            visual._lastIsOpen = false;
            renderer.sharedMaterial = MaterialFor(closedResourcePath, darken);
            return go;
        }

        private void Update()
        {
            if (_openResourcePath == null)
            {
                return;
            }

            Gate gate = SiblingGate;
            bool isOpen = gate != null && gate.IsOpen;
            if (_lastIsOpen == isOpen)
            {
                return;
            }

            _lastIsOpen = isOpen;
            _renderer.sharedMaterial = MaterialFor(isOpen ? _openResourcePath : _closedResourcePath, _darken);
        }

        private static Mesh SharedQuad()
        {
            if (_sharedQuad != null)
            {
                return _sharedQuad;
            }

            // A vertical XY-plane quad (unlike FarmVisual's flat XZ ground
            // quad) - localScale.x/y become the card's real world width/
            // height, Z stays a flat 1 (no depth).
            var vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
            };
            var uvs = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
            };
            // Confirmed empirically live: the opposite winding faced away
            // from the fixed camera at the billboard's own yaw (see
            // BillboardYawDegrees) - this order is the one that's actually
            // visible with normal back-face culling on.
            var triangles = new[] { 0, 1, 2, 2, 1, 3 };

            var mesh = new Mesh { name = "WallSpriteVisualQuad" };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            _sharedQuad = mesh;
            return mesh;
        }

        // darken (Durg): a plain 0.7x material-color multiply against the
        // exact same Classical texture - the user's own "30% darker than
        // Classical's grey texture" instruction, applied as a real material
        // tint rather than a second baked-darker image (no separate Durg
        // art exists or is needed, same "just a darker tint, no separate
        // modeling" idea this project's own wall-kit planning memory
        // already anticipated).
        private static Material MaterialFor(string resourcePath, bool darken)
        {
            var key = (resourcePath, darken);
            if (MaterialCache.TryGetValue(key, out Material cached))
            {
                return cached;
            }

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
            Color tint = darken ? new Color(0.7f, 0.7f, 0.7f, 1f) : Color.white;
            material.color = tint;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", tint);
            }

            MaterialCache[key] = material;
            return material;
        }
    }
}
