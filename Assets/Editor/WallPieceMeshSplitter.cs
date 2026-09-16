using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace KingdomsOfBharat.Editor
{
    /// <summary>
    /// Splits a single fused wall-corner mesh (e.g. a raw Meshy AI export combining a
    /// corner pillar/bastion with two straight wall arms in one mesh) into separate,
    /// independently-reusable Mesh assets: a Pillar piece plus one Wall Arm piece per
    /// side - matching AoE's own real "Pillar + Plank" wall-tiling convention (see the
    /// user's own reference research at the start of the wall-system epic).
    ///
    /// Unlike a naive per-vertex/per-triangle bucketing pass (majority-vote by which
    /// side of a cut plane a triangle's vertices fall on), this does real geometric
    /// plane-clipping - cutting straddling triangles exactly at the seam plane and
    /// generating new interpolated vertices along the cut, rather than assigning a
    /// whole triangle (however large) to whichever side "wins" a 2-of-3 vote. That
    /// naive approach was tried first on the corner test delivery
    /// (Wall_Durg_Corner.glb, a very low-poly ~920-tri block-out with large,
    /// sparsely-subdivided flat faces) and produced long jagged spike artifacts
    /// reaching across the whole model, confirmed via screenshot - a few giant
    /// triangles spanned from deep in one arm across the intended seam, and no
    /// threshold choice could avoid misclassifying them wholesale. Real geometric
    /// clipping is immune to that: it cuts exactly at the plane regardless of how
    /// large or sparse the source triangles are.
    /// </summary>
    public static class WallPieceMeshSplitter
    {
        // Splits sourceMesh by two axis-aligned world-space planes (a corner sits at
        // the intersection of two perpendicular wall arms, each running along either
        // the local X or Z axis - see the Wall Corner concept art / ComputeWallChain's
        // own convention that a Wall segment's long axis is local +X).
        //
        // pillarMinX/pillarMaxZ define the pillar's own square footprint corner - the
        // pillar keeps everything with x >= pillarMinX AND z <= pillarMaxZ; armA (the
        // arm running along -X from the pillar) keeps z <= pillarMaxZ AND x <
        // pillarMinX; armB (the arm running along +Z from the pillar) keeps x >=
        // pillarMinX AND z > pillarMaxZ.
        public static void SplitCorner(string sourceMeshResourcePath, float pillarMinX, float pillarMaxZ, string destFolder, string baseName)
        {
            GameObject prefab = Resources.Load<GameObject>(sourceMeshResourcePath);
            if (prefab == null)
            {
                Debug.LogError($"WallPieceMeshSplitter: could not load {sourceMeshResourcePath}");
                return;
            }

            MeshFilter mf = prefab.GetComponentInChildren<MeshFilter>(true);
            if (mf == null || mf.sharedMesh == null)
            {
                Debug.LogError($"WallPieceMeshSplitter: no mesh found under {sourceMeshResourcePath}");
                return;
            }

            Mesh source = mf.sharedMesh;

            // Clip twice: first isolate everything on the pillar's +X side, then
            // within that, everything on the pillar's -Z side - giving the pillar's
            // square footprint as an intersection of two half-space clips. The
            // complementary halves become the two arms.
            Mesh xHigh, xLow;
            ClipByPlaneX(source, pillarMinX, out xHigh, out xLow);

            Mesh pillar, armB;
            ClipByPlaneZ(xHigh, pillarMaxZ, keepBelow: true, above: out armB, below: out pillar);

            Mesh armA = xLow; // everything on the -X side is armA in full (no further clip needed)

            Directory_CreateIfNeeded(destFolder);
            SaveMesh(pillar, destFolder, baseName + "_Pillar");
            SaveMesh(armA, destFolder, baseName + "_ArmA");
            SaveMesh(armB, destFolder, baseName + "_ArmB");

            Debug.Log($"WallPieceMeshSplitter: split {sourceMeshResourcePath} -> " +
                $"Pillar({pillar.vertexCount}v/{pillar.triangles.Length / 3}t), " +
                $"ArmA({armA.vertexCount}v/{armA.triangles.Length / 3}t), " +
                $"ArmB({armB.vertexCount}v/{armB.triangles.Length / 3}t)");
        }

        private static void Directory_CreateIfNeeded(string folder)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetFullPath(folder));
        }

        private static void SaveMesh(Mesh mesh, string folder, string name)
        {
            mesh.name = name;
            mesh.RecalculateBounds();
            string path = $"{folder}/{name}_split.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(mesh, existing);
                AssetDatabase.SaveAssets();
            }
            else
            {
                AssetDatabase.CreateAsset(mesh, path);
                AssetDatabase.SaveAssets();
            }
        }

        private static void ClipByPlaneX(Mesh source, float planeX, out Mesh keepHighX, out Mesh keepLowX)
        {
            keepHighX = ClipByPlane(source, new Vector3(planeX, 0f, 0f), Vector3.right, keepPositiveSide: true);
            keepLowX = ClipByPlane(source, new Vector3(planeX, 0f, 0f), Vector3.right, keepPositiveSide: false);
        }

        private static void ClipByPlaneZ(Mesh source, float planeZ, bool keepBelow, out Mesh above, out Mesh below)
        {
            above = ClipByPlane(source, new Vector3(0f, 0f, planeZ), Vector3.forward, keepPositiveSide: true);
            below = ClipByPlane(source, new Vector3(0f, 0f, planeZ), Vector3.forward, keepPositiveSide: false);
        }

        // Sutherland-Hodgman-style per-triangle plane clip. planeNormal points toward
        // the "positive" side being tested. Every triangle is classified by how many
        // of its 3 vertices lie on the kept side:
        //   0 kept -> triangle fully discarded
        //   3 kept -> triangle kept unchanged
        //   1 kept -> clipped down to one smaller triangle (2 new interpolated verts)
        //   2 kept -> clipped down to a quad, emitted as 2 triangles (2 new interpolated verts)
        //
        // The cut always leaves a hole where material was removed - every triangle that
        // straddles the plane contributes exactly one boundary edge lying exactly on the
        // plane. Those edges are collected and, at the end, chained into closed loops
        // (one per disjoint cross-section - e.g. one loop per stake pole, since each is
        // a separate small solid) and each loop is flat-triangulated to seal the hole.
        // Without this, a curved decorative element straddling the cut (e.g. a rope
        // lashing wrapped around a post) leaves its own interior tube wall exposed
        // through the new opening, which reads as jagged, sliver-shaped "spike" geometry
        // from odd angles even though the clip itself is geometrically exact - found and
        // confirmed via live screenshot on the Ancient-tier corner's Pillar piece.
        private static Mesh ClipByPlane(Mesh source, Vector3 planePoint, Vector3 planeNormal, bool keepPositiveSide)
        {
            Vector3[] verts = source.vertices;
            Vector3[] normals = source.normals;
            int[] tris = source.triangles;

            var outVerts = new List<Vector3>();
            var outNormals = new List<Vector3>();
            var outTris = new List<int>();
            var boundaryEdges = new List<(Vector3 from, Vector3 to)>();

            float Side(Vector3 v)
            {
                float d = Vector3.Dot(v - planePoint, planeNormal);
                return keepPositiveSide ? d : -d;
            }

            int AddVertex(Vector3 pos, Vector3 normal)
            {
                outVerts.Add(pos);
                outNormals.Add(normal);
                return outVerts.Count - 1;
            }

            // Interpolates a new vertex/normal at the exact point the edge (a -> b)
            // crosses the plane (side value passes through zero).
            void Interpolate(Vector3 aPos, Vector3 aNormal, float aSide, Vector3 bPos, Vector3 bNormal, float bSide, out Vector3 pos, out Vector3 normal)
            {
                float t = aSide / (aSide - bSide);
                pos = Vector3.Lerp(aPos, bPos, t);
                normal = Vector3.Slerp(aNormal, bNormal, t).normalized;
            }

            for (int t = 0; t < tris.Length; t += 3)
            {
                int ia = tris[t], ib = tris[t + 1], ic = tris[t + 2];
                Vector3 pa = verts[ia], pb = verts[ib], pc = verts[ic];
                Vector3 na = normals[ia], nb = normals[ib], nc = normals[ic];
                float sa = Side(pa), sb = Side(pb), sc = Side(pc);
                bool ka = sa >= 0f, kb = sb >= 0f, kc = sc >= 0f;
                int keptCount = (ka ? 1 : 0) + (kb ? 1 : 0) + (kc ? 1 : 0);

                if (keptCount == 0)
                {
                    continue;
                }
                if (keptCount == 3)
                {
                    int a0 = AddVertex(pa, na), b0 = AddVertex(pb, nb), c0 = AddVertex(pc, nc);
                    outTris.Add(a0); outTris.Add(b0); outTris.Add(c0);
                    continue;
                }

                // Rotate (pa,pb,pc) so the "lone" vertex (the one on the minority
                // side) is always first - collapses the 1-kept and 2-kept cases into
                // one shared code path per orientation.
                Vector3[] p = { pa, pb, pc };
                Vector3[] n = { na, nb, nc };
                float[] s = { sa, sb, sc };
                bool[] k = { ka, kb, kc };

                int lone = -1;
                for (int i = 0; i < 3; i++)
                {
                    bool isMinority = keptCount == 1 ? k[i] : !k[i];
                    if (isMinority) { lone = i; break; }
                }
                int i1 = (lone + 1) % 3;
                int i2 = (lone + 2) % 3;

                Vector3 loneEdge1Pos, loneEdge1Normal, loneEdge2Pos, loneEdge2Normal;
                Interpolate(p[lone], n[lone], s[lone], p[i1], n[i1], s[i1], out loneEdge1Pos, out loneEdge1Normal);
                Interpolate(p[lone], n[lone], s[lone], p[i2], n[i2], s[i2], out loneEdge2Pos, out loneEdge2Normal);

                if (keptCount == 1)
                {
                    // Only the lone vertex survives - one smaller triangle.
                    int v0 = AddVertex(p[lone], n[lone]);
                    int v1 = AddVertex(loneEdge1Pos, loneEdge1Normal);
                    int v2 = AddVertex(loneEdge2Pos, loneEdge2Normal);
                    outTris.Add(v0); outTris.Add(v1); outTris.Add(v2);
                    boundaryEdges.Add((loneEdge1Pos, loneEdge2Pos));
                }
                else
                {
                    // The two non-lone vertices survive - a quad, split into 2 triangles.
                    int v0 = AddVertex(p[i1], n[i1]);
                    int v1 = AddVertex(p[i2], n[i2]);
                    int v2 = AddVertex(loneEdge2Pos, loneEdge2Normal);
                    int v3 = AddVertex(loneEdge1Pos, loneEdge1Normal);
                    outTris.Add(v0); outTris.Add(v1); outTris.Add(v2);
                    outTris.Add(v0); outTris.Add(v2); outTris.Add(v3);
                    boundaryEdges.Add((loneEdge2Pos, loneEdge1Pos));
                }
            }

            Vector3 capNormal = keepPositiveSide ? -planeNormal : planeNormal;
            CapBoundary(boundaryEdges, planeNormal, capNormal, outVerts, outNormals, outTris);

            Mesh result = new Mesh();
            result.SetVertices(outVerts);
            result.SetNormals(outNormals);
            result.SetTriangles(outTris, 0);
            return result;
        }

        private const float LoopMatchEpsilon = 1e-4f;

        // Chains directed boundary-edge segments (each exactly on the cut plane) into
        // closed loops, then flat-triangulates and appends each loop as new cap
        // geometry facing capNormal. planeAxis is the raw plane normal (Vector3.right
        // or Vector3.forward) used only to pick which two coordinates to project onto
        // for 2D triangulation - the cut plane is always axis-aligned.
        private static void CapBoundary(List<(Vector3 from, Vector3 to)> edges, Vector3 planeAxis, Vector3 capNormal, List<Vector3> outVerts, List<Vector3> outNormals, List<int> outTris)
        {
            if (edges.Count == 0) return;

            List<List<Vector3>> loops = BuildLoops(edges);
            bool axisIsX = Mathf.Abs(planeAxis.x) > Mathf.Abs(planeAxis.z);

            foreach (List<Vector3> rawLoop in loops)
            {
                List<Vector3> pts = DedupeLoop(rawLoop);
                if (pts.Count < 3) continue;

                var poly2D = new List<Vector2>(pts.Count);
                foreach (Vector3 p in pts)
                    poly2D.Add(axisIsX ? new Vector2(p.y, p.z) : new Vector2(p.x, p.y));

                List<int> earTris = EarClipTriangulate(poly2D);
                if (earTris.Count == 0) continue;

                int baseIndex = outVerts.Count;
                foreach (Vector3 p in pts)
                {
                    outVerts.Add(p);
                    outNormals.Add(capNormal);
                }

                for (int i = 0; i < earTris.Count; i += 3)
                {
                    int ia = earTris[i], ib = earTris[i + 1], ic = earTris[i + 2];
                    Vector3 a = pts[ia], b = pts[ib], c = pts[ic];
                    Vector3 n = Vector3.Cross(b - a, c - a);
                    // Ear-clipping's own winding is only consistent per-loop, not
                    // guaranteed to match capNormal's direction - checked and, if
                    // needed, flipped per triangle rather than assuming the whole
                    // loop shares one orientation.
                    if (Vector3.Dot(n, capNormal) < 0f)
                    {
                        outTris.Add(baseIndex + ia); outTris.Add(baseIndex + ic); outTris.Add(baseIndex + ib);
                    }
                    else
                    {
                        outTris.Add(baseIndex + ia); outTris.Add(baseIndex + ib); outTris.Add(baseIndex + ic);
                    }
                }
            }
        }

        // A watertight mesh's intersection with a plane is a set of disjoint closed
        // loops (one per separate solid crossing the plane - e.g. one per stake pole).
        // Greedily chains directed edges head-to-tail by matching endpoint positions.
        // An edge with no match (a non-manifold source mesh, or a loop that doesn't
        // close cleanly) simply ends that loop early rather than throwing - a partial
        // cap is still strictly better than the open hole this replaces.
        private static List<List<Vector3>> BuildLoops(List<(Vector3 from, Vector3 to)> edges)
        {
            var remaining = new List<(Vector3 from, Vector3 to)>(edges);
            var loops = new List<List<Vector3>>();

            while (remaining.Count > 0)
            {
                var loop = new List<Vector3>();
                (Vector3 from, Vector3 to) edge = remaining[0];
                remaining.RemoveAt(0);
                Vector3 start = edge.from;
                Vector3 current = edge.to;
                loop.Add(start);
                loop.Add(current);

                int guard = remaining.Count + 1;
                while (Vector3.Distance(current, start) > LoopMatchEpsilon && guard-- > 0)
                {
                    int idx = remaining.FindIndex(e => Vector3.Distance(e.from, current) < LoopMatchEpsilon);
                    if (idx < 0) break;
                    current = remaining[idx].to;
                    remaining.RemoveAt(idx);
                    loop.Add(current);
                }

                loops.Add(loop);
            }

            return loops;
        }

        // Removes consecutive (and the closing) near-duplicate points a chained loop
        // accumulates at its shared start/end vertex and at any collinear noise.
        private static List<Vector3> DedupeLoop(List<Vector3> loop)
        {
            var result = new List<Vector3>();
            foreach (Vector3 p in loop)
            {
                if (result.Count == 0 || Vector3.Distance(result[result.Count - 1], p) > LoopMatchEpsilon)
                    result.Add(p);
            }
            if (result.Count > 1 && Vector3.Distance(result[0], result[result.Count - 1]) <= LoopMatchEpsilon)
                result.RemoveAt(result.Count - 1);
            return result;
        }

        // Standard O(n^2) ear-clipping triangulation for a simple (non-self-
        // intersecting) 2D polygon. Good enough for the small, near-convex loops a
        // stake/post cross-section produces. Normalizes to CCW winding first so the
        // convexity/point-in-triangle tests below have a consistent sign convention;
        // gives up gracefully (returns whatever ears were already found) rather than
        // looping forever on a degenerate input.
        private static List<int> EarClipTriangulate(List<Vector2> poly)
        {
            var result = new List<int>();
            if (poly.Count < 3) return result;

            var order = new List<int>(poly.Count);
            for (int i = 0; i < poly.Count; i++) order.Add(i);
            if (SignedArea2D(poly, order) < 0f) order.Reverse();

            int guard = poly.Count * poly.Count + 8;
            while (order.Count > 3 && guard-- > 0)
            {
                bool earFound = false;
                for (int i = 0; i < order.Count; i++)
                {
                    int iPrev = order[(i - 1 + order.Count) % order.Count];
                    int iCurr = order[i];
                    int iNext = order[(i + 1) % order.Count];
                    Vector2 a = poly[iPrev], b = poly[iCurr], c = poly[iNext];
                    if (Cross2D(b - a, c - b) <= 0f) continue; // reflex vertex, can't be an ear

                    bool anyInside = false;
                    for (int j = 0; j < order.Count; j++)
                    {
                        int vi = order[j];
                        if (vi == iPrev || vi == iCurr || vi == iNext) continue;
                        if (PointInTriangle(poly[vi], a, b, c)) { anyInside = true; break; }
                    }
                    if (anyInside) continue;

                    result.Add(iPrev); result.Add(iCurr); result.Add(iNext);
                    order.RemoveAt(i);
                    earFound = true;
                    break;
                }
                if (!earFound) break;
            }
            if (order.Count == 3)
            {
                result.Add(order[0]); result.Add(order[1]); result.Add(order[2]);
            }
            return result;
        }

        private static float Cross2D(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        private static float SignedArea2D(List<Vector2> poly, List<int> order)
        {
            float area = 0f;
            for (int i = 0; i < order.Count; i++)
            {
                Vector2 p0 = poly[order[i]];
                Vector2 p1 = poly[order[(i + 1) % order.Count]];
                area += p0.x * p1.y - p1.x * p0.y;
            }
            return area * 0.5f;
        }

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Cross2D(b - a, p - a);
            float d2 = Cross2D(c - b, p - b);
            float d3 = Cross2D(a - c, p - c);
            bool hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
            bool hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(hasNeg && hasPos);
        }
    }
}
