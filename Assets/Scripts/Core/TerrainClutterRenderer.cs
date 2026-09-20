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

        private class Batch
        {
            public Mesh Mesh;
            public RenderParams Params;
            public Matrix4x4[][] Chunks;
            public Bounds Bounds;
        }

        private readonly List<Batch> _batches = new List<Batch>();
        private readonly Plane[] _planes = new Plane[6];
        private float _drawDistance = 70f;

        public int InstanceCount { get; private set; }
        public int BatchCount => _batches.Count;

        public void Clear()
        {
            _batches.Clear();
            InstanceCount = 0;
        }

        public void SetDrawDistance(float distance)
        {
            _drawDistance = distance;
        }

        public void AddBatch(Mesh mesh, Material material, List<Matrix4x4> matrices, Bounds bounds)
        {
            if (mesh == null || material == null || matrices.Count == 0)
            {
                return;
            }

            var chunks = new List<Matrix4x4[]>();
            for (int i = 0; i < matrices.Count; i += MaxPerCall)
            {
                int n = Mathf.Min(MaxPerCall, matrices.Count - i);
                var chunk = new Matrix4x4[n];
                matrices.CopyTo(i, chunk, 0, n);
                chunks.Add(chunk);
            }

            _batches.Add(new Batch
            {
                Mesh = mesh,
                Params = new RenderParams(material)
                {
                    shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                    receiveShadows = true,
                    worldBounds = bounds,
                },
                Chunks = chunks.ToArray(),
                Bounds = bounds,
            });
            InstanceCount += matrices.Count;
        }

        private void Update()
        {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null || _batches.Count == 0)
            {
                return;
            }

            GeometryUtility.CalculateFrustumPlanes(cam, _planes);
            Vector3 camPos = cam.transform.position;
            float maxSqr = _drawDistance * _drawDistance;

            foreach (Batch batch in _batches)
            {
                if (batch.Bounds.SqrDistance(camPos) > maxSqr || !GeometryUtility.TestPlanesAABB(_planes, batch.Bounds))
                {
                    continue;
                }

                foreach (Matrix4x4[] chunk in batch.Chunks)
                {
                    Graphics.RenderMeshInstanced(batch.Params, batch.Mesh, 0, chunk);
                }
            }
        }
    }
}
