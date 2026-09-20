using System.Collections.Generic;
using UnityEngine;

namespace KingdomsOfBharat.Core
{
    // Draws the terrain's ground clutter (see TerrainClutter) with
    // Graphics.RenderMeshInstanced, one call per visible patch per clutter
    // type. Unity's own Terrain detail system didn't draw mesh details in this
    // URP setup, and this route gives full control: patches are frustum- and
    // distance-culled here, nothing has colliders (so no NavMesh/gameplay
    // impact) and it is purely visual.
    public class TerrainClutterRenderer : MonoBehaviour
    {
        private const int MaxPerCall = 1023;

        // The "Clutter" layer if the project defines it (falls back to Default).
        public static int ClutterLayer
        {
            get
            {
                int layer = LayerMask.NameToLayer("Clutter");
                return layer >= 0 ? layer : 0;
            }
        }
        // Beyond this distance, thinned batches draw half their instances.
        public const float ThinDistance = 38f;

        private class Batch
        {
            public Mesh Mesh;
            public RenderParams Params;
            public Matrix4x4[][] Chunks;
            // Every other instance, drawn instead of Chunks beyond ThinDistance
            // (null when the batch isn't thinned).
            public Matrix4x4[][] ThinChunks;
            public Bounds Bounds;
        }

        private readonly List<Batch> _batches = new List<Batch>();
        private readonly Plane[] _planes = new Plane[6];
        private float _drawDistance = 70f;

        public int InstanceCount { get; private set; }
        public int BatchCount => _batches.Count;
        // Instances submitted for drawing in the last Update (after culling and thinning).
        public int DrawnInstancesLastFrame { get; private set; }

        public void Clear()
        {
            _batches.Clear();
            InstanceCount = 0;
        }

        public void SetDrawDistance(float distance)
        {
            _drawDistance = distance;
        }

        public void AddBatch(Mesh mesh, Material material, List<Matrix4x4> matrices, Bounds bounds, bool thinWithDistance = false)
        {
            if (mesh == null || material == null || matrices.Count == 0)
            {
                return;
            }

            Matrix4x4[][] chunks = Chunk(matrices);
            Matrix4x4[][] thin = null;
            if (thinWithDistance && matrices.Count > 1)
            {
                var half = new List<Matrix4x4>(matrices.Count / 2 + 1);
                for (int i = 0; i < matrices.Count; i += 2)
                {
                    half.Add(matrices[i]);
                }

                thin = Chunk(half);
            }

            _batches.Add(new Batch
            {
                Mesh = mesh,
                Params = new RenderParams(material)
                {
                    shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                    receiveShadows = true,
                    worldBounds = bounds,
                    // Own layer so the water's reflection camera can skip it.
                    layer = ClutterLayer,
                },
                Chunks = chunks,
                ThinChunks = thin,
                Bounds = bounds,
            });
            InstanceCount += matrices.Count;
        }

        private static Matrix4x4[][] Chunk(List<Matrix4x4> matrices)
        {
            var chunks = new List<Matrix4x4[]>();
            for (int i = 0; i < matrices.Count; i += MaxPerCall)
            {
                int n = Mathf.Min(MaxPerCall, matrices.Count - i);
                var chunk = new Matrix4x4[n];
                matrices.CopyTo(i, chunk, 0, n);
                chunks.Add(chunk);
            }

            return chunks.ToArray();
        }

        private void Update()
        {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null || _batches.Count == 0)
            {
                DrawnInstancesLastFrame = 0;
                return;
            }

            GeometryUtility.CalculateFrustumPlanes(cam, _planes);
            Vector3 camPos = cam.transform.position;
            float maxSqr = _drawDistance * _drawDistance;

            float thinSqr = ThinDistance * ThinDistance;
            int drawn = 0;
            foreach (Batch batch in _batches)
            {
                float sqr = batch.Bounds.SqrDistance(camPos);
                if (sqr > maxSqr || !GeometryUtility.TestPlanesAABB(_planes, batch.Bounds))
                {
                    continue;
                }

                Matrix4x4[][] draw = batch.ThinChunks != null && sqr > thinSqr ? batch.ThinChunks : batch.Chunks;
                foreach (Matrix4x4[] chunk in draw)
                {
                    Graphics.RenderMeshInstanced(batch.Params, batch.Mesh, 0, chunk);
                    drawn += chunk.Length;
                }
            }

            DrawnInstancesLastFrame = drawn;
        }
    }
}
