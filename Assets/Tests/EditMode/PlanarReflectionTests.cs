using NUnit.Framework;
using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Tests
{
    public class PlanarReflectionTests
    {
        // Plane y = waterY, i.e. n = (0,1,0), d = -waterY.
        private static Vector4 WaterPlane(float waterY) => new Vector4(0f, 1f, 0f, -waterY);

        [Test]
        public void ReflectionMatrix_MirrorsPointsAcrossThePlane()
        {
            Matrix4x4 m = PlanarReflection.ReflectionMatrix(WaterPlane(1f));

            Vector3 mirrored = m.MultiplyPoint(new Vector3(2f, 3f, -4f));

            Assert.AreEqual(2f, mirrored.x, 0.0001f);
            Assert.AreEqual(-1f, mirrored.y, 0.0001f); // 3 above the plane at y=1 -> 1 - 2 = -1
            Assert.AreEqual(-4f, mirrored.z, 0.0001f);
        }

        [Test]
        public void ReflectionMatrix_LeavesPointsOnThePlaneUnchanged()
        {
            Matrix4x4 m = PlanarReflection.ReflectionMatrix(WaterPlane(0.25f));

            Vector3 p = new Vector3(7f, 0.25f, 3f);

            Assert.AreEqual(p.y, m.MultiplyPoint(p).y, 0.0001f);
        }

        [Test]
        public void ReflectionMatrix_IsItsOwnInverse()
        {
            Matrix4x4 m = PlanarReflection.ReflectionMatrix(WaterPlane(0.6f));
            Vector3 p = new Vector3(-3f, 5f, 9f);

            Vector3 twice = m.MultiplyPoint(m.MultiplyPoint(p));

            Assert.AreEqual(p.x, twice.x, 0.0001f);
            Assert.AreEqual(p.y, twice.y, 0.0001f);
            Assert.AreEqual(p.z, twice.z, 0.0001f);
        }

        [Test]
        public void ReflectionMatrix_FlipsDirectionsAlongTheNormalOnly()
        {
            Matrix4x4 m = PlanarReflection.ReflectionMatrix(WaterPlane(2f));

            Vector3 d = m.MultiplyVector(new Vector3(1f, 2f, 3f));

            Assert.AreEqual(1f, d.x, 0.0001f);
            Assert.AreEqual(-2f, d.y, 0.0001f);
            Assert.AreEqual(3f, d.z, 0.0001f);
        }
    }
}
