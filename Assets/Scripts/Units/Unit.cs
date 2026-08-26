using System.Collections.Generic;
using UnityEngine;

namespace KingdomsOfBharat.Units
{
    // Identity marker for anything the player can select and command.
    // Maintains a registry of all live units so systems like box-select
    // don't need per-frame scene scans.
    //
    // Deliberately NOT [RequireComponent(typeof(NavMeshAgent))] - every
    // land factory (Soldier/Worker/Archer/Cavalry/Siege/Spearman/unique
    // units) already adds its own NavMeshAgent before adding Unit, so
    // that requirement was always redundant for them. But FishingBoat/
    // WarGalley never add one at all - they move via WaterMover instead,
    // precisely because water is a literal hole with no NavMesh baked
    // over it (see ProceduralGround/WaterMover's own comments). The old
    // RequireComponent silently auto-added a NavMeshAgent the instant
    // AddComponent<Unit>() ran on a boat, and NavMeshAgent immediately
    // warps its GameObject onto the nearest valid NavMesh point on
    // enable - which for any boat spawned inside/near the water hole
    // meant getting yanked onto the nearest patch of dry land, no matter
    // what position the boat was actually told to spawn at. Real,
    // previously-undetected bug (user-reported: boats rendering on land,
    // not water) - verified by adding components one at a time and
    // watching the GameObject's position jump the instant AddComponent
    // <Unit>() ran, from exactly where it was told to spawn to the
    // nearest shore point.
    public class Unit : MonoBehaviour
    {
        public static readonly List<Unit> All = new List<Unit>();

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
