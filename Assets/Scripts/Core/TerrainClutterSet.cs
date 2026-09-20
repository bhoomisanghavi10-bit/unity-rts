using System;
using UnityEngine;

namespace KingdomsOfBharat.Core
{
    // Data for the terrain ground-clutter scatter (grass tufts, small rocks,
    // shoreline stones - see TerrainClutter). Lives in Resources so the
    // prefabs it references (which may sit anywhere, e.g. the PolishedSurfaces
    // rock set) are pulled into a build. Built by the editor menu
    // "BharatRTS/Build Terrain Clutter Set" (Assets/Editor/
    // TerrainClutterBuilder.cs) - edit the entries in the Inspector to
    // retune sizes/density.
    [CreateAssetMenu(menuName = "BharatRTS/Terrain Clutter Set")]
    public class TerrainClutterSet : ScriptableObject
    {
        public enum ClutterKind
        {
            // Dense, follows the grass/dirt weights, patchy.
            Grass,
            // Sparse, scattered on grass/dirt.
            Rock,
            // Follows the wet pebble band weight along the waterline.
            Pebble,
        }

        [Serializable]
        public class Entry
        {
            public string name;
            // Prefab with a MeshFilter + MeshRenderer whose material has GPU
            // instancing enabled (terrain detail meshes are instanced).
            public GameObject prefab;
            // Optional: if non-empty, the scatter picks between these per
            // instance instead of always using `prefab` (each needs one
            // MeshFilter + MeshRenderer, same as `prefab`).
            public GameObject[] variants;
            public ClutterKind kind;
            public float minScale = 0.8f;
            public float maxScale = 1.2f;
            // Grass: max instances per detail cell. Rock/Pebble: per-cell
            // chance scale (see TerrainClutter).
            public float density = 4f;
        }

        public Entry[] entries = new Entry[0];
    }
}
