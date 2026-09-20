using System;
using NUnit.Framework;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Tests
{
    public class BakedHeightmapTests
    {
        // 3x3 ramp along +X: 0, 0.5, 1 in every row.
        private static BakedHeightmap Ramp(float scale = 10f)
        {
            var samples = new ushort[9];
            for (int z = 0; z < 3; z++)
            {
                samples[z * 3 + 0] = 0;
                samples[z * 3 + 1] = 32768;
                samples[z * 3 + 2] = 65535;
            }

            return new BakedHeightmap(3, scale, samples);
        }

        [Test]
        public void Bytes_RoundTrip_PreservesResolutionScaleAndSamples()
        {
            var original = Ramp(20f);
            var samples = new ushort[] { 0, 32768, 65535, 0, 32768, 65535, 0, 32768, 65535 };

            BakedHeightmap loaded = BakedHeightmap.FromBytes(BakedHeightmap.ToBytes(3, 20f, samples));

            Assert.AreEqual(original.Resolution, loaded.Resolution);
            Assert.AreEqual(20f, loaded.HeightScale, 0.0001f);
            Assert.AreEqual(original.SampleNormalized(0.5f, 0.5f), loaded.SampleNormalized(0.5f, 0.5f), 0.0001f);
        }

        [Test]
        public void SampleNormalized_HitsCornersAndInterpolates()
        {
            BakedHeightmap map = Ramp();

            Assert.AreEqual(0f, map.SampleNormalized(0f, 0f), 0.0001f);
            Assert.AreEqual(1f, map.SampleNormalized(1f, 0f), 0.0001f);
            Assert.AreEqual(0.5f, map.SampleNormalized(0.5f, 0.7f), 0.001f);
            Assert.AreEqual(0.25f, map.SampleNormalized(0.25f, 0.3f), 0.001f);
        }

        [Test]
        public void SampleNormalized_ClampsOutsideTheMap()
        {
            BakedHeightmap map = Ramp();

            Assert.AreEqual(map.SampleNormalized(1f, 0.5f), map.SampleNormalized(5f, 0.5f), 0.0001f);
            Assert.AreEqual(map.SampleNormalized(0f, 0.5f), map.SampleNormalized(-3f, 0.5f), 0.0001f);
        }

        [Test]
        public void SampleWorld_MapsTheCentredMapAndScalesToMetres()
        {
            BakedHeightmap map = Ramp(10f); // 0 at -X edge, 10 m at +X edge

            Assert.AreEqual(0f, map.SampleWorld(-79f, 0f, 158f), 0.001f);
            Assert.AreEqual(5f, map.SampleWorld(0f, 0f, 158f), 0.01f);
            Assert.AreEqual(10f, map.SampleWorld(79f, 0f, 158f), 0.001f);
        }

        [Test]
        public void SampleWorld_RowsRunAlongZ()
        {
            // Bottom row (z = 0) zeros, top row (z = 1) full.
            var samples = new ushort[] { 0, 0, 65535, 65535 };
            var map = new BakedHeightmap(2, 8f, samples);

            Assert.AreEqual(0f, map.SampleWorld(0f, -79f, 158f), 0.001f);
            Assert.AreEqual(8f, map.SampleWorld(0f, 79f, 158f), 0.001f);
        }

        [Test]
        public void MaxHeight_IsTheHighestSampleInMetres()
        {
            Assert.AreEqual(10f, Ramp(10f).MaxHeight, 0.001f);
        }

        [Test]
        public void FromBytes_RejectsBadMagicTruncatedAndWrongSize()
        {
            var good = BakedHeightmap.ToBytes(2, 1f, new ushort[4]);

            var badMagic = (byte[])good.Clone();
            badMagic[0] = 0;
            Assert.Throws<ArgumentException>(() => BakedHeightmap.FromBytes(badMagic));
            Assert.Throws<ArgumentException>(() => BakedHeightmap.FromBytes(new byte[5]));

            var truncated = new byte[good.Length - 2];
            Array.Copy(good, truncated, truncated.Length);
            Assert.Throws<ArgumentException>(() => BakedHeightmap.FromBytes(truncated));
            Assert.Throws<ArgumentException>(() => BakedHeightmap.FromBytes(null));
        }

        [Test]
        public void Constructor_RejectsSampleCountMismatch()
        {
            Assert.Throws<ArgumentException>(() => new BakedHeightmap(3, 1f, new ushort[5]));
            Assert.Throws<ArgumentException>(() => new BakedHeightmap(1, 1f, new ushort[1]));
        }
    }
}
