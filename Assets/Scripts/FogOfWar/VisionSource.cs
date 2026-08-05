using System.Collections.Generic;
using UnityEngine;

namespace KingdomsOfBharat.FogOfWar
{
    // Marker + radius for anything that reveals fog around itself. Attached
    // by spawners to Player-faction units/buildings. FogOfWarManager scans
    // All each recompute tick instead of scanning the scene.
    public class VisionSource : MonoBehaviour
    {
        [SerializeField] private float visionRadius = 8f;

        public static readonly List<VisionSource> All = new List<VisionSource>();

        public float VisionRadius => visionRadius;

        public void Configure(float radius)
        {
            visionRadius = radius;
        }

        private void OnEnable()
        {
            All.Add(this);
        }

        private void OnDisable()
        {
            All.Remove(this);
        }
    }
}
