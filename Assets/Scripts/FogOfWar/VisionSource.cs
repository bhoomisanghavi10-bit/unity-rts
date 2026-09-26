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

        // Factories attach a VisionSource only when the fog needs it: the
        // single-player Player, or every faction in a LAN match (each peer's
        // local faction is then filtered by FogOfWarManager).
        public static bool IsTracked(Core.FactionId faction)
        {
            return faction == Core.FactionId.Player || Multiplayer.NetworkMatch.IsActive;
        }

        private Core.FactionMember _member;

        // The faction this source belongs to (Player if untagged, matching
        // the old single-faction assumption).
        public Core.FactionId Faction
        {
            get
            {
                if (_member == null)
                {
                    TryGetComponent(out _member);
                }

                return _member != null ? _member.Faction : Core.FactionId.Player;
            }
        }

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
