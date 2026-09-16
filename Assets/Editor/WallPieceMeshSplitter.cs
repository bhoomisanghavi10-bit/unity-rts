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
        private static Mesh ClipByPlane(Mesh source, Vector3 planePoint, Vector3 planeNormal, bool keepPositiveSide)
        {
            Vector3[] verts = source.vertices;
            Vector3[] normals = source.normals;
            int[] tris = source.triangles;

            var outVerts = new List<Vector3>();
            var outNormals = new List<Vector3>();
            var outTris = new List<int>();

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
                }
            }

            Mesh result = new Mesh();
            result.SetVertices(outVerts);
            result.SetNormals(outNormals);
            result.SetTriangles(outTris, 0);
            return result;
        }
    }
}
