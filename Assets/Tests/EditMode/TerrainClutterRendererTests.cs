using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Tests
{
    public class TerrainClutterRendererTests
    {
        private GameObject _go;
        private Mesh _mesh;
        private Material _material;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("ClutterRendererTest");
            _mesh = new Mesh();
            _material = new Material(Shader.Find("Hidden/InternalErrorShader") ?? Shader.Find("Sprites/Default"));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_mesh);
            Object.DestroyImmediate(_material);
        }

        private static List<Matrix4x4> Matrices(int n)
        {
            var list = new List<Matrix4x4>();
            for (int i = 0; i < n; i++)
            {
                list.Add(Matrix4x4.TRS(new Vector3(i, 0f, 0f), Quaternion.identity, Vector3.one));
            }

            return list;
        }

        [Test]
        public void AddBatch_CountsInstancesAndBatches()
        {
            var r = _go.AddComponent<TerrainClutterRenderer>();
            r.AddBatch(_mesh, _material, Matrices(10), new Bounds(Vector3.zero, Vector3.one * 20f));

            Assert.AreEqual(10, r.InstanceCount);
            Assert.AreEqual(1, r.BatchCount);
        }

        [Test]
        public void AddBatch_ThinnedBatchStillReportsAllInstances()
        {
            var r = _go.AddComponent<TerrainClutterRenderer>();
            r.AddBatch(_mesh, _material, Matrices(2500), new Bounds(Vector3.zero, Vector3.one * 20f), thinWithDistance: true);

            Assert.AreEqual(2500, r.InstanceCount);
            Assert.AreEqual(1, r.BatchCount);
        }

        [Test]
        public void AddBatch_EmptyOrMissingInputsAreIgnored()
        {
            var r = _go.AddComponent<TerrainClutterRenderer>();
            r.AddBatch(_mesh, _material, new List<Matrix4x4>(), new Bounds());
            r.AddBatch(null, _material, Matrices(3), new Bounds());
            r.AddBatch(_mesh, null, Matrices(3), new Bounds());

            Assert.AreEqual(0, r.BatchCount);
            Assert.AreEqual(0, r.InstanceCount);
        }

        [Test]
        public void Clear_RemovesEverything()
        {
            var r = _go.AddComponent<TerrainClutterRenderer>();
            r.AddBatch(_mesh, _material, Matrices(5), new Bounds(Vector3.zero, Vector3.one));
            r.Clear();

            Assert.AreEqual(0, r.BatchCount);
            Assert.AreEqual(0, r.InstanceCount);
        }
    }
}
