using System;
using UnityEngine;

namespace KingdomsOfBharat.Core
{
    // A terrain heightmap baked offline (e.g. out of a Vista graph) and loaded
    // by ProceduralTerrain in place of its Perlin ground. Baking in the editor
    // rather than generating at match time keeps every LAN peer on identical
    // ground (lockstep determinism), avoids loading time and removes any
    // runtime dependency on the authoring tool.
    //
    // Binary format (little endian): "KBHM", int32 version (1), int32
    // resolution N, float32 heightScale (world metres at 1.0), then N x N
    // uint16 heights, row-major with rows running along +Z and columns along
    // +X (index = z * N + x). The map is centred on the world origin.
    public sealed class BakedHeightmap
    {
        public const int Version = 1;
        private const int HeaderBytes = 16;
        private static readonly byte[] Magic = { (byte)'K', (byte)'B', (byte)'H', (byte)'M' };

        private readonly ushort[] _samples;

        public int Resolution { get; }
        // World-space height (metres) of a normalized height of 1.0.
        public float HeightScale { get; }
        // Highest baked point in world metres.
        public float MaxHeight { get; }

        public BakedHeightmap(int resolution, float heightScale, ushort[] samples)
        {
            if (resolution < 2)
            {
                throw new ArgumentException("Resolution must be at least 2.", nameof(resolution));
            }

            if (samples == null || samples.Length != resolution * resolution)
            {
                throw new ArgumentException("Sample count must equal resolution squared.", nameof(samples));
            }

            Resolution = resolution;
            HeightScale = heightScale;
            _samples = samples;

            ushort max = 0;
            foreach (ushort s in samples)
            {
                if (s > max)
                {
                    max = s;
                }
            }

            MaxHeight = max / 65535f * heightScale;
        }

        public static byte[] ToBytes(int resolution, float heightScale, ushort[] samples)
        {
            var bytes = new byte[HeaderBytes + samples.Length * 2];
            Buffer.BlockCopy(Magic, 0, bytes, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(Version), 0, bytes, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(resolution), 0, bytes, 8, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(heightScale), 0, bytes, 12, 4);
            Buffer.BlockCopy(samples, 0, bytes, HeaderBytes, samples.Length * 2);
            return bytes;
        }

        public static BakedHeightmap FromBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length < HeaderBytes)
            {
                throw new ArgumentException("Data is too short to be a baked heightmap.");
            }

            for (int i = 0; i < 4; i++)
            {
                if (bytes[i] != Magic[i])
                {
                    throw new ArgumentException("Not a baked heightmap (bad magic).");
                }
            }

            int version = BitConverter.ToInt32(bytes, 4);
            if (version != Version)
            {
                throw new ArgumentException("Unsupported baked heightmap version " + version + ".");
            }

            int resolution = BitConverter.ToInt32(bytes, 8);
            float scale = BitConverter.ToSingle(bytes, 12);
            if (resolution < 2 || bytes.Length != HeaderBytes + resolution * resolution * 2)
            {
                throw new ArgumentException("Baked heightmap size doesn't match its resolution.");
            }

            var samples = new ushort[resolution * resolution];
            Buffer.BlockCopy(bytes, HeaderBytes, samples, 0, samples.Length * 2);
            return new BakedHeightmap(resolution, scale, samples);
        }

        // Loads "<resourcePath>" (a .bytes TextAsset under a Resources folder,
        // path without extension). Null if the resource is missing or invalid.
        public static BakedHeightmap LoadResource(string resourcePath)
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                return null;
            }

            try
            {
                return FromBytes(asset.bytes);
            }
            catch (ArgumentException e)
            {
                Debug.LogError("BakedHeightmap '" + resourcePath + "' is invalid: " + e.Message);
                return null;
            }
        }

        // u, v in 0..1 across the map along +X and +Z; bilinear, 0..1 normalized height.
        public float SampleNormalized(float u, float v)
        {
            float fx = Mathf.Clamp01(u) * (Resolution - 1);
            float fz = Mathf.Clamp01(v) * (Resolution - 1);
            int x0 = Mathf.Min((int)fx, Resolution - 2);
            int z0 = Mathf.Min((int)fz, Resolution - 2);
            float tx = fx - x0;
            float tz = fz - z0;

            float h00 = _samples[z0 * Resolution + x0];
            float h10 = _samples[z0 * Resolution + x0 + 1];
            float h01 = _samples[(z0 + 1) * Resolution + x0];
            float h11 = _samples[(z0 + 1) * Resolution + x0 + 1];

            float h = Mathf.Lerp(Mathf.Lerp(h00, h10, tx), Mathf.Lerp(h01, h11, tx), tz);
            return h / 65535f;
        }

        // World height (metres) at a world X/Z for a map of `mapSize` centred on the origin.
        public float SampleWorld(float worldX, float worldZ, float mapSize)
        {
            float half = mapSize * 0.5f;
            return SampleNormalized((worldX + half) / mapSize, (worldZ + half) / mapSize) * HeightScale;
        }
    }
}
