using System.Collections.Generic;
using UnityEngine;

namespace KingdomsOfBharat.FogOfWar
{
    // Marker + radius for anything that reveals fog around itself. Attached
    // by spawners to Player-faction units/buildings. FogOfWarManager scans
    // All each recompute tick instead of scanning the scene.
    public class VisionSource : MonoBehaviour
    {
        // AoE II reference spec: line-of-sight is hard-capped at 20 tiles
        // regardless of what a unit/building/tech bonus would otherwise
        // grant.
        public const float HardCap = 20f;

        [Range(1f, HardCap)]
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
            visionRadius = Mathf.Clamp(radius, 1f, HardCap);
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
